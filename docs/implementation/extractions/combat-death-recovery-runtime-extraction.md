# title
Combat death recovery runtime extraction

# scope
Detection of combat-dead state, return-home recovery, persisted snapshot repair, and online player relocation after combat death.

# source files
- `GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`
- `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`
- `GameServer/Runtime/CharacterRuntimeSaveService.cs`
- `GameServer/World/WorldInterestService.cs`
- `GameServer/Config/GameConfigValues.cs`

# current runtime behavior
- `CharacterCombatDeathRecoveryService.IsCombatDead(...)` treats combat-dead states as recoverable, but explicitly excludes permanently dead states (`GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`).
- `RecoverOnlinePlayerToHomeAsync` rebuilds current state with recovered HP/MP, home-map zone/spawn, idle state, cleared cultivation start, and persisted timestamp, then updates online runtime, republishes world snapshot, and flushes runtime save (`GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`).
- Online recovery also clears action restriction flags and replaces map entry context with a default-spawn entry into the home map (`GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`).
- `RecoverSnapshotToHomeAsync` and `RecoverDisconnectedPlayerToHomeAsync` provide equivalent persisted/offline repair paths without the full online world publish (`GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`).
- `ReturnHomeAfterCombatDeathHandler` is the explicit client-triggered recovery path; it rejects non-dead players and returns updated base/current state on success (`GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`).

# validations / guards
- Recovery is a no-op when base stats/current state are missing or the state is not combat-dead (`GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`).
- Handler rejects missing online player with `CharacterNotFound` and non-combat-dead players with `CharacterNotCombatDead` (`GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`).
- Recovery ratio is clamped to `[0,1]` before recalculating HP/MP (`GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`).

# config/data dependencies
- Game config: `CombatDeathReturnHomeRecoveryRatio` (`GameServer/Config/GameConfigValues.cs`).
- Home-map default spawn from `MapCatalog.ResolveHomeDefinition()`.
- Runtime save service persists post-recovery state.

# client/server touch points
- `ReturnHomeAfterCombatDeathPacket` / `ReturnHomeAfterCombatDeathResultPacket` is the explicit client recovery surface.
- Successful online recovery also triggers current-state notify plus full world snapshot republish.

# edge cases
- Recovered stamina is clamped against effective max stamina but otherwise preserved from current value.
- Permanently dead states are not treated as recoverable by this service.
- Disconnected recovery path updates persistence without republishing world state.

# unclear or suspicious behavior
- Recovery always returns the player to home-map default spawn; no map- or death-location-specific override is visible.
- Base stats are returned unchanged; only current state/resources/location are repaired in this service.

# suggested canonical target docs
- `docs/combat/combat-death-and-return-home-runtime.md`
