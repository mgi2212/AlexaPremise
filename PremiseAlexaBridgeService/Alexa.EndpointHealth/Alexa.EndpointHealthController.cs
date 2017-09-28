using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using System;
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
        [DataMember(Name = "state", IsRequired = true, Order = 1)]
        public string state { get; set; }
        [DataMember(Name = "lastcontact", IsRequired = true, Order = 2)]
        public string lastcontact { get; set; }

        public AlexaEndpointHealthValue(string stateValue, string timestamp)
        {
            this.lastcontact = timestamp;
            this.state = stateValue;
        }
    }

    #endregion

    public class AlexaEndpointHealthController : AlexaControllerBase<
        AlexaEndpointHealthRequestPayload, 
        ControlResponse, 
        AlexaEndpointHealthControllerRequest>, IAlexaController
    {
        public readonly AlexaEndpointHealth PropertyHelpers = new AlexaEndpointHealth();
        public readonly string @namespace = "Alexa.EndpointHealth";
        
        // no service endpoint this class is accessed in other controller requests, state report requests and change reports
        public readonly string[] directiveNames = { };
        public readonly string premiseProperty = "IsReachable";
        private readonly string alexaProperty = "connectivity";


        public AlexaEndpointHealthController(AlexaEndpointHealthControllerRequest request)
            : base(request)
        {
        }

        public AlexaEndpointHealthController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string[] GetDirectiveNames()
        {
            return directiveNames;
        }

        public AlexaProperty GetPropertyState()
        {
            bool isReachable = endpoint.GetValue<bool>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperty,
                value = new AlexaEndpointHealthValue((isReachable == true ? "REACHABLE" : "UNREACHABLE"), GetUtcTime()),
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
        }
    }
}