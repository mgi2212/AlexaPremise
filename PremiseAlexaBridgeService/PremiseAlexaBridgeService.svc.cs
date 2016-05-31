using System;
using System.Collections.Generic;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using SYSWebSockClient;
using Alexa.SmartHome;

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

        //ILog log;

        public void Preload(string[] parameters)
        {
            //log = LogManager.GetLogger(LogManager.Adapter.GetType());
            //log.Debug("AlexaPremiseBridge Preload");
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

                    if (!CheckAccessToken(alexaRequest.payload.accessToken).GetAwaiter().GetResult())
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
            payload.isHealthy = this.ServiceInstance.HomeObject.GetValue<bool>("Health").GetAwaiter().GetResult();
            payload.description = this.ServiceInstance.HomeObject.GetValue<string>("HealthDescription").GetAwaiter().GetResult();
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
            response.header.messageId = alexaRequest.header.messageId;
            response.header.name = "DiscoverAppliancesResponse";
            response.header.@namespace = alexaRequest.header.@namespace;
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
            if (alexaRequest.header.payloadVersion != "2")
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "'(0)' unexpected payload version.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            if (alexaRequest.header.name != "DiscoverAppliancesRequest")
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "'(0)' is an unexpected request.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            if (!CheckAccessToken(alexaRequest.payload.accessToken).GetAwaiter().GetResult())
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

            var returnClause = new string[] { "Name", "DisplayName", "FriendlyName", "FriendlyDescription", "IsReachable", "IsDiscoverable", "PowerState", "Brightness", "Temperature", "TemperatureMode", "OID", "OPATH", "OTYPENAME", "Type" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            whereClause.TypeOf = this.ServiceInstance.AlexaApplianceClassPath;

            var sysAppliances = await this.ServiceInstance.HomeObject.Select(returnClause, whereClause);
            int count = 0;
            int generatedNameCount = 0;

            foreach (var sysAppliance in sysAppliances)
            {
                if (sysAppliance.IsDiscoverable == false)
                    continue;

                var objectId = (string)sysAppliance.OID;
                var appliance = new Appliance()
                {
                    actions = new List<string>(),
                    applianceId = Guid.Parse(objectId).ToString("D"),
                    manufacturerName = "Premise Object",
                    version = "2.0",
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
                bool hasPowerState = (sysAppliance.PowerState != null);
                bool hasTemperature = (sysAppliance.Temperature != null);
                bool hasDimmer = (sysAppliance.Brightness != null);

                appliance.additionalApplianceDetails = new AdditionalApplianceDetails()
                {
                    dimmable = hasDimmer.ToString(),
                    path = sysAppliance.OPATH
                };

                if (hasPowerState)
                {
                    appliance.actions.Add("turnOn");
                    appliance.actions.Add("turnOff");
                    appliance.additionalApplianceDetails.purpose = "Unknown";
                }
                if (hasDimmer)
                {
                    appliance.actions.Add("setPercentage");
                    appliance.actions.Add("incrementPercentage");
                    appliance.actions.Add("decrementPercentage");
                    appliance.additionalApplianceDetails.purpose = "DimmableLight";
                }
                if (hasTemperature)
                {
                    appliance.actions.Add("setTemperature");
                    appliance.actions.Add("incrementTemperature");
                    appliance.actions.Add("decrementTemperature");
                    appliance.additionalApplianceDetails.purpose = "Thermostat";
                }

                appliances.Add(appliance);
                if (++count >= this.ServiceInstance.AlexaDeviceLimit)
                    break;
            }

            await this.ServiceInstance.HomeObject.SetValue("LastRefreshed", DateTime.Now.ToString());
            await this.ServiceInstance.HomeObject.SetValue("HealthDescription", string.Format("Reported={0},Names Generated={1}", count, generatedNameCount));
            await this.ServiceInstance.HomeObject.SetValue("Health", "True");

            return appliances;
        }

        #endregion

        #region Control

        /// <summary>
        /// Control Requests are processed here
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
            response.header.messageId = alexaRequest.header.messageId;
            response.header.@namespace = alexaRequest.header.@namespace;
            response.header.name = "Confirmation";   // generic in case of a null request
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

            this.InformLastContact("ControlRequest:" + alexaRequest.payload.appliance.additionalApplianceDetails.path);

            // check for a bad request
            if (alexaRequest == null)
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "Null request header.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            try
            {
                response.header.name = alexaRequest.header.name.Replace("Request", "Confirmation");
            }
            catch (Exception)
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "Bad request header.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }


            // check access privleges
            if (!CheckAccessToken(alexaRequest.payload.accessToken).GetAwaiter().GetResult())
            {
                response.payload.exception = GetExceptionPayload("INVALID_ACCESS_TOKEN", "Access denied.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            // check request types
            ControlRequestType requestType = ControlRequestType.Unknown;
            DeviceType deviceType = DeviceType.Unknown;

            string command = alexaRequest.header.name.Trim().ToUpper();
            switch (command)
            {
                case "TURNOFFREQUEST":
                    requestType = ControlRequestType.TurnOffRequest;
                    deviceType = DeviceType.OnOff;
                    break;
                case "TURNONREQUEST":
                    requestType = ControlRequestType.TurnOnRequest;
                    deviceType = DeviceType.OnOff;
                    break;
                case "SETTARGETTEMPERATUREREQUEST":
                    requestType = ControlRequestType.SetTargetTemperature;
                    deviceType = DeviceType.Thermostat;
                    break;
                case "INCREMENTTARGETTEMPERATUREREQUEST":
                    requestType = ControlRequestType.IncrementTargetTemperature;
                    deviceType = DeviceType.Thermostat;
                    break;
                case "DECREMENTTARGETTEMPERATUREREQUEST":
                    requestType = ControlRequestType.DecrementTargetTemperature;
                    deviceType = DeviceType.Thermostat;
                    break;
                case "SETPERCENTAGEREQUEST":
                    requestType = ControlRequestType.SetPercentage;
                    deviceType = DeviceType.Dimmer;
                    break;
                case "INCREMENTPERCENTAGEREQUEST":
                    requestType = ControlRequestType.IncrementPercentage;
                    deviceType = DeviceType.Dimmer;
                    break;
                case "DECREMENTPERCENTAGEREQUEST":
                    requestType = ControlRequestType.DecrementPercentage;
                    deviceType = DeviceType.Dimmer;
                    break;
                default:
                    response.payload.exception = GetExceptionPayload("UNSUPPORTED_OPERATION", string.Format("{0} - Unknown Control Request.", alexaRequest.header.name));
                    ServiceInstance.DisconnectServer(client);
                    return response;
            }


            // get the object
            IPremiseObject applianceToControl = null;
            try
            { 
                Guid premiseId = new Guid(alexaRequest.payload.appliance.applianceId);
                applianceToControl = this.ServiceInstance.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
            }
            catch
            {
                response.payload.exception = GetExceptionPayload("INTERNAL_ERROR", "Cannot Convert Appliance Id.");
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            // report failure
            if (applianceToControl == null)
            {
                response.payload.exception = GetExceptionPayload("NO_SUCH_TARGET", string.Format("Cannot find device {0} ({1})", alexaRequest.payload.appliance.additionalApplianceDetails.path, alexaRequest.payload.appliance.applianceId));
                ServiceInstance.DisconnectServer(client);
                return response;
            }

            if (deviceType == DeviceType.OnOff)
            {
                switch (requestType)
                {
                    case ControlRequestType.TurnOnRequest:
                        applianceToControl.SetValue("PowerState", "True").GetAwaiter().GetResult();
                        break;
                    case ControlRequestType.TurnOffRequest:
                        applianceToControl.SetValue("PowerState", "False").GetAwaiter().GetResult();
                        break;
                    default:
                        break;
                }
            }
            else if (deviceType == DeviceType.Dimmer)
            {
                double currentValue = 0.0;
                double adjustValue = 0.0;
                double valueToSend = 0.0;

                switch (requestType)
                {
                    case ControlRequestType.SetPercentage:
                        // obtain the adjustValue
                        adjustValue = Math.Round(double.Parse(alexaRequest.payload.percentageState.value), 2).LimitToRange(0.00, 100.00);
                        // convert from percentage and maintain fractional accuracy
                        valueToSend = Math.Round(adjustValue / 100.00, 4);
                        applianceToControl.SetValue("Brightness", valueToSend.ToString()).GetAwaiter().GetResult();
                        break;
                    case ControlRequestType.IncrementPercentage:
                        // obtain the adjustValue
                        adjustValue = Math.Round(double.Parse(alexaRequest.payload.deltaPercentage.value) / 100.00, 2).LimitToRange(0.00, 100.00);
                        currentValue = Math.Round(applianceToControl.GetValue<Double>("Brightness").GetAwaiter().GetResult(), 2);
                        // maintain fractional accuracy
                        valueToSend = Math.Round(currentValue + adjustValue, 2).LimitToRange(0.00, 1.00);
                        applianceToControl.SetValue("Brightness", valueToSend.ToString()).GetAwaiter().GetResult();
                        break;
                    case ControlRequestType.DecrementPercentage:
                        // obtain the adjustValue
                        adjustValue = Math.Round(double.Parse(alexaRequest.payload.deltaPercentage.value) / 100.00, 2).LimitToRange(0.00, 100.00);
                        currentValue = Math.Round(applianceToControl.GetValue<Double>("Brightness").GetAwaiter().GetResult(), 2);
                        // maintain fractional accuracy
                        valueToSend = Math.Round(currentValue - adjustValue, 2).LimitToRange(0.00, 1.00);
                        applianceToControl.SetValue("Brightness", valueToSend.ToString()).GetAwaiter().GetResult();
                        break;
                    default:
                        break;
                }
            }
            else if (deviceType == DeviceType.Thermostat)
            {
                int previousTemperatureMode;
                int temperatureMode;
                Temperature previousTargetTemperature = null;
                Temperature targetTemperature = null;
                double deltaTemperatureC = 0.0; // in C

                // obtain previous state (sys stores temperatures as K)
                previousTemperatureMode = applianceToControl.GetValue<int>("TemperatureMode").GetAwaiter().GetResult();
                previousTargetTemperature = new Temperature(applianceToControl.GetValue<double>("CurrentSetPoint").GetAwaiter().GetResult());

                switch (requestType)
                {
                    case ControlRequestType.SetTargetTemperature:
                        // get target temperature in C
                        targetTemperature = new Temperature();
                        targetTemperature.Celcius = double.Parse(alexaRequest.payload.targetTemperature.value);
                        break;
                    case ControlRequestType.IncrementTargetTemperature:
                        // get delta temp in C
                        deltaTemperatureC = double.Parse(alexaRequest.payload.deltaTemperature.value);
                        // increment the targetTemp
                        targetTemperature = new Temperature();
                        targetTemperature.Celcius = previousTargetTemperature.Celcius + deltaTemperatureC;
                        break;

                    case ControlRequestType.DecrementTargetTemperature:
                        // get delta temp in C
                        deltaTemperatureC = double.Parse(alexaRequest.payload.deltaTemperature.value);
                        // decrement the targetTemp
                        targetTemperature = new Temperature();
                        targetTemperature.Celcius = previousTargetTemperature.Celcius - deltaTemperatureC;
                        break;

                    default:
                        targetTemperature = new Temperature(0.00);
                        previousTemperatureMode = 10; // error
                        break;
                }

                // set new target temperature
                applianceToControl.SetValue("CurrentSetPoint", targetTemperature.Kelvin.ToString()).GetAwaiter().GetResult();
                response.payload.targetTemperature = new ApplianceValue();
                response.payload.targetTemperature.value = targetTemperature.Celcius.ToString();

                // get new mode 
                temperatureMode = applianceToControl.GetValue<int>("TemperatureMode").GetAwaiter().GetResult();
                // report new mode
                response.payload.temperatureMode = new ApplianceValue();
                response.payload.temperatureMode.value = TemperatureMode.ModeToString(temperatureMode);

                // alloc a previousState object
                response.payload.previousState = new AppliancePreviousState();

                // report previous mode
                response.payload.previousState.mode = new ApplianceValue();
                response.payload.previousState.mode.value = TemperatureMode.ModeToString(previousTemperatureMode);

                // report previous targetTemperature in C
                response.payload.previousState.targetTemperature = new ApplianceValue();
                response.payload.previousState.targetTemperature.value = previousTargetTemperature.Celcius.ToString();
            }
            else
            {
                response.payload.exception = GetExceptionPayload("UNSUPPORTED_OPERATION", string.Format("{0} - Unknown Device Type.", alexaRequest.header.name));
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
            await this.ServiceInstance.HomeObject.SetValue("LastHeardFromAlexa", DateTime.Now.ToString());
            await this.ServiceInstance.HomeObject.SetValue("LastHeardCommand", command);
        }

        private async Task<bool> CheckAccessToken(string token)
        {
            var accessToken = await this.ServiceInstance.HomeObject.GetValue<string>("AccessToken");
            List<string> tokens = new List<string>(accessToken.Split(','));
            return (-1 != tokens.IndexOf(token));
        }

        #endregion
    }
}
