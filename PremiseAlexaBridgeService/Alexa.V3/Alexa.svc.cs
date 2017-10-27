using System;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using Alexa;
using Alexa.AV;
using Alexa.Discovery;
using Alexa.HVAC;
using Alexa.Lighting;
using Alexa.Power;
using Alexa.Scene;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    /// <summary>
    /// Version 3
    /// </summary>

    [ServiceContract(Name = "PremiseAlexaV3Service", Namespace = "https://PremiseAlexa.com")]
    public interface IPremiseAlexaV3Service
    {
        #region Methods

        [OperationContract]
        ControlResponse AdjustBrightness(AlexaBrightnessControllerRequest request);

        [OperationContract]
        ControlResponse AdjustColorTemperature(AlexaColorTemperatureControllerRequest request);

        [OperationContract]
        ControlResponse AdjustTargetTemperature(AlexaThermostatControllerRequest request);

        [OperationContract]
        AuthorizationResponse Authorization(AuthorizationRequest request);

        [OperationContract]
        DiscoveryControllerResponse Discovery(AlexaDiscoveryControllerRequest request);

        [OperationContract]
        ControlResponse InputController(AlexaInputControllerRequest request);

        [OperationContract]
        ReportStateResponse ReportState(ReportStateRequest request);

        [OperationContract]
        ControlResponse SetBrightness(AlexaBrightnessControllerRequest request);

        [OperationContract]
        ControlResponse SetColor(AlexaColorControllerRequest request);

        [OperationContract]
        ControlResponse SetColorTemperature(AlexaColorTemperatureControllerRequest request);

        [OperationContract]
        ControlResponse SetPowerState(AlexaSetPowerStateControllerRequest request);

        [OperationContract]
        ControlResponse SetScene(AlexaSetSceneControllerRequest request);

        [OperationContract]
        ControlResponse SetTargetTemperature(AlexaThermostatControllerRequest request);

        [OperationContract]
        ControlResponse SetThermostatMode(AlexaThermostatControllerRequest request);

        [OperationContract]
        ControlResponse Speaker(AlexaSpeakerRequest request);

        [OperationContract]
        SystemResponse System(SystemRequest request);

        #endregion Methods
    }

    public class PremiseAlexaV3Service : PremiseAlexaBase, IPremiseAlexaV3Service
    {
        #region System

        public SystemResponsePayload GetHealthCheckResponseV3()
        {
            SystemResponsePayload payload = new SystemResponsePayload();
            payload.isHealthy = PremiseServer.HomeObject.GetValue<bool>("Health").GetAwaiter().GetResult();
            payload.description = PremiseServer.HomeObject.GetValue<string>("HealthDescription").GetAwaiter().GetResult();
            return payload;
        }

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
                response.payload.exception = new ExceptionResponsePayload
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
                    response.payload = GetHealthCheckResponseV3();
                    break;

                default:
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.UnsupportedOperationError;
                    response.payload.exception = new ExceptionResponsePayload();
                    break;
            }
            return response;
        }

        #endregion System

        #region Discovery

        /// <summary>
        /// Discovery happens heres
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

        #endregion Discovery

        #region Control

        #region PowerState

        /// <summary>
        /// Set Power State
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetPowerState/")]
        public ControlResponse SetPowerState(AlexaSetPowerStateControllerRequest request)
        {
            AlexaSetPowerStateController controller = new AlexaSetPowerStateController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }

            return controller.Response;
        }

        #endregion PowerState

        #region Brightness

        /// <summary>
        /// Adjust Brightness
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/AdjustBrightness/")]
        public ControlResponse AdjustBrightness(AlexaBrightnessControllerRequest request)
        {
            AlexaBrightnessController controller = new AlexaBrightnessController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        /// <summary>
        /// Set Brightness
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetBrightness/")]
        public ControlResponse SetBrightness(AlexaBrightnessControllerRequest request)
        {
            AlexaBrightnessController controller = new AlexaBrightnessController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion Brightness

        #region Scene

        /// <summary>
        /// Set Scene
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

        #endregion Scene

        #region ColorTemperature

        /// <summary>
        /// Adjust Color Temp
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/AdjustColorTemperature/")]
        public ControlResponse AdjustColorTemperature(AlexaColorTemperatureControllerRequest request)
        {
            AlexaColorTemperatureController controller = new AlexaColorTemperatureController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        /// <summary>
        /// Set Color Temp
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetColorTemperature/")]
        public ControlResponse SetColorTemperature(AlexaColorTemperatureControllerRequest request)
        {
            AlexaColorTemperatureController controller = new AlexaColorTemperatureController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion ColorTemperature

        #region Color

        /// <summary>
        /// Set Color
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetColor/")]
        public ControlResponse SetColor(AlexaColorControllerRequest request)
        {
            AlexaColorController controller = new AlexaColorController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion Color

        #region Thermostat

        /// <summary>
        /// Adjust temp
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/AdjustTargetTemperature/")]
        public ControlResponse AdjustTargetTemperature(AlexaThermostatControllerRequest request)
        {
            AlexaThermostatController controller = new AlexaThermostatController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        /// <summary>
        /// Set Temp
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetTargetTemperature/")]
        public ControlResponse SetTargetTemperature(AlexaThermostatControllerRequest request)
        {
            AlexaThermostatController controller = new AlexaThermostatController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        /// <summary>
        /// Set Tstat mode
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetThermostatMode/")]
        public ControlResponse SetThermostatMode(AlexaThermostatControllerRequest request)
        {
            AlexaThermostatController controller = new AlexaThermostatController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion Thermostat

        #region Speaker

        /// <summary>
        /// Speaker Controller
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/Speaker/")]
        public ControlResponse Speaker(AlexaSpeakerRequest request)
        {
            AlexaSpeaker controller = new AlexaSpeaker(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion Speaker

        #region InputController

        /// <summary>
        /// Input Controller
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/InputController/")]
        public ControlResponse InputController(AlexaInputControllerRequest request)
        {
            AlexaInputController controller = new AlexaInputController(request);
            if (controller.ValidateDirective(controller.GetDirectiveNames(), controller.GetNameSpace()))
            {
                controller.ProcessControllerDirective();
            }
            return controller.Response;
        }

        #endregion InputController

        #endregion Control

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

            #endregion Validate request

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

            #endregion Connect To Premise Server

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

            #endregion Verify Access

            #region Get Premise Object

            IPremiseObject endpoint;
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

            #endregion Get Premise Object

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
                                AlexaSetSceneController controller = new AlexaSetSceneController(endpoint);
                                AlexaProperty prop = controller.GetPropertyState();
                                response.@event.header.name = (string)prop.value;
                                response.@event.payload.cause = new ChangeReportCause
                                {
                                    type = "VOICE_INTERACTION"
                                };
                                response.@event.payload.timestamp = prop.timeOfSample;
                            }
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
            InformLastContact(string.Format("StateReport: {0}", response.@event?.endpoint?.cookie?.path)).GetAwaiter().GetResult();
            return response;
        }

        #endregion Report State

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

            #endregion Validate request

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

            #endregion Verify Access

            try
            {
                if (PremiseServer.HomeObject == null)
                {
                    response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.ENDPOINT_UNREACHABLE, "Premise Server.");
                    return response;
                }

                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationCode", directive.payload.grant.access_token).GetAwaiter().GetResult();
                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationRefreshToken", directive.payload.grant.refresh_token).GetAwaiter().GetResult();
                DateTime expiry = DateTime.UtcNow.AddSeconds(directive.payload.grant.expires_in);
                PremiseServer.HomeObject.SetValue("AlexaAsyncAuthorizationCodeExpiry", expiry.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
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

        #endregion Authorization
    }
}