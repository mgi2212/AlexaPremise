﻿using Alexa.Controller;
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
        private readonly string premiseProperty = "Temperature";
        private readonly string alexaProperty = "colorTemperatureInKelvin";
        public readonly AlexaLighting PropertyHelpers = new AlexaLighting();

        // corresponds to warm white, soft white, white, daylight, cool white
        private int[] colorTable = { 2200, 2700, 4000, 5500, 7000 };

        public AlexaAdjustColorTemperatureController(AlexaAdjustColorTemperatureControllerRequest request)
            : base(request)
        {
        }

        public AlexaAdjustColorTemperatureController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public string GetNameSpace()
        {
            return @namespace;
        }
        public string [] GetDirectiveNames()
        {
            return directiveNames;
        }

        public AlexaProperty GetPropertyState()
        {
            double ColorTemperature = this.endpoint.GetValue<double>(premiseProperty).GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty
            {
                @namespace = @namespace,
                name = alexaProperty,
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
                name = alexaProperty
            };

            try
            {
                int valueToSend = 0;
                int currentValue = (int)(endpoint.GetValue<Double>(premiseProperty).GetAwaiter().GetResult()).LimitToRange(1000, 10000);

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
                endpoint.SetValue(premiseProperty, valueToSend.ToString()).GetAwaiter().GetResult();
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