using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SYSWebSockClient
{
    using IPremiseObjectCollection = ICollection<IPremiseObject>;

    public class Subscription
    {
        #region Fields

        public dynamic @params;
        public string alexaControllerName;
        public Action<dynamic> callback;
        public string clientSideSubscriptionId;
        public string propertyName;
        public string sysObjectId;

        #endregion Fields

        #region Constructors

        public Subscription(string clientSideId)
        {
            clientSideSubscriptionId = clientSideId;
        }

        #endregion Constructors
    }

    public sealed class SYSClient : JsonWebSocket, IDisposable
    {
        #region Fields

        // ToDo: CHRISBE: Let's make this use the Root by default at some point
        public static string HomeObjectId = "{4F846CA8-6603-4675-AC66-05A0AF6A8ACD}";

        private readonly ConcurrentDictionary<long, JsonRPCFuture> _futures;
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions;
        private Future ConnectFuture;
        private Action<Exception, string> disconnectCallback;

        #endregion Fields

        #region Constructors

        public SYSClient()
        {
            _futures = new ConcurrentDictionary<long, JsonRPCFuture>();
            _subscriptions = new ConcurrentDictionary<string, Subscription>();
        }

        #endregion Constructors

        #region Properties

        public new WebSocketState ConnectionState => base.ConnectionState;

        public ConcurrentDictionary<string, Subscription> Subscriptions => _subscriptions;

        #endregion Properties

        #region Methods

        public Task<IPremiseObject> ConnectAsync(string uri)
        {
            ConnectFuture = new Future();

            return Task.Run(
                () =>
                {
                    try
                    {
                        Connect(uri);
                        ConnectFuture.Await();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                        return null;
                    }
                    return new PremiseObject(this, HomeObjectId) as IPremiseObject;
                });
        }

        public void Disconnect(Action<Exception, string> callback)
        {
            disconnectCallback = callback;
            base.Disconnect();
        }

        internal void AddSubscription(string clientSideSubscriptionId, Subscription subscription)
        {
            _subscriptions.TryAdd(clientSideSubscriptionId, subscription);
        }

        internal void Send(JsonRPCFuture future, out Task task)
        {
            _futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);
                    try
                    {
                        future.Await();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                });
        }

        internal void Send(JsonRPCFuture future, out Task<IPremiseObject> task)
        {
            _futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    dynamic result;
                    try
                    {
                        result = future.Await();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                        return null;
                    }

                    var premiseObject = new PremiseObject(this, (string)result);
                    return (IPremiseObject)premiseObject;
                });
        }

        internal void Send(JsonRPCFuture future, string clientSideSubscriptionId, out Task<IPremiseSubscription> task)
        {
            _futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    dynamic result;
                    try
                    {
                        result = future.Await();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                        return null;
                    }
                    var premiseObject = new PremiseSubscription(this, HomeObjectId, (long)result, clientSideSubscriptionId);
                    return (IPremiseSubscription)premiseObject;
                });
        }

        internal void Send(JsonRPCFuture future, out Task<IPremiseObjectCollection> task)
        {
            _futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    JArray result;
                    try
                    {
                        result = future.Await() as JArray;
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                        return null;
                    }
                    var premiseObjects = new List<IPremiseObject>();
                    if (result == null)
                    {
                        return null;
                    }
                    foreach (var item in result)
                    {
                        var premiseObject = new PremiseObject(this, (string)item);
                        premiseObjects.Add(premiseObject);
                    }
                    var collection = (IPremiseObjectCollection)premiseObjects;
                    return collection;
                });
        }

        internal void Send(JsonRPCFuture future, out Task<dynamic> task)
        {
            _futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    dynamic result;

                    try
                    {
                        result = future.Await();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                        return null;
                    }
                    return result;
                });
        }

        internal void Send<T>(JsonRPCFuture future, out Task<T> task)
        {
            _futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    dynamic result = future.Await();
                    return (T)result;
                });
        }

        protected override void OnConnect()
        {
            ConnectFuture?.Notify(null, null);
        }

        protected override void OnDisconnect()
        {
            if (disconnectCallback == null)
                return;

            try
            {
                disconnectCallback(null, null);
            }
            catch (Exception error)
            {
                try
                {
                    disconnectCallback(error, null);
                }
                catch (Exception finalError)
                {
                    // eat it in retail build
                    Debug.WriteLine(finalError.ToString());
                }
            }
        }

        protected override void OnError(Exception error)
        {
            ConnectFuture?.Notify(error, null);
        }

        protected override void OnMessage(string message)
        {
            // Console.WriteLine("RECEIVED: {0}", message);
            dynamic json;
            try
            {
                json = JObject.Parse(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            if (!((JObject)json).TryGetValue("id", out JToken idObj))
            {
                // this is a notification from the server extract the method to call

                if (!((JObject)json).TryGetValue("method", out JToken methodObj))
                {
                    Console.WriteLine("JSON RPC 2.0 notification received with no method specified");
                    return;
                }

                // look up the callback function
                var method = methodObj.ToString();

                //Action<dynamic> callback;
                _subscriptions.TryGetValue(method, out Subscription subscription);

                if (subscription == null)
                {
                    Console.WriteLine("JSON RPC 2.0 notification received null subscription.");
                    return;
                }

                if (subscription.callback == null)
                {
                    Console.WriteLine("JSON RPC 2.0 notification received with no registered handler");
                    return;
                }

                //dynamic @params = json.@params;

                subscription.@params = json.@params;
                dynamic @params = subscription;

                subscription.callback(@params);

                return;
            }

            bool idExists = long.TryParse(idObj.ToString(), out long id);
            if (!idExists)
            {
                Console.WriteLine("ID set but is not long");
                return;
            }

            _futures.TryRemove(id, out JsonRPCFuture future);

            if (future == null)
            {
                // probably a notification
                return;
            }

            var error = json.error;
            var result = json.result;

            if (error != null)
            {
                // we have an error
                var errorMessage = (string)error.message;
                var ex = errorMessage != null ? new JsonRPCException(errorMessage) : new JsonRPCException("Undefined exception");

                future.Notify(ex, null);

                return;
            }

            future.Notify(null, result);
        }

        #endregion Methods

        #region Public Methods

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "ConnectFuture")]
        public void Dispose()
        {
            ConnectFuture?.Dispose();
        }

        #endregion Public Methods
    }
}