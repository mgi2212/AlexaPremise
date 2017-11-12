using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.HVAC
{
    #region Thermostat Data Contracts

    [DataContract]
    public class AlexaThermostatControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaThermostatControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaThermostatControllerRequestDirective
    {
        #region Constructors

        public AlexaThermostatControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaThermostatRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaThermostatRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaThermostatMode
    {
        #region Constructors

        public AlexaThermostatMode(string valueString)
        {
            value = valueString;
        }

        public AlexaThermostatMode()
        {
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "customName", EmitDefaultValue = false)]
        public string customName { get; set; }

        [DataMember(Name = "value")]
        public string value { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaThermostatRequestPayload
    {
        #region Properties

        [DataMember(Name = "lowerSetpoint", EmitDefaultValue = false)]
        public AlexaTemperature lowerSetpoint { get; set; }

        [DataMember(Name = "targetSetpoint", EmitDefaultValue = false)]
        public AlexaTemperature targetSetpoint { get; set; }

        [DataMember(Name = "targetSetpointDelta", EmitDefaultValue = false)]
        public AlexaTemperature targetSetpointDelta { get; set; }

        [DataMember(Name = "thermostatMode", EmitDefaultValue = false)]
        public AlexaThermostatMode thermostatMode { get; set; }

        [DataMember(Name = "upperSetpoint", EmitDefaultValue = false)]
        public AlexaTemperature upperSetpoint { get; set; }

        #endregion Properties
    }

    #endregion Thermostat Data Contracts

    public class AlexaThermostatController : AlexaControllerBase<
        AlexaThermostatRequestPayload,
        ControlResponse,
        AlexaThermostatControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaHVAC PropertyHelpers;
        private const string Namespace = "Alexa.ThermostatController";
        private readonly string[] _alexaProperties = { "lowerSetpoint", "upperSetpoint", "targetSetpoint", "thermostatMode" };
        private readonly string[] _directiveNames = { "SetTargetTemperature", "AdjustTargetTemperature", "SetThermostatMode" };
        private readonly string[] _premiseProperties = { "HeatingSetPoint", "CoolingSetPoint", "CurrentSetPoint", "TemperatureMode" };

        #endregion Fields

        #region Constructors

        public AlexaThermostatController(AlexaThermostatControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public AlexaThermostatController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public AlexaThermostatController()
        {
            PropertyHelpers = new AlexaHVAC();
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

            for (int x = 0; x <= 2; x++)
            {
                Temperature temp = new Temperature(Endpoint.GetValueAsync<double>(_premiseProperties[x]).GetAwaiter().GetResult());
                AlexaProperty property = new AlexaProperty
                {
                    @namespace = Namespace,
                    name = _alexaProperties[x],
                    value = new AlexaTemperature(Math.Round(temp.Fahrenheit, 1), "FAHRENHEIT"),
                    timeOfSample = PremiseServer.UtcTimeStamp()
                };
                properties.Add(property);
            }

            int mode = Endpoint.GetValueAsync<int>(_premiseProperties[3]).GetAwaiter().GetResult();
            AlexaProperty thermostatMode = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[3],
                value = ModeToString(mode),
                timeOfSample = PremiseServer.UtcTimeStamp()
            };
            properties.Add(thermostatMode);

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
            int x = 0;
            foreach (string property in _premiseProperties)
            {
                if (premiseProperty == property)
                {
                    return _alexaProperties[x];
                }
                x++;
            }
            return "";
        }

        public void ProcessControllerDirective()
        {
            try
            {
                if (Payload.targetSetpoint != null)
                {
                    Temperature target = new Temperature(Payload.targetSetpoint.scale, Payload.targetSetpoint.value);
                    Endpoint.SetValueAsync("CurrentSetPoint", Math.Round(target.Kelvin, 1).ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                }
                if (Payload.lowerSetpoint != null)
                {
                    Temperature lower = new Temperature(Payload.lowerSetpoint.scale, Payload.lowerSetpoint.value);
                    Endpoint.SetValueAsync("HeatingSetPoint", Math.Round(lower.Kelvin, 1).ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                }
                if (Payload.upperSetpoint != null)
                {
                    Temperature upper = new Temperature(Payload.upperSetpoint.scale, Payload.upperSetpoint.value);
                    Endpoint.SetValueAsync("CoolingSetPoint", Math.Round(upper.Kelvin, 1).ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                }
                if (Payload.thermostatMode != null)
                {
                    string mode = Request.directive.payload.thermostatMode.value;
                    switch (mode)
                    {
                        case "AUTO":
                            Endpoint.SetValueAsync("TemperatureMode", "0").GetAwaiter().GetResult();
                            break;

                        case "HEAT":
                            Endpoint.SetValueAsync("TemperatureMode", "1").GetAwaiter().GetResult();
                            break;

                        case "COOL":
                            Endpoint.SetValueAsync("TemperatureMode", "2").GetAwaiter().GetResult();
                            break;

                        case "OFF": // 3 is emergency heat in premise
                            Endpoint.SetValueAsync("TemperatureMode", "4").GetAwaiter().GetResult();
                            break;
                    }
                }
                if (Payload.targetSetpointDelta != null)
                {
                    Temperature target = new Temperature(Endpoint.GetValueAsync<double>("CurrentSetPoint").GetAwaiter().GetResult());
                    switch (Payload.targetSetpointDelta.scale)
                    {
                        case "FAHRENHEIT":
                            target.Fahrenheit += Payload.targetSetpointDelta.value;
                            break;

                        case "CELSIUS":
                            target.Celsius += Payload.targetSetpointDelta.value;
                            break;

                        case "KELVIN":
                            target.Kelvin += Payload.targetSetpointDelta.value;
                            break;
                    }
                    Endpoint.SetValueAsync("CurrentSetPoint", Math.Round(target.Kelvin, 1).ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
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

        #region Public Methods

        public static string ModeToString(int mode)
        {
            switch (mode)
            {
                case 0:
                    return "AUTO";

                case 1:
                    return "HEAT";

                case 2:
                    return "COOL";

                case 4:
                    return "OFF";
            }

            return "ERROR";
        }

        #endregion Public Methods
    }
}