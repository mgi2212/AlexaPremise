﻿using Alexa.Controller;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Lighting
{
    #region SetBrightness Data Contracts

    [DataContract]
    public class AlexaSetBrightnessControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetBrightnessControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetBrightnessControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetBrightnessRequestPayload payload { get; set; }

        public AlexaSetBrightnessControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetBrightnessRequestPayload();
        }
    }

    [DataContract]
    public class AlexaSetBrightnessRequestPayload
    {
        [DataMember(Name = "brightness")]
        public int brightness { get; set; }
    }

    #endregion

    public class AlexaSetBrightnessController : AlexaControllerBase<
        AlexaSetBrightnessRequestPayload, 
        ControlResponse, 
        AlexaSetBrightnessControllerRequest>, IAlexaController
    {
        public readonly string @namespace = "Alexa.BrightnessController";
        public readonly string[] directiveNames = { "SetBrightness" };
        public readonly string premiseProperty = "Brightness";
        public readonly string alexaProperty = "brightness";

        public AlexaSetBrightnessController(AlexaSetBrightnessControllerRequest request)
            : base(request)
        {
        }

        public AlexaSetBrightnessController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public AlexaProperty GetPropertyState()
        {
            double brightness = this.endpoint.GetValue<double>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperty,
                value = ((int)brightness * 100).LimitToRange(0,100),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
            AlexaProperty property = new AlexaProperty(header)
            {
                name = alexaProperty
            };
            try
            {
                double setValue = (double)(payload.brightness / 100.00).LimitToRange(0.00, 1.000);
                this.endpoint.SetValue(premiseProperty, setValue.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = ((int)setValue * 100).LimitToRange(0,100);
                this.response.context.properties.Add(property);
            }
            catch (Exception ex)
            {
                base.ReportError(AlexaErrorTypes.public_ERROR, ex.Message);
            }

            this.Response.Event.header.name = "Response";

            // walk through related and supported controllers and report state
            DiscoveryEndpoint discoveryEndpoint = PremiseServer.GetDiscoveryEndpoint(this.endpoint).GetAwaiter().GetResult();
            if (discoveryEndpoint != null)
            {
                foreach (Capability capability in discoveryEndpoint.capabilities)
                {
                    switch (capability.@interface)
                    {
                        case "Alexa.BrightnessController": // already added
                            break;

                        case "Alexa.PowerController":
                            {
                                AlexaSetPowerStateController controller = new AlexaSetPowerStateController(this.endpoint);
                                AlexaProperty powerState = controller.GetPropertyState();
                                response.context.properties.Add(powerState);
                            }
                            break;

                        case "Alexa.ColorController":
                            {

                            }
                            // TODO
                            break;

                        case "Alexa.ColorTemperatureController":
                            {
                                AlexaSetColorTemperatureController controller = new AlexaSetColorTemperatureController(this.endpoint);
                                AlexaProperty powerState = controller.GetPropertyState();
                                response.context.properties.Add(powerState);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }
    }
}
