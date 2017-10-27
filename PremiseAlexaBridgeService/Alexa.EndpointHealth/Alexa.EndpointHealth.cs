using System;
using System.Collections.Generic;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.EndpointHealth
{
    public class AlexaEndpointHealth : IAlexaDeviceType
    {
        #region Methods

        public List<AlexaProperty> FindRelatedProperties(IPremiseObject endpoint, string currentController)
        {
            List<AlexaProperty> relatedProperties = new List<AlexaProperty>();

            DiscoveryEndpoint discoveryEndpoint = PremiseServer.GetDiscoveryEndpoint(endpoint).GetAwaiter().GetResult();
            if (discoveryEndpoint == null)
            {
                return relatedProperties;
            }
            foreach (Capability capability in discoveryEndpoint.capabilities)
            {
                if (capability.@interface == currentController)
                    continue;

                AlexaProperty property = null;

                switch (capability.@interface)
                {
                    case "Alexa.EndpointHealth":
                        {
                            AlexaEndpointHealthController controller = new AlexaEndpointHealthController(endpoint);
                            property = controller.GetPropertyState();
                        }
                        break;
                }
                if (property != null)
                {
                    relatedProperties.Add(property);
                }
            }
            return relatedProperties;
        }

        public Dictionary<string, IPremiseSubscription> SubcribeToSupportedProperties(IPremiseObject endpoint, DiscoveryEndpoint discoveryEndpoint, Action<dynamic> callback)
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
                        case "Alexa.EndpointHealth":
                            subscription = endpoint.Subscribe("IsReachable", GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
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