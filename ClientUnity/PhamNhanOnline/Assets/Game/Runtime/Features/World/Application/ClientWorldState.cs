using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientWorldState
    {
        private readonly Dictionary<Guid, ObservedCharacterModel> observedCharacters = new Dictionary<Guid, ObservedCharacterModel>();

        public event Action MapChanged;

        public int? CurrentMapId { get; private set; }
        public int? CurrentZoneIndex { get; private set; }
        public string CurrentMapName { get; private set; } = string.Empty;
        public string CurrentClientMapKey { get; private set; } = string.Empty;
        public Vector2 LocalPlayerPosition { get; private set; }
        public int ObservedCharacterCount { get { return observedCharacters.Count; } }

        public void ApplyMapJoin(MapDefinitionModel map, int zoneIndex, Vector2 localPlayerPosition)
        {
            CurrentMapId = map.MapId;
            CurrentZoneIndex = zoneIndex;
            CurrentMapName = map.Name ?? string.Empty;
            CurrentClientMapKey = map.ClientMapKey ?? string.Empty;
            LocalPlayerPosition = localPlayerPosition;
            NotifyMapChanged();
        }

        public void ApplyLocalPlayerPosition(Vector2 localPlayerPosition)
        {
            LocalPlayerPosition = localPlayerPosition;
        }

        public void UpsertObservedCharacter(ObservedCharacterModel observedCharacter)
        {
            observedCharacters[observedCharacter.Character.CharacterId] = observedCharacter;
        }

        public void RemoveObservedCharacter(Guid characterId)
        {
            observedCharacters.Remove(characterId);
        }

        public void ApplyObservedMove(Guid characterId, float currentPosX, float currentPosY)
        {
            ObservedCharacterModel observedCharacter;
            if (!observedCharacters.TryGetValue(characterId, out observedCharacter))
                return;

            observedCharacter.CurrentState.CurrentPosX = currentPosX;
            observedCharacter.CurrentState.CurrentPosY = currentPosY;
            observedCharacters[characterId] = observedCharacter;
        }

        public void ApplyObservedCurrentState(CharacterCurrentStateModel currentState)
        {
            ObservedCharacterModel observedCharacter;
            if (!observedCharacters.TryGetValue(currentState.CharacterId, out observedCharacter))
                return;

            observedCharacter.CurrentState = currentState;
            observedCharacters[currentState.CharacterId] = observedCharacter;
        }

        public void Clear()
        {
            CurrentMapId = null;
            CurrentZoneIndex = null;
            CurrentMapName = string.Empty;
            CurrentClientMapKey = string.Empty;
            LocalPlayerPosition = Vector2.zero;
            observedCharacters.Clear();
            NotifyMapChanged();
        }

        private void NotifyMapChanged()
        {
            var handler = MapChanged;
            if (handler != null)
                handler();
        }
    }
}