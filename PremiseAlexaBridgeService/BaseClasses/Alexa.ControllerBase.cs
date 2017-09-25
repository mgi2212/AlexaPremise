using Alexa.SmartHome.V3;
using PremiseAlexaBridgeService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using SYSWebSockClient;

namespace Alexa.Controller
{
    public class AlexaControllerBase<T, TT> where TT : new()
    {
        internal IPremiseObject endpoint;
        internal TT response;
        internal Header header;
        internal DirectiveEndpoint directiveEndpoint;
        internal T payload;
        internal ControlErrorPayload errorPayload = null;

        public AlexaControllerBase(Header headerObject, DirectiveEndpoint directiveEndpointObject, T payloadObject)
        {
            directiveEndpoint = directiveEndpointObject;
            header = headerObject;
            payload = payloadObject;
            object[] args = { headerObject, directiveEndpointObject };
            response = GetObject(args);
            this.ResponseEvent.header = headerObject;
            this.ResponseEvent.endpoint.cookie = directiveEndpoint.cookie;
        }

        public AlexaControllerBase(IPremiseObject sysObject)
        {
            endpoint = sysObject;
        }

        protected TT GetObject(object[] args)
        {
            return (TT)Activator.CreateInstance(typeof(TT), args);
        }

        public TT Response
        {
            get
            {
                return response;
            }
        }

        public void ClearResponseContextAndEventPayload()
        {
            this.ResponseContext = null;
            this.ResponseEvent.payload = null;
        }

        private AlexaControlResponseContext ResponseContext
        {
            get
            {
                Type t = response.GetType();
                PropertyInfo prop = t.GetProperty("context");
                AlexaControlResponseContext context = prop.GetValue(response) as AlexaControlResponseContext;
                return context;
            }
            set
            {
                Type t = response.GetType();
                PropertyInfo prop = t.GetProperty("context");
                prop.SetValue(response, value);
            }
        }

        private AlexaEventBody ResponseEvent
        {
            get
            {
                Type t = response.GetType();

                IList<PropertyInfo> props = new List<PropertyInfo>(t.GetProperties());

                PropertyInfo prop = t.GetProperty("Event");
                AlexaEventBody context = prop.GetValue(response) as AlexaEventBody;
                return context;
            }
            set
            {
                Type t = response.GetType();
                PropertyInfo prop = t.GetProperty("Event");
                prop.SetValue(response, value);
            }
        }

        public bool ValidateDirective(string[] directiveNames, string @namespace)
        {
            #region Validate request

            if ((this.header.payloadVersion != "3") || (!directiveNames.Contains(this.header.name)) || (this.header.@namespace != @namespace))
            {
                ClearResponseContextAndEventPayload();
                this.ResponseEvent.payload= new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid directive!");
                return false;
            }

            #endregion

            #region Connect To Premise Server

            try
            {
                if (PremiseServer.RootObject == null)
                {
                    ClearResponseContextAndEventPayload();
                    this.ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.ENDPOINT_UNREACHABLE, "Premise Server.");
                    return false;
                }
            }
            catch (Exception)
            {
                ClearResponseContextAndEventPayload();
                this.ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.ENDPOINT_UNREACHABLE, "Premise Server.");
                return false;
            }

            #endregion

            #region VerifyAccess

            try
            {
                if ((directiveEndpoint.scope == null) || (directiveEndpoint.scope.type != "BearerToken"))
                {
                    ClearResponseContextAndEventPayload();
                    this.ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid bearer token.");
                    return false;
                }

                if (!CheckAccessToken(directiveEndpoint.scope.token).GetAwaiter().GetResult())
                {
                    ClearResponseContextAndEventPayload();
                    this.ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_AUTHORIZATION_CREDENTIAL, "Not authorized on local premise server.");
                    return false;
                }

            }
            catch
            {
                ClearResponseContextAndEventPayload();
                this.ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, "Cannot find Alexa home object on local Premise server.");
                return false;
            }

            #endregion

            #region Get Premise Object

#pragma warning disable CS0219 // Variable is assigned but its value is never used
            IPremiseObject endpoint = null;
#pragma warning restore CS0219 
            try
            {
                Guid premiseId = new Guid(directiveEndpoint.endpointId);
                this.endpoint = PremiseServer.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                if (this.endpoint == null)
                {
                    ClearResponseContextAndEventPayload();
                    this.ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find device {0} on server.", directiveEndpoint.endpointId));
                    return false;
                }
            }
            catch
            {
                ClearResponseContextAndEventPayload();
                this.ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find device {0} on server.", directiveEndpoint.endpointId));
                return false;
            }
            #endregion

            //SerialiszationTest(this.Response);

            return true;
        }

        public static void SerialiszationTest(TT response)
        {
            // Serialization Check
            MemoryStream ms = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(TT));
            ser.WriteObject(ms, response);
            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            Debug.WriteLine("JSON serialized response object");
            Debug.WriteLine(sr.ReadToEnd());
        }

        protected static async Task InformLastContact(string command)
        {
            await PremiseServer.HomeObject.SetValue("LastHeardFromAlexa", DateTime.Now.ToString());
            await PremiseServer.HomeObject.SetValue("LastHeardCommand", command);
        }

        protected static async Task<bool> CheckAccessToken(string token)
        {
            var accessToken = await PremiseServer.HomeObject.GetValue<string>("AccessToken");
            List<string> tokens = new List<string>(accessToken.Split(','));
            return (-1 != tokens.IndexOf(token));
        }

    }
}