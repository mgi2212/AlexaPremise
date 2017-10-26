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
    #region Adjust ColorTemperature Data Contracts

    [DataContract]
    public class AlexaAdjustColorTemperatureControllerDirective
    {
        #region Constructors

        public AlexaAdjustColorTemperatureControllerDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaAdjustColorTemperaturePayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "payload")]
        public AlexaAdjustColorTemperaturePayload payload { get; set; }

        #endregion Properties
    }

    public class AlexaAdjustColorTemperatureControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaAdjustColorTemperatureControllerDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaAdjustColorTemperaturePayload : object
    {
    }

    #endregion Adjust ColorTemperature Data Contracts

    public class AlexaAdjustColorTemperatureController : AlexaControllerBase<
        AlexaAdjustColorTemperaturePayload,
        ControlResponse,
        AlexaAdjustColorTemperatureControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly AlexaLighting PropertyHelpers;

        // corresponds to warm white, soft white, white, daylight, cool white
        private static int[] colorTable = { 2200, 2700, 4000, 5500, 7000 };

        private readonly string @namespace = "Alexa.ColorTemperatureController";
        private readonly string[] alexaProperties = { "colorTemperatureInKelvin" };
        private readonly string[] directiveNames = { "IncreaseColorTemperature", "DecreaseColorTemperature" };
        private readonly string[] premiseProperties = { "Temperature" };

        #endregion Fields

        #region Constructors

        public AlexaAdjustColorTemperatureController(AlexaAdjustColorTemperatureControllerRequest request)
            : base(request)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaAdjustColorTemperatureController(IPremiseObject endpoint)
            : base(endpoint)
        {
            PropertyHelpers = new AlexaLighting();
        }

        public AlexaAdjustColorTemperatureController()
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
            double ColorTemperature = Endpoint.GetValue<double>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                value = ((int)ColorTemperature).LimitToRange(1000, 10000),
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
                int valueToSend = 0;
                int currentValue = (int)(Endpoint.GetValue<Double>(premiseProperties[0]).GetAwaiter().GetResult()).LimitToRange(1000, 10000);

                if (Header.name == "IncreaseColorTemperature")
                {
                    valueToSend = GetNextColor(currentValue).LimitToRange(1000, 10000);
                }
                else if (Header.name == "DecreaseColorTemperature")
                {
                    valueToSend = GetPreviousColor(currentValue).LimitToRange(1000, 10000);
                }
                else
                {
                    ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                    return;
                }
                Endpoint.SetValue(premiseProperties[0], valueToSend.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = valueToSend;
                Response.context.properties.Add(property);

                Response.Event.header.name = "Response";
                Response.context.properties.AddRange(PropertyHelpers.FindRelatedProperties(Endpoint, @namespace));
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }

        private static int GetNearestValue(int searchValue)
        {
            int nearest = colorTable.Select(p => new { Value = p, Difference = Math.Abs(p - searchValue) })
                  .OrderBy(p => p.Difference)
                  .First().Value;
            return nearest;
        }

        private static int GetNextColor(int current)
        {
            current = GetNearestValue(current);
            int i = 0;
            foreach (int value in colorTable)
            {
                if (current == value)
                    break;
                i++;
            }
            i++;
            int limit = (colorTable.Count() - 1);
            i = i.LimitToRange(0, colorTable.Count() - 1);
            return colorTable[i];
        }

        private static int GetPreviousColor(int current)
        {
            current = GetNearestValue(current);
            int x = 0;
            for (x = colorTable.Count() - 1; x >= 0; x--)
            {
                if (current == colorTable[x])
                    break;
            }
            x--;
            x = x.LimitToRange(0, colorTable.Count() - 1);
            return colorTable[x];
        }

        #endregion Methods
    }
}