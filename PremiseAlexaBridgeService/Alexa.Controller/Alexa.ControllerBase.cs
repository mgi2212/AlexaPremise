using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Alexa.SmartHomeAPI.V3;
using PremiseAlexaBridgeService;
using SYSWebSockClient;

namespace Alexa.Controller
{
    /// <summary>
    /// Templated base class for Alexa controllers T is the response payload (accounts for
    /// differences in payloads) TT is the response object TTT is the request object
    /// </summary>
    public class AlexaControllerBase<T, TT, TTt> where TT : new()
    {
        #region Fields

        private readonly DirectiveEndpoint _directiveEndpoint;
        private readonly Header _header;
        private readonly T _payload;
        private readonly TTt _request;
        private TT _response;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Used when full controller functionality is required
        /// </summary>
        /// <param name="requestObject"></param>
        public AlexaControllerBase(TTt requestObject)
        {
            _request = requestObject;
            _header = RequestHeader;
            _payload = (T)RequestPayload;
            _directiveEndpoint = RequestDirectiveEndpoint;

            if (_directiveEndpoint == null)
            {
                object[] args = { _header };
                _response = GetNewResponseObject(args);
            }
            else
            {
                object[] args = { _header, _directiveEndpoint };
                _response = GetNewResponseObject(args);
                ResponseEvent.endpoint.cookie = _directiveEndpoint?.cookie;
            }
        }

        /// <summary>
        /// Used on State reports or reports on state in controller responses (when the full
        /// controller object functionality is not required)
        /// </summary>
        /// <param name="sysObject"></param>
        public AlexaControllerBase(IPremiseObject sysObject)
        {
            Endpoint = sysObject;
        }

        public AlexaControllerBase()
        {
        }

        #endregion Constructors

        #region Reflection Code

        protected TT GetNewResponseObject(object[] args)
        {
            _response = (TT)Activator.CreateInstance(typeof(TT), args);
            return _response;
        }

        #endregion Reflection Code

        #region Properties

        public IPremiseObject Endpoint { get; private set; }
        public Header Header => _header;
        public T Payload => _payload;
        public TTt Request => _request;
        public TT Response => _response;

        private object RequestDirective
        {
            get
            {
                Type t = _request.GetType();
                PropertyInfo prop = t.GetProperty("directive");
                if (prop == null) return null;
                object directive = prop.GetValue(_request);
                return directive;
            }
        }

        private DirectiveEndpoint RequestDirectiveEndpoint
        {
            get
            {
                Type t = RequestDirective.GetType();
                PropertyInfo prop = t.GetProperty("endpoint");
                if (prop == null) return null;
                DirectiveEndpoint endpoint = prop.GetValue(RequestDirective) as DirectiveEndpoint;
                return endpoint;
            }
        }

        private Header RequestHeader
        {
            get
            {
                Type t = RequestDirective.GetType();
                PropertyInfo prop = t.GetProperty("header");
                if (prop == null) return null;
                Header header = prop.GetValue(RequestDirective) as Header;
                return header;
            }
        }

        private object RequestPayload
        {
            get
            {
                Type t = RequestDirective.GetType();
                PropertyInfo prop = t.GetProperty("payload");
                if (prop == null) return null;
                object payload = prop.GetValue(RequestDirective);
                return payload;
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
                if (prop == null) return null;
                Scope scope = prop.GetValue(RequestPayload) as Scope;
                return scope;
            }
        }

        private AlexaControlResponseContext ResponseContext
        {
            //get
            //{
            //    Type t = _response.GetType();
            //    PropertyInfo prop = t.GetProperty("context");
            //    if (prop == null) return null;
            //    AlexaControlResponseContext context = prop.GetValue(_response) as AlexaControlResponseContext;
            //    return context;
            //}
            set
            {
                Type t = _response.GetType();
                PropertyInfo prop = t.GetProperty("context");
                if (prop != null)
                {
                    prop.SetValue(_response, value);
                }
            }
        }

        private AlexaEventBody ResponseEvent
        {
            get
            {
                Type t = _response.GetType();
                PropertyInfo prop = t.GetProperty("Event");
                AlexaEventBody context = prop?.GetValue(_response) as AlexaEventBody;
                return context;
            }
        }

        #endregion Properties

        #region Methods

        #region Instance Methods

        public void ReportError(AlexaErrorTypes type, string message)
        {
            if (Response.GetType() != typeof(ControlResponse)) return;
            ResponseContext = null;
            ResponseEvent.payload = new AlexaErrorResponsePayload(type, message);
        }

        public bool ValidateDirective(string[] directiveNames, string @namespace)
        {
            #region Validate request

            if (_header.payloadVersion != "3" || (!directiveNames.Contains(_header.name)) || (_header.@namespace != @namespace))
            {
                ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid directive!");
                return false;
            }

            #endregion Validate request

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

            #endregion Connect To Premise Server

            #region VerifyAccess

            try
            {
                Scope testScope;
                // must be an endpointeless directive (like Discovery) see if there is a scope in the payload
                if ((_directiveEndpoint == null) && (RequestPayloadScope != null))
                {
                    testScope = RequestPayloadScope;
                }
                else
                {
                    testScope = _directiveEndpoint?.scope;
                }

                if ((testScope == null) || (testScope.type != "BearerToken") || (testScope.localAccessToken == null))
                {
                    ReportError(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid bearer token.");
                    return false;
                }
                if (!CheckAccessToken(testScope.localAccessToken).GetAwaiter().GetResult())
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

            #endregion VerifyAccess

            #region Get Premise Object

            try
            {
                if (_directiveEndpoint?.endpointId != null)
                {
                    Guid premiseId = new Guid(_directiveEndpoint.endpointId);
                    Endpoint = PremiseServer.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                    if (Endpoint == null)
                    {
                        ReportError(AlexaErrorTypes.INTERNAL_ERROR,
                            $"Cannot find device {_directiveEndpoint.endpointId} on server.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                ResponseEvent.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR,
                    $"Cannot find device {_directiveEndpoint.endpointId} on server.");
                return false;
            }

            #endregion Get Premise Object

            InformLastContact($"{_header?.name}: {RequestDirectiveEndpoint?.cookie?.path}").GetAwaiter().GetResult();

            return true;
        }

        #endregion Instance Methods

        #region Static Methods

        public static void SerialiszationTest(TT response)
        {
            // Serialization Check
            var memoryStream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(TT));
            serializer.WriteObject(memoryStream, response);
            memoryStream.Position = 0;
            var streamReader = new StreamReader(memoryStream);
            Debug.WriteLine("JSON serialized response object");
            Debug.WriteLine(streamReader.ReadToEnd());
        }

        protected static async Task<bool> CheckAccessToken(string token)
        {
            var accessToken = await PremiseServer.HomeObject.GetValue<string>("AccessToken");
            var tokens = new List<string>(accessToken.Split(','));
            return (-1 != tokens.IndexOf(token));
        }

        protected static string GetUtcTime()
        {
            return DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
        }

        protected static async Task InformLastContact(string command)
        {
            await PremiseServer.HomeObject.SetValue("LastHeardFromAlexa", DateTime.Now.ToString(CultureInfo.CurrentCulture));
            await PremiseServer.HomeObject.SetValue("LastHeardCommand", command);
        }

        #endregion Static Methods

        #endregion Methods
    }
}