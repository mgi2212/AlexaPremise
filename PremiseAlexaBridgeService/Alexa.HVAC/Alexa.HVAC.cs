using Alexa;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Collections.Generic;
using SYSWebSockClient;
using Alexa.Controller;
using Alexa.EndpointHealth;

namespace Alexa.HVAC
{
    public class AlexaHVAC : IAlexaDeviceType
    {

        //Dictionary<string, IAlexaController> Controllers = new Dictionary<string, IAlexaController>();

        public List<AlexaProperty> FindRelatedProperties(IPremiseObject endpoint, string currentController)
        {
            List<AlexaProperty> relatedProperties = new List<AlexaProperty>();
            // walk through related and supported controllers and report state
            DiscoveryEndpoint discoveryEndpoint = PremiseServer.GetDiscoveryEndpoint(endpoint).GetAwaiter().GetResult();
            if (discoveryEndpoint == null)
            {
                return relatedProperties;
            }
            foreach (Capability capability in discoveryEndpoint.capabilities)
            {
                //if (capability.@interface == currentController)
                //    continue;

                AlexaProperty property = null;

                switch (capability.@interface)
                {
                    case "Alexa.EndpointHealth":
                        {
                            AlexaEndpointHealthController controller = new AlexaEndpointHealthController(endpoint);
                            property = controller.GetPropertyState();
                        }
                        break;

                    case "Alexa.TemperatureSensor":
                        {
                            AlexaTemperatureSensor controller = new AlexaTemperatureSensor(endpoint);
                            property = controller.GetPropertyState();
                        }
                        break;
                    case "Alexa.ThermostatController":
                        {
                            AlexaSetThermostatModeController mode = new AlexaSetThermostatModeController(endpoint);
                            property = mode.GetPropertyState();
                            if (property != null)
                            {
                                relatedProperties.Add(property);
                            }
                            property = null;

                            SetTargetTemperatureController temperature = new SetTargetTemperatureController(endpoint);
                            relatedProperties.AddRange(temperature.GetPropertyStates());
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

                if (capability.HasProperties() == false)
                {
                    continue;
                }

                if (capability.properties.proactivelyReported)
                {
                    switch (capability.@interface)
                    {
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