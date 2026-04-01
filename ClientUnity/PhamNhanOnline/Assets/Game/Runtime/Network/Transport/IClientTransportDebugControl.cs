using System;

namespace PhamNhanOnline.Client.Network.Transport
{
    public interface IClientTransportDebugControl
    {
        bool IsDebugNetworkBlocked { get; }
        float DebugNetworkBlockRemainingSeconds { get; }

        void BlockNetwork(TimeSpan? duration = null);
        void UnblockNetwork();
    }
}
