# title
Player stats runtime extraction

# scope
Server-side character base/final stat assembly, current-state clamping, cultivation/progression stat mutations, and outbound stat/state sync paths. Focused on current runtime behavior in `GameServer`, not intended balance/design.

# source files
- `GameServer/Services/CharacterService.cs`
- `GameServer/Services/CharacterFinalStatService.cs`
- `GameServer/Services/CultivationActionService.cs`
- `GameServer/Runtime/CharacterCultivationService.cs`
- `GameServer/Runtime/PotentialStatCatalog.cs`
- `GameServer/Runtime/CharacterRuntimeService.cs`
- `GameServer/Runtime/CharacterRuntimeCalculator.cs`
- `GameServer/Runtime/CharacterRuntimeNotifier.cs`
- `GameServer/Services/EquipmentStatService.cs`
- `GameServer/Runtime/CombatDefinitionCatalog.cs`
- `GameServer/Network/Handlers/GetCharacterDataHandler.cs`
- `GameServer/Network/Handlers/StartCultivationHandler.cs`
- `GameServer/Network/Handlers/AllocatePotentialHandler.cs`
- `GameServer/Network/Handlers/BreakthroughHandler.cs`
- `GameServer/DTO/NetworkModelMapper.cs`
- `GameServer/Config/GameConfigKeys.cs`

# current runtime behavior
- Character creation builds default base stats/current state, persists them, then enriches base stats with realm display metadata and potential upgrade previews before returning the snapshot (`GameServer/Services/CharacterService.cs`).
- `GetCharacterDataHandler` runs snapshot settlement for offline cultivation, then lifecycle prep, then `CharacterFinalStatService.ApplyFinalStatsToSnapshotAsync(...)` before sending `GetCharacterDataResultPacket` (`GameServer/Network/Handlers/GetCharacterDataHandler.cs`).
- Final stats are recomputed from raw base stats plus three modifier sources: allocated potential bonuses, equipped-item stat modifiers, and active martial-art stage bonuses (`GameServer/Services/CharacterFinalStatService.cs`, `GameServer/Services/EquipmentStatService.cs`, `GameServer/Runtime/PotentialStatCatalog.cs`, `GameServer/Runtime/CombatDefinitionCatalog.cs`).
- Integer stats (`hp/mp/stamina/attack/speed/sense`) apply raw base + potential flat bonus + percent modifier bonus + flat modifier bonus; percent values above `1` are normalized as `value / 100` and truncated toward zero (`GameServer/Services/CharacterFinalStatService.cs`).
- Luck is handled separately as `double`, with flat and percent bonuses accumulated without integer truncation until final assignment (`GameServer/Services/CharacterFinalStatService.cs`).
- After any authoritative base-stat mutation, `CharacterRuntimeService.ApplyBaseStatsMutation(...)` clamps current HP/MP/Stamina to the new maxima and notifies both base-stat and current-state changes (`GameServer/Runtime/CharacterRuntimeService.cs`, `GameServer/Runtime/CharacterRuntimeCalculator.cs`, `GameServer/Runtime/CharacterRuntimeNotifier.cs`).
- Cultivation settlement increases `Cultivation`, may increase `UnallocatedPotential`, and can automatically stop cultivation at realm cap by switching runtime state back to `Idle` and clearing cultivation timestamps (`GameServer/Runtime/CharacterCultivationService.cs`).
- Breakthrough success only changes `RealmTemplateId` and clears `PotentialRewardLocked`; failure applies a cultivation penalty path and pushes updated base stats without changing final stats until later final-stat recompute (`GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Services/CultivationActionService.cs`).
- Potential allocation spends `UnallocatedPotential`, increments one of the upgrade-count fields, refreshes preview data, then `CultivationActionService` re-applies authoritative final stats before replying on success (`GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Runtime/PotentialStatCatalog.cs`, `GameServer/Services/CultivationActionService.cs`).
- Damage/resource changes operate on current state only; `CharacterRuntimeCalculator` clamps HP/MP/Stamina and flips state to `CombatDead` or `LifespanExpired` based on resulting HP and expiry flags (`GameServer/Runtime/CharacterRuntimeCalculator.cs`).

# validations / guards
- `CharacterRuntimeService.AttachPlayerSession(...)` throws if snapshot base stats or current state are missing at world attach time (`GameServer/Runtime/CharacterRuntimeService.cs`).
- `CharacterFinalStatService.ApplyFinalStatsToSnapshotAsync(...)` is a no-op if snapshot base stats or current state are null (`GameServer/Services/CharacterFinalStatService.cs`).
- Active martial-art stat bonuses are skipped when `ActiveMartialArtId` is missing/invalid, player progress row is missing, or catalog lookup fails (`GameServer/Services/CharacterFinalStatService.cs`).
- Potential allocation rejects unsupported targets, unavailable tiers, non-positive requested amounts, insufficient potential, and requests that cross beyond the current tier window (`GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Runtime/PotentialStatCatalog.cs`).
- Cultivation start is blocked unless the player is in a private home instance, not expired, not already cultivating, not blocked by practice, and has an active martial art with a resolvable absorption rate (`GameServer/Runtime/CharacterCultivationService.cs`).
- Breakthrough requires a valid current realm, current cultivation at or above realm cap, and existence of the next realm (`GameServer/Runtime/CharacterCultivationService.cs`).
- Stat recompute uses `checked(...)` arithmetic for several integer final stats, so overflow would throw instead of silently wrapping (`GameServer/Services/CharacterFinalStatService.cs`).

# config/data dependencies
- Realm metadata and breakthrough caps/rates come from realm-template rows resolved by `CharacterService.EnrichBaseStatsAsync(...)` and cultivation/breakthrough paths (`GameServer/Services/CharacterService.cs`, `GameServer/Runtime/CharacterCultivationService.cs`).
- Potential-upgrade tiers are loaded at startup from `PotentialStatUpgradeTierRepository` and grouped by `PotentialAllocationTarget` (`GameServer/Runtime/PotentialStatCatalog.cs`).
- Martial-art stage stat bonuses and Qi absorption rate come from `CombatDefinitionCatalog` (`GameServer/Runtime/CombatDefinitionCatalog.cs`).
- Equipment stat modifiers depend on player inventory, equipment rows, optional persisted equipment bonus rows, and item definitions (`GameServer/Services/EquipmentStatService.cs`).
- Config keys `cultivation.potential_per_cultivation_point` and `cultivation.settlement_interval_seconds` directly affect cultivation-driven stat/progression changes (`GameServer/Config/GameConfigKeys.cs`, `GameServer/Runtime/CharacterCultivationService.cs`).

# client/server touch points
- `GetCharacterDataResultPacket` sends full `CharacterModel`, `CharacterBaseStatsModel`, and `CharacterCurrentStateModel` after settlement/final-stat recompute (`GameServer/Network/Handlers/GetCharacterDataHandler.cs`, `GameServer/DTO/NetworkModelMapper.cs`).
- Incremental base-stat updates go through `CharacterBaseStatsChangedPacket`; incremental current-state updates go through `CharacterCurrentStateChangedPacket` (`GameServer/Runtime/CharacterRuntimeNotifier.cs`, `GameShared/Packets/Packets/CharacterPackets.cs`).
- Cultivation actions reply with `StartCultivationResultPacket`, `BreakthroughResultPacket`, `AllocatePotentialResultPacket`, and may additionally emit `CultivationRewardsGrantedPacket` during settlement (`GameServer/Network/Handlers/StartCultivationHandler.cs`, `GameServer/Network/Handlers/BreakthroughHandler.cs`, `GameServer/Network/Handlers/AllocatePotentialHandler.cs`, `GameShared/Packets/Packets/CharacterPackets.cs`).
- `NetworkModelMapper.ToModel(CharacterBaseStatsDto)` exposes both raw and final stats, breakthrough chance percent, active martial art, and potential upgrade previews to the client (`GameServer/DTO/NetworkModelMapper.cs`).

# edge cases
- If final stats already match the runtime snapshot, `ApplyAuthoritativeFinalStatsAsync(...)` returns early and does not persist or notify (`GameServer/Services/CharacterFinalStatService.cs`).
- Base stats can be enriched with empty realm display data and `HasNextRealm = false` when `RealmTemplateId` is absent or the realm row is missing (`GameServer/Services/CharacterService.cs`).
- Cultivation reward settlement can occur both for online players and for offline snapshots loaded through `GetCharacterDataHandler` (`GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Network/Handlers/GetCharacterDataHandler.cs`).
- Reaching realm cap during settlement zeroes `CultivationProgress` and exits cultivation immediately instead of leaving the character in cultivating state at cap (`GameServer/Runtime/CharacterCultivationService.cs`).
- Potential allocation is tier-local per request: even if the user asks for a large amount, applied upgrades are capped by remaining upgrades in the current tier (`GameServer/Runtime/CharacterCultivationService.cs`).

# unclear or suspicious behavior
- `CharacterCultivationService.BreakthroughAsync(...)` updates runtime base stats directly and not through `CharacterFinalStatService`; only the action-service wrapper recomputes final stats for successful action replies, so other callers would need to handle final-stat reconciliation themselves (`GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Services/CultivationActionService.cs`).
- Breakthrough failure updates base stats and notifies immediately, but the exact penalty semantics depend on helper code deeper in `CharacterCultivationService` rather than a separate explicit rules object (`GameServer/Runtime/CharacterCultivationService.cs`).
- Potential bonuses are added into final stats while the original base stat fields remain unchanged; client receives both raw and final values, so downstream consumers must choose the correct field (`GameServer/Services/CharacterFinalStatService.cs`, `GameServer/DTO/NetworkModelMapper.cs`).
- Percent-like values accept both `0..1` and `0..100` style inputs via normalization. That is intentional in code, but it also means data-format inconsistency can be masked rather than surfaced (`GameServer/Services/CharacterFinalStatService.cs`, `GameServer/Services/CharacterService.cs`).

# suggested canonical target docs
- `docs/systems/player-stats-runtime.md`
- `docs/cultivation/cultivation-and-breakthrough-runtime.md`
- `docs/data-design/config-contracts/player-progression-stats-config.md`
