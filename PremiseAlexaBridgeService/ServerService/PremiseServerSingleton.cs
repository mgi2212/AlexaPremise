﻿using Alexa.Lighting;
using Alexa.Power;
using Alexa.SceneController;
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
        public static string AlexaStatusClassPath;
        public static string AlexaApplianceClassPath;
        public static string AlexaEndpointClassPath;
        public static string AlexaLocationClassPath;
        public static string AlexaPowerStateClassPath;
        public static string AlexaDimmerStateClassPath;
        public static string AlexaEventEndpoint;
        public static string PremiseServerAddress;
        public static string PremiseUserName;
        public static string PremiseUserPassword;
        public static bool enableAsyncEvents;
        public static IPremiseObject _homeObject;
        public static IPremiseObject _rootObject;
        public static IPremiseSubscription _asyncEventSubscription;

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

            // Increase and limit threadpool size
            int workerThreads, completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            workerThreads = 1000;
            ThreadPool.SetMaxThreads(workerThreads, completionPortThreads);

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
                foreach (DiscoveryEndpoint endpoint in endpoints)
                {
                    foreach (Capability capability in endpoint.capabilities)
                    {
                        Guid premiseId = new Guid(endpoint.endpointId);
                        IPremiseObject sysObject = await RootObject.GetObject(premiseId.ToString("B"));
                        IPremiseSubscription subscription = null;
                        if (!subscriptions.ContainsKey(endpoint.endpointId + "." + capability.@interface))
                        {
                            if ((capability.HasProperties() == false) && (capability.proactivelyReported == true)) // scense have no properties but use Powerstate
                            {
                                subscription = await sysObject.Subscribe("PowerState", capability.@interface, new Action<dynamic>(AlexaPropertyChanged));  // scenes use powerstate
                            }
                            else if (capability.properties == null)
                            {
                                Debug.Assert(false, "unhandled discovery condition");
                                continue;
                            }
                            else if (capability.properties.proactivelyReported)
                            {
                                switch (capability.@interface)
                                {
                                    case "Alexa.PowerController":
                                        subscription = await sysObject.Subscribe("PowerState", capability.@interface, new Action<dynamic>(AlexaPropertyChanged));
                                        break;
                                    case "Alexa.BrightnessController":
                                        subscription = await sysObject.Subscribe("Brightness", capability.@interface, new Action<dynamic>(AlexaPropertyChanged));
                                        break;
                                    case "Alexa.ColorTemperatureController":
                                        subscription = await sysObject.Subscribe("Temperature", capability.@interface, new Action<dynamic>(AlexaPropertyChanged));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        if (subscription != null)
                        {
                            subscriptions.Add(endpoint.endpointId + "." + capability.@interface, subscription);
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
                DiscoveryEndpoint disoveryEndpoint = GetDiscoveryEndpoint(endpoint).GetAwaiter().GetResult();
                if (disoveryEndpoint == null)
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
                changeReport.@event.endpoint.endpointId = premiseId.ToString("B");
                changeReport.@event.endpoint.cookie = disoveryEndpoint.cookie;
                changeReport.@event.payload.cause.type = "PHYSICAL_INTERACTION";
                AlexaProperty changedProperty = null;
                foreach (Capability capability in disoveryEndpoint.capabilities)
                {
                    if ((capability.HasProperties() == false))
                    {
                        switch (capability.@interface)
                        {
                            case "Alexa.SceneController":
                                {
                                    AlexaSetSceneController controller = new AlexaSetSceneController(endpoint);
                                    AlexaProperty prop = controller.GetPropertyState();
                                    changeReport.@event.header.name = (string)prop.value;
                                    changeReport.@event.payload.timestamp = prop.timeOfSample;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    else
                    {
                        changeReport.@event.header.name = "ChangeReport";
                        switch (capability.@interface)
                        {
                            case "Alexa.PowerController":
                                {
                                    AlexaSetPowerStateController controller = new AlexaSetPowerStateController(endpoint);
                                    changedProperty = controller.GetPropertyState();
                                    changeReport.context.properties.Add(changedProperty);
                                }
                                break;
                            case "Alexa.BrightnessController":
                                {
                                    AlexaSetBrightnessController controller = new AlexaSetBrightnessController(endpoint);
                                    changedProperty = controller.GetPropertyState();
                                    changeReport.context.properties.Add(changedProperty);
                                }
                                break;
                            case "Alexa.ColorTemperatureController":
                                {
                                    AlexaAdjustColorTemperatureController controller = new AlexaAdjustColorTemperatureController(endpoint);
                                    changedProperty = controller.GetPropertyState();
                                    changeReport.context.properties.Add(changedProperty);
                                }
                                break;

                            default:
                                break;
                        }
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

        private void SendStateChangeReportsToAlexa()
        {
            // This queue blocks in the enumerator so this is essentially an infinate loop. 
            foreach (StateChangeReportWrapper item in stateReportQueue)
            {
                if (item.Sent == true)  // should never happen
                    continue;

                try
                {
                    item.Sent = true;
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
