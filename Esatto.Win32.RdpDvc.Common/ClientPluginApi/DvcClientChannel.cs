using Esatto.Win32.RdpDvc.TSVirtualChannels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.ClientPluginApi
{
    // Runs on RDS Client
    // Wraps COM channel to provide IAsyncDvcChannel
    internal sealed class DvcClientChannel : IAsyncDvcChannel, IDisposable
    {
        public string ChannelName { get; }
        private readonly DelegateWtsVirtualChannelCallback _Proxy;
        internal IWTSVirtualChannelCallback Proxy => _Proxy;
        private readonly ILogger Logger;
        private readonly MessageQueue ReadQueue;

        private bool isDisposed;
        private bool isDisconnected;
        public event EventHandler? Disconnected;

        internal DvcClientChannel(string channelName, IWTSVirtualChannel callback, ILogger logger)
        {
            this.Logger = logger;
            this.ReadQueue = new MessageQueue();
            this.ChannelName = channelName;
            this._Proxy = new DelegateWtsVirtualChannelCallback(callback, this);
        }

        private void AssertAlive()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(ChannelName);
            }
        }

        public void Dispose()
        {
            AssertAlive();
            isDisposed = true;

            try
            {
                // Calls out to client code, could throw.
                ReadQueue.Dispose();
            }
            finally
            {
                _Proxy.CloseWriteChannel();
            }
        }

        // Sugar
        public Task SendMessageAsync(byte[] data, int offset, int length) => Task.Run(() => SendMessage(data, offset, length));
        public byte[] ReadMessage(CancellationToken ct) => ReadMessageAsync(ct).GetAwaiter().GetResult();

        public void SendMessage(byte[] data, int offset, int length)
        {
            AssertAlive();
            if (isDisconnected)
            {
                throw new DvcChannelDisconnectedException();
            }

            this._Proxy.WriteMessage(data, offset, length);
        }

        public Task<byte[]> ReadMessageAsync(CancellationToken ct)
        {
            AssertAlive();
            if (isDisconnected)
            {
                throw new DvcChannelDisconnectedException();
            }

            return ReadQueue.ReadAsync(ct);
        }

        private sealed class DelegateWtsVirtualChannelCallback : IWTSVirtualChannelCallback
        {
            private IWTSVirtualChannel? _NativeChannel;
            private IWTSVirtualChannel NativeChannel => _NativeChannel
                ?? throw new ObjectDisposedException(nameof(DelegateWtsVirtualChannelCallback));

            public readonly DvcClientChannel Parent;

            public DelegateWtsVirtualChannelCallback(IWTSVirtualChannel pChannel, DvcClientChannel parent)
            {
                this._NativeChannel = pChannel ?? throw new ArgumentNullException(nameof(pChannel));
                this.Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            }

            // Called from COM
            void IWTSVirtualChannelCallback.OnDataReceived(int cbSize, IntPtr pBuffer)
            {
                var data = new byte[cbSize];
                Marshal.Copy(pBuffer, data, 0, cbSize);

                try
                {
                    if (Parent.isDisposed)
                    {
                        return;
                    }

                    Parent.ReadQueue.WriteMessage(data);
                }
                catch (Exception ex)
                {
                    Parent.Logger.LogError(ex, "Uncaught exception in ReadMessage");
                }
            }

            // Called from COM
            void IWTSVirtualChannelCallback.OnClose()
            {
                try
                {
                    if (Parent.isDisposed)
                    {
                        return;
                    }
                    Parent.isDisconnected = true;

                    Parent.Dispose();
                    Parent.Disconnected?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Parent.Logger.LogError(ex, "Uncaught exception in OnClose");
                }
            }

            // Called from parent
            public unsafe void WriteMessage(byte[] data, int offset, int count)
            {
                if (offset < 0 || offset >= data.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (count < 0 || count + offset > data.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                fixed (byte* pData = data)
                {
                    IntPtr pStart = (IntPtr)(pData + offset);
                    NativeChannel.Write((uint)count, pStart, IntPtr.Zero);
                }
            }

            // Called from parent
            public void CloseWriteChannel()
            {
                NativeChannel.Close();
                _NativeChannel = null;
            }
        }
    }
}
