using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public readonly struct CharacterBaseStatsChangeNotice
    {
        public CharacterBaseStatsChangeNotice(
            CharacterBaseStatsModel? previousBaseStats,
            CharacterBaseStatsModel? baseStats)
        {
            PreviousBaseStats = previousBaseStats;
            BaseStats = baseStats;
        }

        public CharacterBaseStatsModel? PreviousBaseStats { get; }
        public CharacterBaseStatsModel? BaseStats { get; }
    }
}
