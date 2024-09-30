#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

#endregion

internal class PipeIPC
{
    // Delegate for passing received message back to caller
    public delegate void DelegateMessage(PipeServer pipe, string data);

    private readonly List<PipeClient> clientPipes;

    private readonly Dispatcher mDispatcher;
    private readonly string mPipeName;

    private readonly List<PipeServer> serverPipes;

    public PipeIPC(string PipeName)
    {
        mDispatcher = Dispatcher.CurrentDispatcher;

        mPipeName = PipeName;

        serverPipes = new List<PipeServer>();
        clientPipes = new List<PipeClient>();
    }

    public event DelegateMessage PipeMessage;

    public void Listen()
    {
        PipeServer serverPipe = new(mPipeName);
        serverPipes.Add(serverPipe);

        serverPipe.DataReceived += (sndr, data) =>
        {
            mDispatcher.Invoke(() => { PipeMessage?.Invoke(serverPipe, data); });
        };

        serverPipe.Connected += (sndr, args) => { mDispatcher.Invoke(() => { Listen(); }); };

        serverPipe.PipeClosed += (sndr, args) => { mDispatcher.Invoke(() => { serverPipes.Remove(serverPipe); }); };
    }

    public PipeClient Connect(int TimeOut = 10000)
    {
        PipeClient clientPipe = new(".", mPipeName);
        if (!clientPipe.Connect(TimeOut))
            return null;

        clientPipes.Add(clientPipe);

        clientPipe.PipeClosed += (sndr, args) => { mDispatcher.Invoke(() => { clientPipes.Remove(clientPipe); }); };

        return clientPipe;
    }

    internal class PipeTmpl<T> where T : PipeStream
    {
        protected T pipeStream;
        public event EventHandler<string> DataReceived;
        public event EventHandler<EventArgs> PipeClosed;

        public void Close()
        {
            pipeStream.Flush();
            pipeStream.WaitForPipeDrain();
            pipeStream.Close();
            pipeStream.Dispose();
            pipeStream = null;
        }

        public bool IsConnected()
        {
            return pipeStream.IsConnected;
        }

        public Task Send(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] data = BitConverter.GetBytes(bytes.Length);
            byte[] buff = data.Concat(bytes).ToArray();

            return pipeStream.WriteAsync(buff, 0, buff.Length);
        }

        protected void initAsyncReader()
        {
            new Action<PipeTmpl<T>>(p =>
            {
                p.RunAsyncByteReader(b =>
                {
                    DataReceived?.Invoke(this, Encoding.UTF8.GetString(b).TrimEnd('\0'));
                });
            })(this);
        }

        protected void RunAsyncByteReader(Action<byte[]> asyncReader)
        {
            int len = sizeof(int);
            byte[] buff = new byte[len];

            // read the length
            pipeStream.ReadAsync(buff, 0, len).ContinueWith(ret =>
            {
                if (ret.Result == 0)
                {
                    PipeClosed?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // read the data
                len = BitConverter.ToInt32(buff, 0);
                buff = new byte[len];
                pipeStream.ReadAsync(buff, 0, len).ContinueWith(ret2 =>
                {
                    if (ret2.Result == 0)
                    {
                        PipeClosed?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    asyncReader(buff);
                    RunAsyncByteReader(asyncReader);
                });
            });
        }

        public void Flush()
        {
            pipeStream.Flush();
        }
    }

    internal class PipeServer : PipeTmpl<NamedPipeServerStream>
    {
        public PipeServer(string pipeName)
        {
            PipeSecurity pipeSa = new();
            pipeSa.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(FileOps.SID_Worls),
                PipeAccessRights.FullControl, AccessControlType.Allow));
            int buffLen = 1029; // 4 + 1024 + 1
            pipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
                buffLen, buffLen, pipeSa);
            pipeStream.BeginWaitForConnection(PipeConnected, null);
        }

        public event EventHandler<EventArgs> Connected;

        protected void PipeConnected(IAsyncResult asyncResult)
        {
            pipeStream.EndWaitForConnection(asyncResult);
            Connected?.Invoke(this, new EventArgs());
            initAsyncReader();
        }
    }

    internal class PipeClient : PipeTmpl<NamedPipeClientStream>
    {
        private readonly ConcurrentStack<string> MessageQueue = new(); // LIFO

        public PipeClient(string serverName, string pipeName)
        {
            pipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        public bool Connect(int TimeOut = 10000)
        {
            try
            {
                pipeStream.Connect(TimeOut);
            }
            catch
            {
                return false; // timeout
            }

            DataReceived += (sndr, data) => { MessageQueue.Push(data); };

            initAsyncReader();
            return true;
        }

        public string Read(int TimeOut = 10000)
        {
            MessageQueue.Clear();
            // DateTime.Now.Ticks is in 100 ns, TimeOut is in ms
            for (long ticksEnd = DateTime.Now.Ticks + TimeOut * 10000; ticksEnd > DateTime.Now.Ticks;)
            {
                Application.DoEvents();
                if (!IsConnected())
                    break;
                if (MessageQueue.Count > 0)
                    break;
            }

            return Read();
        }

        public string Read()
        {
            // the MessageQueue is a last in first out type of container, so we need to reverse it
            string ret = string.Join("\0", MessageQueue.ToArray().Reverse());
            MessageQueue.Clear();
            return ret;
        }
    }
}
