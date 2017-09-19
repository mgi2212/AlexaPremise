using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using SYSWebSockClient;

namespace Alexa.SmartHome.V3
{

    public enum EndpointDisplayCategories
    {
        LIGHT,
        SMARTPLUG,
        SWITCH,
        CAMERA,
        DOOR,
        THERMOSTAT,
        SMARTLOCK,
        SCENE_TRIGGER,
        ACTIVITY_TRIGGER,
        OTHER
    }

    public static class DiscoveryUtilities
    {
        #region Capabilities

        const bool SUPPORT_ASYNC_REPORTING = false;
        const bool SUPPORT_QUERY = true;

        public static class Capabilities
        {

            public static Capability PowerController()
            {
                Capability capability = new Capability("Alexa.PowerController", SUPPORT_ASYNC_REPORTING, SUPPORT_QUERY);
                capability.properties.supported.Add(new SupportedProperty("powerState"));
                return capability;
            }

            public static Capability BrightnessController()
            {
                Capability capability = new Capability("Alexa.BrightnessController", SUPPORT_ASYNC_REPORTING, SUPPORT_QUERY);
                capability.properties.supported.Add(new SupportedProperty("brightness"));
                return capability;
            }

            public static Capability ColorController()
            {
                Capability capability = new Capability("Alexa.ColorController", SUPPORT_ASYNC_REPORTING, SUPPORT_QUERY);
                capability.properties.supported.Add(new SupportedProperty("color"));
                return capability;
            }

            public static Capability ColorTemperatureController()
            {
                Capability capability = new Capability("Alexa.ColorTemperatureController", SUPPORT_ASYNC_REPORTING, SUPPORT_QUERY);
                capability.properties.supported.Add(new SupportedProperty("colorTemperatureInKelvin"));
                return capability;
            }

            public static Capability TemperatureSensor()
            {
                Capability capability = new Capability("Alexa.TemperatureSensor", SUPPORT_ASYNC_REPORTING, SUPPORT_QUERY);
                capability.properties.supported.Add(new SupportedProperty("temperature"));
                return capability;
            }

            public static Capability ThermostatController()
            {
                Capability capability = new Capability("Alexa.ThermostatController", SUPPORT_ASYNC_REPORTING, SUPPORT_QUERY);
                capability.properties.supported.Add(new SupportedProperty("lowerSetpoint"));
                capability.properties.supported.Add(new SupportedProperty("targetSetpoint"));
                capability.properties.supported.Add(new SupportedProperty("upperSetpoint"));
                capability.properties.supported.Add(new SupportedProperty("thermostatMode"));
                return capability;
            }

            public static Capability LockController()
            {
                Capability capability = new Capability("Alexa.LockController", SUPPORT_ASYNC_REPORTING, SUPPORT_QUERY);
                capability.properties.supported.Add(new SupportedProperty("lockState"));
                return capability;
            }

        }
        #endregion

        #region ErrorResponses

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


        public static class ErrorResponses
        {


        }

        #endregion
    }


    [DataContract(Name = "Brightness", Namespace = "Alexa.BrightnessController")]
    public class BrightnessValue
    {
        [DataMember(Name = "brightness", EmitDefaultValue = true)]
        public int brightness { get; set; }
    }


    [DataContract(Name = "Color", Namespace = "Alexa.ConnectedHome.Control")]
    public class ColorValue
    {
        [DataMember(Name = "hue", EmitDefaultValue = false, Order = 1)]
        public double hue { get; set; }
        [DataMember(Name = "saturation", EmitDefaultValue = false, Order = 2)]
        public double saturation { get; set; }
        [DataMember(Name = "brightness", EmitDefaultValue = false, Order = 3)]
        public double brightness { get; set; }
    }

    [DataContract(Name = "ColorTemperature", Namespace = "Alexa.ColorController")]
    public class ColorTemperatureValue
    {
        [DataMember(Name = "colorTemperatureInKelvin", EmitDefaultValue = false)]
        public int colorTemperatureInKelvin { get; set; }
    }

}
