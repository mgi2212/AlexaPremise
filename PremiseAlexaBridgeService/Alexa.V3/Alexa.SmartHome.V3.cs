using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.EndpointHealth;
using Alexa.HVAC;
using Alexa.Lighting;

namespace Alexa.SmartHomeAPI.V3
{
    /// <summary>
    /// Generic Data Contracts for Alexa.SmartHome V3 API
    /// </summary>

    #region Directive

    [DataContract(Name = "directive")]
    public class AlexaDirective
    {
        #region Constructors

        public AlexaDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public object payload { get; set; }

        #endregion Properties
    }

    #endregion Directive

    #region Header

    [DataContract(Name = "header")]
    public class Header
    {
        #region Constructors

        public Header()
        {
            payloadVersion = "3";
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "namespace", IsRequired = true, Order = 3)]
        public string @namespace { get; set; }

        [DataMember(Name = "correlationToken", EmitDefaultValue = false, IsRequired = false, Order = 1)]
        public string correlationToken { get; set; }

        [DataMember(Name = "messageId", IsRequired = true, Order = 2)]
        public string messageID { get; set; }

        [DataMember(Name = "name", IsRequired = true, Order = 4)]
        public string name { get; set; }

        [DataMember(Name = "payloadVersion", IsRequired = true, Order = 5)]
        public string payloadVersion { get; set; }

        #endregion Properties
    }

    #endregion Header

    #region Endpoint

    [DataContract]
    public class DirectiveEndpoint
    {
        #region Constructors

        public DirectiveEndpoint()
        {
            scope = new Scope();
            cookie = new EndpointCookie();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "cookie", EmitDefaultValue = false, IsRequired = false, Order = 3)]
        public EndpointCookie cookie { get; set; }

        [DataMember(Name = "endpointId", IsRequired = true, Order = 2)]
        public string endpointId { get; set; }

        [DataMember(Name = "scope", IsRequired = true, Order = 1)]
        public Scope scope { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class DiscoveryEndpoint
    {
        #region Constructors

        public DiscoveryEndpoint()
        {
            displayCategories = new List<string>();
            cookie = new EndpointCookie();
            capabilities = new List<Capability>();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "capabilities", EmitDefaultValue = false, IsRequired = true, Order = 7)]
        public List<Capability> capabilities { get; set; }

        [DataMember(Name = "cookie", EmitDefaultValue = false, IsRequired = false, Order = 6)]
        public EndpointCookie cookie { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false, IsRequired = true, Order = 4)]
        public string description { get; set; }

        [DataMember(Name = "displayCategories", EmitDefaultValue = false, IsRequired = true, Order = 5)]
        public List<string> displayCategories { get; set; }

        [DataMember(Name = "endpointId", EmitDefaultValue = true, IsRequired = true, Order = 1)]
        public string endpointId { get; set; }

        [DataMember(Name = "friendlyName", EmitDefaultValue = false, IsRequired = true, Order = 3)]
        public string friendlyName { get; set; }

        [DataMember(Name = "manufacturerName", EmitDefaultValue = false, IsRequired = true, Order = 2)]
        public string manufacturerName { get; set; }

        #endregion Properties
    }

    public class EndpointCookie
    {
        #region Fields

        [DataMember(Name = "path", EmitDefaultValue = false)]
        public string path;

        #endregion Fields
    }

    #endregion Endpoint

    #region Scope

    [DataContract(Name = "scope")]
    public class Scope
    {
        #region Properties

        [DataMember(Name = "localAccessToken", EmitDefaultValue = false, Order = 3)]
        public string localAccessToken { get; set; }

        [DataMember(Name = "token", EmitDefaultValue = true, Order = 2)]
        public string token { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = true, Order = 1)]
        public string type { get; set; }

        #endregion Properties
    }

    #endregion Scope

    #region Property

    [DataContract]
    public class SupportedProperty
    {
        #region Constructors

        public SupportedProperty(string propName)
        {
            propertyName = propName;
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string propertyName { get; set; }

        #endregion Properties
    }

    #endregion Property

    #region Context

    [DataContract]
    public class Context
    {
        #region Constructors

        public Context()
        {
            propertiesInternal = new Dictionary<string, AlexaProperty>();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "properties", EmitDefaultValue = false)]
        public List<AlexaProperty> properties => propertiesInternal?.Values.ToList();

        internal Dictionary<string, AlexaProperty> propertiesInternal { get; set; }

        #endregion Properties

        #region Methods

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if ((propertiesInternal == null) || (propertiesInternal.Count == 0))
            {
                propertiesInternal = default(Dictionary<string, AlexaProperty>); // will not be serialized
            }
        }

        #endregion Methods
    }

    #endregion Context

    #region Capability

    [DataContract(Name = "capability")]
    public class Capability
    {
        #region Constructors

        public Capability(string interfaceName, bool asyncSupported, bool querySupported)
        {
            type = "AlexaInterface";
            version = "3.0";
            @interface = interfaceName;
            properties = new Properties(asyncSupported, querySupported);
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "interface", IsRequired = true, EmitDefaultValue = true, Order = 2)]
        public string @interface { get; set; }

        [DataMember(Name = "proactivelyReported", EmitDefaultValue = false, IsRequired = false, Order = 5)]
        public bool proactivelyReported { get; set; }

        [DataMember(Name = "properties", EmitDefaultValue = false, IsRequired = false, Order = 6)]
        public Properties properties { get; set; }

        [DataMember(Name = "supportsDeactivation", EmitDefaultValue = false, IsRequired = false, Order = 4)]
        public bool supportsDeactivation { get; set; }

        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true, Order = 1)]
        public string type { get; set; }

        [DataMember(Name = "version", IsRequired = true, EmitDefaultValue = true, Order = 3)]
        public string version { get; set; }

        #endregion Properties

        #region Methods

        public bool HasProperties()
        {
            return !(properties?.supported == null || (properties.supported.Count == 0));
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (HasProperties() == false)
            {
                properties = default(Properties); // will not be serialized
            }
        }

        #endregion Methods
    }

    [DataContract(Name = "properties")]
    public class Properties
    {
        #region Constructors

        public Properties()
        {
            supported = new List<SupportedProperty>();
        }

        //[DataMember(Name = "supportsDeactivation", EmitDefaultValue = false, IsRequired = false, Order = 4)]
        //public bool supportsDeactivation { get; set; }
        public Properties(bool asyncSupported, bool querySupported)
        {
            supported = new List<SupportedProperty>();
            proactivelyReported = asyncSupported;
            retrievable = querySupported;
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "proactivelyReported", EmitDefaultValue = true, IsRequired = false, Order = 2)]
        public bool proactivelyReported { get; set; }

        [DataMember(Name = "retrievable", EmitDefaultValue = true, IsRequired = false, Order = 3)]
        public bool retrievable { get; set; }

        [DataMember(Name = "supported", EmitDefaultValue = false, IsRequired = false, Order = 1)]
        public List<SupportedProperty> supported { get; set; }

        #endregion Properties
    }

    #endregion Capability

    #region Exceptions and Errors

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

    [DataContract]
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

        [DataMember(Name = "faultingParameter", EmitDefaultValue = false, Order = 7)]
        public string faultingParameter { get; set; }

        [DataMember(Name = "maximumValue", EmitDefaultValue = false, Order = 4)]
        public double maximumValue { get; set; }

        [DataMember(Name = "minimumFirmwareVersion", EmitDefaultValue = false, Order = 6)]
        public string minimumFirmwareVersion { get; set; }

        [DataMember(Name = "minimumValue", EmitDefaultValue = false, Order = 3)]
        public double minimumValue { get; set; }

        [DataMember(Name = "rateLimit", EmitDefaultValue = false, Order = 7)]
        public int rateLimit { get; set; }  //An integer that represents the maximum number of requests a device will accept in the specified time unit.

        [DataMember(Name = "timeUnit", EmitDefaultValue = false, Order = 7)]
        public string timeUnit { get; set; }

        #endregion Properties

        //An all-caps string that indicates the time unit for rateLimit such as MINUTE, HOUR or DAY.
        // The property or field in the request message that was malformed or unexpected, and could not be handled by the skill adapter.
    }

    #endregion Exceptions and Errors

    #region System

    [DataContract(Namespace = "Alexa.ConnectedHome.System")]
    public class SystemRequest
    {
        #region Properties

        [DataMember(Name = "header", Order = 1)]
        public Header header { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = false, Order = 2)]
        public HealthCheckRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.System")]
    public class SystemResponse
    {
        #region Properties

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = false, IsRequired = true)]
        public SystemResponsePayload payload { get; set; }

        #endregion Properties
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

    #region Control

    [DataContract]
    public class AlexaControlResponseContext
    {
        #region Constructors

        public AlexaControlResponseContext()
        {
            properties = new List<AlexaProperty>();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "properties", EmitDefaultValue = false)]
        public List<AlexaProperty> properties { get; set; }

        #endregion Properties

        #region Methods

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            Debug.WriteLine("Serializing AlexaControlResponseContext");

            if ((properties == null) || (properties.Count == 0))
            {
                properties = default(List<AlexaProperty>); // will not be serialized
            }
        }

        #endregion Methods
    }

    [DataContract]
    public class AlexaErrorResponsePayload : AlexaResponsePayload
    {
        #region Constructors

        public AlexaErrorResponsePayload(AlexaErrorTypes errType, string errMessage)
        {
            type = errType.ToString();
            message = errMessage;
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "message")]
        public string message { get; set; }

        [DataMember(Name = "type")]
        public string type { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaEventBody
    {
        #region Constructors

        public AlexaEventBody()
        {
        }

        public AlexaEventBody(Header header, DirectiveEndpoint directiveEndpoint)
        {
            this.header = new Header
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

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint", EmitDefaultValue = false, Order = 2)]
        public ResponseEndpoint endpoint { get; set; }

        [DataMember(Name = "header", Order = 1)]
        public Header header { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = false, Order = 3)]
        public AlexaResponsePayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    [KnownType(typeof(AlexaEndpointHealthValue))]
    [KnownType(typeof(AlexaColorValue))]
    [KnownType(typeof(AlexaTemperature))]
    [KnownType(typeof(AlexaThermostatMode))]
    public class AlexaProperty
    {
        #region Constructors

        public AlexaProperty()
        {
            uncertaintyInMilliseconds = 20;
        }

        public AlexaProperty(Header header)
        {
            @namespace = header.@namespace;
            uncertaintyInMilliseconds = 20;
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "namespace")]
        public string @namespace { get; set; }

        [DataMember(Name = "name")]
        public string name { get; set; }

        [DataMember(Name = "timeOfSample")]
        public string timeOfSample { get; set; }

        [DataMember(Name = "uncertaintyInMilliseconds")]
        public int uncertaintyInMilliseconds { get; set; }

        [DataMember(Name = "value")]
        public object value { get; set; }

        #endregion Properties
    }

    [KnownType(typeof(AlexaErrorResponsePayload))]
    [DataContract]
    public class AlexaResponsePayload
    {
        #region Properties

        [DataMember(Name = "cause", EmitDefaultValue = false)]
        public ChangeReportCause cause { get; set; }

        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public string timestamp { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class ControlResponse
    {
        #region Constructors

        public ControlResponse()
        {
        }

        public ControlResponse(object[] args)
        {
            context = new AlexaControlResponseContext();
            Event = new AlexaEventBody(args[0] as Header, args[1] as DirectiveEndpoint);
        }

        public ControlResponse(Header headerObject, DirectiveEndpoint endpoint)
        {
            context = new AlexaControlResponseContext();
            Event = new AlexaEventBody(headerObject, endpoint);
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "context", Order = 1, EmitDefaultValue = false)]
        public AlexaControlResponseContext context { get; set; }

        [DataMember(Name = "event", Order = 2)]
        public AlexaEventBody Event { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class ResponseEndpoint
    {
        #region Constructors

        public ResponseEndpoint(DirectiveEndpoint endpoint)
        {
            scope = new Scope
            {
                type = endpoint.scope.type,
                token = endpoint.scope.token
            };
            endpointId = endpoint.endpointId;
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "cookie", EmitDefaultValue = false)]
        public EndpointCookie cookie { get; set; }

        [DataMember(Name = "endpointId")]
        public string endpointId { get; set; }

        [DataMember(Name = "scope")]
        public Scope scope { get; set; }

        #endregion Properties
    }

    #endregion Control

    #region Report State and Change Reports

    [DataContract]
    public class AlexaChangeReport
    {
        #region Constructors

        public AlexaChangeReport()
        {
            context = new Context();
            @event = new AlexaChangeReportEvent();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "event", EmitDefaultValue = false)]
        public AlexaChangeReportEvent @event { get; set; }

        [DataMember(Name = "context", EmitDefaultValue = false)]
        public Context context { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaChangeReportEvent
    {
        #region Constructors

        public AlexaChangeReportEvent()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new ChangeReportPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint", EmitDefaultValue = false)]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header", EmitDefaultValue = false)]
        public Header header { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = false)]
        public ChangeReportPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class Change
    {
        #region Constructors

        public Change()
        {
            properties = new List<AlexaProperty>();
            cause = new ChangeReportCause();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "cause", EmitDefaultValue = false)]
        public ChangeReportCause cause { get; set; }

        [DataMember(Name = "properties", EmitDefaultValue = false)]
        public List<AlexaProperty> properties { get; set; }

        #endregion Properties

        #region Methods

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if ((properties == null) || (properties.Count == 0))
            {
                properties = default(List<AlexaProperty>); // will not be serialized
            }
        }

        #endregion Methods
    }

    [DataContract]
    public class ChangeReportCause
    {
        #region Properties

        [DataMember(Name = "type")]
        public string type { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class ChangeReportPayload
    {
        #region Constructors

        public ChangeReportPayload()
        {
            change = new Change();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "cause", EmitDefaultValue = false)]
        public ChangeReportCause cause { get; set; }

        [DataMember(Name = "change", EmitDefaultValue = false)]
        public Change change { get; set; }

        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public string timestamp { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class ReportStateRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class ReportStateResponse
    {
        #region Constructors

        public ReportStateResponse(AlexaDirective directive)
        {
            @event = new AlexaEventBody(directive.header, directive.endpoint);
            @event.header = new Header
            {
                @namespace = "Alexa",
                name = "Response",
                payloadVersion = "3",
                messageID = directive.header.messageID,
                correlationToken = directive.header.correlationToken
            };

            context = new Context();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "event")]
        public AlexaEventBody @event { get; set; }

        [DataMember(Name = "context", EmitDefaultValue = false)]
        public Context context { get; set; }

        #endregion Properties
    }

    #endregion Report State and Change Reports

    #region Authorization

    [DataContract]
    public class AuthorizationDirective
    {
        #region Properties

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AuthorizationRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AuthorizationGrant
    {
        #region Properties

        [DataMember(Name = "access_token")]
        public string access_token { get; set; }

        [DataMember(Name = "client_id")]
        public string client_id { get; set; }

        [DataMember(Name = "client_secret")]
        public string client_secret { get; set; }

        [DataMember(Name = "expires_in")]
        public int expires_in { get; set; }

        [DataMember(Name = "refresh_token")]
        public string refresh_token { get; set; }

        [DataMember(Name = "token_type")]
        public string token_type { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AuthorizationGrantee
    {
        #region Properties

        [DataMember(Name = "localAccessToken", EmitDefaultValue = false)]
        public string localAccessToken { get; set; }

        [DataMember(Name = "token")]
        public string token { get; set; }

        [DataMember(Name = "type")]
        public string type { get; set; }

        #endregion Properties

        #region Methods

        [OnSerializing]
        private void OnSerializing(StreamingContext streamContext)
        {
            localAccessToken = default(string);
        }

        #endregion Methods
    }

    [DataContract]
    public class AuthorizationRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AuthorizationDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AuthorizationRequestPayload
    {
        #region Properties

        [DataMember(Name = "grant")]
        public AuthorizationGrant grant { get; set; }

        [DataMember(Name = "grantee")]
        public AuthorizationGrantee grantee { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AuthorizationResponse
    {
        #region Constructors

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

        #endregion Constructors

        #region Properties

        [DataMember(Name = "event")]
        public AlexaEventBody @event { get; set; }

        #endregion Properties
    }

    #endregion Authorization
}