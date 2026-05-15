---
doc_type: game_design_requirement
system_id: sect-pvp
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/sect-system.md
related_docs:
  - features/sect-system.md
  - features/mineral-vein-system.md
  - requirements/sect-core.md
  - requirements/home-cave-defense.md
  - requirements/death-penalty.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: true
---

# Tông Môn — PvP / Cổng / Looting Window / Động Phủ Trong Khu — Requirement Spec

## Goal

Implement phần PvP chiến lược của tông môn: cổng tông môn, bùa phá tông môn, trạng thái under_attack, cổng vỡ = giải tán, looting window 1 phút, và quy tắc động phủ nằm trong khu vực tông môn.

**Prerequisite:** `requirements/sect-core.md`.
**Deferred external dependency:** cơ chế tranh mỏ / chiếm mỏ chi tiết nằm ở `features/mineral-vein-system.md`, chưa thuộc requirement này.

## Source Design Summary

Canonical design: `features/sect-system.md` — sections K (sect-side access only), L, M, N.
Shared rules: `shared-rules.md` — Structure Loot Drop Rate, Ownership/Drop Rights, Looting Window, Blueprint.

## Target Design Summary

Chỉ có 2 hình thức PvP tông môn: tranh mỏ (defer system khác) và công cổng tông môn bằng bùa phá. Cổng vỡ = giải tán ngay. Bảo khố + hàng shop rơi ngẫu nhiên theo structure drop rate; phần còn lại đính vào bản vẽ về tay môn chủ. Looting window 1 phút: phe công ở lại map, PvP tự do, không được rời, offline rơi toàn bộ đồ đã nhặt. Sau 1 phút: tele toàn bộ ra map random.

## Current Runtime / Evidence Snapshot

- **Not confirmed**: cổng tông môn entity/runtime combat đã có chưa.
- **Not confirmed**: bùa phá tông môn item + activation flow đã có chưa.
- **Confirmed**: Structure Loot Drop Rate canonical ở shared-rules.md.
- **Confirmed**: Death penalty trong looting window vẫn dùng baseline death penalty.

## Scope

### Must Implement
- Cổng tông môn trên map public
- Bùa phá tông môn (item tiêu hao) kích hoạt chiến dịch công
- Trạng thái `under_attack`
- Shop tạm đóng cho người ngoài khi `under_attack`
- Cổng HP về 0 → tông môn giải tán ngay
- Bảo khố + hàng shop rơi ngẫu nhiên theo Structure Loot Drop Rate
- Phe thủ tele ra map random ngay khi cổng vỡ
- Bản vẽ tông môn: phần còn lại đính vào bản vẽ về tay môn chủ; nếu mất bản vẽ thì phải mua mới và lập từ đầu
- Looting window 1 phút: chỉ người đang trong map (phe công) được loot, PvP tự do, không rời map, offline → rơi toàn bộ đồ đã nhặt
- Sau 1 phút: toàn bộ người còn lại tele ra map random; ai sống giữ đồ đã nhặt
- Động phủ trong khu vực tông môn: không thể bị công phá trực tiếp; phải phá cổng trước
- Slot động phủ trong khu vực: giới hạn, ai đặt trước được trước
- 1 đệ tử chỉ có 1 động phủ tại 1 nơi tại 1 thời điểm
- Bị kick: bản vẽ động phủ trả về tay đệ tử nguyên vẹn
- Tông môn giải tán: động phủ trong khu biến mất; item / linh thú có tỉ lệ rơi mất, phần còn lại về túi player

### Must Not Implement
- Liên minh tông môn
- Tranh mỏ chi tiết (defer sang mineral-vein-system)
- Balance HP cổng, tỉ lệ drop, nguồn bùa phá — data design
- Overlap khu vực nhiều tông môn trên cùng map — defer map design

## Functional Requirements

### Công cổng
- `REQ-001`: Công tông môn chỉ qua bùa phá tông môn (item tiêu hao). Kích hoạt thành công → sect state = `under_attack`.
- `REQ-002`: Khi `under_attack`, shop tạm đóng cho người ngoài. Các hệ phụ thuộc đọc state này từ sect-core/sect-pvp.
- `REQ-003`: Cổng tông môn có HP config. Khi HP về 0: cổng vỡ và tông môn giải tán ngay lập tức.

### Giải tán do cổng vỡ
- `REQ-004`: Khi cổng vỡ: bảo khố (bao gồm hàng shop + escrow shop) rơi ngẫu nhiên theo Structure Loot Drop Rate shared rule. Phần không rơi đính vào bản vẽ tông môn → về tay môn chủ.
- `REQ-005`: Phe thủ bị teleport ra map random ngay khi cổng vỡ.
- `REQ-006`: Bản vẽ động phủ của đệ tử trả về từng đệ tử nguyên vẹn.
- `REQ-007`: Nếu bản vẽ tông môn còn: môn chủ dùng bản vẽ đó để tái lập ở vị trí mới — khôi phục nguyên trạng trừ phần tài sản đã rơi theo drop rate.
- `REQ-008`: Nếu bản vẽ tông môn bị mất: phải mua bản vẽ mới tại NPC và lập tông môn từ đầu — không phục hồi được dữ liệu cũ.

### Looting window
- `REQ-009`: Sau khi cổng vỡ, bắt đầu looting window 60 giây.
- `REQ-010`: Chỉ người đang trong map tại thời điểm đó (phe công) được tham gia loot trong window.
- `REQ-011`: Trong looting window: PvP tự do giữa mọi người trong map.
- `REQ-012`: Trong looting window: không ai được rời map trước khi hết 60 giây.
- `REQ-013`: Nếu player offline trong looting window: toàn bộ đồ đã nhặt trong window rơi ra ngay trong map.
- `REQ-014`: Nếu player chết trong looting window: death penalty baseline áp dụng bình thường; respawn về động phủ cá nhân nếu có, nếu không có → map random.
- `REQ-015`: Khi hết 60 giây: tất cả player còn trong map bị tele ra map random. Người sống giữ đồ đã nhặt.

### Động phủ trong khu vực tông môn
- `REQ-016`: Động phủ đặt trong khu vực tông môn không thể bị công phá trực tiếp. Muốn công phá phải phá cổng tông môn trước.
- `REQ-017`: Slot động phủ trong khu vực sect là finite config; ai đặt trước giữ slot, hết slot thì người sau không đặt được.
- `REQ-018`: 1 đệ tử chỉ được có 1 động phủ tại 1 nơi tại 1 thời điểm (trong sect hoặc ngoài map thường, không đồng thời).
- `REQ-019`: Đệ tử tự đặt / tự rút động phủ, không cần phê duyệt.
- `REQ-020`: Khi đệ tử bị kick: bản vẽ động phủ trả về tay đệ tử nguyên vẹn — item không mất.
- `REQ-021`: Khi tông môn giải tán: động phủ trong khu vực biến mất. Item / linh thú có tỉ lệ rơi mất; phần còn lại về túi người chơi theo config.

## Acceptance Criteria

- `AC-001`: Given attacker dùng bùa phá tông môn thành công, when battle starts, then sect state chuyển `under_attack`.
- `AC-002`: Given sect đang `under_attack`, when người ngoài cố mở shop, then bị từ chối với thông báo shop đang đóng.
- `AC-003`: Given cổng HP về 0, when break resolves, then tông môn giải tán ngay, phe thủ tele ra map random, looting window 60 giây bắt đầu.
- `AC-004`: Given cổng vỡ, when structure drop resolves, then một phần tài sản rơi theo Structure Loot Drop Rate; phần còn lại đính vào bản vẽ về tay môn chủ.
- `AC-005`: Given attacker đang trong map khi looting window active, when attacker loot item rồi offline, then toàn bộ đồ đã nhặt trong window rơi ra ngay trong map.
- `AC-006`: Given attacker đang trong looting window, when cố rời map trước 60 giây, then action bị block.
- `AC-007`: Given looting window hết 60 giây, when timer expires, then toàn bộ player còn lại tele ra map random; ai sống giữ đồ đã nhặt.
- `AC-008`: Given đệ tử có động phủ trong khu vực sect, when sect còn cổng nguyên vẹn, then động phủ đó không thể bị công phá trực tiếp.
- `AC-009`: Given slot động phủ trong khu đã đầy, when đệ tử khác cố đặt thêm, then action bị reject.
- `AC-010`: Given đệ tử bị kick khỏi sect, when kick resolves, then bản vẽ động phủ về tay đệ tử nguyên vẹn.

## Runtime Flow

### Cổng vỡ
1. Attacker dùng bùa phá → sect state `under_attack`.
2. Cổng bị tấn công đến HP = 0.
3. Server dissolve sect ngay lập tức.
4. Resolve structure drop từ bảo khố + shop.
5. Tele phe thủ ra map random.
6. Bắt đầu looting window 60 giây cho phe công trong map.
7. Hết 60 giây → tele toàn bộ ra map random.
8. Môn chủ nhận bản vẽ tông môn chứa phần tài sản còn lại.

## Rules And Invariants

- Cổng vỡ = giải tán ngay, không có state chờ xác nhận.
- Looting window chỉ áp dụng cho người đang trong map tại thời điểm cổng vỡ.
- Offline trong looting window = rơi toàn bộ đồ đã nhặt trong window.
- Động phủ trong khu không bị công phá trực tiếp khi cổng còn sống.
- Không có liên minh tông môn.

## Data / Config Requirements

| Config key | Default / Note |
|---|---|
| `sect.looting_window_seconds` | 60 |
| `sect.cave_slot_limit` | Data design |
| `sect.item_drop_pct_on_destruction` | Data design |
| `sect.treasury_attack_drop_pct` | Data design |
| `sect.gate_hp` | Data design |

## Telemetry / Logs / Debug Needs

- Log bùa phá kích hoạt: sect_id, attacker_id, timestamp.
- Log cổng vỡ: sect_id, timestamp, drop summary.
- Log looting window join/loot/offline/death/teleport out.
- Log động phủ slot occupancy trong khu vực sect.

## Related Systems

- `requirements/sect-core.md` — prerequisite.
- `requirements/sect-shop.md` — shop đóng khi under_attack.
- `requirements/home-cave-defense.md` — blueprint/cave interaction model.
- `requirements/death-penalty.md` — baseline death penalty trong looting window.
- `shared-rules.md` — Structure Loot Drop Rate, Ownership/Drop Rights, Blueprint.

## Blocking Questions

- **None** at design level. Mineral-vein-specific combat vẫn defer system khác.

## Known Conflicts / Drift

- Requires code verification: cổng entity / battle runtime / offline-drop handling chưa confirm có trong code chưa.

## Readiness Level

- Ready for TechDesign: **yes**
- Ready for Dev handoff: **pending** — cần sect-core + runtime verify
- Ready for QA: **no**

## Handoff Checklist

- [x] No blocking design questions.
- [x] Acceptance criteria testable.
- [x] Config/data outlined.
- [x] External deferred dependency explicit.
- [x] `handoff_ready: true`
