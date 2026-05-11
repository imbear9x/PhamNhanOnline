# title
Description template runtime extraction

# scope
Runtime item/skill/martial-art description compilation and fallback behavior.

# source files
- `GameServer/Descriptions/GameplayDescriptionService.cs`
- `GameServer/Descriptions/DescriptionTemplateCompiler.cs`
- `GameServer/DTO/AlchemyModelBuilder.cs`
- `GameServer/DTO/PlayerNotificationModelBuilder.cs`
- `GameServer/Services/ItemService.cs`
- `GameServer/Services/MartialArtService.cs`

# current runtime behavior
- `GameplayDescriptionService` builds description context objects for skills, martial arts, and items, then compiles template strings through `DescriptionTemplateCompiler` (`GameServer/Descriptions/GameplayDescriptionService.cs`).
- For each domain, runtime first tries explicit `DescriptionTemplate`; if compile succeeds with non-empty text, that output wins (`GameServer/Descriptions/GameplayDescriptionService.cs`).
- If template output is empty or missing, service falls back to legacy description text; if that is also missing, it compiles a hard-coded default template per domain (`GameServer/Descriptions/GameplayDescriptionService.cs`).
- Item description context injects equipment stats, requirements, consumable effects, martial art book summary, recipe-book summary, soil summary, herb seed summary, and herb plant summary (`GameServer/Descriptions/GameplayDescriptionService.cs`).
- Built descriptions are consumed by inventory item views, martial art DTOs, alchemy models, and notification item models (`GameServer/Services/ItemService.cs`, `GameServer/Services/MartialArtService.cs`, `GameServer/DTO/AlchemyModelBuilder.cs`, `GameServer/DTO/PlayerNotificationModelBuilder.cs`).

# validations / guards
- Failed or empty explicit template compilation falls back instead of throwing as the normal runtime path (`GameServer/Descriptions/GameplayDescriptionService.cs`).
- Missing optional linked definitions simply omit related context fields rather than failing the build (`GameServer/Descriptions/GameplayDescriptionService.cs`).

# config/data dependencies
- Definition catalogs for combat, alchemy, and item metadata.
- Template strings and legacy descriptions stored in definition data.

# client/server touch points
- Inventory responses, martial art lists, alchemy recipe/item models, and notification payloads all expose compiled description text to clients.

# edge cases
- Empty fallback template can yield `null` description when neither template nor legacy description exists.
- Description output changes automatically when underlying definition data changes; there is no separate cached text layer visible here.

# unclear or suspicious behavior
- This is runtime-significant presentation logic, but current canonical coverage sits mostly in reference/spec docs rather than runtime docs.
- Canonical docs should distinguish compile failure fallback from intentional use of legacy description text.

# suggested canonical target docs
- `docs/systems/runtime-description-template-behavior.md`
