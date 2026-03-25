using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.MartialArts.Application
{
    public readonly struct MartialArtSetActiveResult
    {
        public MartialArtSetActiveResult(
            bool success,
            MessageCode? code,
            CharacterBaseStatsModel? baseStats,
            CultivationPreviewModel? cultivationPreview,
            string message)
        {
            Success = success;
            Code = code;
            BaseStats = baseStats;
            CultivationPreview = cultivationPreview;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public CharacterBaseStatsModel? BaseStats { get; }
        public CultivationPreviewModel? CultivationPreview { get; }
        public string Message { get; }
    }
}
