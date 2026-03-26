using System;
using System.Collections.Generic;

namespace PhamNhanOnline.Client.Features.Combat.Application
{
    public sealed class ClientCombatState
    {
        private readonly Dictionary<long, SkillCooldownState> cooldownsByPlayerSkillId =
            new Dictionary<long, SkillCooldownState>();

        public event Action Changed;

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

        public void Clear()
        {
            HasPendingAttackRequest = false;
            PendingAttackSlotIndex = 0;
            ActiveLocalCast = null;
            cooldownsByPlayerSkillId.Clear();
            NotifyChanged();
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
