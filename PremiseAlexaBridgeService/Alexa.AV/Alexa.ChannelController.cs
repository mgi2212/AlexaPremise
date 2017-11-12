using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;

namespace Alexa.AV
{
    #region Data Contracts

    [DataContract]
    public class AlexaChannelControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaChannelControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaChannelControllerRequestDirective
    {
        #region Constructors

        public AlexaChannelControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaChannelControllerRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaChannelControllerRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaChannelControllerRequestPayload
    {
    }

    #endregion Data Contracts

    internal class AlexaChannelController : AlexaControllerBase<
                    AlexaChannelControllerRequestPayload,
            ControlResponse,
            AlexaChannelControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaAV PropertyHelpers;
        private readonly string[] _alexaProperties = { "channel" };
        private readonly string[] _directiveNames = { "ChangeChannel", "SkipChannels" };
        private readonly string _namespace = "Alexa.ChannelController";
        private readonly string[] _premiseProperties = { "", "", "" };

        #endregion Fields

        #region Constructors

        public AlexaChannelController()
        {
            PropertyHelpers = new AlexaAV();
        }

        public AlexaChannelController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaAV();
        }

        public AlexaChannelController(AlexaChannelControllerRequest request)
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
            return _namespace;
        }

        public string[] GetPremiseProperties()
        {
            return _premiseProperties;
        }

        public List<AlexaProperty> GetPropertyStates()
        {
            throw new NotImplementedException();
        }

        public List<AlexaProperty> GetPropertyStates(IPremiseObject premiseObject)
        {
            Endpoint = premiseObject;
            List<AlexaProperty> properties = new List<AlexaProperty>();

            //double volume = this.endpoint.GetValue<double>(_premiseProperties[0]).GetAwaiter().GetResult();
            //AlexaProperty volumeProperty = new AlexaProperty
            //{
            //    @namespace = _namespace,
            //    name = _alexaProperties[0],
            //    value = (int)((volume * 100)).LimitToRange(0, 100),
            //    timeOfSample = UtcTimeStamp()
            //};
            //properties.Add(volumeProperty);

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
            throw new NotImplementedException();
        }

        public void ProcessControllerDirective()
        {
            throw new NotImplementedException();
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