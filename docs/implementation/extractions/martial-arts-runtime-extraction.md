# title
Martial arts runtime extraction

# scope
Martial art ownership, book consumption, active martial art selection, progression-facing metadata, and cultivation preview touch points.

# source files
- `GameServer/Services/MartialArtService.cs`
- `GameServer/Services/MartialArtActionService.cs`
- `GameServer/Network/Handlers/GetOwnedMartialArtsHandler.cs`
- `GameServer/Network/Handlers/SetActiveMartialArtHandler.cs`
- `GameServer/Network/Handlers/UseMartialArtBookHandler.cs`
- `GameServer/Services/ItemUseService.cs`
- `GameServer/Runtime/CombatDefinitionCatalog.cs`
- `GameServer/Descriptions/GameplayDescriptionService.cs`

# current runtime behavior
- Martial art ownership is stored in `PlayerMartialArt` rows and projected by `MartialArtService.GetOwnedMartialArtsAsync(...)` into DTOs ordered by martial art id (`GameServer/Services/MartialArtService.cs`).
- Using a martial art book validates the item, resolves its referenced martial art definition, rejects duplicate learning, creates a new owned-martial-art row at stage 1 / exp 0, consumes the book item, and reinitializes base stats (`GameServer/Services/MartialArtService.cs`).
- Returned martial art DTOs include compiled description, current stage/exp, exp required for current stage, qi absorption rate, and active flag (`GameServer/Services/MartialArtService.cs`, `GameServer/Descriptions/GameplayDescriptionService.cs`).
- `SetActiveMartialArtAsync` can clear active selection with `<= 0`, or set a specific owned martial art id into base stats after validation (`GameServer/Services/MartialArtService.cs`).
- `MartialArtActionService` blocks active-martial-art switching while the player is cultivating or practicing, then reapplies authoritative final stats and rebuilds cultivation preview (`GameServer/Services/MartialArtActionService.cs`).
- `GetOwnedMartialArtsHandler` returns both owned martial arts and current cultivation preview for client-side progression UI (`GameServer/Network/Handlers/GetOwnedMartialArtsHandler.cs`).

# validations / guards
- Martial art book use requires the item to belong to the player and map to a valid `MartialArtBook` definition (`GameServer/Services/MartialArtService.cs`).
- Duplicate learning returns `MartialArtAlreadyLearned` (`GameServer/Services/MartialArtService.cs`).
- Active selection rejects unknown martial art ids and unowned martial arts (`GameServer/Services/MartialArtService.cs`).
- Switching active martial art while cultivating/practicing throws before mutation (`GameServer/Services/MartialArtActionService.cs`).

# config/data dependencies
- Combat definition data provides martial art metadata, stage thresholds, skill unlock lists, and qi absorption rate.
- Player martial art repository stores ownership/progression rows.
- Base stats persistence stores `ActiveMartialArtId` and downstream stat effects.

# client/server touch points
- `GetOwnedMartialArtsPacket`, `SetActiveMartialArtPacket`, and `UseMartialArtBookPacket` are the direct client surfaces.
- Item-use result for martial art books returns updated inventory, base stats, learned martial art model, and cultivation preview.

# edge cases
- Clearing active martial art is allowed by sending a non-positive id.
- If combat definitions are missing for an owned martial art row, service throws instead of degrading gracefully.
- Book use recalculates base stats immediately after learning, so passive/stat effects can appear before the player manually activates another martial art.

# unclear or suspicious behavior
- Progression runtime beyond learning/selection is mostly outside these files; ownership doc should not imply stage gain logic is complete here.
- Error code for switching while cultivating/practicing currently uses `PracticeAlreadyActive`, which may be semantically broader than the name suggests.

# suggested canonical target docs
- `docs/progression/martial-art-ownership-and-activation-runtime.md`
- `docs/progression/martial-art-book-consumption-runtime.md`
