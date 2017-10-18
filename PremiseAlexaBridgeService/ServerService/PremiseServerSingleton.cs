using Alexa;
using Alexa.Controller;
using Alexa.Scene;
using Alexa.RegisteredTasks;
using Alexa.SmartHomeAPI.V3;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SYSWebSockClient;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace PremiseAlexaBridgeService
{
    public sealed class PremiseServer
    {
        private static readonly PremiseServer instance = new PremiseServer();
        private static readonly SYSClient _sysClient = new SYSClient();
        private static readonly AlexaBlockingQueue<StateChangeReportWrapper> stateReportQueue = new AlexaBlockingQueue<StateChangeReportWrapper>();
        private static readonly Dictionary<string, IPremiseSubscription> subscriptions = new Dictionary<string, IPremiseSubscription>();
        private static readonly List<DiscoveryEndpoint> endpoints = new List<DiscoveryEndpoint>();
        private static readonly AsyncLock homeObjectSubscriptionLock = new AsyncLock();
        private static readonly AsyncLock subscriptionsLock = new AsyncLock();
        private static readonly AsyncLock endpointsLock = new AsyncLock();
        private static readonly AsyncLock HomeObjectLock = new AsyncLock();
        private static readonly AsyncLock RootObjectLock = new AsyncLock();

        public static int AlexaDeviceLimit;
        public static string AlexaApplianceClassPath;
        public static string AlexaEndpointClassPath;
        public static string AlexaLocationClassPath;
        public static string AlexaEventEndpoint;
        public static string PremiseServerAddress;
        public static string PremiseUserName;
        public static string PremiseUserPassword;
        public static string AlexaEventTokenRefreshEndpoint;
        public static bool enableAsyncEvents;
        public static IPremiseObject _homeObject;
        public static IPremiseObject _rootObject;
        public static IPremiseSubscription _asyncEventSubscription;
        public static Dictionary<string, IAlexaController> Controllers { get; set; }

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

            // Increase and limit threadpool size
            ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
            workerThreads = 1000;
            ThreadPool.SetMaxThreads(workerThreads, completionPortThreads);

            var interfaceType = typeof(IAlexaController);
            var all = AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(x => x.GetTypes())
              .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
              .Select(x => Activator.CreateInstance(x));

            Controllers = new Dictionary<string, IAlexaController>();

            // cache a set of controllers that support a unique alexa property type
            // needed to insure we report the actual property that changed 
            foreach (dynamic controller in all)
            {
                foreach (string alexaProperty in controller.GetAlexaProperties())
                {
                    if (!Controllers.ContainsKey(alexaProperty))
                    {
                        Controllers.Add(alexaProperty, controller);
                    }
                }
            }

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
                            return false;
                        }
                    }
                    using (RootObjectLock.Lock())
                    {
                        _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                    }
                }
                else if (_homeObject != null)
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

        private static void SubScribeToHomeObjectEvents()
        {
            using (homeObjectSubscriptionLock.Lock())
            {
                if (_asyncEventSubscription == null)
                {
                    _asyncEventSubscription = HomeObject.Subscribe("SendAsyncEventsToAlexa", "NoController", new Action<dynamic>(EnableAsyncPropertyChanged)).GetAwaiter().GetResult();
                }
            }
        }

        private static void EnableAsyncPropertyChanged(dynamic @params)
        {
            Subscription sub = (Subscription)@params;

            // can't block the event reporting thread.
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

        private static async Task SubscribeAll()
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
                Action<dynamic> callback = new Action<dynamic>(AlexaPropertyChanged);
                using (endpointsLock.Lock())
                {
                    foreach (DiscoveryEndpoint discoveryEndpoint in endpoints)
                    {
                        Guid premiseId = new Guid(discoveryEndpoint.endpointId);
                        IPremiseObject endpoint = await RootObject.GetObject(premiseId.ToString("B"));

                        // use reflection to instantiate all device type controllers
                        var interfaceType = typeof(IAlexaDeviceType);
                        var all = AppDomain.CurrentDomain.GetAssemblies()
                          .SelectMany(x => x.GetTypes())
                          .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                          .Select(x => Activator.CreateInstance(x));

                        foreach (IAlexaDeviceType deviceType in all)
                        {
                            deviceType.SubcribeToSupportedProperties(endpoint, discoveryEndpoint, callback);
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
                    foreach (KeyValuePair<string, IPremiseSubscription> subscription in subscriptions)
                    {
                        await subscription.Value.Unsubscribe();
                    }
                    subscriptions.Clear();
                }
            }
        }

        public static AsyncLock deDupeLock = new AsyncLock();
        private static Dictionary<string, Subscription> deDupeDictionary = new Dictionary<string, Subscription>();

        public static void AlexaPropertyChanged(dynamic @params)
        {
            Subscription sub = (Subscription)@params;
            using (deDupeLock.Lock())
            {
                if (DeDupeDictionary.ContainsKey(sub.sysObjectId))
                    return;
                DeDupeDictionary.Add(sub.sysObjectId, sub);
            }

            Task t = Task.Run(() =>
            {
                // build event notification
                Guid premiseId = new Guid(sub.sysObjectId);
                IPremiseObject endpoint = RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                DiscoveryEndpoint discoveryEndpoint = GetDiscoveryEndpoint(endpoint).GetAwaiter().GetResult();
                if (discoveryEndpoint == null)
                {
                    // object deleted! 
                    return;
                }
                string authCode = (string)HomeObject.GetValue("AlexaAsyncAuthorizationCode").GetAwaiter().GetResult();
                AlexaChangeReport changeReport = new AlexaChangeReport();
                changeReport.@event.header.messageID = Guid.NewGuid().ToString("D");
                changeReport.@event.header.@namespace = "Alexa";
                changeReport.@event.header.payloadVersion = "3";
                changeReport.@event.endpoint.scope.type = "BearerToken";
                changeReport.@event.endpoint.scope.token = authCode;
                changeReport.@event.endpoint.endpointId = premiseId.ToString("D").ToUpper();
                changeReport.@event.endpoint.cookie = discoveryEndpoint.cookie;
                changeReport.@event.payload.change.cause.type = "PHYSICAL_INTERACTION";

                // use reflection to instantiate all device type controllers
                var interfaceType = typeof(IAlexaDeviceType);
                var all = AppDomain.CurrentDomain.GetAssemblies()
                  .SelectMany(x => x.GetTypes())
                  .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                  .Select(x => Activator.CreateInstance(x));

                string causeKey = "";

                foreach (IAlexaDeviceType deviceType in all)
                {
                    var related = deviceType.FindRelatedProperties(endpoint, "");
                    foreach (AlexaProperty prop in related)
                    {
                        if (prop.@namespace == "Alexa.SceneController")
                        {
                            continue;
                        }
                        string propKey = prop.@namespace + "." + prop.name;

                        dynamic controller = Controllers[prop.name];
                        // filter for the property that triggered the actual change, unless it is the beloved SceneController.
                        if ((controller.HasPremiseProperty(sub.propertyName)) && (changeReport.@event.payload.change.properties.Count == 0))
                        {
                            causeKey = propKey;
                            changeReport.@event.payload.change.properties.Add(prop);
                            if (changeReport.context.propertiesInternal.ContainsKey(causeKey))
                            {
                                changeReport.context.propertiesInternal.Remove(causeKey);
                            }
                        }
                        else if ((!changeReport.context.propertiesInternal.ContainsKey(propKey)) && (propKey != causeKey))
                        {
                            changeReport.context.propertiesInternal.Add(propKey, prop);
                        }
                    }
                }

                changeReport.@event.header.name = "ChangeReport";

                foreach (Capability capability in discoveryEndpoint.capabilities)
                {
                    switch (capability.@interface)  // scenes are special cased
                    {
                        case "Alexa.SceneController":
                            {
                                AlexaSetSceneController sceneController = new AlexaSetSceneController("", endpoint);
                                changeReport = sceneController.AlterChangeReport(changeReport);
                            }
                            break;

                        default:
                            break;
                    }
                }

                StateChangeReportWrapper item = new StateChangeReportWrapper
                {
                    Json = JsonConvert.SerializeObject(changeReport, new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    })
                };

                stateReportQueue.Enqueue(item);
                using (deDupeLock.Lock())
                {
                    DeDupeDictionary.Remove(sub.sysObjectId);
                }
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

        public static bool IsClientConnected()
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
                // discovery json is now generated in Premise vb script to vastly improve discovery event response time
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
                GC.Collect(); // Seems like a good idea here.
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
            get
            {
                using (homeObjectSubscriptionLock.Lock())
                {
                    return enableAsyncEvents;
                }
            }
        }

        public static Dictionary<string, Subscription> DeDupeDictionary { get => deDupeDictionary; set => deDupeDictionary = value; }

        private static void RefreshAsyncToken()
        {
            WebRequest refreshRequest = WebRequest.Create(PremiseServer.AlexaEventTokenRefreshEndpoint);
            refreshRequest.Method = WebRequestMethods.Http.Post;
            refreshRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            string refresh_token = PremiseServer.HomeObject.GetValue<string>("AlexaAsyncAuthorizationRefreshToken").GetAwaiter().GetResult();
            string client_id = PremiseServer.HomeObject.GetValue<string>("AlexaAsyncAuthorizationClientId").GetAwaiter().GetResult();
            string client_secret = PremiseServer.HomeObject.GetValue<string>("AlexaAsyncAuthorizationSecret").GetAwaiter().GetResult();
            string refreshData = string.Format("grant_type=refresh_token&refresh_token={0}&client_id={1}&client_secret={2}", refresh_token, client_id, client_secret);
            Stream stream = refreshRequest.GetRequestStream();
            stream.Write(Encoding.UTF8.GetBytes(refreshData), 0, Encoding.UTF8.GetByteCount(refreshData));
            stream.Close();
            using (HttpWebResponse httpResponse = refreshRequest.GetResponse() as HttpWebResponse)
            {
                String responseString = "";

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    Debug.WriteLine("could not get refresh token!");
                    return;
                }

                using (Stream response = httpResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(response, Encoding.UTF8);
                    responseString = reader.ReadToEnd();
                }

                JObject json = JObject.Parse(responseString);

                Debug.WriteLine("access_token={0}", json["access_token"].ToString());
                Debug.WriteLine("refresh_token={0}", json["refresh_token"].ToString());
                Debug.WriteLine("token_type={0}", json["token_type"].ToString());
                Debug.WriteLine("expirse_in={0}", json["expires_in"].ToString());

                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationCode", json["access_token"].ToString()).GetAwaiter().GetResult();
                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationRefreshToken", json["refresh_token"].ToString()).GetAwaiter().GetResult();
                DateTime expiry = DateTime.UtcNow.AddSeconds((double)json["expires_in"]);
                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationCodeExpiry", expiry.ToString()).GetAwaiter().GetResult();

                Debug.WriteLine("refresh response:" + responseString);
            }
        }

        private void SendStateChangeReportsToAlexa()
        {
            // This queue blocks in the enumerator so this is essentially an infinate loop. 
            foreach (StateChangeReportWrapper item in stateReportQueue)
            {
                if (item.Sent == true)  // should never happen
                    continue;

                try
                {
                    string expiry = PremiseServer.HomeObject.GetValue<string>("AlexaAsyncAuthorizationCodeExpiry").GetAwaiter().GetResult();
                    int asyncUpdateCount = PremiseServer.HomeObject.GetValue<int>("AlexaAsyncUpdateCount").GetAwaiter().GetResult();

                    DateTime expiryDateTime = new DateTime();
                    if (DateTime.TryParse(expiry, out expiryDateTime))
                    {
                        if (DateTime.Compare(DateTime.UtcNow, expiryDateTime) >= 0)
                        {
                            RefreshAsyncToken();
                            Thread.Sleep(1000); // give amazon some time to register the refresh
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No Authorization to send state changes");
                        continue;
                    }

                    item.Sent = true;
                    WebRequest request = WebRequest.Create(item.uri);
                    request.Method = WebRequestMethods.Http.Post;
                    request.ContentType = @"application/json";
                    request.ContentLength = item.Json.Length;

                    Stream stream = request.GetRequestStream();
                    stream.Write(item.Bytes, 0, item.Json.Length);
                    stream.Close();
                    WebResponse response = request.GetResponse();
                    Debug.WriteLine("response:" + response.ToString());

                    asyncUpdateCount++;
                    PremiseServer.HomeObject.SetValue("AlexaAsyncUpdateCount", asyncUpdateCount.ToString());
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

    public class StateChangeReportWrapper
    {
        public string Json { get; set; }
        public bool Sent { get; set; }
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
            Sent = false;
            uri = PremiseServer.AlexaEventEndpoint;
        }
    }

}
