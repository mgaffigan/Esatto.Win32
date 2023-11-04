using Esatto.Win32.RdpDvc.TSVirtualChannels;
using Microsoft.Extensions.Logging;

namespace Esatto.Win32.RdpDvc.ClientPluginApi;

public abstract class WtsPluginBase : IWTSPlugin
{
    void IWTSPlugin.Initialize(IWTSVirtualChannelManager pChannelMgr) => Initialize(pChannelMgr);
    protected abstract void Initialize(IWTSVirtualChannelManager pChannelMgr);

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
