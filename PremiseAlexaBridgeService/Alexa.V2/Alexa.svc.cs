using Alexa.SmartHomeAPI.V2;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    /// <summary>
    /// Version 2
    /// </summary>

    [ServiceContract(Name = "PremiseAlexaService", Namespace = "https://PremiseAlexa.com")]
    public interface IPremiseAlexaService
    {
        [OperationContract]
        DiscoveryResponse Discovery(DiscoveryRequest request);

        [OperationContract]
        ControlResponse Control(ControlRequest request);

        [OperationContract]
        QueryResponse Query(QueryRequest request);

        [OperationContract]
        SystemResponse System(SystemRequest request);
    }

    public partial class PremiseAlexaService : PremiseAlexaBase, IPremiseAlexaService
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

            //IPremiseObject PremiseServer.HomeObject;

            //SYSClient client = new SYSClient();

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
                    response.payload = this.GetHealthCheckResponseV2();
                    break;

                default:
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.UnsupportedOperationError;
                    response.payload.exception = new ExceptionResponsePayload();
                    break;
            }
            return response;
        }

        private SystemResponsePayload GetHealthCheckResponseV2()
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
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        public DiscoveryResponse Discovery(DiscoveryRequest alexaRequest)
        {

            //IPremiseObject PremiseServer.HomeObject, rootObject;
            var response = new DiscoveryResponse();

            #region CheckRequest

            if ((alexaRequest == null) || (alexaRequest.header == null) || (alexaRequest.header.payloadVersion == null) || (alexaRequest.header.payloadVersion == "1"))
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.UnexpectedInformationReceivedError;
                response.payload.exception = new ExceptionResponsePayload()
                {
                    faultingParameter = "alexaRequest"
                };
                return response;
            }


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


            #endregion

            #region InitialzeResponse

            try
            {
                if (response.header.payloadVersion == "3")
                {
                    response.header.name = alexaRequest.header.name + ".Response";
                }
                else
                {
                    response.header.name = alexaRequest.header.name.Replace("Request", "Response");
                }
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

            //SYSClient client = new SYSClient();

            #region ConnectToPremise

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

            #endregion

            try
            {

                #region VerifyAccess

                if (!CheckAccessToken(alexaRequest.payload.accessToken).GetAwaiter().GetResult())
                {
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.InvalidAccessTokenError;
                    return response;
                }

                #endregion

                #region Perform Discovery

                InformLastContact(alexaRequest.header.name).GetAwaiter().GetResult();

                response.payload.discoveredAppliances = this.GetAppliances().GetAwaiter().GetResult();
                response.payload.discoveredAppliances.Sort(Appliance.CompareByFriendlyName);

                #endregion
            }
            catch (Exception ex)
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.DriverpublicError;
                response.payload.exception = new ExceptionResponsePayload
                {
                    errorInfo = new ErrorInfo
                    {
                        description = ex.Message.ToString()
                    }
                };
            }

            return response;
        }

        private async Task<List<Appliance>> GetAppliances()
        {
            List<Appliance> appliances = new List<Appliance>();

            var returnClause = new string[] { "Name", "DisplayName", "FriendlyName", "FriendlyDescription", "IsReachable", "IsDiscoverable", "PowerState", "Brightness", "Temperature", "TemperatureMode", "Host", "Port", "Path", "Hue", "OID", "OPATH", "OTYPENAME", "Type", "ApplianceType" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            whereClause.TypeOf = PremiseServer.AlexaApplianceClassPath;

            var sysAppliances = await PremiseServer.HomeObject.Select(returnClause, whereClause);
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
                    friendlyDescription = ((string)sysAppliance.FriendlyDescription).Trim(), // Dan: Premise should be source of truth
                    applianceTypes = new List<string>()

                };

                var premiseObject = await PremiseServer.HomeObject.GetObject(objectId);

                // the FriendlyName is what Alexa tries to match when finding devices, so we need one
                // if no FriendlyName value then try to invent one and set it so we dont have to do this again!
                if (string.IsNullOrEmpty(appliance.friendlyName))
                {
                    generatedNameCount++;

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

                string typeName = sysAppliance.OTYPENAME;
                bool isSceneTrigger = typeName.StartsWith("AlexaVirtual");

                // Deal with empty FriendlyDescription
                if (string.IsNullOrEmpty(appliance.friendlyDescription))
                {
                    generatedDescriptionCount++;
                    // parent should be a container - so get that name
                    var parent = await premiseObject.GetParent();
                    string parentName = (await parent.GetDescription()).Trim();
                    if (parentName.Length == 0)
                    {
                        parentName = (await parent.GetName()).Trim();
                    }
                    // results in something like = "Premise Sconce in the Entry."
                    if (isSceneTrigger)
                        appliance.friendlyDescription = "Scene connected via Premise";
                    else
                        appliance.friendlyDescription = string.Format("Premise {0} in the {1}", sysAppliance.OTYPENAME, parentName).Trim();
                    // set the value in the premise dom
                    await premiseObject.SetValue("FriendlyDescription", appliance.friendlyDescription);
                }

                // determine device types

                AlexaApplianceTypes applianceType = AlexaApplianceTypes.UNKNOWN;
                bool hasDimmer = false;
                bool hasColor = false;
                if (!isSceneTrigger)
                {
                    bool isLight = await premiseObject.IsOfType("{0B1DA7E1-1731-49AC-9814-47470E78EFAB}");  // lighting
                    if (isLight)
                    {
                        applianceType = AlexaApplianceTypes.LIGHT;
                        hasDimmer = await premiseObject.IsOfType("{DEB24C93-9143-4030-86FF-29C7626BC9E3}");  // dimmer
                        hasColor = (sysAppliance.Hue != null);  // note change this to isOfType   // color
                    }
                    else
                    {
                        if (await premiseObject.IsOfType("{35ED9728-21C0-4868-BEFE-BCBA38D4C4B3}"))  // thermostat
                        {
                            applianceType = AlexaApplianceTypes.THERMOSTAT;
                        }
                        else if (await premiseObject.IsOfType("{68BF174A-8984-4214-AC09-2975A4CEBEAA}")) // camera
                        {
                            applianceType = AlexaApplianceTypes.CAMERA;

                        } // else if (await premiseObject.IsOfType("{77319741-F5A7-4CA5-A2EA-4F377D394301}"))
                        //{
                        //    applianceType = AlexaApplianceTypes.FAN;
                        //}
                    }
                }
                else
                {
                    applianceType = AlexaApplianceTypes.SCENE_TRIGGER;
                }

                // If ApplianceType is provided on the premise object, ensure it's the first entry in applianceTypes[]
                // This way you can over-ride the logic below in Premise
                if (!string.IsNullOrEmpty((string)sysAppliance.ApplianceType))
                    appliance.applianceTypes.Add(((string)sysAppliance.ApplianceType).Trim());


                appliance.additionalApplianceDetails = new AdditionalApplianceDetails()
                {
                    path = sysAppliance.OPATH
                };

                switch (applianceType)
                {
                    //case AlexaApplianceTypes.FAN:
                    //    {
                    //        appliance.actions.Add("turnOn");
                    //        appliance.actions.Add("turnOff");
                    //        appliance.actions.Add("setPercentage");
                    //        appliance.actions.Add("incrementPercentage");
                    //        appliance.actions.Add("decrementPercentage");
                    //    }
                    //    break;

                    case AlexaApplianceTypes.LIGHT:
                        {
                            appliance.actions.Add("turnOn");
                            appliance.actions.Add("turnOff");
                            if (hasDimmer)
                            {
                                appliance.actions.Add("setPercentage");
                                appliance.actions.Add("incrementPercentage");
                                appliance.actions.Add("decrementPercentage");
                            }
                            if (hasColor)
                            {
                                appliance.actions.Add("setColor");
                                appliance.actions.Add("setColorTemperature");
                                appliance.actions.Add("incrementColorTemperature");
                                appliance.actions.Add("decrementColorTemperature");
                            }
                        }
                        break;
                    case AlexaApplianceTypes.SCENE_TRIGGER:
                        {
                            appliance.actions.Add("turnOn");
                            appliance.actions.Add("turnOff");
                        }
                        break;

                    case AlexaApplianceTypes.THERMOSTAT:
                        {
                            appliance.actions.Add("setTargetTemperature");
                            appliance.actions.Add("incrementTargetTemperature");
                            appliance.actions.Add("decrementTargetTemperature");
                            appliance.actions.Add("getTemperatureReading");
                            appliance.actions.Add("getTargetTemperature");
                        }
                        break;

                    case AlexaApplianceTypes.CAMERA:
                        {
                            appliance.actions.Add("retrieveCameraStreamUri");
                        }
                        break;

                    default: // UNKNOWN
                        {
                            if (await premiseObject.IsOfType("{9C3E5340-EAB7-402D-979A-93B5135264AA}")) // powerstate 
                            {
                                appliance.actions.Add("turnOn");
                                appliance.actions.Add("turnOff");
                            }
                        }
                        break;
                }

                if (appliance.applianceTypes.Count == 0) // catches the case of a type override by Premise.
                {
                    appliance.applianceTypes.Add(applianceType.ToString());
                }

                appliances.Add(appliance);
                if (++count >= PremiseServer.AlexaDeviceLimit)
                    break;
            }

            await PremiseServer.HomeObject.SetValue("LastRefreshed", DateTime.Now.ToString());
            await PremiseServer.HomeObject.SetValue("HealthDescription", string.Format("Reported={0},Names Generated={1}, Descriptions Generated={2}", count, generatedNameCount, generatedDescriptionCount));
            await PremiseServer.HomeObject.SetValue("Health", "True");
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

            //IPremiseObject PremiseServer.HomeObject, rootObject;
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

            //SYSClient client = new SYSClient();

            #region ConnectToPremise

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

            #endregion

            try
            {
                if (!CheckAccessToken(alexaRequest.payload.accessToken).GetAwaiter().GetResult())
                {
                    response.header.@namespace = Faults.Namespace;
                    response.header.name = Faults.InvalidAccessTokenError;
                    response.payload.exception = new ExceptionResponsePayload();
                    return response;
                }

                InformLastContact("ControlRequest:" + alexaRequest.payload.appliance.additionalApplianceDetails.path).GetAwaiter().GetResult();

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
                        requestType = ControlRequestType.SetColorRequest;
                        deviceType = DeviceType.ColorLight;
                        break;
                    case "SETCOLORTEMPERATUREREQUEST":
                        requestType = ControlRequestType.SetColorTemperatureRequest;
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
                        return response;
                }

                // get the object
                IPremiseObject applianceToControl = null;
                try
                {
                    Guid premiseId = new Guid(alexaRequest.payload.appliance.applianceId);
                    applianceToControl = PremiseServer.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
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
                    const double adjustValue = 100.0;
                    double currentValue = 0.0;
                    double valueToSend = 0.0;
                    response.payload.achievedState = new AchievedState();

                    switch (requestType)
                    {
                        case ControlRequestType.SetColorRequest:
                            // obtain the adjustValue
                            double hue = Math.Round(alexaRequest.payload.color.hue.LimitToRange(0, 360), 1);
                            double saturation = Math.Round(alexaRequest.payload.color.saturation, 4);
                            double brightness = Math.Round(alexaRequest.payload.color.brightness, 4);
                            // set the values
                            applianceToControl.SetValue("Hue", hue.ToString()).GetAwaiter().GetResult();
                            applianceToControl.SetValue("Saturation", saturation.ToString()).GetAwaiter().GetResult();
                            applianceToControl.SetValue("Brightness", brightness.ToString()).GetAwaiter().GetResult();
                            // read them back for achieved state
                            response.payload.achievedState.color = new ApplianceColorValue
                            {
                                hue = Math.Round(applianceToControl.GetValue<Double>("Hue").GetAwaiter().GetResult(), 1),
                                saturation = Math.Round(applianceToControl.GetValue<Double>("Saturation").GetAwaiter().GetResult(), 4),
                                brightness = Math.Round(applianceToControl.GetValue<Double>("Brightness").GetAwaiter().GetResult(), 4)
                            };
                            break;

                        case ControlRequestType.SetColorTemperatureRequest:
                            valueToSend = alexaRequest.payload.colorTemperature.value.LimitToRange(1000, 10000);
                            // set the value
                            applianceToControl.SetValue("Temperature", Math.Round(valueToSend, 0).ToString()).GetAwaiter().GetResult();
                            // read it back
                            response.payload.achievedState.colorTemperature = new ApplianceColorTemperatureValue
                            {
                                value = applianceToControl.GetValue<int>("Temperature").GetAwaiter().GetResult()
                            };
                            break;

                        case ControlRequestType.IncrementColorTemperature:
                            currentValue = applianceToControl.GetValue<int>("Temperature").GetAwaiter().GetResult();
                            valueToSend = Math.Round(currentValue + adjustValue, 0).LimitToRange(1000, 10000);
                            // set the value
                            applianceToControl.SetValue("Temperature", valueToSend.ToString()).GetAwaiter().GetResult();
                            // read it back
                            response.payload.achievedState.colorTemperature = new ApplianceColorTemperatureValue
                            {
                                value = applianceToControl.GetValue<int>("Temperature").GetAwaiter().GetResult()
                            };
                            break;

                        case ControlRequestType.DecrementColorTemperature:
                            currentValue = Math.Round(applianceToControl.GetValue<Double>("Temperature").GetAwaiter().GetResult(), 2);
                            valueToSend = Math.Round(currentValue - adjustValue, 0).LimitToRange(1000, 10000);
                            // set the value
                            applianceToControl.SetValue("Temperature", valueToSend.ToString()).GetAwaiter().GetResult();
                            // read it back
                            response.payload.achievedState.colorTemperature = new ApplianceColorTemperatureValue
                            {
                                value = applianceToControl.GetValue<int>("Temperature").GetAwaiter().GetResult()
                            };
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
                            targetTemperature = new Temperature
                            {
                                Celcius = double.Parse(alexaRequest.payload.targetTemperature.value)
                            };
                            break;
                        case ControlRequestType.IncrementTargetTemperature:
                            // get delta temp in C
                            deltaTemperatureC = double.Parse(alexaRequest.payload.deltaTemperature.value);
                            // increment the targetTemp
                            targetTemperature = new Temperature
                            {
                                Celcius = previousTargetTemperature.Celcius + deltaTemperatureC
                            };
                            break;

                        case ControlRequestType.DecrementTargetTemperature:
                            // get delta temp in C
                            deltaTemperatureC = double.Parse(alexaRequest.payload.deltaTemperature.value);
                            // decrement the targetTemp
                            targetTemperature = new Temperature
                            {
                                Celcius = previousTargetTemperature.Celcius - deltaTemperatureC
                            };
                            break;

                        default:
                            targetTemperature = new Temperature(0.00);
                            previousTemperatureMode = 10; // error
                            break;
                    }

                    // set new target temperature
                    applianceToControl.SetValue("CurrentSetPoint", targetTemperature.Kelvin.ToString()).GetAwaiter().GetResult();
                    response.payload.targetTemperature = new ApplianceValue
                    {
                        value = targetTemperature.Celcius.ToString()
                    };

                    // get new mode 
                    temperatureMode = applianceToControl.GetValue<int>("TemperatureMode").GetAwaiter().GetResult();
                    // report new mode
                    response.payload.temperatureMode = new ApplianceValue
                    {
                        value = TemperatureMode.ModeToString(temperatureMode)
                    };

                    // alloc a previousState object
                    response.payload.previousState = new AppliancePreviousState
                    {

                        // report previous mode
                        mode = new ApplianceValue
                        {
                            value = TemperatureMode.ModeToString(previousTemperatureMode)
                        },

                        // report previous targetTemperature in C
                        targetTemperature = new ApplianceValue
                        {
                            value = previousTargetTemperature.Celcius.ToString()
                        }
                    };
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
                response.header.name = Faults.DriverpublicError;
                response.payload.exception = new ExceptionResponsePayload();
            }

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
            //IPremiseObject PremiseServer.HomeObject, rootObject;

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

            //SYSClient client = new SYSClient();

            #region ConnectToPremise

            if (PremiseServer.HomeObject == null)
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
                if (!CheckAccessToken(alexaRequest.payload.accessToken).GetAwaiter().GetResult())
                {
                    response.header.@namespace = Faults.QueryNamespace;
                    response.header.name = Faults.InvalidAccessTokenError;
                    response.payload.exception = new ExceptionResponsePayload();
                    return response;
                }

                string command = alexaRequest.header.name.Trim().ToUpper();
                switch (command)
                {
                    case "RETRIEVECAMERASTREAMURIREQUEST":
                        ProcessDeviceStateQueryRequest(QueryRequestType.RetrieveCameraStreamUri, alexaRequest, response);
                        break;
                    case "GETTARGETTEMPERATUREREQUEST":
                        ProcessDeviceStateQueryRequest(QueryRequestType.GetTargetTemperature, alexaRequest, response);
                        break;
                    case "GETTEMPERATUREREADINGREQUEST":
                        ProcessDeviceStateQueryRequest(QueryRequestType.GetTemperatureReading, alexaRequest, response);
                        break;
                    default:
                        response.header.@namespace = Faults.QueryNamespace;
                        response.header.name = Faults.UnsupportedOperationError;
                        response.payload.exception = new ExceptionResponsePayload
                        {
                            errorInfo = new ErrorInfo
                            {
                                description = "Unsupported Query Request Type"
                            }
                        };
                        break;
                }
            }
            catch (Exception e)
            {
                response.header.@namespace = Faults.QueryNamespace;
                response.header.name = Faults.DriverpublicError;
                response.payload.exception = new ExceptionResponsePayload
                {
                    errorInfo = new ErrorInfo
                    {
                        description = e.Message
                    }
                };
            }

            return response;
            #endregion
        }

        #region Process Device State Query

        private void ProcessDeviceStateQueryRequest(QueryRequestType requestType, QueryRequest alexaRequest, QueryResponse response)
        {

            IPremiseObject applianceToQuery;

            InformLastContact("QueryRequest:" + alexaRequest.payload.appliance.additionalApplianceDetails.path).GetAwaiter().GetResult();

            try
            {
                // Find the object
                Guid premiseId = new Guid(alexaRequest.payload.appliance.applianceId);
                applianceToQuery = PremiseServer.RootObject.GetObject(premiseId.ToString("B")).GetAwaiter().GetResult();
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
                    case QueryRequestType.RetrieveCameraStreamUri:
                        {
                            response.payload.uri = new ApplianceValue();
                            string host = applianceToQuery.GetValue<string>("Host").GetAwaiter().GetResult();
                            string port = applianceToQuery.GetValue<string>("Port").GetAwaiter().GetResult();
                            string path = applianceToQuery.GetValue<string>("Path").GetAwaiter().GetResult();
                            response.payload.uri.value = string.Format(@"rtsp://{0}:{1}{2}", host, port, path);
                        }
                        break;
                    case QueryRequestType.GetTargetTemperature:
                        Temperature coolingSetPoint = new Temperature(applianceToQuery.GetValue<double>("CoolingSetPoint").GetAwaiter().GetResult());
                        Temperature heatingSetPoint = new Temperature(applianceToQuery.GetValue<double>("HeatingSetPoint").GetAwaiter().GetResult());
                        int temperatureMode = applianceToQuery.GetValue<int>("TemperatureMode").GetAwaiter().GetResult();
                        response.payload.temperatureMode = new ApplianceTemperatureMode
                        {
                            value = TemperatureMode.ModeToString(temperatureMode)
                        };
                        response.payload.heatingTargetTemperature = new ApplianceTemperatureReading
                        {
                            value = double.Parse(string.Format("{0:N2}", heatingSetPoint.Celcius)),
                            scale = "CELSIUS"
                        };
                        response.payload.coolingTargetTemperature = new ApplianceTemperatureReading
                        {
                            value = double.Parse(string.Format("{0:N2}", coolingSetPoint.Celcius)),
                            scale = "CELSIUS"
                        };
                        //response.payload.applianceResponseTimestamp = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ");// XmlConvert.ToString(DateTime.UtcNow.ToUniversalTime(), XmlDateTimeSerializationMode.Utc);
                        break;
                    case QueryRequestType.GetTemperatureReading:
                        Temperature temperature = new Temperature(applianceToQuery.GetValue<double>("Temperature").GetAwaiter().GetResult());
                        response.payload.temperatureReading = new ApplianceTemperatureReading
                        {
                            value = double.Parse(string.Format("{0:N2}", temperature.Celcius)),
                            scale = "CELSIUS"
                        };
                        //response.payload.applianceResponseTimestamp = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffZ"); //XmlConvert.ToString(DateTime.UtcNow.ToUniversalTime(), XmlDateTimeSerializationMode.Utc);
                        break;
                    default:
                        response.header.@namespace = Faults.QueryNamespace;
                        response.header.name = Faults.UnsupportedOperationError;
                        response.payload.exception = new ExceptionResponsePayload
                        {
                            errorInfo = new ErrorInfo
                            {
                                description = "Unsupported Query Request Type"
                            }
                        };
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

        #endregion
    }
}
