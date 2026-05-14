---
doc_type: game_design_feature
system_id: sect-activities
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/sect-system.md
  - features/mineral-vein-system.md
requires_code_verification: false
---

# Hoạt Động Tông Môn — Feature Draft

## Goal

Ghi nhận các hoạt động tập thể của tông môn ở phase hiện tại. Scope phase này chỉ gồm công mỏ và thủ mỏ linh thạch — các hoạt động tông môn khác defer sang phase sau.

## Design Summary

Hoạt động tông môn phase V1 là công/thủ mỏ linh thạch — cơ chế đã được thiết kế đầy đủ trong `features/mineral-vein-system.md`. Không có lịch tổ chức cố định; thành viên tự do tham gia theo thời gian thực. Không có framework hoạt động riêng ở phase này.

## Scope

### In Scope
- Công mỏ linh thạch của tông môn khác
- Thủ mỏ linh thạch của tông môn mình

### Out Of Scope
- Hoạt động tông môn khác (tập luyện nội bộ, thi đấu, v.v.) — defer phase sau
- Lịch tổ chức hoạt động
- Điểm đóng góp hoạt động cá nhân — defer

## Player-Facing Rules

- Công/thủ mỏ linh thạch là hoạt động tông môn chính ở phase V1.
- Không có lịch cố định — thành viên tham gia tự do bất kỳ lúc nào.
- Toàn bộ rule về công/thủ mỏ xem tại `features/mineral-vein-system.md`.

## Related Systems
- **Mineral Vein System** (`features/mineral-vein-system.md`): toàn bộ rule công/thủ mỏ.
- **Sect System** (`features/sect-system.md`): framework tông môn.

## Key Decisions
1. Phase V1: hoạt động tông môn chỉ gồm công/thủ mỏ linh thạch.
2. Không có lịch tổ chức — tự do tham gia.
3. Các hoạt động khác defer phase sau.

## Open Questions
- [ ] Các hoạt động tông môn phase sau — xác định khi roadmap mở rộng.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/` — rule đã đầy đủ trong mineral-vein-system.
