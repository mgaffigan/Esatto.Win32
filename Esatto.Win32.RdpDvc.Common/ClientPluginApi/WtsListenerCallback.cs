#define TRACE

using Esatto.Win32.RdpDvc.TSVirtualChannels;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Esatto.Win32.RdpDvc.ClientPluginApi
{
    // Runs on RDS Client to accept incoming connections
    // Thunk from mstsc Plugin to handle a new connection attempt
    public sealed class WtsListenerCallback : IWTSListenerCallback
    {
        public string ChannelName { get; }

        private readonly ILogger Logger;
        private readonly Action<IAsyncDvcChannel> AcceptChannel;

        public WtsListenerCallback(string channelName, Action<IAsyncDvcChannel> handleAccept, ILogger logger)
        {
            NativeMethods.ValidateChannelName(channelName);

            this.ChannelName = channelName;
            this.Logger = logger;
            this.AcceptChannel = handleAccept ?? throw new ArgumentNullException(nameof(handleAccept));
        }

        // Called from COM
        void IWTSListenerCallback.OnNewChannelConnection(IWTSVirtualChannel pChannel,
            [MarshalAs(UnmanagedType.BStr)] string data,
            [MarshalAs(UnmanagedType.Bool)] out bool pAccept, out IWTSVirtualChannelCallback? pCallback)
        {
            try
            {
                var channel = new DvcClientChannel(ChannelName, pChannel, Logger);
                AcceptChannel(channel);

                pAccept = true;
                pCallback = channel.Proxy;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failure while creating client channel for '{ChannelName}'", ChannelName);

                pAccept = false;
                pCallback = null;
            }
        }
    }
}
