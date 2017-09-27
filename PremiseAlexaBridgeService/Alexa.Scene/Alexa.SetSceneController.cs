using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.SceneController
{
    #region Scene Data Contracts

    [DataContract]
    public class AlexaSetSceneControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaSetSceneControllerRequestDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaSetSceneControllerRequestDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetSceneRequestPayload payload { get; set; }

        public AlexaSetSceneControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetSceneRequestPayload();
        }
    }
    [DataContract]
    public class AlexaSetSceneRequestPayload : object
    {
        [DataMember(Name ="cause", IsRequired = true, EmitDefaultValue = true, Order = 1)]
        public ChangeReportCause cause;
        [DataMember(Name = "timestamp", IsRequired = true, EmitDefaultValue = true, Order = 2)]
        public string timestamp;
    }

    #endregion

    public class AlexaSetSceneController : AlexaControllerBase<
        AlexaSetSceneRequestPayload, 
        ControlResponse, 
        AlexaSetSceneControllerRequest>, IAlexaController
    {
        public readonly string @namespace = "Alexa.SceneController";
        public readonly string[] directiveNames = { "Activate", "Deactivate" };
        public readonly string premiseProperty = "PowerState";

        public AlexaSetSceneController(AlexaSetSceneControllerRequest request)
            : base(request)
        {
        }

        public AlexaSetSceneController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string[] GetDirectiveNames()
        {
            return directiveNames;
        }

        public AlexaProperty GetPropertyState()
        {
            bool powerState = endpoint.GetValue<bool>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                //name = alexaProperty,
                value = (powerState == true ? "ActivationStarted" : "DeactivationStarted"),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public void ProcessControllerDirective()
        {
            try
            {
                string valueToSend;
                if (header.name == "Activate")
                {
                    valueToSend = "True";
                    this.Response.Event.header.name = "ActivationStarted";
                    this.endpoint.SetValue(premiseProperty, valueToSend).GetAwaiter().GetResult();
                }
                else if (header.name == "Deactivate")
                {
                    valueToSend = "False";
                    this.Response.Event.header.name = "DeactivationStarted";
                    this.endpoint.SetValue(premiseProperty, valueToSend).GetAwaiter().GetResult();
                }
                else
                {
                    base.ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                    return;
                }

                this.endpoint.SetValue(premiseProperty, valueToSend).GetAwaiter().GetResult();
                payload.cause = new ChangeReportCause();
                payload.cause.type = "RULE_TRIGGER";
                payload.timestamp = GetUtcTime();
            }
            catch (Exception ex)
            {
                base.ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return;
            }
        }
    }
}