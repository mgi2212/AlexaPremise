using Alexa.SmartHomeAPI.V3;
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

/// <summary>
/// Templated base class for Alexa controllers
/// T is the response payload (accounts for differences in payloads)
/// TT is the response object
/// TTT is the request object
/// </summary>
namespace Alexa.Controller
{
    public class AlexaControllerBase<T, TT, TTT> where TT : new()
    {
        #region public Vars

        public T payload;
        public TT response;
        public TTT request;
        public IPremiseObject endpoint;
        public Header header;
        public DirectiveEndpoint directiveEndpoint;

        #endregion

        #region Constructors
        /// <summary>
        /// Used when full controller functionality is required
        /// </summary>
        /// <param name="requestObject"></param>
        public AlexaControllerBase(TTT requestObject)
        {
            request = requestObject;
            header = RequestHeader;
            payload = (T)RequestPayload;
            directiveEndpoint = RequestDirectiveEndpoint;

            if (directiveEndpoint == null)
            {
                object[] args = { header };
                response = GetNewResponseObject(args);
            }
            else
            {
                object[] args = { header, directiveEndpoint };
                response = GetNewResponseObject(args);
            }
            if (directiveEndpoint != null)
            {
                this.ResponseEvent.endpoint.cookie = directiveEndpoint?.cookie;
            }
        }
        /// <summary>
        /// Used on State reports or reports on state in controller responses (when the full controller object functionality is not required)
        /// </summary>
        /// <param name="sysObject"></param>
        public AlexaControllerBase(IPremiseObject sysObject)
        {
            endpoint = sysObject;
        }
        public AlexaControllerBase()
        {
        }
        #endregion

        #region Reflection Code

        protected TT GetNewResponseObject(object[] args)
        {
            TT response = (TT)Activator.CreateInstance(typeof(TT), args);
            return response;
        }

        #endregion

        #region Properties

        public TT Response
        {
            get
            {
                return response;
            }
        }

        private DirectiveEndpoint RequestDirectiveEndpoint
        {
            get
            {
                Type t = RequestDirective.GetType();
                PropertyInfo prop = t.GetProperty("endpoint");
                if (prop != null)
                {
                    DirectiveEndpoint endpoint = prop.GetValue(RequestDirective) as DirectiveEndpoint;
                    return endpoint;
                }
                return null;
            }
        }

        private object RequestDirective
        {
            get
            {
                Type t = request.GetType();
                PropertyInfo prop = t.GetProperty("directive");
                if (prop != null)
                {
                    object directive = (object)prop.GetValue(request);
                    return directive;
                }
                return null;
            }
        }

        private object RequestPayload
        {
            get
            {
                Type t = RequestDirective.GetType();
                PropertyInfo prop = t.GetProperty("payload");
                if (prop != null)
                {
                    object payload = prop.GetValue(RequestDirective);
                    return payload;
                }
                return null;
            }
        }

        private Header RequestHeader
        {
            get
            {
                Type t = RequestDirective.GetType();
                PropertyInfo prop = t.GetProperty("header");
                if (prop != null)
                {
                    Header header = prop.GetValue(RequestDirective) as Header;
                    return header;
                }
                return null;
            }
        }

        private Scope RequestPayloadScope
        {
            get
            {
                if (RequestPayload == null)
                    return null;

                Type t = RequestPayload.GetType();
                PropertyInfo prop = t.GetProperty("scope");
                if (prop != null)
                {
                    Scope scope = prop.GetValue(RequestPayload) as Scope;
                    return scope;
                }
                return null;
            }
        }

        private AlexaControlResponseContext ResponseContext
        {
            get
            {
                Type t = response.GetType();
                PropertyInfo prop = t.GetProperty("context");
                if (prop != null)
                {
                    AlexaControlResponseContext context = prop.GetValue(response) as AlexaControlResponseContext;
                    return context;
                }
                return null;
            }
            set
            {
                Type t = response.GetType();
                PropertyInfo prop = t.GetProperty("context");
                if (prop != null)
                {
                    prop.SetValue(response, value);
                }
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

        #endregion

        #region Methods

        #region Instance Methods

        public void ReportError(AlexaErrorTypes type, string message)
        {
            if (this.Response.GetType() == typeof(ControlResponse))
            {
                this.ResponseContext = null;
                this.ResponseEvent.payload = new AlexaErrorResponsePayload(type, message);
            }
        }

        public bool ValidateDirective(string[] directiveNames, string @namespace)
        {
            #region Validate request

            if ((this.header.payloadVersion != "3") || (!directiveNames.Contains(this.header.name)) || (this.header.@namespace != @namespace))
            {
                ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid directive!");
                return false;
            }

            #endregion

            #region Connect To Premise Server

            try
            {
                if (PremiseServer.RootObject == null)
                {
                    ReportError(AlexaErrorTypes.ENDPOINT_UNREACHABLE, "Premise Server.");
                    return false;
                }
            }
            catch (Exception)
            {
                ReportError(AlexaErrorTypes.ENDPOINT_UNREACHABLE, "Premise Server.");
                return false;
            }

            #endregion

            #region VerifyAccess

            try
            {
                Scope testScope;
                // must be an endpointeless directive (like Discovery) see if there is a scope in the payload
                if ((this.directiveEndpoint == null) && (this.RequestPayloadScope != null))
                {
                    testScope = this.RequestPayloadScope;
                }
                else
                {
                    testScope = this.directiveEndpoint.scope;
                }

                if ((testScope == null) || (testScope.type != "BearerToken") || (testScope.localAccessToken == null))
                {
                    ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid bearer token.");
                    return false;
                }
                else if (!CheckAccessToken(testScope.localAccessToken).GetAwaiter().GetResult())
                {
                    ReportError(AlexaErrorTypes.INVALID_AUTHORIZATION_CREDENTIAL, "Not authorized on local premise server.");
                    return false;
                }
            }
            catch
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, "Cannot find Alexa home object on local Premise server.");
                return false;
            }

            #endregion

            #region Get Premise Object

#pragma warning disable CS0219 // Variable is assigned but its value is never used
            IPremiseObject endpoint = null;
#pragma warning restore CS0219 
            try
            {
                if ((directiveEndpoint != null) && (directiveEndpoint.endpointId != null))
                {
                    Guid premiseId = new Guid(directiveEndpoint.endpointId);
                    this.endpoint = PremiseServer.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                    if (this.endpoint == null)
                    {
                        ReportError(AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find device {0} on server.", directiveEndpoint.endpointId));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                this.ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find device {0} on server.", directiveEndpoint.endpointId));
                return false;
            }
            #endregion

            #region Debug

            // Uncomment below to check serialization
            // SerialiszationTest(this.Response);

            #endregion

            InformLastContact(header.name).GetAwaiter().GetResult();

            return true;
        }

        #endregion

        #region Static Methods

        protected static string GetUtcTime()
        {
            return DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
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

        #endregion

        #endregion

    }
}