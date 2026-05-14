---
doc_type: game_design_feature
system_id: leaderboard-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/cultivation-and-breakthrough.md
requires_code_verification: false
---

# Hệ Thống Bảng Xếp Hạng - Feature Draft

## Goal

Cung cấp bảng xếp hạng toàn server theo tu vi, cho phép player biết vị thế của mình so với cộng đồng. Không có reward - mục đích thuần túy là thông tin và social recognition.

## Design Summary

Một bảng xếp hạng duy nhất toàn server, hiển thị top 50 player theo tu vi hiện tại. Reset hàng ngày theo server time - snapshot tu vi tại thời điểm reset, không tính tu vi đỉnh trong ngày. Không có reward, không có thông báo khi reset.

## Scope

### In Scope
- 1 bảng xếp hạng toàn server theo tu vi
- Top 50 player
- Reset hàng ngày
- Hiển thị thứ hạng, tên, cảnh giới, tu vi

### Out Of Scope
- Bảng xếp hạng theo tông môn / khu vực
- Xếp hạng theo chiến lực, PvP, thành tích
- Reward cho top player
- Thông báo khi reset hoặc thay đổi thứ hạng
- Lịch sử xếp hạng

## Player-Facing Rules

### Tiêu chí xếp hạng
- Xếp theo **tu vi hiện tại** tại thời điểm reset hàng ngày.
- Không tính tu vi đỉnh trong ngày — chỉ snapshot lúc reset.
- Tiebreak:
  1. Cùng cảnh giới: ai tu vi **cao hơn** xếp trên.
  2. Cùng cảnh giới, cùng tu vi: ai **đạt trước** xếp trên (theo timestamp server).

### Hiển thị
- Top 50 player toàn server.
- Mỗi entry: thứ hạng, tên nhân vật, cảnh giới, tu vi.
- Player xem được thứ hạng của bản thân dù không trong top 50.

### Reset
- Reset **hàng ngày** theo server time - âm thầm, không thông báo.
- Sau reset: bảng cập nhật theo tu vi hiện tại của tất cả player.

## Data / Config Needs
- Reset time hàng ngày → `game_configs`
- Số lượng hiển thị top (mặc định 50) → `game_configs`
- Snapshot tu vi per player tại thời điểm reset → DB

## UI / UX Notes
- Truy cập từ menu chính hoặc profile.
- Hiển thị thứ hạng bản thân ở cuối bảng nếu không trong top 50.

## Key Decisions
1. 1 bảng duy nhất toàn server, theo tu vi.
2. Snapshot lúc reset hàng ngày - không tính đỉnh trong ngày.
3. Không có reward.
4. Reset âm thầm, không thông báo.
5. Top 50.

## Open Questions
- [x] Tiebreak: cùng cảnh giới → tu vi cao hơn xếp trên; cùng tu vi → ai đạt trước xếp trên.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
