# title
Alchemy runtime extraction

# scope
Recipe ownership, recipe detail/preview, craft validation, practice-backed craft start, and mastery/rate-plan behavior.

# source files
- `GameServer/Services/AlchemyService.cs`
- `GameServer/Services/AlchemyCraftQueryService.cs`
- `GameServer/Services/AlchemyCraftActionService.cs`
- `GameServer/Services/AlchemyPracticeService.cs`
- `GameServer/Network/Handlers/GetLearnedPillRecipesHandler.cs`
- `GameServer/Network/Handlers/GetPillRecipeDetailHandler.cs`
- `GameServer/Network/Handlers/PreviewCraftPillHandler.cs`
- `GameServer/Network/Handlers/CraftPillHandler.cs`

# current runtime behavior
- `AlchemyService.ValidateCraftPillAsync` resolves the recipe, verifies the player has learned it, rejects unsupported required-herb-maturity recipes, validates selected inventory ids and optional catalyst selections, computes max craftable count, allocates mandatory/optional inputs, and builds a craft rate plan (`GameServer/Services/AlchemyService.cs`).
- Learned recipe ownership is checked through `PlayerPillRecipeRepository.GetByPlayerAndRecipeAsync(...)` for validation, preview, and rate-plan building (`GameServer/Services/AlchemyService.cs`).
- Preview/detail/list handlers route through query services that expose learned recipe sets, recipe details, and previewed cost/success information before craft start (`GameServer/Network/Handlers/GetLearnedPillRecipesHandler.cs`, `GameServer/Network/Handlers/GetPillRecipeDetailHandler.cs`, `GameServer/Network/Handlers/PreviewCraftPillHandler.cs`).
- `AlchemyCraftActionService.StartCraftAsync` is the craft-entry path used by `CraftPillHandler`; craft result includes created practice session data, consumed items, refreshed inventory, and recipe payload (`GameServer/Network/Handlers/CraftPillHandler.cs`, `GameServer/Services/AlchemyCraftActionService.cs`).
- Success-rate planning uses recipe mastery progression, optional inputs, and segmented rates rather than a single fixed percentage (`GameServer/Services/AlchemyService.cs`).
- Completed alchemy work integrates with practice/notification systems through `AlchemyPracticeService` (`GameServer/Services/AlchemyPracticeService.cs`).

# validations / guards
- Unknown or unlearned recipe fails validation immediately (`GameServer/Services/AlchemyService.cs`).
- Required-herb-maturity inputs are explicitly rejected as phase-later functionality (`GameServer/Services/AlchemyService.cs`).
- Invalid selected inventory ids, invalid catalyst ids, insufficient mandatory inputs, and over-requested craft counts all fail with explicit validation results (`GameServer/Services/AlchemyService.cs`).
- Handler layer requires world-entered player session before query/action services can proceed (`GameServer/Network/Handlers/*.cs`).

# config/data dependencies
- Alchemy definition catalog for recipes, pills, optional inputs, and mastery settings.
- Player recipe repository and inventory/equipment/soil repositories for validating selected ingredients.
- Practice-session storage and item/inventory services for craft execution handoff.

# client/server touch points
- Client surfaces: `GetLearnedPillRecipes`, `GetPillRecipeDetail`, `PreviewCraftPill`, `CraftPill`, and `GetAlchemyPracticeStatus`.
- Craft responses can include session model, consumed items, refreshed inventory, and failure reason strings.

# edge cases
- Craft-count normalization clamps requested count to at least 1 during validation.
- Optional catalyst allocation can partially apply if `allowPartial` path succeeds.
- Recipe detail handler logs request/response metadata around each fetch.

# unclear or suspicious behavior
- Recipes with `required_herb_maturity` are present in data model but intentionally blocked here, so canonical docs need to call that feature incomplete.
- Craft flow spans validation, session creation, later completion, and notification; one doc should not imply synchronous item output at craft-start time.

# suggested canonical target docs
- `docs/alchemy/alchemy-recipe-and-craft-runtime.md`
- `docs/alchemy/alchemy-practice-completion-runtime.md`
