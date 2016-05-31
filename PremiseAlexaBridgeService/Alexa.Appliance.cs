using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using SYSWebSockClient;

namespace Alexa.SmartHome
{

    [DataContract(Namespace ="Alexa.ConnectedHome.Discovery")]
    public class AdditionalApplianceDetails
    {
        [DataMember(Name = "purpose", Order = 1)]
        public string purpose { get; set; }

        [DataMember(Name = "dimmable", Order = 2)]
        public string dimmable { get; set; }

        [DataMember(Name = "path", Order = 3)]
        public string path { get; set; }
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class Appliance
    {

        [DataMember(Name = "actions", EmitDefaultValue = false, Order = 1)]
        public List<string> actions { get; set; }

        [DataMember(Name = "additionalApplianceDetails", EmitDefaultValue = false, Order = 2)]
        public AdditionalApplianceDetails additionalApplianceDetails { get; set; }

        [DataMember(Name = "applianceId", Order = 3)]
        public string applianceId { get; set;}

        [DataMember(Name = "friendlyDescription", EmitDefaultValue = false, Order = 4)]
        public string friendlyDescription { get; set; }

        [DataMember(Name = "friendlyName", EmitDefaultValue = false, Order = 5)]
        public string friendlyName { get; set; }

        [DataMember(Name = "isReachable", EmitDefaultValue = true, Order = 6)]
        public bool isReachable { get; set; }

        [DataMember(Name = "manufacturerName", EmitDefaultValue = false, Order = 7)]
        public string manufacturerName { get; set; }

        [DataMember(Name = "modelName", EmitDefaultValue = false, Order = 8)]
        public string modelName {get; set; }

        [DataMember(Name = "version", EmitDefaultValue = false, Order = 9)]
        public string version { get; set; }

    }


    [DataContract(Namespace = "")]
    public class ApplianceIdPayload
    {

        [DataMember(Name = "applianceId", Order = 1)]
        public string applianceId { get; set; }

        [DataMember(Name = "additionalApplianceDetails", EmitDefaultValue = false, Order = 2)]
        public AdditionalApplianceDetails additionalApplianceDetails { get; set; }
    }

    [DataContract(Name = "previousState", Namespace = "Alexa.ConnectedHome.Control")]
    public class AppliancePreviousState
    {
        [DataMember(Name = "mode", EmitDefaultValue = false)]
        public ApplianceValue mode;
        [DataMember(Name = "targetTemperature", EmitDefaultValue = false)]
        public ApplianceValue targetTemperature;
    }

    [DataContract(Name="payload", Namespace = "Alexa.ConnectedHome.Control")]
    public class ApplianceControlRequestPayload
    {
        [DataMember(Name = "accessToken", Order = 1)]
        public string accessToken { get; set; }

        [DataMember(Name = "appliance", Order = 2)]
        public ApplianceIdPayload appliance;

        [DataMember(Name = "targetTemperature", EmitDefaultValue =false)]
        public ApplianceValue targetTemperature;

        [DataMember(Name = "temperatureMode", EmitDefaultValue = false)]
        public ApplianceValue temperatureMode;

        [DataMember(Name = "deltaTemperature", EmitDefaultValue = false)]
        public ApplianceValue deltaTemperature;

        [DataMember(Name = "percentageState", EmitDefaultValue = false)]
        public ApplianceValue percentageState;

        [DataMember(Name = "deltaPercentage", EmitDefaultValue = false)]
        public ApplianceValue deltaPercentage;

        [DataMember(Name = "initiationTimestamp", EmitDefaultValue = false)]
        public string initiationTimestamp; 
    }

    [DataContract(Namespace = "")]
    public class ApplianceControlResponsePayload
    {

        [DataMember(Name = "previousState", EmitDefaultValue = false, Order = 1)]
        public AppliancePreviousState previousState;

        [DataMember(Name = "targetTemperature", EmitDefaultValue = false)]
        public ApplianceValue targetTemperature;

        [DataMember(Name = "temperatureMode", EmitDefaultValue = false)]
        public ApplianceValue temperatureMode;

        [DataMember(Name = "exception", EmitDefaultValue = false, Order = 2)]
        public ExceptionResponsePayload exception { get; set; }
    }

}
