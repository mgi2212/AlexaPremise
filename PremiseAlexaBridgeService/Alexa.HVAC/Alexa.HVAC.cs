using Alexa.EndpointHealth;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Collections.Generic;
using SYSWebSockClient;

namespace Alexa.HVAC
{
    public class AlexaHVAC : IAlexaDeviceType
    {
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
                        case "Alexa.TemperatureSensor":
                            {
                                string index = discoveryEndpoint.endpointId + ".Temperature." + capability.@interface;
                                if ((subscription != null) && (!subscriptions.ContainsKey(index)))
                                {
                                    subscription = endpoint.Subscribe("Temperature", capability.@interface, callback).GetAwaiter().GetResult();
                                    subscriptions.Add(index, subscription);
                                }
                            }
                            break;
                        case "Alexa.ThermostatController":
                            {
                                Type type = this.GetType();

                                string index = discoveryEndpoint.endpointId + ".HeatingSetPoint." + capability.@interface;
                                if (!subscriptions.ContainsKey(index))
                                {
                                    subscription = endpoint.Subscribe("HeatingSetPoint", this.GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                                    if (subscription != null)
                                    {
                                        subscriptions.Add(index, subscription);
                                    }
                                }

                                index = discoveryEndpoint.endpointId + ".CoolingSetPoint." + capability.@interface;
                                if (!subscriptions.ContainsKey(index))
                                {
                                    subscription = endpoint.Subscribe("CoolingSetPoint", this.GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                                    if (subscription != null)
                                    {
                                        subscriptions.Add(index, subscription);
                                    }
                                }

                                index = discoveryEndpoint.endpointId + ".CurrentSetPoint." + capability.@interface;
                                if (!subscriptions.ContainsKey(index))
                                {
                                    subscription = endpoint.Subscribe("CurrentSetPoint", this.GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                                    if (subscription != null)
                                    {
                                        subscriptions.Add(index, subscription);
                                    }
                                }

                                index = discoveryEndpoint.endpointId + ".TemperatureMode." + capability.@interface;
                                if (!subscriptions.ContainsKey(index))
                                {
                                    subscription = endpoint.Subscribe("TemperatureMode", this.GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                                    if (subscription != null)
                                    {
                                        subscriptions.Add(index, subscription);
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                //if (subscription != null)
                //{
                //    subscriptions.Add(discoveryEndpoint.endpointId + "." + capability.@interface, subscription);
                //}
            }
            return subscriptions;
        }

    }
}