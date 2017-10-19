using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using System;
using System.Linq;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Power
{
    #region PowerState Data Contracts

    [DataContract]
    public class AlexaSetPowerStateControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetPowerStateControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetPowerStateControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetPowerStateRequestPayload payload { get; set; }

        public AlexaSetPowerStateControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetPowerStateRequestPayload();
        }
    }

    public class AlexaSetPowerStateRequestPayload 
    {

    }

    #endregion

    public class AlexaSetPowerStateController : AlexaControllerBase<
        AlexaSetPowerStateRequestPayload, 
        ControlResponse, 
        AlexaSetPowerStateControllerRequest>, IAlexaController
    {
        public readonly AlexaPower PropertyHelpers;
        private readonly string[] directiveNames = { "TurnOn", "TurnOff" };
        private readonly string @namespace = "Alexa.PowerController";
        private readonly string[] premiseProperties = { "PowerState" };
       private readonly string[] alexaProperties = { "powerState" };

        public AlexaSetPowerStateController(AlexaSetPowerStateControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaPower();
        }

        public AlexaSetPowerStateController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaPower();
        }

        public AlexaSetPowerStateController()
            : base()
        {
            PropertyHelpers = new AlexaPower();
        }

        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public string GetAssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string [] GetDirectiveNames()
        {
            return directiveNames;
        }

        public bool HasAlexaProperty(string property)
        {
            return ( this.alexaProperties.Contains(property));
        }

        public bool HasPremiseProperty(string property)
        {
            foreach (string s in this.premiseProperties)
            {
                if (s == property)
                    return true;
            }
            return false;
        }


        public string AssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public AlexaProperty GetPropertyState()
        {
            bool powerState = endpoint.GetValue<bool>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                value = (powerState == true ? "ON" : "OFF"),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
            AlexaProperty property = new AlexaProperty(header)
            {
                name = alexaProperties[0]
            };

            response.Event.header.@namespace = "Alexa";

            try
            {
                string valueToSend;
                if (header.name == "TurnOff")
                {
                    valueToSend = "False";
                    property.value = "OFF";
                }
                else if (header.name == "TurnOn")
                {
                    valueToSend = "True";
                    property.value = "ON";
                }
                else
                {
                    base.ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                    return;
                }

                this.endpoint.SetValue(premiseProperties[0], valueToSend).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                this.response.context.properties.Add(property);
                this.response.context.properties.AddRange(this.PropertyHelpers.FindRelatedProperties(endpoint, @namespace));
            }
            catch (Exception ex)
            {
                base.ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return;
            }

            this.Response.Event.header.name = "Response";
        }
    }
}