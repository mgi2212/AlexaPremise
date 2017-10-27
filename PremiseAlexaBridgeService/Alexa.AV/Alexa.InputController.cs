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
    public class AlexaInputControlerRequestDirective
    {
        #region Constructors

        public AlexaInputControlerRequestDirective()
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
    public class AlexaInputControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaInputControlerRequestDirective directive { get; set; }

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
            return GetType().AssemblyQualifiedName;
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

        public AlexaProperty GetPropertyState()
        {
            return null;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            List<AlexaProperty> properties = new List<AlexaProperty>();

            AlexaProperty volumeProperty = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[0],
                value = GetCurrentInput(),
                timeOfSample = GetUtcTime()
            };
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

        public void ProcessControllerDirective()
        {
            IPremiseObject switcher = GetSwitcherZone();

            Dictionary<string, IPremiseObject> inputs = GetAllInputs(switcher);

            if (inputs.ContainsKey(Request.directive.payload.input))
            {
                // swtich inputs
                IPremiseObject newInput = inputs[Request.directive.payload.input];
                string newInputId = newInput.GetObjectID().GetAwaiter().GetResult();
                switcher.SetValue("CurrentSource", newInputId).GetAwaiter().GetResult();
                Response.context.properties.AddUnique(GetPropertyStates());
                Response.Event.header.name = "Response";
            }
            else
            {
                ReportError(AlexaErrorTypes.INVALID_VALUE, "Input not found!");
            }
        }

        private Dictionary<string, IPremiseObject> GetAllInputs(IPremiseObject switcher)
        {
            Dictionary<string, IPremiseObject> inputs = new Dictionary<string, IPremiseObject>();
            if (switcher == null)
                return inputs;

            // The bound object of the switcher should be the ouput zone object at the device level
            IPremiseObject boundObject = switcher.GetRefValue("BoundObject").GetAwaiter().GetResult();
            // the parent should be the actual switcher
            IPremiseObject actualSwitcher = boundObject.GetParent().GetAwaiter().GetResult();
            // walk through the children looking for inputs
            foreach (IPremiseObject child in actualSwitcher.GetChildren().GetAwaiter().GetResult())
            {
                if (child.IsOfType(PremiseServer.AlexaAudioVideoInput).GetAwaiter().GetResult())
                {
                    string inputName = GetInputName(child);
                    inputs.Add(inputName, child);
                }
            }
            return inputs;
        }

        private string GetCurrentInput()
        {
            IPremiseObject switcher = GetSwitcherZone();
            if (switcher == null)
                return "";
            IPremiseObject currentInput = switcher.GetRefValue("CurrentSource").GetAwaiter().GetResult();

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

            foreach (IPremiseObject child in this.Endpoint.GetChildren().GetAwaiter().GetResult())
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