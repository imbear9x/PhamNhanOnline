# title
Character bootstrap runtime extraction

# scope
Character creation, starter persistence rows, home cave/garden seeding, starter skill grant, and snapshot bootstrap paths in `GameServer`.

# source files
- `GameServer/Services/CharacterService.cs`
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/Config/GameConfigValues.cs`
- `GameServer/Network/Handlers/CreateCharacterHandler.cs`
- `GameServer/Network/Handlers/GetCharacterListHandler.cs`
- `GameServer/Network/Handlers/GetCharacterDataHandler.cs`
- `GameServer/Services/HerbService.cs`

# current runtime behavior
- `CharacterService.CreateCharacterAsync` enforces one character per account, normalizes name, creates `Character`, base stats, and current state rows, then seeds home cave/garden and starter resources inside one DB transaction (`GameServer/Services/CharacterService.cs`).
- Default current state uses home-map default zone/spawn from `MapCatalog.ResolveHomeDefinition()` and current game-time snapshot for lifespan initialization (`GameServer/Services/CharacterService.cs`).
- `EnsureHomeCaveAsync` creates one home cave row and `character.home_garden_plot_count` garden plots if none exist (`GameServer/Services/CharacterService.cs`, `GameServer/Config/GameConfigKeys.cs`).
- `InitializeCharacterStarterResourcesAsync` currently only calls `EnsureStarterSkillAsync`; if `character.starter_skill_id` is `<= 0`, no starter skill is granted (`GameServer/Services/CharacterService.cs`, `GameServer/Config/GameConfigValues.cs`).
- `GetCharacterDataHandler` loads the persisted snapshot and returns it as the pre-world character bootstrap payload; it is not the same as world-entry runtime publish (`GameServer/Network/Handlers/GetCharacterDataHandler.cs`).
- `HerbService.EnsureHomeCaveAsync` duplicates the same home-cave/garden seeding behavior as a reusable service path (`GameServer/Services/HerbService.cs`).

# validations / guards
- Character creation fails if the account already owns a character or the normalized name is not unique (`GameServer/Services/CharacterService.cs`).
- Home cave seeding only happens when no home cave exists for that owner (`GameServer/Services/CharacterService.cs`, `GameServer/Services/HerbService.cs`).
- Starter skill grant depends on `CharacterStarterSkillId` being positive and resolvable (`GameServer/Services/CharacterService.cs`).
- Snapshot load by account returns `null` if the requested character is not owned by the account (`GameServer/Services/CharacterService.cs`).

# config/data dependencies
- Config: `network.reconnect_resume_window_seconds`, `character.home_garden_plot_count`, `character.starter_skill_id` (`GameServer/Config/GameConfigKeys.cs`).
- DB tables behind character, base stat, current state, cave, and garden-plot repositories (`GameServer/Services/CharacterService.cs`).
- Home-map definition from map catalog must exist for default spawn seeding (`GameServer/Services/CharacterService.cs`).

# client/server touch points
- `CreateCharacterHandler` returns the newly created snapshot payload after persistence succeeds.
- `GetCharacterListHandler` returns account-owned characters for selection UI.
- `GetCharacterDataHandler` returns the bootstrap snapshot used before enter-world.

# edge cases
- Existing accounts cannot create a second character through this path.
- If starter skill config is zero, the system silently skips starter skill creation rather than failing.
- Home-cave creation is duplicated in both `CharacterService` and `HerbService`; behavior must stay aligned manually.

# unclear or suspicious behavior
- Starter resources currently appear to mean only starter skill grant; no starter inventory/equipment grant is visible in these files.
- Home-cave seeding logic exists in two services with overlapping responsibilities.

# suggested canonical target docs
- `docs/systems/character-creation-and-bootstrap-runtime.md`
- `docs/progression/starter-state-and-home-cave-seeding.md`
