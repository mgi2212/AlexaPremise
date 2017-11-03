using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;
using PremiseAlexaBridgeService;

namespace Alexa.EndpointHealth
{
    #region Scene Data Contracts

    [DataContract]
    public class AlexaEndpointHealthControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaEndpointHealthControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaEndpointHealthControllerRequestDirective
    {
        #region Constructors

        public AlexaEndpointHealthControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaEndpointHealthRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaEndpointHealthRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaEndpointHealthRequestPayload : object
    {
    }

    [DataContract]
    public class AlexaEndpointHealthValue
    {
        #region Constructors

        public AlexaEndpointHealthValue()
        {
        }

        public AlexaEndpointHealthValue(string stateValue)
        {
            value = stateValue;
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "value", IsRequired = true, Order = 1)]
        public string value { get; set; }

        #endregion Properties
    }

    #endregion Scene Data Contracts

    /// <summary>
    /// there is no web service endpoint associated with class its purpose is to be accessed
    /// internally by other controller requests, state report requests and change reports
    /// </summary>
    public class AlexaEndpointHealthController : AlexaControllerBase<
        AlexaEndpointHealthRequestPayload,
        ControlResponse,
        AlexaEndpointHealthControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly string @namespace = "Alexa.EndpointHealth";
        public readonly string[] directiveNames = { };
        public readonly string[] premiseProperties = { "IsReachable" };
        public readonly AlexaEndpointHealth PropertyHelpers = new AlexaEndpointHealth();
        private readonly string[] alexaProperties = { "connectivity" };

        #endregion Fields

        #region Constructors

        public AlexaEndpointHealthController(AlexaEndpointHealthControllerRequest request)
            : base(request)
        {
        }

        public AlexaEndpointHealthController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public AlexaEndpointHealthController()
        {
        }

        #endregion Constructors

        #region Methods

        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public string GetAssemblyTypeName()
        {
            return PropertyHelpers.GetType().AssemblyQualifiedName;
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

        public List<AlexaProperty> GetPropertyStates()
        {
            List<AlexaProperty> properties = new List<AlexaProperty>();

            bool isReachable = Endpoint.GetValue<bool>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                timeOfSample = PremiseServer.GetUtcTime(),
                value = new AlexaEndpointHealthValue((isReachable ? "OK" : "UNREACHABLE"))
            };
            properties.Add(property);

            return properties;
        }

        public bool HasAlexaProperty(string property)
        {
            return alexaProperties.Contains(property);
        }

        public bool HasPremiseProperty(string property)
        {
            return premiseProperties.Contains(property);
        }

        public string MapPremisePropertyToAlexaProperty(string premiseProperty)
        {
            return premiseProperties.Contains(premiseProperty) ? "connectivity" : "";
        }

        public void ProcessControllerDirective()
        {
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
    }
}