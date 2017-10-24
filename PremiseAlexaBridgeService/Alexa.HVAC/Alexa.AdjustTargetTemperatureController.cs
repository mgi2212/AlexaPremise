using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Linq;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.HVAC
{
    #region AdjustTemperature Data Contracts

    [DataContract]
    public class AlexaAdjustTargetTemperatureControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaAdjustTargetTemperatureControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaAdjustTargetTemperatureControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaAdjustTemperatureRequestPayload payload { get; set; }

        public AlexaAdjustTargetTemperatureControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaAdjustTemperatureRequestPayload();
        }
    }

    [DataContract]
    public class AlexaAdjustTemperatureRequestPayload
    {
        [DataMember(Name = "targetSetpointDelta")]
        public AlexaTemperatureSensorResponsePayload targetSetpointDelta { get; set; }
    }

    #endregion

    public class AlexaAdjustTargetTemperatureController : AlexaControllerBase<
        AlexaAdjustTemperatureRequestPayload,
        ControlResponse,
        AlexaAdjustTargetTemperatureControllerRequest>, IAlexaController
    {
        private readonly string @namespace = "Alexa.ThermostatController";
        private readonly string[] directiveNames = { "AdjustTargetTemperature" };
        private readonly string[] premiseProperties = { "Temperature" };
        private readonly string[] alexaProperties = { "targetSetpoint" };
        public readonly AlexaHVAC PropertyHelpers;

        public AlexaAdjustTargetTemperatureController(AlexaAdjustTargetTemperatureControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public AlexaAdjustTargetTemperatureController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public AlexaAdjustTargetTemperatureController()
            : base()
        {
            PropertyHelpers = new AlexaHVAC();
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
            return null;
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

                Temperature target = new Temperature(this.endpoint.GetValue<double>("CurrentSetPoint").GetAwaiter().GetResult());

                switch (payload.targetSetpointDelta.scale)
                {
                    case "FAHRENHEIT":
                        target.Fahrenheit += payload.targetSetpointDelta.value;
                        break;
                    case "CELCIUS":
                        target.Celcius += payload.targetSetpointDelta.value;
                        break;
                    case "KELVIN":
                        target.Kelvin += payload.targetSetpointDelta.value;
                        break;
                }

                this.endpoint.SetValue("CurrentSetPoint", Math.Round(target.Kelvin, 1).ToString()).GetAwaiter().GetResult();

                //AlexaProperty targetSetpoint = new AlexaProperty
                //{
                //    @namespace = @namespace,
                //    name = "targetSetpoint",
                //    value = new AlexaTemperatureSensorResponsePayload(Math.Round(target.Fahrenheit, 1), "FAHRENHEIT"),
                //    timeOfSample = GetUtcTime()
                //};
                //this.response.context.properties.Add(targetSetpoint);
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
