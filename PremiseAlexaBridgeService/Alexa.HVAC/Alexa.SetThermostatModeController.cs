using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.HVAC
{
    #region SetColorTemperature Data Contracts

    [DataContract]
    public class AlexaSetThermostatModeControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaSetThermostatModeControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSetThermostatModeControllerRequestDirective
    {
        #region Constructors

        public AlexaSetThermostatModeControllerRequestDirective()
        {
            header = new Header();
            Endpoint = new DirectiveEndpoint();
            payload = new AlexaSetThermostatModePayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "Endpoint")]
        public DirectiveEndpoint Endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetThermostatModePayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSetThermostatModePayload
    {
        #region Constructors

        public AlexaSetThermostatModePayload()
        {
            thermostatMode = new AlexaSetThermostatModePayloadValue();
        }

        //public object thermostatMode { get; set; }
        public AlexaSetThermostatModePayload(string value)
        {
            thermostatMode = new AlexaSetThermostatModePayloadValue(value);
            //thermostatMode = value;
        }

        #endregion Constructors
    }

    #endregion SetColorTemperature Data Contracts

    public class AlexaSetThermostatModeController : AlexaControllerBase<
        AlexaSetThermostatModePayload,
        ControlResponse,
        AlexaSetThermostatModeControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaHVAC PropertyHelpers;
        private readonly string @namespace = "Alexa.ThermostatController";
        private readonly string[] alexaProperties = { "thermostatMode" };
        private readonly string[] directiveNames = { "SetThermostatMode" };
        private readonly string[] premiseProperties = { "TemperatureMode" };

        #endregion Fields

        #region Constructors

        public AlexaSetThermostatModeController(AlexaSetThermostatModeControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public AlexaSetThermostatModeController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public AlexaSetThermostatModeController()
        {
            PropertyHelpers = new AlexaHVAC();
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
            int mode = Endpoint.GetValue<int>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                value = TemperatureMode.ModeToString(mode),
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
            return alexaProperties.Contains(property);
        }

        public bool HasPremiseProperty(string property)
        {
            return premiseProperties.Contains(property);
        }

        public void ProcessControllerDirective()
        {
            Response.Event.header.@namespace = "Alexa";

            try
            {
                string mode = Request.directive.payload.thermostatMode.value;
                switch (mode)
                {
                    case "AUTO":
                        Endpoint.SetValue(premiseProperties[0], "0").GetAwaiter().GetResult();
                        break;

                    case "HEAT":
                        Endpoint.SetValue(premiseProperties[0], "1").GetAwaiter().GetResult();
                        break;

                    case "COOL":
                        Endpoint.SetValue(premiseProperties[0], "2").GetAwaiter().GetResult();
                        break;

                    case "OFF": // 3 is emergency heat in premise
                        Endpoint.SetValue(premiseProperties[0], "4").GetAwaiter().GetResult();
                        break;

                    case "ECO":
                    default:
                        // not supported
                        break;
                }

                Response.Event.header.name = "Response";
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, @namespace));
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }

        #endregion Methods
    }
}