using Alexa;
using Alexa.Discovery;
using Alexa.HVAC;
using Alexa.Lighting;
using Alexa.Power;
using Alexa.Scene;
using Alexa.SmartHomeAPI.V3;
using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Web.Script.Serialization;
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
        DiscoveryControllerResponse Discovery(AlexaDiscoveryControllerRequest request);

        [OperationContract]
        ControlResponse SetPowerState(AlexaSetPowerStateControllerRequest request);

        [OperationContract]
        ControlResponse SetBrightness(AlexaSetBrightnessControllerRequest request);

        [OperationContract]
        ControlResponse AdjustBrightness(AlexaAdjustBrightnessControllerRequest request);

        [OperationContract]
        ControlResponse SetScene(AlexaSetSceneControllerRequest request);

        [OperationContract]
        ControlResponse AdjustColorTemperature(AlexaAdjustColorTemperatureControllerRequest request);

        [OperationContract]
        ControlResponse SetColorTemperature(AlexaSetColorTemperatureControllerRequest request);

        [OperationContract]
        ControlResponse SetColor(AlexaSetColorControllerRequest request);

        [OperationContract]
        ControlResponse SetTargetTemperature(AlexaSetTargetTemperatureControllerRequest request);

        [OperationContract]
        ControlResponse AdjustTargetTemperature(AlexaAdjustTargetTemperatureControllerRequest request);

        [OperationContract]
        ControlResponse SetThermostatMode(AlexaSetThermostatModeControllerRequest request);

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

        public SystemResponsePayload GetHealthCheckResponseV3()
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
        /// Discovery directives are processed here
        /// </summary>
        /// <param name="request", type="AlexaDiscoveryControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        /// [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        /// 
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        public DiscoveryControllerResponse Discovery(AlexaDiscoveryControllerRequest request)
        {
            AlexaDiscoveryController controller = new AlexaDiscoveryController(request);
            if (controller.ValidateDirective(controller.directiveNames, controller.@namespace))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion

        #region Control

        #region PowerState

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetPowerStateControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetPowerState/")]
        public ControlResponse SetPowerState(AlexaSetPowerStateControllerRequest request)
        {
            AlexaSetPowerStateController controller = new AlexaSetPowerStateController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }

            var json = new JavaScriptSerializer().Serialize(controller.Response);

            return controller.Response;
        }

        #endregion

        #region Brightness

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetBrightnessControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetBrightness/")]
        public ControlResponse SetBrightness(AlexaSetBrightnessControllerRequest request)
        {
            AlexaSetBrightnessController controller = new AlexaSetBrightnessController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
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
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion

        #region Scene

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetPowerStateControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/Scene/")]
        public ControlResponse SetScene(AlexaSetSceneControllerRequest request)
        {
            AlexaSetSceneController controller = new AlexaSetSceneController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion

        #region ColorTemperature

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaAdjustColorTemperatureController"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/AdjustColorTemperature/")]
        public ControlResponse AdjustColorTemperature(AlexaAdjustColorTemperatureControllerRequest request)
        {
            AlexaAdjustColorTemperatureController controller = new AlexaAdjustColorTemperatureController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaAdjustColorTemperatureController"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetColorTemperature/")]
        public ControlResponse SetColorTemperature(AlexaSetColorTemperatureControllerRequest request)
        {
            AlexaSetColorTemperatureController controller = new AlexaSetColorTemperatureController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion

        #region Color

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetColorControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetColor/")]
        public ControlResponse SetColor(AlexaSetColorControllerRequest request)
        {
            AlexaSetColorController controller = new AlexaSetColorController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion

        #region Thermostat

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetTargetTemperatureControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/AdjustTargetTemperature/")]
        public ControlResponse AdjustTargetTemperature(AlexaAdjustTargetTemperatureControllerRequest request)
        {
            AlexaAdjustTargetTemperatureController controller = new AlexaAdjustTargetTemperatureController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }


        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetTargetTemperatureControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetTargetTemperature/")]
        public ControlResponse SetTargetTemperature(AlexaSetTargetTemperatureControllerRequest request)
        {
            SetTargetTemperatureController controller = new SetTargetTemperatureController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        /// <summary>
        /// Control Requests are processed here
        /// </summary>
        /// <param name="request", type="AlexaSetThermostatModeControllerRequest"></param>
        /// <returns>ControlResponse</returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetThermostatMode/")]
        public ControlResponse SetThermostatMode(AlexaSetThermostatModeControllerRequest request)
        {
            AlexaSetThermostatModeController controller = new AlexaSetThermostatModeController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
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

                if (!CheckAccessToken(directive.endpoint.scope.localAccessToken).GetAwaiter().GetResult())
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
                // use reflection to instantiate all device type controllers
                var interfaceType = typeof(IAlexaDeviceType);
                var all = AppDomain.CurrentDomain.GetAssemblies()
                  .SelectMany(x => x.GetTypes())
                  .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                  .Select(x => Activator.CreateInstance(x));

                foreach (IAlexaDeviceType deviceType in all)
                {
                    var related = deviceType.FindRelatedProperties(endpoint, "");
                    foreach (AlexaProperty property in related)
                    {
                        if (!response.context.propertiesInternal.ContainsKey(property.@namespace + "." + property.name))
                        {
                            response.context.propertiesInternal.Add(property.@namespace + "." + property.name, property);
                        }
                    }
                }

                foreach (Capability capability in discoveryEndpoint.capabilities)
                {
                    switch (capability.@interface)  // scenes are special cased
                    {
                        case "Alexa.SceneController":
                            {
                                AlexaSetSceneController controller = new AlexaSetSceneController("", endpoint);
                                AlexaProperty prop = controller.GetPropertyState();
                                response.@event.header.name = (string)prop.value;
                                response.@event.payload.cause = new ChangeReportCause
                                {
                                    type = "VOICE_INTERACTION"
                                };
                                response.@event.payload.timestamp = prop.timeOfSample;
                            }
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

            response.@event.header.name = "StateReport";
            InformLastContact(string.Format("StateReport: {0}", response?.@event?.endpoint?.cookie?.path)).GetAwaiter().GetResult();
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
                if (!CheckAccessToken(directive.payload.grantee.localAccessToken).GetAwaiter().GetResult())
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

                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationCode", directive.payload.grant.access_token).GetAwaiter().GetResult();
                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationRefreshToken", directive.payload.grant.refresh_token).GetAwaiter().GetResult();
                DateTime expiry = DateTime.UtcNow.AddSeconds((double)directive.payload.grant.expires_in);
                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationCodeExpiry", expiry.ToString()).GetAwaiter().GetResult();
                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationClientId", directive.payload.grant.client_id).GetAwaiter().GetResult();
                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationSecret", directive.payload.grant.client_secret).GetAwaiter().GetResult();
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
