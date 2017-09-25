using Alexa.Controller;
using Alexa.Power;
using Alexa.SmartHome.V3;
using PremiseAlexaBridgeService;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Lighting
{
    #region SetBrightness

    [DataContract]
    public class AlexaSetBrightnessControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetBrightnessControllerDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetBrightnessControllerDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetBrightnessPayload payload { get; set; }

        public AlexaSetBrightnessControllerDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetBrightnessPayload();
        }
    }

    [DataContract]
    public class AlexaSetBrightnessPayload
    {
        [DataMember(Name = "brightness")]
        public int brightness { get; set; }
    }

    #endregion

    public class AlexaSetBrightnessController : AlexaControllerBase<AlexaSetBrightnessPayload, ControlResponse>, IAlexaController 
    {

        public readonly string @namespace = "Alexa.BrightnessController";
        public readonly string[] directiveNames = { "SetBrightness" };
        public readonly string  premiseProperty = "Brightness";
        public readonly string alexaProperty = "brightness";

        public AlexaSetBrightnessController(AlexaSetBrightnessControllerRequest request)
            : base(request.directive.header, request.directive.endpoint, request.directive.payload)
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
                value = (int)(brightness * 100),
                timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ")
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
            AlexaProperty property = new AlexaProperty(header)
            {
                name = "brightness"
            };
            try
            {
                double setValue = (double)(payload.brightness / 100.00).LimitToRange(0.00, 1.000);
                this.endpoint.SetValue(premiseProperty, setValue.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
                property.value = (int)(setValue * 100);
                this.response.context.properties.Add(property);
            }
            catch (Exception ex)
            {
                base.ClearResponseContextAndEventPayload();
                this.Response.Event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }

            this.Response.Event.header.name = "Response";

            // grab walk through related and supported controllers and report state
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

                            }
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
