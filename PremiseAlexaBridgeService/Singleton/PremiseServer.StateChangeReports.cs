using Alexa.SmartHomeAPI.V3;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PremiseAlexaBridgeService
{
    public sealed partial class PremiseServer
    {
        #region Methods

        private static void SendStateChangeReportsToAlexa()
        {
            const int eventID = 20;
            const string errorPrefix = "Proactive state update error:";

            // This queue blocks in the enumerator so this is essentially an infinite loop.
            foreach (StateChangeReportWrapper item in stateReportQueue)
            {
                WebRequest request;
                try
                {
                    string expiry;
                    using (asyncObjectsLock.Lock())
                    {
                        expiry = HomeObject.GetValue<string>("AlexaAsyncAuthorizationCodeExpiry").GetAwaiter()
                            .GetResult();
                    }
                    if (string.IsNullOrEmpty(expiry))
                    {
                        Task.Run(async () => { await HomeObject.SetValue("SendAsyncEventsToAlexa", "False").ConfigureAwait(false); });
                        NotifyErrorAsync(EventLogEntryType.Error,
                            $"{errorPrefix}: no PSU expiry datetime. PSUs are now disabled. Enable premise skill.",
                            eventID + 1).GetAwaiter().GetResult();
                    }
                    if (!DateTime.TryParse(expiry, out var expiryDateTime))
                    {
                        Task.Run(async () => { await HomeObject.SetValue("SendAsyncEventsToAlexa", "False").ConfigureAwait(false); });
                        NotifyErrorAsync(EventLogEntryType.Error,
                            $"{errorPrefix} Cannot parse expiry date. PSUs are now disabled. Enable premise skill.",
                            eventID + 1).GetAwaiter().GetResult();
                        continue;
                    }

                    // refresh auth token if expired
                    if (DateTime.Compare(DateTime.UtcNow, expiryDateTime) >= 0)
                    {
                        RefreshAsyncToken(out string newToken);
                        if (string.IsNullOrEmpty(newToken)
                        ) // Error occurred during refresh - error logged in that method.
                        {
                            continue;
                        }
                        item.ChangeReport.@event.endpoint.scope.token = newToken;
                        foreach (var queuedItem in stateReportQueue.InternalQueue)
                        {
                            queuedItem.ChangeReport.@event.endpoint.scope.token = newToken;
                        }
                    }

                    request = WebRequest.Create(item.uri);
                    request.Method = WebRequestMethods.Http.Post;
                    request.ContentType = @"application/json";
                    request.ContentLength = item.Json.Length;

                    Stream stream = request.GetRequestStream();
                    stream.Write(item.Bytes, 0, (int)request.ContentLength);
                    stream.Close();
                }
                catch (Exception ex)
                {
                    NotifyErrorAsync(EventLogEntryType.Error, $"{errorPrefix}: Unexpected error: {ex.Message}", eventID + 1)
                        .GetAwaiter().GetResult();
                    continue;
                }

                try
                {
                    using (HttpWebResponse httpResponse = request.GetResponse() as HttpWebResponse)
                    {
                        if (httpResponse == null ||
                            !(httpResponse.StatusCode == HttpStatusCode.OK ||
                              httpResponse.StatusCode == HttpStatusCode.Accepted))
                        {
                            NotifyErrorAsync(EventLogEntryType.Error,
                                    $"{errorPrefix} Unexpected status ({httpResponse?.StatusCode})", eventID + 2)
                                .GetAwaiter().GetResult();
                            continue;
                        }

                        string responseString;

                        using (Stream response = httpResponse.GetResponseStream())
                        {
                            if (response == null)
                            {
                                NotifyErrorAsync(EventLogEntryType.Warning, $"{errorPrefix} Null response from request.",
                                    eventID + 3).GetAwaiter().GetResult();
                                continue;
                            }
                            StreamReader reader = new StreamReader(response, Encoding.UTF8);
                            responseString = reader.ReadToEnd();
                        }
                        IncrementCounterAsync("AlexaAsyncUpdateCount").GetAwaiter().GetResult();
                        Debug.WriteLine($"PSU Response Status ({httpResponse.StatusCode}) ContentLength: {httpResponse.ContentLength} Content: {responseString}");
                        Debug.WriteLine(item.Json);
                    }
                }
                catch (WebException e)
                {
                    using (WebResponse webresponse = e.Response)
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)webresponse;

                        switch (httpResponse.StatusCode)
                        {
                            case HttpStatusCode.Unauthorized
                            : // The skill is enabled, but the authentication token has expired.
                                RefreshAsyncToken(out string newToken);
                                if (string.IsNullOrEmpty(newToken)
                                ) // Error occurred during refresh - error logged in that method.
                                {
                                    continue;
                                }
                                item.ChangeReport.@event.endpoint.scope.token = newToken;
                                foreach (var queuedItem in stateReportQueue.InternalQueue)
                                {
                                    queuedItem.ChangeReport.@event.endpoint.scope.token = newToken;
                                }
                                stateReportQueue.Enqueue(item);
                                continue;
                            case HttpStatusCode.Forbidden
                            : // The skill is disabled so disable sending Async events to Alexa
                                Task.Run(async () => { await HomeObject.SetValue("SendAsyncEventsToAlexa", "False").ConfigureAwait(false); });
                                NotifyErrorAsync(EventLogEntryType.Error,
                                    $"{errorPrefix} Premise skill has been disabled. PSUs are now disabled. Enable premise skill.",
                                    eventID + 4).GetAwaiter().GetResult();
                                continue;
                            case HttpStatusCode.BadRequest:
                                NotifyErrorAsync(EventLogEntryType.Warning,
                                    $"{errorPrefix} The message contains invalid identifying information such as a invalid endpoint Id or correlation token. Message:\r\n {item.Json}",
                                    eventID + 5).GetAwaiter().GetResult();
                                continue;
                            default:
                                NotifyErrorAsync(EventLogEntryType.Error, $"{errorPrefix} Unexpected status: {e.Message}",
                                    eventID + 6).GetAwaiter().GetResult();
                                continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    NotifyErrorAsync(EventLogEntryType.Error, $"{errorPrefix} Unexpected error: {ex.Message}", eventID + 7)
                        .GetAwaiter().GetResult();
                    continue;
                }

                Thread.Sleep(100); // throttle per spec
            }
        }

        #endregion Methods
    }

    public class StateChangeReportWrapper
    {
        #region Fields

        public readonly string uri;

        #endregion Fields

        #region Constructors

        public StateChangeReportWrapper()
        {
            uri = PremiseServer.AlexaEventEndpoint;
        }

        #endregion Constructors

        #region Properties

        public byte[] Bytes => Encoding.UTF8.GetBytes(Json);

        public AlexaChangeReport ChangeReport { get; set; }

        public string Json => JsonConvert.SerializeObject(ChangeReport, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        #endregion Properties
    }
}