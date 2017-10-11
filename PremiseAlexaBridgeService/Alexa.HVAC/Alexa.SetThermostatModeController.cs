using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.HVAC
{
    #region SetColorTemperature Data Contracts

    [DataContract]
    public class AlexaSetThermostatModeControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetThermostatModeControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetThermostatModeControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetThermostatModePayload payload { get; set; }

        public AlexaSetThermostatModeControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetThermostatModePayload();
        }
    }

    [DataContract]
    public class AlexaSetThermostatModePayload
    {
        [DataMember(Name = "thermostatMode")]
        public AlexaSetThermostatModePayloadValue thermostatMode { get; set; }
        //public object thermostatMode { get; set; }

        public AlexaSetThermostatModePayload()
        {
            thermostatMode = new AlexaSetThermostatModePayloadValue();
        }

        public AlexaSetThermostatModePayload(string value)
        {
            thermostatMode = new AlexaSetThermostatModePayloadValue(value);
            //thermostatMode = value;
        }
    }

    [DataContract]
    public class AlexaSetThermostatModePayloadValue
    {
        [DataMember(Name = "value")]
        public string value { get; set; }

        [DataMember(Name = "customName", EmitDefaultValue = false)]
        public string customName { get; set; }

        public AlexaSetThermostatModePayloadValue(string valueString)
        {
            value = valueString;
        }

        public AlexaSetThermostatModePayloadValue()
        {

        }
    }

    #endregion

    public class AlexaSetThermostatModeController : AlexaControllerBase<
        AlexaSetThermostatModePayload, 
        ControlResponse, 
        AlexaSetThermostatModeControllerRequest>, IAlexaController
    {
        private readonly string @namespace = "Alexa.ThermostatController";
        private readonly string[] directiveNames = { "SetThermostatMode" };
        private readonly string[] premiseProperties = { "TemperatureMode", "FanControl" };
        public readonly string alexaProperty = "thermostatMode";
        public readonly AlexaHVAC PropertyHelpers;

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
            : base()
        {
            PropertyHelpers = new AlexaHVAC();
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
            int mode = this.endpoint.GetValue<int>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperty,
                value = TemperatureMode.ModeToString(mode),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
            //AlexaProperty property = new AlexaProperty(header)
            //{
            //    name = alexaProperty
            //};

            response.Event.header.@namespace = "Alexa";

            try
            {
                string mode = this.request.directive.payload.thermostatMode.value;
                switch (mode)
                {
                    case "AUTO":
                        this.endpoint.SetValue(premiseProperties[0], "0").GetAwaiter().GetResult();
                        break;
                    case "HEAT":
                        this.endpoint.SetValue(premiseProperties[0], "1").GetAwaiter().GetResult();
                        break;
                    case "COOL":
                        this.endpoint.SetValue(premiseProperties[0], "2").GetAwaiter().GetResult();
                        break;
                    case "OFF": // 3 is emergency heat in premise
                        this.endpoint.SetValue(premiseProperties[0], "4").GetAwaiter().GetResult();
                        break;
                    case "ECO":
                    default:
                        // not supported
                        break;
                }
                //property.timeOfSample = GetUtcTime();
                //property.value = mode;
                //this.response.context.properties.Add(property);
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
