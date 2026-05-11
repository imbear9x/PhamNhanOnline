---
title: Character creation and bootstrap runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/character-bootstrap-runtime-extraction.md
related_code:
  - GameServer/Services/CharacterService.cs
  - GameServer/Network/Handlers/CreateCharacterHandler.cs
  - GameServer/Network/Handlers/GetCharacterListHandler.cs
  - GameServer/Network/Handlers/GetCharacterDataHandler.cs
  - GameServer/Services/HerbService.cs
---

# Character creation and bootstrap runtime

## Scope

Canonical server-runtime behavior for account-owned character creation, initial persisted state, starter seeding, and pre-world bootstrap snapshots.

## Runtime behavior

- `CharacterService.CreateCharacterAsync` enforces one character per account, normalizes the name, creates character/base-stat/current-state rows, then seeds home cave/garden and starter resources in one DB transaction.
- Default current state uses the home map definition from `MapCatalog.ResolveHomeDefinition()` and the current game-time snapshot for lifespan initialization.
- Home-cave seeding creates one home-cave row and `character.home_garden_plot_count` garden plots when none exist.
- Starter skill grant is config-driven by `character.starter_skill_id`; non-positive values skip the grant instead of failing creation.
- `GetCharacterDataHandler` returns a persisted pre-world character bootstrap snapshot. It is not the same as world-entry runtime publication.

## Client/server surface

- `CreateCharacterHandler` returns the newly created snapshot after persistence.
- `GetCharacterListHandler` returns account-owned characters.
- `GetCharacterDataHandler` returns the bootstrap snapshot before enter-world.

## Guards and edge cases

- Creation fails if the account already owns a character or the normalized name is not unique.
- Snapshot load by account returns null if the requested character is not owned by that account.
- Starter resources currently mean starter skill only; no starter inventory/equipment grant was evidenced.
- Home-cave/garden seeding logic exists in both `CharacterService` and `HerbService`, so drift between those paths is a maintenance risk.

## Verification

Supported by `docs/implementation/extractions/character-bootstrap-runtime-extraction.md` and listed code paths. No gameplay code was modified.
