﻿#region

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

namespace wumgr.Common;

internal class PipeIpc(string pipeName)
{
    // Delegate for passing received message back to caller
    public delegate void DelegateMessage(PipeServer pipe, string data);

    private readonly Dispatcher _mDispatcher = Dispatcher.CurrentDispatcher;
    private readonly List<PipeServer> _serverPipes = new();

    private readonly List<PipeClient> ClientPipes = new();

    public event DelegateMessage PipeMessage;

    public void Listen()
    {
        PipeServer serverPipe = new(pipeName);
        _serverPipes.Add(serverPipe);

        serverPipe.DataReceived += (sndr, data) =>
        {
            _mDispatcher.Invoke(() => { PipeMessage?.Invoke(serverPipe, data); });
        };

        serverPipe.Connected += (sndr, args) => { _mDispatcher.Invoke(Listen); };

        serverPipe.PipeClosed += (sndr, args) => { _mDispatcher.Invoke(() => { _serverPipes.Remove(serverPipe); }); };
    }

    public PipeClient Connect(int timeOut = 10000)
    {
        PipeClient clientPipe = new(".", pipeName);
        if (!clientPipe.Connect(timeOut))
            return null;

        ClientPipes.Add(clientPipe);

        clientPipe.PipeClosed += (sndr, args) => { _mDispatcher.Invoke(() => { ClientPipes.Remove(clientPipe); }); };

        return clientPipe;
    }

    internal class PipeTmpl<T> where T : PipeStream
    {
        protected T PipeStream;
        public event EventHandler<string> DataReceived;
        public event EventHandler<EventArgs> PipeClosed;

        public void Close()
        {
            PipeStream.Flush();
            PipeStream.WaitForPipeDrain();
            PipeStream.Close();
            PipeStream.Dispose();
            PipeStream = null;
        }

        protected bool IsConnected()
        {
            return PipeStream.IsConnected;
        }

        public Task Send(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] data = BitConverter.GetBytes(bytes.Length);
            byte[] buff = data.Concat(bytes).ToArray();

            return PipeStream.WriteAsync(buff, 0, buff.Length);
        }

        protected void InitAsyncReader()
        {
            new Action<PipeTmpl<T>>(p =>
            {
                p.RunAsyncByteReader(b =>
                {
                    DataReceived?.Invoke(this, Encoding.UTF8.GetString(b).TrimEnd('\0'));
                });
            })(this);
        }

        private void RunAsyncByteReader(Action<byte[]> asyncReader)
        {
            int len = sizeof(int);
            byte[] buff = new byte[len];

            // read the length
            PipeStream.ReadAsync(buff, 0, len).ContinueWith(ret =>
            {
                if (ret.Result == 0)
                {
                    PipeClosed?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // read the data
                len = BitConverter.ToInt32(buff, 0);
                buff = new byte[len];
                PipeStream.ReadAsync(buff, 0, len).ContinueWith(ret2 =>
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
            PipeStream.Flush();
        }
    }

    internal class PipeServer : PipeTmpl<NamedPipeServerStream>
    {
        public PipeServer(string pipeName)
        {
            PipeSecurity pipeSa = new();
            pipeSa.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(FileOps.SidWorls),
                PipeAccessRights.FullControl, AccessControlType.Allow));
            int buffLen = 1029; // 4 + 1024 + 1
            PipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
                buffLen, buffLen, pipeSa);
            PipeStream.BeginWaitForConnection(PipeConnected, null);
        }

        public event EventHandler<EventArgs> Connected;

        private void PipeConnected(IAsyncResult asyncResult)
        {
            PipeStream.EndWaitForConnection(asyncResult);
            Connected?.Invoke(this, EventArgs.Empty);
            InitAsyncReader();
        }
    }

    internal class PipeClient : PipeTmpl<NamedPipeClientStream>
    {
        private readonly ConcurrentStack<string> _messageQueue = new(); // LIFO

        public PipeClient(string serverName, string pipeName)
        {
            PipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        public bool Connect(int timeOut = 10000)
        {
            try
            {
                PipeStream.Connect(timeOut);
            }
            catch
            {
                return false; // timeout
            }

            DataReceived += (sndr, data) => { _messageQueue.Push(data); };

            InitAsyncReader();
            return true;
        }

        public string Read(int timeOut = 10000)
        {
            _messageQueue.Clear();
            // DateTime.Now.Ticks is in 100 ns, TimeOut is in ms
            for (long ticksEnd = DateTime.Now.Ticks + timeOut * 10000; ticksEnd > DateTime.Now.Ticks;)
            {
                Application.DoEvents();
                if (!IsConnected())
                    break;
                if (_messageQueue.Count > 0)
                    break;
            }

            return Read();
        }

        private string Read()
        {
            // the MessageQueue is a last in first out type of container, so we need to reverse it
            string ret = string.Join("\0", _messageQueue.ToArray().Reverse());
            _messageQueue.Clear();
            return ret;
        }
    }
}
