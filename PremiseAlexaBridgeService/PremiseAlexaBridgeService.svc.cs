﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Premise;


namespace PremiseAlexaBridgeService
{
    public class PremiseAlexaService : IPremiseAlexaService
    {
        PremiseServer _server = PremiseServer.Instance;
        /// <summary>
        /// System messages
        /// Pretty much of a hack right now since Amazon doesn't utilize yet
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/System/")]
        public HealthCheckResponse Health(HealthCheckRequest alexaRequest)
        {
            var response = new HealthCheckResponse();
            var err = new ExceptionResponsePayload();

            _server.Refresh(); // refreshes the cache of appliances and checks status

            response.header = new Header();
            response.header.name = "HealthCheckResponse";
            response.header.@namespace = "System";
            response.payload = new HealthCheckResponsePayload();
            response.payload.isHealthy = _server.Status.Health;
            response.payload.description = _server.Status.HealthDescription;
            return response;
        }

        /// <summary>
        /// Discovry - proxy call to premise looking for the AlexaEx class
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Discovery/")]
        public DiscoveryResponse Discover(DiscoveryRequest alexaRequest)
        {
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

            if (alexaRequest.payload.accessToken != _server.Status.AccessToken)
            {
                var err = new ExceptionResponsePayload();
                err.code = "INVALID_ACCESS_TOKEN";
                err.description = "Invalid access token.";
                response.payload.exception = err;
                return response;
            }
            response.payload.discoveredAppliances = _server.Appliances;
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
            if (alexaRequest.payload.accessToken != _server.Status.AccessToken)
            {
                err.code = "INVALID_ACCESS_TOKEN";
                err.description = "Invalid access token.";
                response.payload.exception = err;
                return response;
            }

            // find the appliance to control
            Appliance find = _server.Appliances.Find(x => x.applianceId.ToUpper() == alexaRequest.payload.appliance.applianceId.ToUpper());
            if (find == null)
            {
                err.code = "NO_SUCH_TARGET";
                err.description = string.Format("Cannot find device {0} ({1})", alexaRequest.payload.appliance.additionalApplianceDetails.path, alexaRequest.payload.appliance.applianceId);
                response.payload.exception = err;
                return response;
            }

            // check state and determine if it needs to be changed
            var currentState = new object();
            if (requestType == ControlRequestType.SwitchOnOff)
            {

                currentState = _server.Home.GetObject(find.additionalApplianceDetails.path).GetValue("PowerState");
                bool state = Convert.ToBoolean(currentState);

                switch (alexaRequest.payload.switchControlAction.ToUpper())
                {
                    case "TURN_OFF":
                        if (state == true)
                        {
                            _server.Home.GetObject(find.additionalApplianceDetails.path).SetValue("PowerState", false);
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
                            _server.Home.GetObject(find.additionalApplianceDetails.path).SetValue("PowerState", true);
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

                if ((find.additionalApplianceDetails.dimmable == false) || (alexaRequest.payload.adjustmentUnit != "PERCENTAGE"))
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

                currentState = _server.Home.GetObject(find.additionalApplianceDetails.path).GetValue("Brightness");

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

                var currentValue = Math.Round(double.Parse(currentState.ToString()) * 100.0, 0);
                var adjustValue = Math.Round(double.Parse(alexaRequest.payload.adjustmentValue), 0);
                var resultValue = 0.0;

                if (relativeAdustment)
                {
                    resultValue = Math.Round(currentValue + adjustValue, 0);
                }
                else
                {
                    resultValue = adjustValue;
                }

                if ((resultValue > 100.0) || (resultValue < 0))
                {
                    err.code = "TARGET_SETTING_OUT_OF_RAGE";
                    err.description = string.Format("current={0} adjust={1}", currentValue, adjustValue);
                    response.payload.exception = err;
                    return response;
                }
                resultValue = resultValue / 100.0;
                _server.Home.GetObject(find.additionalApplianceDetails.path).SetValue("Brightness", resultValue);
                response.payload.success = true;

            }

            return response;
        }
    }
}