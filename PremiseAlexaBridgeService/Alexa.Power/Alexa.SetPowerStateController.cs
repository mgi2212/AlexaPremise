using Alexa.Controller;
using Alexa.Lighting;
using Alexa.SmartHome.V3;
using PremiseAlexaBridgeService;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Power
{

    [DataContract]
    public class AlexaSetPowerStateControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetPowerStateControllerDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetPowerStateControllerDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetPowerStatePayload payload { get; set; }

        public AlexaSetPowerStateControllerDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetPowerStatePayload();
        }
    }

    public class AlexaSetPowerStatePayload : object
    {

    }

    public class AlexaSetPowerStateController : AlexaControllerBase<AlexaSetPowerStatePayload, ControlResponse>, IAlexaController
        {

        public readonly string @namespace = "Alexa.PowerController";
        public readonly string[] directiveNames = { "TurnOn", "TurnOff" };
        public readonly string premiseProperty = "PowerState";
        public readonly string alexaProperty = "powerState";

        public AlexaSetPowerStateController(AlexaSetPowerStateControllerRequest request)
            : base(request.directive.header, request.directive.endpoint, request.directive.payload)
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
                timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ")
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
                if (header.name == "TurnOff")
                {
                    endpoint.SetValue(premiseProperty, "False").GetAwaiter().GetResult();
                    property.timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
                    property.value = "OFF";
                    response.context.properties.Add(property);
                }
                else if (header.name == "TurnOn")
                {
                    endpoint.SetValue(premiseProperty, "True").GetAwaiter().GetResult();
                    property.timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
                    property.value = "ON";
                    response.context.properties.Add(property);
                }
            }
            catch(Exception ex)
            {
                base.ClearResponseContextAndEventPayload();
                this.Response.Event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return;
            }

            this.Response.Event.header.name = "Response";

            // grab walk through remaining supported controllers and report state
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
                            AlexaSetBrightnessController controller = new AlexaSetBrightnessController(endpoint);
                            AlexaProperty brightness = controller.GetPropertyState();
                            response.context.properties.Add(brightness);
                            break;

                        case "Alexa.ColorController":
                            // TODO
                            break;

                        case "Alexa.ColorTemperatureController":
                            // TODO
                            break;

                        default:
                            break;
                    }
                }
            }
        }
    }
}