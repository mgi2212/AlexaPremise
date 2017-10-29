using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;

namespace Alexa.Scene
{
    #region Scene Data Contracts

    [DataContract]
    public class AlexaSetSceneControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaSetSceneControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSetSceneControllerRequestDirective
    {
        #region Constructors

        public AlexaSetSceneControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetSceneRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetSceneRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSetSceneRequestPayload : object
    {
    }

    #endregion Scene Data Contracts

    public class AlexaSetSceneController : AlexaControllerBase<
        AlexaSetSceneRequestPayload,
        ControlResponse,
        AlexaSetSceneControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly string @namespace = "Alexa.SceneController";
        public readonly string[] directiveNames = { "Activate", "Deactivate" };
        public readonly string premiseProperty = "PowerState";
        public readonly AlexaScene PropertyHelpers;
        private readonly string[] alexaProperties = { "" };

        #endregion Fields

        #region Constructors

        public AlexaSetSceneController(AlexaSetSceneControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaScene();
        }

        public AlexaSetSceneController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaScene();
        }

        public AlexaSetSceneController()
        {
            PropertyHelpers = new AlexaScene();
        }

        #endregion Constructors

        #region Methods

        public AlexaChangeReport AlterChangeReport(AlexaChangeReport report)
        {
            AlexaProperty prop = GetPropertyState();
            report.context.propertiesInternal = null;
            report.@event.payload.change = null;
            report.@event.header.@namespace = @namespace;
            report.@event.payload.cause = new ChangeReportCause();
            report.@event.payload.cause.type = "APP_INTERACTION";
            report.@event.header.name = (string)prop.value;
            report.@event.payload.timestamp = prop.timeOfSample;
            return report;
        }

        public string AssemblyTypeName()
        {
            return GetType().AssemblyQualifiedName;
        }

        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public string GetAssemblyTypeName()
        {
            return GetType().AssemblyQualifiedName;
        }

        public string[] GetDirectiveNames()
        {
            return directiveNames;
        }

        public string GetNameSpace()
        {
            return @namespace;
        }

        public string[] GetPremiseProperties()
        {
            return null;
        }

        public AlexaProperty GetPropertyState()
        {
            bool powerState = Endpoint.GetValue<bool>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                value = (powerState ? "ActivationStarted" : "DeactivationStarted"),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            return null;
        }

        public bool HasAlexaProperty(string property)
        {
            return false;
        }

        public bool HasPremiseProperty(string property)
        {
            return false;
        }

        public void ProcessControllerDirective()
        {
            try
            {
                string valueToSend;
                if (Header.name == "Activate")
                {
                    valueToSend = "True";
                    Response.Event.header.name = "ActivationStarted";
                }
                else if (Header.name == "Deactivate")
                {
                    valueToSend = "False";
                    Response.Event.header.name = "DeactivationStarted";
                }
                else
                {
                    ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                    return;
                }

                Endpoint.SetValue(premiseProperty, valueToSend).GetAwaiter().GetResult();
                Response.context.properties = null;
                Response.Event.header.@namespace = "Alexa.SceneController";
                Response.Event.payload.cause = new ChangeReportCause { type = "VOICE_INTERACTION" };
                Response.Event.payload.timestamp = GetUtcTime();
                Response.Event.endpoint.cookie.path = Endpoint.GetPath().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }

        #endregion Methods
    }
}