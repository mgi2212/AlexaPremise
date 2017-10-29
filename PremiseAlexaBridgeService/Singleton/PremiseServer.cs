using Alexa.Controller;
using Alexa.RegisteredTasks;
using Alexa.SmartHomeAPI.V3;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    public sealed partial class PremiseServer
    {
        #region Fields

        public static IPremiseObject _homeObject;
        public static IPremiseObject _rootObject;
        public static string AlexaApplianceClassPath;
        public static string AlexaAudioVideoInput;
        public static int AlexaDeviceLimit;
        public static string AlexaEndpointClassPath;
        public static string AlexaEventEndpoint;
        public static string AlexaEventTokenRefreshEndpoint;
        public static string AlexaLocationClassPath;
        public static string AlexaMatrixSwitcherZone;
        public static AsyncLock deDupeLock = new AsyncLock();
        public static string PremiseServerAddress;
        public static string PremiseUserName;
        public static string PremiseUserPassword;
        private static readonly SYSClient _sysClient = new SYSClient();
        private static readonly AsyncLock asyncObjectsLock = new AsyncLock();
        private static readonly List<DiscoveryEndpoint> endpoints = new List<DiscoveryEndpoint>();
        private static readonly AsyncLock endpointsLock = new AsyncLock();
        private static readonly AsyncLock HomeObjectLock = new AsyncLock();
        private static readonly AsyncLock RootObjectLock = new AsyncLock();
        private static readonly AlexaBlockingQueue<StateChangeReportWrapper> stateReportQueue = new AlexaBlockingQueue<StateChangeReportWrapper>();

        #endregion Fields

        #region Constructors

        private PremiseServer()
        {
            PremiseServerAddress = ConfigurationManager.AppSettings["premiseServer"];
            PremiseUserName = ConfigurationManager.AppSettings["premiseUser"];
            PremiseUserPassword = ConfigurationManager.AppSettings["premisePassword"];
            AlexaApplianceClassPath = ConfigurationManager.AppSettings["premiseAlexaApplianceClassPath"];
            AlexaLocationClassPath = ConfigurationManager.AppSettings["premiseAlexaLocationClassPath"];
            AlexaEndpointClassPath = ConfigurationManager.AppSettings["premiseAlexaEndpointClassPath"];
            AlexaEventEndpoint = ConfigurationManager.AppSettings["alexaEventEndpoint"];
            AlexaEventTokenRefreshEndpoint = ConfigurationManager.AppSettings["loginWithAmazonEndpoint"];
            AlexaAudioVideoInput = ConfigurationManager.AppSettings["premiseAlexaAudioVideoInput"];
            AlexaMatrixSwitcherZone = ConfigurationManager.AppSettings["premiseAlexaMatrixSwitcherZone"];

            try
            {
                AlexaDeviceLimit = int.Parse(ConfigurationManager.AppSettings["premiseAlexaDeviceLimit"]);
            }
            catch (Exception)
            {
                AlexaDeviceLimit = 300;
            }

            enableAsyncEvents = false;
            _asyncEventSubscription = null;

            //// Increase and limit threadpool size
            //ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
            //workerThreads = 1000;
            //ThreadPool.SetMaxThreads(workerThreads, completionPortThreads);

            var interfaceType = typeof(IAlexaController);
            var all = AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(x => x.GetTypes())
              .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
              .Select(Activator.CreateInstance);

            Controllers = new Dictionary<string, IAlexaController>();

            List<string> controllerNames = new List<string>();

            // cache a set of controllers that support a unique alexa property type needed to insure
            // we report the actual property that changed
            foreach (dynamic controller in all)
            {
                foreach (string alexaProperty in controller.GetAlexaProperties())
                {
                    if (!Controllers.ContainsKey(alexaProperty))
                    {
                        string controllerName = ((IAlexaController)controller).GetNameSpace();
                        if (!controllerNames.Contains(controllerName))
                        {
                            controllerNames.Add(controllerName);
                        }
                        Controllers.Add(alexaProperty, controller);
                    }
                }
            }

            controllerNames.Sort();

            Task.Run(() =>
            {
                BackgroundTaskManager.Run(() =>
                {
                    SendStateChangeReportsToAlexa();
                });
            });

            string xmlElements = new XElement("interfaces", controllerNames.Select(i => new XElement("interface", i))).ToString();
            WriteToWindowsApplicationEventLog(EventLogEntryType.Information, $"PremiseAlexaBridge has started with {controllerNames.Count} interfaces:\r\n {xmlElements}", 1);
        }

        #endregion Constructors

        #region Destructors

        ~PremiseServer()
        {
            if (_homeObject == null)
            {
                return;
            }

            UnsubscribeAll().GetAwaiter().GetResult();

            _asyncEventSubscription?.Unsubscribe().GetAwaiter().GetResult();
            _asyncEventSubscription = null;

            using (endpointsLock.Lock())
            {
                endpoints?.Clear();
            }

            _sysClient.Disconnect();
            _homeObject = null;
            _rootObject = null;
        }

        #endregion Destructors

        #region Properties

        public static AsyncLock AsyncObjectsLock => asyncObjectsLock;

        public static Dictionary<string, IAlexaController> Controllers { get; set; }
        public static Dictionary<string, Subscription> DeDupeDictionary { get; set; } = new Dictionary<string, Subscription>();

        public static IPremiseObject HomeObject
        {
            get
            {
                CheckStatus();
                using (HomeObjectLock.Lock())
                {
                    return _homeObject;
                }
            }
        }

        public static PremiseServer Instance { get; } = new PremiseServer();

        public static IPremiseObject RootObject
        {
            get
            {
                CheckStatus();
                using (RootObjectLock.Lock())
                {
                    return _rootObject;
                }
            }
        }

        #endregion Properties

        #region Methods

        public static async Task<DiscoveryEndpoint> GetDiscoveryEndpoint(IPremiseObject endpoint)
        {
            DiscoveryEndpoint discoveryEndpoint;

            try
            {
                string json = await endpoint.GetValue("discoveryJson");
                discoveryEndpoint = JsonConvert.DeserializeObject<DiscoveryEndpoint>(json, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            }
            catch
            {
                discoveryEndpoint = null;
            }

            return discoveryEndpoint;
        }

        public static async Task<List<DiscoveryEndpoint>> GetEndpoints()
        {
            using (endpointsLock.Lock())
            {
                if (endpoints.Count != 0)
                {
                    endpoints.Clear();
                }
                // discovery json is now generated in Premise vb script to vastly improve discovery
                // event response time
                var returnClause = new[] { "discoveryJson", "IsDiscoverable" };
                dynamic whereClause = new ExpandoObject();
                whereClause.TypeOf = AlexaApplianceClassPath;
                int count = 0;
                var devices = await HomeObject.Select(returnClause, whereClause);

                foreach (var device in devices)
                {
                    if (device.IsDiscoverable == false)
                        continue;

                    DiscoveryEndpoint endpoint;
                    try
                    {
                        string json = device.discoveryJson.ToString();
                        if (string.IsNullOrEmpty(json))
                        {
                            continue;
                        }
                        endpoint = JsonConvert.DeserializeObject<DiscoveryEndpoint>(json, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                    }
                    catch
                    {
                        continue;
                    }

                    if (endpoint != null)
                    {
                        endpoints.Add(endpoint);
                        if (++count >= AlexaDeviceLimit)
                        {
                            break;
                        }
                    }
                }
                GC.Collect(); // Seems like a good idea here.
                return endpoints;
            }
        }

        public static async Task IncrementCounter(string property)
        {
            int count = await HomeObject.GetValue<int>(property);
            count++;
            await HomeObject.SetValue(property, count.ToString());
        }

        public static bool IsClientConnected()
        {
            return (_sysClient.ConnectionState == WebSocketState.Open);
        }

        public static async Task NotifyError(EventLogEntryType errorType, string errorMessage, int id)
        {
            await HomeObject.SetValue("AlexaLastCommunicationsError", errorMessage);
            await IncrementCounter("AlexaCommunicationsErrorCount");
            WriteToWindowsApplicationEventLog(errorType, errorMessage, id);
            Debug.WriteLine(errorMessage);
        }

        public static void WriteToWindowsApplicationEventLog(EventLogEntryType logType, string message, int id)
        {
            const string logSource = "PremiseBridge";
            if (!EventLog.SourceExists(logSource))
            {
                Debug.WriteLine("No event source for application log.  Please run install-premise powershell script. ");
                return;
            }
            EventLog.WriteEntry(logSource, message, logType, id);
        }

        internal static async void WarmUpCache()
        {
            if (CheckStatus())
            {
                await GetEndpoints();
            }
        }

        private static bool CheckStatus()
        {
            try
            {
                if (IsClientConnected())
                {
                    return true;
                }

                if (_homeObject == null)
                {
                    using (HomeObjectLock.Lock())
                    {
                        try
                        {
                            _homeObject = _sysClient.Connect(PremiseServerAddress)?.GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            _homeObject = null;
                        }
                    }

                    if (_homeObject == null)
                    {
                        Debug.WriteLine("Cannot connect to Premise server!");
                        return false;
                    }
                    using (RootObjectLock.Lock())
                    {
                        _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                    }
                }
                else
                {
                    _sysClient.Disconnect();

                    using (homeObjectSubscriptionLock.Lock())
                    {
                        _asyncEventSubscription = null;
                    }
                    using (subscriptionsLock.Lock())
                    {
                        subscriptions.Clear();
                        _sysClient.Subscriptions.Clear();
                    }
                    using (endpointsLock.Lock())
                    {
                        endpoints.Clear();
                    }
                    using (HomeObjectLock.Lock())
                    {
                        _homeObject = null;
                        try
                        {
                            _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                        }
                        catch
                        {
                            _homeObject = null;
                            return false;
                        }
                    }
                    using (RootObjectLock.Lock())
                    {
                        _rootObject = null;
                        _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                    }
                }
                using (homeObjectSubscriptionLock.Lock())
                {
                    enableAsyncEvents = HomeObject.GetValue<bool>("SendAsyncEventsToAlexa").GetAwaiter().GetResult();
                }
                if (IsAsyncEventsEnabled)
                {
                    Task.Run(SubscribeAll);
                }
                SubScribeToHomeObjectEvents();
                return true;
            }
            catch (Exception ex)
            {
                _homeObject = null;
                _rootObject = null;
                subscriptions.Clear();
                endpoints.Clear();
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion Methods
    }
}