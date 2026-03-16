using GameShared.Messages;

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
}
