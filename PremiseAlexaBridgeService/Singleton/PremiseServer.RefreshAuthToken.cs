using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PremiseAlexaBridgeService
{
    public sealed partial class PremiseServer
    {
        #region Methods

        private static void RefreshAsyncToken(out string newToken)
        {
            const int eventID = 10;
            const string errorPrefix = "Refresh async token error:";

            using (asyncObjectsLock.Lock())
            {
                string previousToken;
                WebRequest refreshRequest;
                newToken = "";

                try
                {
                    refreshRequest = WebRequest.Create(AlexaEventTokenRefreshEndpoint);
                    refreshRequest.Method = WebRequestMethods.Http.Post;
                    refreshRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
                    previousToken = HomeObject.GetValue<string>("AlexaAsyncAuthorizationCode").GetAwaiter().GetResult();
                    string refresh_token = HomeObject.GetValue<string>("AlexaAsyncAuthorizationRefreshToken").GetAwaiter().GetResult();
                    string client_id = HomeObject.GetValue<string>("AlexaAsyncAuthorizationClientId").GetAwaiter().GetResult();
                    string client_secret = HomeObject.GetValue<string>("AlexaAsyncAuthorizationSecret").GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(refresh_token) || string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(client_secret))
                    {
                        Task.Run(async () => { await HomeObject.SetValue("SendAsyncEventsToAlexa", "False"); });
                        NotifyError(EventLogEntryType.Warning, $"{errorPrefix} Alexa authorization token is missing. PSUs are now disabled. Re-enable Premise skill.", eventID + 1).GetAwaiter().GetResult();
                        return;
                    }
                    string refreshData = $"grant_type=refresh_token&refresh_token={refresh_token}&client_id={client_id}&client_secret={client_secret}";
                    Stream stream = refreshRequest.GetRequestStream();
                    stream.Write(Encoding.UTF8.GetBytes(refreshData), 0, Encoding.UTF8.GetByteCount(refreshData));
                    stream.Close();
                }
                catch (Exception e)
                {
                    NotifyError(EventLogEntryType.Error, $"{errorPrefix} Unexpected error: {e.Message}", eventID + 2).GetAwaiter().GetResult();
                    return;
                }

                try
                {
                    using (HttpWebResponse httpResponse = refreshRequest.GetResponse() as HttpWebResponse)
                    {
                        if (httpResponse == null || !(httpResponse.StatusCode == HttpStatusCode.OK || httpResponse.StatusCode == HttpStatusCode.Accepted))
                        {
                            NotifyError(EventLogEntryType.Error, $"{errorPrefix} Null response or unexpected status: ({httpResponse?.StatusCode})", eventID + 3).GetAwaiter().GetResult();
                            return;
                        }

                        string responseString;

                        using (Stream response = httpResponse.GetResponseStream())
                        {
                            if (response == null)
                            {
                                NotifyError(EventLogEntryType.Warning, $"{errorPrefix} Null response from Amazon.", eventID + 4).GetAwaiter().GetResult();
                                return;
                            }
                            StreamReader reader = new StreamReader(response, Encoding.UTF8);
                            responseString = reader.ReadToEnd();
                        }

                        JObject json = JObject.Parse(responseString);

                        newToken = json["access_token"].ToString();
                        HomeObject.SetValue("AlexaAsyncAuthorizationCode", newToken).GetAwaiter().GetResult();
                        HomeObject.SetValue("AlexaAsyncAuthorizationRefreshToken", json["refresh_token"].ToString()).GetAwaiter().GetResult();
                        DateTime expiry = DateTime.UtcNow.AddSeconds((double)json["expires_in"]);
                        HomeObject.SetValue("AlexaAsyncAuthorizationCodeExpiry", expiry.ToString(CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                        Debug.WriteLine("async token refresh response:" + responseString);
                        WriteToWindowsApplicationEventLog(EventLogEntryType.Information, $"Alexa async auth token successfully refreshed. Previous Token (hash):{previousToken.GetHashCode()} New Token (hash):{newToken.GetHashCode()}", eventID);
                    }
                }
                catch (WebException e)
                {
                    using (WebResponse webresponse = e.Response)
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)webresponse;

                        if (httpResponse.StatusCode == HttpStatusCode.Unauthorized
                        ) // skill has ben disabled so disable sending Async events to Alexa
                        {
                            Task.Run(async () => { await HomeObject.SetValue("SendAsyncEventsToAlexa", "False"); });

                            string responseString;
                            using (Stream response = e.Response.GetResponseStream())
                            {
                                if (response == null)
                                {
                                    NotifyError(EventLogEntryType.Error, $"{errorPrefix} Null response.", eventID + 5).GetAwaiter().GetResult();
                                    return;
                                }
                                StreamReader reader = new StreamReader(response, Encoding.UTF8);
                                responseString = reader.ReadToEnd();
                            }
                            JObject json = JObject.Parse(responseString);
                            string message = $"{errorPrefix} PSUs are disabled. ErrorInfo: {json["error"]} Description: {json["error_description"]} More info: {json["error_uri"]}";
                            NotifyError(EventLogEntryType.Error, message, eventID + 6).GetAwaiter().GetResult();
                            return;
                        }
                        NotifyError(EventLogEntryType.Error, $"{errorPrefix} Unexpected error status ({httpResponse.StatusCode})", eventID + 7).GetAwaiter().GetResult();
                    }
                }
                catch (Exception e)
                {
                    NotifyError(EventLogEntryType.Error, $"{errorPrefix} Unexpected error: {e.Message}", eventID + 8).GetAwaiter().GetResult();
                }
            }
        }

        #endregion Methods
    }
}