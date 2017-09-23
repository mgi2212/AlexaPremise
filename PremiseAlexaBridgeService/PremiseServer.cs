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
        private static string HomeObjectId = "{4F846CA8-6603-4675-AC66-05A0AF6A8ACD}";

        internal static int AlexaDeviceLimit;
        internal static bool AlexaCheckStateBeforeSetValue;
        internal static string AlexaStatusClassPath;
        internal static string AlexaApplianceClassPath;
        internal static string AlexaEndpointClassPath;
        internal static string AlexaLocationClassPath;
        internal static string AlexaPowerStateClassPath;
        internal static string AlexaDimmerStateClassPath;
        internal static string PremiseServerAddress;
        internal static string PremiseUserName;
        internal static string PremiseUserPassword;
        internal static string PremiseHomeObject;
        internal static string AlexaEventEndpoint;

        private static IPremiseObject _homeObject;
        private static IPremiseObject _rootObject;
        private static SYSClient _sysClient;
        private static IPremiseSubscription _asyncEventSubscription;
        private static List<IPremiseSubscription> subscriptions;
        private static List<DiscoveryEndpoint> endpoints;
        private static bool enableAsyncEvents;
        private static AlexaBlockingQueue<PostToAlexa> stateReportQueue;

        private static readonly AsyncLock homeObjectSubscriptionLock = new AsyncLock();
        private static readonly AsyncLock subscriptionsLock = new AsyncLock();
        private static readonly AsyncLock endpointsLock = new AsyncLock();


        private PremiseServer()
        {
            PremiseServerAddress = ConfigurationManager.AppSettings["premiseServer"];
            PremiseUserName = ConfigurationManager.AppSettings["premiseUser"];
            PremiseUserPassword = ConfigurationManager.AppSettings["premisePassword"];
            PremiseHomeObject = ConfigurationManager.AppSettings["premiseHomeObject"];
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

            try
            {
                AlexaCheckStateBeforeSetValue = bool.Parse(ConfigurationManager.AppSettings["AlexaCheckStateBeforeSetValue"]);
            }
            catch (Exception)
            {
                AlexaCheckStateBeforeSetValue = true;
            }

            subscriptions = new List<IPremiseSubscription>();
            endpoints = new List<DiscoveryEndpoint>();
            enableAsyncEvents = false;
            stateReportQueue = new AlexaBlockingQueue<PostToAlexa>();
            _sysClient = new SYSClient();
            _asyncEventSubscription = null;

            Task.Run(() =>
            {
                BackgroundTaskManager.Run(() =>
                {
                    this.SendChangeReportsToAlexa();
                });
            });
            //CheckStatus();
        }

        ~PremiseServer()
        {
            if (_homeObject == null)
            {
                return;
            }
            if (subscriptions != null)
            {
                UnsubscribeAll().GetAwaiter().GetResult();
            }
            subscriptions = null;

            _asyncEventSubscription.Unsubscribe().GetAwaiter().GetResult();
            _asyncEventSubscription = null;

            if (endpoints != null)
            {
                using (endpointsLock.Lock())
                {
                    endpoints.Clear();
                }
            }
            endpoints = null;

            _sysClient.Disconnect();
            _homeObject = null;
            _rootObject = null;
        }

        public static bool CheckStatus()
        {
            try
            {
                if (isClientConnected())
                {
                    return true;
                }

                if (_homeObject == null)
                {
                    _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                    _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                    enableAsyncEvents = _homeObject.GetValue("SendAsyncEventsToAlexa").GetAwaiter().GetResult();
                    if (enableAsyncEvents)
                    {
                        Task t = Task.Run(() => SubscribeAll());
                    }
                }
                else if (_homeObject != null)
                {
                    _homeObject = null;
                    _rootObject = null;
                    using (homeObjectSubscriptionLock.Lock())
                    {
                        _asyncEventSubscription = null;
                    }
                    if (enableAsyncEvents)
                    {
                        using (subscriptionsLock.Lock())
                        {
                            subscriptions.Clear();
                        }
                    }
                    using (endpointsLock.Lock())
                    {
                        endpoints.Clear();
                    }
                    _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                    _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                    enableAsyncEvents = _rootObject.GetValue("SendAsyncEventsToAlexa").GetAwaiter().GetResult();
                    if (enableAsyncEvents)
                    {
                        Task t = Task.Run(() => SubscribeAll());
                    }

                }
                SubScribeToHomeObjectEvents();
                return true;
            }
            catch (Exception ex)
            {
                _homeObject = null;
                _rootObject = null;
                if (enableAsyncEvents)
                {
                    subscriptions.Clear();
                }
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
                    _asyncEventSubscription = _homeObject.Subscribe("SendAsyncEventsToAlexa", new Action<dynamic>(EnableAsyncPropertyChanged)).GetAwaiter().GetResult();
                }
            }
        }

        public static IPremiseObject SysRootObject
        {
            get
            {
                CheckStatus();
                return _rootObject;
            }
        }

        public static IPremiseObject SysHomeObject
        {
            get
            {
                CheckStatus();
                return _homeObject;
            }
        }

        public static void EnableAsyncPropertyChanged(dynamic @params)
        {
            Subscription sub = (Subscription)@params;

            // dont block the event reporting thread.
            Task t = Task.Run(async () =>
           {
               if ((sub.sysObjectId == HomeObjectId) && (sub.propertyName == "SendAsyncEventsToAlexa"))
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
                   if (enableAsyncEvents)
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
                            IPremiseObject sysObject = await _rootObject.GetObject(premiseId.ToString("B"));
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
                IPremiseObject sysObject = _rootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                DiscoveryEndpoint endpoint = GetDiscoveryEndpoint(sysObject).GetAwaiter().GetResult();
                string authCode = (string)_homeObject.GetValue("AlexaAsyncAuthorizationCode").GetAwaiter().GetResult();

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

                PostToAlexa item = new PostToAlexa();
                item.json = JsonConvert.SerializeObject(changeReport);
                stateReportQueue.Enqueue(item);
            });
        }

        public static IPremiseObject ConnectToServer(SYSClient client)
        {
            CheckStatus();
            return _homeObject;
        }

        public static void DisconnectServer(SYSClient client)
        {
            CheckStatus();
            return;
        }

        public static PremiseServer Instance
        {
            get { return instance; }
        }

        private static bool isClientConnected()
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
                var devices = await _homeObject.Select(returnClause, whereClause);

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
                return endpoints;
            }
        }

        public static void Resubscribe()
        {
            if (enableAsyncEvents)
            {
                UnsubscribeAll().GetAwaiter().GetResult();
                SubscribeAll().GetAwaiter().GetResult();
            }
        }

        public static bool areAsyncEventsEnabled
        {
            get { return enableAsyncEvents; }
        }

        private void SendChangeReportsToAlexa()
        {
            //blocks in the enumerator
            foreach (PostToAlexa item in stateReportQueue)
            {
                try
                {
                    // post to Alexa
                    WebRequest request = WebRequest.Create(item.uri);
                    request.Method = WebRequestMethods.Http.Post;
                    request.ContentType = @"application/json";
                    request.ContentLength = item.json.Length;

                    Stream stream = request.GetRequestStream();
                    stream.Write(item.Bytes, 0, item.json.Length);
                    stream.Close();
                    WebResponse response = request.GetResponse();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                Debug.WriteLine(item.json);
                Thread.Sleep(100);
            }
        }
    }

    internal class PostToAlexa
    {
        public string json { get; set; }
        public byte[] Bytes
        {
            get
            {
                return Encoding.UTF8.GetBytes(json);
            }
        }
        public readonly string uri;

        public PostToAlexa()
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
