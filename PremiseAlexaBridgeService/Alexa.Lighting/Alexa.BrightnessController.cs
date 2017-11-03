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
    #region Adjust Brightness Data Contracts

    [DataContract]
    public class AlexaBrightness
    {
        #region Properties

        [DataMember(Name = "brightness", EmitDefaultValue = false)]
        public int brightness { get; set; }

        [DataMember(Name = "brightnessDelta", EmitDefaultValue = false)]
        public int brightnessDelta { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaBrightnessControllerDirective
    {
        #region Constructors

        public AlexaBrightnessControllerDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaBrightness();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaBrightness payload { get; set; }

        #endregion Properties
    }

    public class AlexaBrightnessControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaBrightnessControllerDirective directive { get; set; }

        #endregion Properties
    }

    #endregion Adjust Brightness Data Contracts

    public class AlexaBrightnessController : AlexaControllerBase<
        AlexaBrightness,
        ControlResponse,
        AlexaBrightnessControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaLighting PropertyHelpers;
        private const string Namespace = "Alexa.BrightnessController";
        private readonly string[] _alexaProperties = { "brightness" };
        private readonly string[] _directiveNames = { "SetBrightness", "AdjustBrightness" };
        private readonly string[] _premiseProperties = { "Brightness" };

        #endregion Fields

        #region Constructors

        public AlexaBrightnessController(AlexaBrightnessControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaBrightnessController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaBrightnessController()
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

            double brightness = Endpoint.GetValue<double>(_premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[0],
                value = (int)((brightness * 100)).LimitToRange(0, 100),
                timeOfSample = PremiseServer.GetUtcTime()
            };
            properties.Add(property);

            return properties;
        }

        public bool HasAlexaProperty(string property)
        {
            return _alexaProperties.Contains(property);
        }

        public bool HasPremiseProperty(string property)
        {
            return _premiseProperties.Contains(property);
        }

        public string MapPremisePropertyToAlexaProperty(string premiseProperty)
        {
            return _premiseProperties.Contains(premiseProperty) ? "brightness" : "";
        }

        public void ProcessControllerDirective()
        {
            try
            {
                switch (Header.name)
                {
                    case "AdjustBrightness":
                        double adjustValue = Math.Round((Payload.brightnessDelta / 100.00), 2).LimitToRange(-1.00, 1.00);
                        double currentValue = Math.Round(Endpoint.GetValue<double>(_premiseProperties[0]).GetAwaiter().GetResult(), 2);
                        double valueToSend = Math.Round(currentValue + adjustValue, 2).LimitToRange(0.00, 1.00);
                        Endpoint.SetValue(_premiseProperties[0], valueToSend.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                        break;

                    case "SetBrightness":
                        double setValue = (Payload.brightness / 100.00).LimitToRange(0.00, 1.000);
                        Endpoint.SetValue(_premiseProperties[0], setValue.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                        break;

                    default:
                        ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                        return;
                }
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