using Alexa.SmartHome.V3;
using System;
using System.Configuration;
using SYSWebSockClient;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Threading;

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

            _sysClient = new SYSClient();
            _asyncEventSubscription = null;
            CheckStatus();
        }

        ~PremiseServer()
        {
            if (_homeObject != null)
            {
                _homeObject = null;
                _rootObject = null;
                _sysClient.Disconnect();
            }
        }

        private static IPremiseObject _homeObject;
        private static IPremiseObject _rootObject;
        private static SYSClient _sysClient;
        private static IPremiseSubscription _asyncEventSubscription;
        private static List<IPremiseSubscription> subscriptions = new List<IPremiseSubscription>();
        private static List<DiscoveryEndpoint> endpoints = new List<DiscoveryEndpoint>();
        private static Throttler throttler = new Throttler(1, new TimeSpan(0, 0, 0, 0, 100));

        public static void CheckStatus()
        {
            try
            {

                if ((_homeObject == null) && (!isClientConnected()))
                {
                    _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                    _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                }
                else if ((_homeObject != null) && (!isClientConnected()))
                {
                    _homeObject = null;
                    _rootObject = null;
                    _asyncEventSubscription.Unsubscribe().GetAwaiter().GetResult();
                    _asyncEventSubscription = null;
                    _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                    _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                }
                if (_asyncEventSubscription == null)
                {
                    _asyncEventSubscription = _homeObject.Subscribe("SendAsyncEventsToAlexa", new Action<dynamic>(EnableAsyncPropertyChanged)).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _homeObject = null;
                _rootObject = null;
                Debug.WriteLine(ex.Message);
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
            Task t = Task.Run(() =>
            {
                if ((sub.sysObjectId == HomeObjectId) && (sub.propertyName == "SendAsyncEventsToAlexa"))
                {
                    bool enableAsyncEvents = false;
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
                        if (endpoints.Count == 0)
                        {
                            GetEndpoints().GetAwaiter().GetResult();
                        }
                        foreach (DiscoveryEndpoint endpoint in endpoints)
                        {
                            foreach (Capability capability in endpoint.capabilities)
                            {
                                if (capability.properties.proactivelyReported)
                                {
                                    Guid premiseId = new Guid(endpoint.endpointId);
                                    IPremiseObject sysObject = _rootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                                    IPremiseSubscription subscription = null;

                                    switch (capability.@interface)
                                    {
                                        case "Alexa.BrightnessController":
                                            subscription = sysObject.Subscribe("Brightness", new Action<dynamic>(AlexaPropertyChanged)).GetAwaiter().GetResult();
                                            break;
                                        case "Alexa.PowerController":
                                            subscription = sysObject.Subscribe("PowerState", new Action<dynamic>(AlexaPropertyChanged)).GetAwaiter().GetResult();
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
                    else
                    {
                        if (subscriptions != null)
                        {
                            foreach (IPremiseSubscription subscription in subscriptions)
                            {
                                subscription.Unsubscribe().GetAwaiter().GetResult();
                            }
                            subscriptions.Clear();
                        }
                    }
                }
            });

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

                AlexaChangeReport changeReport = new AlexaChangeReport();
                changeReport.@event.header.messageID = Guid.NewGuid().ToString("D");
                changeReport.@event.header.@namespace = "Alexa";
                changeReport.@event.header.name = "ChangeReport";
                changeReport.@event.header.payloadVersion = "3";
                changeReport.@event.endpoint.scope.type = "BearerToken";
                changeReport.@event.endpoint.scope.token = "Alexa-access-token"; //todo get real value
                changeReport.@event.endpoint.endpointId = sub.sysObjectId;
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
                string json = JsonConvert.SerializeObject(changeReport);
                //throttler.Enqueue();
                Debug.WriteLine(json);
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
            return (_sysClient.ConnectionState == System.Net.WebSockets.WebSocketState.Open);
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
            endpoints.Clear();

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

    public class Throttler : IDisposable
    {
        private readonly TimeSpan _maxPeriod;
        private readonly SemaphoreSlim _throttleActions, _throttlePeriods;

        public Throttler(int maxActions, TimeSpan maxPeriod)
        {
            _throttleActions = new SemaphoreSlim(maxActions, maxActions);
            _throttlePeriods = new SemaphoreSlim(maxActions, maxActions);
            _maxPeriod = maxPeriod;
        }

        public Task<T> Enqueue<T>(Func<T> action, System.Threading.CancellationToken cancel)
        {
            return _throttleActions.WaitAsync(cancel).ContinueWith<T>(t =>
            {
                try
                {
                    _throttlePeriods.Wait(cancel);

                    // Release after period
                    // - Allow bursts up to maxActions requests at once
                    // - Do not allow more than maxActions requests per period
                    Task.Delay(_maxPeriod).ContinueWith((tt) =>
                    {
                        _throttlePeriods.Release(1);
                    });

                    return action();
                }
                finally
                {
                    _throttleActions.Release(1);
                }
            });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _throttleActions.Dispose();
                    _throttlePeriods.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Throttler() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
