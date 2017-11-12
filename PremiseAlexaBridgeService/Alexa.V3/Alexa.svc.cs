using Alexa;
using Alexa.AV;
using Alexa.Discovery;
using Alexa.HVAC;
using Alexa.Lighting;
using Alexa.Power;
using Alexa.Scene;
using Alexa.SmartHomeAPI.V3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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

    public static class InputExtensions
    {
        #region Methods

        public static void AddUnique<TList>(this IList<TList> self, IEnumerable<TList> items)
        {
            foreach (var item in items)
                if (!self.Contains(item))
                    self.Add(item);
        }

        public static double LimitToRange(
            this double value, double inclusiveMinimum, double inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }

        public static int LimitToRange(
            this int value, int inclusiveMinimum, int inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }

        #endregion Methods
    }

    public class PremiseAlexaV3Service : IPremiseAlexaV3Service
    {
        #region System

        public SystemResponsePayload GetHealthCheckResponseV3()
        {
            SystemResponsePayload payload = new SystemResponsePayload
            {
                isHealthy = PremiseServer.HomeObject.GetValueAsync<bool>("Health").GetAwaiter().GetResult(),
                description = PremiseServer.HomeObject.GetValueAsync<string>("HealthDescription").GetAwaiter().GetResult()
            };
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
                    PremiseServer.InformLastContactAsync("System:HealthCheckRequest").GetAwaiter().GetResult();
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
        /// Discovery happens here
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        public DiscoveryControllerResponse Discovery(AlexaDiscoveryControllerRequest request)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaDiscoveryController controller = new AlexaDiscoveryController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            PremiseServer.WriteToWindowsApplicationEventLog(EventLogEntryType.Information, $"Discovery processing time {stopwatch.ElapsedMilliseconds}ms", 51);
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaSetPowerStateController controller = new AlexaSetPowerStateController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaBrightnessController controller = new AlexaBrightnessController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaBrightnessController controller = new AlexaBrightnessController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaSetSceneController controller = new AlexaSetSceneController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaColorTemperatureController controller = new AlexaColorTemperatureController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaColorTemperatureController controller = new AlexaColorTemperatureController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaColorController controller = new AlexaColorController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaThermostatController controller = new AlexaThermostatController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaThermostatController controller = new AlexaThermostatController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
            return controller.Response;
        }

        /// <summary>
        /// Set Thermostat mode
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/SetThermostatMode/")]
        public ControlResponse SetThermostatMode(AlexaThermostatControllerRequest request)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaThermostatController controller = new AlexaThermostatController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            AlexaSpeaker controller = new AlexaSpeaker(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();

            AlexaInputController controller = new AlexaInputController(request);
            if (controller.ValidateDirective())
            {
                controller.ProcessControllerDirective();
            }
            stopwatch.Stop();
            controller.Response.Event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
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
            Stopwatch stopwatch = Stopwatch.StartNew();

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
                if (PremiseServer.HomeObject == null)
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

                if (!PremiseServer.CheckAccessTokenAsync(directive.endpoint.scope.localAccessToken).GetAwaiter().GetResult())
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
                endpoint = PremiseServer.HomeObject.GetObjectAsync(premiseId.ToString("B")).GetAwaiter().GetResult();
                if (!endpoint.IsValidObject())
                {
                    response.context = null;
                    response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.NO_SUCH_ENDPOINT,
                        $"Cannot find device {directive.endpoint.endpointId} on server.");
                    return response;
                }
            }
            catch
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.NO_SUCH_ENDPOINT,
                    $"Cannot find device {directive.endpoint.endpointId} on server.");
                return response;
            }

            if (directive.header.name != "ReportState")
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INVALID_DIRECTIVE, "Invalid Directive");
                return response;
            }

            #endregion Get Premise Object

            DiscoveryEndpoint discoveryEndpoint = PremiseServer.GetDiscoveryEndpointAsync(endpoint).GetAwaiter().GetResult();
            if (discoveryEndpoint == null)
            {
                response.context = null;
                response.@event.payload = new AlexaErrorResponsePayload(AlexaErrorTypes.INTERNAL_ERROR,
                    $"Cannot find or invalid discoveryJson for {directive.endpoint.endpointId} on server.");
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
                  .Select(Activator.CreateInstance);

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
                                AlexaProperty prop = controller.GetPropertyStates()[0];
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
            PremiseServer.InformLastContactAsync($"StateReport: {response.@event?.endpoint?.cookie?.path}").GetAwaiter().GetResult();
            stopwatch.Stop();
            if (response.@event.endpoint.cookie != null)
            {
                response.@event.endpoint.cookie.processingTime = stopwatch.ElapsedMilliseconds;
            }
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
                if (!PremiseServer.CheckAccessTokenAsync(directive.payload.grantee.localAccessToken).GetAwaiter().GetResult())
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

                using (PremiseServer.AsyncObjectsLock.Lock())
                {
                    PremiseServer.HomeObject.SetValueAsync("AlexaAsyncAuthorizationCode", directive.payload.grant.access_token).GetAwaiter().GetResult();
                    PremiseServer.HomeObject.SetValueAsync("AlexaAsyncAuthorizationRefreshToken", directive.payload.grant.refresh_token).GetAwaiter().GetResult();
                    PremiseServer.HomeObject.SetValueAsync("AlexaAsyncAuthorizationClientId", directive.payload.grant.client_id).GetAwaiter().GetResult();
                    PremiseServer.HomeObject.SetValueAsync("AlexaAsyncAuthorizationSecret", directive.payload.grant.client_secret).GetAwaiter().GetResult();

                    DateTime expiry = DateTime.UtcNow.AddSeconds(directive.payload.grant.expires_in);
                    PremiseServer.HomeObject.SetValueAsync("AlexaAsyncAuthorizationCodeExpiry", expiry.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                }

                const string message = "Skill is now enabled and authorized to send async updates to Alexa. A task has been started to subscribe to property change events.";
                PremiseServer.InformLastContactAsync(message).GetAwaiter().GetResult();
                PremiseServer.WriteToWindowsApplicationEventLog(EventLogEntryType.Information, message, 60);

                Task.Run(async () =>
                {
                    // Generate Discovery Json
                    await PremiseServer.HomeObject.SetValueAsync("GenerateDiscoveryJson", "True").ConfigureAwait(false);
                    // Signal sending async property change events - this will also subscribe to all properties
                    await PremiseServer.HomeObject.SetValueAsync("SendAsyncEventsToAlexa", "True").ConfigureAwait(false);
                });
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