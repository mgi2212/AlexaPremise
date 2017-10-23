using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using System.Linq;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.EndpointHealth
{
    #region Scene Data Contracts

    [DataContract]
    public class AlexaEndpointHealthControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaEndpointHealthControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaEndpointHealthControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaEndpointHealthRequestPayload payload { get; set; }

        public AlexaEndpointHealthControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaEndpointHealthRequestPayload();
        }
    }

    [DataContract]
    public class AlexaEndpointHealthRequestPayload : object
    {

    }

    [DataContract]
    public class AlexaEndpointHealthValue
    {
        [DataMember(Name = "value", IsRequired = true, Order = 1)]
        public string value { get; set; }

        public AlexaEndpointHealthValue()
        {

        }

        public AlexaEndpointHealthValue(string stateValue)
        {
            //this.lastcontact = timestamp;
            this.value = stateValue;
        }
    }

    #endregion

    /// <summary>
    /// there is no web service endpoint associated with class its pupose 
    /// is to be accessed internally by other controller requests, 
    /// state report requests and change reports
    /// </summary>
    public class AlexaEndpointHealthController : AlexaControllerBase<
        AlexaEndpointHealthRequestPayload,
        ControlResponse,
        AlexaEndpointHealthControllerRequest>, IAlexaController
    {
        public readonly AlexaEndpointHealth PropertyHelpers = new AlexaEndpointHealth();
        public readonly string @namespace = "Alexa.EndpointHealth";
        public readonly string[] directiveNames = { };
        public readonly string[] premiseProperties = { "IsReachable" };
        private readonly string[] alexaProperties = { "connectivity" };

        public AlexaEndpointHealthController(AlexaEndpointHealthControllerRequest request)
            : base(request)
        {
        }

        public AlexaEndpointHealthController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public AlexaEndpointHealthController()
            : base()
        {
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
            bool isReachable = endpoint.GetValue<bool>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                timeOfSample = GetUtcTime(),
                value = new AlexaEndpointHealthValue((isReachable == true ? "OK" : "UNREACHABLE")),
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
        }
    }
}