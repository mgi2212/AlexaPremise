using Alexa.Controller;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Lighting
{
    #region SetColorTemperature Data Contracts

    [DataContract]
    public class AlexaSetColorTemperatureControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetColorTemperatureControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetColorTemperatureControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetColorTemperatureRequestPayload payload { get; set; }

        public AlexaSetColorTemperatureControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetColorTemperatureRequestPayload();
        }
    }

    [DataContract]
    public class AlexaSetColorTemperatureRequestPayload
    {
        [DataMember(Name = "colorTemperatureInKelvin")]
        public int colorTemperatureInKelvin { get; set; }
    }

    #endregion

    public class AlexaSetColorTemperatureController : AlexaControllerBase<
        AlexaSetColorTemperatureRequestPayload, 
        ControlResponse, 
        AlexaSetColorTemperatureControllerRequest>, IAlexaController
    {
        public readonly string @namespace = "Alexa.ColorTemperatureController";
        public readonly string[] directiveNames = { "SetColorTemperature" };
        public readonly string premiseProperty = "Temperature";
        public readonly string alexaProperty = "colorTemperatureInKelvin";

        public AlexaSetColorTemperatureController(AlexaSetColorTemperatureControllerRequest request)
            : base(request)
        {
        }

        public AlexaSetColorTemperatureController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public AlexaProperty GetPropertyState()
        {
            double ColorTemperature = this.endpoint.GetValue<double>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperty,
                value = ((int)ColorTemperature).LimitToRange(1000,10000),
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
                int setValue = payload.colorTemperatureInKelvin.LimitToRange(1000, 10000);
                this.endpoint.SetValue(premiseProperty, setValue.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = setValue;
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
                        case "Alexa.BrightnessController": 
                            {
                                AlexaSetBrightnessController controller = new AlexaSetBrightnessController(endpoint);
                                AlexaProperty brightness = controller.GetPropertyState();
                                response.context.properties.Add(brightness);
                            }
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

                        case "Alexa.ColorTemperatureController": // already added
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
