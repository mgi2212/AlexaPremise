using Alexa.Lighting;
using Alexa.Power;
using Alexa.SmartHome.V3;
using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
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
        ControlResponse SetPowerState(AlexaSetPowerStateControllerRequest request);

        [OperationContract]
        ControlResponse SetBrightness(AlexaSetBrightnessControllerRequest request);

        [OperationContract]
        ControlResponse AdjustBrightness(AlexaAdjustBrightnessControllerRequest request);

        [OperationContract]
        ReportStateResponse ReportState(ReportStateRequest request);

        [OperationContract]
        SystemResponse System(SystemRequest request);
    }

    public class PremiseAlexaV3Service : PremiseAlexaBase, IPremiseAlexaV3Service
    {

        #region System

        /// <summary>
        /// System messages
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/System/")]
        public SystemResponse System(SystemRequest alexaRequest)
        {
            var response = new SystemResponse
            {
                header = new Header
                {
                    @namespace = "System"
                },
                payload = new SystemResponsePayload()
            };

            if (PremiseServer.HomeObject == null)
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
                    InformLastContact("System:HealthCheckRequest").GetAwaiter().GetResult();
                    response.header.name = "HealthCheckResponse";
                    response.payload = this.GetHealthCheckResponseV3();
                    break;

                default:
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.UnsupportedOperationError;
                    response.payload.exception = new ExceptionResponsePayload();
                    break;
            }
            return response;
        }

        private SystemResponsePayload GetHealthCheckResponseV3()
        {
            SystemResponsePayload payload = new SystemResponsePayload();
            var returnClause = new string[] { "Health", "HealthDescription" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            payload.isHealthy = PremiseServer.HomeObject.GetValue<bool>("Health").GetAwaiter().GetResult();
            payload.description = PremiseServer.HomeObject.GetValue<string>("HealthDescription").GetAwaiter().GetResult();
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
                // return empty discovery per spec
                return response;
            }

            #endregion

            #region Connect To Premise Server

            if (PremiseServer.RootObject == null)
            {
                // return empty discovery per spec
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

                if (!CheckAccessToken(directive.payload.scope.token).GetAwaiter().GetResult())
                {
                    // return empty discovery per spec (todo: this is not helpful discovering what went wrong and pushes any investigation to 3P logs)
                    return response;
                }

                #region Perform Discovery

                InformLastContact(directive.header.name).GetAwaiter().GetResult();
                response.@event.payload.endpoints = PremiseServer.GetEndpoints().GetAwaiter().GetResult();

                if (PremiseServer.IsAsyncEventsEnabled)
                {
                    Task t = Task.Run(() =>
                    {
                        PremiseServer.Resubscribe();
                    });
                }


                PremiseServer.HomeObject.SetValue("LastRefreshed", DateTime.Now.ToString());
                int count = response.@event.payload.endpoints.Count;

                if (count >= PremiseServer.AlexaDeviceLimit)
                {
                    PremiseServer.HomeObject.SetValue("HealthDescription", string.Format("Alexa device discovery limit reached or exceeded! Reported {0} endpoints.", count)).GetAwaiter().GetResult() ;
                }
                else
                {
                    PremiseServer.HomeObject.SetValue("HealthDescription", string.Format("Alexa discovery reported {0} endpoints.", count)).GetAwaiter().GetResult();
                }
                PremiseServer.HomeObject.SetValue("Health", "True").GetAwaiter().GetResult();

                #endregion

            }
            catch
            {
                // return empty discovery
            }

            #endregion

            return response;
        }


        #endregion

        #region Control

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetBrightnessControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetPowerState/")]
        public ControlResponse SetPowerState(AlexaSetPowerStateControllerRequest request)
        {
            AlexaSetPowerStateController controller = new AlexaSetPowerStateController(request);
            if (controller.ValidateDirective(controller.directiveNames, controller.@namespace))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetBrightnessControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetBrightness/")]
        public ControlResponse SetBrightness(AlexaSetBrightnessControllerRequest request)
        {
            AlexaSetBrightnessController controller = new AlexaSetBrightnessController(request);
            if (controller.ValidateDirective(controller.directiveNames, controller.@namespace))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaAdjustBrightnessControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/AdjustBrightness/")]
        public ControlResponse AdjustBrightness(AlexaAdjustBrightnessControllerRequest request)
        {
            AlexaAdjustBrightnessController controller = new AlexaAdjustBrightnessController(request);
            if (controller.ValidateDirective(controller.directiveNames, controller.@namespace))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

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

            AlexaDirective directive = request.directive;

            var response = new ReportStateResponse(directive);

            #region Validate request

            if (directive.header.payloadVersion != "3")
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid payload version.");
                return response;
            }

            #endregion

            #region Connect To Premise Server

            try
            {
                if (PremiseServer.RootObject == null)
                {
                    response.context = null;
                    response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.ENDPOINT_UNREACHABLE, "Premise Server.");
                    return response;
                }
            }
            catch (Exception)
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.ENDPOINT_UNREACHABLE, "Premise Server.");
                return response;
            }

            #endregion

            #region Verify Access

            try
            {
                if ((directive.endpoint.scope == null) || (directive.endpoint.scope.type != "BearerToken"))
                {
                    response.context = null;
                    response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid bearer token.");
                    return response;
                }

                if (!CheckAccessToken(directive.endpoint.scope.token).GetAwaiter().GetResult())
                {
                    response.context = null;
                    response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_AUTHORIZATION_CREDENTIAL, "Not authorized on local premise server.");
                    return response;
                }

            }
            catch
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, "Cannot find Alexa home object on local Premise server.");
                return response;
            }

            #endregion

            #region Get Premise Object

            IPremiseObject endpoint = null;
            try
            {
                Guid premiseId = new Guid(directive.endpoint.endpointId);
                endpoint = PremiseServer.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                if (endpoint == null)
                {
                    throw new Exception();
                }
            }
            catch
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find device {0} on server.", directive.endpoint.endpointId));
                return response;
            }

            if (directive.header.name != "ReportState")
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid Directive");
                return response;
            }

            #endregion

            DiscoveryEndpoint discoveryEndpoint = PremiseServer.GetDiscoveryEndpoint(endpoint).GetAwaiter().GetResult();
            if (discoveryEndpoint == null)
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, string.Format("Cannot find or invalid discoveryJson for {0} on server.", directive.endpoint.endpointId));
                return response;
            }
            response.@event.endpoint.cookie = discoveryEndpoint.cookie;

            try
            {

                foreach (Capability capability in discoveryEndpoint.capabilities)
                {
                    switch (capability.@interface)
                    {
                        case "Alexa.PowerController":
                            {
                                AlexaSetPowerStateController controller = new AlexaSetPowerStateController(endpoint);
                                response.context.properties.Add(controller.GetPropertyState());
                            }
                            break;
                        case "Alexa.BrightnessController":
                            {
                                AlexaSetBrightnessController controller = new AlexaSetBrightnessController(endpoint);
                                response.context.properties.Add(controller.GetPropertyState());
                            }
                            break;

                        case "Alexa.ColorController":
                            break;

                        case "Alexa.ColorTemperatureController":
                            break;

                        default:
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return response;
            }

            return response;
        }
        #endregion

        #region Authorization

        /// <summary>
        /// Query Requests are processed here
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Authorization/")]
        public AuthorizationResponse Authorization(AuthorizationRequest request)
        {

            AuthorizationDirective directive = request.directive;

            var response = new AuthorizationResponse(directive);

            #region Validate request

            if (directive.header.payloadVersion != "3")
            {
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid payload version.");
                return response;
            }

            #endregion

            #region Verify Access

            if ((directive.payload.grantee == null) || (directive.payload.grantee.type != "BearerToken"))
            {
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid bearer token.");
                return response;
            }

            try
            {
                if (!CheckAccessToken(directive.payload.grantee.token).GetAwaiter().GetResult())
                {
                    response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_AUTHORIZATION_CREDENTIAL, "Not authorized on local premise server.");
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return response;
            }

            #endregion

            try
            {
                if (PremiseServer.HomeObject == null)
                {
                    response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.ENDPOINT_UNREACHABLE, "Premise Server.");
                    return response;
                }

                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationCode", directive.payload.grant.code).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR, ex.Message);
                return response;
            }

            return response;
        }

        #endregion
    }
}
