using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Linq;
using Alexa.EndpointHealth;
using Alexa.Lighting;
using Alexa.HVAC;
/// <summary>
/// Generic Data Contracts for Alexa.SmartHome V3 API
/// </summary>
namespace Alexa.SmartHomeAPI.V3
{

    #region Directive

    [DataContract(Name = "directive")]
    public class AlexaDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public object payload { get; set; }

        public AlexaDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
        }
    }

    #endregion

    #region Header

    [DataContract(Name = "header")]
    public class Header
    {
        [DataMember(Name = "correlationToken", EmitDefaultValue = false, IsRequired = false, Order = 1)]
        public string correlationToken { get; set; }
        [DataMember(Name = "messageId", IsRequired = true, Order = 2)]
        public string messageID { get; set; }
        [DataMember(Name = "namespace", IsRequired = true, Order = 3)]
        public string @namespace { get; set; }
        [DataMember(Name = "name", IsRequired = true, Order = 4)]
        public string name { get; set; }
        [DataMember(Name = "payloadVersion", IsRequired = true, Order = 5)]
        public string payloadVersion { get; set; }

        public Header()
        {
            payloadVersion = "3";
        }
    }

    #endregion

    #region Endpoint
    [DataContract]
    public class DiscoveryEndpoint
    {
        [DataMember(Name = "endpointId", EmitDefaultValue = true, IsRequired = true, Order = 1)]
        public string endpointId { get; set; }
        [DataMember(Name = "manufacturerName", EmitDefaultValue = false, IsRequired = true, Order = 2)]
        public string manufacturerName { get; set; }
        [DataMember(Name = "friendlyName", EmitDefaultValue = false, IsRequired = true, Order = 3)]
        public string friendlyName { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = false, IsRequired = true, Order = 4)]
        public string description { get; set; }
        [DataMember(Name = "displayCategories", EmitDefaultValue = false, IsRequired = true, Order = 5)]
        public List<string> displayCategories { get; set; }
        [DataMember(Name = "cookie", EmitDefaultValue = false, IsRequired = false, Order = 6)]
        public EndpointCookie cookie { get; set; }
        [DataMember(Name = "capabilities", EmitDefaultValue = false, IsRequired = true, Order = 7)]
        public List<Capability> capabilities { get; set; }
        public DiscoveryEndpoint()
        {
            displayCategories = new List<string>();
            cookie = new EndpointCookie();
            capabilities = new List<Capability>();
        }
    }

    [DataContract]
    public class DirectiveEndpoint
    {
        [DataMember(Name = "scope", IsRequired = true, Order = 1)]
        public Scope scope { get; set; }
        [DataMember(Name = "endpointId", IsRequired = true, Order = 2)]
        public string endpointId { get; set; }
        [DataMember(Name = "cookie", EmitDefaultValue = false, IsRequired = false, Order = 3)]
        public EndpointCookie cookie { get; set; }
        public DirectiveEndpoint()
        {
            scope = new Scope();
            cookie = new EndpointCookie();
        }
    }

    public class EndpointCookie
    {
        [DataMember(Name = "path", EmitDefaultValue = false)]
        public string path;
    }
    #endregion

    #region Scope 

    [DataContract(Name = "scope")]
    public class Scope
    {
        [DataMember(Name = "type", EmitDefaultValue = true, Order = 1)]
        public string type { get; set; }
        [DataMember(Name = "token", EmitDefaultValue = true, Order = 2)]
        public string token { get; set; }
        [DataMember(Name = "localAccessToken", EmitDefaultValue = false, Order = 3)]
        public string localAccessToken { get; set; }

        void OnSerializing(StreamingContext context)
        {
            this.localAccessToken = default(string); // will not be serialized
        }
    }

    #endregion

    #region Property 

    [DataContract]
    public class SupportedProperty
    {
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string propertyName { get; set; }

        public SupportedProperty(string propName)
        {
            propertyName = propName;
        }
    }

    #endregion

    #region Context

    [DataContract]
    public class Context
    {
        internal Dictionary<string, AlexaProperty> propertiesInternal {get; set; }

        [DataMember(Name = "properties", EmitDefaultValue = false)]
        public List<AlexaProperty> properties
        {
            get
            {
                if (propertiesInternal != null)
                {
                    return propertiesInternal.Values.ToList();
                }
                return null;
            }
        }

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            if ((this.propertiesInternal == null) || (this.propertiesInternal.Count == 0))
            {
                this.propertiesInternal = default(Dictionary<string, AlexaProperty>); // will not be serialized
            }
        }

        public Context()
        {
            propertiesInternal = new Dictionary<string, AlexaProperty>();
        }
    }

    #endregion

    #region Capability

    [DataContract(Name = "properties")]
    public class Properties
    {
        [DataMember(Name = "supported", EmitDefaultValue = false, IsRequired = false, Order = 1)]
        public List<SupportedProperty> supported { get; set; }
        [DataMember(Name = "proactivelyReported", EmitDefaultValue = true, IsRequired = false, Order = 2)]
        public bool proactivelyReported { get; set; }
        [DataMember(Name = "retrievable",  EmitDefaultValue = true, IsRequired = false, Order = 3)]
        public bool retrievable { get; set; }
        //[DataMember(Name = "supportsDeactivation", EmitDefaultValue = false, IsRequired = false, Order = 4)]
        //public bool supportsDeactivation { get; set; }

        public Properties()
        {
            supported = new List<SupportedProperty>();
        }

        public Properties(bool asyncSupported, bool querySupported)
        {
             this.supported = new List<SupportedProperty>();
             this.proactivelyReported = asyncSupported;
             this.retrievable = querySupported;
        }

    }

    [DataContract(Name = "capability")]
    public class Capability
    {
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true, Order = 1)]
        public string type { get; set; }
        [DataMember(Name = "interface", IsRequired = true, EmitDefaultValue = true, Order = 2)]
        public string @interface { get; set; }
        [DataMember(Name = "version", IsRequired = true, EmitDefaultValue = true, Order = 3)]
        public string version { get; set; }
        [DataMember(Name = "supportsDeactivation", EmitDefaultValue = false, IsRequired = false, Order = 4)]
        public bool supportsDeactivation { get; set; }
        [DataMember(Name = "proactivelyReported", EmitDefaultValue = false, IsRequired = false, Order = 5)]
        public bool proactivelyReported { get; set; }
        [DataMember(Name = "properties", EmitDefaultValue = false, IsRequired = false, Order = 6)]
        public Properties properties { get; set; }
        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            if (this.HasProperties() == false)
            {
                this.properties = default(Properties); // will not be serialized
            }
        }

        public Capability(string interfaceName, bool asyncSupported, bool querySupported)
        {
            this.type = "AlexaInterface";
            this.version = "3.0";
            this.@interface = interfaceName;
            this.properties = new Properties(asyncSupported, querySupported);
        }

        public bool HasProperties()
        {
            return !((this.properties == null) || (this.properties.supported == null) || (this.properties.supported.Count == 0));
        }
    }


    #endregion

    #region Exceptions and Errors

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

    public enum AlexaErrorTypes
    {
        ENDPOINT_UNREACHABLE,               // Indicates a directive targeting an endpoint that is currently unreachable or offline. For example, the endpoint might be turned off, disconnected from the customer's local area network, or connectivity between the endpoint and bridge or the endpoint and the device cloud might have been lost.
        NO_SUCH_ENDPOINT,                   // Indicates an Endpoint that does not exist or no longer exists.
        INVALID_VALUE,                      // Indicates a directive that attempts to set an invalid value for that endpoint. For example, use to indicate a request with an invalid heating mode, channel value or program value.
        VALUE_OUT_OF_RANGE,                 // Indicates a directive that attempts to set a value that is outside the numerical range accepted for that endpoint. For example, use to respond to a request to set a percentage value over 100 percent. For temperature values, use TEMPERATURE_VALUE_OUT_OF_RANGE
        TEMPERATURE_VALUE_OUT_OF_RANGE,     // Indicates a directive that attempts to set a value that outside the numeric temperature range accepted for that thermostat. For more thermostat-specific errors, see the error section of the Alexa.ThermostatController interface. Note that the namespace for thermostat-specific errors is Alexa.ThermostatController
        INVALID_DIRECTIVE,                  // Indicates a directive that is invalid or malformed. For example, in the unlikely event an endpoint receives a request it does not support, you would return this error type.
        FIRMWARE_OUT_OF_DATE,               // Indicates a directive could not be handled because the firmware for a bridge or an endpoint is out of date.
        HARDWARE_MALFUNCTION,               // Indicates a directive could not be handled because a bridge or an endpoint has experienced a hardware malfunction.
        RATE_LIMIT_EXCEEDED,                // Indicates the maximum rate at which an endpoint or bridge can process directives has been exceeded.
        INVALID_AUTHORIZATION_CREDENTIAL,   // Indicates that the authorization credential provided by Alexa is invalid. For example, the OAuth2 access token is not valid for the customer's device cloud account.
        EXPIRED_AUTHORIZATION_CREDENTIAL,   // Indicates that the authorization credential provided by Alexa has expired. For example, the OAuth2 access token for that customer has expired.
        INTERNAL_ERROR                      // Indicates an error that cannot be accurately described as one of the other error types occurred while you were handling the directive. For example, a generic runtime exception occurred while handling a directive. Ideally, you will never send this error event, but instead send a more specific error type.
    }

    public class ErrorInfo
    {
        [DataMember(Name = "code", EmitDefaultValue = false, Order = 1)]
        public string code { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = false, Order = 2)]
        public string description { get; set; }
    }

    [DataContract]
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

    [DataContract(Namespace = "Alexa.ConnectedHome.System")]
    public class SystemRequest
    {
        [DataMember(Name = "header", Order = 1)]
        public Header header { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = false, Order = 2)]
        public HealthCheckRequestPayload payload { get; set; }
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.System")]
    public class SystemResponse
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = false, IsRequired = true)]
        public SystemResponsePayload payload { get; set; }

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

    #region Control
    
    [DataContract]
    public class ControlResponse
    {
        [DataMember(Name = "context", Order = 1, EmitDefaultValue = false)]
        public AlexaControlResponseContext context { get; set; }
        [DataMember(Name = "event", Order = 2)]
        public AlexaEventBody Event { get; set; }

        void OnSerializing(StreamingContext context)
        {
            Debug.WriteLine("Serializing ControlResponse");
            this.Event.header.@namespace = "Alexa";
        }

        public ControlResponse()
        {

        }

        public ControlResponse(object[] args)
        {
            context = new AlexaControlResponseContext();
            Event = new AlexaEventBody(args[0] as Header, args[1] as DirectiveEndpoint);
        }

        public ControlResponse(Header header, DirectiveEndpoint endpoint)
        {
            context = new AlexaControlResponseContext();
            Event = new AlexaEventBody(header, endpoint)
            {
                header = header
            };
        }
    }


    [DataContract] 
    [KnownType(typeof(AlexaEndpointHealthValue))]
    [KnownType(typeof(AlexaColorValue))] 
    [KnownType(typeof(AlexaTemperatureSensorResponsePayload))]
    [KnownType(typeof(AlexaSetThermostatModePayloadValue))] 
    [KnownType(typeof(AlexaSetTargetTemperatureRequestPayload))] 
    public class AlexaProperty
    {
        [DataMember(Name = "namespace")]
        public string @namespace { get; set; }
        [DataMember(Name = "name")]
        public string name { get; set; }
        [DataMember(Name = "value")]
        public object value {get; set; }
        [DataMember(Name = "timeOfSample")]
        public string timeOfSample { get; set; }
        [DataMember(Name = "uncertaintyInMilliseconds")]
        public int uncertaintyInMilliseconds { get; set; }

        void OnSerializing(StreamingContext context)
        {
            Debug.WriteLine("Serializing AlexaProperty");
        }

        public AlexaProperty()
        {
            uncertaintyInMilliseconds = 20;
        }

        public AlexaProperty(Header header)
        {
            @namespace = header.@namespace;
            uncertaintyInMilliseconds = 20;
        }

    }

    [DataContract]
    public class AlexaControlResponseContext
    {
        [DataMember(Name = "properties", EmitDefaultValue = false)]
        public List<AlexaProperty> properties { get; set; }
        void OnSerializing(StreamingContext context)
        {
            Debug.WriteLine("Serializing AlexaControlResponseContext");

            if ((this.properties == null) || (this.properties.Count == 0))
            {
                this.properties = default(List<AlexaProperty>); // will not be serialized
            }
        }

        public AlexaControlResponseContext()
        {
            properties = new List<AlexaProperty>();
        }
    }

    [KnownType(typeof(AlexaErrorResponsePayload))]
    [DataContract]
    public class AlexaResponsePayload
    {
        [DataMember(Name = "cause", EmitDefaultValue = false)]
        public ChangeReportCause cause { get; set; }
        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public string timestamp { get; set; }

        void OnSerializing(StreamingContext context)
        {
            Debug.WriteLine("Serializing AlexaResponsePayload");
        }

    }

    [DataContract]
    public class AlexaErrorResponsePayload : AlexaResponsePayload
    {
        [DataMember(Name = "type")]
        public string type { get; set; }

        [DataMember(Name = "message")]
        public string message { get; set; }

        public AlexaErrorResponsePayload(AlexaErrorTypes errType, string errMessage)
        {
            type = errType.ToString();
            message = errMessage;
        }
    }

    [DataContract]
    public class ResponseEndpoint
    {
        [DataMember(Name = "scope")]
        public Scope scope { get; set; }

        [DataMember(Name = "endpointId")]
        public string endpointId { get; set; }

        [DataMember(Name = "cookie", EmitDefaultValue = false)]
        public EndpointCookie cookie { get; set; }

        public ResponseEndpoint(DirectiveEndpoint endpoint)
        {
            scope = new Scope
            {
                type = endpoint.scope.type,
                token = endpoint.scope.token
            };
            endpointId = endpoint.endpointId;
        }
    }

    [DataContract]
    public class AlexaEventBody
    {
        [DataMember(Name = "header", Order = 1)]
        public Header header { get; set; }

        [DataMember(Name = "endpoint", EmitDefaultValue = false, Order = 2)]
        public ResponseEndpoint endpoint { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = false, Order = 3)]
        public AlexaResponsePayload payload { get; set; }

        public AlexaEventBody()
        {

        }

        public AlexaEventBody(Header header, DirectiveEndpoint directiveEndpoint)
        {
            header = new Header()
            {
                @namespace = "Alexa",
                name = "Response",
                payloadVersion = "3",
                messageID = header.messageID,
                correlationToken = header.correlationToken
            };
            if (directiveEndpoint != null)
            {
                endpoint = new ResponseEndpoint(directiveEndpoint);
            }
            payload = new AlexaResponsePayload();
        }
    }

    #endregion

    #region Report State and Change Reports

    [DataContract]
    public class ReportStateRequest
    {
        [DataMember(Name = "directive")]
        public AlexaDirective directive { get; set; }
    }

    [DataContract]
    public class ReportStateResponse
    {
        [DataMember(Name = "context", EmitDefaultValue = false)]
        public Context context { get; set; }

        [DataMember(Name = "event")]
        public AlexaEventBody @event { get; set; }

        public ReportStateResponse(AlexaDirective directive)
        {
            @event = new AlexaEventBody(directive.header, directive.endpoint);
            @event.header = new Header()
            {
                @namespace = "Alexa",
                name = "Response",
                payloadVersion = "3",
                messageID = directive.header.messageID,
                correlationToken = directive.header.correlationToken
            };

            context = new Context();
        }
    }

    [DataContract]
    public class ChangeReportCause
    {
        [DataMember(Name = "type")]
        public string type { get; set; }
    }

    [DataContract]
    public class ChangeReportPayload
    {
        [DataMember(Name = "change", EmitDefaultValue = false)]
        public Change change { get; set; }

        [DataMember(Name = "cause", EmitDefaultValue = false)]
        public ChangeReportCause cause { get; set; }

        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public string timestamp { get; set; }

        public ChangeReportPayload()
        {
            change = new Change();
        }
    }


    [DataContract]
    public class Change
    {
        [DataMember(Name = "cause", EmitDefaultValue = false)]
        public ChangeReportCause cause { get; set; }
        [DataMember(Name = "properties", EmitDefaultValue = false)]
        public List<AlexaProperty> properties { get; set; }
        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            if ((this.properties == null) || (this.properties.Count == 0))
            {
                this.properties = default(List<AlexaProperty>); // will not be serialized
            }
        }

        public Change()
        {
            properties = new List<AlexaProperty>();
            cause = new ChangeReportCause();
        }
    }

    [DataContract]
    public class AlexaChangeReportEvent
    {
        [DataMember(Name = "header", EmitDefaultValue = false)]
        public Header header { get; set; }
        [DataMember(Name = "endpoint", EmitDefaultValue = false)]
        public DirectiveEndpoint endpoint { get; set; }
        [DataMember(Name = "payload", EmitDefaultValue = false)]
        public ChangeReportPayload payload { get; set; }

        public AlexaChangeReportEvent()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new ChangeReportPayload();
        }
    }

    [DataContract]
    public class AlexaChangeReport
    {
        [DataMember(Name = "context", EmitDefaultValue = false)]
        public Context context { get; set; }
        [DataMember(Name = "event", EmitDefaultValue = false)]
        public AlexaChangeReportEvent @event { get; set; }

        public AlexaChangeReport()
        {
            context = new Context();
            @event = new AlexaChangeReportEvent();
        }
    }

    #endregion

    #region Authorization

    [DataContract]
    public class AuthorizationGrant
    {
        [DataMember(Name = "token_type")]
        public string token_type { get; set; }

        [DataMember(Name = "access_token")]
        public string access_token { get; set; }

        [DataMember(Name = "refresh_token")]
        public string refresh_token { get; set; }

        [DataMember(Name = "expires_in")]
        public int expires_in { get; set; }

        [DataMember(Name = "client_id")]
        public string client_id { get; set; }

        [DataMember(Name = "client_secret")]
        public string client_secret { get; set; }
    }

    [DataContract]
    public class AuthorizationGrantee
    {
        [DataMember(Name = "type")]
        public string type { get; set; }

        [DataMember(Name = "token")]
        public string token { get; set; }

        [DataMember(Name = "localAccessToken", EmitDefaultValue = false)]
        public string localAccessToken { get; set; }

        void OnSerializing(StreamingContext context)
        {
            localAccessToken = default(string);
        }

    }

    [DataContract]
    public class AuthorizationRequestPayload
    {
        [DataMember(Name = "grant")]
        public AuthorizationGrant grant { get; set; }

        [DataMember(Name = "grantee")]
        public AuthorizationGrantee grantee { get; set; }
    }

    [DataContract]
    public class AuthorizationDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }
        [DataMember(Name = "payload")]
        public AuthorizationRequestPayload payload { get; set; }
    }

    [DataContract]
    public class AuthorizationRequest
    {
        [DataMember(Name = "directive")]
        public AuthorizationDirective directive { get; set; }
    }


    [DataContract]
    public class AuthorizationResponse
    {
        [DataMember(Name = "event")]
        public AlexaEventBody @event { get; set; }

        public AuthorizationResponse()
        {

        }

        public AuthorizationResponse(AuthorizationDirective directive)
        {
            @event = new AlexaEventBody
            {
                header = new Header
                {
                    @namespace = directive.header.@namespace,
                    name = "AcceptGrant.Response",
                    messageID = directive.header.messageID
                },
                payload = new AlexaResponsePayload()
            };
        }
    }

    #endregion

}