namespace SYSWebSockClient
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using IPremiseObjectCollection = System.Collections.Generic.ICollection<IPremiseObject>;

    public class SYSClient : JsonWebSocket
    {
        private ConcurrentDictionary<long, JsonRPCFuture> Futures;
        private ConcurrentDictionary<string, Action<dynamic>> Subscriptions;

        // ToDo: CHRISBE: Let's make this use the Root by default at some point
        private static string HomeObjectId = "{4F846CA8-6603-4675-AC66-05A0AF6A8ACD}";

        private Future ConnectFuture;
        private Action<Exception, string> disconnectCallback;

        public SYSClient()
        {
            this.Futures = new ConcurrentDictionary<long, JsonRPCFuture>();
            this.Subscriptions = new ConcurrentDictionary<string, Action<dynamic>>();
        }

        protected override void OnError(Exception error)
        {
            if (this.ConnectFuture == null)
                return;

            this.ConnectFuture.Notify(error, null);
        }

        protected override void OnConnect()
        {
            if (this.ConnectFuture == null)
                return;

            this.ConnectFuture.Notify(null, null);
        }

        protected override void OnDisconnect()
        {
            if (this.disconnectCallback == null)
                return;

            try
            {
                this.disconnectCallback(null, null);
            }
            catch (Exception error)
            {
                try
                {
                    this.disconnectCallback(error, null);
                }
                catch (Exception finalError)
                {
                    // eat it in retail build
                    Debug.WriteLine(finalError.ToString());
                }
            }
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

            JToken idObj;

            if (!(json as JObject).TryGetValue("id", out idObj))
            {
                // this is a notification from the server
                // extract the method to call

                JToken methodObj;
                if (!(json as JObject).TryGetValue("method", out methodObj))
                {
                    Console.WriteLine("JSON RPC 2.0 notification received with no method specified");
                    return;
                }

                // look up the callback function
                var method = methodObj.ToString();
                Action<dynamic> callback;
                this.Subscriptions.TryGetValue(method, out callback);

                if (callback == null)
                {
                    Console.WriteLine("JSON RPC 2.0 notification received with no registered handler");
                    return;
                }

                dynamic @params = json.@params;

                callback(@params);

                return;
            }

            long id;
            bool idExists = long.TryParse(idObj.ToString(), out id);
            if (!idExists)
            {
                Console.WriteLine("ID set but is not long");
                return;
            }

            JsonRPCFuture future = null;
            this.Futures.TryRemove(id, out future);

            if (future == null)
            {
                // probably a notification
                return;
            }

            var error = json.error;
            var result = json.result;

            if (error != null)
            {
                JsonRPCException ex;

                // we have an error
                var errorMessage = (string) error.message;
                if (errorMessage != null)
                    ex = new JsonRPCException(errorMessage);
                else
                    ex = new JsonRPCException("Undefined exception");

                future.Notify(ex, null);

                return;
            }

            future.Notify(null, result);
        }

        internal void Send(JsonRPCFuture future, out Task task)
        {
            this.Futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    future.Await();
                });
        }

        internal void Send(JsonRPCFuture future, out Task<IPremiseObject> task)
        {
            this.Futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    dynamic result = future.Await();
                    var premiseObject = new PremiseObject(this, (string) result);
                    return premiseObject as IPremiseObject;
                });
        }

        internal void Send(JsonRPCFuture future, out Task<IPremiseSubscription> task)
        {
            this.Futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    dynamic result = future.Await();
                    var premiseObject = new PremiseSubscription(this, SYSClient.HomeObjectId, (long)result);
                    return premiseObject as IPremiseSubscription;
                });
        }

        internal void Send(JsonRPCFuture future, out Task<IPremiseObjectCollection> task)
        {
            this.Futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    JArray result = future.Await() as JArray;
                    var premiseObjects = new List<IPremiseObject>();

                    foreach (var item in result)
                    {
                        var premiseObject = new PremiseObject(this, (string)item);
                        premiseObjects.Add(premiseObject);
                    }

                    var collection = premiseObjects as IPremiseObjectCollection;
                    return collection;
                });
        }

        internal void Send(JsonRPCFuture future, out Task<dynamic> task)
        {
            this.Futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    dynamic result = future.Await();
                    return result;
                });
        }

        internal void Send<T>(JsonRPCFuture future, out Task<T> task)
        {
            this.Futures[future.id] = future;

            task = Task.Run(
                () =>
                {
                    string strMessage = JsonConvert.SerializeObject(future);
                    base.Send(strMessage);

                    dynamic result = future.Await();
                    return (T) result;
                });
        }

        public Task<IPremiseObject> Connect(string uri)
        {
            this.ConnectFuture = new Future();

            return Task.Run(
                () =>
                {
                    base.Connect(uri);
                    this.ConnectFuture.Await();
                    return new PremiseObject(this, SYSClient.HomeObjectId) as IPremiseObject;
                });
        }

        public void Disconnect(Action<Exception, string> callback)
        {
            this.disconnectCallback = callback;
            base.Disconnect();
        }

        internal void AddSubscription(string clientSideSubscriptionId, Action<dynamic> callback)
        {
            this.Subscriptions.TryAdd(clientSideSubscriptionId, callback);
        }
    }
}
