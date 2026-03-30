using System;
using System.Collections.Generic;
using PhamNhanOnline.Client.Features.Targeting.Application;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    internal static class CharacterSkillPresenterRegistry
    {
        private static readonly Dictionary<Guid, CharacterSkillPresenter> ByCharacterId =
            new Dictionary<Guid, CharacterSkillPresenter>();

        private static readonly Dictionary<string, CharacterSkillPresenter> ByTargetHandleKey =
            new Dictionary<string, CharacterSkillPresenter>(StringComparer.Ordinal);

        public static void Register(CharacterSkillPresenter presenter)
        {
            if (presenter == null)
                return;

            if (presenter.CharacterId.HasValue)
                ByCharacterId[presenter.CharacterId.Value] = presenter;

            if (presenter.TargetHandle.HasValue && presenter.TargetHandle.Value.IsValid)
                ByTargetHandleKey[BuildHandleKey(presenter.TargetHandle.Value)] = presenter;
        }

        public static void Unregister(CharacterSkillPresenter presenter)
        {
            if (presenter == null)
                return;

            if (presenter.CharacterId.HasValue)
            {
                CharacterSkillPresenter current;
                if (ByCharacterId.TryGetValue(presenter.CharacterId.Value, out current) && ReferenceEquals(current, presenter))
                    ByCharacterId.Remove(presenter.CharacterId.Value);
            }

            if (presenter.TargetHandle.HasValue && presenter.TargetHandle.Value.IsValid)
            {
                var key = BuildHandleKey(presenter.TargetHandle.Value);
                CharacterSkillPresenter current;
                if (ByTargetHandleKey.TryGetValue(key, out current) && ReferenceEquals(current, presenter))
                    ByTargetHandleKey.Remove(key);
            }
        }

        public static bool TryGetByCharacterId(Guid characterId, out CharacterSkillPresenter presenter)
        {
            return ByCharacterId.TryGetValue(characterId, out presenter) && presenter != null;
        }

        public static bool TryGetByTargetHandle(WorldTargetHandle handle, out CharacterSkillPresenter presenter)
        {
            presenter = null;
            return handle.IsValid &&
                   ByTargetHandleKey.TryGetValue(BuildHandleKey(handle), out presenter) &&
                   presenter != null;
        }

        private static string BuildHandleKey(WorldTargetHandle handle)
        {
            return string.Concat((int)handle.Kind, ":", handle.TargetId ?? string.Empty);
        }
    }
}
