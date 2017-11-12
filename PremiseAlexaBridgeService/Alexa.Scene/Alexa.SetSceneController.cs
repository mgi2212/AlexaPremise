using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;
using PremiseAlexaBridgeService;

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

        public readonly AlexaScene PropertyHelpers;
        private const string Namespace = "Alexa.SceneController";
        private readonly string[] _alexaProperties = { "" };
        private readonly string[] _directiveNames = { "Activate", "Deactivate" };
        private readonly string[] _premiseProperties = { "PowerState" };

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
            AlexaProperty prop = GetPropertyStates()[0];
            report.context.propertiesInternal = null;
            report.@event.payload.change = null;
            report.@event.header.@namespace = Namespace;
            report.@event.payload.cause = new ChangeReportCause { type = "APP_INTERACTION" };
            report.@event.header.name = (string)prop.value;
            report.@event.payload.timestamp = prop.timeOfSample;
            return report;
        }

        public string[] GetAlexaProperties()
        {
            return _alexaProperties;
        }

        public string GetAssemblyTypeName()
        {
            return PropertyHelpers.GetType().AssemblyQualifiedName;
        }

        public string[] GetDirectiveNames()
        {
            return _directiveNames;
        }

        public string GetNameSpace()
        {
            return Namespace;
        }

        public string[] GetPremiseProperties()
        {
            return null;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            List<AlexaProperty> properties = new List<AlexaProperty>();

            bool powerState = Endpoint.GetValueAsync<bool>(_premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = Namespace,
                value = (powerState ? "ActivationStarted" : "DeactivationStarted"),
                timeOfSample = PremiseServer.UtcTimeStamp()
            };

            properties.Add(property);

            return properties;
        }

        public bool HasAlexaProperty(string property)
        {
            return false;
        }

        public bool HasPremiseProperty(string property)
        {
            return false;
        }

        public string MapPremisePropertyToAlexaProperty(string premiseProperty)
        {
            throw new NotImplementedException();
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

                Endpoint.SetValueAsync(_premiseProperties[0], valueToSend).GetAwaiter().GetResult();
                Response.context.properties = null;
                Response.Event.header.@namespace = "Alexa.SceneController";
                Response.Event.payload.cause = new ChangeReportCause { type = "VOICE_INTERACTION" };
                Response.Event.payload.timestamp = PremiseServer.UtcTimeStamp();
                Response.Event.endpoint.cookie.path = Endpoint.GetPathAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }

        public void SetEndpoint(IPremiseObject premiseObject)
        {
            Endpoint = premiseObject;
        }

        public bool ValidateDirective()
        {
            return ValidateDirective(GetDirectiveNames(), GetNameSpace());
        }

        #endregion Methods
    }
}