using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Packets;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientWorldTravelService
    {
        private readonly ClientConnectionService connection;
        private TaskCompletionSource<WorldTravelResult> travelCompletionSource;

        public ClientWorldTravelService(ClientConnectionService connection)
        {
            this.connection = connection;
            connection.Packets.Subscribe<TravelToMapResultPacket>(HandleTravelToMapResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<WorldTravelResult> TravelToMapAsync(int targetMapId)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new WorldTravelResult(false, null, targetMapId, "Not connected to server."));

            travelCompletionSource = new TaskCompletionSource<WorldTravelResult>();
            connection.Send(new TravelToMapPacket
            {
                TargetMapId = targetMapId
            });
            return travelCompletionSource.Task;
        }

        private void HandleTravelToMapResult(TravelToMapResultPacket packet)
        {
            var result = new WorldTravelResult(
                packet.Success == true,
                packet.Code,
                packet.TargetMapId,
                packet.Success == true
                    ? $"Travelled to map {packet.TargetMapId}."
                    : $"Failed to travel to map {packet.TargetMapId}: {packet.Code ?? MessageCode.UnknownError}");

            if (packet.Success == true)
                ClientLog.Info(result.Message);
            else
                ClientLog.Warn(result.Message);

            CompletePending(ref travelCompletionSource, result);
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            CompletePending(ref travelCompletionSource, new WorldTravelResult(false, null, null, "Connection closed."));
        }

        private static void CompletePending(ref TaskCompletionSource<WorldTravelResult> completionSource, WorldTravelResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
