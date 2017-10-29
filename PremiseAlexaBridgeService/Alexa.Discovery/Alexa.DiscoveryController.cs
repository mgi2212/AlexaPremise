﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Alexa.Controller;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.Discovery
{
    #region Discovery Data Contracts

    #region Response

    [DataContract]
    public class AlexaDiscoveryResponsePayload : AlexaResponsePayload
    {
        #region Constructors

        public AlexaDiscoveryResponsePayload()
        {
            endpoints = new List<DiscoveryEndpoint>();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "endpoints", EmitDefaultValue = false, IsRequired = false)]
        public List<DiscoveryEndpoint> endpoints { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class DiscoveryControllerResponse
    {
        #region Constructors

        public DiscoveryControllerResponse()
        {
        }

        public DiscoveryControllerResponse(Header header)
        {
            Event = new DiscoveryControllerResponseEvent
            {
                header = header,
                payload = new AlexaDiscoveryResponsePayload()
            };
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "event", Order = 2)]
        public DiscoveryControllerResponseEvent Event { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class DiscoveryControllerResponseEvent
    {
        #region Constructors

        public DiscoveryControllerResponseEvent()
        {
            payload = new AlexaDiscoveryResponsePayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "header", Order = 1)]
        public Header header { get; set; }

        [DataMember(Name = "payload", EmitDefaultValue = false, Order = 3)]
        public AlexaDiscoveryResponsePayload payload { get; set; }

        #endregion Properties
    }

    #endregion Response

    #region Request

    [DataContract]
    public class AlexaDiscoveryControllerRequest
    {
        #region Properties

        [DataMember(Name = "directive")]
        public AlexaDiscoveryControllerRequestDirective directive { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class AlexaDiscoveryControllerRequestDirective
    {
        #region Constructors

        public AlexaDiscoveryControllerRequestDirective()
        {
            header = new Header();
            payload = new DiscoveryControllerRequestDirectivePayload();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "header", IsRequired = true, Order = 1)]
        public Header header { get; set; }

        [DataMember(Name = "payload", IsRequired = true, Order = 3)]
        public DiscoveryControllerRequestDirectivePayload payload { get; set; }

        #endregion Properties
    }

    [DataContract]
    public class DiscoveryControllerRequestDirectivePayload
    {
        #region Constructors

        public DiscoveryControllerRequestDirectivePayload()
        {
            scope = new Scope();
        }

        #endregion Constructors

        #region Properties

        [DataMember(Name = "scope", EmitDefaultValue = false)]
        public Scope scope { get; set; }

        #endregion Properties
    }

    #endregion Request

    #endregion Discovery Data Contracts

    public class AlexaDiscoveryController : AlexaControllerBase<
        DiscoveryControllerRequestDirectivePayload,
        DiscoveryControllerResponse,
        AlexaDiscoveryControllerRequest>, IAlexaController
    {
        #region Fields

        public readonly string @namespace = "Alexa.Discovery";
        public readonly string[] directiveNames = { "Discover" };
        public readonly string[] premiseProperties = { "none" };
        private readonly string[] alexaProperties = { "Discover.Response" };

        #endregion Fields

        #region Constructors

        public AlexaDiscoveryController(AlexaDiscoveryControllerRequest request)
            : base(request)
        {
        }

        public AlexaDiscoveryController(IPremiseObject endpoint)
            : base(endpoint)
        {
        }

        public AlexaDiscoveryController()
        {
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
            return null;
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
            try
            {
                Response.Event.payload.endpoints = PremiseServer.GetEndpoints().GetAwaiter().GetResult();
                if (PremiseServer.IsAsyncEventsEnabled)
                {
                    Task.Run(() =>
                    {
                        PremiseServer.Resubscribe();
                    });
                }
            }
            catch
            {
                Response.Event.payload.endpoints.Clear();
            }

            Response.Event.header.name = alexaProperties[0];
            string message = $"Discovery reported {Response.Event.payload.endpoints.Count} devices and scenes.";
            PremiseServer.HomeObject.SetValue("LastRefreshed", DateTime.Now.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
            PremiseServer.HomeObject.SetValue("HealthDescription", message).GetAwaiter().GetResult();
            PremiseServer.HomeObject.SetValue("Health", "True").GetAwaiter().GetResult();
            PremiseServer.WriteToWindowsApplicationEventLog(EventLogEntryType.Information, message + $" Client ip {GetClientIp()}", 50);
        }

        #endregion Methods
    }
}