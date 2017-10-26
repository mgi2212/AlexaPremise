using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.Lighting
{
    #region SetBrightness Data Contracts

    [DataContract]
    public class AlexaSetBrightnessControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaSetBrightnessControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSetBrightnessControllerRequestDirective
    {
        #region Constructors

        public AlexaSetBrightnessControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSetBrightnessRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSetBrightnessRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSetBrightnessRequestPayload
    {
        #region Properties

        [DataMember(Name = "brightness")]
        public int brightness { get; set; }

        #endregion Properties
    }

    #endregion SetBrightness Data Contracts

    public class AlexaSetBrightnessController : AlexaControllerBase<
        AlexaSetBrightnessRequestPayload,
        ControlResponse,
        AlexaSetBrightnessControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaLighting PropertyHelpers;
        private readonly string @namespace = "Alexa.BrightnessController";
        private readonly string[] alexaProperties = { "brightness" };
        private readonly string[] directiveNames = { "SetBrightness" };
        private readonly string[] premiseProperties = { "Brightness" };

        #endregion Fields

        #region Constructors

        public AlexaSetBrightnessController(AlexaSetBrightnessControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaSetBrightnessController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaSetBrightnessController()
        {
            PropertyHelpers = new AlexaLighting();
        }

        #endregion Constructors

        #region Methods

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
            return premiseProperties;
        }

        public AlexaProperty GetPropertyState()
        {
            double brightness = Endpoint.GetValue<double>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                value = (int)((brightness * 100)).LimitToRange(0, 100),
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
            return (alexaProperties.Contains(property));
        }

        public bool HasPremiseProperty(string property)
        {
            foreach (string s in premiseProperties)
            {
                if (s == property)
                    return true;
            }
            return false;
        }

        public void ProcessControllerDirective()
        {
            AlexaProperty property = new AlexaProperty(Header)
            {
                name = alexaProperties[0]
            };
            Response.Event.header.@namespace = "Alexa";

            try
            {
                double setValue = (Payload.brightness / 100.00).LimitToRange(0.00, 1.000);
                Endpoint.SetValue(premiseProperties[0], setValue.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = ((int)(setValue * 100)).LimitToRange(0, 100);
                Response.context.properties.Add(property);
                Response.Event.header.name = "Response";
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, @namespace));
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }

        #endregion Methods
    }
}