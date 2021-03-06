﻿using System;
using System.Dynamic;
using System.ServiceModel.Web;
using Alexa.Premise.Custom;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    /// <summary>
    /// This uses the V2 service
    /// </summary>
    public partial class PremiseAlexaService
    {
        #region Custom Skill

        /// <summary>
        /// Custom Skill Requests are processed here
        /// </summary>
        /// <param name="alexaRequest"></param>
        /// <returns></returns>
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/Custom/")]
        public CustomResponse ProcessRequest(CustomRequest alexaRequest)
        {
            var response = new CustomResponse();
            //IPremiseObject PremiseServer.HomeObject, rootObject;

            #region CheckRequest

            if ((alexaRequest == null) || (alexaRequest.header == null) || (alexaRequest.header.payloadVersion != "2"))
            {
                response.header.@namespace = Faults.Namespace;
                response.header.name = Faults.UnexpectedInformationReceivedError;
                response.payload.exception = new ExceptionResponsePayload
                {
                    faultingParameter = "alexaRequest"
                };
                return response;
            }

            #endregion CheckRequest

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
                response.payload.exception = new ExceptionResponsePayload
                {
                    faultingParameter = "alexaRequest.header.name"
                };
                return response;
            }

            #endregion Initialize Response

            //SYSClient client = new SYSClient();

            #region ConnectToPremise

            if (PremiseServer.HomeObject == null)
            {
                response.header.@namespace = Faults.QueryNamespace;
                response.header.name = Faults.DependentServiceUnavailableError;
                response.payload.exception = new ExceptionResponsePayload
                {
                    dependentServiceName = "Premise Server"
                };
                return response;
            }

            #endregion ConnectToPremise

            #region Dispatch Requests

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
                    //case "GETHOUSESTATUS":
                    case "ROOMASSIGNMENTREQUEST":
                        ProcessRoomAssignmentRequest(alexaRequest, response);
                        break;

                    case "ROOMCOMMANDREQUEST":
                        ProcessRoomCommandRequest(alexaRequest, response);
                        break;

                    case "GETSPACEMODEREQUEST":
                        ProcessGetSpaceModeRequest(alexaRequest, response);
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

            #endregion Dispatch Requests
        }

        #region Process Room Assignment Request

        private void ProcessRoomAssignmentRequest(CustomRequest alexaRequest, CustomResponse response)
        {
            string toMatch = alexaRequest.payload.space.name;
            string deviceId = alexaRequest.payload.space.deviceId;
            string userId = alexaRequest.payload.space.userId;
            string spaceName = alexaRequest.payload.device.name.ToUpper();
            string spaceOperation = alexaRequest.payload.device.operation.ToUpper();

            var returnClause = new[] { "Name", "DisplayName", "Description", "OID" };
            dynamic whereClause = new ExpandoObject();
            whereClause.TypeOf = PremiseServer.AlexaLocationClassPath; ;
            var availableLocations = PremiseServer.HomeObject.Select(returnClause, whereClause).GetAwaiter().GetResult();

            response.payload.spacesStatus = new SpacesOperationStatus();

            foreach (var space in availableLocations)
            {
                string displayName = NormalizeDisplayName(space.DisplayName.ToString());

                if ((displayName.ToUpper() != spaceName) && (space.Name.ToString().ToUpper() != spaceName))
                    continue;

                var objectId = (string)space.OID;

                var premiseObject = PremiseServer.HomeObject.GetObject(objectId).GetAwaiter().GetResult();

                returnClause = new[] { "Name", "DisplayName", "Description", "DeviceID", "OID" };
                whereClause = new ExpandoObject();
                whereClause.TypeOf = PremiseServer.AlexaEndpointClassPath;
                var currentAlexaEndpoints = premiseObject.Select(returnClause, whereClause).GetAwaiter().GetResult();
                foreach (var endpoint in currentAlexaEndpoints)
                {
                    if (endpoint.DeviceID == deviceId) // endpoint exists.
                    {
                        if (isAddOperation(spaceOperation))
                        {
                            response.payload.spacesStatus.friendlyResponse = "ALREADY EXISTS";
                            InformLastContact("Endpoint Assignment Request (already exists)").GetAwaiter().GetResult();
                            return;
                        }
                        if (isRemoveOperation(spaceOperation))
                        {
                            premiseObject.DeleteChildObject(endpoint.OID.ToString("B")).GetAwaiter().GetResult();
                            response.payload.spacesStatus.friendlyResponse = spaceOperation + "D";
                            InformLastContact("Endpoint Removal Request").GetAwaiter().GetResult();
                            return;
                        }
                    }
                }
                if (isRemoveOperation(spaceOperation))
                {
                    InformLastContact("Endpoint Removal Request (Failed - Not Found)").GetAwaiter().GetResult();
                    response.payload.spacesStatus.friendlyResponse = "NOT FOUND";
                    return;
                }

                if (isAddOperation(spaceOperation))
                {
                    var createdObject = premiseObject.CreateObject(PremiseServer.AlexaEndpointClassPath, spaceName + " AlexaEndpoint").GetAwaiter().GetResult();
                    createdObject.SetValue("DeviceID", deviceId).GetAwaiter().GetResult();
                    response.payload.spacesStatus.friendlyResponse = "CREATED";
                    InformLastContact("Endpoint Assignment Request").GetAwaiter().GetResult();
                }
            }
        }

        #endregion Process Room Assignment Request



        #region Process Room Command Request

        private void ProcessRoomCommandRequest(CustomRequest alexaRequest, CustomResponse response)
        {
            string toMatch = alexaRequest.payload.space.name;
            string deviceId = alexaRequest.payload.space.deviceId;
            string userId = alexaRequest.payload.space.userId;
            string deviceType = alexaRequest.payload.device.type;
            string deviceOperation = alexaRequest.payload.device.operation;

            var returnClause = new[] { "Name", "DisplayName", "Description", "DeviceID", "OID" };
            dynamic whereClause = new ExpandoObject();
            whereClause.TypeOf = PremiseServer.AlexaEndpointClassPath; ;
            var alexaEndpoints = PremiseServer.HomeObject.Select(returnClause, whereClause).GetAwaiter().GetResult();

            int opCount = 0;
            int assignedSpaces = 0;

            foreach (var endpoint in alexaEndpoints)
            {
                if (endpoint.DeviceID != deviceId)
                    continue;

                assignedSpaces++;

                var objectId = (string)endpoint.OID;

                var premiseObject = PremiseServer.HomeObject.GetObject(objectId).GetAwaiter().GetResult();

                var this_space = premiseObject.GetParent().GetAwaiter().GetResult();

                var devices = this_space.GetChildren().GetAwaiter().GetResult();

                foreach (var device in devices)
                {
                    if (device.IsOfType("{0B1DA7E1-1731-49AC-9814-47470E78EFAB}").GetAwaiter().GetResult())  // lighting
                    {
                        switch (deviceOperation.ToUpper())
                        {
                            case "TURN ON":
                            case "ON":
                                device.SetValue("PowerState", "True").GetAwaiter().GetResult();
                                opCount++;
                                break;

                            case "TURN OFF":
                            case "OFF":
                                device.SetValue("PowerState", "False").GetAwaiter().GetResult();
                                opCount++;
                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            response.payload.spacesStatus = new SpacesOperationStatus
            {
                friendlyResponse = (assignedSpaces == 0) ? "FAILED_NO_ASSIGNED SPACES" : "SUCCESS",
                count = opCount.ToString(),
                assignedSpacesCount = assignedSpaces.ToString()
            };
            InformLastContact("Implicit Control Request").GetAwaiter().GetResult();
        }

        #endregion Process Room Command Request

        #region Process Space Mode Request

        private void ProcessGetSpaceModeRequest(CustomRequest alexaRequest, CustomResponse response)
        {
            string toMatch = alexaRequest.payload.space.name;
            if (string.IsNullOrEmpty(toMatch) == false)
            {
                toMatch = toMatch.Trim();
                var returnClause = new[] { "Name", "DisplayName", "Description", "CurrentScene", "Occupancy", "LastOccupied", "OccupancyCount", "OID", "OPATH", "OTYPENAME", "Type" };
                dynamic whereClause = new ExpandoObject();
                whereClause.TypeOf = PremiseServer.AlexaLocationClassPath;
                var sysRooms = PremiseServer.HomeObject.Select(returnClause, whereClause).GetAwaiter().GetResult();

                foreach (var room in sysRooms)
                {
                    string room_name = room.Name;
                    string room_description = room.DisplayName;
                    if ((!string.IsNullOrEmpty(room_description)) && (room_description.IndexOf("(Occupied)") != -1))
                    {
                        room_description = room_description.Replace("(Occupied)", "").Trim();
                    }

                    if ((room_name.Trim().ToLower() == toMatch) || (room_description.Trim().ToLower() == toMatch))
                    {
                        InformLastContact("Get Space Status (success): " + toMatch).GetAwaiter().GetResult();

                        IPremiseObject this_room = PremiseServer.RootObject.GetObject(room.OID.ToString("B")).GetAwaiter().GetResult();
                        var devices = this_room.GetChildren().GetAwaiter().GetResult();

                        var count = 0;
                        var onCount = 0;
                        Temperature temperature = null;

                        foreach (var device in devices)
                        {
                            if (device.IsOfType("{3470B9B5-E685-4EB2-ABC0-2F4CCD7F686A}").GetAwaiter().GetResult())
                            {
                                count++;
                                if (device.IsOfType("{65C7B5C2-153D-4711-BAD7-D334FDB12338}").GetAwaiter().GetResult())
                                {
                                    temperature = new Temperature(device.GetValue<double>("Temperature").GetAwaiter().GetResult());
                                }
                                else if (device.IsOfType("{0B1DA7E1-1731-49AC-9814-47470E78EFAB}").GetAwaiter().GetResult())
                                {
                                    onCount += device.GetValue<bool>("PowerState").GetAwaiter().GetResult() ? 1 : 0;
                                }
                            }
                        }

                        // TODO: Aggregated properties
                        //ICollection<IPremiseObject> i = this_room.GetAggregatedProperties().GetAwaiter().GetResult();
                        //response.payload.applianceRoomStatus.lastOccupied = room.lastOccupied.ToString();

                        response.payload.applianceRoomStatus = new RoomStatus
                        {
                            friendlyName = toMatch,
                            occupied = room.Occupancy,
                            occupancyCount = room.OccupancyCount,
                            currentScene = room.CurrentScene,
                            deviceCount = count.ToString()
                        };
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
                InformLastContact("Get Space Status (space name missing in request)").GetAwaiter().GetResult();
            }
            else
            {
                InformLastContact("Get Space Status (no such room): " + alexaRequest.payload.space.name.ToLower()).GetAwaiter().GetResult();
            }
            response.header.@namespace = Faults.QueryNamespace;
            response.header.name = Faults.NoSuchTargetError;
            response.payload.exception = new ExceptionResponsePayload();
        }

        #endregion Process Space Mode Request

        #region Utility

        private static bool isAddOperation(string operation)
        {
            return (operation == "ASSIGN") || (operation == "PUT") || (operation == "ADD");
        }

        private static bool isRemoveOperation(string operation)
        {
            return (operation == "REMOVE") || (operation == "DELETE");
        }

        #endregion Utility

        #endregion Custom Skill
    }
}