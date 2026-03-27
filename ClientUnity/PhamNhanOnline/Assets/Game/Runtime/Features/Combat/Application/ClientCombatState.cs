using System;
using System.Collections.Generic;

namespace PhamNhanOnline.Client.Features.Combat.Application
{
    public sealed class ClientCombatState
    {
        private readonly Dictionary<long, SkillCooldownState> cooldownsByPlayerSkillId =
            new Dictionary<long, SkillCooldownState>();
        private readonly Dictionary<int, SkillCooldownState> cooldownsBySlotIndex =
            new Dictionary<int, SkillCooldownState>();

        public event Action Changed;
        public event Action<SkillCastStartedNotice> SkillCastStarted;
        public event Action<SkillImpactResolvedNotice> SkillImpactResolved;

        public bool HasPendingAttackRequest { get; private set; }
        public int PendingAttackSlotIndex { get; private set; }
        public LocalSkillCastState? ActiveLocalCast { get; private set; }

        public bool IsLocalCastActive(DateTime utcNow)
        {
            CleanupTransientState(utcNow);
            return ActiveLocalCast.HasValue && utcNow < ActiveLocalCast.Value.CastCompletedAtUtc;
        }

        public void MarkPendingAttackRequest(int slotIndex)
        {
            HasPendingAttackRequest = true;
            PendingAttackSlotIndex = Math.Max(0, slotIndex);
            NotifyChanged();
        }

        public void ClearPendingAttackRequest()
        {
            if (!HasPendingAttackRequest && PendingAttackSlotIndex <= 0)
                return;

            HasPendingAttackRequest = false;
            PendingAttackSlotIndex = 0;
            NotifyChanged();
        }

        public void ApplyAttackAccepted(
            int slotIndex,
            long playerSkillId,
            int cooldownMs,
            DateTime? cooldownEndsAtUtc,
            DateTime? castStartedAtUtc,
            DateTime? castCompletedAtUtc,
            DateTime? impactAtUtc)
        {
            ClearPendingAttackRequest();

            if (playerSkillId > 0 && cooldownMs > 0 && cooldownEndsAtUtc.HasValue)
            {
                cooldownsByPlayerSkillId[playerSkillId] = new SkillCooldownState(
                    playerSkillId,
                    Math.Max(0, cooldownMs),
                    cooldownEndsAtUtc.Value);
            }

            if (slotIndex > 0 && cooldownMs > 0 && cooldownEndsAtUtc.HasValue)
            {
                cooldownsBySlotIndex[slotIndex] = new SkillCooldownState(
                    playerSkillId,
                    Math.Max(0, cooldownMs),
                    cooldownEndsAtUtc.Value);
            }

            if (slotIndex > 0 && castStartedAtUtc.HasValue && castCompletedAtUtc.HasValue)
            {
                ActiveLocalCast = new LocalSkillCastState(
                    slotIndex,
                    playerSkillId,
                    castStartedAtUtc.Value,
                    castCompletedAtUtc.Value,
                    impactAtUtc ?? castCompletedAtUtc.Value);
            }
            else
            {
                ActiveLocalCast = null;
            }

            NotifyChanged();
        }

        public void ApplyAttackRejected(
            int slotIndex,
            long playerSkillId,
            int cooldownMs,
            DateTime? cooldownEndsAtUtc)
        {
            ClearPendingAttackRequest();

            if (playerSkillId > 0 && cooldownMs > 0 && cooldownEndsAtUtc.HasValue)
            {
                cooldownsByPlayerSkillId[playerSkillId] = new SkillCooldownState(
                    playerSkillId,
                    Math.Max(0, cooldownMs),
                    cooldownEndsAtUtc.Value);
            }

            if (slotIndex > 0 && cooldownMs > 0 && cooldownEndsAtUtc.HasValue)
            {
                cooldownsBySlotIndex[slotIndex] = new SkillCooldownState(
                    playerSkillId,
                    Math.Max(0, cooldownMs),
                    cooldownEndsAtUtc.Value);
            }

            NotifyChanged();
        }

        public void ApplyLocalCastStarted(
            int slotIndex,
            long playerSkillId,
            DateTime? castStartedAtUtc,
            DateTime? castCompletedAtUtc,
            DateTime? impactAtUtc)
        {
            if (slotIndex <= 0 || !castStartedAtUtc.HasValue || !castCompletedAtUtc.HasValue)
                return;

            ActiveLocalCast = new LocalSkillCastState(
                slotIndex,
                playerSkillId,
                castStartedAtUtc.Value,
                castCompletedAtUtc.Value,
                impactAtUtc ?? castCompletedAtUtc.Value);
            NotifyChanged();
        }

        public void ClearActiveCast()
        {
            if (!ActiveLocalCast.HasValue)
                return;

            ActiveLocalCast = null;
            NotifyChanged();
        }

        public bool TryGetCooldown(
            long playerSkillId,
            DateTime utcNow,
            out float fillAmount,
            out int remainingMs,
            out int durationMs)
        {
            CleanupTransientState(utcNow);

            SkillCooldownState cooldown;
            if (playerSkillId <= 0 || !cooldownsByPlayerSkillId.TryGetValue(playerSkillId, out cooldown))
            {
                fillAmount = 0f;
                remainingMs = 0;
                durationMs = 0;
                return false;
            }

            var remaining = cooldown.EndsAtUtc - utcNow;
            if (remaining <= TimeSpan.Zero)
            {
                cooldownsByPlayerSkillId.Remove(playerSkillId);
                fillAmount = 0f;
                remainingMs = 0;
                durationMs = 0;
                return false;
            }

            durationMs = Math.Max(1, cooldown.DurationMs);
            remainingMs = (int)Math.Ceiling(remaining.TotalMilliseconds);
            fillAmount = UnityEngine.Mathf.Clamp01((float)remainingMs / durationMs);
            return true;
        }

        public bool TryGetCooldownForSlot(
            int slotIndex,
            long playerSkillId,
            DateTime utcNow,
            out float fillAmount,
            out int remainingMs,
            out int durationMs)
        {
            if (TryGetCooldown(playerSkillId, utcNow, out fillAmount, out remainingMs, out durationMs))
                return true;

            CleanupTransientState(utcNow);

            SkillCooldownState cooldown;
            if (slotIndex <= 0 || !cooldownsBySlotIndex.TryGetValue(slotIndex, out cooldown))
            {
                fillAmount = 0f;
                remainingMs = 0;
                durationMs = 0;
                return false;
            }

            var remaining = cooldown.EndsAtUtc - utcNow;
            if (remaining <= TimeSpan.Zero)
            {
                cooldownsBySlotIndex.Remove(slotIndex);
                fillAmount = 0f;
                remainingMs = 0;
                durationMs = 0;
                return false;
            }

            durationMs = Math.Max(1, cooldown.DurationMs);
            remainingMs = (int)Math.Ceiling(remaining.TotalMilliseconds);
            fillAmount = UnityEngine.Mathf.Clamp01((float)remainingMs / durationMs);
            return true;
        }

        public void Clear()
        {
            HasPendingAttackRequest = false;
            PendingAttackSlotIndex = 0;
            ActiveLocalCast = null;
            cooldownsByPlayerSkillId.Clear();
            cooldownsBySlotIndex.Clear();
            NotifyChanged();
        }

        public void PublishSkillCastStarted(SkillCastStartedNotice notice)
        {
            var handler = SkillCastStarted;
            if (handler != null)
                handler(notice);
        }

        public void PublishSkillImpactResolved(SkillImpactResolvedNotice notice)
        {
            var handler = SkillImpactResolved;
            if (handler != null)
                handler(notice);
        }

        private void CleanupTransientState(DateTime utcNow)
        {
            var changed = false;

            if (ActiveLocalCast.HasValue && utcNow >= ActiveLocalCast.Value.CastCompletedAtUtc)
            {
                ActiveLocalCast = null;
                changed = true;
            }

            if (cooldownsByPlayerSkillId.Count > 0)
            {
                var expiredKeys = new List<long>();
                foreach (var pair in cooldownsByPlayerSkillId)
                {
                    if (utcNow >= pair.Value.EndsAtUtc)
                        expiredKeys.Add(pair.Key);
                }

                if (expiredKeys.Count > 0)
                {
                    for (var i = 0; i < expiredKeys.Count; i++)
                        cooldownsByPlayerSkillId.Remove(expiredKeys[i]);

                    changed = true;
                }
            }

            if (cooldownsBySlotIndex.Count > 0)
            {
                var expiredSlotKeys = new List<int>();
                foreach (var pair in cooldownsBySlotIndex)
                {
                    if (utcNow >= pair.Value.EndsAtUtc)
                        expiredSlotKeys.Add(pair.Key);
                }

                if (expiredSlotKeys.Count > 0)
                {
                    for (var i = 0; i < expiredSlotKeys.Count; i++)
                        cooldownsBySlotIndex.Remove(expiredSlotKeys[i]);

                    changed = true;
                }
            }

            if (changed)
                NotifyChanged();
        }

        private void NotifyChanged()
        {
            var handler = Changed;
            if (handler != null)
                handler();
        }
    }

    public readonly struct SkillCooldownState
    {
        public SkillCooldownState(long playerSkillId, int durationMs, DateTime endsAtUtc)
        {
            PlayerSkillId = playerSkillId;
            DurationMs = durationMs;
            EndsAtUtc = endsAtUtc;
        }

        public long PlayerSkillId { get; }
        public int DurationMs { get; }
        public DateTime EndsAtUtc { get; }
    }

    public readonly struct LocalSkillCastState
    {
        public LocalSkillCastState(
            int slotIndex,
            long playerSkillId,
            DateTime castStartedAtUtc,
            DateTime castCompletedAtUtc,
            DateTime impactAtUtc)
        {
            SlotIndex = slotIndex;
            PlayerSkillId = playerSkillId;
            CastStartedAtUtc = castStartedAtUtc;
            CastCompletedAtUtc = castCompletedAtUtc;
            ImpactAtUtc = impactAtUtc;
        }

        public int SlotIndex { get; }
        public long PlayerSkillId { get; }
        public DateTime CastStartedAtUtc { get; }
        public DateTime CastCompletedAtUtc { get; }
        public DateTime ImpactAtUtc { get; }
    }
}
