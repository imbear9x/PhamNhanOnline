using System;
using System.Globalization;

namespace PhamNhanOnline.Client.Features.Targeting.Application
{
    public enum TargetPinMode
    {
        None = 0,
        Manual = 1,
        CombatLocked = 2
    }

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
        public event Action PinStateChanged;

        public WorldTargetHandle? CurrentTarget { get; private set; }
        public TargetPinMode PinMode { get; private set; }
        public bool HasPinnedTarget { get { return PinMode != TargetPinMode.None && CurrentTarget.HasValue; } }
        public bool IsManualSelection { get; private set; }

        public void Select(WorldTargetHandle handle)
        {
            if (!handle.IsValid)
            {
                Clear();
                return;
            }

            if (CurrentTarget.HasValue && CurrentTarget.Value.Equals(handle))
                return;

            var pinChanged = PinMode != TargetPinMode.None;
            CurrentTarget = handle;
            PinMode = TargetPinMode.None;
            IsManualSelection = true;
            NotifyChanged();
            if (pinChanged)
                NotifyPinChanged();
        }

        public void SelectAuto(WorldTargetHandle handle)
        {
            if (!handle.IsValid)
                return;

            if (HasPinnedTarget)
                return;

            if (CurrentTarget.HasValue && CurrentTarget.Value.Equals(handle))
                return;

            CurrentTarget = handle;
            IsManualSelection = false;
            NotifyChanged();
        }

        public void Clear()
        {
            if (!CurrentTarget.HasValue && PinMode == TargetPinMode.None)
                return;

            CurrentTarget = null;
            var pinChanged = PinMode != TargetPinMode.None;
            PinMode = TargetPinMode.None;
            IsManualSelection = false;
            NotifyChanged();
            if (pinChanged)
                NotifyPinChanged();
        }

        public bool PinCurrent(TargetPinMode pinMode)
        {
            if (!CurrentTarget.HasValue || pinMode == TargetPinMode.None)
                return false;

            if (PinMode == pinMode)
                return true;

            PinMode = pinMode;
            NotifyPinChanged();
            return true;
        }

        public bool ClearPin()
        {
            if (PinMode == TargetPinMode.None)
                return false;

            PinMode = TargetPinMode.None;
            NotifyPinChanged();
            return true;
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

        private void NotifyPinChanged()
        {
            var handler = PinStateChanged;
            if (handler != null)
                handler();
        }
    }
}
