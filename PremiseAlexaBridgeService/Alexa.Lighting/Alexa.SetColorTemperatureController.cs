using Alexa.Controller;
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
        private readonly string @namespace = "Alexa.ColorTemperatureController";
        private readonly string[] directiveNames = { "SetColorTemperature" };
        private readonly string[] premiseProperties = { "Temperature" };
        public readonly string alexaProperty = "colorTemperatureInKelvin";
        public readonly AlexaLighting PropertyHelpers;

        public AlexaSetColorTemperatureController(AlexaSetColorTemperatureControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaSetColorTemperatureController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaSetColorTemperatureController()
            : base()
        {
            PropertyHelpers = new AlexaLighting();
        }

        public string GetAlexaProperty()
        {
            return alexaProperty;
        }

        public string GetAssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string [] GetDirectiveNames()
        {
            return directiveNames;
        }

        public bool HasAlexaProperty(string property)
        {
            return (property == this.alexaProperty);
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

        public string AssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public AlexaProperty GetPropertyState()
        {
            double ColorTemperature = this.endpoint.GetValue<double>(premiseProperties[0]).GetAwaiter().GetResult();
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

            response.Event.header.@namespace = "Alexa";


            try
            {
                int setValue = payload.colorTemperatureInKelvin.LimitToRange(1000, 10000);
                this.endpoint.SetValue(premiseProperties[0], setValue.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = setValue;
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
