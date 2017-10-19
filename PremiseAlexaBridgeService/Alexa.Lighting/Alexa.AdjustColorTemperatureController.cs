using Alexa.Controller;
using Alexa.Power;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using System;
using System.Linq;
using System.Runtime.Serialization;
using SYSWebSockClient;

namespace Alexa.Lighting
{
    #region Adjust ColorTemperature Data Contracts

    public class AlexaAdjustColorTemperatureControllerRequest
    {
        [DataMember(Name = "directive")]
        public AlexaAdjustColorTemperatureControllerDirective directive { get; set; }
    }

    [DataContract]
    public class AlexaAdjustColorTemperatureControllerDirective
    {
        [DataMember(Name = "header")]
        public Header header { get; set; }

        [DataMember(Name = "endpoint")]
        public DirectiveEndpoint endpoint { get; set; }

        [DataMember(Name = "payload")]
        public AlexaAdjustColorTemperaturePayload payload { get; set; }

        public AlexaAdjustColorTemperatureControllerDirective()
        {
            header = new Header();
            endpoint = new DirectiveEndpoint();
            payload = new AlexaAdjustColorTemperaturePayload();
        }
    }

    [DataContract]
    public class AlexaAdjustColorTemperaturePayload : object
    {
    }

    #endregion 

    public class AlexaAdjustColorTemperatureController : AlexaControllerBase<
        AlexaAdjustColorTemperaturePayload,
        ControlResponse,
        AlexaAdjustColorTemperatureControllerRequest>, IAlexaController
    {
        private readonly string @namespace = "Alexa.ColorTemperatureController";
        private readonly string[] directiveNames = { "IncreaseColorTemperature", "DecreaseColorTemperature" };
        private readonly string[] premiseProperties = { "Temperature" };
       private readonly string[] alexaProperties = {"colorTemperatureInKelvin"};
        public readonly AlexaLighting PropertyHelpers;

        // corresponds to warm white, soft white, white, daylight, cool white
        private int[] colorTable = { 2200, 2700, 4000, 5500, 7000 };

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
            : base()
        {
            PropertyHelpers = new AlexaLighting();
        }

        public string GetNameSpace()
        {
            return @namespace;
        }
        
        public string GetAssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }
        
        public string[] GetAlexaProperties()
        {
            return alexaProperties;
        }

        public string [] GetDirectiveNames()
        {
            return directiveNames;
        }
        public bool HasAlexaProperty(string property)
        {
            return (this.alexaProperties.Contains(property));
        }
        public bool HasPremiseProperty(string property)
        {
            foreach (string s in this.premiseProperties)
            {
                if (s == property)
                    return true;
            }
            return false;
        }

        public string AssemblyTypeName()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public AlexaProperty GetPropertyState()
        {
            double ColorTemperature = this.endpoint.GetValue<double>(premiseProperties[0]).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperties[0],
                value = ((int)ColorTemperature).LimitToRange(1000, 10000),
                timeOfSample = GetUtcTime()
            };
            return property;
        }

        public int GetNearestValue(int searchValue)
        {
            int nearest = colorTable.Select(p => new { Value = p, Difference = Math.Abs(p - searchValue) })
                  .OrderBy(p => p.Difference)
                  .First().Value;
            return nearest;
        }

        public int GetNextColor(int current)
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

        public int GetPreviousColor(int current)
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

        public void ProcessControllerDirective()
        {
            AlexaProperty property = new AlexaProperty(header)
            {
                name = alexaProperties[0]
            };

            response.Event.header.@namespace = "Alexa";

            try
            {
                int valueToSend = 0;
                int currentValue = (int)(endpoint.GetValue<Double>(premiseProperties[0]).GetAwaiter().GetResult()).LimitToRange(1000, 10000);

                if (header.name == "IncreaseColorTemperature")
                {
                    valueToSend = GetNextColor(currentValue).LimitToRange(1000, 10000);
                }
                else if (header.name == "DecreaseColorTemperature")
                {
                    valueToSend = GetPreviousColor(currentValue).LimitToRange(1000, 10000);
                }
                else
                {
                    base.ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Operation not supported!");
                    return;
                }
                endpoint.SetValue(premiseProperties[0], valueToSend.ToString()).GetAwaiter().GetResult();
                property.timeOfSample = GetUtcTime();
                property.value = valueToSend;
                response.context.properties.Add(property);

                this.Response.Event.header.name = "Response";
                this.response.context.properties.AddRange(this.PropertyHelpers.FindRelatedProperties(endpoint, @namespace));
            }
            catch (Exception ex)
            {
                base.ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
            }
        }
    }
}
