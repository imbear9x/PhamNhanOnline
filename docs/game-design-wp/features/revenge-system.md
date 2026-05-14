---
doc_type: game_design_feature
system_id: revenge-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/death-penalty.md
  - features/player-interaction-group.md
  - shared-rules.md
requires_code_verification: false
---

# Hệ Thống Trả Thù - Feature Draft

## Goal

Cho phép player bị PK có quyền chủ động tấn công lại kẻ đã giết mình ở bất kỳ map nào cho phép PvP - tạo vòng lặp accountability cho hành vi PK và cơ chế giải quyết thù oán player-to-player.

## Design Summary

Khi player A bị player B giết (PK), B tự động được thêm vào danh sách kẻ thù của A. A có thể chọn trả thù B - khi đó A có thể chủ động tấn công B ở bất kỳ map nào trừ map cấm PvP, dù bình thường việc đó sẽ bị tính là PK. Trạng thái tấn công trong revenge vẫn là `pk` state - không có state đặc biệt. B nhận thông báo khi A vào cùng map. Revenge kết thúc khi A giết được B hoặc A tự xóa B khỏi danh sách. Bị giết trong revenge cũng tự động thêm kẻ thù mới - vòng lặp có thể tiếp tục.

## Scope

### In Scope
- Tự động thêm kẻ thù khi bị PK
- Danh sách kẻ thù tối đa 50 người
- Player chọn ai muốn trả thù trong danh sách
- Tấn công kẻ thù trong trạng thái revenge = pk state (không phải state riêng)
- Thông báo cho B khi A vào cùng map (A đang trong revenge với B)
- Kết thúc revenge khi A giết B hoặc A tự xóa B
- Vòng lặp revenge: bị giết lại thì thêm kẻ thù mới

### Out Of Scope
- State riêng cho revenge (dùng pk state)
- Reward hay bonus khi trả thù thành công
- Revenge theo nhóm / tông môn
- Giới hạn thời gian revenge

## Core Loop

1. Player A bị player B giết (pk) → B tự động vào danh sách kẻ thù của A.
2. A mở danh sách kẻ thù → chọn B để trả thù (hoặc bỏ qua).
3. Khi A và B cùng map (map cho phép PvP): A có thể tấn công B; B nhận thông báo.
4. A giết B → B xóa khỏi danh sách trả thù của A tự động.
5. B bị giết bởi A → A tự động vào danh sách kẻ thù của B (nếu B muốn trả thù lại).

## Player-Facing Rules

### Danh sách kẻ thù

- Tự động thêm kẻ thù: **chỉ khi bị PK** - không thêm khi chết trong duel, pvp_zone, cave_raid, sect_war, mineral_conflict.
- Danh sách tối đa **50 người**.
- Khi danh sách đầy và có kẻ thù mới: **người lâu nhất bị xóa tự động** để nhường chỗ.
- Player có thể **tự xóa bất kỳ ai** trong danh sách bất kỳ lúc nào.
- Không có timeout - tên trong danh sách tồn tại cho đến khi bị xóa (thủ công hoặc do overflow hoặc do revenge thành công).

### Chọn trả thù

- Player **chủ động chọn** ai trong danh sách kẻ thù để kích hoạt trạng thái trả thù.
- Có thể trả thù nhiều người cùng lúc - không giới hạn số lượng đang active trong danh sách.
- Không chọn trả thù → kẻ thù vẫn nằm trong danh sách nhưng không có mechanic đặc biệt.

### Tấn công trong trạng thái trả thù

- Khi A đang trả thù B: A có thể **chủ động tấn công B ở bất kỳ map nào** - kể cả map bình thường ngoài các context PvP.
- Tuy nhiên trạng thái vẫn là **`pk` state** - A vẫn chịu pk penalty nếu giết B (trừ thọ nguyên, penalty bổ sung theo game design data).
- **Map cấm PvP**: map được đánh dấu no-pvp (thành thị, làng, v.v.) - không thể tấn công trong revenge, duel, hay PK.
- B **nhận thông báo** khi A (đang trả thù B) vào cùng map với B.
- B có thể **tấn công lại A** khi A vào map - tấn công lại trong context này cũng là `pk` state.

### Kết thúc revenge

- A giết được B → **B tự động xóa khỏi danh sách trả thù của A**.
- A tự xóa B khỏi danh sách → revenge kết thúc.
- B bị overflow ra khỏi danh sách (danh sách quá 50) → revenge với B kết thúc.

### Vòng lặp revenge

- Bị giết trong bất kỳ ngữ cảnh pk nào → kẻ giết tự động vào danh sách kẻ thù của người bị giết.
- Tức là: A trả thù và giết B → A vào danh sách kẻ thù của B, B có thể trả thù lại A.
- Vòng lặp không tự kết thúc - chỉ dừng khi một bên tự xóa hoặc không chọn trả thù.

## System States

- `enemy_listed`: B nằm trong danh sách kẻ thù của A, A chưa chọn trả thù.
- `revenge_active`: A đã chọn trả thù B - A có thể tấn công B ở map PvP-allowed.
- `revenge_ended`: B bị xóa khỏi danh sách (giết thành công, tự xóa, hoặc overflow).

## Main Flows

### Flow 1 - Bị PK, thêm kẻ thù
1. B giết A bằng PK.
2. Server tự động thêm B vào danh sách kẻ thù của A.
3. A nhận notification "Bạn đã bị [B] giết. [B] đã được thêm vào danh sách kẻ thù."
4. A mở danh sách → chọn trả thù B hoặc bỏ qua.

### Flow 2 - Trả thù
1. A chọn trả thù B → state `revenge_active`.
2. A và B vào cùng map (map PvP-allowed).
3. B nhận thông báo "[A] đang truy đuổi trả thù bạn."
4. A tấn công B (pk state) → combat diễn ra bình thường.
5. A giết B → B tự động xóa khỏi danh sách. B có thể thêm A vào danh sách kẻ thù của B.

### Flow 3 - Kẻ thù chạy sang map khác
1. A đang trả thù B, B chạy sang map khác.
2. A tiếp tục theo - khi gặp lại ở bất kỳ map PvP-allowed nào, A vẫn có thể tấn công.
3. Không có mechanic "mất dấu" - trạng thái trả thù duy trì vĩnh viễn cho đến khi kết thúc.

### Flow 4 - Danh sách đầy
1. A đã có 50 kẻ thù trong danh sách.
2. A bị PK bởi C → C cần được thêm vào.
3. Server tự động xóa kẻ thù **lâu nhất** trong danh sách → thêm C vào.
4. Nếu kẻ thù bị xóa đang trong trạng thái `revenge_active` → revenge với người đó kết thúc.

## Edge Cases
- A đang trả thù B, B đang trong map cấm PvP: A không thể tấn công, phải chờ B ra map khác.
- A và B cùng trong pvp_zone: combat diễn ra bình thường theo pvp_zone rule - không cần revenge state để tấn công nhau.
- B đã chết vì lý do khác (không phải A giết): revenge vẫn active cho đến khi A tự xóa hoặc A giết B.
- A offline khi B vào cùng map: không có thông báo (A offline); khi A online lại không có retroactive notification.
- A và B là thành viên cùng tông môn: revenge vẫn hoạt động bình thường - không có friendly fire protection.

## Data / Config Needs
- Danh sách kẻ thù per player: max 50 entries, timestamp thêm vào → DB
- Trạng thái revenge active per player-pair → DB
- Notification trigger khi kẻ thù vào cùng map → server event
- Map no-pvp flag → DB map config (đã dùng trong PvP state taxonomy)

## UI / UX Notes
- Danh sách kẻ thù: hiển thị tên, thời gian bị PK, trạng thái (đang trả thù / chưa chọn).
- Nút "Trả thù" và "Bỏ qua" per entry.
- Notification khi bị thêm kẻ thù mới.
- Notification khi kẻ thù vào cùng map (nếu đang revenge active).
- Badge hoặc indicator khi có kẻ thù mới chưa xử lý.

## Related Systems
- **PvP State Taxonomy** (`shared-rules.md`): revenge tấn công = pk state.
- **Death Penalty** (`features/death-penalty.md`): pk penalty áp dụng khi giết trong revenge.
- **Player Interaction** (`features/player-interaction-group.md`): entry point từ player profile / target action list.
- **Map config**: no-pvp flag quyết định map nào không thể revenge.

## Key Decisions
1. Tự động thêm kẻ thù **chỉ khi bị PK** - không thêm khi chết trong duel/pvp_zone/raid/war.
2. Danh sách tối đa 50, overflow xóa người lâu nhất.
3. Tấn công trong revenge = **pk state** - không có state riêng, không miễn penalty.
4. Map cấm PvP: không thể tấn công trong revenge (giống mọi PvP khác).
5. B nhận thông báo khi A (đang revenge) vào cùng map.
6. Revenge kết thúc khi A giết B hoặc A tự xóa.
7. Vòng lặp revenge: bị giết lại → thêm kẻ thù mới, không tự dừng.
8. Không có timeout — danh sách tồn tại cho đến khi bị xóa.
9. Kẻ thù được thêm **chỉ khi chết** — bị tấn công nhưng chưa chết không trigger thêm kẻ thù.

## Open Questions
- [x] Chỉ khi chết mới được thêm vào danh sách kẻ thù - A tấn công B trước nhưng chưa giết được: B chưa thêm A vào danh sách. Chỉ khi B chết bởi A mới thêm.

## Known Conflicts / Drift
- Revenge tấn công = pk state → A vẫn chịu pk penalty khi giết B. Điều này có thể tạo cảm giác "bất công" cho người trả thù hợp lý. Đây là thiết kế có chủ ý - không có state miễn penalty cho revenge. Ghi nhận để review nếu cần sau.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
