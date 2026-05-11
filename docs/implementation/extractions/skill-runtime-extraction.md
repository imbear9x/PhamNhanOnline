# title
Skill runtime extraction

# scope
Owned skill acquisition/synchronization, loadout slot assignment, equipment-granted skill behavior, and combat-time skill resolution for attacks. Focused on current server runtime behavior, not skill balance intent.

# source files
- `GameServer/Services/SkillService.cs`
- `GameServer/Services/CharacterService.cs`
- `GameServer/Services/EquipmentActionService.cs`
- `GameServer/Runtime/SkillExecutionService.cs`
- `GameServer/Runtime/SkillRuntimeNotifier.cs`
- `GameServer/Runtime/CombatDefinitionCatalog.cs`
- `GameServer/Network/Handlers/GetOwnedSkillsHandler.cs`
- `GameServer/Network/Handlers/SetSkillLoadoutSlotHandler.cs`
- `GameServer/Network/Handlers/SwapSkillLoadoutSlotsHandler.cs`
- `GameServer/Network/Handlers/AttackEnemyHandler.cs`
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/Config/GameConfigValues.cs`

# current runtime behavior
- Starter skill grant is character-config driven: `CharacterService.EnsureStarterSkillAsync(...)` reads `character.starter_skill_id` and creates a player-skill row only when the configured id is positive and the player does not already own that exact skill (`GameServer/Services/CharacterService.cs`, `GameServer/Config/GameConfigKeys.cs`).
- Owned-skills snapshots are built by `SkillService.GetOwnedSkillsSnapshotAsync(...)`, which returns `MaxLoadoutSlotCount`, canonicalized skill rows, and loadout slot rows (`GameServer/Services/SkillService.cs`).
- Loadout slot capacity is config-driven through `skill.max_loadout_slot_count` and defaults to `5` (`GameServer/Config/GameConfigKeys.cs`, `GameServer/Config/GameConfigValues.cs`).
- `SkillService` canonicalizes duplicate skill sources by grouping on skill identity and preferring one source row based on skill level, source type, and equipment metadata; extra source rows can still exist in persistence even though only one representative is exposed to clients (`GameServer/Services/SkillService.cs`).
- Equipment changes trigger `SkillService.SyncEquipmentGrantedSkillsAsync(...)`, which adds missing equipment-granted `PlayerSkillEntity` rows, removes stale ones, and removes/blocks loadout entries when the underlying equipment source is gone or realm requirements are not met (`GameServer/Services/EquipmentActionService.cs`, `GameServer/Services/SkillService.cs`).
- `GetOwnedSkillsHandler` returns the current owned-skills snapshot as `GetOwnedSkillsResultPacket`; it does not require world-entry state beyond an authenticated session (`GameServer/Network/Handlers/GetOwnedSkillsHandler.cs`).
- `SetSkillLoadoutSlotHandler` delegates to `SkillService.SetLoadoutSlotAsync(...)`, then returns the fully rebuilt owned-skills snapshot instead of a slot-only delta (`GameServer/Network/Handlers/SetSkillLoadoutSlotHandler.cs`, `GameServer/Services/SkillService.cs`).
- Assigning `PlayerSkillId = 0` clears the target slot; assigning a skill first removes duplicate loadout rows for that same skill and then inserts/replaces the target slot row (`GameServer/Services/SkillService.cs`).
- `SwapSkillLoadoutSlotsHandler` swaps two occupied rows using a temporary slot index, or moves a single occupied row into an empty slot; it returns the full snapshot after the mutation (`GameServer/Network/Handlers/SwapSkillLoadoutSlotsHandler.cs`, `GameServer/Services/SkillService.cs`).
- Combat uses `AttackEnemyHandler` -> `SkillExecutionService.ExecuteAttackEnemyAsync(...)` to resolve the equipped skill from the requested loadout slot, verify target/range, and execute skill effects against the selected enemy (`GameServer/Network/Handlers/AttackEnemyHandler.cs`, `GameServer/Runtime/SkillExecutionService.cs`).
- Skill execution reads the actual skill definition from `CombatDefinitionCatalog`; if a loadout row exists but the skill definition does not, execution fails instead of using fallback behavior (`GameServer/Runtime/SkillExecutionService.cs`, `GameServer/Runtime/CombatDefinitionCatalog.cs`).

# validations / guards
- Loadout slot indexes must be within `1..SkillMaxLoadoutSlotCount`; invalid values are rejected by packet validation and by service checks (`GameServer/Network/Handlers/SetSkillLoadoutSlotHandler.cs`, `GameServer/Network/Handlers/SwapSkillLoadoutSlotsHandler.cs`, `GameServer/Services/SkillService.cs`).
- `SetLoadoutSlotAsync(...)` rejects unknown `PlayerSkillId`, skills belonging to another player, and equipment-granted skills that currently fail availability checks (`GameServer/Services/SkillService.cs`).
- Equipment-granted skills are blocked from loadout assignment when the source equipment row is missing, the equipment definition no longer grants that skill, or the player has not reached the grant's required realm (`GameServer/Services/SkillService.cs`).
- `AttackEnemyHandler` requires `session.Player` to exist; combat execution then validates target existence and skill usability before applying effects (`GameServer/Network/Handlers/AttackEnemyHandler.cs`, `GameServer/Runtime/SkillExecutionService.cs`).
- Sync logic removes loadout rows for player-skill rows that are deleted as part of equipment-granted skill cleanup (`GameServer/Services/SkillService.cs`).

# config/data dependencies
- Config keys `character.starter_skill_id` and `skill.max_loadout_slot_count` directly affect starter ownership and loadout capacity (`GameServer/Config/GameConfigKeys.cs`, `GameServer/Config/GameConfigValues.cs`).
- Skill definitions, skill level ordering, and martial/combat metadata come from `CombatDefinitionCatalog` (`GameServer/Runtime/CombatDefinitionCatalog.cs`).
- Equipment-granted skill sync depends on item definitions containing `Equipment.SkillGrants` (`GameServer/Services/SkillService.cs`).
- Runtime persistence depends on player-skill rows, player-skill-loadout rows, inventory rows, and equipment rows (`GameServer/Services/SkillService.cs`).

# client/server touch points
- Client fetches owned skills with `GetOwnedSkillsPacket` / `GetOwnedSkillsResultPacket` (`GameServer/Network/Handlers/GetOwnedSkillsHandler.cs`).
- Client mutates loadout through `SetSkillLoadoutSlotPacket` and `SwapSkillLoadoutSlotsPacket`, each returning a full snapshot payload (`GameServer/Network/Handlers/SetSkillLoadoutSlotHandler.cs`, `GameServer/Network/Handlers/SwapSkillLoadoutSlotsHandler.cs`).
- Server-initiated skill snapshot refreshes use `OwnedSkillsChangedPacket`, mainly after equipment sync side effects (`GameServer/Runtime/SkillRuntimeNotifier.cs`, `GameServer/Services/EquipmentActionService.cs`).
- Combat entry point is `AttackEnemyPacket`; success/failure and combat consequences flow through the attack/runtime combat packet path rather than a separate skill-cast response packet in the inspected code (`GameServer/Network/Handlers/AttackEnemyHandler.cs`, `GameServer/Runtime/SkillExecutionService.cs`).

# edge cases
- Owned skill canonicalization can hide duplicate persisted rows from the client while the database still contains multiple sources for the same skill (`GameServer/Services/SkillService.cs`).
- A skill can remain owned but unavailable for loadout use if it is tied to equipment and the realm requirement is no longer satisfied (`GameServer/Services/SkillService.cs`).
- Clearing a slot is implemented by deleting the row if present; clearing an already-empty slot is treated as success/no-op (`GameServer/Services/SkillService.cs`).
- Slot swap logic uses a temporary slot index outside the normal configured range during persistence updates; this is internal and not exposed to clients (`GameServer/Services/SkillService.cs`).
- Equipment sync may remove stale equipment-granted skills and then also remove any loadout rows that referenced them, which can make loadout changes appear indirectly after equip/unequip (`GameServer/Services/SkillService.cs`, `GameServer/Services/EquipmentActionService.cs`).

# unclear or suspicious behavior
- `GetOwnedSkillsHandler` appears usable without `CharacterMustEnterWorld` checks, unlike several inventory/equipment handlers; if intentional, skill browsing is account/session-level rather than world-session-level (`GameServer/Network/Handlers/GetOwnedSkillsHandler.cs`).
- Canonical source selection in `SkillService` is non-trivial and may hide persistence anomalies rather than surfacing them (`GameServer/Services/SkillService.cs`).
- Equipment-granted skills are modeled as normal `PlayerSkillEntity` rows with a `SourcePlayerItemId`, so downstream code must remember to distinguish permanent ownership from equipment-derived ownership (`GameServer/Services/SkillService.cs`).
- Combat-time behavior depends heavily on definition data in `CombatDefinitionCatalog`; invalid/missing definitions mostly surface as unusable skills rather than being repaired during loadout reads (`GameServer/Runtime/SkillExecutionService.cs`, `GameServer/Services/SkillService.cs`).

# suggested canonical target docs
- `docs/combat/skill-ownership-and-loadout-runtime.md`
- `docs/combat/skill-execution-runtime.md`
- `docs/data-design/config-contracts/skills-runtime-config.md`
