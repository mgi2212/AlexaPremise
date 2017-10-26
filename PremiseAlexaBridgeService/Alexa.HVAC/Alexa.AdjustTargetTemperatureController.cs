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
    #region AdjustTemperature Data Contracts

    [DataContract]
    public class AlexaAdjustTargetTemperatureControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaAdjustTargetTemperatureControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaAdjustTargetTemperatureControllerRequestDirective
    {
        #region Constructors

        public AlexaAdjustTargetTemperatureControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaAdjustTemperatureRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaAdjustTemperatureRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaAdjustTemperatureRequestPayload
    {
        #region Properties

        [DataMember(Name = "targetSetpointDelta")]
        public AlexaTemperature targetSetpointDelta { get; set; }

        #endregion Properties
    }

    #endregion AdjustTemperature Data Contracts

    public class AlexaAdjustTargetTemperatureController : AlexaControllerBase<
        AlexaAdjustTemperatureRequestPayload,
        ControlResponse,
        AlexaAdjustTargetTemperatureControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaHVAC PropertyHelpers;
        private const string Namespace = "Alexa.ThermostatController";
        private readonly string[] _alexaProperties = { "targetSetpoint" };
        private readonly string[] _directiveNames = { "AdjustTargetTemperature" };
        private readonly string[] _premiseProperties = { "Temperature" };

        #endregion Fields

        #region Constructors

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
            return _alexaProperties;
        }

        public string GetAssemblyTypeName()
        {
            return GetType().AssemblyQualifiedName;
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

        public AlexaProperty GetPropertyState()
        {
            return null;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            return null;
        }

        public bool HasAlexaProperty(string property)
        {
            return (_alexaProperties.Contains(property));
        }

        public bool HasPremiseProperty(string property)
        {
            return _premiseProperties.Contains(property);
        }

        public void ProcessControllerDirective()
        {
            Response.Event.header.@namespace = "Alexa";

            try
            {
                Temperature target = new Temperature(Endpoint.GetValue<double>("CurrentSetPoint").GetAwaiter().GetResult());

                switch (Payload.targetSetpointDelta.scale)
                {
                    case "FAHRENHEIT":
                        target.Fahrenheit += Payload.targetSetpointDelta.value;
                        break;

                    case "CELCIUS":
                        target.Celcius += Payload.targetSetpointDelta.value;
                        break;

                    case "KELVIN":
                        target.Kelvin += Payload.targetSetpointDelta.value;
                        break;
                }

                Endpoint.SetValue("CurrentSetPoint", Math.Round(target.Kelvin, 1).ToString()).GetAwaiter().GetResult();
                Response.Event.header.name = "Response";
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, Namespace));
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }

        #endregion Methods
    }
}