using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.HVAC
{
    #region SetTargetTemperature Data Contracts

    [DataContract]
    public class AlexaSetTargetTemperatureControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetTargetTemperatureControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetTargetTemperatureControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetTargetTemperatureRequestPayload payload { get; set; }

        public AlexaSetTargetTemperatureControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetTargetTemperatureRequestPayload();
        }
    }

    [DataContract]
    public class AlexaSetTargetTemperatureRequestPayload
    {
        [DataMember(Name = "targetSetpoint", EmitDefaultValue = false)]
        public AlexaTemperatureSensorResponsePayload targetSetpoint { get; set; }
        [DataMember(Name = "lowerSetpoint", EmitDefaultValue = false)]
        public AlexaTemperatureSensorResponsePayload lowerSetpoint { get; set; }
        [DataMember(Name = "upperSetpoint", EmitDefaultValue = false)]
        public AlexaTemperatureSensorResponsePayload upperSetpoint { get; set; }
    }

    #endregion

    public class SetTargetTemperatureController : AlexaControllerBase<
        AlexaSetTargetTemperatureRequestPayload, 
        ControlResponse,
        AlexaSetTargetTemperatureControllerRequest>, IAlexaController
    {
        private readonly string @namespace = "Alexa.ThermostatController";
        private readonly string[] directiveNames = { "SetTargetTemperature" };
        private readonly string[] premiseProperties = { "Temperature" };
        public readonly string alexaProperty = "targetTemperature";
        public readonly AlexaHVAC PropertyHelpers;

        public SetTargetTemperatureController(AlexaSetTargetTemperatureControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public SetTargetTemperatureController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public SetTargetTemperatureController()
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
            return null;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            List<AlexaProperty> properties = new List<AlexaProperty>();
            
            Temperature lower = new Temperature(this.endpoint.GetValue<double>("HeatingSetPoint").GetAwaiter().GetResult());
            AlexaProperty lowerSetpoint = new AlexaProperty
            { 
                @namespace = @namespace,
                name = "lowerSetpoint",
                value = new AlexaTemperatureSensorResponsePayload(Math.Round(lower.Fahrenheit,1), "FAHRENHEIT"),
                timeOfSample = GetUtcTime()
            };
            properties.Add(lowerSetpoint);

            Temperature upper = new Temperature(this.endpoint.GetValue<double>("CoolingSetPoint").GetAwaiter().GetResult());
            AlexaProperty upperSetpoint = new AlexaProperty
            {
                @namespace = @namespace,
                name = "upperSetpoint",
                value = new AlexaTemperatureSensorResponsePayload(Math.Round(upper.Fahrenheit, 1), "FAHRENHEIT"),
                timeOfSample = GetUtcTime()
            };
            properties.Add(upperSetpoint);

            Temperature target = new Temperature(this.endpoint.GetValue<double>("CurrentSetPoint").GetAwaiter().GetResult());
            AlexaProperty targetSetpoint = new AlexaProperty
            {
                @namespace = @namespace,
                name = "targetSetpoint",
                value = new AlexaTemperatureSensorResponsePayload(Math.Round(target.Fahrenheit, 1), "FAHRENHEIT"),
                timeOfSample = GetUtcTime()
            };
            properties.Add(targetSetpoint);
            return properties;
        }


        public void ProcessControllerDirective()
        {

            response.Event.header.@namespace = "Alexa";
            try
            {
                if (payload.targetSetpoint != null)
                {
                    Temperature target = new Temperature();
                    switch (payload.targetSetpoint.scale)
                    {
                        case "FAHRENHEIT":
                            target.Fahrenheit = payload.targetSetpoint.value;
                            break;
                        case "CELCIUS":
                            target.Celcius = payload.targetSetpoint.value;
                            break;
                        case "KELVIN":
                            target.Kelvin = payload.targetSetpoint.value;
                            break;
                        default:
                            break;
                    }
                    this.endpoint.SetValue("CurrentSetPoint", Math.Round(target.Kelvin,1).ToString()).GetAwaiter().GetResult();
                }
                if (payload.lowerSetpoint != null)
                {
                    Temperature lower = new Temperature();
                    switch (payload.lowerSetpoint.scale)
                    {
                        case "FAHRENHEIT":
                            lower.Fahrenheit = payload.lowerSetpoint.value;
                            break;
                        case "CELCIUS":
                            lower.Celcius = payload.lowerSetpoint.value;
                            break;
                        case "KELVIN":
                            lower.Kelvin = payload.lowerSetpoint.value;
                            break;
                        default:
                            break;
                    }
                    this.endpoint.SetValue("HeatingSetPoint", Math.Round(lower.Kelvin, 1).ToString()).GetAwaiter().GetResult();
                }
                if (payload.upperSetpoint != null)
                {
                    Temperature upper = new Temperature();
                    switch (payload.upperSetpoint.scale)
                    {
                        case "FAHRENHEIT":
                            upper.Fahrenheit = payload.upperSetpoint.value;
                            break;
                        case "CELCIUS":
                            upper.Celcius = payload.upperSetpoint.value;
                            break;
                        case "KELVIN":
                            upper.Kelvin = payload.upperSetpoint.value;
                            break;
                        default:
                            break;
                    }
                    this.endpoint.SetValue("CoolingSetPoint", Math.Round(upper.Kelvin, 1).ToString()).GetAwaiter().GetResult();
                }

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
