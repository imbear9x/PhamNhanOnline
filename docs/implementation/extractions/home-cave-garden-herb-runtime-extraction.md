# title
Home cave, garden, and herb runtime extraction

# scope
Home cave seeding, garden plot ownership, soil insertion, herb planting/replanting, herb inventory transfer, and server-side herb progression materialization.

# source files
- `GameServer/Services/HerbService.cs`
- `GameServer/Services/CharacterService.cs`
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/Config/GameConfigValues.cs`
- `GameServer/Runtime/AlchemyDefinitionCatalog.cs`
- `GameServer/Runtime/ItemDefinitionCatalog.cs`
- `GameServer/Randomness/GameRandomService.cs`

# current runtime behavior
- Home cave seeding creates one owner-bound cave plus `character.home_garden_plot_count` empty plots, either during character creation or via `HerbService.EnsureHomeCaveAsync(...)` (`GameServer/Services/CharacterService.cs`, `GameServer/Services/HerbService.cs`).
- `GetGardenPlotsAsync` requires owned cave access and returns plot rows for that cave (`GameServer/Services/HerbService.cs`).
- `InsertSoilAsync` validates that the chosen player item is a soil item with a matching soil template, ensures the plot belongs to the player's cave, clears depleted residual soil if present, then marks the soil as inserted and binds it to the plot (`GameServer/Services/HerbService.cs`).
- `PlantSeedAsync` requires inserted soil and empty herb slot, validates herb-seed item/template mapping, consumes one seed item, creates a `PlayerHerb` row at seedling stage, and binds it to the plot inside a transaction (`GameServer/Services/HerbService.cs`).
- `PlantExistingHerbAsync` supports re-placing a herb currently stored in inventory back into a plot (`GameServer/Services/HerbService.cs`).
- `MoveHerbToInventoryAsync` materializes herb progress first, detaches herb and soil from the plot, updates soil state, and moves the herb into inventory state (`GameServer/Services/HerbService.cs`).

# validations / guards
- Cave/plot/herb operations require ownership checks through helper methods like `RequireOwnedCaveAsync`, `RequireOwnedPlotAsync`, and `RequireOwnedHerbAsync` (`GameServer/Services/HerbService.cs`).
- Plot must contain soil before planting; plot must not already contain another herb (`GameServer/Services/HerbService.cs`).
- Soil and seed items must belong to the player and match expected item/template types (`GameServer/Services/HerbService.cs`).
- Inserted soil already in another plot or herbs not currently in inventory are rejected (`GameServer/Services/HerbService.cs`).

# config/data dependencies
- Config: `character.home_garden_plot_count` (`GameServer/Config/GameConfigKeys.cs`).
- Map home definition, alchemy herb/soil definitions, item definitions, and RNG service for herb-related outcomes.
- Cave, plot, soil, herb, and item repositories.

# client/server touch points
- No dedicated network handlers were visible in the inspected set for cave/garden operations.
- This currently looks like server-present gameplay state with limited or not-yet-wired packet/UI surface in the reviewed files.

# edge cases
- Depleted soil already sitting in a plot can be auto-detached and returned to inventory-state bookkeeping during new soil insertion.
- Existing herb can be replanted without consuming a seed item if it is already an inventory herb record.
- Home cave creation logic appears in both character bootstrap and herb service paths.

# unclear or suspicious behavior
- The core runtime exists server-side, but direct client packet surface was not found in the inspected handlers, so accessibility/live completeness is unclear.
- Canonical docs should avoid claiming player-facing functionality until packet/UI wiring is verified.

# suggested canonical target docs
- `docs/systems/home-cave-and-garden-runtime.md`
- `docs/alchemy/herb-planting-and-soil-runtime.md`
