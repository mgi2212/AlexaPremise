using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Alexa.RegisteredTasks;

namespace SYSWebSockClient
{
    public class JsonWebSocket
    {
        #region Fields

        private static int receiveCount;
        private static int sendCount;
        private readonly object AccumulatorLock = new object();
        private byte[] Accumulator = ArrayUtils<byte>.Empty;
        private int AccumulatorLength;
        private BlockingCollection<byte[]> SendQueue;

        private ClientWebSocket WebSocket;

        #endregion Fields

        #region Constructors

        public JsonWebSocket()
        {
            WebSocket = null;
        }

        #endregion Constructors

        #region Properties

        public WebSocketState ConnectionState
        {
            get
            {
                if (WebSocket != null)
                {
                    return WebSocket.State;
                }
                return WebSocketState.None;
            }
        }

        #endregion Properties

        #region Methods

        public void Disconnect(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure)
        {
            SendQueue.CompleteAdding();

            WebSocket.CloseAsync(status, "Closing Connection", CancellationToken.None);
        }

        public void Send(string message)
        {
            byte[] sendBuffer = Encoding.UTF8.GetBytes(message);

            Interlocked.Increment(ref sendCount);

            SendQueue.Add(sendBuffer);
        }

        protected async void Connect(string uri)
        {
            if (WebSocket != null)
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    Disconnect();
                }
                WebSocket = null;
            }

            WebSocket = new ClientWebSocket();
            WebSocket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);

            SendQueue = null;
            SendQueue = new BlockingCollection<byte[]>();

            await WebSocket.ConnectAsync(new Uri(uri), CancellationToken.None)
                .ContinueWith(
                    task =>
                    {
                        if (WebSocket.State != WebSocketState.Open)
                        {
                            OnError(new Exception("Cannot open Premise Connection!"));
                            return;
                        }

                        OnConnect();

                        BackgroundTaskManager.Run(() =>
                        {
                            StartSending();
                        });

                        BackgroundTaskManager.Run(() =>
                        {
                            StartReceiving();
                        });
                    }).ConfigureAwait(false);
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

        private void Accumulate(byte[] buffer, int count, bool endOfMessage)
        {
            lock (AccumulatorLock)
            {
                ArrayUtils.AddRange(ref Accumulator, ref AccumulatorLength, buffer, count);
            }

            if (!endOfMessage)
                return;

            string message;

            lock (AccumulatorLock)
            {
                message = Encoding.UTF8.GetString(Accumulator, 0, AccumulatorLength);
                AccumulatorLength = 0;
            }

            try
            {
                //Debug.WriteLine(message);
                OnMessage(message);
            }
            catch
            {
                // ignored
            }
        }

        private void StartReceiving()
        {
            loop:
            if (WebSocket.State != WebSocketState.Open)
                return;

            byte[] buffer = new byte[4096];

            WebSocketReceiveResult result;
            try
            {
                result = WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).GetAwaiter().GetResult();
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
                    Accumulate(buffer, result.Count, result.EndOfMessage);
                    break;

                case WebSocketMessageType.Binary:
                    Disconnect();
                    break;

                case WebSocketMessageType.Close:
                    Disconnect();
                    break;

                default:
                    Disconnect();
                    break;
            }

            goto loop;
        }

        private void StartSending()
        {
            foreach (var sendBuffer in SendQueue.GetConsumingEnumerable())
            {
                // Note: CHRISBE: ClientWebSocket SendAsync can be called from any thread, but
                // SendAsync isn't thread safe - so you can't issue overlapped calls kind of lame
                // ...sooo have to wait for it to complete
                try
                {
                    WebSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        #endregion Methods
    }
}