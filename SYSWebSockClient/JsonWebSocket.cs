namespace SYSWebSockClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    public class JsonWebSocket
    {
        private object AccumulatorLock = new object();
        private byte[] Accumulator = ArrayUtils<byte>.Empty;
        private int AccumulatorLength;
        private BlockingCollection<byte[]> SendQueue = new BlockingCollection<byte[]>();

        private ClientWebSocket WebSocket = null;

        public JsonWebSocket()
        {
            this.WebSocket = null;
        }

        public WebSocketState ConnectionState
        {
            get
            {
                if (WebSocket != null)
                {
                    return this.WebSocket.State;
                }
                else
                {
                    return WebSocketState.None;
                }
            }
        }

        protected async void Connect(string uri)
        {
            this.WebSocket = new ClientWebSocket();
            ClientWebSocketOptions options = this.WebSocket.Options;
            this.WebSocket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);
            await this.WebSocket.ConnectAsync(new Uri(uri), CancellationToken.None).ContinueWith(
                (task) =>
                {
                    if (this.WebSocket.State != WebSocketState.Open)
                    {
                        this.OnError(new Exception("Cannot open Premise Connection!"));
                        return;
                    }

                    this.OnConnect();

                    Task.Factory.StartNew(this.StartSending);
                    Task.Factory.StartNew(this.StartReceiving);
                }
                );
        }

        protected virtual void OnConnect()
        {
        }

        protected virtual void OnDisconnect()
        {
        }

        protected virtual void OnError(Exception error)
        {
        }
        protected virtual void OnMessage(string message)
        {
        }

        static int sendCount = 0;

        public void Send(string message)
        {
            byte[] sendBuffer = Encoding.UTF8.GetBytes(message);

            Interlocked.Increment(ref sendCount);

            this.SendQueue.Add(sendBuffer);
        }

        private void StartSending()
        {
            foreach (var sendBuffer in this.SendQueue.GetConsumingEnumerable())
            {
                // Note: CHRISBE: ClientWebSocket SendAsync can be called from any thread, but
                // SendAsync isn't thread safe - so you can't issue overlapped calls
                // kind of lame
                // ...sooo have to wait for it to complete
                try
                { 
                    this.WebSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    this.OnError(ex);
                }
            }
        }

        private void Accumulate(byte[] buffer, int count, bool endOfMessage)
        {
            lock (this.AccumulatorLock)
            {
                ArrayUtils.AddRange(ref this.Accumulator, ref this.AccumulatorLength, buffer, count);
            }

            if (!endOfMessage)
                return;

            string message = string.Empty;

            lock (this.AccumulatorLock)
            {
                message = Encoding.UTF8.GetString(this.Accumulator, 0, this.AccumulatorLength);
                this.AccumulatorLength = 0;
            }

            try
            {
                this.OnMessage(message);
            }
            catch
            {
            }
        }

        public void Disconnect(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure)
        {
            this.SendQueue.CompleteAdding();

            this.WebSocket.CloseAsync(status, "Closing Connection", CancellationToken.None);
        }

        static int receiveCount = 0;

        private void StartReceiving()
        {
        loop:
            if (this.WebSocket.State != WebSocketState.Open)
                return;

            byte[] buffer = new byte[4096];

            WebSocketReceiveResult result;
            try
            { 
                result = this.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                //TODO: mark client as closed at some point.
                return;
            }

            Interlocked.Increment(ref receiveCount);

            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    this.Accumulate(buffer, result.Count, result.EndOfMessage);
                    break;
                case WebSocketMessageType.Binary:
                    this.Disconnect();
                    break;
                case WebSocketMessageType.Close:
                    this.Disconnect();
                    break;
                default:
                    this.Disconnect();
                    break;
            }

            goto loop;
        }
    }
}
