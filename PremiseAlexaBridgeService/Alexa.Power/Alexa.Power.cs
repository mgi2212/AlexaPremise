using System;
using System.Collections.Generic;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.Power
{
    public class AlexaPower : IAlexaDeviceType
    {
        #region Methods

        public List<AlexaProperty> FindRelatedProperties(IPremiseObject endpoint, string currentController)
        {
            List<AlexaProperty> relatedProperties = new List<AlexaProperty>();

            DiscoveryEndpoint discoveryEndpoint = PremiseServer.GetDiscoveryEndpointAsync(endpoint).GetAwaiter().GetResult();
            if (discoveryEndpoint == null)
            {
                return relatedProperties;
            }

            foreach (Capability capability in discoveryEndpoint.capabilities)
            {
                if (capability.@interface == currentController)
                    continue;

                if (PremiseServer.Controllers.ContainsKey(capability.@interface))
                {
                    IAlexaController controller = PremiseServer.Controllers[capability.@interface];
                    controller.SetEndpoint(endpoint);
                    relatedProperties.AddRange(controller.GetPropertyStates());
                }
            }

            return relatedProperties;
        }

        public Dictionary<string, IPremiseSubscription> SubscribeToSupportedProperties(IPremiseObject endpoint, DiscoveryEndpoint discoveryEndpoint, Action<dynamic> callback)
        {
            Dictionary<string, IPremiseSubscription> subscriptions = new Dictionary<string, IPremiseSubscription>();
            foreach (Capability capability in discoveryEndpoint.capabilities)
            {
                IPremiseSubscription subscription = null;

                if (capability.HasProperties() == false)
                {
                    continue;
                }

                if (capability.properties.proactivelyReported)
                {
                    switch (capability.@interface)
                    {
                        case "Alexa.PowerController":
                            subscription = endpoint.Subscribe("PowerState", GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                            break;
                    }
                }
                if (subscription != null)
                {
                    subscriptions.Add(discoveryEndpoint.endpointId + "." + capability.@interface, subscription);
                }
            }
            return subscriptions;
        }

        #endregion Methods
    }
}