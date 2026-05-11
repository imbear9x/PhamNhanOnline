# title
Map runtime extraction

# scope
Server-side map definition loading, instance selection/creation, player world entry, zone/public-instance behavior, and instance shutdown/redirect flow. Focused on current runtime truth in `GameServer`.

# source files
- `GameServer/World/MapCatalog.cs`
- `GameServer/World/MapDefinition.cs`
- `GameServer/World/MapTemplate.cs`
- `GameServer/World/MapManager.cs`
- `GameServer/World/WorldInterestService.cs`
- `GameServer/Runtime/MapInstanceLifecycleService.cs`
- `GameServer/World/MapTravelTopologyTypes.cs`
- `GameServer/Config/GameConfigKeys.cs`
- `GameShared/Packets/Packets/WorldPackets.cs`
- `GameServer/DTO/NetworkModelMapper.cs`

# current runtime behavior
- `MapCatalog` eagerly loads map templates, adjacent-map rows, zone slots, spawn points, portals, and spiritual-energy templates at construction time, then builds immutable lookup dictionaries keyed by map/zone/spawn/portal (`GameServer/World/MapCatalog.cs`).
- Effective adjacency for a map is the union of configured adjacent maps plus enabled portal target maps; `MapDefinition.CanTravelTo` checks membership in this merged list (`GameServer/World/MapCatalog.cs`, `GameServer/World/MapDefinition.cs`).
- `MapTemplate.DefaultZoneIndex` is `0` for private-per-player maps and `1` otherwise; `ClampPosition` bounds coordinates to `[0..Width] x [0..Height]` (`GameServer/World/MapTemplate.cs`).
- `MapManager.JoinInstance` routes players by map type/runtime data: private-per-player maps use owner-bound private instances; maps with `MapInstanceConfigDefinition` use owner-bound configured instances; other maps use public zone instances (`GameServer/World/MapManager.cs`).
- Public instances use `zoneIndex` as `instanceId`; configured/private instances use an incrementing instance id (`GameServer/World/MapManager.cs`).
- `ResolveAutoJoinZone` prefers an already-populated non-full public instance; otherwise it falls back to the map default zone (`GameServer/World/MapManager.cs`).
- `WorldInterestService.EnsurePlayerInWorld` resolves entry context, joins the instance, syncs runtime current-state map/zone/position if needed, updates instance-side player position, and stores the last map entry context (`GameServer/World/WorldInterestService.cs`).
- `PublishWorldSnapshot` sends `MapJoinedPacket` followed by `WorldRuntimeSnapshotPacket`, then rebuilds visible-player state for the session (`GameServer/World/WorldInterestService.cs`, `GameShared/Packets/Packets/WorldPackets.cs`).
- `MapInstanceLifecycleService` destroys expired/completed instances immediately if empty; otherwise it sends `MapInstanceClosedPacket`, redirects all players to the resolved home map default zone/spawn, republishes world snapshots, then destroys the instance (`GameServer/Runtime/MapInstanceLifecycleService.cs`).
- `MapManager.CleanupExpiredInstances` separately removes empty public instances that have stayed empty longer than `world.empty_public_instance_lifetime_seconds` (`GameServer/World/MapManager.cs`, `GameServer/Config/GameConfigKeys.cs`).

# validations / guards
- Missing home-map definition is fatal: `ResolveHomeDefinition` throws if no `MapType.Home` exists (`GameServer/World/MapCatalog.cs`).
- Public join rejects invalid zones outside `1..MaxPublicZoneCount` by throwing `InvalidOperationException` (`GameServer/World/MapManager.cs`).
- Public instance creation throws if `MaxPublicZoneCount <= 0` (`GameServer/World/MapManager.cs`).
- `EnsurePlayerInWorld` resolves unknown/zero map ids through `ResolveDefinitionOrDefault`, which falls back to home map rather than failing (`GameServer/World/WorldInterestService.cs`, `GameServer/World/MapCatalog.cs`).
- `PublishWorldSnapshot` logs an error and aborts if the player’s `mapId/instanceId` cannot be resolved to a live instance (`GameServer/World/WorldInterestService.cs`).
- `MapInstanceLifecycleService` only redirects players after `instance.ShouldDestroy(utcNow)` becomes true (`GameServer/Runtime/MapInstanceLifecycleService.cs`).

# config/data dependencies
- DB-backed: map templates, adjacent maps, zone slots, spawn points, portals, spiritual-energy templates (`GameServer/World/MapCatalog.cs`).
- DB-backed instance configs and enemy spawn groups influence whether a map runs as public/private/configured instance, even though the core map catalog itself does not own that data (`GameServer/World/MapManager.cs`).
- Config key `world.empty_public_instance_lifetime_seconds` controls cleanup of empty public instances (`GameServer/Config/GameConfigKeys.cs`, `GameServer/World/MapManager.cs`).

# client/server touch points
- Server sends `MapJoinedPacket` with map model, zone, and entry metadata (`GameServer/World/WorldInterestService.cs`, `GameShared/Packets/Packets/WorldPackets.cs`).
- Server sends `WorldRuntimeSnapshotPacket` containing runtime kind, instance expiry/completion timestamps, enemy snapshot, and ground reward snapshot (`GameServer/World/WorldInterestService.cs`, `GameShared/Packets/Packets/WorldPackets.cs`).
- Map definitions serialized to client include spawn points and portals via `NetworkModelMapper.ToModel(MapDefinition)` (`GameServer/DTO/NetworkModelMapper.cs`).
- Instance shutdown uses `MapInstanceClosedPacket` to tell the client the closed map/instance and redirect destination (`GameServer/Runtime/MapInstanceLifecycleService.cs`, `GameShared/Packets/Packets/WorldPackets.cs`).

# edge cases
- If a player is found in another live instance, `MapManager` silently removes stale membership before adding them to the target instance (`GameServer/World/MapManager.cs`).
- Empty private instances are removed immediately when their last player leaves; empty public instances persist until cleanup/lifecycle rules remove them (`GameServer/World/MapManager.cs`).
- Configured owner-bound instances can be rejoined if not yet destroyable; old ones are skipped when `ShouldDestroy(DateTime.UtcNow)` is already true (`GameServer/World/MapManager.cs`).
- On world snapshot publish, visible-player cache is fully reset and rebuilt, so observers are re-spawned from snapshot time rather than incrementally trusted (`GameServer/World/WorldInterestService.cs`).

# unclear or suspicious behavior
- `MapDefinition.ResolveSpawnPosition` returns `DefaultSpawnPosition` without clamping when spawn point is missing, while spawn-point resolution path clamps explicit spawn points (`GameServer/World/MapDefinition.cs`). If DB default spawn lies outside bounds, behavior depends on upstream data quality.
- Effective adjacency includes enabled portal targets even for legacy `CanTravel` checks (`GameServer/World/MapCatalog.cs`). That couples portal data to non-portal map-travel permission.
- Instance destroy/redirect flow always sends players to the home map default spawn; no per-instance fallback destination is visible in these files (`GameServer/Runtime/MapInstanceLifecycleService.cs`).

# suggested canonical target docs
- `docs/canonical/runtime/map-instance-lifecycle.md`
- `docs/canonical/runtime/map-entry-and-world-snapshot.md`
- `docs/canonical/data/map-catalog-and-zone-model.md`
