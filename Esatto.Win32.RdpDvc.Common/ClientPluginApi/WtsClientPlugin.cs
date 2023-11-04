#define TRACE

using Esatto.Win32.RdpDvc.TSVirtualChannels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Esatto.Win32.RdpDvc.ClientPluginApi
{
    // Runs on RDS Client
    // Class called by MSTSC to control plugin lifetime
    public sealed class WtsClientPlugin : IWTSPlugin
    {
        private readonly ILogger Logger;
        private readonly IReadOnlyDictionary<string, Action<IAsyncDvcChannel>> Registrations;
        private readonly List<WtsListenerCallback> Callbacks;

        public WtsClientPlugin(Dictionary<string, Action<IAsyncDvcChannel>> registeredServices, ILogger logger)
        {
            this.Logger = logger;
            this.Registrations = new ReadOnlyDictionary<string, Action<IAsyncDvcChannel>>(registeredServices);
            this.Callbacks = new List<WtsListenerCallback>(registeredServices.Count);
        }

        void IWTSPlugin.Initialize(IWTSVirtualChannelManager pChannelMgr)
        {
            foreach (var registration in Registrations)
            {
                try
                {
                    var callback = new WtsListenerCallback(registration.Key, registration.Value, Logger);
                    // keep a reference out of paranoia
                    Callbacks.Add(callback);
                    pChannelMgr.CreateListener(callback.ChannelName, 0, callback);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to create listener for '{Key}'", registration.Key);
                }
            }
        }

        void IWTSPlugin.Connected()
        {
            // no-op
        }

        void IWTSPlugin.Disconnected(uint dwDisconnectCode)
        {
            // no-op
        }

        void IWTSPlugin.Terminated()
        {
            // no-op
        }
    }
}
