using Alexa.Controller;
using Alexa.Power;
using Alexa.SmartHome.V3;
using PremiseAlexaBridgeService;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Lighting
{
    #region Adjust Brightness Data Contracts

    public class AlexaAdjustBrightnessControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaAjustBrightnessControllerDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaAjustBrightnessControllerDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaAdjustBrightnessPayload payload { get; set; }

        public AlexaAjustBrightnessControllerDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaAdjustBrightnessPayload();
        }
    }
     
    [DataContract]
    public class AlexaAdjustBrightnessPayload
    {
        [DataMember(Name = "brightnessDelta")]
        public int brightnessDelta { get; set; }
    }

    #endregion 

    public class AlexaAdjustBrightnessController : AlexaControllerBase<AlexaAdjustBrightnessPayload, ControlResponse>, IAlexaController 
    {
        public readonly string @namespace = "Alexa.BrightnessController";
        public readonly string[] directiveNames = { "AdjustBrightness" };
        public readonly string  premiseProperty = "Brightness";
        public readonly string alexaProperty = "brightness";

        public AlexaAdjustBrightnessController(AlexaAdjustBrightnessControllerRequest request)
            : base(request.directive.header, request.directive.endpoint, request.directive.payload)
        {
        }

        public AlexaAdjustBrightnessController(IPremiseObject endpoint)
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
                name = alexaProperty
            };

            try
            {
                double adjustValue = Math.Round(((double)payload.brightnessDelta / 100.00), 2).LimitToRange(-1.00, 1.00);
                double currentValue = Math.Round(endpoint.GetValue<Double>(premiseProperty).GetAwaiter().GetResult(), 2);
                double valueToSend = Math.Round(currentValue + adjustValue, 2).LimitToRange(0.00, 1.00);
                endpoint.SetValue(premiseProperty, valueToSend.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
                property.value = (int)(valueToSend * 100);
                response.context.properties.Add(property);
            }
            catch (Exception ex)
            {
                base.ClearResponseContextAndEventPayload();
                this.Response.Event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }

            this.Response.Event.header.name = "Response";

            // walk through related and supported controllers and report state
            DiscoveryEndpoint discoveryEndpoint = PremiseServer.GetDiscoveryEndpoint(endpoint).GetAwaiter().GetResult();
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
                                AlexaSetPowerStateController controller = new AlexaSetPowerStateController(endpoint);
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
