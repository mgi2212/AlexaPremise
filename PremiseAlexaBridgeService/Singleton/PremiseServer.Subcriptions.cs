using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa;
using Alexa.Scene;
using Alexa.SmartHomeAPI.V3;
using Nito.AsyncEx;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    public sealed partial class PremiseServer
    {
        #region Fields

        public static IPremiseSubscription _asyncEventSubscription;
        public static bool enableAsyncEvents;
        private static readonly AsyncLock homeObjectSubscriptionLock = new AsyncLock();
        private static readonly Dictionary<string, IPremiseSubscription> subscriptions = new Dictionary<string, IPremiseSubscription>();
        private static readonly AsyncLock subscriptionsLock = new AsyncLock();

        #endregion Fields

        #region Properties

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

        #endregion Properties

        #region Methods

        public static void AlexaPropertyChanged(dynamic @params)
        {
            Subscription sub = (Subscription)@params;
            using (deDupeLock.Lock())
            {
                if (DeDupeDictionary.ContainsKey(sub.sysObjectId))
                    return;
                DeDupeDictionary.Add(sub.sysObjectId, sub);
            }

            Task.Run(() =>
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
                string authCode;
                using (asyncObjectsLock.Lock())
                {
                    authCode = (string)HomeObject.GetValue("AlexaAsyncAuthorizationCode").GetAwaiter().GetResult();
                }
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
                    .Select(Activator.CreateInstance);

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
                        // filter for the property that triggered the actual change, unless it is the
                        // beloved SceneController.
                        if ((controller.HasPremiseProperty(sub.propertyName)) &&
                            (changeReport.@event.payload.change.properties.Count == 0))
                        {
                            causeKey = propKey;
                            changeReport.@event.payload.change.properties.Add(prop);
                            if (changeReport.context.propertiesInternal.ContainsKey(causeKey))
                            {
                                changeReport.context.propertiesInternal.Remove(causeKey);
                            }
                        }
                        else if ((!changeReport.context.propertiesInternal.ContainsKey(propKey)) &&
                                 (propKey != causeKey))
                        {
                            changeReport.context.propertiesInternal.Add(propKey, prop);
                        }
                    }
                }

                changeReport.@event.header.name = "ChangeReport";

                foreach (Capability capability in discoveryEndpoint.capabilities)
                {
                    switch (capability.@interface) // scenes are special cased
                    {
                        case "Alexa.SceneController":
                            {
                                AlexaSetSceneController sceneController = new AlexaSetSceneController(endpoint);
                                changeReport = sceneController.AlterChangeReport(changeReport);
                            }
                            break;
                    }
                }

                StateChangeReportWrapper item = new StateChangeReportWrapper
                {
                    ChangeReport = changeReport
                };

                stateReportQueue.Enqueue(item);
                using (deDupeLock.Lock())
                {
                    DeDupeDictionary.Remove(sub.sysObjectId);
                }
            });
        }

        public static void Resubscribe()
        {
            if (IsAsyncEventsEnabled)
            {
                UnsubscribeAll().GetAwaiter().GetResult();
                SubscribeAll().GetAwaiter().GetResult();
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

        private static void EnableAsyncPropertyChanged(dynamic @params)
        {
            Subscription sub = (Subscription)@params;

            // can't block the event reporting thread.
            Task.Run(async () =>
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
                Action<dynamic> callback = AlexaPropertyChanged;
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
                            .Select(Activator.CreateInstance);

                        foreach (IAlexaDeviceType deviceType in all)
                        {
                            Dictionary<string, IPremiseSubscription> subs =
                                deviceType.SubcribeToSupportedProperties(endpoint, discoveryEndpoint, callback);
                            foreach (string key in subs.Keys)
                            {
                                subscriptions.Add(key, subs[key]);
                            }
                        }
                    }
                }
            }
        }

        private static void SubScribeToHomeObjectEvents()
        {
            using (homeObjectSubscriptionLock.Lock())
            {
                if (_asyncEventSubscription == null)
                {
                    _asyncEventSubscription = HomeObject
                        .Subscribe("SendAsyncEventsToAlexa", "NoController", EnableAsyncPropertyChanged).GetAwaiter()
                        .GetResult();
                }
            }
        }

        #endregion Methods
    }
}