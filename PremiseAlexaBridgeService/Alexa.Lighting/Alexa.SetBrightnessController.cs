using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Linq;
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
        private readonly string[] premiseProperties = { "Brightness" };
        private readonly string[] alexaProperties = { "brightness" };
        public readonly AlexaLighting PropertyHelpers;

        public AlexaSetBrightnessController(AlexaSetBrightnessControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaSetBrightnessController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }
        public AlexaSetBrightnessController()
            : base()
        {
            PropertyHelpers = new AlexaLighting();
        }

        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public string GetAssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string[] GetDirectiveNames()
        {
            return directiveNames;
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

        public string AssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
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
                double setValue = (double)(payload.brightness / 100.00).LimitToRange(0.00, 1.000);
                this.endpoint.SetValue(premiseProperties[0], setValue.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = ((int)(setValue * 100)).LimitToRange(0, 100);
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
