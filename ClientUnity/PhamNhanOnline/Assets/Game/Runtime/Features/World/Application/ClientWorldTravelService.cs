using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;
using System.Collections.Generic;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientWorldTravelService
    {
        private readonly ClientConnectionService connection;
        private TaskCompletionSource<WorldTravelResult> travelCompletionSource;
        private TaskCompletionSource<MapZonesQueryResult> mapZonesCompletionSource;
        private TaskCompletionSource<MapZoneSwitchResult> switchZoneCompletionSource;

        public ClientWorldTravelService(ClientConnectionService connection)
        {
            this.connection = connection;
            connection.Packets.Subscribe<TravelToMapResultPacket>(HandleTravelToMapResult);
            connection.Packets.Subscribe<GetMapZonesResultPacket>(HandleGetMapZonesResult);
            connection.Packets.Subscribe<SwitchMapZoneResultPacket>(HandleSwitchMapZoneResult);
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

        public Task<MapZonesQueryResult> GetMapZonesAsync(int mapId)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new MapZonesQueryResult(false, null, mapId, null, null, false, null, "Not connected to server."));

            mapZonesCompletionSource = new TaskCompletionSource<MapZonesQueryResult>();
            connection.Send(new GetMapZonesPacket
            {
                MapId = mapId
            });
            return mapZonesCompletionSource.Task;
        }

        public Task<MapZoneSwitchResult> SwitchMapZoneAsync(int mapId, int zoneIndex)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new MapZoneSwitchResult(false, null, mapId, zoneIndex, null, "Not connected to server."));

            switchZoneCompletionSource = new TaskCompletionSource<MapZoneSwitchResult>();
            connection.Send(new SwitchMapZonePacket
            {
                MapId = mapId,
                TargetZoneIndex = zoneIndex
            });
            return switchZoneCompletionSource.Task;
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

        private void HandleGetMapZonesResult(GetMapZonesResultPacket packet)
        {
            var mapId = packet.MapId;
            var zones = packet.Zones ?? new List<MapZoneSummaryModel>();
            var result = new MapZonesQueryResult(
                packet.Success == true,
                packet.Code,
                mapId,
                packet.CurrentZoneIndex,
                packet.MaxZoneCount,
                packet.SupportsCavePlacement == true,
                zones,
                packet.Success == true
                    ? $"Loaded zones for map {mapId}."
                    : $"Failed to load zones for map {mapId}: {packet.Code ?? MessageCode.UnknownError}");

            if (packet.Success == true)
                ClientLog.Info(result.Message);
            else
                ClientLog.Warn(result.Message);

            CompletePending(ref mapZonesCompletionSource, result);
        }

        private void HandleSwitchMapZoneResult(SwitchMapZoneResultPacket packet)
        {
            var result = new MapZoneSwitchResult(
                packet.Success == true,
                packet.Code,
                packet.MapId,
                packet.ZoneIndex,
                packet.Zone,
                packet.Success == true
                    ? $"Switched to zone {packet.ZoneIndex} on map {packet.MapId}."
                    : $"Failed to switch zone on map {packet.MapId}: {packet.Code ?? MessageCode.UnknownError}");

            if (packet.Success == true)
                ClientLog.Info(result.Message);
            else
                ClientLog.Warn(result.Message);

            CompletePending(ref switchZoneCompletionSource, result);
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            CompletePending(ref travelCompletionSource, new WorldTravelResult(false, null, null, "Connection closed."));
            CompletePending(ref mapZonesCompletionSource, new MapZonesQueryResult(false, null, null, null, null, false, null, "Connection closed."));
            CompletePending(ref switchZoneCompletionSource, new MapZoneSwitchResult(false, null, null, null, null, "Connection closed."));
        }

        private static void CompletePending(ref TaskCompletionSource<WorldTravelResult> completionSource, WorldTravelResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<MapZonesQueryResult> completionSource, MapZonesQueryResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<MapZoneSwitchResult> completionSource, MapZoneSwitchResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
