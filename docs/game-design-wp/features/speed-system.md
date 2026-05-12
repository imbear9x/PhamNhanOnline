---
doc_type: game_design_feature
system_id: speed-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-08
updated_at: 2026-05-12
promoted_from: notes/speed-system.md
related_docs: []
requires_code_verification: false
---

# Hệ Thống Speed — Feature Draft

## Goal

Tạo chỉ số **Speed** cho mọi thực thể trong game (player, quái, boss) quyết định tốc độ di chuyển, tốc độ bay, và tỉ lệ né tránh. Thay vì dùng Accuracy/Hit rate riêng, hệ này dùng chênh lệch Speed để quyết định evasion — tạo cảm giác chiến đấu có chiều sâu dựa trên tốc độ tương đối.

## Design Summary

Speed là một chỉ số duy nhất dùng cho cả di chuyển lẫn bay và evasion. Evasion không phải stat riêng mà được tính từ **chênh lệch Speed giữa attacker và defender** theo một curve phi tuyến, với ngưỡng và cap config-driven. Kẻ tấn công nhanh hơn đủ nhiều sẽ đánh trúng 100%; kẻ chậm hơn đủ nhiều sẽ bị né theo curve.

## Scope

### In Scope
- Speed là stat của mọi thực thể
- Tốc độ di chuyển và bay dùng chung 1 chỉ số
- Evasion tính từ chênh lệch Speed theo curve phi tuyến
- Ngưỡng và cap config-driven

### Out Of Scope
- Shape cụ thể của curve (quadratic, exponential...) — phase balance
- Balance cụ thể của ngưỡng Y%, Z%
- Các mechanic liên quan đến slow/stun/root (các trạng thái kiểm soát)

## Core Loop

1. Attacker dùng kỹ năng/đòn tấn công nhắm vào defender.
2. Server tính chênh lệch Speed giữa 2 bên.
3. Nếu attacker nhanh hơn đủ nhiều (≥ Y%) → 100% trúng.
4. Nếu attacker chậm hơn (< Z% so với defender) → evasion theo curve.
5. Càng chậm hơn nhiều → evasion càng cao (đến cap tối đa).

## Player-Facing Rules

### Speed là stat của mọi thực thể
- Áp dụng cho player, quái, boss.
- Quái/boss có Speed fixed theo template trong DB.
- Bị buff Speed nhất thời bởi skill → Speed tăng tạm thời, evasion tính lại theo Speed mới.

### Tốc độ di chuyển và bay
- Speed map trực tiếp lên movement speed và fly speed.
- **Tốc độ bay = tốc độ di chuyển** — dùng chung 1 chỉ số, không tách riêng.

### Evasion — Relative Speed System
Không có Accuracy/Hit rate là stat riêng. Chỉ có Speed quyết định evasion.

**Rule:**
- Nếu Speed attacker **cao hơn defender vượt ngưỡng Y%** → **100% trúng**, defender không thể né.
- Nếu Speed attacker **thấp hơn defender dưới ngưỡng Z%** → bắt đầu có evasion.
- Càng thấp hơn nhiều → evasion càng cao theo **curve phi tuyến**.
- Curve mượt hơn linear: chênh lệch nhỏ thì evasion tăng chậm, chênh lệch lớn thì tăng nhanh hơn.
- Có **cap evasion tối đa** (không thể né 100%) — đặt trong `game_configs`.

**Ví dụ minh họa (Y=20%, Z=20%, evasion cap=80%):**

| Speed attacker | Speed defender | Kết quả |
|---|---|---|
| 120 | 100 | Cao hơn 20% → 100% trúng |
| 100 | 100 | Bằng nhau → base hit rate |
| 80 | 100 | Thấp hơn 20% → bắt đầu có evasion (thấp) |
| 50 | 100 | Thấp hơn nhiều → evasion cao (gần cap 80%) |

### Áp dụng
- Áp dụng cho cả **tấn công đơn lẫn AoE** — dùng chung rule.
- Server tính evasion roll mỗi khi có hit event.

## System States
- Không có state machine riêng — evasion tính real-time mỗi hit event.

## Edge Cases
- Speed attacker bằng defender: base hit rate, evasion thấp nhất.
- Buff Speed nhất thời cho quái/boss: evasion tính lại theo Speed mới trong thời gian buff.
- AoE nhắm nhiều mục tiêu: mỗi mục tiêu tính evasion riêng theo chênh lệch Speed với attacker.

## Data / Config Needs
- Ngưỡng Y% (attacker cao hơn X% → 100% trúng) → `game_configs`
- Ngưỡng Z% (attacker thấp hơn Z% → bắt đầu evasion) → `game_configs`
- Cap evasion tối đa → `game_configs`
- Shape của curve → xác định khi làm balance
- Speed template của quái/boss theo từng loại (DB)

## UI / UX Notes
- Speed hiển thị trong bảng chỉ số nhân vật.
- Không cần hiển thị evasion % trực tiếp cho người dùng (derived từ Speed, tính server-side).

## Related Systems
- Không có related system bắt buộc hiện tại.

## Key Decisions
1. Speed dùng chung cho di chuyển, bay, và evasion — không tách riêng.
2. Không có stat Accuracy/Hit rate riêng.
3. Evasion tính từ chênh lệch Speed theo curve phi tuyến.
4. Có cap evasion tối đa, không thể né 100%.
5. Áp dụng cho tất cả thực thể: player, quái, boss.
6. Cả đơn lẫn AoE đều dùng chung rule.

## Open Questions
- [ ] Shape curve cụ thể (quadratic, exponential...) — phase balance.
- [ ] Giá trị cụ thể của Y%, Z%, cap evasion — phase balance.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [ ] Behavior is specific enough for `dev` to estimate.
- [ ] Acceptance criteria can be written without guessing.
- [ ] Major edge cases are covered.
- [ ] Config/data needs are listed.
- [ ] Out-of-scope items are explicit.
- [ ] Ready to promote to `requirements/`.
