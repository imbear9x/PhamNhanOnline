# title
Cultivation runtime extraction

# scope
Online/offline cultivation state, settlement, breakthrough, and potential allocation behavior in `GameServer`.

# source files
- `GameServer/Runtime/CharacterCultivationService.cs`
- `GameServer/Services/CultivationActionService.cs`
- `GameServer/Network/Handlers/StartCultivationHandler.cs`
- `GameServer/Network/Handlers/StopCultivationHandler.cs`
- `GameServer/Network/Handlers/BreakthroughHandler.cs`
- `GameServer/Network/Handlers/AllocatePotentialHandler.cs`
- `GameServer/Runtime/PotentialStatCatalog.cs`
- `GameServer/Runtime/CharacterRuntimeNotifier.cs`

# current runtime behavior
- Starting cultivation requires an online player in their private home instance, with no blocking practice session, no restricted lifespan state, and an active martial art that yields qi absorption rate (`GameServer/Runtime/CharacterCultivationService.cs`).
- Start sets current state to `Cultivating`, stamps `CultivationStartedAtUtc` and `LastCultivationRewardedAtUtc`, and mutates runtime state without immediate breakthrough/allocation changes (`GameServer/Runtime/CharacterCultivationService.cs`).
- Stop/breakthrough/allocation all settle pending cultivation first through `SettleOnlinePlayerAsync(...)`, then mutate state or stats from the latest snapshot (`GameServer/Runtime/CharacterCultivationService.cs`).
- Breakthrough checks current realm, cultivation cap, and next-realm existence, then runs a random chance check, records the attempt, applies either failure penalty or realm promotion, and clears `PotentialRewardLocked` on success (`GameServer/Runtime/CharacterCultivationService.cs`).
- Potential allocation validates target support through `PotentialStatCatalog`, builds an allocation plan, applies the plan to base stats, attaches preview values, and notifies base stat changes (`GameServer/Runtime/CharacterCultivationService.cs`).
- `CultivationActionService` re-applies authoritative final stats after successful breakthrough/allocation so the response reflects downstream recalculation (`GameServer/Services/CultivationActionService.cs`).

# validations / guards
- Start fails for non-world players, already-cultivating players, practice-blocked players, non-private-home maps, expired/lifespan-restricted states, and missing active martial art (`GameServer/Runtime/CharacterCultivationService.cs`).
- Breakthrough fails if no valid current realm, cultivation is below cap, or next realm does not exist (`GameServer/Runtime/CharacterCultivationService.cs`).
- Potential allocation rejects unsupported targets, invalid requested amounts, and no-op plans (`GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Runtime/PotentialStatCatalog.cs`).

# config/data dependencies
- Game config values drive cultivation settlement interval and related reward coefficients (`GameServer/Runtime/CharacterCultivationService.cs`).
- Realm templates, map definitions, martial art data, random service, and potential-tier catalog all contribute to reward/breakthrough results.
- Runtime and persistence layers both participate because settlement can update online runtime snapshots or persisted offline snapshots.

# client/server touch points
- Handlers for start/stop/breakthrough/allocate return updated base stats and current state.
- `GetOwnedMartialArtsHandler` and martial-art actions surface cultivation preview built from the same underlying service.
- Runtime notifier pushes base stat changes after breakthrough/allocation mutation.

# edge cases
- Offline cultivation settlement can update persisted snapshots even when the player is not online.
- Breakthrough failure still mutates base stats through penalty application.
- Potential rewards can lock at realm cap, and enemy reward runtime respects that lock before granting potential.

# unclear or suspicious behavior
- Formation coefficient is a stub constant in this service, so canonical docs should not imply fully implemented formation-based cultivation math.
- Large parts of settlement math are internal helpers; canonical doc should cite behavior, not infer design intent not visible in public handlers.

# suggested canonical target docs
- `docs/progression/cultivation-runtime.md`
- `docs/progression/breakthrough-and-potential-allocation-runtime.md`
