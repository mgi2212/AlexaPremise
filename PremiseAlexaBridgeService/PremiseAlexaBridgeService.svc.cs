using Alexa.SmartHome;
using System;
using System.Collections.Generic;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using System.Xml;
using SYSWebSockClient;

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

        public void Preload(string[] parameters)
        {
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
                    response.payload = this.GetHealthCheckResponse(homeObject);
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

        private SystemResponsePayload GetHealthCheckResponse(IPremiseObject homeObject)
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
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        public DiscoveryResponse Discovery(DiscoveryRequest alexaRequest)
        {

            IPremiseObject homeObject, rootObject;
            var response = new DiscoveryResponse();

            #region CheckRequest

            if ((alexaRequest == null) || (alexaRequest.header == null) || (alexaRequest.header.payloadVersion != "2"))
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.UnexpectedInformationReceivedError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    faultingParameter = "alexaRequest"
                };
                return response;
            }

            #endregion

            #region InitialzeResponse

            try
            {
                response.header.name = alexaRequest.header.name.Replace("Request", "Response");
                response.header.messageId = alexaRequest.header.messageId;
                response.header.@namespace = alexaRequest.header.@namespace;
            }
            catch (Exception)
            {
                response.header.@namespace = Faults.QueryNamespace;
                response.header.name = Faults.UnexpectedInformationReceivedError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    faultingParameter = "alexaRequest.header.name"
                };
                return response;
            }

            #endregion

            if (alexaRequest.header.name != "DiscoverAppliancesRequest")
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.UnsupportedOperationError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    faultingParameter = "alexaRequest.header.name"
                };
                return response;
            }

            SYSClient client = new SYSClient();

            #region ConnectToPremise

            try
            {
                homeObject = ServiceInstance.ConnectToServer(client);
                rootObject = homeObject.GetRoot().GetAwaiter().GetResult();
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

            #endregion

            try
            {

                #region VerifyAccess

                if (!CheckAccessToken(homeObject, alexaRequest.payload.accessToken).GetAwaiter().GetResult())
                {
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.InvalidAccessTokenError;
                    ServiceInstance.DisconnectServer(client);
                    return response;
                }

                #endregion

                #region Perform Discovery

                InformLastContact(homeObject, alexaRequest.header.name).GetAwaiter().GetResult();

                response.payload.discoveredAppliances = this.GetAppliances(homeObject).GetAwaiter().GetResult();
                response.payload.discoveredAppliances.Sort(Appliance.CompareByFriendlyName);

                #endregion
            }
            catch (Exception)
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.DriverInternalError;
                response.payload.exception = new ExceptionResponsePayload();
            }

            ServiceInstance.DisconnectServer(client);
            return response;
        }

        private async Task<List<Appliance>> GetAppliances(IPremiseObject homeObject)
        {
            List<Appliance> appliances = new List<Appliance>();

            var returnClause = new string[] { "Name", "DisplayName", "FriendlyName", "FriendlyDescription", "IsReachable", "IsDiscoverable", "PowerState", "Brightness", "Temperature", "TemperatureMode", "Hue", "OID", "OPATH", "OTYPENAME", "Type" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            whereClause.TypeOf = this.ServiceInstance.AlexaApplianceClassPath;

            var sysAppliances = await homeObject.Select(returnClause, whereClause);
            int count = 0;
            int generatedNameCount = 0;
            int generatedDescriptionCount = 0;

            foreach (var sysAppliance in sysAppliances)
            {
                if (sysAppliance.IsDiscoverable == false)
                    continue;

                var objectId = (string)sysAppliance.OID;
                var appliance = new Appliance()
                {
                    actions = new List<string>(),
                    applianceId = Guid.Parse(objectId).ToString("D"),
                    manufacturerName = "Premise",
                    version = "2.1",
                    isReachable = sysAppliance.IsReachable,
                    modelName = sysAppliance.OTYPENAME,
                    friendlyName = ((string)sysAppliance.FriendlyName).Trim(),
                    friendlyDescription = "" // Generate a new description each time ((string)sysAppliance.FriendlyDescription).Trim()
                };

                // the FriendlyName is what Alexa tries to match when finding devices, so we need one
                // if no FriendlyName value then try to invent one and set it so we dont have to do this again!
                if (string.IsNullOrEmpty(appliance.friendlyName))
                {
                    generatedNameCount++;
                    var premiseObject = await homeObject.GetObject(objectId);

                    // parent should be a container - so get that 
                    var parent = await premiseObject.GetParent();
                    
                    // use displayName and if not that, the the object name. (note: this should help handle cases for objects with names lile like LivingRoom)
                    string parentName = (await parent.GetDisplayName()).Trim();
                    if (string.IsNullOrEmpty(parentName))
                    {
                        parentName = (await parent.GetName()).Trim();
                    }

                    if (parentName.IndexOf("(Occupied)") != 01)
                    {
                        parentName = parentName.Replace("(Occupied)", "").Trim();
                    }

                    string deviceName = sysAppliance.FriendlyName;
                    deviceName = deviceName.Trim();
                    if (string.IsNullOrEmpty(deviceName))
                    {
                        deviceName = sysAppliance.Name;
                        deviceName = deviceName.Trim();
                    }

                    // preceed the parent container name with the appliance name
                    appliance.friendlyName = string.Format("{0} {1}", parentName, deviceName).Trim();

                    // set the value in the dom
                    await premiseObject.SetValue("FriendlyName", appliance.friendlyName);
                }

                // construct the additional details
                bool hasPowerState = (sysAppliance.PowerState != null);
                bool hasTemperature = (sysAppliance.Temperature != null); // note a color light will have a temperature property
                bool hasDimmer = (sysAppliance.Brightness != null);
                bool hasColor = (sysAppliance.Hue != null);
                if (hasColor)
                {
                    hasTemperature = false;
                }

                // Deal with empty FriendlyDescription
                if (string.IsNullOrEmpty(appliance.friendlyDescription))
                {
                    generatedDescriptionCount++;
                    // parent should be a container - so get that name
                    var premiseObject = await homeObject.GetObject(objectId);
                    var parent = await premiseObject.GetParent();
                    string parentName = (await parent.GetDescription()).Trim();
                    if (parentName.Length == 0)
                    {
                        parentName = (await parent.GetName()).Trim();
                    }
                    // results in something like = "A Sconce in the Entry."
                    appliance.friendlyDescription = string.Format("Premise {0} in the {1}", sysAppliance.OTYPENAME, parentName).Trim();
                    // set the value in the premise dom
                    await premiseObject.SetValue("FriendlyDescription", appliance.friendlyDescription);
                }


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
                if (hasColor)
                {
                    appliance.actions.Add("setColor");
                    appliance.actions.Add("setColorTemperature");
                    appliance.actions.Add("incrementColorTemperature");
                    appliance.actions.Add("decrementColorTemperature");
                    appliance.additionalApplianceDetails.purpose = "ColorLight";
                }
                if (hasTemperature)
                {
                    appliance.actions.Add("setTargetTemperature");
                    appliance.actions.Add("incrementTargetTemperature");
                    appliance.actions.Add("decrementTargetTemperature");
                    appliance.actions.Add("getTemperatureReading");
                    appliance.actions.Add("getTargetTemperature");
                    appliance.additionalApplianceDetails.purpose = "Thermostat";
                }

                appliances.Add(appliance);
                if (++count >= this.ServiceInstance.AlexaDeviceLimit)
                    break;
            }

            await homeObject.SetValue("LastRefreshed", DateTime.Now.ToString());
            await homeObject.SetValue("HealthDescription", string.Format("Reported={0},Names Generated={1}, Descriptions Generated={2}", count, generatedNameCount, generatedDescriptionCount));
            await homeObject.SetValue("Health", "True");
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

            IPremiseObject homeObject, rootObject;
            var response = new ControlResponse();

            #region CheckRequest

            if ((alexaRequest == null) || (alexaRequest.header == null) || (alexaRequest.header.payloadVersion != "2"))
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.UnexpectedInformationReceivedError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    faultingParameter = "alexaRequest"
                };
                return response;
            }

            #endregion

            #region BuildResponse 

            try
            {
                response.header.messageId = alexaRequest.header.messageId;
                response.header.@namespace = alexaRequest.header.@namespace;
                response.header.name = alexaRequest.header.name.Replace("Request", "Confirmation");
            }
            catch (Exception)
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.UnexpectedInformationReceivedError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    faultingParameter = "alexaRequest.header.name"
                };
                return response;

            }

            #endregion

            SYSClient client = new SYSClient();

            #region ConnectToPremise

            try
            {
                homeObject = ServiceInstance.ConnectToServer(client);
                rootObject = homeObject.GetRoot().GetAwaiter().GetResult();
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

            #endregion

            try
            {
                if (!CheckAccessToken(homeObject, alexaRequest.payload.accessToken).GetAwaiter().GetResult())
                {
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.InvalidAccessTokenError;
                    response.payload.exception = new ExceptionResponsePayload();
                    ServiceInstance.DisconnectServer(client);
                    return response;
                }

                InformLastContact(homeObject, "ControlRequest:" + alexaRequest.payload.appliance.additionalApplianceDetails.path).GetAwaiter().GetResult();

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
                    case "SETCOLORREQUEST":
                        requestType = ControlRequestType.SetColor;
                        deviceType = DeviceType.ColorLight;
                        break;
                    case "SETCOLORTEMPERATUREREQUEST":
                        requestType = ControlRequestType.SetColorTemperature;
                        deviceType = DeviceType.ColorLight;
                        break;
                    case "INCREMENTCOLORTEMPERATUREREQUEST":
                        requestType = ControlRequestType.IncrementColorTemperature;
                        deviceType = DeviceType.ColorLight;
                        break;
                    case "DECREMENTCOLORTEMPERATUREREQUEST":
                        requestType = ControlRequestType.DecrementColorTemperature;
                        deviceType = DeviceType.ColorLight;
                        break;

                    default:
                        response.header.@namespace = Faults.Namespace;
                        response.header.name = Faults.UnsupportedOperationError;
                        response.payload.exception = new ExceptionResponsePayload();
                        ServiceInstance.DisconnectServer(client);
                        return response;
                }

                // get the object
                IPremiseObject applianceToControl = null;
                try
                {
                    Guid premiseId = new Guid(alexaRequest.payload.appliance.applianceId);
                    applianceToControl = rootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                    if (applianceToControl == null)
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.NoSuchTargetError;
                    response.payload.exception = new ExceptionResponsePayload();
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
                else if (deviceType == DeviceType.ColorLight)
                {
                    double currentValue = 0.0;
                    double adjustValue = 0.0;
                    double valueToSend = 0.0;

                    switch (requestType)
                    {
                        case ControlRequestType.SetColor:
                            // obtain the adjustValue
                            double hue = Math.Round(double.Parse(alexaRequest.payload.color.hue.value), 1).LimitToRange(0.0, 360.0);
                            double saturation = Math.Round(double.Parse(alexaRequest.payload.color.saturation.value) / 100.00, 2).LimitToRange(0.00, 100.00);
                            double brightness = Math.Round(double.Parse(alexaRequest.payload.color.brightness.value) / 100.00, 2).LimitToRange(0.00, 100.00);
                            // convert from percentage and maintain fractional accuracy
                            applianceToControl.SetValue("Hue", hue.ToString()).GetAwaiter().GetResult();
                            applianceToControl.SetValue("Saturation", saturation.ToString()).GetAwaiter().GetResult();
                            applianceToControl.SetValue("Brightness", brightness.ToString()).GetAwaiter().GetResult();
                            break;
                        case ControlRequestType.SetColorTemperature:
                            valueToSend = Math.Round(double.Parse(alexaRequest.payload.colorTemperature.value.value), 1).LimitToRange(1000.0, 10000.0);
                            applianceToControl.SetValue("Temperature", valueToSend.ToString()).GetAwaiter().GetResult();
                            break;
                        case ControlRequestType.IncrementColorTemperature:
                            currentValue = Math.Round(applianceToControl.GetValue<Double>("Temperature").GetAwaiter().GetResult(), 2);
                            adjustValue = 100.00;
                            // maintain fractional accuracy
                            valueToSend = Math.Round(currentValue + adjustValue, 2).LimitToRange(1000.00, 10000.00);
                            applianceToControl.SetValue("Temperature", valueToSend.ToString()).GetAwaiter().GetResult();
                            break;
                        case ControlRequestType.DecrementColorTemperature:
                            currentValue = Math.Round(applianceToControl.GetValue<Double>("Temperature").GetAwaiter().GetResult(), 2);
                            adjustValue = 100.00;
                            // maintain fractional accuracy
                            valueToSend = Math.Round(currentValue - adjustValue, 2).LimitToRange(1000.00, 10000.00);
                            applianceToControl.SetValue("Temperature", valueToSend.ToString()).GetAwaiter().GetResult();
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
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.UnsupportedOperationError;
                    response.payload.exception = new ExceptionResponsePayload();
                }
            }
            catch
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.DriverInternalError;
                response.payload.exception = new ExceptionResponsePayload();
            }

            ServiceInstance.DisconnectServer(client);
            return response;
        }

        #endregion

        #region Query

        /// <summary>
        /// Query Requests are processed here
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Query/")]
        public QueryResponse Query(QueryRequest alexaRequest)
        {
            var response = new QueryResponse();
            IPremiseObject homeObject, rootObject;

            #region CheckRequest

            if ((alexaRequest == null) || (alexaRequest.header == null) || (alexaRequest.header.payloadVersion != "2"))
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.UnexpectedInformationReceivedError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    faultingParameter = "alexaRequest"
                };
                return response;
            }

            #endregion

            #region Initialize Response
            try
            {
                response.header.messageId = alexaRequest.header.messageId;
                response.header.@namespace = alexaRequest.header.@namespace;
                response.header.name = alexaRequest.header.name.Replace("Request", "Response"); //alexaRequest.header.name + "Response";
            }
            catch (Exception)
            {
                response.header.@namespace = Faults.QueryNamespace;
                response.header.name = Faults.UnexpectedInformationReceivedError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    faultingParameter = "alexaRequest.header.name"
                };
                return response;
            }

            #endregion

            SYSClient client = new SYSClient();

            #region ConnectToPremise

            try
            {
                homeObject = ServiceInstance.ConnectToServer(client);
                rootObject = homeObject.GetRoot().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                response.header.@namespace = Faults.QueryNamespace;
                response.header.name = Faults.DependentServiceUnavailableError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    dependentServiceName = "Premise Server"
                };
                return response;
            }

            #endregion

            #region Dispatch Query

            try
            {
                if (!CheckAccessToken(homeObject, alexaRequest.payload.accessToken).GetAwaiter().GetResult())
                {
                    response.header.@namespace = Faults.QueryNamespace;
                    response.header.name = Faults.InvalidAccessTokenError;
                    response.payload.exception = new ExceptionResponsePayload();
                    ServiceInstance.DisconnectServer(client);
                    return response;
                }

                string command = alexaRequest.header.name.Trim().ToUpper();
                switch (command)
                {
                    //case "GETHOUSESTATUS":
                    case "GETSPACEMODEREQUEST":
                        ProcessGetSpaceModeRequest(homeObject, rootObject, alexaRequest, response);
                        break;
                    case "GETTARGETTEMPERATUREREQUEST":
                        ProcessDeviceStateQueryRequest(QueryRequestType.GetTargetTemperature, homeObject, rootObject, alexaRequest, response);
                        break;
                    case "GETTEMPERATUREREADINGREQUEST":
                        ProcessDeviceStateQueryRequest(QueryRequestType.GetTemperatureReading, homeObject, rootObject, alexaRequest, response);
                        break;
                    default:
                        response.header.@namespace = Faults.QueryNamespace;
                        response.header.name = Faults.UnsupportedOperationError;
                        response.payload.exception = new ExceptionResponsePayload();
                        response.payload.exception.errorInfo = new ErrorInfo();
                        response.payload.exception.errorInfo.description = "Unsupported Query Request Type";
                        break;
                }
            }
            catch (Exception e)
            {
                response.header.@namespace = Faults.QueryNamespace;
                response.header.name = Faults.DriverInternalError;
                response.payload.exception = new ExceptionResponsePayload();
                response.payload.exception.errorInfo = new ErrorInfo();
                response.payload.exception.errorInfo.description = e.Message;
            }

            ServiceInstance.DisconnectServer(client);
            return response;
            #endregion
        }

        #region Process Device State Query

        private void ProcessDeviceStateQueryRequest(QueryRequestType requestType, IPremiseObject homeObject, IPremiseObject rootObject, QueryRequest alexaRequest, QueryResponse response)
        {

            IPremiseObject applianceToQuery;

            InformLastContact(homeObject, "QueryRequest:" + alexaRequest.payload.appliance.additionalApplianceDetails.path).GetAwaiter().GetResult();

            try
            {
                // Find the object
                Guid premiseId = new Guid(alexaRequest.payload.appliance.applianceId);
                applianceToQuery = rootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
                if (applianceToQuery == null)
                {
                    throw new Exception();
                }

                switch (requestType)
                {
                    /*
                    case QueryRequestType.PowerState:
                        string state = applianceToQuery.GetValue("PowerState").GetAwaiter().GetResult();
                        break;
                    case QueryRequestType.DimmerLevel:
                        string state = applianceToQuery.GetValue("Brightness").GetAwaiter().GetResult();
                        break;
                    case QueryRequestType.ColorTemperature:
                        string state = applianceToQuery.GetValue("ColorTemperature").GetAwaiter().GetResult();
                        break;
                    case QueryRequestType.Color:
                        string state = applianceToQuery.GetValue("Hue").GetAwaiter().GetResult();
                        break;
                    */
                    case QueryRequestType.GetTargetTemperature:
                        Temperature coolingSetPoint = new Temperature(applianceToQuery.GetValue<double>("CoolingSetPoint").GetAwaiter().GetResult());
                        Temperature heatingSetPoint = new Temperature(applianceToQuery.GetValue<double>("HeatingSetPoint").GetAwaiter().GetResult());
                        int temperatureMode = applianceToQuery.GetValue<int>("TemperatureMode").GetAwaiter().GetResult();
                        response.payload.temperatureMode = new ApplianceTemperatureMode();
                        response.payload.temperatureMode.value = TemperatureMode.ModeToString(temperatureMode);
                        response.payload.heatingTargetTemperature = new ApplianceTemperatureReading();
                        response.payload.heatingTargetTemperature.value = double.Parse(string.Format("{0:N2}", heatingSetPoint.Celcius));
                        response.payload.heatingTargetTemperature.scale = "CELSIUS";
                        response.payload.coolingTargetTemperature = new ApplianceTemperatureReading();
                        response.payload.coolingTargetTemperature.value = double.Parse(string.Format("{0:N2}", coolingSetPoint.Celcius));
                        response.payload.coolingTargetTemperature.scale = "CELSIUS";
                        //response.payload.applianceResponseTimestamp = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");// XmlConvert.ToString(DateTime.UtcNow.ToUniversalTime(), XmlDateTimeSerializationMode.Utc);
                        break;
                    case QueryRequestType.GetTemperatureReading:
                        Temperature temperature = new Temperature(applianceToQuery.GetValue<double>("Temperature").GetAwaiter().GetResult());
                        response.payload.temperatureReading = new ApplianceTemperatureReading();
                        response.payload.temperatureReading.value = double.Parse(string.Format("{0:N2}", temperature.Celcius));
                        response.payload.temperatureReading.scale = "CELSIUS";
                        //response.payload.applianceResponseTimestamp = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ"); //XmlConvert.ToString(DateTime.UtcNow.ToUniversalTime(), XmlDateTimeSerializationMode.Utc);
                        break;
                    default:
                        response.header.@namespace = Faults.QueryNamespace;
                        response.header.name = Faults.UnsupportedOperationError;
                        response.payload.exception = new ExceptionResponsePayload();
                        response.payload.exception.errorInfo = new ErrorInfo();
                        response.payload.exception.errorInfo.description = "Unsupported Query Request Type";
                        break;
                }
            }
            catch
            {
                response.header.@namespace = Faults.QueryNamespace;
                response.header.name = Faults.NoSuchTargetError;
                response.payload.exception = new ExceptionResponsePayload();
            }
        }

        #endregion

        #region Process Space Mode Query

        private void ProcessGetSpaceModeRequest(IPremiseObject homeObject, IPremiseObject rootObject, QueryRequest alexaRequest, QueryResponse response)
        {
            string toMatch =  alexaRequest.payload.space.name;
            if (string.IsNullOrEmpty(toMatch) == false)
            {
                toMatch = toMatch.Trim();
                var returnClause = new string[] { "Name", "Description", "CleanMode", "Freeze", "DisplayedTemporalMode", "Occupancy", "LastOccupied", "OccupancyCount", "Temperature", "OID", "OPATH", "OTYPENAME", "Type" };
                dynamic whereClause = new System.Dynamic.ExpandoObject();
                whereClause.TypeOf = this.ServiceInstance.AlexaLocationClassPath;
                var sysRooms = homeObject.Select(returnClause, whereClause).GetAwaiter().GetResult();

                foreach (var room in sysRooms)
                {
                    string room_name = room.Name;
                    string room_description = room.Description;

                    if ((room_name.Trim().ToLower() == toMatch) || (room_description.Trim().ToLower() == toMatch))
                    {
                        InformLastContact(homeObject, "Get Space Status (success): " + toMatch).GetAwaiter().GetResult();

                        IPremiseObject this_room = rootObject.GetObject(room.OID.ToString("B")).GetAwaiter().GetResult();
                        var devices = this_room.GetChildren().GetAwaiter().GetResult();

                        var count = 0;
                        var onCount = 0;
                        Temperature temperature = null;

                        foreach (var device in devices)
                        {
                            if (device.IsOfType("{3470B9B5-E685-4EB2-ABC0-2F4CCD7F686A}").GetAwaiter().GetResult() == true)
                            {
                                count++;
                                if (device.IsOfType("{65C7B5C2-153D-4711-BAD7-D334FDB12338}").GetAwaiter().GetResult() == true)
                                {
                                    temperature = new Temperature(device.GetValue<double>("Temperature").GetAwaiter().GetResult());
                                }
                                else if (device.IsOfType("{0B1DA7E1-1731-49AC-9814-47470E78EFAB}").GetAwaiter().GetResult() == true)
                                {
                                    onCount += (device.GetValue<bool>("PowerState").GetAwaiter().GetResult() == true) ? 1 : 0;
                                }
                            }
                        }

                        // TODO: Aggregated properties
                        //ICollection<IPremiseObject> i = this_room.GetAggregatedProperties().GetAwaiter().GetResult();
                        //response.payload.applianceRoomStatus.lastOccupied = room.lastOccupied.ToString();

                        response.payload.applianceRoomStatus = new ApplianceRoomStatus();
                        response.payload.applianceRoomStatus.friendlyName = room.Description;
                        response.payload.applianceRoomStatus.occupied = room.Occupancy;
                        response.payload.applianceRoomStatus.freeze = room.Freeze;
                        response.payload.applianceRoomStatus.clean = room.CleanMode;
                        response.payload.applianceRoomStatus.occupancyCount = room.OccupancyCount;
                        response.payload.applianceRoomStatus.mode = RoomMode.ModeToString((int)room.DisplayedTemporalMode);
                        response.payload.applianceRoomStatus.deviceCount = count.ToString();
                        if (temperature != null)
                        {
                            response.payload.applianceRoomStatus.currentTemperature = double.Parse(string.Format("{0:N2}", temperature.Fahrenheit)).ToString();
                        }
                        response.payload.applianceRoomStatus.lightsOnCount = onCount.ToString();
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(toMatch))
            {
                InformLastContact(homeObject, "Get Space Status (space name missing in request)").GetAwaiter().GetResult();
            }
            else
            { 
                InformLastContact(homeObject, "Get Space Status (no such room): " + alexaRequest.payload.space.name.ToLower()).GetAwaiter().GetResult();
            }
            response.header.@namespace = Faults.QueryNamespace;
            response.header.name = Faults.NoSuchTargetError;
            response.payload.exception = new ExceptionResponsePayload();
        }

        #endregion

        #endregion

        #region Utility

        private static async Task InformLastContact(IPremiseObject homeObject, string command)
        {
            await homeObject.SetValue("LastHeardFromAlexa", DateTime.Now.ToString());
            await homeObject.SetValue("LastHeardCommand", command);
        }

        private static async Task<bool> CheckAccessToken(IPremiseObject homeObject, string token)
        {
            var accessToken = await homeObject.GetValue<string>("AccessToken");
            List<string> tokens = new List<string>(accessToken.Split(','));
            return (-1 != tokens.IndexOf(token));
        }

        #endregion
    }
}
