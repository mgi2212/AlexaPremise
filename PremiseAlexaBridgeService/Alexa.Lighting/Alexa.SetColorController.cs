using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Lighting
{
    #region SetColor Data Contracts

    [DataContract]
    public class AlexaSetColorControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetColorControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetColorControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetColorRequestPayload payload { get; set; }

        public AlexaSetColorControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetColorRequestPayload();
        }
    }

    [DataContract]
    public class AlexaColorValue
    {
        [DataMember(Name = "hue", EmitDefaultValue = true, Order = 1)]
        public double hue { get; set; }
        [DataMember(Name = "saturation", EmitDefaultValue = true, Order = 2)]
        public double saturation { get; set; }
        [DataMember(Name = "brightness", EmitDefaultValue = true, Order = 3)]
        public double brightness { get; set; }
    }

    [DataContract]
    public class AlexaSetColorRequestPayload
    {
        [DataMember(Name = "color")]
        public AlexaColorValue color { get; set; }
    }

    #endregion

    public class AlexaSetColorController : AlexaControllerBase<
        AlexaSetColorRequestPayload,
        ControlResponse,
        AlexaSetColorControllerRequest>, IAlexaController
    {
        private readonly string @namespace = "Alexa.ColorController";
        private readonly string[] directiveNames = { "SetColor" };
        private readonly string[] premiseProperties = { "Hue", "Saturation", "Brightness" };
        public readonly string alexaProperty = "color";
        public readonly AlexaLighting PropertyHelpers;

        public AlexaSetColorController(AlexaSetColorControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaSetColorController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaSetColorController()
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

        public string[] GetDirectiveNames()
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
            AlexaColorValue colorValue = new AlexaColorValue();
            foreach (string premiseProperty in premiseProperties)
            {
                switch (premiseProperty)
                {
                    case "Hue":
                        colorValue.hue = Math.Round(endpoint.GetValue<double>(premiseProperty).GetAwaiter().GetResult().LimitToRange(0.0, 360.0), 4);
                        break;
                    case "Saturation":
                        colorValue.saturation = Math.Round(endpoint.GetValue<double>(premiseProperty).GetAwaiter().GetResult().LimitToRange(0.0, 1.0),4);
                        break;
                    case "Brightness":
                        colorValue.brightness = Math.Round(endpoint.GetValue<double>(premiseProperty).GetAwaiter().GetResult().LimitToRange(0.0, 1.0), 4);
                        break;
                    default:
                        break;
                }
            }
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperty,
                value = colorValue,
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
                this.endpoint.SetValue("Hue", payload.color.hue.ToString()).GetAwaiter().GetResult();
                this.endpoint.SetValue("Saturation", payload.color.saturation.ToString()).GetAwaiter().GetResult();
                this.endpoint.SetValue("Brightness", payload.color.brightness.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = payload.color;
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
