using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alexa.SmartHome.V2
{

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class ApplianceIdPayload
    {

        [DataMember(Name = "applianceId", Order = 1)]
        public string applianceId { get; set; }

        [DataMember(Name = "additionalApplianceDetails", EmitDefaultValue = false, Order = 2)]
        public AdditionalApplianceDetails additionalApplianceDetails { get; set; }
    }

    
    #region Discovery

    public enum AlexaApplianceTypes
    {
        UNKNOWN,
        LIGHT,
        THERMOSTAT,
        SCENE_TRIGGER,
        CAMERA,
        SMARTLOCK,
        SMARTPLUG,
        SWITCH,
        ACTIVITY_TRIGGER,
        BLIND,
        DOOR,
        FAN
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class AdditionalApplianceDetails
    {

        [DataMember(Name = "path", Order = 3)]
        public string path { get; set; }
    }

    [DataContract(Namespace = "Alexa.ConnectedHome.Discovery")]
    public class Appliance
    {
        [DataMember(Name = "friendlyName", EmitDefaultValue = false, Order = 1)]
        private string _friendlyName;
        public string friendlyName {
            get { return _friendlyName; }
            set { _friendlyName = value != null && value.Length > 128 ? value.Substring(0, 128) : value; }
        }

        [DataMember(Name = "friendlyDescription", EmitDefaultValue = false, Order = 2)]
        private string _friendlyDescription;
        public string friendlyDescription {
            get { return _friendlyDescription; }
            set { _friendlyDescription = value != null && value.Length > 128 ? value.Substring(0, 128) : value; }
        }

                [DataMember(Name = "manufacturerName", EmitDefaultValue = false, Order = 3)]
        private string _manufacturerName;
        public string manufacturerName {
            get { return _manufacturerName; }
            set { _manufacturerName = value != null && value.Length > 128 ? value.Substring(0, 128) : value; }
        }

        [DataMember(Name = "modelName", EmitDefaultValue = false, Order = 4)]
        private string _modelName;
        public string modelName {
            get { return _modelName; }
            set { _modelName = value != null && value.Length > 128 ? value.Substring(0, 128) : value; }
        }

        [DataMember(Name = "applianceId", Order = 5)]
        private string _applianceId;
        public string applianceId {

            get { return _applianceId; }
            set { _applianceId = value != null && value.Length > 256 ? value.Substring(0, 256) : value; }
        }

        [DataMember(Name = "applianceTypes", EmitDefaultValue = false, Order = 6)]
        public List<string> applianceTypes { get; set; }

        // If present in on the Premise object, will end up as the first entry in appliancetypes[]
        [DataMember(Name = "applianceType", EmitDefaultValue = false, Order = 7)]
        private string _applianceType;
        public string applianceType {
            get { return _applianceType; }
            set { _applianceType = value != null && value.Length > 128 ? value.Substring(0, 128) : value; }
        }

        [DataMember(Name = "actions", EmitDefaultValue = false, Order = 8)]
        public List<string> actions { get; set; }

        [DataMember(Name = "additionalApplianceDetails", EmitDefaultValue = false, Order = 9)]
        public AdditionalApplianceDetails additionalApplianceDetails { get; set; }

        [DataMember(Name = "isReachable", EmitDefaultValue = true, Order = 10)]
        public bool isReachable { get; set; }

        [DataMember(Name = "version", EmitDefaultValue = false, Order = 11)]
        private string _version;
        public string version {
            get { return _version; }
            set { _version = value != null && value.Length > 128 ? value.Substring(0, 128) : value; }
        }

        public static int CompareByFriendlyName(Appliance appliance1, Appliance appliance2) {
            return String.Compare(appliance1.friendlyName, appliance2.friendlyName, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    #endregion

    #region Control

    [DataContract(Name = "previousState", Namespace = "Alexa.ConnectedHome.Control")]
    public class AppliancePreviousState
    {
        [DataMember(Name = "mode", EmitDefaultValue = false)]
        public ApplianceValue mode;
        [DataMember(Name = "targetTemperature", EmitDefaultValue = false)]
        public ApplianceValue targetTemperature;
    }

    [DataContract(Name = "color", Namespace = "Alexa.ConnectedHome.Control")]
    public class ApplianceColorValue
    {
        [DataMember(Name = "hue", EmitDefaultValue = false, Order = 1)]
        public double hue;
        [DataMember(Name = "saturation", EmitDefaultValue = false, Order = 2)]
        public double saturation;
        [DataMember(Name = "brightness", EmitDefaultValue = false, Order = 3)]
        public double brightness;
    }

    [DataContract(Name = "colorTemperature", Namespace = "Alexa.ConnectedHome.Control")]
    public class ApplianceColorTemperatureValue
    {
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public int value;
    }

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.Control")]
    public class ApplianceControlRequestPayload
    {
        [DataMember(Name = "accessToken", Order = 1)]
        public string accessToken { get; set; }

        [DataMember(Name = "appliance", Order = 2)]
        public ApplianceIdPayload appliance;

        [DataMember(Name = "targetTemperature", EmitDefaultValue = false)]
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

        [DataMember(Name = "color", EmitDefaultValue = false)]
        public ApplianceColorValue color;

        [DataMember(Name = "colorTemperature", EmitDefaultValue = false)]
        public ApplianceColorTemperatureValue colorTemperature;

    }

    [DataContract(Name = "achievedState", Namespace = "Alexa.ConnectedHome.Control")]
    public class AchievedState
    {
        [DataMember(Name = "color", EmitDefaultValue = false)]
        public ApplianceColorValue color;

        [DataMember(Name = "colorTemperature", EmitDefaultValue = false)]
        public ApplianceColorTemperatureValue colorTemperature;
    }

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.Control")]
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

        [DataMember(Name = "achievedState", EmitDefaultValue = false, Order = 3)]
        public AchievedState achievedState{ get; set; }

    }

    #endregion

    #region Query


    [DataContract(Name = "temperatureReading", Namespace = "Alexa.ConnectedHome.Query")]
    public class ApplianceTemperatureReading
    {
        [DataMember(Name = "value", EmitDefaultValue = false, Order = 1)]
        public double value;
        [DataMember(Name = "scale", EmitDefaultValue = false, Order = 2 )]
        public string scale;
    }

    [DataContract(Name = "temperatureMode", Namespace = "Alexa.ConnectedHome.Query")]
    public class ApplianceTemperatureMode
    {
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public string value;
        [DataMember(Name = "friendlyName", EmitDefaultValue = false)]
        public string friendlyName;
    }

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.Query")]
    public class ApplianceQueryRequestPayload
    {
        [DataMember(Name = "accessToken", Order = 1)]
        public string accessToken { get; set; }

        [DataMember(Name = "appliance", Order = 2)]
        public ApplianceIdPayload appliance;

    }

    [DataContract(Name = "payload", Namespace = "Alexa.ConnectedHome.Query")]
    public class ApplianceQueryResponsePayload
    {

        [DataMember(Name = "temperatureReading", EmitDefaultValue = false, Order = 1)]
        public ApplianceTemperatureReading temperatureReading;

        [DataMember(Name = "targetTemperature", EmitDefaultValue = false)]
        public ApplianceTemperatureReading targetTemperature;

        [DataMember(Name = "coolingTargetTemperature", EmitDefaultValue = false)]
        public ApplianceTemperatureReading coolingTargetTemperature;

        [DataMember(Name = "heatingTargetTemperature", EmitDefaultValue = false)]
        public ApplianceTemperatureReading heatingTargetTemperature;

        [DataMember(Name = "temperatureMode", EmitDefaultValue = false)]
        public ApplianceTemperatureMode temperatureMode;

        [DataMember(Name = "uri", EmitDefaultValue = false)]
        public ApplianceValue uri;

        [DataMember(Name = "applianceResponseTimestamp", EmitDefaultValue = false, Order = 7)]
        public string applianceResponseTimestamp;

        [DataMember(Name = "exception", EmitDefaultValue = false, Order = 8)]
        public ExceptionResponsePayload exception { get; set; }

    }

    #endregion

}
