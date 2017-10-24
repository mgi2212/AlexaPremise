using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using System;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Scene
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
    }

    #endregion

    public class AlexaSetSceneController : AlexaControllerBase<
        AlexaSetSceneRequestPayload,
        ControlResponse,
        AlexaSetSceneControllerRequest>, IAlexaController
    {
        public readonly AlexaScene PropertyHelpers;
        public readonly string @namespace = "Alexa.SceneController";
        public readonly string[] directiveNames = { "Activate", "Deactivate" };
        private readonly string[] alexaProperties = { "" };
        public readonly string premiseProperty = "PowerState";

        public AlexaSetSceneController(AlexaSetSceneControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaScene();
        }

        public AlexaSetSceneController(string value, IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaScene();
        }

        public AlexaSetSceneController()
            : base()
        {
            PropertyHelpers = new AlexaScene();
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

        public string[] GetDirectiveNames()
        {
            return directiveNames;
        }

        public bool HasAlexaProperty(string property)
        {
            return false;
        }
        public bool HasPremiseProperty(string property)
        {
            return false;
        }

        public string AssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public AlexaProperty GetPropertyState()
        {
            bool powerState = endpoint.GetValue<bool>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                value = (powerState == true ? "ActivationStarted" : "DeactivationStarted"),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public AlexaChangeReport AlterChangeReport(AlexaChangeReport report)
        {
            AlexaProperty prop = this.GetPropertyState();
            report.context.propertiesInternal = null;
            report.@event.payload.change = null;
            report.@event.header.@namespace = @namespace;
            report.@event.payload.cause = new ChangeReportCause();
            report.@event.payload.cause.type = "APP_INTERACTION";
            report.@event.header.name = (string)prop.value;
            report.@event.payload.timestamp = prop.timeOfSample;
            return report;
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
                this.Response.context.properties = null;
                this.Response.Event.payload.cause = new ChangeReportCause();
                this.Response.Event.payload.cause.type = "VOICE_INTERACTION";
                this.Response.Event.payload.timestamp = GetUtcTime();
                this.Response.Event.endpoint.cookie.path = endpoint.GetPath().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                base.ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return;
            }
        }
    }
}