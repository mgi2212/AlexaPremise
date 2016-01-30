using System;
using System.Linq;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using SYSWebSockClient;
using System.Threading.Tasks;
using Common.Logging;

namespace PremiseAlexaBridgeService
{

    public static class InputExtensions
    {
        public static double LimitToRange(
            this double value, double inclusiveMinimum, double inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }
    }

    public class PreWarmCache : System.Web.Hosting.IProcessHostPreloadClient
    {
        PremiseServer ServiceInstance;

        ILog log;

        public void Preload(string[] parameters)
        {
            log = LogManager.GetLogger(LogManager.Adapter.GetType());
            log.Debug("AlexaPremiseBridge Preload");
            ServiceInstance = PremiseServer.Instance;
        }
    }

    public class PremiseAlexaService : IPremiseAlexaService
    {
        PremiseServer ServiceInstance = PremiseServer.Instance;

        #region System

        /// <summary>
        /// System messages
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/System/")]
        public SystemResponse System(SystemRequest alexaRequest)
        {

            SYSClient client = new SYSClient();

            var response = new SystemResponse();
            response.header = new Header();
            response.header.@namespace = "System";
            response.payload = new SystemResponsePayload();

            try
            {
                ServiceInstance.ConnectToServer(client);
            }
            catch (Exception)
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "Cannot Connect To Premise Server!");
                return response;
            }


            SystemRequestType requestType = SystemRequestType.Unknown;

            switch (alexaRequest.header.name)
            {
                case "HealthCheckRequest":
                    this.InformLastContact("System:HealthCheckRequest");

                    string accessToken = this.GetAlexaStatusAccessToken().GetAwaiter().GetResult();
                    if (alexaRequest.payload.accessToken != accessToken)
                    {
                        response.payload.exception = GetExceptionPayload("INVALID_ACCESS_TOKEN", "Access denied.");
                        break;
                    }

                    requestType = SystemRequestType.HealthCheck;
                    response.header.name = "HealthCheckResponse";
                    response.payload = this.GetHealthCheckResponse();
                    break;

                default:
                    try
                    {
                        response.header.name = alexaRequest.header.name.Replace("Request", "Response");
                    }
                    catch
                    {
                        response.header.name = "UnknownResponse";
                    }
                    response.payload.exception = GetExceptionPayload("UNSUPPORTED_OPERATION", string.Format("{0} or unsupported Request = '{1}'.", requestType.ToString(), alexaRequest.header.name));
                    break;
            }
            ServiceInstance.DisconnectServer(client);
            return response;
        }

        private SystemResponsePayload GetHealthCheckResponse()
        {
            SystemResponsePayload payload = new SystemResponsePayload(); 
            var returnClause = new string[] { "Health", "HealthDescription" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            payload.isHealthy = this.ServiceInstance.AlexaStatus.GetValue<bool>("Health").GetAwaiter().GetResult();
            payload.description = this.ServiceInstance.AlexaStatus.GetValue<string>("HealthDescription").GetAwaiter().GetResult();
            return payload;
        }

        #endregion

        #region Discovery

        /// <summary>
        /// Discovry - proxy call to premise looking for the AlexaEx class
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        public DiscoveryResponse Discovery(DiscoveryRequest alexaRequest)
        {

            SYSClient client = new SYSClient();

            var response = new DiscoveryResponse();
            response.header = new Header();
            response.header.name = "DiscoverAppliancesResponse";
            response.header.@namespace = "Discovery";
            response.payload = new DiscoveryResponsePayload();

            try
            {
                ServiceInstance.ConnectToServer(client);
            }
            catch (Exception)
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "Cannot Connect To Server!");
                return response;
            }

            this.InformLastContact("DiscoveryRequest");


            if (alexaRequest == null)
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "Null request header!");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            //response.header = alexaRequest.header;

            if (alexaRequest.header.name != "DiscoverAppliancesRequest")
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "'(0)' is an unexpected request.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            string accessToken = this.GetAlexaStatusAccessToken().GetAwaiter().GetResult();
            if (alexaRequest.payload.accessToken != accessToken)
            {
                response.payload.exception = GetExceptionPayload("INVALID_ACCESS_TOKEN", "Access denied.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            response.payload.discoveredAppliances = this.GetAppliances().GetAwaiter().GetResult();
            ServiceInstance.DisconnectServer(client);
            return response;
        }

        private async Task<List<Appliance>> GetAppliances()
        {
            List<Appliance> appliances = new List<Appliance>();

            var returnClause = new string[] { "Name", "DisplayName", "FriendlyName", "FriendlyDescription", "IsReachable", "PowerState", "Brightness", "OID", "OPATH", "OTYPENAME", "Type" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            whereClause.TypeOf = this.ServiceInstance.AlexaApplianceClassPath;

            var sysAppliances = await this.ServiceInstance.HomeObject.Select(returnClause, whereClause);
            int count = 0;
            int generatedNameCount = 0;

            foreach (var sysAppliance in sysAppliances)
            {
                var objectId = (string)sysAppliance.OID;
                var appliance = new Appliance()
                {
                    applianceId = Guid.Parse(objectId).ToString("D"),
                    manufacturerName = "Premise Object",
                    version = "1.0",
                    isReachable = sysAppliance.IsReachable,
                    modelName = sysAppliance.OTYPENAME,
                    friendlyName = ((string)sysAppliance.FriendlyName).Trim(),
                    friendlyDescription = ((string)sysAppliance.FriendlyDescription).Trim()
                };

                // the FriendlyName is what Alexa tries to match when finding devices, so we need one
                // if no FriendlyName value then try to invent one and set it so we dont have to do this again!
                if (string.IsNullOrEmpty(appliance.friendlyName))
                {
                    generatedNameCount++;
                    // parent should be a container - so get that name
                    var premiseObject = await this.ServiceInstance.HomeObject.GetObject(objectId);
                    var parent = await premiseObject.GetParent();
                    string parentName = (await parent.GetName()).Trim();

                    // preceed the parent container name with the appliance name
                    appliance.friendlyName = string.Format("{0} {1}", parentName, sysAppliance.Name).Trim();

                    // set the value in the dom
                    await premiseObject.SetValue("FriendlyName", appliance.friendlyName);
                }

                // Deal with empty FriendlyDescription
                if (string.IsNullOrEmpty(appliance.friendlyDescription))
                {
                    generatedNameCount++;
                    // parent should be a container - so get that name
                    var premiseObject = await this.ServiceInstance.HomeObject.GetObject(objectId);
                    var parent = await premiseObject.GetParent();
                    string parentName = (await parent.GetName()).Trim();

                    // results in something like = "A Sconce in the Entry."
                    // appending the path may make it easier to locate in the early Amazon UI - we'll see
                    // appliance.friendlyDescription = string.Format("A {0} in the {1}. Path={2}", sysAppliance.OTYPENAME, parentName, sysAppliance.OPATH).Trim();

                    appliance.friendlyDescription = string.Format("A {0} in the {1}", sysAppliance.OTYPENAME, parentName).Trim();

                    // set the value in the premise dom
                    await premiseObject.SetValue("FriendlyDescription", appliance.friendlyDescription);
                }

                // construct the additional details
                bool hasDimmer = (sysAppliance.Brightness != null);
                appliance.additionalApplianceDetails = new AdditionalApplianceDetails()
                {
                    dimmable = hasDimmer.ToString(),
                    path = sysAppliance.OPATH
                };

                appliances.Add(appliance);
                if (++count >= this.ServiceInstance.AlexaDeviceLimit)
                    break;
            }

            await this.ServiceInstance.AlexaStatus.SetValue("RefreshDevices", "False");
            await this.ServiceInstance.AlexaStatus.SetValue("LastRefreshed", DateTime.Now.ToString());
            await this.ServiceInstance.AlexaStatus.SetValue("HealthDescription", string.Format("Reported={0},Names Generated={1}", count, generatedNameCount));
            await this.ServiceInstance.AlexaStatus.SetValue("Health", "True");

            return appliances;
        }

        #endregion

        #region Control

        /// <summary>
        /// Control Requests are process here
        /// 1) validate the json
        /// 2) determine the request type
        /// 3) check for 'token' access privs
        /// 4) find the premise object to control
        /// 5) check state
        /// 6) change state if needed
        /// 7) report back
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Control/")]
        public ControlResponse Control(ControlRequest alexaRequest)
        {
            SYSClient client = new SYSClient();

            // allocate and setup header
            var response = new ControlResponse();
            response.header = new Header();
            response.header.name = "ControlResponse";   // generic in case of a null request
            response.header.@namespace = "Control";

            // allocate a payload
            response.payload = new ApplianceControlResponsePayload();

            try
            {
                ServiceInstance.ConnectToServer(client);
            }
            catch (Exception)
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "Cannot Connect To Server!");
                return response;
            }

            this.InformLastContact("ControlRequest");

            // check for a bad request
            if (alexaRequest == null)
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "Null request header.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            try
            {
                response.header.name = alexaRequest.header.name.Replace("Request", "Response");
            }
            catch (Exception)
            {
                // TODO: need to log these
            }


            // check access privleges
            string accessToken = this.GetAlexaStatusAccessToken().GetAwaiter().GetResult();
            if (alexaRequest.payload.accessToken != accessToken)
            {
                response.payload.exception = GetExceptionPayload("INVALID_ACCESS_TOKEN", "Access denied.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            // check request types
            ControlRequestType requestType = ControlRequestType.Unknown;

            switch (alexaRequest.header.name)
            {
                case "SwitchOnOffRequest":
                    requestType = ControlRequestType.SwitchOnOff;
                    break;
                case "AdjustNumericalSettingRequest":
                    requestType = ControlRequestType.AdjustNumericalSetting;
                    break;
                default:
                    response.payload.exception = GetExceptionPayload("UNSUPPORTED_OPERATION", string.Format("{0} - Unknown Control Request.", alexaRequest.header.name));
                    ServiceInstance.DisconnectServer(client);
                    return response;
            }

            // get the object
            Guid premiseId = new Guid(alexaRequest.payload.appliance.applianceId);
            var applianceToControl = this.ServiceInstance.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
            
            // report failure
            if (applianceToControl == null)
            {
                response.payload.exception = GetExceptionPayload("NO_SUCH_TARGET", string.Format("Cannot find device {0} ({1})", alexaRequest.payload.appliance.additionalApplianceDetails.path, alexaRequest.payload.appliance.applianceId));
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            // check state and determine if it needs to be changed
            if (requestType == ControlRequestType.SwitchOnOff)
            {
                bool state = applianceToControl.GetValue<bool>("PowerState").GetAwaiter().GetResult();
                switch (alexaRequest.payload.switchControlAction.ToUpper())
                {
                    case "TURN_OFF":
                        //if (state == true)
                        //{
                            applianceToControl.SetValue("PowerState", "False").GetAwaiter().GetResult(); 
                            response.payload.success = true;
                        //}
                        //else
                        //{
                        //    response.payload.exception = GetExceptionPayload("TARGET_ALREADY_AT_REQUESTED_STATE", "Appliance is off."); ;
                        //    return response;
                        //}
                        break;
                    case "TURN_ON":
                        //if (state == false)
                        //{
                            applianceToControl.SetValue("PowerState", "True").GetAwaiter().GetResult();
                            response.payload.success = true;
                        //}
                        //else
                        //{
                        //    response.payload.exception = GetExceptionPayload("TARGET_ALREADY_AT_REQUESTED_STATE", "Appliance is On."); 
                        //    return response;
                        //}
                        break;
                    default:
                        response.payload.exception = GetExceptionPayload("UNSUPPORTED_OPERATION", "Unknown or unsupported operation.");
                        ServiceInstance.DisconnectServer(client);
                        return response;
                }
            }

            // currently only percentage is viable here
            if (requestType == ControlRequestType.AdjustNumericalSetting)
            {
                bool dimmable = bool.Parse(alexaRequest.payload.appliance.additionalApplianceDetails.dimmable);

                if ((dimmable == false) || (alexaRequest.payload.adjustmentUnit.ToUpper() != "PERCENTAGE"))
                {
                    response.payload.exception = GetExceptionPayload("UNSUPPORTED_TARGET_SETTING", "Appliance is not dimmable.");
                    if ((alexaRequest.payload.adjustmentUnit.ToUpper() != "PERCENTAGE"))
                    {
                        response.payload.exception.description = string.Format("{0} is an unsupported adjustment command.", alexaRequest.payload.adjustmentUnit);
                    }
                    ServiceInstance.DisconnectServer(client);
                    return response;
                }

                /*
                The adjustmentValue Value is the adjustment to apply to the specified appliance. This is a 64-bit
                double value accurate up to two decimal places.

                When the adjustmentUnit is PERCENTAGE and the adjustmentType is ABSOLUTE, 
                the possible range of the adjustmentValue is 0.00 to 100.00 inclusive.

                When the adjustmentUnit is PERCENTAGE and the adjustmentType is RELATIVE, 
                the possible range of the adjustmentValue is -100.00 to 100.00 inclusive.
                */

                // obtain the adjustValue
                double adjustValue = Math.Round(double.Parse(alexaRequest.payload.adjustmentValue), 2);

                // allocate a value to send
                double valueToSend = 0.00;

                // determine relative or absolute operation
                switch (alexaRequest.payload.adjustmentType.ToUpper())
                {
                    case "RELATIVE":
                        // since it's relative, get current level of the dimmer and convert to percentage 
                        double currentBrightnessValue = applianceToControl.GetValue<Double>("Brightness").GetAwaiter().GetResult();
                        currentBrightnessValue = Math.Round(currentBrightnessValue * 100.00, 2);

                        // do the math and limit the range of the result
                        valueToSend = Math.Round(currentBrightnessValue + adjustValue, 2).LimitToRange(0.00, 100.00);
                        break;

                    case "ABSOLUTE":
                        valueToSend = adjustValue.LimitToRange(0.00, 100.00);
                        break;

                    default:
                        response.payload.exception = GetExceptionPayload("UNSUPPORTED_TARGET_SETTING", "Appliance is not ajustable.");
                        ServiceInstance.DisconnectServer(client);
                        return response;
                }

                // convert from percentage and maintain fractional accuracy
                valueToSend = Math.Round(valueToSend / 100.00, 4);

                // set the device
                applianceToControl.SetValue("Brightness", valueToSend.ToString()).GetAwaiter().GetResult();
                response.payload.success = true;
            }

            ServiceInstance.DisconnectServer(client);
            return response;
        }

        #endregion

        #region Utility

        private static ExceptionResponsePayload GetExceptionPayload(string exceptionCode, string exceptionDescription)
        {
            var err = new ExceptionResponsePayload()
            {
                code = exceptionCode,
                description = exceptionDescription
            };
            return err;
        }

        private async void InformLastContact(string command)
        {
            await this.ServiceInstance.AlexaStatus.SetValue("LastHeardFromAlexa", DateTime.Now.ToString());
            await this.ServiceInstance.AlexaStatus.SetValue("LastHeardCommand", command);
        }

        private async Task<string> GetAlexaStatusAccessToken()
        {
            var accessToken = await this.ServiceInstance.AlexaStatus.GetValue<string>("AccessToken");
            return accessToken;
        }

        #endregion
    }
}
