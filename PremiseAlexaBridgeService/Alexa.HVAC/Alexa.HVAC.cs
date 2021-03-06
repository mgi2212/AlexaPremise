﻿using System;
using System.Collections.Generic;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;
using Alexa.Controller;
using System.Runtime.Serialization;

namespace Alexa.HVAC
{
    public class AlexaHVAC : IAlexaDeviceType
    {
        #region Methods

        public List<AlexaProperty> FindRelatedProperties(IPremiseObject endpoint, string currentController)
        {
            List<AlexaProperty> relatedProperties = new List<AlexaProperty>();
            // walk through related and supported controllers and report state
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
                    case "Alexa.TemperatureSensor":
                        controller = new AlexaTemperatureSensor();
                        break;

                    case "Alexa.ThermostatController":
                        controller = new AlexaThermostatController();
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

                    IPremiseSubscription subscription = endpoint.SubscribeAsync(premiseProperty, GetType().AssemblyQualifiedName, callback).GetAwaiter().GetResult();
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

    [DataContract]
    public class AlexaTemperature
    {
        #region Fields

        [DataMember(Name = "scale")]
        public string scale;

        [DataMember(Name = "value")]
        public double value;

        #endregion Fields

        #region Constructors

        public AlexaTemperature(double temperature, string scaleString)
        {
            value = temperature;
            scale = scaleString;
        }

        #endregion Constructors
    }
}