---
doc_type: game_design_requirement
system_id: inventory-bag-system
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: features/inventory-bag-system.md
related_docs:
  - features/inventory-bag-system.md
  - features/npc-system.md
  - features/inbox-mail-system.md
  - shared-rules.md
requires_code_verification: false
handoff_ready: false
---

# Hệ Thống Túi Trữ Vật — Requirement Spec

## Goal

Implement túi trữ vật per-character: container chứa item có cấp và số slot. Nhân vật mới có túi cấp 1. Nâng cấp bằng cách mua từ NPC bằng linh thạch — đồ chuyển nguyên vẹn sang túi mới. Không thể hạ cấp. Túi không phải item, không giao dịch được.

## Source Design Summary

Canonical design lives in `features/inventory-bag-system.md`.

Requirement-level clarifications locked for implementation:
- Túi là container gắn với nhân vật, không phải item trong inventory.
- Per-character — không share giữa các nhân vật cùng account.
- 4 cấp túi — số slot per cấp là data config.
- Nhân vật mới tạo luôn được cấp túi cấp 1 tự động.
- Nâng cấp chỉ từ NPC bằng linh thạch, chỉ được lên cấp cao hơn.
- Không thể hạ cấp — block cả UI lẫn server.
- Nâng cấp không làm mất item — chuyển nguyên vẹn sang túi mới.
- Túi đầy + loot rơi đất từ quái/boss → không nhặt được, item vẫn ở đất trong looting window, báo túi đầy — không inbox.
- Túi đầy + reward hệ thống (quest, event, sect, dungeon, crafting output) → inbox overflow.
- Túi đầy + action chủ động (harvest, extract, mua NPC, drop linh thảo…) → reject hoàn toàn.

## Target Design Summary

Player luôn có đúng 1 túi trữ vật gắn với nhân vật. Túi có cấp và số slot tương ứng. Khi nâng cấp túi, server thay túi cũ bằng túi mới và chuyển toàn bộ item sang — player không mất đồ, không cần làm gì thêm. Túi không phải item nên không xuất hiện trong inventory, không thể giao dịch.

Behavior cần đạt:
- Nhân vật mới → túi cấp 1 tự động, không cần player thao tác.
- Nâng cấp túi chỉ được lên cấp cao hơn — UI ẩn/disable option thấp hơn hoặc cùng cấp; server reject nếu request vi phạm.
- Khi nâng cấp: tất cả item trong túi cũ chuyển sang túi mới nguyên vẹn, không mất item nào.
- Túi đầy + reward hệ thống (quest, mail, event, sect, dungeon, crafting output) → inbox overflow.
- Túi đầy + loot rơi đất (quái/boss drop) → không nhặt được, item vẫn ở đất, báo túi đầy.
- Túi đầy + action chủ động (harvest, extract) → reject toàn bộ action.

## Current Runtime / Evidence Snapshot

- **Not yet confirmed**: Inventory/bag system hiện đã implement hay chưa — cần code verification.
- **Not yet confirmed**: Character init flow có tự gán túi cấp 1 chưa.
- **Not yet confirmed**: NPC shop có hỗ trợ bag upgrade transaction chưa.
- **Requires code verification**: Toàn bộ bag upgrade flow (validate, replace, item transfer).

## Scope

### Must Implement
- Bag entity per character: grade, slot count, linked to character not account.
- Character creation: auto-assign bag grade 1.
- Bag grade config table: grade, slot count, upgrade cost (linh thạch), display name.
- NPC bag upgrade transaction: validate higher grade only, deduct linh thạch, replace bag, transfer items.
- Server-side downgrade prevention: reject any request to equip bag grade ≤ current grade.
- UI: show current bag grade and slot count; hide/disable lower-or-equal grade options in NPC shop.
- Inventory full handling:
  - Loot rơi đất (quái/boss): không nhặt được, item vẫn đất, báo túi đầy — không inbox.
  - Reward hệ thống (quest/event/sect/dungeon/crafting): inbox overflow.
  - Action chủ động (harvest, extract, mua NPC, drop linh thảo): reject hoàn toàn.

### Must Not Implement
- Bag as tradeable/droppable item.
- Shared storage across characters on same account.
- Per-type sub-bags (weapon bag, material bag, etc.).
- Bag downgrade path.

## Terminology

- `bag`: per-character container with grade and slot count; not an item.
- `bag grade`: integer 1–4; determines slot count.
- `slot`: one inventory position that can hold one item stack.
- `upgrade`: replace current bag with a higher-grade bag; items transfer automatically.
- `inbox overflow`: reward hệ thống không vào được inventory thì redirect sang inbox mail system. Chỉ áp dụng cho reward không rơi ra đất.
- `loot reject`: loot rơi đất từ quái/boss khi túi đầy — không nhặt được, item vẫn nằm đất, báo túi đầy. Không redirect inbox.

## Functional Requirements

- `REQ-001`: Each character shall have exactly one bag at all times.
- `REQ-002`: Bag shall be a character-level attribute, not an inventory item; it shall not be droppable, tradeable, or mailable.
- `REQ-003`: On character creation, the system shall automatically assign bag grade 1 to the character.
- `REQ-004`: Bag slot count shall be determined by bag grade via a fixed data config table.
- `REQ-005`: A character's bag shall be scoped to that character; it shall not be shared with other characters on the same account.
- `REQ-006`: Bag upgrade shall only be possible to a grade strictly higher than the current bag grade.
- `REQ-007`: Server shall reject any bag upgrade request where the requested grade is less than or equal to the current bag grade, regardless of UI state.
- `REQ-008`: NPC shop UI shall only display bag grades higher than the player's current bag grade.
- `REQ-009`: Bag upgrade transaction shall: (a) validate grade and cost, (b) deduct linh thạch, (c) replace bag grade, (d) transfer all items from old bag to new bag without loss.
- `REQ-010`: Item transfer on upgrade shall never result in item loss; all items from old bag must appear in new bag after upgrade.
- `REQ-011a`: When inventory is full and a system reward (quest, event, sect welfare, dungeon completion, crafting output, admin grant) cannot enter inventory, it shall follow shared inbox overflow rule — redirect to inbox.
- `REQ-011b`: When inventory is full and a player attempts to pick up loot that has dropped on the ground (quái/boss drop), the pick-up shall be rejected; the item shall remain on the ground within its looting window; the client shall receive an inventory-full notification. No inbox redirect.
- `REQ-012`: When inventory is full and a player initiates an active action that produces items (harvest, extract), the action shall be rejected entirely; no partial grant, no inbox fallback.
- `REQ-013`: UI shall display current bag grade and current used/total slot count to the player.

## Acceptance Criteria

- `AC-001`: Given a new character is created, when character init completes, then the character has bag grade 1 with the configured slot count for grade 1.
- `AC-002`: Given a character has bag grade 2, when the player opens NPC bag shop, then only bag grade 3 and 4 are shown; grades 1 and 2 are not shown or are disabled.
- `AC-003`: Given a character has bag grade 2, when a server request is made to upgrade to grade 1 or 2, then the server rejects the request and no change occurs.
- `AC-004`: Given a character has bag grade 1 with 30 items and sufficient linh thạch, when the player upgrades to grade 2, then after upgrade the character has bag grade 2 and all 30 items are present in the new bag.
- `AC-005`: Given a character has bag grade 4, when the player opens NPC bag shop, then no bag upgrade options are shown (already at max grade).
- `AC-006a`: Given inventory is full, when a system reward (quest, event, sect, dungeon, crafting) is granted, then the item is redirected to inbox and the player can claim it later.
- `AC-006b`: Given inventory is full, when a player attempts to pick up loot dropped on the ground by a quái or boss, then the pick-up is rejected, the item remains on the ground, and the client receives an inventory-full notification.
- `AC-007`: Given inventory is full, when the player attempts to harvest a herb from a plot, then the harvest action is rejected and no item is granted.
- `AC-008`: Given a character's bag, when the player inspects the inventory UI, then bag grade and slot count (used/total) are visible.

## Runtime Flow

### Character creation
1. Server creates character.
2. Server assigns bag grade 1 to character from config.
3. Character enters game with empty bag, slot count = grade 1 config value.

### Bag upgrade
1. Player opens NPC bag shop.
2. UI fetches player current bag grade; shows only higher grades.
3. Player selects target grade and confirms purchase.
4. Server validates: target grade > current grade AND player has enough linh thạch.
5. Server deducts linh thạch.
6. Server replaces bag grade on character record.
7. Server transfers all items from old bag to new bag.
8. Server responds with new bag state.
9. Client updates inventory UI with new grade and slot count.

### Inventory full — system reward (quest/event/sect/dungeon/crafting)
1. System attempts to grant item to inventory.
2. Inventory has no free slot.
3. Item is redirected to inbox per shared overflow rule.

### Inventory full — ground loot pick-up (quái/boss drop)
1. Player attempts to pick up item lying on the ground.
2. Inventory has no free slot.
3. Pick-up is rejected — item remains on the ground within looting window.
4. Client receives inventory-full notification.
5. Player frees up a slot and retries within the looting window.

### Inventory full — active action
1. Player initiates action that would produce item (harvest, extract, etc.).
2. Server checks available inventory slots before processing.
3. If insufficient slots: reject action, return error to client.
4. Client shows "túi đầy" error.

## State / Lifecycle

### Bag state
- `grade_1` → `grade_2` → `grade_3` → `grade_4` (one-way only)
- No downgrade path exists.

### Item slot state
- `empty` — slot available
- `occupied` — slot holds an item stack

## Rules And Invariants

- A character always has exactly one bag; it cannot be unequipped or removed.
- Bag grade can only increase, never decrease.
- Bag upgrade never causes item loss.
- Bag is not an item; it cannot appear in any inventory, trade, drop, or mail flow.
- Slot count is always determined by bag grade config; it cannot be set independently.
- Inbox overflow applies to system reward grants only (quest/event/sect/dungeon/crafting output/admin). Ground loot pick-up is rejected, not redirected.

## Edge Cases

- Player upgrades bag while inventory is full: allowed — new bag has more slots, items transfer, no issue.
- Player at max grade (4) opens NPC shop: no bag upgrade options shown.
- Server receives downgrade request (e.g. client exploit attempt): server rejects, logs violation.
- Character deletion: bag and all items in bag are deleted with the character.
- Item transfer during upgrade and server crashes mid-transaction: must be atomic or recoverable — no partial state where items are lost.

## Data / Config Requirements

- Bag grade config table:
  - `grade` (1–4)
  - `slot_count`
  - `upgrade_cost_linh_thach`
  - `display_name`
- NPC shop config: which NPC sells bag upgrades, which grades are available.
- Character init config: default bag grade on creation = 1.

## UI / UX Requirements

- Inventory UI: display bag grade label and slot count (e.g. "Túi cấp 2 — 45/100 ô").
- NPC bag shop: only show grades higher than current; disable or hide lower/equal grades.
- On upgrade success: inventory UI refreshes with new slot count.
- On action reject due to full inventory: show clear "túi đầy" message.

## Telemetry / Logs / Debug Needs

- Log bag assignment on character creation.
- Log bag upgrade transactions: character id, old grade, new grade, cost deducted, item count transferred.
- Log server-side downgrade rejection with character id and requested grade.
- Log inventory-full rejections for active actions with action type and character id.

## Related Systems

- `features/inventory-bag-system.md` — canonical feature design source.
- `features/npc-system.md` — NPC shop sells bag upgrades.
- `features/inbox-mail-system.md` — inbox overflow for passive rewards.
- `shared-rules.md` — inbox overflow rule.

## Non-Blocking Follow-Ups

- Confirm whether current character init flow already assigns a default bag or needs to be added.
- Confirm NPC shop transaction framework supports bag upgrade (non-item purchase) or needs extension.

## Blocking Questions

- None at game-design level.

## Known Conflicts / Drift

- Bag system may not yet exist as a distinct entity in backend — requires code verification before Dev handoff.
- NPC shop currently handles item purchases; bag upgrade is a non-item transaction — may require shop framework extension.

## Readiness Level

- Ready for TechDesign refinement: **yes**
- Ready for Dev handoff: **no** — pending code verification of existing bag/inventory infrastructure and NPC shop framework
- Ready for QA verification: **no** — implementation not yet started
- Notes: TD should confirm whether bag is already modeled as a character attribute or needs new schema.

## Handoff Checklist

- [x] No blocking design questions remain.
- [x] Acceptance criteria are testable.
- [x] Config/data impacts are listed.
- [x] Edge cases are listed.
- [x] Related docs are linked.
- [x] Target design and current runtime/evidence are clearly separated.
- [x] Readiness Level is filled consistently with `handoff_ready`.
- [ ] `handoff_ready` is set correctly — currently `false` pending code verification.
