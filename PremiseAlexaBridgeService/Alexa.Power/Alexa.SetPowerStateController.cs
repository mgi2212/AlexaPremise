using Alexa.Controller;
using Alexa.Lighting;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Power
{
    #region PowerState Data Contracts

    [DataContract]
    public class AlexaSetPowerStateControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetPowerStateControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetPowerStateControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetPowerStateRequestPayload payload { get; set; }

        public AlexaSetPowerStateControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetPowerStateRequestPayload();
        }
    }

    public class AlexaSetPowerStateRequestPayload : object
    {

    }

    #endregion

    public class AlexaSetPowerStateController : AlexaControllerBase<
        AlexaSetPowerStateRequestPayload, 
        ControlResponse, 
        AlexaSetPowerStateControllerRequest>, IAlexaController
    {
        public readonly string @namespace = "Alexa.PowerController";
        public readonly string[] directiveNames = { "TurnOn", "TurnOff" };
        public readonly string premiseProperty = "PowerState";
        public readonly string alexaProperty = "powerState";

        public AlexaSetPowerStateController(AlexaSetPowerStateControllerRequest request)
            : base(request)
        {
        }

        public AlexaSetPowerStateController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public AlexaProperty GetPropertyState()
        {
            bool powerState = endpoint.GetValue<bool>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperty,
                value = (powerState == true ? "ON" : "OFF"),
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
                string valueToSend;
                if (header.name == "TurnOff")
                {
                    valueToSend = "False";
                    property.value = "OFF";
                }
                else if (header.name == "TurnOn")
                {
                    valueToSend = "True";
                    property.value = "ON";
                }
                else
                {
                    base.ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                    return;
                }

                this.endpoint.SetValue(premiseProperty, valueToSend).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                this.response.context.properties.Add(property);
            }
            catch (Exception ex)
            {
                base.ReportError(AlexaErrorTypes.public_ERROR, ex.Message);
                return;
            }

            this.Response.Event.header.name = "Response";

            // walk through remaining supported controllers and report state
            DiscoveryEndpoint discoveryEndpoint = PremiseServer.GetDiscoveryEndpoint(endpoint).GetAwaiter().GetResult();
            if (discoveryEndpoint != null)
            {
                foreach (Capability capability in discoveryEndpoint.capabilities)
                {
                    switch (capability.@interface)
                    {
                        case "Alexa.PowerController": // already added
                            break;

                        case "Alexa.BrightnessController":
                            {
                                AlexaSetBrightnessController controller = new AlexaSetBrightnessController(endpoint);
                                AlexaProperty brightness = controller.GetPropertyState();
                                response.context.properties.Add(brightness);
                            }
                            break;

                        case "Alexa.ColorController":
                            {
                                // TODO
                            }
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