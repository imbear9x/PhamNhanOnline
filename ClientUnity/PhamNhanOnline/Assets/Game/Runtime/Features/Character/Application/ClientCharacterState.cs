using System;
using System.Linq;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public sealed class ClientCharacterState
    {
        public event Action<CultivationRewardNotice> CultivationRewardGranted;
        public event Action<CharacterCurrentStateChangeNotice> CurrentStateChanged;
        public event Action<CharacterStateTransitionNotice> StateTransitioned;

        public Guid? SelectedCharacterId { get; private set; }
        public bool HasLoadedCharacterList { get; private set; }
        public CharacterModel[] CharacterList { get; private set; } = Array.Empty<CharacterModel>();
        public CharacterModel? SelectedCharacter { get; private set; }
        public CharacterBaseStatsModel? BaseStats { get; private set; }
        public CharacterCurrentStateModel? CurrentState { get; private set; }
        public CultivationRewardNotice? LastCultivationReward { get; private set; }

        public void ApplyCharacterList(CharacterModel[] characters)
        {
            HasLoadedCharacterList = true;
            CharacterList = characters ?? Array.Empty<CharacterModel>();
        }

        public void AppendCharacter(CharacterModel character)
        {
            HasLoadedCharacterList = true;
            CharacterList = CharacterList
                .Where(existing => existing.CharacterId != character.CharacterId)
                .Append(character)
                .ToArray();
        }

        public void SelectCharacter(Guid characterId)
        {
            SelectedCharacterId = characterId;
        }

        public void ApplyCharacterData(
            CharacterModel character,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState)
        {
            var previousState = CurrentState;
            SelectedCharacterId = character.CharacterId;
            SelectedCharacter = character;
            BaseStats = baseStats;
            CurrentState = currentState;
            NotifyCurrentStateChanged(previousState, currentState);
        }

        public void ApplyBaseStats(CharacterBaseStatsModel? baseStats)
        {
            BaseStats = baseStats;
        }

        public void ApplyCurrentState(CharacterCurrentStateModel? currentState)
        {
            var previousState = CurrentState;
            CurrentState = currentState;
            NotifyCurrentStateChanged(previousState, currentState);
        }

        public void ApplyCultivationReward(CultivationRewardNotice notice)
        {
            LastCultivationReward = notice;
            var handler = CultivationRewardGranted;
            if (handler != null)
                handler(notice);
        }

        public void PublishStateTransition(CharacterStateTransitionNotice notice)
        {
            var handler = StateTransitioned;
            if (handler != null)
                handler(notice);
        }

        public void Clear()
        {
            var previousState = CurrentState;
            SelectedCharacterId = null;
            HasLoadedCharacterList = false;
            CharacterList = Array.Empty<CharacterModel>();
            SelectedCharacter = null;
            BaseStats = null;
            CurrentState = null;
            LastCultivationReward = null;
            NotifyCurrentStateChanged(previousState, null);
        }

        private void NotifyCurrentStateChanged(CharacterCurrentStateModel? previousState, CharacterCurrentStateModel? currentState)
        {
            var handler = CurrentStateChanged;
            if (handler != null)
                handler(new CharacterCurrentStateChangeNotice(previousState, currentState));
        }
    }
}
