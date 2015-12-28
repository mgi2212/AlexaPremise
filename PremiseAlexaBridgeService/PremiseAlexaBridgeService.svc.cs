using System;
using System.Linq;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using SYSWebSockClient;
using System.Threading.Tasks;

namespace PremiseAlexaBridgeService
{

    public static class HumanFriendlyInteger
    {
        static string[] ones = new string[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
        static string[] teens = new string[] { "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        static string[] tens = new string[] { "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
        static string[] thousandsGroups = { "", " Thousand", " Million", " Billion" };

        private static string FriendlyInteger(int n, string leftDigits, int thousands)
        {
            if (n == 0)
            {
                return leftDigits;
            }
            string friendlyInt = leftDigits;
            if (friendlyInt.Length > 0)
            {
                friendlyInt += " ";
            }

            if (n < 10)
            {
                friendlyInt += ones[n];
            }
            else if (n < 20)
            {
                friendlyInt += teens[n - 10];
            }
            else if (n < 100)
            {
                friendlyInt += FriendlyInteger(n % 10, tens[n / 10 - 2], 0);
            }
            else if (n < 1000)
            {
                friendlyInt += FriendlyInteger(n % 100, (ones[n / 100] + " Hundred"), 0);
            }
            else
            {
                friendlyInt += FriendlyInteger(n % 1000, FriendlyInteger(n / 1000, "", thousands + 1), 0);
            }

            return friendlyInt + thousandsGroups[thousands];
        }

        public static string IntegerToWritten(int n)
        {
            if (n == 0)
            {
                return "Zero";
            }
            else if (n < 0)
            {
                return "Negative " + IntegerToWritten(-n);
            }

            return FriendlyInteger(n, "", 0);
        }
    }

    public class PremiseAlexaService : IPremiseAlexaService
    {
        PremiseServer Server = PremiseServer.Instance;
        /// <summary>
        /// System messages
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/System/")]
        public HealthCheckResponse Health(HealthCheckRequest alexaRequest)
        {
            this.InformLastContact("HealthCheckRequest");

            var response = new HealthCheckResponse();
            var err = new ExceptionResponsePayload();

            response.header = new Header();
            response.header.name = "HealthCheckResponse";
            response.header.@namespace = "System";
            response.payload = new HealthCheckResponsePayload();
            this.GetAlexaStatusHealth(response.payload);
            return response;
        }

        private void GetAlexaStatusHealth(HealthCheckResponsePayload payload)
        {
            var returnClause = new string[] { "Health", "HealthDescription" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            var items = this.Server.AlexaStatus.Select(returnClause, whereClause).GetAwaiter().GetResult();
            payload.isHealthy = this.Server.AlexaStatus.GetValue<bool>("Health").GetAwaiter().GetResult();
            payload.description = this.Server.AlexaStatus.GetValue<string>("HealthDescription").GetAwaiter().GetResult();
        }

        private async void InformLastContact(string command)
        {
            await this.Server.AlexaStatus.SetValue("LastHeardFromAlexa", DateTime.Now.ToString());
            await this.Server.AlexaStatus.SetValue("LastHeardCommand", command);
        }

        private string GetAlexaStatusAccessToken()
        {
            // bogus async whatever
            var accessToken = this.Server.AlexaStatus.GetValue<string>("AccessToken").GetAwaiter().GetResult();
            return accessToken;
        }

        private async Task<List<Appliance>> GetAppliances()
        {
            List<Appliance> appliances = new List<Appliance>();

            var returnClause = new string[] { "Name", "DisplayName", "FriendlyName","FriendlyDescription", "IsReachable", "PowerState", "Brightness", "OID", "OPATH", "OTYPENAME", "Type" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            whereClause.TypeOf = this.Server.AlexaApplianceClassPath;

            var sysAppliances = await this.Server.HomeObject.Select(returnClause, whereClause);
            int count = 0;
            //int collisionCount = 0;
            int noFriendlyNameCount = 0;

            foreach (var sysAppliance in sysAppliances)
            {
                var appliance = new Appliance();

                var objectId = (string)sysAppliance.OID;
                appliance.applianceId = Guid.Parse(objectId).ToString("D");
                appliance.modelName = sysAppliance.OTYPENAME;
                appliance.friendlyName = ((string) sysAppliance.FriendlyName).Trim();
                appliance.friendlyDescription = ((string) sysAppliance.FriendlyDescription).Trim();

                // if no FriendlyName value then try to invent one and set it so we dont have to do this again!
                if (string.IsNullOrEmpty(appliance.friendlyName))
                {
                    noFriendlyNameCount++;
                    var premiseObject = await this.Server.HomeObject.GetObject(objectId);
                    var parent = await premiseObject.GetParent();

                    // parent should be a container - so get that name
                    string parentName = (await parent.GetName()).Trim();

                    // preceed the parent container name with the appliance name
                    appliance.friendlyName = string.Format("{0} {1}", parentName, sysAppliance.Name).Trim();
                    await premiseObject.SetValue("FriendlyName", appliance.friendlyName); 
                }

                // if no FriendlyDescription
                if (string.IsNullOrEmpty(appliance.friendlyDescription))
                    appliance.friendlyDescription = sysAppliance.OPATH;

                appliance.isReachable = sysAppliance.IsReachable;
                appliance.manufacturerName = "Premise Object";
                appliance.version = "1.0";
                bool hasDimmer = (sysAppliance.Brightness != null);
                appliance.additionalApplianceDetails = new AdditionalApplianceDetails()
                {
                    dimmable = hasDimmer.ToString(),
                    path = sysAppliance.OPATH
                };

#if false
                // detect, avoid and flag friendly name collisions
                Appliance existing;
                if (this.Appliances.TryGetValue(appliance.friendlyName, out existing))
                {
                    collisionCount++;
                    appliance.friendlyName = string.Format("Name Collision {0} {1}", HumanFriendlyInteger.IntegerToWritten(collisionCount), appliance.friendlyName);
                    appliance.friendlyDescription = targetObjectPath;
                }
#endif
                appliances.Add(appliance);
                if (++count >= this.Server.AlexaDeviceLimit)
                    break;
            }

            await this.Server.AlexaStatus.SetValue("RefreshDevices", "False");
            await this.Server.AlexaStatus.SetValue("LastRefreshed", DateTime.Now.ToString());
            await this.Server.AlexaStatus.SetValue("HealthDescription", string.Format("Reported={0},NameCollisions={1},NoFriendlyNameCount={2}", count, 0, noFriendlyNameCount));
            await this.Server.AlexaStatus.SetValue("Health", "True");

            return appliances;
        }

        /// <summary>
        /// Discovry - proxy call to premise looking for the AlexaEx class
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        public DiscoveryResponse Discover(DiscoveryRequest alexaRequest)
        {
            this.InformLastContact("DiscoveryRequest");

            var response = new DiscoveryResponse();
            response.payload = new DiscoveryResponsePayload();
            response.payload.discoveredAppliances = null;
            response.payload.exception = null;
            if (alexaRequest == null)
            {
                response.header = new Header();
                response.header.name = "DiscoverAppliancesResponse";
                response.header.@namespace = "Discovery";
                var err = new ExceptionResponsePayload();
                err.code = "INTERNAL_ERROR";
                err.description = "Null request header.";
                response.payload.exception = err;
                return response;
            }

            response.header = alexaRequest.header;

            if (alexaRequest.header.name != "DiscoverAppliancesRequest")
            {
                response.header.name = "DiscoverAppliancesResponse";
                var err = new ExceptionResponsePayload();
                err.code = "INTERNAL_ERROR";
                err.description = "Unexpected request received.";
                response.payload.exception = err;
                return response;
            }
            response.header.name = "DiscoverAppliancesResponse";

            string accessToken = this.GetAlexaStatusAccessToken();
            if (alexaRequest.payload.accessToken != accessToken)
            {
                var err = new ExceptionResponsePayload();
                err.code = "INVALID_ACCESS_TOKEN";
                err.description = "Invalid access token.";
                response.payload.exception = err;
                return response;
            }
            response.payload.discoveredAppliances = this.GetAppliances().GetAwaiter().GetResult();
            return response;
        }

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
            this.InformLastContact("ControlRequest");

            var response = new ControlResponse();
            var err = new ExceptionResponsePayload();
            response.payload = new ApplianceControlResponsePayload();

            // bad request
            if (alexaRequest == null)
            {
                response.header = new Header();
                response.header.name = "SwitchOnOffResponse";
                response.header.@namespace = "Control";
                err.code = "INTERNAL_ERROR";
                err.description = "Null request header.";
                response.payload.exception = err;
                return response;
            }
            ControlRequestType requestType = ControlRequestType.Unknown;
            response.header = alexaRequest.header;

            // determine request type
            switch (alexaRequest.header.name)
            {
                case "SwitchOnOffRequest":
                    requestType = ControlRequestType.SwitchOnOff;
                    response.header.name = "SwitchOnOffResponse";
                    break;
                case "AdjustNumericalSettingRequest":
                    requestType = ControlRequestType.AdjustNumericalSetting;
                    response.header.name = "AdjustNumericalSettingResponse";
                    break;
                default:
                    response.header = new Header();
                    try
                    {
                        response.header.name = alexaRequest.header.name.Replace("Request", "Response");
                    }
                    catch
                    {
                        response.header.name = "UnknownResponse";
                    }
                    response.header.@namespace = "Control";
                    err.code = "UNSUPPORTED_OPERATION";
                    err.description = string.Format("{0} - Unknown or unsupported Request.", alexaRequest.header.name);
                    response.payload.exception = err;
                    return response;
            }

            // check for access privleges
            string accessToken = this.GetAlexaStatusAccessToken();
            if (alexaRequest.payload.accessToken != accessToken)
            { 
                err.code = "INVALID_ACCESS_TOKEN";
                err.description = "Invalid access token.";
                response.payload.exception = err;
                return response;
            }

            Guid premiseId = new Guid(alexaRequest.payload.appliance.applianceId);
            var applianceToControl = this.Server.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
            // find the appliance to control
            // Server.Appliances.Values.First(x => x.applianceId.ToUpper() == alexaRequest.payload.appliance.applianceId.ToUpper());
            if (applianceToControl == null)
            {
                err.code = "NO_SUCH_TARGET";
                err.description = string.Format("Cannot find device {0} ({1})", alexaRequest.payload.appliance.additionalApplianceDetails.path, alexaRequest.payload.appliance.applianceId);
                response.payload.exception = err;
                return response;
            }

            // check state and determine if it needs to be changed
            if (requestType == ControlRequestType.SwitchOnOff)
            {
                bool state = applianceToControl.GetValue<bool>("PowerState").GetAwaiter().GetResult();
                switch (alexaRequest.payload.switchControlAction.ToUpper())
                {
                    case "TURN_OFF":
                        if (state == true)
                        {
                            applianceToControl.SetValue("PowerState", "False");
                            response.payload.success = true;
                        }
                        else
                        {
                            err.code = "TARGET_ALREADY_AT_REQUESTED_STATE";
                            err.description = "Appliance is off.";
                            response.payload.exception = err;
                            return response;
                        }
                        break;
                    case "TURN_ON":
                        if (state == false)
                        {
                            applianceToControl.SetValue("PowerState", "True");
                            response.payload.success = true;
                        }
                        else
                        {
                            err.code = "TARGET_ALREADY_AT_REQUESTED_STATE";
                            err.description = "Appliance is on.";
                            response.payload.exception = err;
                            return response;
                        }
                        break;
                    default:
                        err.code = "UNSUPPORTED_OPERATION";
                        err.description = "Unknown or unsupported operation.";
                        response.payload.exception = err;
                        return response;
                }
            }

            // currently only percentage is viable here
            if (requestType == ControlRequestType.AdjustNumericalSetting)
            {
                bool dimmable = bool.Parse(alexaRequest.payload.appliance.additionalApplianceDetails.dimmable);
                if ((dimmable == false) || (alexaRequest.payload.adjustmentUnit != "PERCENTAGE"))
                {
                    err.code = "UNSUPPORTED_TARGET_SETTING";
                    if ((alexaRequest.payload.adjustmentUnit != "PERCENTAGE"))
                    {
                        err.description = string.Format("{0} is an unsupported adjustmentUnit", alexaRequest.payload.adjustmentUnit);
                    }
                    else
                    {
                        err.description = "Appliance is not dimmable.";
                    }
                    response.payload.exception = err;
                    return response;
                }
                double currentBrightnessValue = applianceToControl.GetValue<Double>("Brightness").GetAwaiter().GetResult(); 
                bool relativeAdustment = false;
                switch (alexaRequest.payload.adjustmentType.ToUpper())
                {
                    case "RELATIVE":
                        relativeAdustment = true;
                        break;

                    case "ABSOLUTE":
                        relativeAdustment = false;
                        break;

                    default:
                        err.code = "UNSUPPORTED_TARGET_SETTING";
                        err.description = "Appliance is not dimmable.";
                        response.payload.exception = err;
                        return response;
                }

                currentBrightnessValue = Math.Round(currentBrightnessValue * 100.0, 0);
                var adjustValue = Math.Round(double.Parse(alexaRequest.payload.adjustmentValue), 0);
                var resultValue = 0.0;

                if (relativeAdustment)
                {
                    resultValue = Math.Round(currentBrightnessValue + adjustValue, 0);
                }
                else
                {
                    resultValue = adjustValue;
                }

                if ((resultValue > 100.0) || (resultValue < 0))
                {
                    err.code = "TARGET_SETTING_OUT_OF_RANGE";
                    err.description = string.Format("current={0} adjust={1}", currentBrightnessValue, adjustValue);
                    response.payload.exception = err;
                    return response;
                }
                resultValue = resultValue / 100.0;
                applianceToControl.SetValue("Brightness", resultValue.ToString()).GetAwaiter().GetResult();
                response.payload.success = true;
            }

            return response;
        }
    }
}
