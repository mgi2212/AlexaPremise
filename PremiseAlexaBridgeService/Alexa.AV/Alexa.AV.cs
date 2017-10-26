using System;
using System.Collections.Generic;
using Alexa.Controller;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.AV
{
    public class AlexaAV : IAlexaDeviceType
    {
        #region Methods

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
                if (capability.@interface == currentController)
                    continue;

                AlexaProperty property;

                switch (capability.@interface)
                {
                    case "Alexa.PowerController":
                        {
                            AlexaSetPowerStateController controller = new AlexaSetPowerStateController(endpoint);
                            property = controller.GetPropertyState();
                            relatedProperties.Add(property);
                        }
                        break;

                    case "Alexa.ChannelController":
                        {
                            AlexaChannelController controller = new AlexaChannelController(endpoint);
                            property = controller.GetPropertyState();
                            relatedProperties.Add(property);
                        }
                        break;

                    case "Alexa.InputController":
                        {
                            AlexaInputController controller = new AlexaInputController(endpoint);
                            relatedProperties.AddUnique(controller.GetPropertyStates());
                        }
                        break;

                    case "Alexa.PlaybackController":
                        {
                            AlexaPlaybackController controller = new AlexaPlaybackController(endpoint);
                            property = controller.GetPropertyState();
                            relatedProperties.Add(property);
                        }
                        break;

                    case "Alexa.Speaker":
                        {
                            AlexaSpeaker controller = new AlexaSpeaker(endpoint);
                            relatedProperties.AddUnique(controller.GetPropertyStates());
                        }
                        break;
                }
            }
            return relatedProperties;
        }

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
                    case "Alexa.ChannelController":
                        controller = new AlexaChannelController();
                        break;

                    case "Alexa.InputController":
                        controller = new AlexaInputController();
                        break;

                    case "Alexa.PlaybackController":
                        controller = new AlexaPlaybackController();
                        break;

                    case "Alexa.Speaker":
                        controller = new AlexaSpeaker();
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

        #endregion Methods
    }

    internal class Channel
    {
        #region Properties

        private string affiliateCallSign { get; set; }
        private string callSign { get; set; }
        private float number { get; set; }

        #endregion Properties
    }
}