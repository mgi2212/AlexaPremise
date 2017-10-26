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
    #region TemperatureSensor Data Contracts

    [DataContract]
    public class AlexaTemperatureSensorDirective
    {
        #region Constructors

        public AlexaTemperatureSensorDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaTemperatureSensorRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaTemperatureSensorRequestPayload payload { get; set; }

        #endregion Properties
    }

    public class AlexaTemperatureSensorRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaTemperatureSensorDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaTemperatureSensorRequestPayload : object
    {
    }

    #endregion TemperatureSensor Data Contracts

    public class AlexaTemperatureSensor : AlexaControllerBase<
        AlexaTemperatureSensorRequestPayload,
        ControlResponse,
        AlexaTemperatureSensorRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaHVAC PropertyHelpers;
        private const string Namespace = "Alexa.TemperatureSensor";
        private readonly string[] _alexaProperties = { "temperature" };
        private readonly string[] _directiveNames = { "ReportState" };
        private readonly string[] _premiseProperties = { "Temperature" };

        #endregion Fields

        #region Constructors

        public AlexaTemperatureSensor(AlexaTemperatureSensorRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public AlexaTemperatureSensor(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public AlexaTemperatureSensor()
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
            double temperature = Endpoint.GetValue<double>(_premiseProperties[0]).GetAwaiter().GetResult();
            Temperature temp = new Temperature(temperature);
            AlexaProperty property = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[0],
                value = new AlexaTemperature(Math.Round(temp.Fahrenheit, 1), "FAHRENHEIT"),
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

        public void ProcessControllerDirective()
        {
        }

        #endregion Methods
    }
}