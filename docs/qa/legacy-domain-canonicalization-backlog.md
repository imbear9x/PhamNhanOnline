# Legacy domain canonicalization backlog

## Purpose

Turn `docs/qa/legacy-domain-coverage-audit.md` into an execution-oriented backlog for canonical documentation work. This is still planning/audit output, not canonicalization itself.

## Source

Primary source:
- `docs/qa/legacy-domain-coverage-audit.md`

Supporting evidence families:
- `GameServer/Services/`
- `GameServer/Runtime/`
- `GameServer/Network/Handlers/`
- `docs/implementation/extractions/`
- existing canonical docs under `docs/systems/`, `docs/maps/`, `docs/monsters/`, `docs/combat/`, `docs/inventory/`, `docs/rules/`, `docs/data-design/config-contracts/`

## Priority rubric

- `P0`: strong code evidence, important player-facing runtime, canonical coverage currently missing or too fragmented
- `P1`: important runtime area, but can follow after P0 cluster docs exist
- `P2`: narrower support domain, ops domain, or depends on earlier canonical docs
- `review-first`: existing canonical doc exists but should not be expanded blindly before conflict/review resolution

## Canonicalization backlog

| Priority | Domain | Why now | Minimum canonical target | Key evidence starting points | Dependency / sequencing notes |
|---|---|---|---|---|---|
| P0 | Player stats / final stats / state clamping | High runtime centrality; many systems depend on it; currently only extraction coverage | `docs/systems/player-stats-runtime.md` | `GameServer/Services/CharacterFinalStatService.cs`, `GameServer/Runtime/CharacterRuntimeService.cs`, `GameServer/Runtime/CharacterRuntimeCalculator.cs`, `GameServer/Runtime/CharacterRuntimeNotifier.cs`, `docs/implementation/extractions/player-stats-runtime-extraction.md` | Do before or together with cultivation and equipment canonicals. |
| P0 | Cultivation / breakthrough | Core progression loop; current knowledge lives in extraction + broad legacy inventory only | `docs/cultivation/cultivation-and-breakthrough-runtime.md` | `GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Services/CultivationActionService.cs`, `GameServer/Network/Handlers/StartCultivationHandler.cs`, `StopCultivationHandler.cs`, `BreakthroughHandler.cs`, `docs/implementation/extractions/player-stats-runtime-extraction.md` | Depends on player-stats terminology being settled. |
| P0 | Potential allocation | Directly coupled to progression and stats; currently no canonical doc | `docs/cultivation/potential-allocation-runtime.md` or section under player/cultivation runtime | `GameServer/Runtime/PotentialStatCatalog.cs`, `GameServer/Network/Handlers/AllocatePotentialHandler.cs`, `docs/implementation/extractions/player-stats-runtime-extraction.md` | Can be folded into cultivation or player-stats cluster if doc split is kept small. |
| P0 | Martial arts / active martial art runtime impact | Needed to explain cultivation eligibility and stat modifiers; currently missing | `docs/combat/martial-art-ownership-and-activation-runtime.md` | `GameServer/Services/MartialArtService.cs`, `GameServer/Services/MartialArtActionService.cs`, `GameServer/Runtime/MartialArtProgressionService.cs`, `GameServer/Network/Handlers/GetOwnedMartialArtsHandler.cs`, `SetActiveMartialArtHandler.cs`, `UseMartialArtBookHandler.cs` | Should align terminology with cultivation and player-stats docs. |
| P0 | Equipment runtime | Strong runtime evidence, player-facing, currently extraction-only | `docs/inventory/equipment-runtime.md` | `GameServer/Services/EquipmentService.cs`, `GameServer/Services/EquipmentActionService.cs`, `GameServer/Services/EquipmentStatService.cs`, `GameServer/Network/Handlers/EquipInventoryItemHandler.cs`, `UnequipInventoryItemHandler.cs`, `docs/implementation/extractions/equipment-runtime-extraction.md` | Do before or with skill-ownership/loadout doc because equipment grants skills. |
| P0 | Skill ownership / loadout / equipment-granted skills | Core gameplay surface; currently canonical combat doc does not cover ownership/loadout well | `docs/combat/skill-ownership-and-loadout-runtime.md` | `GameServer/Services/SkillService.cs`, `GameServer/Network/Handlers/GetOwnedSkillsHandler.cs`, `SetSkillLoadoutSlotHandler.cs`, `SwapSkillLoadoutSlotsHandler.cs`, `docs/implementation/extractions/skill-runtime-extraction.md` | Pair with equipment runtime doc. |
| P1 | Inventory runtime | Canonical item-use exists, but inventory listing/drop/refresh/ownership model is still fragmented | `docs/inventory/inventory-runtime.md` | `GameServer/Services/ItemService.cs`, `GameServer/Services/PlayerInventoryTransactionService.cs`, `GameServer/Network/Handlers/GetInventoryHandler.cs`, `DropInventoryItemHandler.cs` | Best written after equipment doc so slot semantics are stable. |
| P1 | Loot / ground reward / pickup | Strongly player-facing and referenced by enemy runtime, but not yet isolated canonically | `docs/inventory/ground-reward-and-loot-runtime.md` | `GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/Runtime/GroundItemRuntimeService.cs`, `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`, `DropInventoryItemHandler.cs` | Best after enemy/runtime and inventory terminology are stable. |
| P1 | Combat death recovery | Small but important runtime behavior; currently hidden in broad inventory doc | `docs/systems/combat-death-recovery-runtime.md` | `GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`, `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs` | Can be a short standalone canonical or a subsection under player state/runtime. |
| P1 | Character creation / bootstrap | Partial canonical coverage exists, but important seeded behaviors are not isolated | `docs/systems/character-creation-and-bootstrap-runtime.md` | `GameServer/Services/CharacterService.cs`, `GameServer/Services/CharacterCreationActionService.cs`, `GameServer/Network/Handlers/CreateCharacterHandler.cs`, `GetCharacterListHandler.cs`, `GetCharacterDataHandler.cs` | Should reference home cave, starter skill, and base/current state seed. |
| P1 | Session reconnect / resume | Existing phase-1 docs mention it, but contract details are not isolated | `docs/systems/session-reconnect-runtime.md` | `GameServer/Network/NetworkServer.cs`, `GameServer/Network/Handlers/ReconnectHandler.cs`, `GameServer/Config/GameConfigKeys.cs` | Can remain compact. |
| P1 | Alchemy runtime | Clear server surface exists; docs are still legacy/reference-heavy | `docs/alchemy/alchemy-runtime.md` | `GameServer/Services/AlchemyService.cs`, `AlchemyCraftQueryService.cs`, `AlchemyCraftActionService.cs`, `GameServer/Network/Handlers/GetLearnedPillRecipesHandler.cs`, `GetPillRecipeDetailHandler.cs`, `PreviewCraftPillHandler.cs`, `CraftPillHandler.cs`, `docs/reference-and-specs/game_design_luyen_dan.md` | Coordinate with practice-session doc because some flows hand off there. |
| P1 | Practice sessions | Distinct runtime surface; currently undocumented canonically | `docs/systems/practice-session-runtime.md` | `GameServer/Services/PracticeService.cs`, `GameServer/Network/Handlers/GetAlchemyPracticeStatusHandler.cs`, `PausePracticeHandler.cs`, `ResumePracticeHandler.cs`, `CancelPracticeHandler.cs`, `AcknowledgePracticeResultHandler.cs` | Best after alchemy draft outline or in parallel if kept scope-tight. |
| P1 | Notifications / inbox | Dedicated service + handler evidence exists; no canonical doc | `docs/systems/player-notifications-runtime.md` | `GameServer/Services/PlayerNotificationService.cs`, `GameServer/Network/Handlers/AcknowledgePlayerNotificationHandler.cs`, `GameServer/DTO/PlayerNotificationModelBuilder.cs` | Low dependency; good filler task between larger domains. |
| P2 | Home cave / garden / herb farming | Strong entity/service evidence, but live player-facing surface needs confirmation | `docs/systems/home-cave-and-herb-runtime.md` or split docs once scope is verified | `GameServer/Services/HerbService.cs`, `GameServer/Services/CharacterService.cs`, `GameServer/Entities/PlayerCaveEntity.cs`, `PlayerGardenPlotEntity.cs`, `PlayerHerbEntity.cs`, `SoilTemplateEntity.cs` | First confirm actual packet/UI/runtime entry points. |
| P2 | Description template system | Reference/spec exists; not yet clearly critical as canonical runtime behavior | `docs/systems/description-template-runtime.md` if promoted from reference | `GameServer/Descriptions/DescriptionTemplateCompiler.cs`, `GameServer/Descriptions/GameplayDescriptionService.cs`, `docs/reference-and-specs/DESCRIPTION_TEMPLATE_SYSTEM.md` | Only promote if downstream systems treat descriptions as runtime contract. |
| P2 | Random tables / reward RNG | Important support system, but usually secondary to loot/enemy docs | `docs/data-design/random-table-runtime-contract.md` | `GameServer/Randomness/GameRandomService.cs`, `GameServer/Entities/GameRandom*`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/Runtime/CharacterCultivationService.cs` | Best after loot/enemy docs establish where RNG matters. |
| P2 | Diagnostics / metrics / observability | Evidence exists but may belong to ops docs rather than gameplay canonicals | `docs/operations/server-observability-runtime.md` if included in scope | `GameServer/Diagnostics/ServerMetricsService.cs`, `GameServer/Diagnostics/ServerMetricsLoggerService.cs` | Decide knowledge-owner scope before investing. |
| review-first | Portal travel | Canonical doc exists, but conflict docs show unresolved interpretation gaps | Existing: `docs/maps/portal-travel-runtime.md` | `GameServer/Network/Handlers/TravelToMapHandler.cs`, `docs/conflicts/portal-interaction-mode-runtime-gap.md`, `docs/conflicts/map-travel-topology-vs-portal-semantics.md` | Resolve/accept conflicts before major doc expansion. |
| review-first | Enemy runtime | Canonical doc exists but has open scope/reset questions | Existing: `docs/monsters/enemy-runtime-batch1.md` | `GameServer/Runtime/EnemyDefinitionCatalog.cs`, `GameServer/World/MapInstance.*.cs`, `docs/conflicts/enemy-runtime-scope-and-reset-open-questions.md` | Review conflict notes before moving to deeper batch. |
| review-first | Generic item use | Canonical exists, but notifier ordering is still flagged | Existing: `docs/inventory/item-use-flow.md` | `GameServer/Services/ItemUseService.cs`, `GameServer/Network/Handlers/UseItemHandler.cs`, `docs/conflicts/item-use-notifier-ordering-review.md` | Review ordering issue before declaring fully stable. |

## Suggested execution batches

### Batch A — progression and stat core
- Player stats / final stats / state clamping
- Cultivation / breakthrough
- Potential allocation
- Martial arts / active martial art runtime impact

### Batch B — inventory-combat ownership bridge
- Equipment runtime
- Skill ownership / loadout / equipment-granted skills
- Inventory runtime

### Batch C — combat aftermath and utility systems
- Loot / ground reward / pickup
- Combat death recovery
- Notifications / inbox
- Session reconnect / resume

### Batch D — crafting and long-running actions
- Alchemy runtime
- Practice sessions

### Batch E — lower-confidence or support domains
- Home cave / garden / herb farming
- Description template system
- Random tables / reward RNG
- Diagnostics / metrics / observability

### Review lane (not normal batch work)
- Portal travel conflict resolution
- Enemy runtime follow-up review
- Generic item-use notifier ordering review

## Recommended final target set

If the goal is a practical “good enough canonical baseline”, the minimum target set should be:

1. `docs/systems/player-stats-runtime.md`
2. `docs/cultivation/cultivation-and-breakthrough-runtime.md`
3. `docs/combat/martial-art-ownership-and-activation-runtime.md`
4. `docs/inventory/equipment-runtime.md`
5. `docs/combat/skill-ownership-and-loadout-runtime.md`
6. `docs/inventory/inventory-runtime.md`
7. `docs/inventory/ground-reward-and-loot-runtime.md`
8. `docs/alchemy/alchemy-runtime.md`
9. `docs/systems/practice-session-runtime.md`
10. `docs/systems/player-notifications-runtime.md`

## Notes

- This backlog assumes extraction notes remain supporting evidence, not final deliverables.
- `docs/game-design-current-state/02_system_inventory.md` remains useful as a discovery index, but should not be treated as a substitute for canonical runtime docs.
- Review-lane items should not be “completed by writing more docs” until the underlying interpretation gap is resolved or deliberately accepted.
