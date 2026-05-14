---
doc_type: game_design_feature
system_id: event-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/dungeon-system.md
  - features/npc-system.md
  - features/inbox-mail-system.md
  - shared-rules.md
requires_code_verification: false
---

# Hệ Thống Event — Feature Draft

## Goal

Tạo framework cho các sự kiện có thời hạn xuất hiện theo lịch hoặc trigger thủ công — boss event, crafting event, farming event, v.v. Mỗi event là một content package riêng với rule, reward, và thời gian riêng. Hệ này định nghĩa framework chung; chi tiết từng event do data design xác định.

## Design Summary

Event là content xuất hiện trong khoảng thời gian xác định. Có UI thông báo event đang diễn ra hoặc sắp diễn ra. Mỗi event có loại riêng (boss, crafting, farming, dungeon, v.v.), thời gian mở/đóng, điều kiện tham gia, và reward (có thể có hoặc không). Framework này không định nghĩa nội dung cụ thể — mỗi event instance sẽ được thiết kế riêng.

## Scope

### In Scope
- Framework event: loại, thời gian, trigger, trạng thái
- UI thông báo event đang diễn ra / sắp diễn ra
- Các loại event: boss event, crafting event, farming event, dungeon event, v.v.
- Reward per event — có thể có hoặc không
- Trigger: scheduled (giờ cố định) hoặc admin trigger

### Out Of Scope
- Nội dung cụ thể của từng event — data design
- Login reward / daily challenge — không phải event loại này
- Chi tiết balance reward — data design

## Event Types

Framework hỗ trợ các loại event sau — chi tiết nội dung do data design:

| Loại | Mô tả |
|---|---|
| `boss_event` | Boss đặc biệt xuất hiện trong thời gian giới hạn |
| `crafting_event` | Bonus hoặc content đặc biệt cho crafting |
| `farming_event` | Bonus drop, spawn đặc biệt cho farming |
| `dungeon_event` | Phó bản sự kiện mở trong thời gian giới hạn |
| `other` | Các loại event mở rộng sau này |

## Player-Facing Rules

### Thời gian và trigger
- Event có **thời gian bắt đầu và kết thúc** xác định.
- Trigger: **scheduled** (giờ cố định, lặp lại theo lịch) hoặc **admin trigger** (bật thủ công).
- Khi hết thời gian: event đóng, content liên quan biến mất hoặc kết thúc.

### UI thông báo
- Có **UI riêng** hiển thị:
  - Event đang diễn ra: tên event, thời gian còn lại.
  - Event sắp diễn ra: tên event, thời gian bắt đầu.
- UI truy cập từ menu chính hoặc HUD shortcut.
- Notification khi event bắt đầu hoặc sắp bắt đầu (config per event).

### Điều kiện tham gia
- Config per event — có thể không có điều kiện (ai cũng tham gia được).

### Reward
- Có thể có hoặc không — config per event.
- Nếu có: reward nằm trong content của event (loot boss, loot crafting, v.v.) hoặc reward hoàn thành event riêng.
- Reward overflow: vào inbox theo shared overflow rule.

## System States
- `upcoming`: event chưa bắt đầu, hiển thị countdown.
- `active`: event đang diễn ra.
- `ended`: event kết thúc, không tham gia được nữa.

## Main Flows

### Flow 1 — Event scheduled tự động
1. Server theo lịch → event vào state `active`.
2. UI cập nhật: hiển thị event đang diễn ra + countdown kết thúc.
3. Notification gửi đến player (nếu config).
4. Hết thời gian → event `ended`, content dọn dẹp.

### Flow 2 — Admin trigger event
1. Admin bật event thủ công.
2. Event vào state `active` ngay lập tức.
3. UI cập nhật, notification gửi.

### Flow 3 — Player tham gia event
1. Player thấy event active trong UI.
2. Vào nội dung event (map, NPC, phó bản sự kiện, v.v.).
3. Tham gia theo rule của event đó.
4. Nhận reward nếu có — overflow vào inbox.

## Edge Cases
- Player đang trong nội dung event khi event kết thúc: xử lý theo rule của content đó (ví dụ phó bản sự kiện → teleport ra theo dungeon close rule).
- Admin trigger event đang có event scheduled cùng loại: cần check conflict — config per event hoặc admin quyết định.

## Data / Config Needs
- Event template: ID, tên, loại, trigger type, thời gian bắt đầu/kết thúc, điều kiện tham gia, reward config → DB
- Notification config per event → DB
- Content liên kết per event (map, boss, dungeon, v.v.) → DB

## UI / UX Notes
- Event panel: danh sách event upcoming + active, countdown rõ ràng.
- Badge/indicator trên HUD khi có event active.
- Mỗi event entry: tên, loại, thời gian còn lại, nút tham gia (nếu có entry point trực tiếp).

## Related Systems
- **Dungeon System** (`features/dungeon-system.md`): dungeon event dùng phó bản sự kiện.
- **NPC System** (`features/npc-system.md`): timed NPC có thể là một phần của event.
- **Inbox** (`features/inbox-mail-system.md`): reward overflow.
- **Boss Thế Giới** (`features/world-boss-system.md`): boss event là một loại event.

## Key Decisions
1. Event là framework — nội dung cụ thể do data design.
2. 2 trigger type: scheduled và admin trigger.
3. Có UI thông báo event upcoming và active.
4. Reward: có thể có hoặc không — config per event.
5. Nhiều loại event: boss, crafting, farming, dungeon, v.v.

## Open Questions
- [x] Nhiều event có thể active cùng lúc — không giới hạn.
- [x] Notification: cả in-game popup và badge — frontend quyết định cách hiển thị cụ thể.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
