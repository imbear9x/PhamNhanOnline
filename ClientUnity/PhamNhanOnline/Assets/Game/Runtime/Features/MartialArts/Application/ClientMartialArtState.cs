using System;
using System.Linq;
using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.MartialArts.Application
{
    public sealed class ClientMartialArtState
    {
        public event Action Changed;

        public bool HasLoadedMartialArts { get; private set; }
        public bool IsLoading { get; private set; }
        public MessageCode? LastResultCode { get; private set; }
        public string LastStatusMessage { get; private set; } = string.Empty;
        public DateTime? LastLoadedAtUtc { get; private set; }
        public int? ActiveMartialArtId { get; private set; }
        public PlayerMartialArtModel[] OwnedMartialArts { get; private set; } = Array.Empty<PlayerMartialArtModel>();
        public CultivationPreviewModel? CultivationPreview { get; private set; }

        public void BeginLoading()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            NotifyChanged();
        }

        public void ApplyOwnedMartialArts(
            PlayerMartialArtModel[] martialArts,
            int? activeMartialArtId,
            CultivationPreviewModel? cultivationPreview,
            MessageCode? code,
            string statusMessage)
        {
            HasLoadedMartialArts = true;
            IsLoading = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastLoadedAtUtc = DateTime.UtcNow;
            ActiveMartialArtId = NormalizeActiveMartialArtId(activeMartialArtId);
            CultivationPreview = cultivationPreview;
            OwnedMartialArts = NormalizeMartialArts(martialArts, ActiveMartialArtId);
            NotifyChanged();
        }

        public void ApplyActiveMartialArt(
            int? activeMartialArtId,
            CultivationPreviewModel? cultivationPreview,
            MessageCode? code,
            string statusMessage)
        {
            ActiveMartialArtId = NormalizeActiveMartialArtId(activeMartialArtId);
            CultivationPreview = cultivationPreview;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            OwnedMartialArts = NormalizeMartialArts(OwnedMartialArts, ActiveMartialArtId);
            NotifyChanged();
        }

        public void AppendLearnedMartialArt(
            PlayerMartialArtModel learnedMartialArt,
            int? activeMartialArtId,
            CultivationPreviewModel? cultivationPreview,
            MessageCode? code,
            string statusMessage)
        {
            HasLoadedMartialArts = true;
            IsLoading = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastLoadedAtUtc = DateTime.UtcNow;
            ActiveMartialArtId = NormalizeActiveMartialArtId(activeMartialArtId);
            CultivationPreview = cultivationPreview;

            OwnedMartialArts = OwnedMartialArts
                .Where(existing => existing.MartialArtId != learnedMartialArt.MartialArtId)
                .Append(learnedMartialArt)
                .OrderBy(existing => existing.MartialArtId)
                .ToArray();

            OwnedMartialArts = NormalizeMartialArts(OwnedMartialArts, ActiveMartialArtId);
            NotifyChanged();
        }

        public void ApplyFailure(MessageCode? code, string statusMessage)
        {
            IsLoading = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            NotifyChanged();
        }

        public bool TryGetActiveMartialArt(out PlayerMartialArtModel martialArt)
        {
            if (ActiveMartialArtId.HasValue)
            {
                for (var i = 0; i < OwnedMartialArts.Length; i++)
                {
                    if (OwnedMartialArts[i].MartialArtId != ActiveMartialArtId.Value)
                        continue;

                    martialArt = OwnedMartialArts[i];
                    return true;
                }
            }

            martialArt = default(PlayerMartialArtModel);
            return false;
        }

        public void Clear()
        {
            HasLoadedMartialArts = false;
            IsLoading = false;
            LastResultCode = null;
            LastStatusMessage = string.Empty;
            LastLoadedAtUtc = null;
            ActiveMartialArtId = null;
            CultivationPreview = null;
            OwnedMartialArts = Array.Empty<PlayerMartialArtModel>();
            NotifyChanged();
        }

        private static PlayerMartialArtModel[] NormalizeMartialArts(
            PlayerMartialArtModel[] martialArts,
            int? activeMartialArtId)
        {
            if (martialArts == null || martialArts.Length == 0)
                return Array.Empty<PlayerMartialArtModel>();

            var normalized = new PlayerMartialArtModel[martialArts.Length];
            for (var i = 0; i < martialArts.Length; i++)
            {
                var martialArt = martialArts[i];
                martialArt.IsActive = activeMartialArtId.HasValue && martialArt.MartialArtId == activeMartialArtId.Value;
                normalized[i] = martialArt;
            }

            return normalized;
        }

        private static int? NormalizeActiveMartialArtId(int? activeMartialArtId)
        {
            return activeMartialArtId.HasValue && activeMartialArtId.Value > 0
                ? activeMartialArtId
                : null;
        }

        private void NotifyChanged()
        {
            var handler = Changed;
            if (handler != null)
                handler();
        }
    }
}
