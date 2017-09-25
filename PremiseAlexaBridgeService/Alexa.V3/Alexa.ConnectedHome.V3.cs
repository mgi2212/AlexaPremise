using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alexa.SmartHome.V3
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

    public class Context
    {
        [DataMember(Name = "properties", EmitDefaultValue = false)]
        public List<AlexaProperty> properties { get; set; }

        public Context()
        {
            properties = new List<AlexaProperty>();
        }
    }

    #endregion

    #region Capability

    [DataContract(Name = "properties")]
    public class Properties
    {
        [DataMember(Name = "supported", EmitDefaultValue = false, IsRequired = false, Order = 1)]
        public List<SupportedProperty> supported { get; set; }
        [DataMember(Name = "proactivelyReported", EmitDefaultValue = false, IsRequired = false, Order = 2)]
        public bool proactivelyReported { get; set; }
        [DataMember(Name = "retrievable", EmitDefaultValue = false, IsRequired = false, Order = 3)]
        public bool retrievable { get; set; }
        [DataMember(Name = "supportsDeactivation", EmitDefaultValue = false, IsRequired = false, Order = 4)]
        public bool supportsDeactivation { get; set; }

        public Properties()
        {
            supported = new List<SupportedProperty>();
        }

        public Properties(bool asyncSupported, bool querySupported)
        {
            // this.supported = new List<SupportedProperty>();
            // this.proactivelyReported = asyncSupported;
            // this.retrievable = querySupported;
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
        [DataMember(Name = "properties", EmitDefaultValue = false, IsRequired = false, Order = 4)]
        public Properties properties { get; set; }
        public Capability()
        {

        }

        public Capability(string interfaceName, bool asyncSupported, bool querySupported)
        {
            this.type = "Alexa.Interface";
            this.@interface = interfaceName;
            this.version = "3.0";
            this.properties = new Properties(asyncSupported, querySupported);
        }

    }

    #endregion

    #region Discovery

    [DataContract]
    public class DiscoveryRequest
    {
        [DataMember(Name = "directive")]
        public DiscoveryDirective directive { get; set; }

        public DiscoveryRequest()
        {
            directive = new DiscoveryDirective();
        }
    }

    [DataContract]
    public class DiscoveryDirectivePayload
    {
        [DataMember(Name = "scope", EmitDefaultValue = false)]
        public Scope scope { get; set; }

        public DiscoveryDirectivePayload()
        {
            scope = new Scope();
        }
    }

    [DataContract]
    public class DiscoveryDirective
    {
        [DataMember(Name = "header", IsRequired = true, Order = 1)]
        public Header header { get; set; }
        [DataMember(Name = "payload", IsRequired = true, Order = 3)]
        public DiscoveryDirectivePayload payload { get; set; }

        public DiscoveryDirective()
        {
            header = new Header();
            payload = new DiscoveryDirectivePayload();
        }
    }

    [DataContract]
    public class DiscoveryResponse
    {
        [DataMember(Name = "event", IsRequired = true, Order = 1)]
        public DiscoveryResponseEvent @event { get; set; }
        public DiscoveryResponse(DiscoveryDirective directive)
        {
            this.@event = new DiscoveryResponseEvent(directive);
        }
    }

    [DataContract]
    public class DiscoveryResponseEvent
    {
        [DataMember(Name = "header", IsRequired = true, Order = 1)]
        public Header header { get; set; }
        [DataMember(Name = "payload", IsRequired = true, Order = 2)]
        public DiscoveryResponsePayload payload { get; set; }
        public DiscoveryResponseEvent(DiscoveryDirective discoveryDirective)
        {

            this.header = new Header()
            {
                correlationToken = discoveryDirective?.header?.correlationToken,     // new ? is Null-condition operator does null test on member
                messageID = discoveryDirective?.header?.messageID,                   // and returns null if member is null
                name = "Discover.Response",
                @namespace = "Alexa.Discovery"
            };

            this.payload = new DiscoveryResponsePayload();
        }
    }

    [DataContract]
    public class DiscoveryResponsePayload
    {
        [DataMember(Name = "endpoints", EmitDefaultValue = false, IsRequired = false)]
        public List<DiscoveryEndpoint> endpoints { get; set; }

        [DataMember(Name = "exception", EmitDefaultValue = false, IsRequired = false)]
        public ExceptionResponsePayload exception { get; set; }

        public DiscoveryResponsePayload()
        {
            endpoints = new List<DiscoveryEndpoint>();
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

    public enum SystemRequestType
    {
        Unknown,
        HealthCheck,
    }

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
    public class ValidRangeInt
    {
        [DataMember(Name = "minimumValue")]
        int minimumValue { get; set; }
        [DataMember(Name = "maximumValue")]
        int maximumValue { get; set; }
    }

    [DataContract]
    public class ControlErrorPayload
    {
        // used for error responses
        [DataMember(Name = "type")]
        public string @type { get; set; }

        // used for error responses
        [DataMember(Name = "message")]
        public string message { get; set; }

        public ControlErrorPayload(string errorType, string errorMessage)
        {
            @type = errorType;
            message = errorMessage;
        }

    }

    [DataContract]
    public class ControlResponse
    {
        [DataMember(Name = "context", Order = 1, EmitDefaultValue = false)]
        public AlexaControlResponseContext context { get; set; }
        [DataMember(Name = "event", Order = 2)]
        public AlexaEventBody Event { get; set; }

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
            Event = new AlexaEventBody(header, endpoint);
        }
    }

    [DataContract]
    public class AlexaProperty
    {
        [DataMember(Name = "namespace")]
        public string @namespace { get; set; }
        [DataMember(Name = "name")]
        public string name { get; set; }
        [DataMember(Name = "value")]
        public object value { get; set; }
        [DataMember(Name = "timeOfSample")]
        public string timeOfSample { get; set; }
        [DataMember(Name = "uncertaintyInMilliseconds")]
        public int uncertaintyInMilliseconds { get; set; }

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
        [DataMember(Name = "properties")]
        public List<AlexaProperty> properties { get; set; }

        public AlexaControlResponseContext()
        {
            properties = new List<AlexaProperty>();
        }
    }

    [KnownType(typeof(AlexaErrorResponsePayload))]
    [DataContract(Namespace = "")]
    public class AlexaResponsePayload
    {

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

            endpoint = new ResponseEndpoint(directiveEndpoint);
            payload = new AlexaResponsePayload();
        }
    }

    #endregion

    #region Report State

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
        [DataMember(Name = "cause")]
        public ChangeReportCause cause { get; set; }
        [DataMember(Name = "properties")]
        public List<AlexaProperty> properties { get; set; }

        public ChangeReportPayload()
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
        [DataMember(Name = "type")]
        public string type { get; set; }

        [DataMember(Name = "code")]
        public string code { get; set; }
    }

    [DataContract]
    public class AuthorizationGrantee
    {
        [DataMember(Name = "type")]
        public string type { get; set; }

        [DataMember(Name = "token")]
        public string token { get; set; }
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