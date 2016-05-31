using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alexa.SmartHome
{

    #region Header

    [DataContract(Namespace = "Alexa.ConnectedHome")]
    public class Header
    {
        [DataMember(Name = "messageId", IsRequired = true, Order = 1)]
        public string messageId { get; set; }
        [DataMember(Name = "namespace", IsRequired = true, Order = 2)]
        public string @namespace { get; set; }
        [DataMember(Name = "name", IsRequired = true, Order = 3)]
        public string name { get; set; }
        [DataMember(Name = "payloadVersion", IsRequired = true, Order = 4)]
        public string payloadVersion { get; set; }

        public Header()
        {
            payloadVersion = "2";
        }
    }

    #endregion

    #region Exception

    [DataContract(Name = "payload")]
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

    [DataContract(Namespace = "Alexa.ConnectedHome.System")]
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

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class DiscoveryRequestPayload
    {
        [DataMember(Name = "accessToken")]
        public string accessToken { get; set; }
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class DiscoveryRequest
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public DiscoveryRequestPayload payload;
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class DiscoveryResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public DiscoveryResponsePayload payload;
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
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
        HealthCheck,
        TurnOnRequest,
        TurnOffRequest,
        SetTargetTemperature,
        IncrementTargetTemperature,
        DecrementTargetTemperature,
        SetPercentage,
        IncrementPercentage,
        DecrementPercentage
    }

    public enum DeviceType
    {
        Unknown,
        OnOff,
        Dimmer,
        Thermostat
    }

    public class ApplianceValue
    {
        public string value;
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Control")]
    public class ControlRequest
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceControlRequestPayload payload;
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Control")]
    public class ControlResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceControlResponsePayload payload;
    }

    #endregion

}