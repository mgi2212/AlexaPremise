using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alexa.SmartHome.V3
{

    #region Event

    [DataContract(Name = "event")]
    public class AlexaEvent
    {
        [DataMember(Name = "header", IsRequired = true, Order = 1)]
        public Header header { get; set; }
        [DataMember(Name = "endpoint", IsRequired = false, Order = 2)]
        public DiscoveryEndpoint endpoint { get; set; }
        [DataMember(Name = "payload", IsRequired = true, Order = 3)]
        public EventPayload payload { get; set; }
        [DataMember(Name = "context", IsRequired = false, Order = 4)]
        public Context context { get; set; }

        public AlexaEvent()
        {
            header = new Header();
            payload = new EventPayload();
        }
    }
    #endregion

    #region Directive

    [DataContract(Name = "directive")]
    public class AlexaDirective
    {
        [DataMember(Name = "header")]
        public Header header;

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint;

        [DataMember(Name = "payload")]
        public object payload;
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
        [DataMember(Name = "cookie", IsRequired = false, Order = 3)]
        public EndpointCookie cookie { get; set; }
    }

    public class EndpointCookie
    {
        [DataMember(Name = "path", EmitDefaultValue = false)]
        public string path;
    }
    #endregion

    #region Payload

    public class EventPayload
    {
        [DataMember(Name = "endpoints", EmitDefaultValue = false)]
        public List<DiscoveryEndpoint> endpoints;
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

    #region Value

    public class Value
    {
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public object value { get; set; }
        [DataMember(Name = "scale", EmitDefaultValue = false)]
        public string scale { get; set; }
    }

    #endregion

    //public class Property
    //{
    //    [DataMember(Name = "namespace", EmitDefaultValue = false)]
    //    public string @namespace { get; set; }
    //    [DataMember(Name = "name", EmitDefaultValue = false)]
    //    public string name { get; set; }
    //    [DataMember(Name = "value", EmitDefaultValue = false)]
    //    public Value value { get; set; }
    //    [DataMember(Name = "timeOfSample", EmitDefaultValue = false)]
    //    public string timeOfSample { get; set; }
    //    [DataMember(Name = "uncertaintyInMilliseconds", EmitDefaultValue = false)]
    //    public string uncertaintyInMilliseconds { get; set; }
    //}

    #endregion

    #endregion

    #region Capability

    [DataContract(Name = "properties")]
    public class Properties
    {
        [DataMember(Name = "supported", EmitDefaultValue = false, IsRequired = false, Order = 1)]
        public List<SupportedProperty> supported;
        [DataMember(Name = "proactivelyReported", EmitDefaultValue = false, IsRequired = false, Order = 2)]
        public bool proactivelyReported { get; set; }
        [DataMember(Name = "retrievable", EmitDefaultValue = false, IsRequired = false, Order = 3)]
        public bool retrievable { get; set; }
        [DataMember(Name = "supportsDeactivation", EmitDefaultValue = false, IsRequired = false, Order = 4)]
        public bool supportsDeactivation { get; set; }

        public Properties()
        {

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

    #region Discovery.Request

    [DataContract]
    public class DiscoveryRequest
    {
        [DataMember(Name = "directive")]
        public DiscoveryDirective directive;
    }

    [DataContract]
    public class DiscoveryDirectivePayload
    {
        [DataMember(Name = "scope", EmitDefaultValue = false)]
        public Scope scope { get; set; }
    }

    [DataContract]
    public class DiscoveryDirective
    {
        [DataMember(Name = "header", IsRequired = true, Order = 1)]
        public Header header { get; set; }
        [DataMember(Name = "payload", IsRequired = true, Order = 3)]
        public DiscoveryDirectivePayload payload { get; set; }
    }

    #endregion

    #region Discovery.Response

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
        public DiscoverResponseEventPayload payload { get; set; }

        public DiscoveryResponseEvent(DiscoveryDirective discoveryDirective)
        {

            this.header = new Header()
            {
                // correlationToken = discoveryDirective?.header?.correlationToken,  // new ? is Null-condition operator does null test on member
                messageID = discoveryDirective?.header?.messageID,                // and returns null if member is null
                name = "Discover.Response",
                @namespace = "Alexa.Discovery"
            };

            this.payload = new DiscoverResponseEventPayload();
        }
    }

    [DataContract(Namespace = "Alexa.Discovery")]
    public class DiscoverResponseEventPayload
    {
        [DataMember(Name = "endpoints", EmitDefaultValue = false, IsRequired = true)]
        public List<DiscoveryEndpoint> endpoints { get; set; }

        public DiscoverResponseEventPayload()
        {
            endpoints = new List<DiscoveryEndpoint>();
        }
    }

    #endregion

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

    #region Control
    [DataContract]
    public class ControlRequest
    {
        [DataMember(Name = "directive")]
        public AlexaDirective directive;
    }

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

    }

    [DataContract]
    public class ControlResponse
    {
        [DataMember(Name = "context", Order = 1, EmitDefaultValue = false)]
        public AlexaControlResponseContext context { get; set; }
        [DataMember(Name = "event", Order = 2)]
        public AlexaEventBody @event;

        public ControlResponse(AlexaDirective directive)
        {
            context = new AlexaControlResponseContext(directive);
            @event = new AlexaEventBody(directive);
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
            uncertaintyInMilliseconds = 200;
        }

        public AlexaProperty(AlexaDirective directive)
        {
            @namespace = directive.header.@namespace;
            uncertaintyInMilliseconds = 200;
        }

    }

    [DataContract]
    public class AlexaControlResponseContext
    {
        [DataMember(Name = "properties")]
        public List<AlexaProperty> properties;

        public AlexaControlResponseContext(AlexaDirective directive)
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
    internal class AlexaErrorResponsePayload : AlexaResponsePayload
    {
        [DataMember(Name = "type")]
        public string type { get; set; }

        [DataMember(Name = "message")]
        public string message { get; set; }

        public AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes errType, string errMessage)
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

        [DataMember(Name = "cookie")]
        public EndpointCookie cookie { get; set;}

        public ResponseEndpoint(DirectiveEndpoint endpoint)
        {
            scope = new Scope();
            scope.type = endpoint.scope.type;
            scope.token = endpoint.scope.token;
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

        [DataMember(Name = "payload", Order = 3)]
        public AlexaResponsePayload payload { get; set; }

        public AlexaEventBody()
        {

        }

        public AlexaEventBody(AlexaDirective directive)
        {
            header = new Header()
            {
                @namespace = "Alexa",
                name = "Response",
                payloadVersion = "3",
                messageID = directive.header.messageID,
                correlationToken = directive.header.correlationToken
            };

            endpoint = new ResponseEndpoint(directive.endpoint);
            payload = new AlexaResponsePayload();
        }
    }

    #endregion

    #region Report State

    [DataContract]
    public class ReportStateRequest
    {
        [DataMember(Name = "directive")]
        public AlexaDirective directive;
    }

    [DataContract]
    public class ReportStateResponse
    {
        [DataMember(Name = "context")]
        public Context context;

        [DataMember(Name = "event")]
        public AlexaEventBody @event;

        public ReportStateResponse(AlexaDirective directive)
        {
            @event = new AlexaEventBody(directive);
            context = new Context();
        }
    }

    #endregion

    #region Authorization

    [DataContract]
    public class AuthorizationGrant
    {
        [DataMember(Name = "type")]
        public string type;

        [DataMember(Name = "code")]
        public string code;
    }

    [DataContract]
    public class AuthorizationGrantee
    {
        [DataMember(Name = "type")]
        public string type;

        [DataMember(Name = "token")]
        public string token;
    }
    
    [DataContract]
    public class AuthorizationRequestPayload
    {
        [DataMember(Name = "grant")]
        public AuthorizationGrant grant;

        [DataMember(Name = "grantee")]
        public AuthorizationGrantee grantee;
    }

    [DataContract]
    public class AuthorizationDirective
    {
        [DataMember(Name = "header")]
        public Header header;
        [DataMember(Name = "payload")]
        public AuthorizationRequestPayload payload;
    }

    [DataContract]
    public class AuthorizationRequest
    {
        [DataMember(Name = "directive")]
        public AuthorizationDirective directive;
    }


    [DataContract]
    public class AuthorizationResponse
    {
        [DataMember(Name = "event")]
        public AlexaEventBody @event;


        public AuthorizationResponse()
        {

        }

        public AuthorizationResponse(AuthorizationDirective directive)
        {
            @event = new AlexaEventBody();
            @event.header = new Header();
            @event.header.@namespace = directive.header.@namespace;
            @event.header.name = "AcceptGrant.Response";
            @event.header.messageID = directive.header.messageID;
            @event.payload = new AlexaResponsePayload();
        }
    }

    #endregion

}