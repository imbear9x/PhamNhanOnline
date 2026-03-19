using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientWorldState
    {
        private readonly Dictionary<Guid, ObservedCharacterModel> observedCharacters = new Dictionary<Guid, ObservedCharacterModel>();
        private readonly Dictionary<int, EnemyRuntimeModel> enemies = new Dictionary<int, EnemyRuntimeModel>();
        private readonly List<int> currentAdjacentMapIds = new List<int>();

        public event Action MapChanged;
        public event Action ObservedCharactersChanged;
        public event Action EnemiesChanged;

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
        public int EnemyCount { get { return enemies.Count; } }
        public IEnumerable<EnemyRuntimeModel> Enemies { get { return enemies.Values; } }
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
            observedCharacters.Clear();
            enemies.Clear();
            if (map.AdjacentMapIds != null)
                currentAdjacentMapIds.AddRange(map.AdjacentMapIds);
            NotifyMapChanged();
            NotifyObservedCharactersChanged();
            NotifyEnemiesChanged();
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
            ApplyObservedCurrentState(currentState, null, null);
        }

        public void ApplyObservedCurrentState(CharacterCurrentStateModel currentState, int? maxHp, int? maxMp)
        {
            ObservedCharacterModel observedCharacter;
            if (!observedCharacters.TryGetValue(currentState.CharacterId, out observedCharacter))
                return;

            observedCharacter.CurrentState = currentState;
            if (maxHp.HasValue)
                observedCharacter.MaxHp = Mathf.Max(0, maxHp.Value);
            if (maxMp.HasValue)
                observedCharacter.MaxMp = Mathf.Max(0, maxMp.Value);
            observedCharacters[currentState.CharacterId] = observedCharacter;
            NotifyObservedCharactersChanged();
        }

        public void ReplaceEnemies(IEnumerable<EnemyRuntimeModel> enemySnapshot)
        {
            enemies.Clear();
            if (enemySnapshot != null)
            {
                foreach (var enemy in enemySnapshot)
                    enemies[enemy.RuntimeId] = enemy;
            }

            NotifyEnemiesChanged();
        }

        public void UpsertEnemy(EnemyRuntimeModel enemy)
        {
            enemies[enemy.RuntimeId] = enemy;
            NotifyEnemiesChanged();
        }

        public void RemoveEnemy(int runtimeId)
        {
            if (!enemies.Remove(runtimeId))
                return;

            NotifyEnemiesChanged();
        }

        public void ApplyEnemyHpChanged(int runtimeId, int? currentHp, int? maxHp, int? runtimeState)
        {
            EnemyRuntimeModel enemy;
            if (!enemies.TryGetValue(runtimeId, out enemy))
                return;

            if (currentHp.HasValue)
                enemy.CurrentHp = Mathf.Max(0, currentHp.Value);
            if (maxHp.HasValue)
                enemy.MaxHp = Mathf.Max(0, maxHp.Value);
            if (runtimeState.HasValue)
                enemy.RuntimeState = runtimeState.Value;

            enemies[runtimeId] = enemy;
            NotifyEnemiesChanged();
        }

        public bool TryGetObservedCharacter(Guid characterId, out ObservedCharacterModel observedCharacter)
        {
            return observedCharacters.TryGetValue(characterId, out observedCharacter);
        }

        public bool TryGetEnemy(int runtimeId, out EnemyRuntimeModel enemy)
        {
            return enemies.TryGetValue(runtimeId, out enemy);
        }

        public bool TryBuildTargetSnapshot(WorldTargetHandle handle, out WorldTargetSnapshot snapshot)
        {
            snapshot = default;
            if (!handle.IsValid)
                return false;

            switch (handle.Kind)
            {
                case WorldTargetKind.Player:
                    return TryBuildObservedCharacterTargetSnapshot(handle.TargetId, out snapshot);
                case WorldTargetKind.Enemy:
                case WorldTargetKind.Boss:
                    return TryBuildEnemyTargetSnapshot(handle.TargetId, handle.Kind, out snapshot);
                default:
                    return false;
            }
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
            enemies.Clear();
            NotifyMapChanged();
            NotifyObservedCharactersChanged();
            NotifyEnemiesChanged();
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

        private void NotifyEnemiesChanged()
        {
            var handler = EnemiesChanged;
            if (handler != null)
                handler();
        }

        private bool TryBuildObservedCharacterTargetSnapshot(string targetId, out WorldTargetSnapshot snapshot)
        {
            snapshot = default;

            Guid characterId;
            if (!Guid.TryParse(targetId, out characterId))
                return false;

            ObservedCharacterModel observedCharacter;
            if (!observedCharacters.TryGetValue(characterId, out observedCharacter))
                return false;

            var currentHp = Mathf.Max(0, observedCharacter.CurrentState.CurrentHp);
            var maxHp = Mathf.Max(currentHp, observedCharacter.MaxHp);
            var currentMp = Mathf.Max(0, observedCharacter.CurrentState.CurrentMp);
            var maxMp = Mathf.Max(currentMp, observedCharacter.MaxMp);
            var displayName = !string.IsNullOrWhiteSpace(observedCharacter.Character.Name)
                ? observedCharacter.Character.Name
                : "Nguoi choi";

            snapshot = new WorldTargetSnapshot(
                WorldTargetKind.Player,
                targetId,
                displayName,
                currentHp,
                maxHp,
                maxHp > 0 || currentHp > 0,
                currentMp,
                maxMp,
                maxMp > 0 || currentMp > 0,
                observedCharacter.CurrentState.IsDead);
            return true;
        }

        private bool TryBuildEnemyTargetSnapshot(string targetId, WorldTargetKind requestedKind, out WorldTargetSnapshot snapshot)
        {
            snapshot = default;

            int runtimeId;
            if (!int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out runtimeId))
                return false;

            EnemyRuntimeModel enemy;
            if (!enemies.TryGetValue(runtimeId, out enemy))
                return false;

            var resolvedKind = enemy.Kind == 3 ? WorldTargetKind.Boss : requestedKind;
            if (resolvedKind == WorldTargetKind.Boss && enemy.Kind != 3)
                resolvedKind = WorldTargetKind.Enemy;

            var currentHp = Mathf.Max(0, enemy.CurrentHp);
            var maxHp = Mathf.Max(currentHp, enemy.MaxHp);
            var displayName = !string.IsNullOrWhiteSpace(enemy.Name)
                ? enemy.Name
                : "Yeu thu";

            snapshot = new WorldTargetSnapshot(
                resolvedKind,
                targetId,
                displayName,
                currentHp,
                maxHp,
                maxHp > 0 || currentHp > 0,
                0,
                0,
                false,
                enemy.RuntimeState != 0 && currentHp <= 0);
            return true;
        }
    }
}

