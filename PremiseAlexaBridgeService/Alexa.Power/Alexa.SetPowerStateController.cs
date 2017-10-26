using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;

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
        private readonly string @namespace = "Alexa.PowerController";
        private readonly string[] alexaProperties = { "powerState" };
        private readonly string[] directiveNames = { "TurnOn", "TurnOff" };
        private readonly string[] premiseProperties = { "PowerState" };

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

        public string AssemblyTypeName()
        {
            return GetType().AssemblyQualifiedName;
        }

        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public string GetAssemblyTypeName()
        {
            return GetType().AssemblyQualifiedName;
        }

        public string[] GetDirectiveNames()
        {
            return directiveNames;
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string[] GetPremiseProperties()
        {
            return premiseProperties;
        }

        public AlexaProperty GetPropertyState()
        {
            bool powerState = Endpoint.GetValue<bool>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                value = (powerState ? "ON" : "OFF"),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            return null;
        }

        public bool HasAlexaProperty(string property)
        {
            return (alexaProperties.Contains(property));
        }

        public bool HasPremiseProperty(string property)
        {
            foreach (string s in premiseProperties)
            {
                if (s == property)
                    return true;
            }
            return false;
        }

        public void ProcessControllerDirective()
        {
            AlexaProperty property = new AlexaProperty(Header)
            {
                name = alexaProperties[0]
            };

            Response.Event.header.@namespace = "Alexa";

            try
            {
                string valueToSend;
                switch (Header.name)
                {
                    case "TurnOff":
                        valueToSend = "False";
                        property.value = "OFF";
                        break;

                    case "TurnOn":
                        valueToSend = "True";
                        property.value = "ON";
                        break;

                    default:
                        ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                        return;
                }
                // only change powerState if it needs to.
                if (GetPropertyState().value != property.value)
                {
                    Endpoint.SetValue(premiseProperties[0], valueToSend).GetAwaiter().GetResult();
                }
                property.timeOfSample = GetUtcTime();
                Response.context.properties.Add(property);
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, @namespace));
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return;
            }

            Response.Event.header.name = "Response";
        }

        #endregion Methods
    }
}