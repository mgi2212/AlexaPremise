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
    #region SetColorTemperature Data Contracts

    [DataContract]
    public class AlexaColorTemperatureControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaColorTemperatureControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaColorTemperatureControllerRequestDirective
    {
        #region Constructors

        public AlexaColorTemperatureControllerRequestDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaColorTemperatureControllerRequestPayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaColorTemperatureControllerRequestPayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaColorTemperatureControllerRequestPayload
    {
        #region Properties

        [DataMember(Name = "colorTemperatureInKelvin")]
        public int colorTemperatureInKelvin { get; set; }

        #endregion Properties
    }

    #endregion SetColorTemperature Data Contracts

    public class AlexaColorTemperatureController : AlexaControllerBase<
        AlexaColorTemperatureControllerRequestPayload,
        ControlResponse,
        AlexaColorTemperatureControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaLighting PropertyHelpers;

        private const string Namespace = "Alexa.ColorTemperatureController";

        // corresponds to warm white, soft white, white, daylight, cool white
        private static readonly int[] ColorTable = { 2200, 2700, 4000, 5500, 7000 };

        private readonly string[] _alexaProperties = { "colorTemperatureInKelvin" };
        private readonly string[] _directiveNames = { "SetColorTemperature", "DecreaseColorTemperature", "IncreaseColorTemperature" };
        private readonly string[] _premiseProperties = { "Temperature" };

        #endregion Fields

        #region Constructors

        public AlexaColorTemperatureController(AlexaColorTemperatureControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaColorTemperatureController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaColorTemperatureController()
        {
            PropertyHelpers = new AlexaLighting();
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

            double colorTemperature = Endpoint.GetValue<double>(_premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = Namespace,
                name = _alexaProperties[0],
                value = ((int)colorTemperature).LimitToRange(1000, 10000),
                timeOfSample = PremiseServer.GetUtcTime()
            };

            properties.Add(property);

            return properties;
        }

        public bool HasAlexaProperty(string property)
        {
            return (_alexaProperties.Contains(property));
        }

        public bool HasPremiseProperty(string property)
        {
            return _alexaProperties.Contains(property);
        }

        public string MapPremisePropertyToAlexaProperty(string premiseProperty)
        {
            return _premiseProperties.Contains(premiseProperty) ? "colorTemperatureInKelvin" : "";
        }

        public void ProcessControllerDirective()
        {
            try
            {
                switch (Header.name)
                {
                    case "SetColorTemperature":
                        {
                            int setValue = Payload.colorTemperatureInKelvin.LimitToRange(1000, 10000);
                            Endpoint.SetValue(_premiseProperties[0], setValue.ToString()).GetAwaiter().GetResult();
                        }
                        break;

                    case "DecreaseColorTemperature":
                        {
                            int currentValue = (int)(Endpoint.GetValue<double>(_premiseProperties[0]).GetAwaiter().GetResult()).LimitToRange(1000, 10000);
                            int valueToSend = GetPreviousColor(currentValue).LimitToRange(1000, 10000);
                            Endpoint.SetValue(_premiseProperties[0], valueToSend.ToString()).GetAwaiter().GetResult();
                        }
                        break;

                    case "IncreaseColorTemperature":
                        {
                            int currentValue = (int)(Endpoint.GetValue<double>(_premiseProperties[0]).GetAwaiter().GetResult()).LimitToRange(1000, 10000);
                            int valueToSend = GetNextColor(currentValue).LimitToRange(1000, 10000);
                            Endpoint.SetValue(_premiseProperties[0], valueToSend.ToString()).GetAwaiter().GetResult();
                        }
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

        #region Static Methods

        private static int GetNearestValue(int searchValue)
        {
            int nearest = ColorTable.Select(p => new { Value = p, Difference = Math.Abs(p - searchValue) })
                .OrderBy(p => p.Difference)
                .First().Value;
            return nearest;
        }

        private static int GetNextColor(int current)
        {
            current = GetNearestValue(current);
            int i = 0;
            foreach (int value in ColorTable)
            {
                if (current == value)
                    break;
                i++;
            }
            i++;
            //int limit = (ColorTable.Count() - 1);
            i = i.LimitToRange(0, ColorTable.Length - 1);
            return ColorTable[i];
        }

        private static int GetPreviousColor(int current)
        {
            current = GetNearestValue(current);
            int x;
            for (x = ColorTable.Length - 1; x >= 0; x--)
            {
                if (current == ColorTable[x])
                    break;
            }
            x--;
            x = x.LimitToRange(0, ColorTable.Length - 1);
            return ColorTable[x];
        }

        #endregion Static Methods
    }
}