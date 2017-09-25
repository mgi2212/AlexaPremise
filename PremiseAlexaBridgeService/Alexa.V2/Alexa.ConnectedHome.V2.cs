using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alexa.SmartHome.V2
{

    #region Header

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


    public static class Faults
    {
        public const string Namespace = "Alexa.ConnectedHome.Control";
        public const string QueryNamespace = "Alexa.ConnectedHome.Query";
        // User Faults:
        // These errors occur when the request is invalid due to customer error. For example the customer asks to set a
        // thermostat to 1000 degrees.
        public const string ValueOutOfRangeError = "ValueOutOfRangeError";
        public const string TargetOfflineError = "TargetOfflineError";
        public const string NoSuchTargetError = "NoSuchTargetError";
        public const string BridgeOfflineError = "BridgeOfflineError";
        // Skill Adapter Faults:
        // These errors occur when the request is valid but the skill adapter cannot complete the required task because
        // of a hardware issue or limitation.
        public const string DriverInternalError = "DriverInternalError";
        public const string DependentServiceUnavailableError = "DependentServiceUnavailableError";
        public const string TargetConnectivityUnstableError = "TargetConnectivityUnstableError";
        public const string TargetBridgeConnectivityUnstableError = "TargetBridgeConnectivityUnstableError";
        public const string TargetFirmwareOutdatedError = "TargetFirmwareOutdatedError";
        public const string TargetBridgeFirmwareOutdatedError = "TargetBridgeFirmwareOutdatedError";
        public const string TargetHardwareMalfunctionError = "TargetHardwareMalfunctionError";
        public const string TargetBridgeHardwareMalfunctionError = "TargetBridgeHardwareMalfunctionError";
        public const string UnwillingToSetValueError = "UnwillingToSetValueError";
        public const string RateLimitExceededError = "RateLimitExceededError";
        public const string NotSupportedInCurrentModeError = "NotSupportedInCurrentModeError";
        // Other Faults: 
        // These errors occur when the request cannot be fulfilled due to content in the request; either the authentication token 
        // is not valid, or some other aspect of the request cannot be fulfilled by the skill adapter.
        public const string ExpiredAccessTokenError = "ExpiredAccessTokenError";
        public const string InvalidAccessTokenError = "InvalidAccessTokenError";
        public const string UnsupportedTargetError = "UnsupportedTargetError";
        public const string UnsupportedOperationError = "UnsupportedOperationError";
        public const string UnsupportedTargetSettingError = "UnsupportedTargetSettingError";
        public const string UnexpectedInformationReceivedError = "UnexpectedInformationReceivedError";
    }

    public class ErrorInfo
    {
        [DataMember(Name = "code", EmitDefaultValue = false, Order = 1)]
        public string code { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = false, Order = 2)]
        public string description { get; set; }
    }

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.System")]
    public class ExceptionResponsePayload
    {
        [DataMember(Name = "errorInfo", EmitDefaultValue = false, Order = 1)]
        public ErrorInfo errorInfo;
        [DataMember(Name = "minimumValue", EmitDefaultValue = false, Order = 3)]
        public double minimumValue { get; set; }
        [DataMember(Name = "maximumValue", EmitDefaultValue = false, Order = 4)]
        public double maximumValue { get; set; }
        [DataMember(Name = "dependentServiceName", EmitDefaultValue = false, Order = 5)]
        public string dependentServiceName { get; set; }
        [DataMember(Name = "minimumFirmwareVersion", EmitDefaultValue = false, Order = 6)]
        public string minimumFirmwareVersion { get; set; }
        [DataMember(Name = "currentFirmwareVersion", EmitDefaultValue = false, Order = 7)]
        public string currentFirmwareVersion { get; set; }
        [DataMember(Name = "rateLimit", EmitDefaultValue = false, Order = 7)]
        public int rateLimit { get; set; }  //An integer that represents the maximum number of requests a device will accept in the specifed time unit.
        [DataMember(Name = "timeUnit", EmitDefaultValue = false, Order = 7)]
        public string timeUnit { get; set; } //An all-caps string that indicates the time unit for rateLimit such as MINUTE, HOUR or DAY.
        [DataMember(Name = "currentDeviceMode", EmitDefaultValue = false, Order = 7)]
        public string currentDeviceMode { get; set; } // A string that represents the current mode of the device.Valid values are AUTO, COOL, HEAT, and OTHER.
        [DataMember(Name = "faultingParameter", EmitDefaultValue = false, Order = 7)]
        public string faultingParameter { get; set; } // The property or field in the request message that was malformed or unexpected, and could not be handled by the skill adapter.
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

    [DataContract(Namespace = "Alexa.ConnectedHome.System")]
    public class SystemResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload", EmitDefaultValue = false, IsRequired = true)]
        public SystemResponsePayload payload;

    }

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.System")]
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

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.System")]
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

        public DiscoveryResponse ()
        {
            header = new Header
            {
                messageId = "0" // default
            };
            payload = new DiscoveryResponsePayload();
        }
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

    public enum QueryRequestType
    {
        Unknown,
        GetTemperatureReading,
        GetTargetTemperature,
        GetSpaceMode,
        GetHouseStatus,
        PowerState,
        DimmerLevel,
        Color,
        ColorTemperature,
        RetrieveCameraStreamUri
    }


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
        DecrementPercentage,
        SetColorRequest,
        SetColorTemperatureRequest,
        IncrementColorTemperature,
        DecrementColorTemperature
    }

    public enum DeviceType
    {
        Unknown,
        OnOff,
        Dimmer,
        Thermostat,
        ColorLight,
        Space,
        Camera,
        Status
    }

    public class ApplianceValue
    {
        public string value;
    }

    public class ApplianceIntegerValue
    {
        public int value;
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

        public ControlResponse()
        {
            header = new Header
            {
                messageId = "0" // default
            };
            payload = new ApplianceControlResponsePayload();
        }

    }

    #endregion

    #region Query

    [DataContract(Namespace = "Alexa.ConnectedHome.Query")]
    public class QueryRequest
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceQueryRequestPayload payload;
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Query")]
    public class QueryResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceQueryResponsePayload payload;

        public QueryResponse()
        {
            header = new Header
            {
                messageId = "0" // default
            };
            payload = new ApplianceQueryResponsePayload();
        }
    }
    
    #endregion

}