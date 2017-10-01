using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alexa.Premise.Custom
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
        public const string DriverpublicError = "DriverpublicError";
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

    #region Custom Skill

    [DataContract(Name = "roomStatus", Namespace = "Alexa.Premise.Custom")]
    public class RoomStatus
    {
        [DataMember(Name = "friendlyName", EmitDefaultValue = false)]
        public string friendlyName;

        [DataMember(Name = "occupied", EmitDefaultValue = false)]
        public string occupied;

        [DataMember(Name = "occupancyCount", EmitDefaultValue = false)]
        public string occupancyCount;

        [DataMember(Name = "lastOccupied", EmitDefaultValue = false)]
        public string lastOccupied;

        [DataMember(Name = "currentScene", EmitDefaultValue = false)]
        public string currentScene;

        [DataMember(Name = "deviceCount", EmitDefaultValue = false)]
        public string deviceCount;

        [DataMember(Name = "currentTemperature", EmitDefaultValue = false)]
        public string currentTemperature;

        [DataMember(Name = "lightsOnCount", EmitDefaultValue = false)]
        public string lightsOnCount;

    }

    [DataContract(Name = "spacesStatus", Namespace = "Alexa.Premise.Custom")]
    public class SpacesOperationStatus
    {
        [DataMember(Name = "friendlyResponse", EmitDefaultValue = false)]
        public string friendlyResponse;

        [DataMember(Name = "count", EmitDefaultValue = false)]
        public string count;

        [DataMember(Name = "assignedSpacesCount", EmitDefaultValue = false)]
        public string assignedSpacesCount;
    }


    [DataContract(Namespace = "Alexa.Premise.Custom")]
    public class CustomRequest
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public CustomRequestPayload payload;
    }

    [DataContract(Namespace = "Alexa.Premise.Custom")]
    public class CustomResponse
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "payload")]
        public CustomResponsePayload payload;

        public CustomResponse()
        {
            header = new Header
            {
                messageId = "0" // default
            };
            payload = new CustomResponsePayload();
        }
    }

    [DataContract(Namespace = "Alexa.Premise.Custom")]
    public class DevicePayload
    {
        [DataMember(Name = "type", Order = 1)]
        public string type { get; set; }

        [DataMember(Name = "operation", Order = 2)]
        public string operation { get; set; }

        [DataMember(Name = "name", Order = 3)]
        public string name { get; set; }

    }

    [DataContract(Namespace = "Alexa.Premise.Custom")]
    public class SpacePayload
    {
        [DataMember(Name = "name", Order = 1)]
        public string name { get; set; }

        [DataMember(Name = "deviceId", EmitDefaultValue = false, Order = 1)]
        public string deviceId { get; set; }

        [DataMember(Name = "userId", EmitDefaultValue = false, Order = 1)]
        public string userId { get; set; }

    }

    [DataContract(Name = "payload", Namespace = "Alexa.Premise.Custom")]
    public class CustomRequestPayload
    {
        [DataMember(Name = "accessToken", Order = 1)]
        public string accessToken { get; set; }

        [DataMember(Name = "space", EmitDefaultValue = false)]
        public SpacePayload space;

        [DataMember(Name = "device", EmitDefaultValue = false)]
        public DevicePayload device;

    }

    [DataContract(Name = "payload", Namespace = "Alexa.Premise.Custom")]
    public class CustomResponsePayload
    {

        [DataMember(Name = "roomStatus", EmitDefaultValue = false)]
        public RoomStatus applianceRoomStatus;

        [DataMember(Name = "spacesStatus", EmitDefaultValue = false)]
        public SpacesOperationStatus spacesStatus;

        [DataMember(Name = "exception", EmitDefaultValue = false, Order = 8)]
        public ExceptionResponsePayload exception { get; set; }

    }


    #endregion

}