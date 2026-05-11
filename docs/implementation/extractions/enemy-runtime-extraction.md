# title
Enemy runtime extraction

# scope
Server-side enemy definition loading, spawn-group instancing, patrol/combat state updates, death/reward handling, and enemy-related world packets.

# source files
- `GameServer/Runtime/EnemyDefinitionCatalog.cs`
- `GameServer/Runtime/EnemySystemTypes.cs`
- `GameServer/World/MapManager.cs`
- `GameServer/World/MapInstance.Runtime.cs`
- `GameServer/World/MapInstance.Combat.cs`
- `GameServer/World/MapInstance.Events.cs`
- `GameServer/World/MonsterEntity.cs`
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GameServer/World/WorldInterestService.cs`
- `GameShared/Packets/Packets/WorldPackets.cs`
- `GameServer/DTO/NetworkModelMapper.cs`
- `GameServer/Config/GameConfigKeys.cs`

# current runtime behavior
- `EnemyDefinitionCatalog` eagerly loads enemy templates, enemy skill loadouts, reward rules, spawn groups, spawn entries, instance configs, and random tables at construction time, then builds immutable dictionaries keyed by enemy id, map id, and map template id (`GameServer/Runtime/EnemyDefinitionCatalog.cs`).
- `MapManager` filters spawn groups per instance by runtime scope (`Any/Public/Private/Instance`) and, for public maps only, optional `ZoneIndex` match (`GameServer/World/MapManager.cs`, `GameServer/Runtime/EnemySystemTypes.cs`).
- `MapInstance.Update` runs a world-tick loop that queues due skill events, updates enemy states, updates spawn groups, updates ground rewards, and updates instance completion state (`GameServer/World/MapInstance.Runtime.cs`).
- Timer spawn groups do an initial fill up to `MaxAlive`, then respawn one enemy at a time using weighted entry selection and random spawn position within `SpawnRadius` (`GameServer/World/MapInstance.Runtime.cs`).
- `MonsterEntity` starts alive at full HP in `Patrol` state, tracks contributions per player, patrol target/wait state, combat target, last-hit player, next attack time, and movement decision version (`GameServer/World/MonsterEntity.cs`).
- Aggressive enemies auto-acquire the nearest valid player within effective aggro radius; passive enemies only keep/resolve an existing combat target or aggro override from being hit (`GameServer/World/MapInstance.Runtime.cs`).
- During combat, enemies attack on a minimum interval of `max(250ms, MinimumSkillIntervalMs)`, rotating through configured skills in loadout order; if no skills exist, the runtime still opens attack windows but only logs once that no attack skill/basic attack is configured (`GameServer/World/MonsterEntity.cs`, `GameServer/World/MapInstance.Runtime.cs`).
- Damage application absorbs shields first, subtracts HP, records contribution + last hit, forces `Combat` state, and on kill enqueues HP change, death event, movement decision, and timer respawn scheduling for the spawn group (`GameServer/World/MonsterEntity.cs`, `GameServer/World/MapInstance.Combat.cs`).
- Dead monsters remain in `Monsters` for 2 seconds after `DiedAtUtc`, then are removed and broadcast as despawns (`GameServer/World/MapInstance.Runtime.cs`).
- Out-of-combat restore logic waits until `LastDamagedAtUtc + OutOfCombatRestoreDelaySeconds`; bosses only return to patrol and keep current HP, while non-boss enemies restore to full HP and clear contribution/aggro state (`GameServer/World/MapInstance.Runtime.cs`, `GameServer/World/MonsterEntity.cs`).
- Instance completion for `KillBoss` is marked only after no alive boss exists and every boss spawn group has already completed initial fill (`GameServer/World/MapInstance.Runtime.cs`).
- `EnemyRewardRuntimeService` processes queued deaths after world ticks: cultivation/potential rewards are split by contribution damage, then reward rules roll random tables for direct grants or ground drops (`GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/World/MapInstance.Events.cs`).

# validations / guards
- Catalog build throws if an enemy reward rule references a missing random table (`GameServer/Runtime/EnemyDefinitionCatalog.cs`).
- `MonsterEntity` constructor throws if `definition.MaxHp <= 0` (`GameServer/World/MonsterEntity.cs`).
- Spawn-group selection throws if a spawn group has zero entries when spawn is attempted (`GameServer/World/MapInstance.Runtime.cs`).
- Damage/healing <= 0 are ignored; damaging or healing a dead/missing enemy returns `EnemyAlreadyDead` / `EnemyNotFound` codes (`GameServer/World/MonsterEntity.cs`, `GameServer/World/MapInstance.Combat.cs`).
- Combat target resolution rejects disconnected players, players in a different map/instance, and defeated players (`GameServer/World/MapInstance.Runtime.cs`).
- Stun suppresses attack-window consumption until expiration (`GameServer/World/MonsterEntity.cs`).

# config/data dependencies
- DB-backed enemy templates, enemy skills, reward rules, spawn groups, spawn entries, instance configs, random tables, and realm templates (`GameServer/Runtime/EnemyDefinitionCatalog.cs`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`).
- Reward-item resolution depends on item definition catalog and random-table entry ids supported by `EnemyRewardRuntimeService` (`GameServer/Runtime/EnemyRewardRuntimeService.cs`).
- Config keys affecting reward behavior include `item_drop.enemy_drop_default_ownership_seconds`, `item_drop.enemy_drop_default_free_for_all_seconds`, and `item_drop.ground_spawn_offset_server_units` (`GameServer/Config/GameConfigKeys.cs`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`).

# client/server touch points
- World snapshot includes full enemy runtime models in `WorldRuntimeSnapshotPacket.Enemies` (`GameServer/World/WorldInterestService.cs`, `GameShared/Packets/Packets/WorldPackets.cs`).
- Incremental enemy packets: `EnemySpawnedPacket`, `EnemyDespawnedPacket`, `EnemyHpChangedPacket`, `EnemyMovementDecisionPacket` (`GameServer/World/WorldInterestService.cs`, `GameShared/Packets/Packets/WorldPackets.cs`).
- Enemy-driven combat also emits `SkillCastStartedPacket` and `SkillImpactResolvedPacket` to all players in the instance (`GameServer/World/WorldInterestService.cs`, `GameShared/Packets/Packets/WorldPackets.cs`).
- `EnemyRuntimeModel` sent to clients includes template id/code/name, kind, HP, runtime state, spawn group id, and movement state/target/version (`GameServer/DTO/NetworkModelMapper.cs`).

# edge cases
- Passive enemies still switch to combat when damaged because `ApplyDamage` forces `CombatTargetPlayerId` and sets a pending aggro override (`GameServer/World/MonsterEntity.cs`).
- If a combat target disappears or leaves valid range, the enemy returns to patrol immediately on the next update tick (`GameServer/World/MapInstance.Runtime.cs`).
- If patrol radius is zero or move speed is non-positive, patrol logic converts into waiting instead of movement (`GameServer/World/MapInstance.Runtime.cs`, `GameServer/World/MonsterEntity.cs`).
- Ground reward free-for-all time starts immediately when no owner is assigned; otherwise it starts after ownership duration elapses (`GameServer/Runtime/EnemyRewardRuntimeService.cs`).
- Configured instance completion is possible before boss corpses despawn, because completion checks alive bosses only (`GameServer/World/MapInstance.Runtime.cs`).

# unclear or suspicious behavior
- `EnemySpawnMode.Objective` and `EnemySpawnMode.Manual` exist in enums, but the shown runtime update path only visibly implements timer spawning; objective/manual trigger points are not evident in the inspected files (`GameServer/Runtime/EnemySystemTypes.cs`, `GameServer/World/MapInstance.Runtime.cs`).
- Boss out-of-combat restore enqueues an HP-changed event but does not heal HP, only returns state to patrol (`GameServer/World/MapInstance.Runtime.cs`). This may be intentional anti-reset behavior, but code alone does not explain it.
- If an enemy has no skills, combat continues generating attack windows with only a one-time log message; there is no visible fallback basic attack implementation in the inspected files (`GameServer/World/MonsterEntity.cs`, `GameServer/World/MapInstance.Runtime.cs`).

# suggested canonical target docs
- `docs/canonical/runtime/enemy-spawn-and-ai-loop.md`
- `docs/canonical/runtime/enemy-combat-state-and-events.md`
- `docs/canonical/runtime/enemy-reward-distribution.md`
