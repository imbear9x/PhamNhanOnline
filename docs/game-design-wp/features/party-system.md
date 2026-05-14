---
doc_type: game_design_feature
system_id: party-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/player-interaction-group.md
  - features/sect-system.md
  - features/dungeon-system.md
  - shared-rules.md
requires_code_verification: false
---

# Hệ Thống Party — Feature Draft

## Goal

Cho phép player trong cùng tông môn phối hợp trong map PvP có hỗ trợ party: không đánh nhau, chia sẻ tu vi và tiềm năng nhận được khi cùng map và trong tầm proximity.

## Design Summary

Party là nhóm tạm thời, được tạo trước khi vào map và chỉ tồn tại trong map đó. Khi rời map, party tự giải tán. Party chỉ có effect khi map cho phép — bao gồm friendly fire protection và chia sẻ tu vi/tiềm năng. Cơ chế chia sẻ tu vi/tiềm năng là shared rule, chỉ áp dụng cho thành viên cùng tông môn trong cùng map và trong tầm proximity config.

## Scope

### In Scope
- Tạo party trước khi vào map
- Party tự giải tán khi rời map
- Friendly fire protection giữa party member trong map cho phép party
- Chia sẻ tu vi và tiềm năng giữa thành viên cùng tông môn trong proximity
- Số lượng thành viên tối đa config per map

### Out Of Scope
- Party tồn tại liên map
- Ally giữa các tông môn
- Chia sẻ loot
- Party cross-tông môn nhận chia sẻ tu vi/tiềm năng
- Chi tiết tỉ lệ chia sẻ tu vi/tiềm năng — data design

## Player-Facing Rules

### Tạo và giải tán party
- Party được **tạo trước khi vào map** — không thể tạo sau khi đã vào.
- Party **chỉ tồn tại trong map đó** — khi bất kỳ thành viên rời map, party tự giải tán.
- Số thành viên tối đa per party: **config per map**.

### Điều kiện map
- Chỉ map được đánh dấu **party-allowed** mới kích hoạt effect party.
- Trong map không hỗ trợ party: không có friendly fire protection, không chia sẻ tu vi/tiềm năng.

### Friendly fire protection
- Thành viên cùng party **không thể tấn công nhau** trong map party-allowed.
- Áp dụng cho mọi loại tấn công: đơn, AoE, skill.

### Chia sẻ tu vi và tiềm năng
- Khi player A đánh quái nhận tu vi và tiềm năng, **thành viên cùng tông môn** của A trong cùng map và trong **tầm proximity** cũng nhận được một lượng tương ứng.
- Chỉ áp dụng cho thành viên **cùng tông môn** — không áp dụng cho party member khác tông.
- Proximity range: config trong `game_configs`.
- Tỉ lệ chia sẻ: data design xác định sau.
- Xem chi tiết tại **Shared Rule: Tu Vi / Tiềm Năng Chia Sẻ** trong `shared-rules.md`.

## System States
- `party_forming`: đang tạo party, chưa vào map.
- `party_active`: đang trong map party-allowed, effect đang hoạt động.
- `party_dissolved`: rời map, party giải tán.

## Main Flows

### Flow 1 — Tạo party và vào map
1. Player tạo party, mời thành viên cùng tông môn.
2. Party vào map party-allowed.
3. Friendly fire protection kích hoạt.
4. Chia sẻ tu vi/tiềm năng kích hoạt cho thành viên cùng tông môn trong proximity.

### Flow 2 — Thành viên rời map
1. Thành viên A rời map (thoát, chết bị đẩy ra, v.v.).
2. Party tự giải tán ngay lập tức.
3. Các thành viên còn lại mất friendly fire protection và chia sẻ tu vi/tiềm năng.

## Edge Cases
- Thành viên chết trong map: nếu bị đẩy ra ngoài → party giải tán. Nếu hồi sinh tại checkpoint trong map → vẫn trong party.
- AoE của party member A vô tình vào vùng party member B: damage không áp dụng lên B.
- Thành viên khác tông trong party: nhận friendly fire protection nhưng không nhận chia sẻ tu vi/tiềm năng.

## Data / Config Needs
- Map party-allowed flag → DB map config
- Party size max per map → DB map config
- Proximity range cho chia sẻ tu vi/tiềm năng → `game_configs`
- Tỉ lệ chia sẻ tu vi/tiềm năng → data design

## UI / UX Notes
- Party UI: danh sách thành viên, HP bar cơ bản.
- Indicator khi thành viên trong/ngoài proximity range.
- Thông báo khi party giải tán.

## Related Systems
- **Sect System** (`features/sect-system.md`): chia sẻ tu vi/tiềm năng chỉ cho thành viên cùng tông môn.
- **Shared Rule: Tu Vi / Tiềm Năng Chia Sẻ** (`shared-rules.md`): canonical rule về proximity sharing.
- **Dungeon System** (`features/dungeon-system.md`): phó bản có thể là map party-allowed.
- **PvP State Taxonomy** (`shared-rules.md`): friendly fire protection override trong party-allowed map.

## Key Decisions
1. Party chỉ tồn tại trong map — tạo trước khi vào, giải tán khi rời.
2. Party size config per map.
3. Friendly fire protection: không thể tấn công nhau trong map party-allowed.
4. Chia sẻ tu vi/tiềm năng: chỉ thành viên cùng tông môn, cùng map, trong proximity.
5. Proximity range config trong `game_configs`.
6. Không có ally giữa tông môn.

## Open Questions
- [ ] Proximity range cụ thể — data design.
- [ ] Tỉ lệ chia sẻ tu vi/tiềm năng — data design.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/` — 2 open questions là data design, không block promote.
