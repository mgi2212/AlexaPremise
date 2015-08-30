using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PremiseAlexaBridgeService
{

    [DataContract(Namespace = "")]
    public class Header
    {
        [DataMember(Name = "namespace", Order = 1)]
        public string @namespace { get; set; }
        [DataMember(Name = "name", Order = 2)]
        public string name { get; set; }
        [DataMember(Name = "payloadVersion", Order = 3)]
        public string payloadVersion { get; set; }

        public Header()
        {
            payloadVersion = "1";
        }
    }

    [DataContract(Namespace = "")]
    public class DiscoveryRequestPayload
    {
        [DataMember(Name = "accessToken")]
        public string accessToken { get; set; }
    }

    [DataContract(Namespace = "")]
    public class HealthCheckRequestPayload
    {
        [DataMember(Name = "initiationTimeStamp")]
        public int initiationTimeStamp { get; set; }
    }


    [DataContract(Namespace = "")]
    public class HealthCheckResponsePayload
    {
        [DataMember(Name = "isHealthy", Order = 1)]
        public bool isHealthy{ get; set; }
        [DataMember(Name = "description", Order = 2)]
        public string description { get; set; }
    }


    [DataContract(Namespace = "")]
    public class DiscoveryResponsePayload
    {
        [DataMember(Name = "discoveredAppliances", EmitDefaultValue = false)]
        public List<Appliance> discoveredAppliances { get; set; }
        [DataMember(Name = "exception", EmitDefaultValue = false)]
        public ExceptionResponsePayload exception { get; set; }
    }


    [DataContract(Namespace = "")]
    public class ExceptionResponsePayload
    {
        [DataMember(Name = "code")]
        public string code { get; set; }
        [DataMember(Name = "description")]
        public string description { get; set; }
    }

    [DataContract(Namespace = "")]
    public class HealthCheckRequest
    {
        [DataMember(Name = "header", Order = 1)]
        public Header header;

        [DataMember(Name = "payload", Order = 2)]
        public HealthCheckRequestPayload payload;
    }

    [DataContract(Namespace = "")]
    public class HealthCheckResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public HealthCheckResponsePayload payload;
    }


    [DataContract(Namespace = "")]
    public class DiscoveryRequest
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public DiscoveryRequestPayload payload;
    }

    [DataContract(Namespace = "")]
    public class DiscoveryResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public DiscoveryResponsePayload payload;
    }

    [DataContract(Namespace = "")]
    public class ControlRequest
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceControlRequestPayload payload;
    }

    [DataContract(Namespace = "")]
    public class ControlResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceControlResponsePayload payload;
    }

}