using GameShared.Messages;
using GameShared.Models;
using System.Collections.Generic;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public readonly struct WorldTravelResult
    {
        public WorldTravelResult(bool success, MessageCode? code, int? targetMapId, string message)
        {
            Success = success;
            Code = code;
            TargetMapId = targetMapId;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public int? TargetMapId { get; }
        public string Message { get; }
    }

    public readonly struct MapZonesQueryResult
    {
        public MapZonesQueryResult(
            bool success,
            MessageCode? code,
            int? mapId,
            int? currentZoneIndex,
            int? maxZoneCount,
            bool supportsCavePlacement,
            IReadOnlyList<MapZoneSummaryModel> zones,
            string message)
        {
            Success = success;
            Code = code;
            MapId = mapId;
            CurrentZoneIndex = currentZoneIndex;
            MaxZoneCount = maxZoneCount;
            SupportsCavePlacement = supportsCavePlacement;
            Zones = zones ?? new List<MapZoneSummaryModel>();
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public int? MapId { get; }
        public int? CurrentZoneIndex { get; }
        public int? MaxZoneCount { get; }
        public bool SupportsCavePlacement { get; }
        public IReadOnlyList<MapZoneSummaryModel> Zones { get; }
        public string Message { get; }
    }

    public readonly struct MapZoneSwitchResult
    {
        public MapZoneSwitchResult(bool success, MessageCode? code, int? mapId, int? zoneIndex, MapZoneDetailModel? zone, string message)
        {
            Success = success;
            Code = code;
            MapId = mapId;
            ZoneIndex = zoneIndex;
            Zone = zone;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public int? MapId { get; }
        public int? ZoneIndex { get; }
        public MapZoneDetailModel? Zone { get; }
        public string Message { get; }
    }
}
