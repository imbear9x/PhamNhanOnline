using System;
using System.Globalization;

namespace PhamNhanOnline.Client.Features.Targeting.Application
{
    public enum WorldTargetKind
    {
        None = 0,
        Player = 1,
        Enemy = 2,
        Boss = 3,
        Npc = 4
    }

    public readonly struct WorldTargetHandle : IEquatable<WorldTargetHandle>
    {
        public WorldTargetHandle(WorldTargetKind kind, string targetId)
        {
            Kind = kind;
            TargetId = targetId ?? string.Empty;
        }

        public WorldTargetKind Kind { get; }
        public string TargetId { get; }
        public bool IsValid { get { return Kind != WorldTargetKind.None && !string.IsNullOrWhiteSpace(TargetId); } }

        public static WorldTargetHandle CreateObservedCharacter(Guid characterId)
        {
            return new WorldTargetHandle(WorldTargetKind.Player, characterId.ToString("D"));
        }

        public static WorldTargetHandle CreateEnemy(int runtimeId, bool isBoss = false)
        {
            return new WorldTargetHandle(
                isBoss ? WorldTargetKind.Boss : WorldTargetKind.Enemy,
                runtimeId.ToString(CultureInfo.InvariantCulture));
        }

        public bool Equals(WorldTargetHandle other)
        {
            return Kind == other.Kind &&
                   string.Equals(TargetId, other.TargetId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldTargetHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ (TargetId != null ? StringComparer.Ordinal.GetHashCode(TargetId) : 0);
            }
        }
    }

    public readonly struct WorldTargetSnapshot
    {
        public WorldTargetSnapshot(
            WorldTargetKind kind,
            string targetId,
            string displayName,
            int primaryCurrentValue,
            int primaryMaxValue,
            bool hasPrimaryResource,
            int secondaryCurrentValue,
            int secondaryMaxValue,
            bool hasSecondaryResource,
            bool isDead)
        {
            Kind = kind;
            TargetId = targetId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            PrimaryCurrentValue = primaryCurrentValue;
            PrimaryMaxValue = primaryMaxValue;
            HasPrimaryResource = hasPrimaryResource;
            SecondaryCurrentValue = secondaryCurrentValue;
            SecondaryMaxValue = secondaryMaxValue;
            HasSecondaryResource = hasSecondaryResource;
            IsDead = isDead;
        }

        public WorldTargetKind Kind { get; }
        public string TargetId { get; }
        public string DisplayName { get; }
        public int PrimaryCurrentValue { get; }
        public int PrimaryMaxValue { get; }
        public bool HasPrimaryResource { get; }
        public int SecondaryCurrentValue { get; }
        public int SecondaryMaxValue { get; }
        public bool HasSecondaryResource { get; }
        public bool IsDead { get; }
    }

    public sealed class ClientTargetState
    {
        public event Action CurrentTargetChanged;

        public WorldTargetHandle? CurrentTarget { get; private set; }

        public void Select(WorldTargetHandle handle)
        {
            if (!handle.IsValid)
            {
                Clear();
                return;
            }

            if (CurrentTarget.HasValue && CurrentTarget.Value.Equals(handle))
                return;

            CurrentTarget = handle;
            NotifyChanged();
        }

        public void Clear()
        {
            if (!CurrentTarget.HasValue)
                return;

            CurrentTarget = null;
            NotifyChanged();
        }

        public bool IsSelectedObservedCharacter(Guid characterId)
        {
            return CurrentTarget.HasValue &&
                   CurrentTarget.Value.Kind == WorldTargetKind.Player &&
                   string.Equals(CurrentTarget.Value.TargetId, characterId.ToString("D"), StringComparison.Ordinal);
        }

        public bool IsSelectedEnemy(int runtimeId)
        {
            if (!CurrentTarget.HasValue)
                return false;

            var kind = CurrentTarget.Value.Kind;
            if (kind != WorldTargetKind.Enemy && kind != WorldTargetKind.Boss)
                return false;

            return string.Equals(
                CurrentTarget.Value.TargetId,
                runtimeId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
        }

        private void NotifyChanged()
        {
            var handler = CurrentTargetChanged;
            if (handler != null)
                handler();
        }
    }
}
