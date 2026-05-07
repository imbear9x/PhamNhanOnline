using System;
using System.Threading;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;
using System.Collections.Generic;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientWorldTravelService
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        private readonly ClientConnectionService connection;
        private readonly ClientRequestTracker<WorldTravelResult> travelRequests;
        private readonly ClientRequestTracker<MapZonesQueryResult> mapZoneRequests;
        private readonly ClientRequestTracker<MapZoneSwitchResult> switchZoneRequests;

        public ClientWorldTravelService(ClientConnectionService connection)
        {
            this.connection = connection;
            travelRequests = new ClientRequestTracker<WorldTravelResult>("world travel", RequestTimeout);
            mapZoneRequests = new ClientRequestTracker<MapZonesQueryResult>("map zones query", RequestTimeout);
            switchZoneRequests = new ClientRequestTracker<MapZoneSwitchResult>("map zone switch", RequestTimeout);
            connection.Packets.Subscribe<TravelToMapResultPacket>(HandleTravelToMapResult);
            connection.Packets.Subscribe<GetMapZonesResultPacket>(HandleGetMapZonesResult);
            connection.Packets.Subscribe<SwitchMapZoneResultPacket>(HandleSwitchMapZoneResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<WorldTravelResult> TravelToMapAsync(int targetMapId)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new WorldTravelResult(false, null, targetMapId, null, null, "Not connected to server."));

            var task = travelRequests.Start(
                message => new WorldTravelResult(false, null, targetMapId, null, null, message));
            try
            {
                connection.Send(new TravelToMapPacket
                {
                    TargetMapId = targetMapId
                });
            }
            catch (Exception ex)
            {
                travelRequests.FailActive($"Failed to send travel request to map {targetMapId}: {ex.Message}");
            }

            return task;
        }

        public Task<WorldTravelResult> UsePortalAsync(int portalId, Vector2? currentServerPosition = null)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new WorldTravelResult(false, null, null, portalId, null, "Not connected to server."));

            var task = travelRequests.Start(
                message => new WorldTravelResult(false, null, null, portalId, null, message));
            var packet = new TravelToMapPacket
            {
                PortalId = portalId
            };
            if (currentServerPosition.HasValue)
            {
                packet.CurrentPosX = currentServerPosition.Value.x;
                packet.CurrentPosY = currentServerPosition.Value.y;
            }

            try
            {
                connection.Send(packet);
            }
            catch (Exception ex)
            {
                travelRequests.FailActive($"Failed to send portal request {portalId}: {ex.Message}");
            }

            return task;
        }

        public Task<MapZonesQueryResult> GetMapZonesAsync(int mapId)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new MapZonesQueryResult(false, null, mapId, null, null, false, null, "Not connected to server."));

            var task = mapZoneRequests.Start(
                message => new MapZonesQueryResult(false, null, mapId, null, null, false, null, message));
            try
            {
                connection.Send(new GetMapZonesPacket
                {
                    MapId = mapId
                });
            }
            catch (Exception ex)
            {
                mapZoneRequests.FailActive($"Failed to send map zones request for map {mapId}: {ex.Message}");
            }

            return task;
        }

        public Task<MapZoneSwitchResult> SwitchMapZoneAsync(int mapId, int zoneIndex)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new MapZoneSwitchResult(false, null, mapId, zoneIndex, null, "Not connected to server."));

            var task = switchZoneRequests.Start(
                message => new MapZoneSwitchResult(false, null, mapId, zoneIndex, null, message));
            try
            {
                connection.Send(new SwitchMapZonePacket
                {
                    MapId = mapId,
                    TargetZoneIndex = zoneIndex
                });
            }
            catch (Exception ex)
            {
                switchZoneRequests.FailActive($"Failed to send map zone switch request for map {mapId}, zone {zoneIndex}: {ex.Message}");
            }

            return task;
        }

        private void HandleTravelToMapResult(TravelToMapResultPacket packet)
        {
            var result = new WorldTravelResult(
                packet.Success == true,
                packet.Code,
                packet.TargetMapId,
                packet.PortalId,
                packet.TargetSpawnPointId,
                packet.Success == true
                    ? packet.PortalId.HasValue
                        ? $"Used portal {packet.PortalId} to map {packet.TargetMapId} spawn {packet.TargetSpawnPointId}."
                        : $"Travelled to map {packet.TargetMapId}."
                    : packet.PortalId.HasValue
                        ? $"Failed to use portal {packet.PortalId}: {packet.Code ?? MessageCode.UnknownError}"
                        : $"Failed to travel to map {packet.TargetMapId}: {packet.Code ?? MessageCode.UnknownError}");

            if (packet.Success == true)
                ClientLog.Info(result.Message);
            else
                ClientLog.Warn(result.Message);

            travelRequests.Complete(result);
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
            {
                ClientLog.Info(result.Message);
            }
            else if (packet.Code != MessageCode.MapZoneSelectionNotSupported)
            {
                ClientLog.Warn(result.Message);
            }

            mapZoneRequests.Complete(result);
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

            switchZoneRequests.Complete(result);
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            travelRequests.FailActive("Connection closed.");
            mapZoneRequests.FailActive("Connection closed.");
            switchZoneRequests.FailActive("Connection closed.");
        }

        private sealed class ClientRequestTracker<TResult>
        {
            private readonly string operationName;
            private readonly TimeSpan timeout;
            private TaskCompletionSource<TResult> completionSource;
            private CancellationTokenSource timeoutCancellation;
            private Func<string, TResult> activeFailureFactory;
            private int requestVersion;

            public ClientRequestTracker(string operationName, TimeSpan timeout)
            {
                this.operationName = operationName;
                this.timeout = timeout;
            }

            public Task<TResult> Start(Func<string, TResult> failureFactory)
            {
                if (failureFactory == null)
                    throw new ArgumentNullException(nameof(failureFactory));

                if (completionSource != null && activeFailureFactory != null)
                    Complete(activeFailureFactory($"Cancelled {operationName}: superseded by a newer request."));

                requestVersion++;
                activeFailureFactory = failureFactory;
                timeoutCancellation = new CancellationTokenSource();
                completionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = CompleteAfterTimeoutAsync(requestVersion, timeoutCancellation.Token);
                return completionSource.Task;
            }

            public void Complete(TResult result)
            {
                var pending = completionSource;
                var cancellation = timeoutCancellation;
                completionSource = null;
                timeoutCancellation = null;
                activeFailureFactory = null;
                cancellation?.Cancel();
                cancellation?.Dispose();
                pending?.TrySetResult(result);
            }

            public void FailActive(string message)
            {
                if (completionSource == null || activeFailureFactory == null)
                    return;

                Complete(activeFailureFactory(message));
            }

            private async Task CompleteAfterTimeoutAsync(int version, CancellationToken cancellationToken)
            {
                try
                {
                    await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (version != requestVersion || completionSource == null || activeFailureFactory == null)
                    return;

                var message = $"Timed out waiting for {operationName} response after {timeout.TotalSeconds:0.#}s.";
                ClientLog.Warn(message);
                Complete(activeFailureFactory(message));
            }
        }
    }
}
