namespace SYSWebSockClient
{
    using Newtonsoft.Json;
    using System;
    using System.Threading;

    internal static class FutureId
    {
        #region Fields

        private static long NextId;

        #endregion Fields

        #region Methods

        public static long Next()
        {
            return Interlocked.Increment(ref NextId);
        }

        #endregion Methods
    }

    internal class Future
    {
        #region Fields

        private readonly ManualResetEventSlim Event;
        private Exception Exception;
        private object Result;

        #endregion Fields

        #region Constructors

        public Future()
        {
            this.Event = new ManualResetEventSlim();
        }

        #endregion Constructors

        #region Methods

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

        public void Notify(Exception exception, object result)
        {
            this.Exception = exception;
            this.Result = result;
            this.Event.Set();
        }

        #endregion Methods
    }

    /// <summary>
    /// Represents a JSON RPC call Future
    /// </summary>
    internal class JsonRPCFuture : Future
    {
        #region Fields

        public long id;
        public string jsonrpc = JsonRPCVersion;
        public string method;
        private static string JsonRPCVersion = "2.0";

        #endregion Fields

        #region Constructors

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

        #endregion Constructors

        #region Methods

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        #endregion Methods
    }
}