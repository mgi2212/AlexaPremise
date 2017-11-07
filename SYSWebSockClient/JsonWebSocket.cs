using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
        private CancellationToken sendReceiveCancellationToken;
        private CancellationTokenSource sendReceiveCancellationTokenSource;
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

            WebSocket.CloseAsync(status, "Closing Connection", CancellationToken.None).GetAwaiter().GetResult();

            sendReceiveCancellationTokenSource.Cancel();
        }

        public void Send(string message)
        {
            byte[] sendBuffer = Encoding.UTF8.GetBytes(message);

            Interlocked.Increment(ref sendCount);

            SendQueue.Add(sendBuffer, sendReceiveCancellationToken);
        }

        [SuppressMessage("ReSharper", "AsyncConverter.AsyncAwaitMayBeElidedHighlighting")]
        protected async void Connect(string uri)
        {
            if (WebSocket != null)
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    sendReceiveCancellationTokenSource?.Cancel();
                    Disconnect();
                }
                WebSocket = null;
                sendReceiveCancellationTokenSource = null;
                sendReceiveCancellationToken = CancellationToken.None;
            }

            WebSocket = new ClientWebSocket();
            WebSocket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);

            SendQueue = null;
            SendQueue = new BlockingCollection<byte[]>();

            sendReceiveCancellationTokenSource = new CancellationTokenSource();
            sendReceiveCancellationToken = sendReceiveCancellationTokenSource.Token;

            await WebSocket.ConnectAsync(new Uri(uri), sendReceiveCancellationToken)
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
                            StartSending(sendReceiveCancellationToken);
                        });

                        BackgroundTaskManager.Run(() =>
                        {
                            StartReceiving(sendReceiveCancellationToken);
                        });
                    }, sendReceiveCancellationToken).ConfigureAwait(false);
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

        private void StartReceiving(CancellationToken token)
        {
            loop:

            if (token.IsCancellationRequested || WebSocket.State != WebSocketState.Open)
            {
                return;
            }

            byte[] buffer = new byte[4096];

            WebSocketReceiveResult result;
            try
            {
                result = WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token).GetAwaiter().GetResult();
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

        private void StartSending(CancellationToken token)
        {
            foreach (var sendBuffer in SendQueue.GetConsumingEnumerable(token))
            {
                if (token.IsCancellationRequested || WebSocket.State != WebSocketState.Open)
                {
                    return;
                }

                // Note: CHRISBE: ClientWebSocket SendAsync can be called from any thread, but
                // SendAsync isn't thread safe - so you can't issue overlapped calls kind of lame
                // ...sooo have to wait for it to complete
                try
                {
                    WebSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, token).GetAwaiter().GetResult();
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