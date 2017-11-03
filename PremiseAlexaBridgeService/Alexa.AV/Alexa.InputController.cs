using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.AV
{
    #region Data Contracts

    [DataContract]
    public class AlexaInputControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaInputControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaInputControllerRequestDirective
    {
        #region Constructors

        public AlexaInputControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaInputControllerRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaInputControllerRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaInputControllerRequestPayload
    {
        #region Properties

        [DataMember(Name = "input")]
        public string input { get; set; }

        #endregion Properties
    }

    #endregion Data Contracts

    internal class AlexaInputController : AlexaControllerBase<
            AlexaInputControllerRequestPayload,
            ControlResponse,
            AlexaInputControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaAV PropertyHelpers;
        private const string Namespace = "Alexa.InputController";
        private readonly string[] _alexaProperties = { "input" };
        private readonly string[] _directiveNames = { "SelectInput" };
        private readonly string[] _premiseProperties = { }; // proactive state updates not supported now

        #endregion Fields

        #region Constructors

        public AlexaInputController()
        {
            PropertyHelpers = new AlexaAV();
        }

        public AlexaInputController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaAV();
        }

        public AlexaInputController(AlexaInputControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaAV();
        }

        #endregion Constructors

        #region Methods

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
            return _premiseProperties;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            List<AlexaProperty> properties = new List<AlexaProperty>();

            AlexaProperty volumeProperty = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[0],
                value = GetCurrentInput(),
                timeOfSample = PremiseServer.GetUtcTime()
            };
            if ((string)volumeProperty.value == "")
            {
                volumeProperty.value = "NONE";
            }
            properties.Add(volumeProperty);

            return properties;
        }

        public bool HasAlexaProperty(string property)
        {
            return (_alexaProperties.Contains(property));
        }

        public bool HasPremiseProperty(string property)
        {
            return (_premiseProperties.Contains(property));
        }

        public string MapPremisePropertyToAlexaProperty(string premiseProperty)
        {
            return _premiseProperties.Contains(premiseProperty) ? "input" : "";
        }

        public void ProcessControllerDirective()
        {
            IPremiseObject switcher = GetSwitcherZone();

            if (switcher == null || !switcher.IsValidObject())
            {
                string name = Endpoint.GetPath().GetAwaiter().GetResult();
                ReportError(AlexaErrorTypes.NO_SUCH_ENDPOINT, $"No switcher child object in MediaZone at {name}.");
                return;
            }

            Dictionary<string, IPremiseObject> inputs = GetAllInputs(switcher);

            if (inputs.ContainsKey(Request.directive.payload.input.ToUpper()))
            {
                // switch inputs
                IPremiseObject newInput = inputs[Request.directive.payload.input];
                string newInputId = newInput.GetObjectID().GetAwaiter().GetResult();
                switcher.SetValue("CurrentSource", newInputId).GetAwaiter().GetResult();
                Response.Event.header.name = "Response";
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, ""));
            }
            else
            {
                ReportError(AlexaErrorTypes.INVALID_VALUE, $"Input {Request.directive.payload.input} not found!");
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

        private Dictionary<string, IPremiseObject> GetAllInputs(IPremiseObject switcher)
        {
            Dictionary<string, IPremiseObject> inputs = new Dictionary<string, IPremiseObject>();
            if (switcher == null)
                return inputs;

            // The bound object of the switcher should be the output zone object at the device level
            IPremiseObject boundObject = switcher.GetRefValue("BoundObject").GetAwaiter().GetResult();
            if (!boundObject.IsValidObject())
            {
                string path = switcher.GetPath().GetAwaiter().GetResult();
                ReportError(AlexaErrorTypes.ENDPOINT_UNREACHABLE, $"No device bound to switcher at {path}.");
                return inputs;
            }

            // the parent should be the actual switcher
            IPremiseObject actualSwitcher = boundObject.GetParent().GetAwaiter().GetResult();
            if (!actualSwitcher.IsValidObject())
            {
                string path = boundObject.GetPath().GetAwaiter().GetResult();
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, $"Unexpected parent at {path}.");
                return inputs;
            }

            // walk through the children looking for inputs
            foreach (IPremiseObject child in actualSwitcher.GetChildren().GetAwaiter().GetResult())
            {
                if (child.IsOfType(PremiseServer.AlexaAudioVideoInput).GetAwaiter().GetResult())
                {
                    string inputName = GetInputName(child).ToUpper();
                    inputs.Add(inputName, child);
                }
            }
            return inputs;
        }

        private string GetCurrentInput()
        {
            IPremiseObject switcher = GetSwitcherZone();
            if (!switcher.IsValidObject())
            {
                return "";
            }

            IPremiseObject currentInput = switcher.GetRefValue("CurrentSource").GetAwaiter().GetResult();
            if (!currentInput.IsValidObject())
            {
                return "";
            }
            return GetInputName(currentInput);
        }

        private string GetInputName(IPremiseObject input)
        {
            string inputName = input.GetValue<string>("AlexaInputName").GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(inputName))
            {
                // if the AlexaInputName isn't set then use the object name.
                inputName = input.GetName().GetAwaiter().GetResult();
            }
            return inputName;
        }

        private IPremiseObject GetSwitcherZone()
        {
            IPremiseObject switcher = null;

            foreach (IPremiseObject child in Endpoint.GetChildren().GetAwaiter().GetResult())
            {
                // there should be an object of type matrix switcher zone as a child
                if (child.IsOfType(PremiseServer.AlexaMatrixSwitcherZone).GetAwaiter().GetResult())
                {
                    switcher = child;
                    break;
                }
            }
            return switcher;
        }

        #endregion Methods
    }
}