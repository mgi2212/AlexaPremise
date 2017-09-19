using Alexa.SmartHome.V3;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;
using Newtonsoft.Json;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    /// <summary>
    /// Version 3
    /// </summary>

    [ServiceContract(Name = "PremiseAlexaV3Service", Namespace = "https://PremiseAlexa.com")]
    public interface IPremiseAlexaV3Service
    {

        [OperationContract]
        AuthorizationResponse Authorization(AuthorizationRequest request);

        [OperationContract]
        DiscoveryResponse Discovery(DiscoveryRequest request);

        [OperationContract]
        ControlResponse Control(ControlRequest request);

        [OperationContract]
        ReportStateResponse ReportState(ReportStateRequest request);

        [OperationContract]
        SystemResponse System(SystemRequest request);
    }

    public class PremiseAlexaV3Service : PremiseAlexaBase, IPremiseAlexaV3Service
    {

        DiscoveryEndpoint GetDiscoveryEndpoint(IPremiseObject endpoint)
        {

            DiscoveryEndpoint discoveryEndpoint;

            try
            {
                string json = endpoint.GetValue("discoveryJson").GetAwaiter().GetResult();
                discoveryEndpoint = JsonConvert.DeserializeObject<DiscoveryEndpoint>(json, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            }
            catch
            {
                discoveryEndpoint = null;
            }

            return discoveryEndpoint;
        }


        #region System

        /// <summary>
        /// System messages
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/System/")]
        public SystemResponse System(SystemRequest alexaRequest)
        {
            var response = new SystemResponse();
            response.header = new Header();
            response.header.@namespace = "System";
            response.payload = new SystemResponsePayload();

            IPremiseObject homeObject;

            SYSClient client = new SYSClient();

            try
            {
                homeObject = ServiceInstance.ConnectToServer(client);
            }
            catch (Exception)
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.DependentServiceUnavailableError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    dependentServiceName = "Premise Server"
                };
                return response;
            }

            switch (alexaRequest.header.name)
            {
                case "HealthCheckRequest":
                    InformLastContact(homeObject, "System:HealthCheckRequest").GetAwaiter().GetResult();
                    response.header.name = "HealthCheckResponse";
                    response.payload = this.GetHealthCheckResponseV3(homeObject);
                    break;

                default:
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.UnsupportedOperationError;
                    response.payload.exception = new ExceptionResponsePayload();
                    break;
            }
            ServiceInstance.DisconnectServer(client);
            return response;
        }

        private SystemResponsePayload GetHealthCheckResponseV3(IPremiseObject homeObject)
        {
            SystemResponsePayload payload = new SystemResponsePayload();
            var returnClause = new string[] { "Health", "HealthDescription" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            payload.isHealthy = homeObject.GetValue<bool>("Health").GetAwaiter().GetResult();
            payload.description = homeObject.GetValue<string>("HealthDescription").GetAwaiter().GetResult();
            return payload;
        }

        #endregion

        #region Discovery

        /// <summary>
        /// Discovery - proxy call to premise looking for the AlexaEx class
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        public DiscoveryResponse Discovery(DiscoveryRequest request)
        {

            IPremiseObject homeObject, rootObject;

            DiscoveryDirective directive = request.directive;

            var response = new DiscoveryResponse(directive);

            #region Validate Request

            if (directive == null)
            {
                // return empty discovery
                return response;
            }

            if ((directive == null) || (directive.header == null) || (directive.payload == null))
            {
                // return empty discovery
                return response;
            }

            if ((directive.header.payloadVersion == null) || (directive.header.payloadVersion != "3"))
            {
                return response;
            }

            if ((directive.header.name != "Discover") || (directive.header.@namespace != "Alexa.Discovery"))
            {
                // return empty discovery
                return response;
            }

            #endregion

            SYSClient client = new SYSClient();

            #region Connect To Premise Server

            try
            {
                homeObject = ServiceInstance.ConnectToServer(client);
                rootObject = homeObject.GetRoot().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // return empty discovery
                return response;
            }

            #endregion

            #region Verify Access and Perform Discovery

            try
            {

                if ((directive.payload.scope == null) || (directive.payload.scope.type != "BearerToken"))
                {
                    return response;
                }

                if (!CheckAccessToken(homeObject, directive.payload.scope.token).GetAwaiter().GetResult())
                {
                    // return empty discovery per spec (todo: this is not helpful discovering what went wrong and pushes any investigation to 3P logs)
                    return response;
                }

                #region Perform Discovery

                InformLastContact(homeObject, directive.header.name).GetAwaiter().GetResult();
                response.@event.payload.endpoints = GetEndpoints(homeObject).GetAwaiter().GetResult();

                #endregion

            }
            catch
            {
                // return empty discovery
            }

            #endregion

            ServiceInstance.DisconnectServer(client);

            // uncomment below to find serialization errors
            //MemoryStream ms = new MemoryStream();
            //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DiscoverResponseEvent));
            //ser.WriteObject(ms, response);
            //ms.Position = 0;
            //StreamReader sr = new StreamReader(ms);
            //Console.WriteLine("JSON serialized response object");
            //Console.WriteLine(sr.ReadToEnd());

            return response;
        }

        #region Perform Discovery
        private async Task<List<DiscoveryEndpoint>> GetEndpoints(IPremiseObject homeObject)
        {
            List<DiscoveryEndpoint> endpoints = new List<DiscoveryEndpoint>();

            // discovery json is now generated in Premise script to vastly improve discovery event response time
            var returnClause = new string[] { "discoveryJson", "IsDiscoverable" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            whereClause.TypeOf = ServiceInstance.AlexaApplianceClassPath;

            var sysAppliances = await homeObject.Select(returnClause, whereClause);
            int count = 0;

            foreach (var sysAppliance in sysAppliances)
            {
                if (sysAppliance.IsDiscoverable == false)
                    continue;

                DiscoveryEndpoint endpoint = new DiscoveryEndpoint();
                try
                {
                    string json = sysAppliance.discoveryJson.ToString();
                    endpoint = JsonConvert.DeserializeObject<DiscoveryEndpoint>(json, new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                }
                catch
                {
                    continue;
                }

                if (endpoint != null)
                {
                    endpoints.Add(endpoint);
                    if (++count >= ServiceInstance.AlexaDeviceLimit)
                    {
                        break;
                    }
                }
            }

            await homeObject.SetValue("LastRefreshed", DateTime.Now.ToString());
            if (count >= ServiceInstance.AlexaDeviceLimit)
            {
                await homeObject.SetValue("HealthDescription", string.Format("Alexa device discovery limit reached or exceeded! Reported {0} endpoints.", count));
            }
            else
            {
                await homeObject.SetValue("HealthDescription", string.Format("Alexa discovery reported {0} endpoints.", count));
            }
            await homeObject.SetValue("Health", "True");
            return endpoints;
        }

        #endregion

        #endregion

        #region Control

        /// <summary>
        /// Control Requests are processed here
        /// 1) validate the json
        /// 2) determine the request type
        /// 3) check for 'token' access privs
        /// 4) find the premise object to control
        /// 5) change state 
        /// 7) report other state changes
        /// </summary>
        /// <param name="alexaDirective"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/")]
        public ControlResponse Control(ControlRequest request)
        {

            IPremiseObject homeObject, rootObject;

            AlexaDirective directive = request.directive;

            var response = new ControlResponse(directive);

            #region Validate request

            if (directive.header.payloadVersion != "3")
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid payload version.");
                return response;
            }

            #endregion

            SYSClient client = new SYSClient();

            #region Connect To Premise Server

            try
            {
                homeObject = ServiceInstance.ConnectToServer(client);
                rootObject = homeObject.GetRoot().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INTERNAL_ERROR, "Cannot connect to local Premise server.");
                return response;
            }

            #endregion

            #region VerifyAccess

            try
            {


                if ((directive.endpoint.scope == null) || (directive.endpoint.scope.type != "BearerToken"))
                {
                    response.context = null;
                    response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid bearer token.");
                    return response;
                }

                if (!CheckAccessToken(homeObject, directive.endpoint.scope.token).GetAwaiter().GetResult())
                {
                    response.context = null;
                    response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_AUTHORIZATION_CREDENTIAL, "Not authorized on local premise server.");
                    return response;
                }

            }
            catch
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INTERNAL_ERROR, "Cannot find Alexa home object on local Premise server.");
                return response;
            }

            #endregion

            #region Get Premise Object
            // get the object
            IPremiseObject endpoint = null;
            try
            {
                Guid premiseId = new Guid(directive.endpoint.endpointId);
                endpoint = rootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                if (endpoint == null)
                {
                    throw new Exception();
                }
            }
            catch
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find device {0} on server.", directive.endpoint.endpointId));
                return response;
            }

            #endregion

            #region Process Controller Directives

            switch (directive.header.@namespace)
            {
                case "Alexa.PowerController":
                    ProcessPowerControllerDirective(endpoint, directive, response);
                    break;

                default:
                    break;

            }

            #endregion

            ServiceInstance.DisconnectServer(client);

            // Serialization Check
            //MemoryStream ms = new MemoryStream();
            //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DiscoveryEvent));
            //ser.WriteObject(ms, response);
            //ms.Position = 0;
            //StreamReader sr = new StreamReader(ms);
            //Console.WriteLine("JSON serialized response object");
            //Console.WriteLine(sr.ReadToEnd());

            return response;
        }

        #region PowerController

        AlexaProperty GetPowerStateProperty(IPremiseObject endpoint)
        {
            bool powerState = endpoint.GetValue("PowerState").GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty();
            property.@namespace = "Alexa.PowerController";
            property.name = "powerState";
            property.value = (powerState == true ? "ON" : "OFF");
            property.timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
            return property;
        }

        void ProcessPowerControllerDirective(IPremiseObject endpoint, AlexaDirective directive, ControlResponse response)
        {
            AlexaProperty property = new AlexaProperty(directive);
            property.name = "powerState";

            if (directive.header.name == "TurnOff")
            {
                endpoint.SetValue("PowerState", "False").GetAwaiter().GetResult();
                property.timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
                property.value = "OFF";
            }
            else if (directive.header.name == "TurnOn")
            {
                endpoint.SetValue("PowerState", "True").GetAwaiter().GetResult();
                property.timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
                property.value = "ON";
            }
            else
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid payload version.");
            }

            response.context.properties.Add(property);

            // grab walk through remaining supported controllers and report state
            DiscoveryEndpoint discoveryEndpoint = GetDiscoveryEndpoint(endpoint);
            if (discoveryEndpoint != null)
            {
                foreach (Capability capability in discoveryEndpoint.capabilities)
                {
                    switch (capability.@interface)
                    {
                        case "Alexa.PowerController": // already added
                            break;

                        case "Alexa.BrightnessController":

                            AlexaProperty brightness = GetBrightnessProperty(endpoint);
                            response.context.properties.Add(brightness);
                            break;

                        case "Alexa.ColorController":
                            // TODO
                            break;

                        case "Alexa.ColorTemperatureController":
                            // TODO
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        #endregion

        #region BrightnessController

        AlexaProperty GetBrightnessProperty(IPremiseObject endpoint)
        {
            double brightness = endpoint.GetValue("Brightness").GetAwaiter().GetResult();
            AlexaProperty property = new AlexaProperty();
            property.@namespace = "Alexa.BrightnessController";
            property.name = "brightness";
            property.value = (int)(brightness * 100);
            property.timeOfSample = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");
            return property;
        }

        #endregion

        #endregion

        #region Report State

        /// <summary>
        /// Query Requests are processed here
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/ReportState/")]
        public ReportStateResponse ReportState(ReportStateRequest request)
        {

            IPremiseObject homeObject, rootObject;

            AlexaDirective directive = request.directive;

            var response = new ReportStateResponse(directive);

            #region Validate request

            if (directive.header.payloadVersion != "3")
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid payload version.");
                return response;
            }

            #endregion

            SYSClient client = new SYSClient();

            #region Connect To Premise Server

            try
            {
                homeObject = ServiceInstance.ConnectToServer(client);
                rootObject = homeObject.GetRoot().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INTERNAL_ERROR, "Cannot connect to local Premise server.");
                return response;
            }

            #endregion

            #region Verify Access

            try
            {


                if ((directive.endpoint.scope == null) || (directive.endpoint.scope.type != "BearerToken"))
                {
                    response.context = null;
                    response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid bearer token.");
                    return response;
                }

                if (!CheckAccessToken(homeObject, directive.endpoint.scope.token).GetAwaiter().GetResult())
                {
                    response.context = null;
                    response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_AUTHORIZATION_CREDENTIAL, "Not authorized on local premise server.");
                    return response;
                }

            }
            catch
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INTERNAL_ERROR, "Cannot find Alexa home object on local Premise server.");
                return response;
            }

            #endregion

            #region Get Premise Object

            IPremiseObject endpoint = null;
            try
            {
                Guid premiseId = new Guid(directive.endpoint.endpointId);
                endpoint = rootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                if (endpoint == null)
                {
                    throw new Exception();
                }
            }
            catch
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find device {0} on server.", directive.endpoint.endpointId));
                return response;
            }

            if (directive.header.name != "ReportState")
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid Directive");
                return response;
            }

            #endregion

            DiscoveryEndpoint discoveryEndpoint = GetDiscoveryEndpoint(endpoint);
            if (discoveryEndpoint == null)
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find or invalid discoveryJson for {0} on server.", directive.endpoint.endpointId));
                return response;
            }
            response.@event.endpoint.cookie = discoveryEndpoint.cookie;

            foreach (Capability capability in discoveryEndpoint.capabilities)
            {
                switch (capability.@interface)
                {
                    case "Alexa.PowerController":

                        AlexaProperty powerState = GetPowerStateProperty(endpoint);
                        response.context.properties.Add(powerState);

                        break;
                    case "Alexa.BrightnessController":

                        AlexaProperty brightness = GetBrightnessProperty(endpoint);
                        response.context.properties.Add(brightness);
                        break;

                    default:
                        break;
                }
            }

            // Serialization Check
            //MemoryStream ms = new MemoryStream();
            //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DiscoveryEvent));
            //ser.WriteObject(ms, response);
            //ms.Position = 0;
            //StreamReader sr = new StreamReader(ms);
            //Console.WriteLine("JSON serialized response object");
            //Console.WriteLine(sr.ReadToEnd());

            return response;
        }
        #endregion

        #region Report State

        /// <summary>
        /// Query Requests are processed here
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Authorization/")]
        public AuthorizationResponse Authorization(AuthorizationRequest request)
        {

            IPremiseObject homeObject, rootObject;

            AuthorizationDirective directive = request.directive;

            var response = new AuthorizationResponse(directive);

            #region Validate request

            if (directive.header.payloadVersion != "3")
            {
                //response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid payload version.");
                return response;
            }

            #endregion

            SYSClient client = new SYSClient();

            #region Connect To Premise Server

            try
            {
                homeObject = ServiceInstance.ConnectToServer(client);
                rootObject = homeObject.GetRoot().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                //response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INTERNAL_ERROR, "Cannot connect to local Premise server.");
                return response;
            }

            #endregion

            #region Verify Access

            if ((directive.payload.grantee == null) || (directive.payload.grantee.type != "BearerToken"))
            {
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid bearer token.");
                return response;
            }

            if (!CheckAccessToken(homeObject, directive.payload.grantee.token).GetAwaiter().GetResult())
            {
                response.@event.payload = new AlexaErrorResponsePayload(DiscoveryUtilities.AlexaErrorTypes.INVALID_AUTHORIZATION_CREDENTIAL, "Not authorized on local premise server.");
                return response;
            }

            #endregion

            homeObject.SetValue("AlexaAsyncAuthorizationCode", directive.payload.grant.code).GetAwaiter().GetResult();

            return response;
        }

        #endregion
    }
}
