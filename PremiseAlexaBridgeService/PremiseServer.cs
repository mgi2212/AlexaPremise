using Alexa.SmartHome.V3;
using Alexa.RegisteredTasks;
using System;
using System.Configuration;
using SYSWebSockClient;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.Collections;
using System.Net;
using System.Text;
using Nito.AsyncEx;
using System.IO;

namespace PremiseAlexaBridgeService
{
    public sealed class PremiseServer
    {
        private static readonly PremiseServer instance = new PremiseServer();
        private static readonly SYSClient _sysClient = new SYSClient();
        private static readonly AlexaBlockingQueue<StateChangeReportWrapper> stateReportQueue = new AlexaBlockingQueue<StateChangeReportWrapper>();
        private static readonly List<IPremiseSubscription> subscriptions = new List<IPremiseSubscription>();
        private static readonly List<DiscoveryEndpoint> endpoints = new List<DiscoveryEndpoint>();
        private static readonly AsyncLock homeObjectSubscriptionLock = new AsyncLock();
        private static readonly AsyncLock subscriptionsLock = new AsyncLock();
        private static readonly AsyncLock endpointsLock = new AsyncLock();
        private static readonly AsyncLock HomeObjectLock = new AsyncLock();
        private static readonly AsyncLock RootObjectLock = new AsyncLock();

        internal static int AlexaDeviceLimit;
        internal static string AlexaStatusClassPath;
        internal static string AlexaApplianceClassPath;
        internal static string AlexaEndpointClassPath;
        internal static string AlexaLocationClassPath;
        internal static string AlexaPowerStateClassPath;
        internal static string AlexaDimmerStateClassPath;
        internal static string AlexaEventEndpoint;
        internal static string PremiseServerAddress;
        internal static string PremiseUserName;
        internal static string PremiseUserPassword;
        internal static bool enableAsyncEvents;
        internal static IPremiseObject _homeObject;
        internal static IPremiseObject _rootObject;
        internal static IPremiseSubscription _asyncEventSubscription;

        private PremiseServer()
        {
            PremiseServerAddress = ConfigurationManager.AppSettings["premiseServer"];
            PremiseUserName = ConfigurationManager.AppSettings["premiseUser"];
            PremiseUserPassword = ConfigurationManager.AppSettings["premisePassword"];
            AlexaStatusClassPath = ConfigurationManager.AppSettings["premiseAlexaStatusClassPath"];
            AlexaApplianceClassPath = ConfigurationManager.AppSettings["premiseAlexaApplianceClassPath"];
            AlexaLocationClassPath = ConfigurationManager.AppSettings["premiseAlexaLocationClassPath"];
            AlexaEndpointClassPath = ConfigurationManager.AppSettings["premiseAlexaEndpointClassPath"];
            AlexaPowerStateClassPath = ConfigurationManager.AppSettings["premisePowerStateClassPath"];
            AlexaDimmerStateClassPath = ConfigurationManager.AppSettings["premiseDimmerClassPath"];
            AlexaEventEndpoint = ConfigurationManager.AppSettings["alexaEventEndpoint"];
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

            Task.Run(() =>
            {
                BackgroundTaskManager.Run(() =>
                {
                    SendStateChangeReportsToAlexa();
                });
            });
        }

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

        public static bool CheckStatus()
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
                        _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                    }
                    using (RootObjectLock.Lock())
                    {
                        _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                    }
                }
                else if (_homeObject != null)
                {
                    using (homeObjectSubscriptionLock.Lock())
                    {
                        _asyncEventSubscription = null;
                    }
                    using (subscriptionsLock.Lock())
                    {
                        subscriptions.Clear();
                    }
                    using (endpointsLock.Lock())
                    {
                        endpoints.Clear();
                    }
                    using (HomeObjectLock.Lock())
                    {
                        _homeObject = null;
                        _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                    }
                    using (RootObjectLock.Lock())
                    {
                        _rootObject = null;
                        _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                    }
                }
                using (homeObjectSubscriptionLock.Lock())
                {
                    enableAsyncEvents = HomeObject.GetValue("SendAsyncEventsToAlexa").GetAwaiter().GetResult();
                }
                if (IsAsyncEventsEnabled)
                {
                    Task t = Task.Run(() => SubscribeAll());
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

        public static void SubScribeToHomeObjectEvents()
        {
            using (homeObjectSubscriptionLock.Lock())
            {
                if (_asyncEventSubscription == null)
                {
                    _asyncEventSubscription = HomeObject.Subscribe("SendAsyncEventsToAlexa", new Action<dynamic>(EnableAsyncPropertyChanged)).GetAwaiter().GetResult();
                }
            }
        }

        public static void EnableAsyncPropertyChanged(dynamic @params)
        {
            Subscription sub = (Subscription)@params;

            // dont block the event reporting thread.
            Task t = Task.Run(async () =>
           {
               if ((sub.sysObjectId == SYSClient.HomeObjectId) && (sub.propertyName == "SendAsyncEventsToAlexa"))
               {
                   using (homeObjectSubscriptionLock.Lock())
                   {
                       string value = sub.@params;
                       try
                       {
                           enableAsyncEvents = bool.Parse(value);
                       }
                       catch
                       {
                           enableAsyncEvents = false;
                       }
                   }
                   if (IsAsyncEventsEnabled)
                   {
                       await SubscribeAll();
                   }
                   else
                   {
                       await UnsubscribeAll();
                   }
               }
           });

        }

        public static async Task SubscribeAll()
        {
            using (subscriptionsLock.Lock())
            {
                if (endpoints.Count == 0)
                {
                    GetEndpoints().GetAwaiter().GetResult();
                }
                if (subscriptions.Count != 0)
                {
                    UnsubscribeAll().GetAwaiter().GetResult();
                }
                foreach (DiscoveryEndpoint endpoint in endpoints)
                {
                    foreach (Capability capability in endpoint.capabilities)
                    {
                        if (capability.properties.proactivelyReported)
                        {
                            Guid premiseId = new Guid(endpoint.endpointId);
                            IPremiseObject sysObject = await RootObject.GetObject(premiseId.ToString("B"));
                            IPremiseSubscription subscription = null;

                            switch (capability.@interface)
                            {
                                case "Alexa.BrightnessController":
                                    subscription = await sysObject.Subscribe("Brightness", new Action<dynamic>(AlexaPropertyChanged));
                                    break;
                                case "Alexa.PowerController":
                                    subscription = await sysObject.Subscribe("PowerState", new Action<dynamic>(AlexaPropertyChanged));
                                    break;
                                default:
                                    break;
                            }
                            if (subscription != null)
                            {
                                subscriptions.Add(subscription);
                            }
                        }
                    }
                }
            }
        }

        public static async Task UnsubscribeAll()
        {
            using (subscriptionsLock.Lock())
            {
                if (subscriptions != null)
                {
                    foreach (IPremiseSubscription subscription in subscriptions)
                    {
                        await subscription.Unsubscribe();
                    }
                    subscriptions.Clear();
                }
            }
        }

        public static void AlexaPropertyChanged(dynamic @params)
        {
            Subscription sub = (Subscription)@params;
            Task t = Task.Run(() =>
            {
                // build event notification
                Guid premiseId = new Guid(sub.sysObjectId);
                IPremiseObject sysObject = RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                DiscoveryEndpoint endpoint = GetDiscoveryEndpoint(sysObject).GetAwaiter().GetResult();
                string authCode = (string) HomeObject.GetValue("AlexaAsyncAuthorizationCode").GetAwaiter().GetResult();

                AlexaChangeReport changeReport = new AlexaChangeReport();
                changeReport.@event.header.messageID = Guid.NewGuid().ToString("D");
                changeReport.@event.header.@namespace = "Alexa";
                changeReport.@event.header.name = "ChangeReport";
                changeReport.@event.header.payloadVersion = "3";
                changeReport.@event.endpoint.scope.type = "BearerToken";
                changeReport.@event.endpoint.scope.token = authCode;
                changeReport.@event.endpoint.endpointId = premiseId.ToString("B");
                changeReport.@event.endpoint.cookie = endpoint.cookie;
                changeReport.@event.payload.cause.type = "PHYSICAL_INTERACTION";
                AlexaProperty changedProperty = null;
                switch (sub.propertyName)
                {
                    case "Brightness":
                        changedProperty = PremiseAlexaV3Service.GetBrightnessProperty(sysObject);
                        break;
                    case "PowerState":
                        changedProperty = PremiseAlexaV3Service.GetPowerStateProperty(sysObject);
                        break;
                    default:
                        break;
                }
                changeReport.context.properties.Add(changedProperty);

                StateChangeReportWrapper item = new StateChangeReportWrapper
                {
                    Json = JsonConvert.SerializeObject(changeReport)
                };
                stateReportQueue.Enqueue(item);
            });
        }

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
        
        public static PremiseServer Instance
        {
            get { return instance; }
        }

        private static bool IsClientConnected()
        {
            return (_sysClient.ConnectionState == WebSocketState.Open);
        }

        public static async Task<DiscoveryEndpoint> GetDiscoveryEndpoint(IPremiseObject endpoint)
        {

            DiscoveryEndpoint discoveryEndpoint;

            try
            {
                string json = await endpoint.GetValue("discoveryJson");
                discoveryEndpoint = JsonConvert.DeserializeObject<DiscoveryEndpoint>(json, new JsonSerializerSettings()
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
                // discovery json is now generated in Premise script to vastly improve discovery event response time
                var returnClause = new string[] { "discoveryJson", "IsDiscoverable" };
                dynamic whereClause = new System.Dynamic.ExpandoObject();
                whereClause.TypeOf = PremiseServer.AlexaApplianceClassPath;
                int count = 0;
                var devices = await PremiseServer.HomeObject.Select(returnClause, whereClause);

                foreach (var device in devices)
                {
                    if (device.IsDiscoverable == false)
                        continue;

                    DiscoveryEndpoint endpoint = new DiscoveryEndpoint();
                    try
                    {
                        string json = device.discoveryJson.ToString();
                        endpoint = JsonConvert.DeserializeObject<DiscoveryEndpoint>(json, new JsonSerializerSettings()
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
                        if (++count >= PremiseServer.AlexaDeviceLimit)
                        {
                            break;
                        }
                    }
                }
                GC.Collect();
                return endpoints;
            }
        }

        public static void Resubscribe()
        {
            if (IsAsyncEventsEnabled)
            {
                UnsubscribeAll().GetAwaiter().GetResult();
                SubscribeAll().GetAwaiter().GetResult();
            }
        }

        public static bool IsAsyncEventsEnabled
        {
            get {
                using (homeObjectSubscriptionLock.Lock())
                {
                    return enableAsyncEvents;
                }
            }
        }

        private void SendStateChangeReportsToAlexa()
        {
            //blocks in the enumerator
            foreach (StateChangeReportWrapper item in stateReportQueue)
            {
                try
                {
                    // post to Alexa
                    WebRequest request = WebRequest.Create(item.uri);
                    request.Method = WebRequestMethods.Http.Post;
                    request.ContentType = @"application/json";
                    request.ContentLength = item.Json.Length;

                    Stream stream = request.GetRequestStream();
                    stream.Write(item.Bytes, 0, item.Json.Length);
                    stream.Close();
                    WebResponse response = request.GetResponse();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                Debug.WriteLine(item.Json);
                Thread.Sleep(100);
            }
        }
    }

    internal class StateChangeReportWrapper
    {
        public string Json { get; set; }
        public byte[] Bytes
        {
            get
            {
                return Encoding.UTF8.GetBytes(Json);
            }
        }
        public readonly string uri;

        public StateChangeReportWrapper()
        {
            uri = PremiseServer.AlexaEventEndpoint;
        }
    }

    internal class AlexaBlockingQueue<T> : IEnumerable<T>
    {
        private int _count = 0;
        private Queue<T> _queue = new Queue<T>();

        public T Dequeue()
        {
            lock (_queue)
            {
                while (_count <= 0) Monitor.Wait(_queue);
                _count--;
                return _queue.Dequeue();
            }
        }

        public void Enqueue(T data)
        {
            if (data == null) throw new ArgumentNullException("data");

            lock (_queue)
            {
                _queue.Enqueue(data);
                _count++;
                Monitor.Pulse(_queue);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            while (true) yield return Dequeue();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
