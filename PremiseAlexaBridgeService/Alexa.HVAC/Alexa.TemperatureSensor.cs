using Alexa.Controller;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Linq;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.HVAC
{
    #region Adjust ColorTemperature Data Contracts

    public class AlexaTemperatureSensorRequest
    {
        [DataMember(Name = "directive")]
        public AlexaTemperatureSensorDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaTemperatureSensorDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaTemperatureSensorRequestPayload payload { get; set; }

        public AlexaTemperatureSensorDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaTemperatureSensorRequestPayload();
        }
    }

    [DataContract]
    public class AlexaTemperatureSensorRequestPayload : object
    {
    }

    [DataContract]
    public class AlexaTemperatureSensorResponsePayload 
    {
        [DataMember(Name = "value")]
        public double value;

        [DataMember(Name = "scale")]
        public string scale;

        public AlexaTemperatureSensorResponsePayload(double temperature, string scaleString)
        {
            value = temperature;
            scale = scaleString;
        }
    }

    #endregion 

    public class AlexaTemperatureSensor : AlexaControllerBase<
        AlexaTemperatureSensorRequestPayload,
        ControlResponse,
        AlexaTemperatureSensorRequest>, IAlexaController
    {
        private readonly string @namespace = "Alexa.TemperatureSensor";
        private readonly string[] directiveNames = { "ReportState"};
        private readonly string[] premiseProperties = { "Temperature" };
       private readonly string[] alexaProperties = { "temperature" };
        public readonly AlexaHVAC PropertyHelpers;

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
            : base()
        {
            PropertyHelpers = new AlexaHVAC();
        }

        public string GetNameSpace()
        {
            return @namespace;
        }
        
        public string GetAssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }
        
        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public string [] GetDirectiveNames()
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
            double temperature = this.endpoint.GetValue<double>(premiseProperties[0]).GetAwaiter().GetResult();
            Temperature temp = new Temperature();
            temp.Kelvin = temperature;

            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                value = new AlexaTemperatureSensorResponsePayload(Math.Round(temp.Fahrenheit,1), "FAHRENHEIT"),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
        }
    }
}
