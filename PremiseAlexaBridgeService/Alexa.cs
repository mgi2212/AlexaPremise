using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PremiseAlexaBridgeService
{

    #region Header

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

    #endregion

    #region Exception

    [DataContract(Name = "payload", Namespace = "z")]
    public class ExceptionResponsePayload
    {
        [DataMember(Name = "code")]
        public string code { get; set; }
        [DataMember(Name = "description")]
        public string description { get; set; }
    }

    #endregion

    #region System

    public enum SystemRequestType
    {
        Unknown,
        HealthCheck,
    }

    [DataContract(Namespace = "System")]
    public class SystemRequest
    {
        [DataMember(Name = "header", Order = 1)]
        public Header header;

        [DataMember(Name = "payload", EmitDefaultValue = false, Order = 2)]
        public HealthCheckRequestPayload payload;
    }

    [DataContract(Namespace = "System")]
    public class SystemResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload", EmitDefaultValue = false, IsRequired = true)]
        public SystemResponsePayload payload;

    }

    [DataContract(Name = "payload", Namespace = "System")]
    public class SystemResponsePayload
    {
        [DataMember(Name = "isHealthy", EmitDefaultValue = false, Order = 1)]
        public bool isHealthy { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = false, Order = 2)]
        public string description { get; set; }
        [DataMember(Name = "exception", EmitDefaultValue = false)]
        public ExceptionResponsePayload exception { get; set; }

    }

    #region HealthCheck 

    [DataContract(Name = "payload", Namespace = "System")]
    public class HealthCheckRequestPayload
    {
        [DataMember(Name = "accessToken", EmitDefaultValue = false, Order = 1)]
        public string accessToken { get; set; }
        [DataMember(Name = "initiationTimeStamp", Order = 2)]
        public int initiationTimeStamp { get; set; }
    }

    #endregion

    #endregion

    #region Discovery

    [DataContract(Namespace = "Discovery")]
    public class DiscoveryRequestPayload
    {
        [DataMember(Name = "accessToken")]
        public string accessToken { get; set; }
    }

    [DataContract(Namespace = "Discovery")]
    public class DiscoveryRequest
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public DiscoveryRequestPayload payload;
    }

    [DataContract(Namespace = "Discovery")]
    public class DiscoveryResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public DiscoveryResponsePayload payload;
    }

    [DataContract(Namespace = "Discovery")]
    public class DiscoveryResponsePayload
    {
        [DataMember(Name = "discoveredAppliances", EmitDefaultValue = false)]
        public List<Appliance> discoveredAppliances { get; set; }

        [DataMember(Name = "exception", EmitDefaultValue = false)]
        public ExceptionResponsePayload exception { get; set; }
    }

    #endregion

    #region Control

    public enum ControlRequestType
    {
        Unknown,
        SwitchOnOff,
        AdjustNumericalSetting
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

    #endregion

}