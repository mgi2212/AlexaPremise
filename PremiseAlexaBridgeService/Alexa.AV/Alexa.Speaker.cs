using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;
using System.Globalization;

namespace Alexa.AV
{
    #region Data Contracts

    [DataContract]
    public class AlexaSpeakerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaSpeakerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSpeakerRequestDirective
    {
        #region Constructors

        public AlexaSpeakerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaSpeakerRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaSpeakerRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaSpeakerRequestPayload
    {
        #region Properties

        [DataMember(Name = "mute", EmitDefaultValue = false)]
        public bool mute { get; set; }

        [DataMember(Name = "volume", EmitDefaultValue = false)]
        public int volume { get; set; }

        [DataMember(Name = "volumeDefault", EmitDefaultValue = false)]
        public bool volumeDefault { get; set; }

        #endregion Properties
    }

    #endregion Data Contracts

    internal class AlexaSpeaker : AlexaControllerBase<
            AlexaSpeakerRequestPayload,
            ControlResponse,
            AlexaSpeakerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaAV PropertyHelpers;
        private const string Namespace = "Alexa.Speaker";
        private readonly string[] _alexaProperties = { "volume", "muted" };
        private readonly string[] _directiveNames = { "SetVolume", "AdjustVolume", "SetMute" };
        private readonly string[] _premiseProperties = { "Volume", "Mute" };

        #endregion Fields

        #region Constructors

        public AlexaSpeaker()
        {
            PropertyHelpers = new AlexaAV();
        }

        public AlexaSpeaker(IPremiseObject endpoint)
                 : base(endpoint)
        {
            PropertyHelpers = new AlexaAV();
        }

        public AlexaSpeaker(AlexaSpeakerRequest request)
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

            double volume = Endpoint.GetValue<double>("Volume").GetAwaiter().GetResult();
            AlexaProperty volumeProperty = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[0],
                value = (int)((volume * 100)).LimitToRange(0, 100),
                timeOfSample = PremiseServer.GetUtcTime()
            };
            properties.Add(volumeProperty);

            bool mute = Endpoint.GetValue<bool>("Mute").GetAwaiter().GetResult();
            AlexaProperty muteProperty = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[1],
                value = mute,
                timeOfSample = PremiseServer.GetUtcTime()
            };
            properties.Add(muteProperty);

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
            int x = 0;
            foreach (string property in _premiseProperties)
            {
                if (premiseProperty == property)
                {
                    return _alexaProperties[x];
                }
                x++;
            }
            return "";
        }

        public void ProcessControllerDirective()
        {
            Response.Event.header.@namespace = "Alexa";

            try
            {
                switch (Header.name)
                {
                    case "AdjustVolume":
                        double adjustValue = Math.Round((Payload.volume / 100.00), 2).LimitToRange(-1.00, 1.00);
                        double currentValue = Math.Round(Endpoint.GetValue<double>("Volume").GetAwaiter().GetResult(), 2);
                        double valueToSend = Math.Round(currentValue + adjustValue, 2).LimitToRange(0.00, 1.00);
                        Endpoint.SetValue(_premiseProperties[0], valueToSend.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                        break;

                    case "SetVolume":
                        double setValue = (Payload.volume / 100.00).LimitToRange(0.00, 1.000);
                        Endpoint.SetValue("Volume", setValue.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                        break;

                    case "SetMute":
                        bool mute = Payload.mute;
                        Endpoint.SetValue("Mute", mute.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                        break;

                    default:
                        ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                        return;
                }
                Response.Event.header.name = "Response";
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, ""));
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