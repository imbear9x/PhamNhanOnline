---
title: Equipment runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/equipment-runtime-extraction.md
related_code:
  - GameServer/Services/EquipmentService.cs
  - GameServer/Services/EquipmentActionService.cs
  - GameServer/Services/EquipmentStatService.cs
  - GameServer/Services/CharacterFinalStatService.cs
  - GameServer/Services/SkillService.cs
---

# Equipment runtime

## Runtime behavior

- Equipment slot count is config-driven by `character.equipment_slot_count`, defaulting to `4`.
- Equip validates slot range, ownership, item definition, and equipment metadata, then ensures a `PlayerEquipmentEntity` row exists.
- Slots are generic integer slots in current runtime; no code-evidenced equipment-category-to-slot compatibility rule was found.
- Equipping into an occupied slot silently unequips the previous occupant.
- Equip/unequip actions run inside the player inventory transaction boundary, then synchronize equipment-granted skills, reapply final stats, and return refreshed inventory.
- Equipment stat modifiers come from item definition base modifiers plus persisted per-item bonus rows.

## Client/server surface

- `GetInventoryResultPacket` includes `EquipmentSlotCount`.
- Equip/unequip results include refreshed items plus updated base stats/current state.
- Equipment skill changes may emit `OwnedSkillsChangedPacket`.

## Risks / needs review

- `ValidateEquipAsync(...)` can create a `PlayerEquipmentEntity`, so it is not a pure validation method.
- Downstream stat/skill recompute lives in `EquipmentActionService`; callers bypassing it must perform those steps explicitly.

## Verification

Supported by `docs/implementation/extractions/equipment-runtime-extraction.md`.
