using System;
using System.Threading;
using Newtonsoft.Json;

namespace SYSWebSockClient
{
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
            Event = new ManualResetEventSlim();
        }

        #endregion Constructors

        #region Methods

        public object Await()
        {
            // bool complete = this.Event.Wait(5000);
            bool complete = true;
            Event.Wait();

            if (!complete)
                throw new JsonRPCException("Operation timed out");

            if (Exception == null)
                return Result;

            throw Exception;
        }

        public void Notify(Exception exception, object result)
        {
            Exception = exception;
            Result = result;
            Event.Set();
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
            id = FutureId.Next();

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