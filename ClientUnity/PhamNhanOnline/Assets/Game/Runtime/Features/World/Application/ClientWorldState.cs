using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientWorldState
    {
        private readonly Dictionary<Guid, ObservedCharacterModel> observedCharacters = new Dictionary<Guid, ObservedCharacterModel>();
        private readonly Dictionary<int, EnemyRuntimeModel> enemies = new Dictionary<int, EnemyRuntimeModel>();
        private readonly Dictionary<int, GroundRewardModel> groundRewards = new Dictionary<int, GroundRewardModel>();
        private readonly Dictionary<int, MapSpawnPointModel> spawnPoints = new Dictionary<int, MapSpawnPointModel>();
        private readonly Dictionary<int, MapPortalModel> portals = new Dictionary<int, MapPortalModel>();
        private readonly List<int> currentAdjacentMapIds = new List<int>();
        private const string PortalTargetIdPrefix = "portal:";
        private const string GroundRewardTargetIdPrefix = "reward:";

        public event Action MapChanged;
        public event Action ObservedCharactersChanged;
        public event Action EnemiesChanged;
        public event Action<ObservedCharacterModel> ObservedCharacterUpserted;
        public event Action<Guid> ObservedCharacterRemoved;
        public event Action<ObservedCharacterMovedNotice> ObservedCharacterMoved;
        public event Action<ObservedCharacterStateChangedNotice> ObservedCharacterStateChanged;
        public event Action<EnemyRuntimeModel> EnemyUpserted;
        public event Action<int> EnemyRemoved;
        public event Action<EnemyHpChangedNotice> EnemyHpChanged;
        public event Action GroundRewardsChanged;
        public event Action<GroundRewardModel> GroundRewardUpserted;
        public event Action<int> GroundRewardRemoved;

        public int? CurrentMapId { get; private set; }
        public int? CurrentZoneIndex { get; private set; }
        public string CurrentMapName { get; private set; } = string.Empty;
        public string CurrentClientMapKey { get; private set; } = string.Empty;
        public float CurrentMapWidth { get; private set; }
        public float CurrentMapHeight { get; private set; }
        public bool CurrentMapIsPrivatePerPlayer { get; private set; }
        public int? CurrentEntryReason { get; private set; }
        public int? CurrentEntryPortalId { get; private set; }
        public int? CurrentEntrySpawnPointId { get; private set; }
        public Vector2 LocalPlayerPosition { get; private set; }
        public int ObservedCharacterCount { get { return observedCharacters.Count; } }
        public IEnumerable<ObservedCharacterModel> ObservedCharacters { get { return observedCharacters.Values; } }
        public int EnemyCount { get { return enemies.Count; } }
        public IEnumerable<EnemyRuntimeModel> Enemies { get { return enemies.Values; } }
        public int GroundRewardCount { get { return groundRewards.Count; } }
        public IEnumerable<GroundRewardModel> GroundRewards { get { return groundRewards.Values; } }
        public IReadOnlyList<int> CurrentAdjacentMapIds { get { return currentAdjacentMapIds; } }
        public IEnumerable<MapSpawnPointModel> CurrentSpawnPoints { get { return spawnPoints.Values; } }
        public IEnumerable<MapPortalModel> CurrentPortals { get { return portals.Values; } }

        public void ApplyMapJoin(
            MapDefinitionModel map,
            int zoneIndex,
            Vector2 localPlayerPosition,
            int? entryReason,
            int? entryPortalId,
            int? entrySpawnPointId)
        {
            var didMapVisualChange =
                CurrentMapId != map.MapId ||
                !string.Equals(CurrentClientMapKey, map.ClientMapKey ?? string.Empty, StringComparison.Ordinal);

            CurrentMapId = map.MapId;
            CurrentZoneIndex = zoneIndex;
            CurrentMapName = map.Name ?? string.Empty;
            CurrentClientMapKey = map.ClientMapKey ?? string.Empty;
            CurrentMapWidth = map.Width;
            CurrentMapHeight = map.Height;
            CurrentMapIsPrivatePerPlayer = map.IsPrivatePerPlayer;
            CurrentEntryReason = entryReason;
            CurrentEntryPortalId = entryPortalId;
            CurrentEntrySpawnPointId = entrySpawnPointId;
            LocalPlayerPosition = localPlayerPosition;
            currentAdjacentMapIds.Clear();
            observedCharacters.Clear();
            enemies.Clear();
            groundRewards.Clear();
            spawnPoints.Clear();
            portals.Clear();
            if (map.AdjacentMapIds != null)
                currentAdjacentMapIds.AddRange(map.AdjacentMapIds);
            if (map.SpawnPoints != null)
            {
                for (var i = 0; i < map.SpawnPoints.Count; i++)
                    spawnPoints[map.SpawnPoints[i].Id] = map.SpawnPoints[i];
            }

            if (map.Portals != null)
            {
                for (var i = 0; i < map.Portals.Count; i++)
                    portals[map.Portals[i].Id] = map.Portals[i];
            }

            if (didMapVisualChange)
                NotifyMapChanged();

            NotifyObservedCharactersChanged();
            NotifyEnemiesChanged();
            NotifyGroundRewardsChanged();
        }

        public void ApplyLocalPlayerPosition(Vector2 localPlayerPosition)
        {
            LocalPlayerPosition = localPlayerPosition;
        }

        public void UpsertObservedCharacter(ObservedCharacterModel observedCharacter)
        {
            observedCharacters[observedCharacter.Character.CharacterId] = observedCharacter;
            NotifyObservedCharacterUpserted(observedCharacter);
            NotifyObservedCharactersChanged();
        }

        public void RemoveObservedCharacter(Guid characterId)
        {
            if (!observedCharacters.Remove(characterId))
                return;

            NotifyObservedCharacterRemoved(characterId);
            NotifyObservedCharactersChanged();
        }

        public void ApplyObservedMove(Guid characterId, float currentPosX, float currentPosY)
        {
            ObservedCharacterModel observedCharacter;
            if (!observedCharacters.TryGetValue(characterId, out observedCharacter))
                return;

            var previousPosX = observedCharacter.CurrentState.CurrentPosX;
            var previousPosY = observedCharacter.CurrentState.CurrentPosY;
            observedCharacter.CurrentState.CurrentPosX = currentPosX;
            observedCharacter.CurrentState.CurrentPosY = currentPosY;
            observedCharacters[characterId] = observedCharacter;
            NotifyObservedCharacterMoved(new ObservedCharacterMovedNotice(
                characterId,
                observedCharacter,
                previousPosX,
                previousPosY,
                currentPosX,
                currentPosY));
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

            var previousState = observedCharacter.CurrentState;
            var previousMaxHp = observedCharacter.MaxHp;
            var previousMaxMp = observedCharacter.MaxMp;
            observedCharacter.CurrentState = currentState;
            if (maxHp.HasValue)
                observedCharacter.MaxHp = Mathf.Max(0, maxHp.Value);
            if (maxMp.HasValue)
                observedCharacter.MaxMp = Mathf.Max(0, maxMp.Value);
            observedCharacters[currentState.CharacterId] = observedCharacter;
            NotifyObservedCharacterStateChanged(new ObservedCharacterStateChangedNotice(
                currentState.CharacterId,
                observedCharacter,
                previousState,
                currentState,
                previousMaxHp,
                observedCharacter.MaxHp,
                previousMaxMp,
                observedCharacter.MaxMp));
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

        public void ReplaceGroundRewards(IEnumerable<GroundRewardModel> rewardSnapshot)
        {
            groundRewards.Clear();
            if (rewardSnapshot != null)
            {
                foreach (var reward in rewardSnapshot)
                    groundRewards[reward.RewardId] = reward;
            }

            NotifyGroundRewardsChanged();
        }

        public void UpsertGroundReward(GroundRewardModel reward)
        {
            groundRewards[reward.RewardId] = reward;
            NotifyGroundRewardUpserted(reward);
            NotifyGroundRewardsChanged();
        }

        public void RemoveGroundReward(int rewardId)
        {
            if (!groundRewards.Remove(rewardId))
                return;

            NotifyGroundRewardRemoved(rewardId);
            NotifyGroundRewardsChanged();
        }

        public void UpsertEnemy(EnemyRuntimeModel enemy)
        {
            enemies[enemy.RuntimeId] = enemy;
            NotifyEnemyUpserted(enemy);
            NotifyEnemiesChanged();
        }

        public void RemoveEnemy(int runtimeId)
        {
            if (!enemies.Remove(runtimeId))
                return;

            NotifyEnemyRemoved(runtimeId);
            NotifyEnemiesChanged();
        }

        public void ApplyEnemyHpChanged(int runtimeId, int? currentHp, int? maxHp, int? runtimeState)
        {
            EnemyRuntimeModel enemy;
            if (!enemies.TryGetValue(runtimeId, out enemy))
                return;

            var previousCurrentHp = enemy.CurrentHp;
            var previousMaxHp = enemy.MaxHp;
            var previousRuntimeState = enemy.RuntimeState;
            if (currentHp.HasValue)
                enemy.CurrentHp = Mathf.Max(0, currentHp.Value);
            if (maxHp.HasValue)
                enemy.MaxHp = Mathf.Max(0, maxHp.Value);
            if (runtimeState.HasValue)
                enemy.RuntimeState = runtimeState.Value;

            enemies[runtimeId] = enemy;
            NotifyEnemyHpChanged(new EnemyHpChangedNotice(
                runtimeId,
                enemy,
                previousCurrentHp,
                enemy.CurrentHp,
                previousMaxHp,
                enemy.MaxHp,
                previousRuntimeState,
                enemy.RuntimeState));
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

        public bool TryGetGroundReward(int rewardId, out GroundRewardModel reward)
        {
            return groundRewards.TryGetValue(rewardId, out reward);
        }

        public bool TryGetSpawnPoint(int spawnPointId, out MapSpawnPointModel spawnPoint)
        {
            return spawnPoints.TryGetValue(spawnPointId, out spawnPoint);
        }

        public bool TryGetPortal(int portalId, out MapPortalModel portal)
        {
            return portals.TryGetValue(portalId, out portal);
        }

        public bool TryGetPortal(WorldTargetHandle handle, out MapPortalModel portal)
        {
            portal = default;
            if (handle.Kind != WorldTargetKind.Npc)
                return false;

            int portalId;
            return TryParsePortalTargetId(handle.TargetId, out portalId) && portals.TryGetValue(portalId, out portal);
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
                case WorldTargetKind.Npc:
                    return TryBuildPortalTargetSnapshot(handle.TargetId, out snapshot);
                case WorldTargetKind.GroundReward:
                    return TryBuildGroundRewardTargetSnapshot(handle.TargetId, out snapshot);
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
            CurrentEntryReason = null;
            CurrentEntryPortalId = null;
            CurrentEntrySpawnPointId = null;
            LocalPlayerPosition = Vector2.zero;
            currentAdjacentMapIds.Clear();
            observedCharacters.Clear();
            enemies.Clear();
            groundRewards.Clear();
            spawnPoints.Clear();
            portals.Clear();
            NotifyMapChanged();
            NotifyObservedCharactersChanged();
            NotifyEnemiesChanged();
            NotifyGroundRewardsChanged();
        }

        public void ClearRuntimeEntitiesPreservingMap()
        {
            observedCharacters.Clear();
            enemies.Clear();
            groundRewards.Clear();
            NotifyObservedCharactersChanged();
            NotifyEnemiesChanged();
            NotifyGroundRewardsChanged();
        }
        public static string BuildPortalTargetId(int portalId)
        {
            return PortalTargetIdPrefix + portalId.ToString(CultureInfo.InvariantCulture);
        }

        public static string BuildGroundRewardTargetId(int rewardId)
        {
            return GroundRewardTargetIdPrefix + rewardId.ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryParsePortalTargetId(string targetId, out int portalId)
        {
            portalId = 0;
            if (string.IsNullOrWhiteSpace(targetId) ||
                !targetId.StartsWith(PortalTargetIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var rawPortalId = targetId.Substring(PortalTargetIdPrefix.Length);
            return int.TryParse(rawPortalId, NumberStyles.Integer, CultureInfo.InvariantCulture, out portalId) &&
                   portalId > 0;
        }

        public static bool TryParseGroundRewardTargetId(string targetId, out int rewardId)
        {
            rewardId = 0;
            if (string.IsNullOrWhiteSpace(targetId) ||
                !targetId.StartsWith(GroundRewardTargetIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var rawRewardId = targetId.Substring(GroundRewardTargetIdPrefix.Length);
            return int.TryParse(rawRewardId, NumberStyles.Integer, CultureInfo.InvariantCulture, out rewardId) &&
                   rewardId > 0;
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

        private void NotifyObservedCharacterUpserted(ObservedCharacterModel observedCharacter)
        {
            var handler = ObservedCharacterUpserted;
            if (handler != null)
                handler(observedCharacter);
        }

        private void NotifyObservedCharacterRemoved(Guid characterId)
        {
            var handler = ObservedCharacterRemoved;
            if (handler != null)
                handler(characterId);
        }

        private void NotifyObservedCharacterMoved(ObservedCharacterMovedNotice notice)
        {
            var handler = ObservedCharacterMoved;
            if (handler != null)
                handler(notice);
        }

        private void NotifyObservedCharacterStateChanged(ObservedCharacterStateChangedNotice notice)
        {
            var handler = ObservedCharacterStateChanged;
            if (handler != null)
                handler(notice);
        }

        private void NotifyEnemyUpserted(EnemyRuntimeModel enemy)
        {
            var handler = EnemyUpserted;
            if (handler != null)
                handler(enemy);
        }

        private void NotifyEnemyRemoved(int runtimeId)
        {
            var handler = EnemyRemoved;
            if (handler != null)
                handler(runtimeId);
        }

        private void NotifyEnemyHpChanged(EnemyHpChangedNotice notice)
        {
            var handler = EnemyHpChanged;
            if (handler != null)
                handler(notice);
        }

        private void NotifyGroundRewardsChanged()
        {
            var handler = GroundRewardsChanged;
            if (handler != null)
                handler();
        }

        private void NotifyGroundRewardUpserted(GroundRewardModel reward)
        {
            var handler = GroundRewardUpserted;
            if (handler != null)
                handler(reward);
        }

        private void NotifyGroundRewardRemoved(int rewardId)
        {
            var handler = GroundRewardRemoved;
            if (handler != null)
                handler(rewardId);
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
                ClientCharacterRuntimeStateCodes.IsDefeated(observedCharacter.CurrentState));
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

        private bool TryBuildPortalTargetSnapshot(string targetId, out WorldTargetSnapshot snapshot)
        {
            snapshot = default;

            int portalId;
            MapPortalModel portal;
            if (!TryParsePortalTargetId(targetId, out portalId) || !portals.TryGetValue(portalId, out portal))
                return false;

            var displayName = !string.IsNullOrWhiteSpace(portal.TargetMapName)
                ? portal.TargetMapName
                : (!string.IsNullOrWhiteSpace(portal.Name) ? portal.Name : "Portal");

            snapshot = new WorldTargetSnapshot(
                WorldTargetKind.Npc,
                targetId,
                displayName,
                0,
                0,
                false,
                0,
                0,
                false,
                false);
            return true;
        }

        private bool TryBuildGroundRewardTargetSnapshot(string targetId, out WorldTargetSnapshot snapshot)
        {
            snapshot = default;

            int rewardId;
            GroundRewardModel reward;
            if (!TryParseGroundRewardTargetId(targetId, out rewardId) || !groundRewards.TryGetValue(rewardId, out reward))
                return false;

            var itemCount = reward.Items != null ? reward.Items.Count : 0;
            var displayName = itemCount switch
            {
                <= 0 => "Vat pham",
                1 => string.IsNullOrWhiteSpace(reward.Items[0].Name) ? "Vat pham" : reward.Items[0].Name,
                _ => "Nhieu vat pham"
            };

            snapshot = new WorldTargetSnapshot(
                WorldTargetKind.GroundReward,
                targetId,
                displayName,
                0,
                0,
                false,
                0,
                0,
                false,
                false);
            return true;
        }
    }
}

