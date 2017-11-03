using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;
using PremiseAlexaBridgeService;

namespace Alexa.Power
{
    #region PowerState Data Contracts

    [DataContract]
    public class AlexaSetPowerStateControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaSetPowerStateControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSetPowerStateControllerRequestDirective
    {
        #region Constructors

        public AlexaSetPowerStateControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetPowerStateRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetPowerStateRequestPayload payload { get; set; }

        #endregion Properties
    }

    public class AlexaSetPowerStateRequestPayload
    {
    }

    #endregion PowerState Data Contracts

    public class AlexaSetPowerStateController : AlexaControllerBase<
        AlexaSetPowerStateRequestPayload,
        ControlResponse,
        AlexaSetPowerStateControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaPower PropertyHelpers;
        private const string Namespace = "Alexa.PowerController";
        private readonly string[] _alexaProperties = { "powerState" };
        private readonly string[] _directiveNames = { "TurnOn", "TurnOff" };
        private readonly string[] _premiseProperties = { "PowerState" };

        #endregion Fields

        #region Constructors

        public AlexaSetPowerStateController(AlexaSetPowerStateControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaPower();
        }

        public AlexaSetPowerStateController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaPower();
        }

        public AlexaSetPowerStateController()
        {
            PropertyHelpers = new AlexaPower();
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

            bool powerState = Endpoint.GetValue<bool>(_premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[0],
                value = (powerState ? "ON" : "OFF"),
                timeOfSample = PremiseServer.GetUtcTime()
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
            foreach (string s in _premiseProperties)
            {
                if (s == property)
                    return true;
            }
            return false;
        }

        public string MapPremisePropertyToAlexaProperty(string premiseProperty)
        {
            return _premiseProperties.Contains(premiseProperty) ? "powerState" : "";
        }

        public void ProcessControllerDirective()
        {
            try
            {
                bool valueToSend = false;
                switch (Header.name)
                {
                    case "TurnOff":
                        break;

                    case "TurnOn":
                        valueToSend = true;
                        break;

                    default:
                        ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                        return;
                }
                // only change powerState if it needs to.
                if ((string)GetPropertyStates()[0].value != (valueToSend ? "ON" : "OFF"))
                {
                    Endpoint.SetValue(_premiseProperties[0], valueToSend.ToString()).GetAwaiter().GetResult();
                }
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, ""));
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return;
            }

            Response.Event.header.name = "Response";
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