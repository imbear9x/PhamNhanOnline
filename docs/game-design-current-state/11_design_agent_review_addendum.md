# 11. Design Agent Review Addendum

This file exists to make the current-state docs easier to use in actual design discussion. The codebase already has a lot of information, but a new Game Design Agent can still misread placeholder UI, local-only affordances, or partial backend foundations as fully implemented gameplay. This addendum narrows that gap.

## What is actually playable today

- Account login/register/reconnect is real and server-backed.
- Character creation and world entry are real, but current flow behaves like `1 account = 1 active character` for the current phase.
- Public world moment-to-moment loop is real: move, target, cast skill, kill enemies, receive rewards, pick up/drop items, switch map, switch zone.
- Progression loop is real: inventory, equipment, martial art selection, cultivation, breakthrough, potential allocation, alchemy practice sessions, notification-driven result claim.
- Private home instance matters today because cultivation and alchemy both depend on that home/private context rather than being global anywhere actions.

Source:
- `GameServer/Services/AccountService.cs`
- `GameServer/Services/CharacterService.cs`
- `GameServer/Services/WorldEntryService.cs`
- `GameServer/Runtime/GameLoop.cs`
- `GameServer/Runtime/WorldRuntimeSettlementService.cs`
- `GameServer/Runtime/CharacterCultivationService.cs`
- `GameServer/Services/AlchemyCraftActionService.cs`

## What looks available but is not safe to assume

- `Quest` and `Guild` tabs exist in the client menu, but they are placeholder text, not live systems.
- `Smithing` and `Talisman` stations can open a panel, but the panel intentionally shows placeholder messaging for unsupported stations.
- Local home station portals are client-local interaction shortcuts that open panels. They are not server-authenticated world travel or NPC content.
- Herb/garden/soil data and server services exist, but no player-facing packet + client loop was confirmed from the current playable path.
- `breakthrough_conditions` exists as schema/repository foundation, but no confirmed runtime enforcement was found in breakthrough resolution.

Source:
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCraftingPanelController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/LocalFixPortalPresenter.cs`
- `GameServer/Services/HerbService.cs`
- `GameServer/Repositories/BreakthroughConditionRepository.cs`
- `GameServer/Runtime/CharacterCultivationService.cs`

## Code term -> design term glossary

- `private home instance` / `home cave`: the player's private utility space. This is the current non-combat progression hub.
- `martial art`: a long-form progression discipline that affects cultivation behavior and may grant stats/skills. It is not the same as an instant combat skill.
- `skill`: the combat action data used in the active skill loadout and runtime cast pipeline.
- `practice session`: a time-based asynchronous production/progression job stored in DB and settled later by the server. Right now the visible end-to-end example is alchemy.
- `ground reward`: a server-spawned reward object in the world that can be picked up, distinct from a direct inventory grant.
- `potential`: spendable progression currency used for stat allocation.
- `breakthrough`: server-resolved realm advancement attempt after cultivation progress reaches its cap.

Source:
- `GameServer/Runtime/PracticeSystemTypes.cs`
- `GameServer/Services/SkillService.cs`
- `GameServer/Services/MartialArtService.cs`
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GameServer/Runtime/CharacterCultivationService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCultivationPanelController.cs`

## Current player fantasy that the code supports

- The player enters a shared cultivation-themed world, fights enemies, gets loot and progression resources, then returns to a private home context to deepen progression through cultivation and alchemy.
- This is currently more of a `combat -> resource gain -> private progression -> stronger combat` loop than a story-driven quest MMO.
- The code supports a strong PvE progression skeleton, but not yet a strong authored content skeleton such as quest chains, faction tracks, or guild goals.

Source:
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GameServer/Runtime/CharacterCultivationService.cs`
- `GameServer/Services/AlchemyCraftActionService.cs`
- `ClientUnity/PhamNhanOnline/docs/game-design-client-overview.md`

## Design levers that already exist today

- Combat numbers can be tuned through `skills`, `skill_effects`, enemy definition tables, reward tables, and player stat sources.
- Progression pacing can be tuned through `realm_templates`, map spiritual-energy data, cultivation config keys, potential tier tables, and breakthrough chance behavior.
- Economy/resource pacing can be tuned through item templates, drop rule tables, direct-grant vs ground-reward behavior, recipe inputs, recipe duration, and result definitions.
- Loadout expression already exists through skill ownership, skill loadout slots, equipment stat bonuses, equipment-granted skills, martial arts, and consumable item use.
- Zone and map density can be tuned through map, spawn-group, and zone-slot tables.

Source:
- `database/initDatabase.sql`
- `database/phamnhan_online.sql`
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/Services/EquipmentStatService.cs`
- `GameServer/Runtime/EnemyDefinitionCatalog.cs`
- `GameServer/Runtime/PotentialStatCatalog.cs`

## Guardrails before proposing new features

- If a proposal depends on quest/NPC/guild state, label it as new backend work unless you can point to an existing packet/service/table path.
- If a proposal depends on farming, herb planting, or garden UX, label it as `backend foundation exists but player-facing flow still needs implementation`.
- If a proposal changes combat feel, respect that combat resolution is server authoritative and loop settlement happens in runtime services, not only in Unity presentation.
- If a proposal changes movement or traversal, remember current server authority is mostly clamp/range/state based; obstacle-aware path authority was not confirmed.
- If a proposal assumes config hot-reload, note that current `game_configs` behavior appears startup-driven rather than a full live reload pipeline.
- If a proposal depends on DB bootstrap certainty, flag the current two-file schema setup as a prerequisite risk.

Source:
- `GameServer/Runtime/GameLoop.cs`
- `GameServer/Runtime/WorldInteractionGate.cs`
- `GameServer/Network/Middleware/RateLimitMiddleware.cs`
- `GameServer/Network/Handlers/TravelToMapHandler.cs`
- `GameServer/Program.cs`
- `database/phamnhan_online.sql`
- `database/initDatabase.sql`

## Immediate design conversations that are now unblocked

- What should be the next real meta-loop after combat/cultivation/alchemy: quest, farming, smithing, talisman, or social?
- Should the private home become a stronger lifestyle/crafting economy space, or remain a lightweight utility hub?
- How should direct grants versus ground drops be divided by content type and emotional intent?
- Should breakthroughs stay mostly chance-based, or should item preparation / condition fulfillment become a stronger strategic layer?
- Should the project commit to `1 character per account` for this phase, or invest early in a real character selection model?
- Should placeholder tabs and stations be hidden until their backend exists, or intentionally exposed as roadmap affordances?

Source:
- `GameServer/Services/CharacterService.cs`
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GameServer/Runtime/CharacterCultivationService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCraftingPanelController.cs`

## Recommended discussion style for the next Game Design Agent

- Separate `implemented loop`, `partial foundation`, and `new feature proposal` explicitly.
- For every proposed system, say whether it can reuse current tables/services or whether it requires new packets, server actions, DB tables, and client screens.
- Treat server code as the source of truth for success/fail conditions. Treat client UI as proof of visibility, not proof of authoritative gameplay.
- When unsure, mark `Need confirmation` instead of inferring from placeholder UI.

Source:
- `GameServer/Network/Handlers/*`
- `GameServer/Services/*`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/*`
