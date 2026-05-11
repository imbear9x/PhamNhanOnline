# Legacy domain coverage audit

## Scope

Audit coverage giữa domain hiện có trong server code và canonical docs hiện có. Chỉ tính domain có evidence trực tiếp trong codebase đã rà. Không suy domain từ chat hoặc wishlist.

## Evidence baseline

Code scanned:
- `GameServer/Network/Handlers/`
- `GameServer/Services/`
- `GameServer/Runtime/`
- selected entities/repositories under `GameServer/Entities/` and `GameServer/Repositories/`
- supporting packet/model surface under `GameShared/Packets/` and `GameShared/Models/`

Docs scanned:
- `docs/systems/`
- `docs/maps/`
- `docs/monsters/`
- `docs/combat/`
- `docs/inventory/`
- `docs/loot/`
- `docs/progression/`
- `docs/alchemy/`
- `docs/rules/`
- `docs/data-design/config-contracts/`
- `docs/conflicts/`
- `docs/implementation/extractions/`
- `docs/game-design-wp/clarifications/`

## Coverage table

| Domain | Code evidence | Existing canonical docs | Coverage status | Recommended next action |
|---|---|---|---|---|
| Auth / account / login / register | `GameServer/Services/AccountService.cs`, `GameServer/Services/AccountActionService.cs`, `GameServer/Network/Handlers/LoginHandler.cs`, `GameServer/Network/Handlers/RegisterHandler.cs` | `docs/systems/auth-character-world-phase1.md`, `docs/systems/phase1-runtime-flow.md` | good | Keep current phase-1 docs as canonical baseline. |
| Character creation / character snapshot bootstrap | `GameServer/Services/CharacterService.cs`, `GameServer/Services/CharacterCreationActionService.cs`, `GameServer/Network/Handlers/CreateCharacterHandler.cs`, `GameServer/Network/Handlers/GetCharacterListHandler.cs`, `GameServer/Network/Handlers/GetCharacterDataHandler.cs` | `docs/systems/auth-character-world-phase1.md`, `docs/systems/character-creation-and-bootstrap-runtime.md`, extraction support in `docs/implementation/extractions/character-bootstrap-runtime-extraction.md` | good | Maintain doc and extend only if starter resources or bootstrap shape changes. |
| Session reconnect / resume | `GameServer/Network/NetworkServer.cs`, `GameServer/Network/Handlers/ReconnectHandler.cs`, `GameServer/Config/GameConfigKeys.cs` | `docs/systems/reconnect-and-session-resume-runtime.md`, `docs/systems/auth-character-world-phase1.md`, extraction support in `docs/implementation/extractions/reconnect-runtime-extraction.md` | good | Keep as canonical session-resume reference. |
| World entry / world snapshot publish | `GameServer/Services/WorldEntryService.cs`, `GameServer/Runtime/CharacterRuntimeService.cs`, `GameServer/World/WorldInterestService.cs`, `GameServer/Network/Handlers/EnterWorldHandler.cs` | `docs/systems/auth-character-world-phase1.md`, `docs/maps/map-instance-and-world-entry-runtime.md`, `docs/systems/phase1-runtime-flow.md` | good | Maintain current docs. |
| Map instances / world membership / zone selection | `GameServer/World/MapCatalog.cs`, `GameServer/World/MapManager.cs`, `GameServer/Runtime/MapInstanceLifecycleService.cs`, `GameServer/World/WorldInterestService.cs` | `docs/maps/map-instance-and-world-entry-runtime.md`, `docs/rules/world-instance-membership-invariant.md`, `docs/data-design/config-contracts/world-map-runtime-configs-batch1.md` | good | Keep as canonical world-runtime baseline. |
| Portal travel | `GameServer/Network/Handlers/TravelToMapHandler.cs`, `GameServer/Runtime/WorldTargetResolver.cs`, `GameServer/Network/Validations/TravelToMapPacketValidator.cs`, `GameServer/Entities/MapPortalEntity.cs` | `docs/maps/portal-travel-runtime.md`, `docs/conflicts/portal-interaction-mode-runtime-gap.md`, `docs/conflicts/map-travel-topology-vs-portal-semantics.md`, `docs/game-design-wp/clarifications/portal-design-clarification.md` | needs-review | Keep current canonical doc, but do not mark stable until portal interaction mode and topology semantics are explicitly resolved. |
| World movement / observer sync | `GameServer/Network/Handlers/CharacterPositionSyncHandler.cs`, `GameServer/Runtime/GameLoop.cs`, `GameServer/World/WorldInterestService.cs` | `docs/systems/world-observer-and-movement-runtime.md`, `docs/rules/client-state-sync-runtime.md`, `docs/systems/world-scene-readiness-runtime.md` | good | Keep current canonical docs. |
| Enemy runtime / spawn / patrol / death / rewards | `GameServer/Runtime/EnemyDefinitionCatalog.cs`, `GameServer/World/MapInstance.Runtime.cs`, `GameServer/World/MapInstance.Combat.cs`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/World/MonsterEntity.cs` | `docs/monsters/enemy-runtime-batch1.md`, `docs/conflicts/enemy-runtime-scope-and-reset-open-questions.md`, `docs/game-design-wp/clarifications/enemy-design-clarification.md`, extraction support in `docs/implementation/extractions/enemy-runtime-extraction.md` | needs-review | Keep canonical batch-1 doc, but unresolved spawn/reset semantics still block full closure. |
| Combat skill execution | `GameServer/Network/Handlers/AttackEnemyHandler.cs`, `GameServer/Runtime/SkillExecutionService.cs`, `GameServer/Runtime/WorldRuntimeSettlementService.cs`, `GameServer/Runtime/CombatDefinitionCatalog.cs` | `docs/combat/skill-combat-runtime.md`, extraction support in `docs/implementation/extractions/combat-skill-execution-runtime-extraction.md` | good | Current canonical combat runtime is now sufficient; update only when execution pipeline changes. |
| Skill ownership / loadout / equipment-granted skills | `GameServer/Services/SkillService.cs`, `GameServer/Network/Handlers/GetOwnedSkillsHandler.cs`, `GameServer/Network/Handlers/SetSkillLoadoutSlotHandler.cs`, `GameServer/Network/Handlers/SwapSkillLoadoutSlotsHandler.cs` | `docs/combat/skill-ownership-and-loadout-runtime.md`, extraction support in `docs/implementation/extractions/skill-runtime-extraction.md` | good | Keep dedicated ownership/loadout doc linked from combat docs. |
| Inventory / item read model / inventory transactions | `GameServer/Services/ItemService.cs`, `GameServer/Services/PlayerInventoryTransactionService.cs`, `GameServer/Network/Handlers/GetInventoryHandler.cs`, `GameServer/Network/Handlers/DropInventoryItemHandler.cs` | `docs/inventory/inventory-runtime.md`, `docs/inventory/item-use-flow.md`, `docs/rules/server-transaction-boundary.md`, extraction support in `docs/implementation/extractions/inventory-runtime-extraction.md` | good | Maintain as canonical inventory runtime set. |
| Equipment / equipment slots / stat modifiers | `GameServer/Services/EquipmentService.cs`, `GameServer/Services/EquipmentActionService.cs`, `GameServer/Services/EquipmentStatService.cs`, `GameServer/Network/Handlers/EquipInventoryItemHandler.cs`, `GameServer/Network/Handlers/UnequipInventoryItemHandler.cs` | `docs/inventory/equipment-runtime.md`, extraction support in `docs/implementation/extractions/equipment-runtime-extraction.md` | good | Maintain equipment runtime doc and cross-link from stats/skills docs. |
| Generic item use | `GameServer/Services/ItemUseService.cs`, `GameServer/Network/Handlers/UseItemHandler.cs`, `GameServer/Services/PlayerInventoryTransactionService.cs` | `docs/inventory/item-use-flow.md`, `docs/conflicts/item-use-notifier-ordering-review.md` | needs-review | Runtime flow is documented, but notifier ordering remains an explicit open conflict. |
| Player stats / final stat recompute / state clamping | `GameServer/Services/CharacterFinalStatService.cs`, `GameServer/Runtime/CharacterRuntimeService.cs`, `GameServer/Runtime/CharacterRuntimeCalculator.cs`, `GameServer/Runtime/CharacterRuntimeNotifier.cs` | `docs/systems/player-stats-runtime.md`, extraction support in `docs/implementation/extractions/player-stats-runtime-extraction.md` | good | Keep as canonical stats runtime baseline. |
| Martial arts / active martial art progression hooks | `GameServer/Services/MartialArtService.cs`, `GameServer/Services/MartialArtActionService.cs`, `GameServer/Runtime/MartialArtProgressionService.cs`, `GameServer/Network/Handlers/GetOwnedMartialArtsHandler.cs`, `GameServer/Network/Handlers/SetActiveMartialArtHandler.cs`, `GameServer/Network/Handlers/UseMartialArtBookHandler.cs` | `docs/progression/martial-art-ownership-and-activation-runtime.md`, extraction support in `docs/implementation/extractions/martial-arts-runtime-extraction.md` | good | Maintain as canonical martial-art runtime doc. |
| Cultivation / breakthrough | `GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Services/CultivationActionService.cs`, `GameServer/Network/Handlers/StartCultivationHandler.cs`, `GameServer/Network/Handlers/StopCultivationHandler.cs`, `GameServer/Network/Handlers/BreakthroughHandler.cs` | `docs/progression/cultivation-breakthrough-and-potential-runtime.md`, extraction support in `docs/implementation/extractions/cultivation-runtime-extraction.md` | good | Maintain together with potential allocation in progression runtime docs. |
| Potential allocation | `GameServer/Runtime/PotentialStatCatalog.cs`, `GameServer/Network/Handlers/AllocatePotentialHandler.cs` | `docs/progression/cultivation-breakthrough-and-potential-runtime.md`, `docs/systems/player-stats-runtime.md`, extraction support in `docs/implementation/extractions/player-stats-runtime-extraction.md`, `docs/implementation/extractions/cultivation-runtime-extraction.md` | good | Keep as canonical subsection within progression/stats docs. |
| Combat death recovery / return home | `GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`, `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs` | `docs/combat/combat-death-and-return-home-runtime.md`, extraction support in `docs/implementation/extractions/combat-death-recovery-runtime-extraction.md` | good | Keep current canonical death-recovery doc. |
| Loot / ground reward / pickup | `GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/Runtime/GroundItemRuntimeService.cs`, `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`, `GameServer/Network/Handlers/DropInventoryItemHandler.cs` | `docs/loot/ground-reward-runtime.md`, extraction support in `docs/implementation/extractions/loot-ground-reward-runtime-extraction.md` | good | Maintain current canonical loot doc. |
| Notifications / inbox / acknowledgement | `GameServer/Services/PlayerNotificationService.cs`, `GameServer/Network/Handlers/AcknowledgePlayerNotificationHandler.cs`, `GameServer/DTO/PlayerNotificationModelBuilder.cs` | `docs/systems/player-notification-runtime.md`, extraction support in `docs/implementation/extractions/notifications-runtime-extraction.md` | good | Maintain notification runtime doc. |
| Practice sessions / pause-resume-cancel / result acknowledgement | `GameServer/Services/PracticeService.cs`, `GameServer/Network/Handlers/GetAlchemyPracticeStatusHandler.cs`, `GameServer/Network/Handlers/PausePracticeHandler.cs`, `GameServer/Network/Handlers/ResumePracticeHandler.cs`, `GameServer/Network/Handlers/CancelPracticeHandler.cs`, `GameServer/Network/Handlers/AcknowledgePracticeResultHandler.cs` | `docs/systems/practice-session-runtime.md`, `docs/game-design-wp/clarifications/practice-session-design-clarification.md`, extraction support in `docs/implementation/extractions/practice-sessions-runtime-extraction.md` | partial | Canonical lifecycle doc exists, but broader intended taxonomy/scope is still unresolved. Keep partial until design scope is explicit. |
| Alchemy crafting / recipe preview / craft start | `GameServer/Services/AlchemyService.cs`, `GameServer/Services/AlchemyCraftQueryService.cs`, `GameServer/Services/AlchemyCraftActionService.cs`, `GameServer/Network/Handlers/GetLearnedPillRecipesHandler.cs`, `GameServer/Network/Handlers/GetPillRecipeDetailHandler.cs`, `GameServer/Network/Handlers/PreviewCraftPillHandler.cs`, `GameServer/Network/Handlers/CraftPillHandler.cs` | `docs/alchemy/alchemy-recipe-and-craft-runtime.md`, `docs/game-design-wp/clarifications/alchemy-required-herb-maturity-clarification.md`, extraction support in `docs/implementation/extractions/alchemy-runtime-extraction.md` | good | Canonical doc is sufficient for current runtime because deferred `required_herb_maturity` behavior is explicitly documented. |
| Home cave / garden / herb farming | `GameServer/Services/HerbService.cs`, `GameServer/Services/CharacterService.cs`, `GameServer/Entities/PlayerGardenPlotEntity.cs`, `GameServer/Entities/PlayerHerbEntity.cs`, `GameServer/Entities/SoilTemplateEntity.cs` | `docs/game-design-wp/clarifications/home-cave-garden-herb-design-clarification.md`, extraction support in `docs/implementation/extractions/home-cave-garden-herb-runtime-extraction.md` | needs-review | Server-side system exists, but live packet/UI accessibility is still unverified; do not claim player-facing canonical coverage yet. |
| Descriptions / template text compilation | `GameServer/Descriptions/DescriptionTemplateCompiler.cs`, `GameServer/Descriptions/GameplayDescriptionService.cs` | `docs/reference-and-specs/DESCRIPTION_TEMPLATE_SYSTEM.md`, extraction support in `docs/implementation/extractions/description-template-runtime-extraction.md` | partial | Decide whether this should become second-brain canonical runtime knowledge or remain reference/spec only. |
| Game config contract surface | `GameServer/Config/GameConfigKeys.cs`, `GameServer/Config/GameConfigValues.cs`, `GameServer/Repositories/GameConfigRepository.cs` | `docs/data-design/config-contracts/game-configs-phase1.md`, `docs/data-design/config-contracts/world-map-runtime-configs-batch1.md` | good | Continue extending config-contract docs alongside surfaced domains. |
| Server validation / transaction / runtime rule layer | packet validators under `GameServer/Network/Validations/`, transaction wrapper `GameServer/Services/PlayerInventoryTransactionService.cs`, handler guard patterns across `GameServer/Network/Handlers/` | `docs/rules/server-validation-and-runtime-rules.md`, `docs/rules/server-transaction-boundary.md`, `docs/rules/world-instance-membership-invariant.md` | good | Keep as shared rule docs. |
| Random tables / reward RNG | `GameServer/Randomness/GameRandomService.cs`, `GameServer/Entities/GameRandom*`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/Runtime/CharacterCultivationService.cs` | `docs/rules/random-table-and-luck-runtime.md`, extraction support in `docs/implementation/extractions/randomness-runtime-extraction.md` | good | Maintain random-table/luck doc as canonical rule layer. |
| Metrics / diagnostics / server observability | `GameServer/Diagnostics/ServerMetricsService.cs`, `GameServer/Diagnostics/ServerMetricsLoggerService.cs` | extraction support in `docs/implementation/extractions/diagnostics-runtime-extraction.md` | partial | Decide whether observability belongs in second-brain canonical set or should stay ops-only; no canonical target exists yet. |

## Summary by status

- `good`: auth/account, character bootstrap, reconnect, world entry, map instances, movement/observer sync, combat skill execution, skill ownership/loadout, inventory, equipment, player stats, martial arts, cultivation, potential allocation, combat death recovery, loot, notifications, alchemy, config contracts, server validation/runtime rules, random tables/luck
- `partial`: practice sessions, description templates, metrics/diagnostics
- `needs-review`: portal travel, enemy runtime, generic item use, home cave/garden/herb
- `missing`: none in the current code-evidenced audit

## Domains requiring GameDesign clarification

- `portal travel`: portal interaction mode and topology semantics are still unresolved.
- `enemy runtime`: objective/manual spawn mode intent and boss reset semantics remain open.
- `practice sessions`: generic runtime exists, but intended multi-type scope vs alchemy-only scope is still unclear.
- `home cave / garden / herb farming`: server-side mechanics exist, but player-facing accessibility and intended UX are still unverified.
- `description templates`: needs a scope decision on whether this is canonical gameplay knowledge or reference-only infrastructure.
- `metrics / diagnostics`: needs a scope decision on whether observability belongs in second-brain canonical docs.

## Domains ready for direct canonicalization

Already canonicalized in this pass:
- `character creation / bootstrap`
- `session reconnect / resume`
- `combat skill execution`
- `skill ownership / loadout`
- `inventory runtime`
- `equipment runtime`
- `player stats / final stat recompute`
- `martial arts ownership / activation`
- `cultivation / breakthrough / potential allocation`
- `combat death recovery`
- `loot / ground reward / pickup`
- `notifications`
- `alchemy recipe / craft flow`
- `random table / luck runtime`

Still not ready for full closure:
- `practice sessions`
- `home cave / garden / herb farming`
- `portal travel`
- `enemy runtime`
- `generic item use`
- `description templates`
- `metrics / diagnostics`

## Notes

- Extraction notes in `docs/implementation/extractions/` were treated as supporting evidence, not as canonical coverage by themselves.
- Clarification notes in `docs/game-design-wp/clarifications/` were used only where the audit showed a real code-evidenced ambiguity.
- No additional code-evidenced domain remains completely outside the current knowledge set: every listed domain now has either canonical docs, extraction notes, clarification notes, or conflict artifacts.
