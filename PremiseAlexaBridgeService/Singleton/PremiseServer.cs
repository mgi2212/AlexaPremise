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
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
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

            var interfaceType = typeof(IAlexaController);
            var all = AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(x => x.GetTypes())
              .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
              .Select(Activator.CreateInstance);

            Controllers = new Dictionary<string, IAlexaController>();

            List<string> controllerNames = new List<string>();

            // cache the set of controllers
            foreach (dynamic controller in all)
            {
                Controllers.Add(controller.GetNameSpace(), controller);
                controllerNames.Add(controller.GetNameSpace());
            }

            Task.Run(() =>
            {
                BackgroundTaskManager.Run(() =>
                {
                    SendStateChangeReportsToAlexa();
                });
            });

            controllerNames.Sort();
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

            UnsubscribeAllAsync().GetAwaiter().GetResult();

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

        public static string GetClientIp()
        {
            string address = "unavailable";
            try
            {
                OperationContext context = OperationContext.Current;
                MessageProperties properties = context.IncomingMessageProperties;

                if (!(properties[RemoteEndpointMessageProperty.Name] is RemoteEndpointMessageProperty endpoint))
                {
                    return address;
                }
                if (properties.Keys.Contains(HttpRequestMessageProperty.Name))
                {
                    if (properties[HttpRequestMessageProperty.Name] is HttpRequestMessageProperty endpointLoadBalancer && endpointLoadBalancer.Headers["X-Forwarded-For"] != null)
                        address = endpointLoadBalancer.Headers["X-Forwarded-For"];
                }
                if (address == "unavailable")
                {
                    address = endpoint.Address;
                }
            }
            catch (Exception e)
            {
                NotifyErrorAsync(EventLogEntryType.Warning, $"Error obtaining client IP address: {e.Message}", 201).GetAwaiter().GetResult();
            }
            return address;
        }

        public static async Task<DiscoveryEndpoint> GetDiscoveryEndpointAsync(IPremiseObject endpoint)
        {
            DiscoveryEndpoint discoveryEndpoint;

            try
            {
                string json = await endpoint.GetValue("discoveryJson").ConfigureAwait(false);
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

        public static async Task<List<DiscoveryEndpoint>> GetEndpointsAsync()
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
                var devices = await HomeObject.Select(returnClause, whereClause).ConfigureAwait(false);

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

                // Seems like a good idea here.
                HostingEnvironment.QueueBackgroundWorkItem(ct => BackgroundGarbageCollectAsync());

                return endpoints;
            }
        }

        public static async Task IncrementCounterAsync(string property)
        {
            int count = await HomeObject.GetValue<int>(property).ConfigureAwait(false);
            count++;
            await HomeObject.SetValue(property, count.ToString()).ConfigureAwait(false);
        }

        public static bool IsClientConnected()
        {
            return (_sysClient.ConnectionState == WebSocketState.Open);
        }

        public static async Task NotifyErrorAsync(EventLogEntryType errorType, string errorMessage, int id)
        {
            await HomeObject.SetValue("AlexaLastCommunicationsError", errorMessage).ConfigureAwait(false);
            await IncrementCounterAsync("AlexaCommunicationsErrorCount").ConfigureAwait(false);
            WriteToWindowsApplicationEventLog(errorType, errorMessage, id);
            Debug.WriteLine(errorMessage);
        }

        public static void WriteToWindowsApplicationEventLog(EventLogEntryType logType, string message, int id)
        {
            const string logSource = "PremiseBridge";
            if (!EventLog.SourceExists(logSource))
            {
                Debug.WriteLine("No event source for application log.  Please run install-premise power shell script. ");
                return;
            }
            EventLog.WriteEntry(logSource, message, logType, id);
        }

        internal static async Task<bool> CheckAccessTokenAsync(string token)
        {
            var accessToken = await HomeObject.GetValue<string>("AccessToken").ConfigureAwait(false);
            var tokens = new List<string>(accessToken.Split(','));
            return (-1 != tokens.IndexOf(token));
        }

        internal static string GetUtcTime()
        {
            return DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
        }

        internal static async Task InformLastContactAsync(string command)
        {
            command += $" Client ip: {GetClientIp()}";
            await HomeObject.SetValue("LastHeardFromAlexa", DateTime.Now.ToString(CultureInfo.CurrentCulture)).ConfigureAwait(false);
            await HomeObject.SetValue("LastHeardCommand", command).ConfigureAwait(false);
        }

        internal static void WarmUpCache()
        {
            if (CheckStatus())
            {
                GetEndpointsAsync().GetAwaiter().GetResult();
            }
        }

        private static async Task BackgroundGarbageCollectAsync()
        {
            // collect garbage after 5 seconds.
            Thread.Sleep(5000);
            await Task.Run(() =>
            {
                GC.Collect();
            }).ConfigureAwait(false);
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
                    Task.Run(SubscribeAllAsync);
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