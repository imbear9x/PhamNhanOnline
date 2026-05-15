---
doc_type: game_design_requirement
system_id: death-penalty
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/death-penalty.md
related_docs:
  - features/death-penalty.md
  - features/home-cave-defense.md
  - features/player-interaction-group.md
  - features/tribulation-system.md
  - features/cultivation-and-breakthrough.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: true
---

# Death Penalty — Requirement Spec

## Goal

Implement hệ thống trừng phạt khi chết có ý nghĩa nhưng không phá hủy tiến trình dài hạn. Penalty áp dụng đồng nhất bất kể nguyên nhân chết. Mất đồ + mất thọ nguyên / rút ngắn Lôi Kiếp là hai trụ cột chính. Hết thọ nguyên = chết vĩnh viễn.

## Source Design Summary

Canonical design lives in `features/death-penalty.md`.
Shared rules: `shared-rules.md` — Death Taxonomy, Ownership/Drop Rights, Looting Window, PvP State Taxonomy.

## Target Design Summary

Khi player chết, server ngay lập tức:
1. Roll drop linh thạch (tỉ lệ + % random trong ngưỡng min/max).
2. Roll drop item (tỉ lệ + random 1 item droppable trên người).
3. Đồ rơi ra map với priority window cho chủ.
4. Trừ thọ nguyên (realm 1–18) hoặc rút ngắn đếm ngược Lôi Kiếp (realm 19+).
5. Xóa buff skill, giữ buff bùa chú/trận pháp.
6. Hiển thị popup chọn hồi sinh.

Nếu thọ nguyên về 0: nhân vật bị xóa vĩnh viễn.

## Current Runtime / Evidence Snapshot

- **Confirmed**: `lifespan` field trong `realm_templates` — thọ nguyên per cảnh giới đã có trong DB.
- **Confirmed**: `is_expired` / `lifespan-restricted` state đã có trong runtime.
- **Confirmed**: `breakthrough_attempts` đã có — Cultivation runtime đang chạy.
- **Not confirmed**: drop logic khi chết đã implement chưa — cần TechDesign verify.
- **Not confirmed**: buff clear on death đã implement chưa.
- **Not confirmed**: respawn/revive flow đã có chưa.
- **Not confirmed**: penalty thọ nguyên per lần chết đã hook vào death event chưa.

## Scope

### Must Implement
- Drop linh thạch khi chết: roll tỉ lệ → nếu trúng, rớt X% linh thạch đang cầm (random trong range), min/max ngưỡng
- Drop item khi chết: roll tỉ lệ → nếu trúng, random 1 item droppable trên người
- Item rơi ra map với priority window (Ownership/Drop Rights rule — xem `shared-rules.md`)
- Xóa buff skill khi chết; giữ buff từ bùa chú/trận pháp
- Penalty thọ nguyên: realm 1–18 → trừ trực tiếp vào thọ nguyên hiện tại
- Penalty Lôi Kiếp: realm 19+ → rút ngắn đếm ngược đến Lôi Kiếp tiếp theo
- Hết thọ nguyên → xóa nhân vật vĩnh viễn
- Popup chọn hồi sinh: Về Động Phủ (luôn có) / Về Checkpoint (chỉ khi map có checkpoint)
- Penalty bổ sung config per nguyên nhân chết (data-driven, không hardcode per loại)
- Đan dược tăng thọ nguyên: item consume → cộng thêm thọ nguyên (amount per item template)

### Must Not Implement
- Mất tu vi khi chết
- Mất tiềm năng khi chết
- Partial drop (drop nhiều item cùng lúc) — chỉ 1 item random
- Chi tiết recipe/nguồn gốc đan dược tăng thọ nguyên — defer đến economy phase
- Penalty Lôi Kiếp thất bại — xem `features/tribulation-system.md`
- Balance cụ thể (tỉ lệ drop, lượng thọ nguyên trừ, giá đan dược)

## Terminology

- `baseline death penalty`: penalty áp dụng cho mọi loại chết — drop + thọ nguyên/lôi kiếp.
- `additional penalty`: penalty bổ sung config per nguyên nhân chết đặc biệt (PK, raid...).
- `droppable item`: item có flag cho phép rơi khi chết; non-droppable/non-tradable không rơi.
- `priority window`: khoảng thời gian chỉ chủ nhân mới nhặt được đồ của mình.
- `permanent death`: thọ nguyên về 0 → nhân vật bị xóa, không hồi phục.
- `lifespan pool`: tổng thọ nguyên của cảnh giới hiện tại (config per realm).

## Functional Requirements

### Drop
- `REQ-001`: Khi player chết, server phải roll tỉ lệ rớt linh thạch (config). Nếu trúng: rơi X% linh thạch player đang cầm, X random trong range config, kết quả clamp vào [min, max] config.
- `REQ-002`: Khi player chết, server phải roll tỉ lệ rớt item (config). Nếu trúng: random 1 item trong danh sách item droppable trên người player. Không có droppable item → không rơi gì.
- `REQ-003`: Item non-droppable và non-tradable không bao giờ rơi khi chết, bất kể roll.
- `REQ-004`: Đồ rơi xuống map tại vị trí player chết. Priority window áp dụng theo Ownership/Drop Rights shared rule — chỉ chủ nhân nhặt được trong window; sau window mở public.
- `REQ-005`: Baseline drop áp dụng cho mọi nguyên nhân chết. Penalty bổ sung (nếu có) được config per death context type — không hardcode.

### Buff
- `REQ-006`: Khi chết, tất cả buff từ skill bị xóa ngay lập tức.
- `REQ-007`: Buff từ bùa chú và trận pháp **không bị xóa** khi chết — đây là entity bên ngoài nhân vật.

### Thọ Nguyên (realm 1–18)
- `REQ-008`: Khi player realm 1–18 chết, server trừ trực tiếp một lượng thọ nguyên (config per death context hoặc baseline config).
- `REQ-009`: Thọ nguyên không thể âm — floor tại 0.
- `REQ-010`: Khi thọ nguyên về 0: nhân vật bị xóa vĩnh viễn. Xóa nhân vật không thể hoàn tác.
- `REQ-011`: Khi đột phá cảnh giới: thọ nguyên hiện tại cộng thêm phần chênh lệch pool mới vs pool cũ (không reset về full). Ví dụ: pool cũ 30 ngày, còn 1 ngày, pool mới 60 ngày → sau đột phá còn 31 ngày.
- `REQ-012`: Đan dược tăng thọ nguyên: consume item → cộng thêm lượng thọ nguyên config per item template. Không vượt quá pool tối đa của cảnh giới hiện tại (hoặc không có cap — cần confirm trước TD handoff, xem Blocking Questions).

### Lôi Kiếp countdown (realm 19+)
- `REQ-013`: Khi player realm 19+ chết, server rút ngắn đếm ngược đến Lôi Kiếp tiếp theo (config per death context hoặc baseline config).
- `REQ-014`: Đếm ngược Lôi Kiếp không thể âm — floor tại 0 (Lôi Kiếp kích hoạt ngay nếu về 0).
- `REQ-015`: Penalty Lôi Kiếp thất bại (tụt cảnh giới) là hệ riêng — không implement trong scope này.

### Hồi Sinh
- `REQ-016`: Khi chết, player phải được hiển thị popup chọn hồi sinh.
- `REQ-017`: Option "Về Động Phủ" luôn available — tele về home cave của player.
- `REQ-018`: Option "Về Checkpoint" chỉ available khi map hiện tại có checkpoint active và map cho phép checkpoint respawn.
- `REQ-019`: Nếu player không có Động Phủ (chưa mở): tele về map/điểm default config.

## Acceptance Criteria

- `AC-001`: Given player chết với 5 viên linh thạch, roll trúng drop, khi drop resolve, then một lượng linh thạch trong range config rơi xuống vị trí chết; player còn lại phần không rơi.
- `AC-002`: Given player không có linh thạch khi chết, when death resolves, then không có linh thạch rơi dù roll trúng.
- `AC-003`: Given player có 3 item droppable, roll trúng drop item, when death resolves, then đúng 1 item random trong 3 item đó rơi xuống.
- `AC-004`: Given player có item non-droppable, khi roll trúng item drop, then item non-droppable không bao giờ được chọn vào pool random.
- `AC-005`: Given đồ rơi sau khi chết, when priority window còn active, then player khác nhấn nhặt nhận thông báo "chưa thể nhặt"; chỉ chủ nhân nhặt được.
- `AC-006`: Given priority window hết hạn, when player khác nhặt đồ, then nhặt thành công bình thường.
- `AC-007`: Given player realm 1–18 chết, when death resolves, then thọ nguyên giảm đúng lượng config; không thể âm.
- `AC-008`: Given player realm 1–18 còn 1 giây thọ nguyên và chết, when death resolves, then thọ nguyên về 0 và nhân vật bị xóa vĩnh viễn.
- `AC-009`: Given player realm 19+ chết, when death resolves, then đếm ngược Lôi Kiếp giảm đúng lượng config; không thể âm.
- `AC-010`: Given player chết với buff skill đang active, when death resolves, then tất cả buff skill bị xóa.
- `AC-011`: Given player chết với buff bùa chú đang active, when death resolves, then buff bùa chú không bị xóa.
- `AC-012`: Given player đột phá từ realm X lên realm X+1, pool cũ 30 ngày, còn 5 ngày, pool mới 60 ngày, when breakthrough completes, then thọ nguyên sau đột phá = 5 + (60-30) = 35 ngày.
- `AC-013`: Given player chết trong dungeon có checkpoint, when death popup shows, then cả 2 option "Về Động Phủ" và "Về Checkpoint" đều visible.
- `AC-014`: Given player chết trong map không có checkpoint, when death popup shows, then chỉ option "Về Động Phủ" visible.
- `AC-015`: Given player uống đan dược tăng thọ nguyên, when item is consumed, then thọ nguyên tăng đúng lượng config per item template.

## Runtime Flow

### Death Resolution
1. Player HP về 0 — death event trigger.
2. Server xác định death context type (pve, duel, pk, raid...).
3. Server roll drop linh thạch → nếu trúng, tính lượng rơi, tạo ground item.
4. Server roll drop item → nếu trúng, random 1 droppable item, tạo ground item.
5. Ground items spawn tại vị trí death với priority window.
6. Server xóa toàn bộ buff skill của player.
7. Server apply thọ nguyên penalty (realm 1–18) hoặc Lôi Kiếp penalty (realm 19+).
8. Nếu thọ nguyên = 0: trigger permanent death flow → xóa nhân vật.
9. Server apply additional penalty nếu death context có config bổ sung.
10. Server gửi death event về client → hiển thị popup hồi sinh.
11. Player chọn respawn point → tele.

### Permanent Death Flow
1. Thọ nguyên về 0 khi apply penalty.
2. Server lock nhân vật (không thể đăng nhập lại với nhân vật này).
3. Xóa hoặc archive nhân vật data (TechDesign quyết định approach).
4. Client nhận thông báo nhân vật đã mất vĩnh viễn.
5. Player được redirect về màn hình tạo nhân vật mới.

## State / Lifecycle

### Thọ Nguyên States
- `active`: còn thọ nguyên > 0, đếm ngược bình thường.
- `warning`: còn ít hơn threshold (threshold cần xác định — xem Blocking Questions).
- `expired`: thọ nguyên = 0 → trigger permanent death.

### Lôi Kiếp States
- `counting_down`: đếm ngược bình thường.
- `imminent`: còn ít hơn threshold (cần xác định).
- `triggered`: đếm ngược về 0 → Lôi Kiếp kích hoạt (detail trong `features/tribulation-system.md`).

### Nhân Vật States liên quan
- `alive`: bình thường.
- `dead`: đang trong death flow, chờ respawn.
- `permanently_dead`: nhân vật đã bị xóa.

## Rules And Invariants

- Baseline death penalty áp dụng cho **mọi** nguyên nhân chết — không có exception.
- Không bao giờ mất tu vi hay tiềm năng khi chết.
- Chỉ rớt tối đa 1 item per lần chết.
- Item non-droppable/non-tradable không bao giờ vào drop pool.
- Thọ nguyên và Lôi Kiếp countdown không thể âm.
- Buff bùa chú/trận pháp không bị clear bởi death.
- Permanent death không thể hoàn tác.
- Additional penalty hoàn toàn config-driven — không hardcode per death context trong code.

## Data / Config Requirements

| Config key | Notes |
|---|---|
| `death.drop_lingstone_rate` | Tỉ lệ % rớt linh thạch khi chết |
| `death.drop_lingstone_pct_min` | % min linh thạch rớt |
| `death.drop_lingstone_pct_max` | % max linh thạch rớt |
| `death.drop_lingstone_min_amount` | Ngưỡng sàn số viên rớt (tạm 1) |
| `death.drop_lingstone_max_amount` | Ngưỡng trần số viên rớt (tạm 10) |
| `death.drop_item_rate` | Tỉ lệ % rớt item khi chết (tạm 5%) |
| `death.lifespan_penalty_seconds` | Lượng thọ nguyên trừ per lần chết (baseline) |
| `death.tribulation_penalty_seconds` | Lượng rút ngắn Lôi Kiếp per lần chết (baseline) |
| `death.priority_window_seconds` | Thời gian ưu tiên nhặt đồ của chủ |
| `death.lifespan_warning_threshold_seconds` | Ngưỡng cảnh báo thọ nguyên thấp (TBD) |
| `realm_templates.lifespan_pool` | Pool thọ nguyên per cảnh giới (đã có DB) |
| `death_context_additional_penalty` | Config bảng penalty bổ sung per context type |

- Item droppable flag: per item template trong item schema.
- Đan dược tăng thọ nguyên: lượng thọ nguyên tăng per item template.

## UI / UX Requirements

- Thanh thọ nguyên hiển thị rõ trong UI chính, đếm ngược realtime.
- Warning UI khi thọ nguyên thấp hơn threshold.
- Khi chết: popup chọn hồi sinh (Về Động Phủ / Về Checkpoint nếu có).
- Popup hiển thị đồ vừa rơi và thời gian còn priority window.
- Thông báo permanent death rõ ràng, không thể bỏ qua.

## Telemetry / Logs / Debug Needs

- Log mỗi death event: player_id, realm, death_context, drop_lingstone_result, drop_item_result, lifespan_before/after, tribulation_countdown_before/after.
- Log permanent death: player_id, realm, timestamp.
- Log respawn: player_id, respawn_point chosen.
- Log buff clear: player_id, buffs cleared count.
- Log lifespan restore (đan dược): player_id, item_template, amount_restored.

## Related Systems

- `features/death-penalty.md` — canonical feature design source.
- `shared-rules.md` — Death Taxonomy, Ownership/Drop Rights, Looting Window, PvP State Taxonomy.
- `features/home-cave-defense.md` — penalty nặng hơn khi chết lúc bị công/thủ phủ.
- `features/player-interaction-group.md` — định nghĩa pk vs lawful pvp context.
- `features/tribulation-system.md` — Lôi Kiếp trigger khi countdown về 0.
- `features/spirit-beast.md` — pet death rule riêng, không dùng lifespan system của player.

## Blocking Questions (cần chốt trước Dev handoff)

1. **Thọ nguyên tối đa khi uống đan dược**: có cap tại pool cảnh giới hiện tại không, hay có thể vượt pool? Ví dụ: pool 30 ngày, đang còn 25 ngày, uống đan +10 ngày → kết quả là 30 hay 35?
2. **Threshold cảnh báo thọ nguyên thấp**: bao nhiêu % hoặc bao nhiêu giây thì hiển thị warning?
3. **Permanent death — data**: xóa hẳn hay archive nhân vật? (TechDesign quyết định approach nhưng cần biết constraint game design — ví dụ: tên nhân vật có được tái sử dụng không?)

## Known Conflicts / Drift

- `death.priority_window_seconds` dùng chung với Ownership/Drop Rights rule — cần TechDesign đảm bảo cùng 1 config source, không duplicate.
- Chưa confirm drop logic và buff-clear-on-death đã có trong server runtime chưa.

## Readiness Level

- Ready for TechDesign refinement: **yes**
- Ready for Dev handoff: **pending** — 3 blocking questions cần chốt (có thể TechDesign hỏi lại GameDesign trong quá trình spec)
- Ready for QA: **no** — chờ implementation

## Handoff Checklist

- [x] No blocking design questions block TD from starting.
- [x] Acceptance criteria are testable.
- [x] Config/data impacts are listed.
- [x] Edge cases are covered.
- [x] Shared rules referenced explicitly.
- [x] Blocking questions called out clearly for TD to raise back.
- [x] `handoff_ready: true`
