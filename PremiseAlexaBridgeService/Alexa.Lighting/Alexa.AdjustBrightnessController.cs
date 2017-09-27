using Alexa.Controller;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
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
        public AlexaAdjustBrightnessControllerDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaAdjustBrightnessControllerDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaAdjustBrightnessPayload payload { get; set; }

        public AlexaAdjustBrightnessControllerDirective()
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

    public class AlexaAdjustBrightnessController : AlexaControllerBase<
        AlexaAdjustBrightnessPayload, 
        ControlResponse, 
        AlexaAdjustBrightnessControllerRequest>, IAlexaController 
    {
        public readonly AlexaLighting PropertyHelpers = new AlexaLighting();
        private readonly string @namespace = "Alexa.BrightnessController";
        private readonly string[] directiveNames = { "AdjustBrightness" };
        private readonly string premiseProperty = "Brightness";
        private readonly string alexaProperty = "brightness";

        public AlexaAdjustBrightnessController(AlexaAdjustBrightnessControllerRequest request)
            : base(request)
        {
        }

        public AlexaAdjustBrightnessController(IPremiseObject endpoint)
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
                value = (int)(brightness * 100),
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
                double adjustValue = Math.Round(((double)payload.brightnessDelta / 100.00), 2).LimitToRange(-1.00, 1.00);
                double currentValue = Math.Round(endpoint.GetValue<Double>(premiseProperty).GetAwaiter().GetResult(), 2);
                double valueToSend = Math.Round(currentValue + adjustValue, 2).LimitToRange(0.00, 1.00);
                endpoint.SetValue(premiseProperty, valueToSend.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = (int)(valueToSend * 100);
                response.context.properties.Add(property);

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
