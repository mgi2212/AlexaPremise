using Alexa;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System.Collections.Generic;
using Alexa.Controller;
using SYSWebSockClient;
using System;

namespace Alexa.Scene
{
    public class AlexaScene  : IAlexaDeviceType
    {

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
                    case "Alexa.SceneController":
                        {
                            AlexaSetSceneController controller = new AlexaSetSceneController("", endpoint);
                            property = controller.GetPropertyState();
                        }
                        break;

                    default:
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

                if (!capability.HasProperties()) // scenes are a special cased
                {
                    switch (capability.@interface)
                    {
                        case "Alexa.SceneController":
                            Type type = this.GetType();
                            subscription = endpoint.Subscribe("PowerState", this.GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                            break;
                        default:
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
    }
 }

