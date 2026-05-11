---
title: Skill ownership and loadout runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/skill-runtime-extraction.md
related_code:
  - GameServer/Services/SkillService.cs
  - GameServer/Network/Handlers/GetOwnedSkillsHandler.cs
  - GameServer/Network/Handlers/SetSkillLoadoutSlotHandler.cs
  - GameServer/Network/Handlers/SwapSkillLoadoutSlotsHandler.cs
  - GameServer/Services/EquipmentActionService.cs
---

# Skill ownership and loadout runtime

## Runtime behavior

- Starter skill ownership is driven by `character.starter_skill_id` during character initialization.
- Owned-skill snapshots return `MaxLoadoutSlotCount`, canonicalized owned-skill rows, and loadout slots.
- Loadout capacity is config-driven by `skill.max_loadout_slot_count`, defaulting to `5`.
- Duplicate persisted skill sources are canonicalized for the client by skill identity, preferring a representative row based on level/source/equipment metadata.
- Equipment changes synchronize equipment-granted `PlayerSkillEntity` rows, remove stale grants, and remove/block loadout entries for unavailable equipment skills.
- Setting `PlayerSkillId = 0` clears a slot. Assigning a skill removes duplicate loadout rows for that skill before inserting/replacing the target slot row.
- Swapping slots moves or swaps rows and returns a full rebuilt owned-skill snapshot.

## Client/server surface

- `GetOwnedSkillsPacket` / `GetOwnedSkillsResultPacket`
- `SetSkillLoadoutSlotPacket`
- `SwapSkillLoadoutSlotsPacket`
- `OwnedSkillsChangedPacket` after equipment-driven skill sync

## Guards and edge cases

- Slot indexes must be within `1..skill.max_loadout_slot_count`.
- Unknown skills, skills owned by another player, and unavailable equipment-granted skills are rejected.
- Equipment-granted skills are normal player-skill rows with `SourcePlayerItemId`; consumers must distinguish permanent and equipment-derived ownership.
- Canonicalization can hide duplicate persisted skill rows from clients.

## Verification

Supported by `docs/implementation/extractions/skill-runtime-extraction.md`.
