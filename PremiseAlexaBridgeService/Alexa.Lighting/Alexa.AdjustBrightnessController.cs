using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Linq;
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
        public readonly AlexaLighting PropertyHelpers;
        private readonly string @namespace = "Alexa.BrightnessController";
        private readonly string[] directiveNames = { "AdjustBrightness" };
        private readonly string[] premiseProperties = { "Brightness" };
       private readonly string[] alexaProperties = { "brightness" };

        public AlexaAdjustBrightnessController(AlexaAdjustBrightnessControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaAdjustBrightnessController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaAdjustBrightnessController()
            : base()
        {
            PropertyHelpers = new AlexaLighting();
        }

        public string GetNameSpace()
        {
            return @namespace;
        }
        public string [] GetDirectiveNames()
        {
            return directiveNames;
        }

        public string GetAssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public bool HasAlexaProperty(string property)
        {
            return (this.alexaProperties.Contains(property));
        }

        public bool HasPremiseProperty(string property)
        {
            foreach (string s in this.premiseProperties)
            {
                if (s == property)
                    return true;
            }
            return false;
        }

        public AlexaProperty GetPropertyState()
        {
            double brightness = this.endpoint.GetValue<double>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                value = (int)((brightness * 100)).LimitToRange(0, 100),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
            AlexaProperty property = new AlexaProperty(header)
            {
                name = alexaProperties[0]
            };
            response.Event.header.@namespace = "Alexa";

            try
            {
                double adjustValue = Math.Round(((double)payload.brightnessDelta / 100.00), 2).LimitToRange(-1.00, 1.00);
                double currentValue = Math.Round(endpoint.GetValue<Double>(premiseProperties[0]).GetAwaiter().GetResult(), 2);
                double valueToSend = Math.Round(currentValue + adjustValue, 2).LimitToRange(0.00, 1.00);
                endpoint.SetValue(premiseProperties[0], valueToSend.ToString()).GetAwaiter().GetResult();
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
