using Alexa.EndpointHealth;
using Alexa.Lighting;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Collections.Generic;
using SYSWebSockClient;
using Alexa.Controller;
namespace Alexa
{
    public class AlexaLighting : IAlexaDeviceType
    {

        //Dictionary<string, IAlexaController> Controllers = new Dictionary<string, IAlexaController>();

        #region Related Properties
        /// <summary>
        /// Add all capabilites here exclusively related to this device type. Yes, this differs from the method below by design.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="currentController"></param>
        /// <returns></returns>
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
                            AlexaSetColorController controller = new AlexaSetColorController(endpoint);
                            property = controller.GetPropertyState();
                        }
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

        #region Subscribe To Supported Properties
        /// <summary>
        /// Only add subscription supprt for the type of class. For example adding PowerState here in this function is not correct results in a stack overflow issue
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="discoveryEndpoint"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
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
                            {
                                subscription = endpoint.Subscribe("Brightness", capability.@interface, callback).GetAwaiter().GetResult();
                                if (subscription != null)
                                {
                                    subscriptions.Add(discoveryEndpoint.endpointId + "." + capability.@interface, subscription);
                                }
                            }
                            break;
                        case "Alexa.ColorController":
                            Type type = this.GetType();
                            subscription = endpoint.Subscribe("Hue", this.GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                            if (subscription != null)
                            {
                                subscriptions.Add(discoveryEndpoint.endpointId + ".Hue." + capability.@interface, subscription);
                            }
                            subscription = endpoint.Subscribe("Saturation", this.GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                            if (subscription != null)
                            {
                                subscriptions.Add(discoveryEndpoint.endpointId + ".Saturation." + capability.@interface, subscription);
                            }
                            break;
                        case "Alexa.ColorTemperatureController":
                            {
                                subscription = endpoint.Subscribe("Temperature", capability.@interface, callback).GetAwaiter().GetResult();
                                if (subscription != null)
                                {
                                    subscriptions.Add(discoveryEndpoint.endpointId + "." + capability.@interface, subscription);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return subscriptions;
        }

        #endregion
    }
}