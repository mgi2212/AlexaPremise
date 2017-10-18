using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using SYSWebSockClient;
using System.Threading.Tasks;

namespace Alexa.Discovery
{
    #region Discovery Data Contracts

    #region Response 

    [DataContract]
    public class DiscoveryControllerResponse
    {
        [DataMember(Name = "event", Order = 2)]
        public DiscoveryControllerResponseEvent Event { get; set; }

        public DiscoveryControllerResponse()
        {
        }

        public DiscoveryControllerResponse(Header header)
        {
            Event = new DiscoveryControllerResponseEvent()
            {
                header = header,
                payload = new AlexaDiscoveryResponsePayload(),
            };
        }
    }

    [DataContract]
    public class DiscoveryControllerResponseEvent
    {
        [DataMember(Name = "header", Order = 1)]
        public Header header { get; set; }
        [DataMember(Name = "payload", EmitDefaultValue = false, Order = 3)]
        public AlexaDiscoveryResponsePayload payload { get; set; }

        public DiscoveryControllerResponseEvent()
        {
            payload = new AlexaDiscoveryResponsePayload();

        }
    }

    [DataContract]
    public class AlexaDiscoveryResponsePayload : AlexaResponsePayload
    {
        [DataMember(Name = "endpoints", EmitDefaultValue = false, IsRequired = false)]
        public List<DiscoveryEndpoint> endpoints { get; set; }

        public AlexaDiscoveryResponsePayload()
        {
            endpoints = new List<DiscoveryEndpoint>();
        }
    }

    #endregion

    #region Request

    [DataContract]
    public class AlexaDiscoveryControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaDiscoveryControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaDiscoveryControllerRequestDirective
    {
        [DataMember(Name = "header", IsRequired = true, Order = 1)]
        public Header header { get; set; }
        [DataMember(Name = "payload", IsRequired = true, Order = 3)]
        public DiscoveryControllerRequestDirectivePayload payload { get; set; }

        public AlexaDiscoveryControllerRequestDirective()
        {
            header = new Header();
            payload = new DiscoveryControllerRequestDirectivePayload();
        }
    }

    [DataContract]
    public class DiscoveryControllerRequestDirectivePayload
    {
        [DataMember(Name = "scope", EmitDefaultValue = false)]
        public Scope scope { get; set; }

        public DiscoveryControllerRequestDirectivePayload()
        {
            scope = new Scope();
        }
    }

    #endregion

    #endregion

    public class AlexaDiscoveryController : AlexaControllerBase<
        DiscoveryControllerRequestDirectivePayload, 
        DiscoveryControllerResponse, 
        AlexaDiscoveryControllerRequest>, IAlexaController
    {
        public readonly string @namespace = "Alexa.Discovery";
        public readonly string[] directiveNames = { "Discover" };
       private readonly string[] alexaProperties = { "Discover.Response" };
        public readonly string[] premiseProperties = { "none" };

        public AlexaDiscoveryController(AlexaDiscoveryControllerRequest request)
            : base(request)
        {
        }

        public AlexaDiscoveryController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }
        public string GetAssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public AlexaDiscoveryController()
            : base()
        {
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public string[] GetDirectiveNames()
        {
            return directiveNames;
        }

        public AlexaProperty GetPropertyState()
        {
            return null;
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

        public void ProcessControllerDirective()
        {
            try
            {
                response.Event.payload.endpoints = PremiseServer.GetEndpoints().GetAwaiter().GetResult();
                if (PremiseServer.IsAsyncEventsEnabled)
                {
                    Task t = Task.Run(() =>
                    {
                        PremiseServer.Resubscribe();
                    });
                }
            }
            catch 
            {
                response.Event.payload.endpoints.Clear();
            }

            this.Response.Event.header.name = alexaProperties[0];
        }
    }
}
