using System.Runtime.Serialization;

namespace Alexa.SmartHome.V3
{
    /// <summary>
    /// A header has a set of expected fields that are the same across message types. 
    /// These provide different types of identifying information. 
    /// </summary>
    public class Header
    {
        [DataMember(Name = "correlationToken", EmitDefaultValue = false, Order = 1)]
        public string correlationToken { get; set; }
        [DataMember(Name = "messageId", EmitDefaultValue = false, Order = 2)]
        public string messageId { get; set; }
        [DataMember(Name = "namespace", EmitDefaultValue = false, Order = 3)]
        public string @namespace { get; set; }
        [DataMember(Name = "name", EmitDefaultValue = false, Order = 4)]
        public string name { get; set; }
        [DataMember(Name = "payloadVersion", EmitDefaultValue = false, Order = 5)]
        public string payloadVersion { get; set; }
    }

    public class ScopeType
    {
        [DataMember(Name = "type", EmitDefaultValue = false, Order = 1)]
        public string type { get; set; }
        [DataMember(Name = "value", EmitDefaultValue = false, Order = 2)]
        public string value { get; set; }
    }

    public class CookieType
    {
        [DataMember(Name = "key", EmitDefaultValue = false, Order = 1)]
        public string key { get; set; }
        [DataMember(Name = "value", EmitDefaultValue = false, Order = 2)]
        public string value { get; set; }
    }

    public class Scope
    {

        [DataMember(Name = "scopeType", EmitDefaultValue = false, Order = 1)]
        ScopeType scopeType;
    }

    public class Cookie
    {
        [DataMember(Name = "cookies", EmitDefaultValue = false, Order = 1)]
        CookieType[] cookies;
    }

    public class Endpoint
    {

    }



    #region Exception

    [DataContract(Namespace = "")]
    public class ExceptionResponsePayload
    {
        [DataMember(Name = "code", EmitDefaultValue = true, Order = 1)]
        public string code { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = true, Order = 2)]
        public string description { get; set; }
    }

    #endregion

    public class ApplianceDetails
    {
        public string dimmable { get; set; }
        public string path { get; set; }
    }

    [DataContract(Namespace = "")]
    public class Appliance
    {
        [DataMember(Name = "applianceId", EmitDefaultValue = true, Order = 1)]
        public string applianceId { get; set; }
        [DataMember(Name = "manufacturerName", EmitDefaultValue = false, Order = 2)]
        public string manufacturerName { get; set; }
        [DataMember(Name = "modelName", EmitDefaultValue = false, Order = 3)]
        public string modelName { get; set; }
        [DataMember(Name = "version", EmitDefaultValue = false, Order = 4)]
        public string version { get; set; }
        [DataMember(Name = "friendlyName", EmitDefaultValue = false, Order = 5)]
        public string friendlyName { get; set; }
        [DataMember(Name = "friendlyDescription", EmitDefaultValue = false, Order = 6)]
        public string friendlyDescription { get; set; }
        [DataMember(Name = "isReachable", EmitDefaultValue = false, Order = 7)]
        public string isReachable { get; set; }
        [DataMember(Name = "additionalApplianceDetails", EmitDefaultValue = true, Order = 8)]
        public ApplianceDetails additionalApplianceDetails { get; set; }

        public Appliance()
        {
            this.additionalApplianceDetails = new ApplianceDetails();
        }
    }

    public class DiscoveryResponsePayload
    {
        public Appliance[] discoveredAppliances { get; set; }
    }

    public class DiscoveryResponse
    {
        public Header header { get; set; }
        public DiscoveryResponsePayload payload { get; set; }
    }

    public class DiscoveryRequestPayload
    {
        public string accessToken { get; set; }
    }

    public class DiscoveryRequest
    {
        public Header header { get; set; }
        public DiscoveryRequestPayload payload { get; set; }

        public DiscoveryRequest()
        {
            header = new Header();
            header.@namespace = "Discovery";
            header.name = "DiscoverAppliancesRequest";
            header.payloadVersion = "2";
            payload = new DiscoveryRequestPayload();
        }
    }

    public class ControlSwitchOnOffRequestPayload
    {
        public string accessToken { get; set; }
        public string switchControlAction { get; set; }
        public Appliance appliance { get; set; }
        public ControlSwitchOnOffRequestPayload(Appliance appliance)
        {
            this.appliance = appliance;
        }
    }

    public class ControlSwitchOnOffRequest
    {
        [DataMember(Name = "header", EmitDefaultValue = true, Order = 1)]
        public Header header { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = true, Order = 2)]
        public ControlSwitchOnOffRequestPayload payload { get; set; }

        public ControlSwitchOnOffRequest(string accessToken, Appliance appliance, string switchControlAction)
        {
            header = new Header();
            header.@namespace = "Control";
            header.name = "SwitchOnOffRequest";
            header.payloadVersion = "1";
            payload = new ControlSwitchOnOffRequestPayload(appliance);
            payload.accessToken = accessToken;
            payload.switchControlAction = switchControlAction;
        }
    }

    [DataContract(Namespace = "")]
    public class ControlResponse
    {
        [DataMember(Name = "header", EmitDefaultValue = true, Order = 1)]
        public Header header { get; set; }
        [DataMember(Name = "payload", EmitDefaultValue = true, Order = 2)]
        public ControlResponsePayload payload { get; set; }
    }

    [DataContract(Namespace = "")]
    public class ControlResponsePayload
    {
        [DataMember(Name = "success", EmitDefaultValue = true, Order = 1)]
        public bool success { get; set; }

        [DataMember(Name = "exception", EmitDefaultValue = false, Order = 2)]
        public ExceptionResponsePayload exception { get; set; }
    }


}
