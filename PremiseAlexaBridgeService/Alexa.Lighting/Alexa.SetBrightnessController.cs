using Alexa.Controller;
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
        private readonly string @namespace = "Alexa.BrightnessController";
        private readonly string[] directiveNames = { "SetBrightness" };
        private readonly string premiseProperty = "Brightness";
        private readonly string alexaProperty = "brightness";
        public readonly AlexaLighting PropertyHelpers = new AlexaLighting();

        public AlexaSetBrightnessController(AlexaSetBrightnessControllerRequest request)
            : base(request)
        {
        }

        public AlexaSetBrightnessController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string [] GetDirectiveNames()
        {
            return directiveNames;
        }

        public AlexaProperty GetPropertyState()
        {
            double brightness = this.endpoint.GetValue<double>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperty,
                value = (int)((brightness * 100)).LimitToRange(0,100),
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
                this.Response.Event.header.name = "Response";
                this.response.context.properties.AddRange(this.PropertyHelpers.FindRelatedProperties(endpoint, @namespace));
            }
            catch (Exception ex)
            {
                base.ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }
    }
}
