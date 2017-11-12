using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.Lighting
{
    #region SetColor Data Contracts

    [DataContract]
    public class AlexaColorControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaColorControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaColorControllerRequestDirective
    {
        #region Constructors

        public AlexaColorControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaColorControllerRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaColorControllerRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaColorControllerRequestPayload
    {
        #region Properties

        [DataMember(Name = "color")]
        public AlexaColorValue color { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaColorValue
    {
        #region Properties

        [DataMember(Name = "brightness", EmitDefaultValue = true, Order = 3)]
        public double brightness { get; set; }

        [DataMember(Name = "hue", EmitDefaultValue = true, Order = 1)]
        public double hue { get; set; }

        [DataMember(Name = "saturation", EmitDefaultValue = true, Order = 2)]
        public double saturation { get; set; }

        #endregion Properties
    }

    #endregion SetColor Data Contracts

    public class AlexaColorController : AlexaControllerBase<
        AlexaColorControllerRequestPayload,
        ControlResponse,
        AlexaColorControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaLighting PropertyHelpers;
        private const string Namespace = "Alexa.ColorController";
        private readonly string[] _alexaProperties = { "color" };
        private readonly string[] _directiveNames = { "SetColor" };
        private readonly string[] _premiseProperties = { "Hue", "Saturation", "Brightness" };

        #endregion Fields

        #region Constructors

        public AlexaColorController(AlexaColorControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaColorController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaColorController()
        {
            PropertyHelpers = new AlexaLighting();
        }

        #endregion Constructors

        #region Methods

        public string[] GetAlexaProperties()
        {
            return _alexaProperties;
        }

        public string GetAssemblyTypeName()
        {
            return PropertyHelpers.GetType().AssemblyQualifiedName;
        }

        public string[] GetDirectiveNames()
        {
            return _directiveNames;
        }

        public string GetNameSpace()
        {
            return Namespace;
        }

        public string[] GetPremiseProperties()
        {
            return _premiseProperties;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            List<AlexaProperty> properties = new List<AlexaProperty>();

            AlexaColorValue colorValue = new AlexaColorValue();
            foreach (string premiseProperty in _premiseProperties)
            {
                switch (premiseProperty)
                {
                    case "Hue":
                        colorValue.hue = Math.Round(Endpoint.GetValueAsync<double>(premiseProperty).GetAwaiter().GetResult().LimitToRange(0.0, 360.0), 4);
                        break;

                    case "Saturation":
                        colorValue.saturation = Math.Round(Endpoint.GetValueAsync<double>(premiseProperty).GetAwaiter().GetResult().LimitToRange(0.0, 1.0), 4);
                        break;

                    case "Brightness":
                        colorValue.brightness = Math.Round(Endpoint.GetValueAsync<double>(premiseProperty).GetAwaiter().GetResult().LimitToRange(0.0, 1.0), 4);
                        break;
                }
            }

            AlexaProperty property = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[0],
                value = colorValue,
                timeOfSample = PremiseServer.UtcTimeStamp()
            };

            properties.Add(property);

            return properties;
        }

        public bool HasAlexaProperty(string property)
        {
            return (_alexaProperties.Contains(property));
        }

        public bool HasPremiseProperty(string property)
        {
            return _premiseProperties.Contains(property);
        }

        public string MapPremisePropertyToAlexaProperty(string premiseProperty)
        {
            foreach (string property in _premiseProperties)
            {
                if (premiseProperty == property)
                {
                    return "color";
                }
            }
            return "";
        }

        public void ProcessControllerDirective()
        {
            try
            {
                Endpoint.SetValueAsync("Hue", Payload.color.hue.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                Endpoint.SetValueAsync("Saturation", Payload.color.saturation.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                Endpoint.SetValueAsync("Brightness", Payload.color.brightness.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                Response.Event.header.name = "Response";
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, ""));
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }

        public void SetEndpoint(IPremiseObject premiseObject)
        {
            Endpoint = premiseObject;
        }

        public bool ValidateDirective()
        {
            return ValidateDirective(GetDirectiveNames(), GetNameSpace());
        }

        #endregion Methods
    }
}