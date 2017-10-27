using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alexa.SmartHomeAPI.V2
{
    #region Header

    public class Header
    {
        #region Constructors

        public Header()
        {
            payloadVersion = "2";
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "namespace", IsRequired = true, Order = 2)]
        public string @namespace { get; set; }

        [DataMember(Name = "messageId", IsRequired = true, Order = 1)]
        public string messageId { get; set; }

        [DataMember(Name = "name", IsRequired = true, Order = 3)]
        public string name { get; set; }

        [DataMember(Name = "payloadVersion", IsRequired = true, Order = 4)]
        public string payloadVersion { get; set; }

        #endregion Properties
    }

    #endregion Header

    #region Exception

    public static class Faults
    {
        #region Fields

        public const string BridgeOfflineError = "BridgeOfflineError";
        public const string DependentServiceUnavailableError = "DependentServiceUnavailableError";

        // Skill Adapter Faults: These errors occur when the request is valid but the skill adapter
        // cannot complete the required task because of a hardware issue or limitation.
        public const string DriverpublicError = "DriverpublicError";

        // Other Faults: These errors occur when the request cannot be fulfilled due to content in
        // the request; either the authentication token is not valid, or some other aspect of the
        // request cannot be fulfilled by the skill adapter.
        public const string ExpiredAccessTokenError = "ExpiredAccessTokenError";

        public const string InvalidAccessTokenError = "InvalidAccessTokenError";
        public const string Namespace = "Alexa.ConnectedHome.Control";
        public const string NoSuchTargetError = "NoSuchTargetError";
        public const string NotSupportedInCurrentModeError = "NotSupportedInCurrentModeError";
        public const string QueryNamespace = "Alexa.ConnectedHome.Query";
        public const string RateLimitExceededError = "RateLimitExceededError";

        public const string TargetBridgeConnectivityUnstableError = "TargetBridgeConnectivityUnstableError";

        public const string TargetBridgeFirmwareOutdatedError = "TargetBridgeFirmwareOutdatedError";

        public const string TargetBridgeHardwareMalfunctionError = "TargetBridgeHardwareMalfunctionError";

        public const string TargetConnectivityUnstableError = "TargetConnectivityUnstableError";

        public const string TargetFirmwareOutdatedError = "TargetFirmwareOutdatedError";

        public const string TargetHardwareMalfunctionError = "TargetHardwareMalfunctionError";

        public const string TargetOfflineError = "TargetOfflineError";

        public const string UnexpectedInformationReceivedError = "UnexpectedInformationReceivedError";

        public const string UnsupportedOperationError = "UnsupportedOperationError";

        public const string UnsupportedTargetError = "UnsupportedTargetError";

        public const string UnsupportedTargetSettingError = "UnsupportedTargetSettingError";

        public const string UnwillingToSetValueError = "UnwillingToSetValueError";

        // User Faults: These errors occur when the request is invalid due to customer error. For
        // example the customer asks to set a thermostat to 1000 degrees.
        public const string ValueOutOfRangeError = "ValueOutOfRangeError";

        #endregion Fields
    }

    public class ErrorInfo
    {
        #region Properties

        [DataMember(Name = "code", EmitDefaultValue = false, Order = 1)]
        public string code { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false, Order = 2)]
        public string description { get; set; }

        #endregion Properties
    }

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.System")]
    public class ExceptionResponsePayload
    {
        #region Fields

        [DataMember(Name = "errorInfo", EmitDefaultValue = false, Order = 1)]
        public ErrorInfo errorInfo;

        #endregion Fields

        #region Properties

        [DataMember(Name = "currentDeviceMode", EmitDefaultValue = false, Order = 7)]
        public string currentDeviceMode { get; set; }

        [DataMember(Name = "currentFirmwareVersion", EmitDefaultValue = false, Order = 7)]
        public string currentFirmwareVersion { get; set; }

        [DataMember(Name = "dependentServiceName", EmitDefaultValue = false, Order = 5)]
        public string dependentServiceName { get; set; }

        // A string that represents the current mode of the device.Valid values are AUTO, COOL, HEAT,
        // and OTHER.
        [DataMember(Name = "faultingParameter", EmitDefaultValue = false, Order = 7)]
        public string faultingParameter { get; set; }

        [DataMember(Name = "maximumValue", EmitDefaultValue = false, Order = 4)]
        public double maximumValue { get; set; }

        [DataMember(Name = "minimumFirmwareVersion", EmitDefaultValue = false, Order = 6)]
        public string minimumFirmwareVersion { get; set; }

        [DataMember(Name = "minimumValue", EmitDefaultValue = false, Order = 3)]
        public double minimumValue { get; set; }

        [DataMember(Name = "rateLimit", EmitDefaultValue = false, Order = 7)]
        public int rateLimit { get; set; }  //An integer that represents the maximum number of requests a device will accept in the specifed time unit.

        [DataMember(Name = "timeUnit", EmitDefaultValue = false, Order = 7)]
        public string timeUnit { get; set; }

        #endregion Properties

        //An all-caps string that indicates the time unit for rateLimit such as MINUTE, HOUR or DAY.
        // The property or field in the request message that was malformed or unexpected, and could not be handled by the skill adapter.
    }

    #endregion Exception

    #region System

    public enum SystemRequestType
    {
        Unknown,
        HealthCheck
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.System")]
    public class SystemRequest
    {
        #region Fields

        [DataMember(Name = "header", Order = 1)]
        public Header header;

        [DataMember(Name = "payload", EmitDefaultValue = false, Order = 2)]
        public HealthCheckRequestPayload payload;

        #endregion Fields
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.System")]
    public class SystemResponse
    {
        #region Fields

        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload", EmitDefaultValue = false, IsRequired = true)]
        public SystemResponsePayload payload;

        #endregion Fields
    }

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.System")]
    public class SystemResponsePayload
    {
        #region Properties

        [DataMember(Name = "description", EmitDefaultValue = false, Order = 2)]
        public string description { get; set; }

        [DataMember(Name = "exception", EmitDefaultValue = false)]
        public ExceptionResponsePayload exception { get; set; }

        [DataMember(Name = "isHealthy", EmitDefaultValue = false, Order = 1)]
        public bool isHealthy { get; set; }

        #endregion Properties
    }

    #region HealthCheck

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.System")]
    public class HealthCheckRequestPayload
    {
        #region Properties

        [DataMember(Name = "accessToken", EmitDefaultValue = false, Order = 1)]
        public string accessToken { get; set; }

        [DataMember(Name = "initiationTimeStamp", Order = 2)]
        public int initiationTimeStamp { get; set; }

        #endregion Properties
    }

    #endregion HealthCheck

    #endregion System

    #region Discovery

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class DiscoveryRequest
    {
        #region Fields

        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public DiscoveryRequestPayload payload;

        #endregion Fields
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class DiscoveryRequestPayload
    {
        #region Properties

        [DataMember(Name = "accessToken")]
        public string accessToken { get; set; }

        #endregion Properties
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class DiscoveryResponse
    {
        #region Fields

        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public DiscoveryResponsePayload payload;

        #endregion Fields

        #region Constructors

        public DiscoveryResponse()
        {
            header = new Header
            {
                messageId = "0" // default
            };
            payload = new DiscoveryResponsePayload();
        }

        #endregion Constructors
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class DiscoveryResponsePayload
    {
        #region Properties

        [DataMember(Name = "discoveredAppliances", EmitDefaultValue = false)]
        public List<Appliance> discoveredAppliances { get; set; }

        [DataMember(Name = "exception", EmitDefaultValue = false)]
        public ExceptionResponsePayload exception { get; set; }

        #endregion Properties
    }

    #endregion Discovery

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

    public class ApplianceIntegerValue
    {
        #region Fields

        public int value;

        #endregion Fields
    }

    public class ApplianceValue
    {
        #region Fields

        public string value;

        #endregion Fields
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Control")]
    public class ControlRequest
    {
        #region Fields

        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceControlRequestPayload payload;

        #endregion Fields
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Control")]
    public class ControlResponse
    {
        #region Fields

        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceControlResponsePayload payload;

        #endregion Fields

        #region Constructors

        public ControlResponse()
        {
            header = new Header
            {
                messageId = "0" // default
            };
            payload = new ApplianceControlResponsePayload();
        }

        #endregion Constructors
    }

    #endregion Control

    #region Query

    [DataContract(Namespace = "Alexa.ConnectedHome.Query")]
    public class QueryRequest
    {
        #region Fields

        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceQueryRequestPayload payload;

        #endregion Fields
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Query")]
    public class QueryResponse
    {
        #region Fields

        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public ApplianceQueryResponsePayload payload;

        #endregion Fields

        #region Constructors

        public QueryResponse()
        {
            header = new Header
            {
                messageId = "0" // default
            };
            payload = new ApplianceQueryResponsePayload();
        }

        #endregion Constructors
    }

    #endregion Query
}