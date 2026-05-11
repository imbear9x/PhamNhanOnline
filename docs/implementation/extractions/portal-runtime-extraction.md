# title
Portal runtime extraction

# scope
Server-side portal definition loading, portal-target resolution, request validation, interaction gating, and travel result/world snapshot flow.

# source files
- `GameServer/Entities/MapPortalEntity.cs`
- `GameServer/World/MapCatalog.cs`
- `GameServer/World/MapDefinition.cs`
- `GameServer/World/MapTravelTopologyTypes.cs`
- `GameServer/Runtime/WorldTargetResolver.cs`
- `GameServer/Network/Validations/TravelToMapPacketValidator.cs`
- `GameServer/Network/Handlers/TravelToMapHandler.cs`
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/DTO/NetworkModelMapper.cs`
- `GameShared/Models/MapPortalModel.cs`
- `GameShared/Packets/Packets/WorldPackets.cs`

# current runtime behavior
- Portal rows come from `map_portals` and carry source map/position, interaction radius/mode, target map, target spawn point, enabled flag, order index, and description (`GameServer/Entities/MapPortalEntity.cs`).
- `MapCatalog` loads portals at startup, groups them by source map, sorts by `OrderIndex` then `Id`, and exposes them as `MapPortalDefinition` lookup lists/dictionaries (`GameServer/World/MapCatalog.cs`).
- Each portal definition stores source position, `Touch`/`Interact` mode, target map metadata, target spawn point, and enabled state (`GameServer/World/MapTravelTopologyTypes.cs`).
- `TravelToMapPacket` supports two travel paths: legacy direct `TargetMapId`, or portal-based travel via `PortalId` (`GameShared/Packets/Packets/WorldPackets.cs`, `GameServer/Network/Handlers/TravelToMapHandler.cs`).
- In portal flow, `TravelToMapHandler` resolves the portal from the player’s current map, resolves the target map and target spawn point, checks that the player’s current instance exists, then asks `WorldInteractionGate` to validate portal interaction against portal source position and range (`GameServer/Network/Handlers/TravelToMapHandler.cs`).
- Successful portal travel updates the player runtime position to the target map/spawn, sets `MapEntryContext` with reason `Portal` and source `portalId`, republishes the world snapshot, and sends `TravelToMapResultPacket { Success = true }` (`GameServer/Network/Handlers/TravelToMapHandler.cs`).
- `WorldTargetResolver.ResolveForPlayerInteraction` can resolve portal targets for generic interaction flows; it returns a snapshot located at the portal source position and marks it interactable if the portal exists and is enabled (`GameServer/Runtime/WorldTargetResolver.cs`).
- Portal data is also sent to the client inside `MapDefinitionModel.Portals` using `MapPortalModel` (`GameServer/DTO/NetworkModelMapper.cs`, `GameShared/Models/MapPortalModel.cs`).

# validations / guards
- Packet validator accepts portal travel only when `PortalId > 0`; otherwise it returns `TravelToMapResultPacket` with `MessageCode.MapPortalInvalid` (`GameServer/Network/Validations/TravelToMapPacketValidator.cs`).
- Portal travel is blocked early if the session has no player, if interaction start is denied by `WorldInteractionGate.CheckPlayerCanStartAction`, or if the portal is missing/disabled (`GameServer/Network/Handlers/TravelToMapHandler.cs`).
- Startup catalog build throws if a portal’s `SourceMapTemplateId` does not match the map being processed, if target spawn point is missing, or if target map is missing (`GameServer/World/MapCatalog.cs`).
- Portal runtime interaction range is `portal.InteractionRadius + world.portal_validation_buffer_server_units`, both clamped to non-negative at use time (`GameServer/Network/Handlers/TravelToMapHandler.cs`, `GameServer/Config/GameConfigKeys.cs`).
- `WorldTargetResolver` rejects portal interaction if player/map/instance/zone do not match the instance, or if portal lookup fails / portal is disabled (`GameServer/Runtime/WorldTargetResolver.cs`).

# config/data dependencies
- DB-backed `map_portals` table (`GameServer/Entities/MapPortalEntity.cs`).
- DB-backed map and spawn-point data must stay consistent with portal targets, or `MapCatalog` construction fails (`GameServer/World/MapCatalog.cs`).
- Config key `world.portal_validation_buffer_server_units` expands allowed server-side interaction distance (`GameServer/Config/GameConfigKeys.cs`, `GameServer/Network/Handlers/TravelToMapHandler.cs`).

# client/server touch points
- Client sends `TravelToMapPacket` with either `PortalId` or `TargetMapId`, plus optional current position fields (`GameShared/Packets/Packets/WorldPackets.cs`).
- Server replies with `TravelToMapResultPacket`, echoing resolved target map / spawn info on success or failure (`GameServer/Network/Handlers/TravelToMapHandler.cs`, `GameShared/Packets/Packets/WorldPackets.cs`).
- Client receives portal definitions embedded in `MapJoinedPacket.Map.Portals` as `MapPortalModel` (`GameServer/World/WorldInterestService.cs`, `GameServer/DTO/NetworkModelMapper.cs`).
- On successful portal travel the server also sends a fresh `MapJoinedPacket` and `WorldRuntimeSnapshotPacket` through `PublishWorldSnapshot` (`GameServer/World/WorldInterestService.cs`).

# edge cases
- If portal target map exists but target spawn point is missing, the runtime returns `MapPortalInvalid` rather than `MapIdInvalid` (`GameServer/Network/Handlers/TravelToMapHandler.cs`).
- If player state says they are in a map but `MapManager` cannot resolve the current instance, portal travel fails with `CharacterNotInWorldInstance` (`GameServer/Network/Handlers/TravelToMapHandler.cs`).
- Private target maps always force zone `0`; non-private targets auto-select a public zone via `ResolveAutoJoinZone` (`GameServer/Network/Handlers/TravelToMapHandler.cs`).
- Optional packet fields `CurrentPosX/CurrentPosY` are logged but not used for validation in this handler (`GameServer/Network/Handlers/TravelToMapHandler.cs`).

# unclear or suspicious behavior
- `MapPortalInteractionMode` (`Touch` vs `Interact`) is loaded and serialized but not used in `TravelToMapHandler`; actual enforcement may live elsewhere or may not exist yet (`GameServer/World/MapTravelTopologyTypes.cs`, `GameServer/Network/Handlers/TravelToMapHandler.cs`).
- Legacy direct travel and portal travel share the same packet, so clients can still bypass portal-specific UX when adjacency allows legacy map travel (`GameServer/Network/Handlers/TravelToMapHandler.cs`).
- Portal target validation is strict at catalog-build time, meaning one bad portal row can fail startup for the whole catalog (`GameServer/World/MapCatalog.cs`).

# suggested canonical target docs
- `docs/canonical/runtime/portal-travel-flow.md`
- `docs/canonical/data/map-portal-data-contract.md`
- `docs/canonical/network/world-travel-packets.md`
