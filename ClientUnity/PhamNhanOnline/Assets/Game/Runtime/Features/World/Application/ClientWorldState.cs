using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientWorldState
    {
        private readonly Dictionary<Guid, ObservedCharacterModel> observedCharacters = new Dictionary<Guid, ObservedCharacterModel>();
        private readonly List<int> currentAdjacentMapIds = new List<int>();

        public event Action MapChanged;
        public event Action ObservedCharactersChanged;

        public int? CurrentMapId { get; private set; }
        public int? CurrentZoneIndex { get; private set; }
        public string CurrentMapName { get; private set; } = string.Empty;
        public string CurrentClientMapKey { get; private set; } = string.Empty;
        public float CurrentMapWidth { get; private set; }
        public float CurrentMapHeight { get; private set; }
        public bool CurrentMapIsPrivatePerPlayer { get; private set; }
        public Vector2 LocalPlayerPosition { get; private set; }
        public int ObservedCharacterCount { get { return observedCharacters.Count; } }
        public IEnumerable<ObservedCharacterModel> ObservedCharacters { get { return observedCharacters.Values; } }
        public IReadOnlyList<int> CurrentAdjacentMapIds { get { return currentAdjacentMapIds; } }

        public void ApplyMapJoin(MapDefinitionModel map, int zoneIndex, Vector2 localPlayerPosition)
        {
            CurrentMapId = map.MapId;
            CurrentZoneIndex = zoneIndex;
            CurrentMapName = map.Name ?? string.Empty;
            CurrentClientMapKey = map.ClientMapKey ?? string.Empty;
            CurrentMapWidth = map.Width;
            CurrentMapHeight = map.Height;
            CurrentMapIsPrivatePerPlayer = map.IsPrivatePerPlayer;
            LocalPlayerPosition = localPlayerPosition;
            currentAdjacentMapIds.Clear();
            if (map.AdjacentMapIds != null)
                currentAdjacentMapIds.AddRange(map.AdjacentMapIds);
            NotifyMapChanged();
        }

        public void ApplyLocalPlayerPosition(Vector2 localPlayerPosition)
        {
            LocalPlayerPosition = localPlayerPosition;
        }

        public void UpsertObservedCharacter(ObservedCharacterModel observedCharacter)
        {
            observedCharacters[observedCharacter.Character.CharacterId] = observedCharacter;
            NotifyObservedCharactersChanged();
        }

        public void RemoveObservedCharacter(Guid characterId)
        {
            if (!observedCharacters.Remove(characterId))
                return;

            NotifyObservedCharactersChanged();
        }

        public void ApplyObservedMove(Guid characterId, float currentPosX, float currentPosY)
        {
            ObservedCharacterModel observedCharacter;
            if (!observedCharacters.TryGetValue(characterId, out observedCharacter))
                return;

            observedCharacter.CurrentState.CurrentPosX = currentPosX;
            observedCharacter.CurrentState.CurrentPosY = currentPosY;
            observedCharacters[characterId] = observedCharacter;
            NotifyObservedCharactersChanged();
        }

        public void ApplyObservedCurrentState(CharacterCurrentStateModel currentState)
        {
            ObservedCharacterModel observedCharacter;
            if (!observedCharacters.TryGetValue(currentState.CharacterId, out observedCharacter))
                return;

            observedCharacter.CurrentState = currentState;
            observedCharacters[currentState.CharacterId] = observedCharacter;
            NotifyObservedCharactersChanged();
        }

        public void Clear()
        {
            CurrentMapId = null;
            CurrentZoneIndex = null;
            CurrentMapName = string.Empty;
            CurrentClientMapKey = string.Empty;
            CurrentMapWidth = 0f;
            CurrentMapHeight = 0f;
            CurrentMapIsPrivatePerPlayer = false;
            LocalPlayerPosition = Vector2.zero;
            currentAdjacentMapIds.Clear();
            observedCharacters.Clear();
            NotifyMapChanged();
            NotifyObservedCharactersChanged();
        }

        private void NotifyMapChanged()
        {
            var handler = MapChanged;
            if (handler != null)
                handler();
        }

        private void NotifyObservedCharactersChanged()
        {
            var handler = ObservedCharactersChanged;
            if (handler != null)
                handler();
        }
    }
}
