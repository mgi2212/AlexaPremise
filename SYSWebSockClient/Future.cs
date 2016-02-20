namespace SYSWebSockClient
{
    using Newtonsoft.Json;
    using System;
    using System.Threading;

    internal static class FutureId
    {
        private static long NextId;

        public static long Next()
        {
            return Interlocked.Increment(ref FutureId.NextId);
        }
    }

    internal class Future
    {
        private ManualResetEventSlim Event;
        private Exception Exception;
        private object Result;

        public Future()
        {
            this.Event = new ManualResetEventSlim();
        }

        public void Notify(Exception exception, object result)
        {
            this.Exception = exception;
            this.Result = result;
            this.Event.Set();
        }

        public object Await()
        {
            // bool complete = this.Event.Wait(5000);
            bool complete = true;
            this.Event.Wait();

            if (!complete)
                throw new JsonRPCException("Operation timed out");

            if (this.Exception == null)
                return this.Result;

            throw this.Exception;
        }
    }

    /// <summary>
    /// Represents a JSON RPC call Future
    /// </summary>
    internal class JsonRPCFuture : Future
    {
        private static string JsonRPCVersion = "2.0";
        public string jsonrpc = JsonRPCFuture.JsonRPCVersion;
        public long id;
        public string method;

        protected JsonRPCFuture(string objectId, string method)
        {
            this.id = FutureId.Next();

            if (string.IsNullOrEmpty(objectId))
            {
                this.method = method;
                return;
            }

            if (objectId.EndsWith("/"))
                this.method = objectId + method;
            else
                this.method = objectId + "/" + method;
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
