using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa;
using Alexa.Controller;
using Alexa.Scene;
using Alexa.SmartHomeAPI.V3;
using Nito.AsyncEx;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    // ReSharper disable once ClassCannotBeInstantiated
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
                // Premise can send multiple notifications for a single object, one for each
                // subscribed property that changes. The state report function here will capture
                // states of all properties, so the DeDupeDictionary prevents multiple state reports
                // from being sent for essentially the same event.
                if (DeDupeDictionary.ContainsKey(sub.sysObjectId))
                    return;

                DeDupeDictionary.Add(sub.sysObjectId, sub);
            }

            Task.Run(() =>
            {
                // get the endpoint and endpoint capabilities
                Guid premiseId = new Guid(sub.sysObjectId);
                IPremiseObject endpoint = RootObject.GetObjectAsync(premiseId.ToString("B")).GetAwaiter().GetResult();
                DiscoveryEndpoint discoveryEndpoint = GetDiscoveryEndpointAsync(endpoint).GetAwaiter().GetResult();
                if (discoveryEndpoint == null)
                {
                    return;
                }

                // get the authorization code for the notification
                string authCode;
                using (asyncObjectsLock.Lock())
                {
                    authCode = (string)HomeObject.GetValueAsync("AlexaAsyncAuthorizationCode").GetAwaiter().GetResult();
                }

                // build the change report
                AlexaChangeReport changeReport = new AlexaChangeReport();
                changeReport.@event.header.messageID = Guid.NewGuid().ToString("D");
                changeReport.@event.header.@namespace = "Alexa";
                changeReport.@event.header.payloadVersion = "3";
                changeReport.@event.endpoint.scope.type = "BearerToken";
                changeReport.@event.endpoint.scope.token = authCode;
                changeReport.@event.endpoint.endpointId = premiseId.ToString("D").ToUpper();
                changeReport.@event.endpoint.cookie = discoveryEndpoint.cookie;
                changeReport.@event.payload.change.cause.type = "PHYSICAL_INTERACTION";

                // get the device type and controller (e.g. AlexaAV, AlexaHVAC)
                IAlexaDeviceType deviceType = null;
                IAlexaController controller = null;
                List<AlexaProperty> relatedPropertyStates = null;

                bool hasScene = false;

                foreach (IAlexaController controllerToTest in Controllers.Values)
                {
                    if (!controllerToTest.HasPremiseProperty(sub.propertyName))
                    {
                        continue;
                    }

                    controller = controllerToTest;
                    Type type = Type.GetType(controller.GetAssemblyTypeName());
                    if (type == null)
                    {
                        continue;
                    }

                    // found a controller, get an instance of the assembly
                    deviceType = (IAlexaDeviceType)Activator.CreateInstance(type);

                    // Determine if this deviceType supports the desired capability
                    // note: This handles situation where the same property name is used by different
                    // controllers. e.g. "brightness" is used in both ColorController and BrightnessController
                    relatedPropertyStates = deviceType.FindRelatedProperties(endpoint, "");
                    foreach (AlexaProperty property in relatedPropertyStates)
                    {
                        // if so, this is the correct type
                        if (property.@namespace == controller.GetNameSpace())
                        {
                            break;
                        }
                    }
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if ((deviceType == null) || (controller == null || relatedPropertyStates == null))
                {
                    return;
                }

                foreach (AlexaProperty prop in relatedPropertyStates)
                {
                    if (prop.@namespace == "Alexa.SceneController")
                    {
                        hasScene = true;
                        continue;
                    }

                    if ((changeReport.@event.payload.change.properties.Count == 0) && (prop.name == controller.MapPremisePropertyToAlexaProperty(sub.propertyName)))
                    {
                        changeReport.@event.payload.change.properties.Add(prop);
                    }
                    else
                    {
                        string propKey = prop.@namespace + "." + prop.name;
                        changeReport.context.propertiesInternal.Add(propKey, prop);
                    }
                }

                changeReport.@event.header.name = "ChangeReport";

                // scenes are special case
                if (hasScene)
                {
                    AlexaSetSceneController sceneController = new AlexaSetSceneController(endpoint);
                    changeReport = sceneController.AlterChangeReport(changeReport);
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
                UnsubscribeAllAsync().GetAwaiter().GetResult();
                SubscribeAllAsync().GetAwaiter().GetResult();
            }
        }

        public static async Task UnsubscribeAllAsync()
        {
            using (subscriptionsLock.Lock())
            {
                if (subscriptions != null)
                {
                    foreach (KeyValuePair<string, IPremiseSubscription> subscription in subscriptions)
                    {
                        await subscription.Value.UnsubscribeAsync().ConfigureAwait(false);
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
                        await SubscribeAllAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await UnsubscribeAllAsync().ConfigureAwait(false);
                    }
                }
            });
        }

        private static async Task SubscribeAllAsync()
        {
            using (subscriptionsLock.Lock())
            {
                if (endpoints.Count == 0)
                {
                    await GetEndpointsAsync().ConfigureAwait(false);
                }
                if (subscriptions.Count != 0)
                {
                    await UnsubscribeAllAsync().ConfigureAwait(false);
                }
                Action<dynamic> callback = AlexaPropertyChanged;
                using (endpointsLock.Lock())
                {
                    foreach (DiscoveryEndpoint discoveryEndpoint in endpoints)
                    {
                        Guid premiseId = new Guid(discoveryEndpoint.endpointId);
                        IPremiseObject endpoint = await RootObject.GetObjectAsync(premiseId.ToString("B")).ConfigureAwait(false);

                        // use reflection to instantiate all device type controllers
                        var interfaceType = typeof(IAlexaDeviceType);
                        var all = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(x => x.GetTypes())
                            .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                            .Select(Activator.CreateInstance);

                        foreach (IAlexaDeviceType deviceType in all)
                        {
                            Dictionary<string, IPremiseSubscription> subs =
                                deviceType.SubscribeToSupportedProperties(endpoint, discoveryEndpoint, callback);
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
                        .SubscribeAsync("SendAsyncEventsToAlexa", "NoController", EnableAsyncPropertyChanged).GetAwaiter().GetResult();
                }
            }
        }

        #endregion Methods
    }
}