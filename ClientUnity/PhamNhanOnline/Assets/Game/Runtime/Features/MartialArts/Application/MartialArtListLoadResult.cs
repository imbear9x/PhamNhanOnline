using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.MartialArts.Application
{
    public readonly struct MartialArtListLoadResult
    {
        public MartialArtListLoadResult(
            bool success,
            MessageCode? code,
            PlayerMartialArtModel[] martialArts,
            CultivationPreviewModel? cultivationPreview,
            string message,
            bool fromCache)
        {
            Success = success;
            Code = code;
            MartialArts = martialArts ?? System.Array.Empty<PlayerMartialArtModel>();
            CultivationPreview = cultivationPreview;
            Message = message ?? string.Empty;
            FromCache = fromCache;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public PlayerMartialArtModel[] MartialArts { get; }
        public CultivationPreviewModel? CultivationPreview { get; }
        public string Message { get; }
        public bool FromCache { get; }
    }
}
