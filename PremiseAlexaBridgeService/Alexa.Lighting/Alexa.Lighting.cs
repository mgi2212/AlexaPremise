using System;
using System.Collections.Generic;
using Alexa.EndpointHealth;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;
using Alexa.Controller;

namespace Alexa.Lighting
{
    public class AlexaLighting : IAlexaDeviceType
    {
        #region Related Properties

        /// <summary>
        /// Add all capabilites here exclusively related to this device type. Yes, this differs from
        /// the method below by design.
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
                            AlexaBrightnessController controller = new AlexaBrightnessController(endpoint);
                            property = controller.GetPropertyState();
                        }
                        break;

                    case "Alexa.ColorController":
                        {
                            AlexaColorController controller = new AlexaColorController(endpoint);
                            property = controller.GetPropertyState();
                        }
                        break;

                    case "Alexa.ColorTemperatureController":
                        {
                            AlexaColorTemperatureController controller = new AlexaColorTemperatureController(endpoint);
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

        #endregion Related Properties

        #region Subscribe To Supported Properties

        /// <summary>
        /// Only add subscription supprt for the type of class. For example adding PowerState here in
        /// this function is not correct results in a stack overflow issue
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
                if (capability.HasProperties() == false)
                {
                    continue;
                }

                if (!capability.properties.proactivelyReported)
                {
                    continue;
                }

                IAlexaController controller = null;

                switch (capability.@interface)
                {
                    case "Alexa.BrightnessController":
                        controller = new AlexaBrightnessController();
                        break;

                    case "Alexa.ColorController":
                        controller = new AlexaColorController();
                        break;

                    case "Alexa.ColorTemperatureController":
                        controller = new AlexaColorTemperatureController();
                        break;
                }
                if (controller == null)
                {
                    continue;
                }

                foreach (string premiseProperty in controller.GetPremiseProperties())
                {
                    string index = discoveryEndpoint.endpointId + $".{premiseProperty}." + capability.@interface;
                    if (subscriptions.ContainsKey(index)) continue;

                    IPremiseSubscription subscription = endpoint.Subscribe(premiseProperty, GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
                    if (subscription != null)
                    {
                        subscriptions.Add(index, subscription);
                    }
                }
            }
            return subscriptions;
        }

        #endregion Subscribe To Supported Properties
    }
}