using Alexa.Lighting;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Collections.Generic;
using SYSWebSockClient;

namespace Alexa
{
    public class AlexaLighting : IAlexaDeviceType
    {
        #region Related Properties
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
                    case "Alexa.PowerController":
                        {
                            AlexaSetPowerStateController controller = new AlexaSetPowerStateController(endpoint);
                            property = controller.GetPropertyState();
                        }
                        break;
                    case "Alexa.BrightnessController":
                        {
                            AlexaSetBrightnessController controller = new AlexaSetBrightnessController(endpoint);
                            property = controller.GetPropertyState();
                        }
                        break;

                    case "Alexa.ColorController":
                        {

                        }
                        // TODO
                        break;

                    case "Alexa.ColorTemperatureController":
                        {
                            AlexaSetColorTemperatureController controller = new AlexaSetColorTemperatureController(endpoint);
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

        #endregion

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
                        case "Alexa.BrightnessController":
                            subscription = endpoint.Subscribe("Brightness", capability.@interface, callback).GetAwaiter().GetResult();
                            break;
                        case "Alexa.ColorTemperatureController":
                            subscription = endpoint.Subscribe("Temperature", capability.@interface, callback).GetAwaiter().GetResult();
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