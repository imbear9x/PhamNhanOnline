---
doc_type: game_design_requirement
system_id: mineral-vein-ownership-and-siege
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/mineral-vein-system.md
related_docs:
  - features/mineral-vein-system.md
  - requirements/sect-core.md
  - requirements/sect-pvp.md
  - requirements/home-cave-defense.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: true
---

# Mỏ Linh Thạch — Ownership & Siege — Requirement Spec

## Goal

Implement vòng đời sở hữu và tranh đoạt mỏ linh thạch: spawn, chiếm mỏ vô chủ, sở hữu cá nhân / tông môn, cổng mỏ, công phá bằng bùa phá mỏ, priority window sau khi cổng vỡ, và reset ownership khi mỏ bị chiếm lại hoặc cạn trữ lượng.

## Source Design Summary

Canonical design: `features/mineral-vein-system.md` — sections Spawn mỏ, Chiếm mỏ, Bảo vệ mỏ, Công phá mỏ, System States.
Shared rules: `shared-rules.md` — Structure Deployment Cast Time / Setup Lock.

## Target Design Summary

Mỏ spawn random trên các map whitelist, random zone, random vị trí, tối đa 3 mỏ mỗi map. Mỏ vô chủ được chiếm bằng **bản vẽ khai thác** tiêu hao 1 lần. Người chiếm chọn danh nghĩa **cá nhân** hoặc **tông môn**. Sau khi chiếm, server tạo **cổng mỏ** làm lớp bảo vệ ownership.

Người khác muốn cướp phải dùng **bùa phá mỏ** để phát động chiến dịch công. Cổng mỏ có HP và có time limit chiến dịch. Nếu hết giờ mà chưa phá xong, cổng hồi đầy HP và combat kết thúc. Nếu cổng vỡ, attacker last hit nhận **priority window 1 phút** để chiếm lại bằng bản vẽ khai thác **danh nghĩa tông môn**; hết 1 phút thì mỏ trở về `unclaimed`.

## Current Runtime / Evidence Snapshot

- **Not confirmed**: mineral vein runtime/service đã có chưa.
- **Not confirmed**: gate entity / siege timer / priority window runtime đã có chưa.
- **Not confirmed**: NPC bán bản vẽ khai thác / bùa phá mỏ đã có chưa.
- **Confirmed**: setup lock rule dùng chung blueprint deployment đã canonical trong `shared-rules.md`.

## Scope

### Must Implement
- Spawn mỏ trên map whitelist, random zone, random vị trí, tối đa 3 mỏ per map
- Mỏ tồn tại vô hạn cho đến khi cạn trữ lượng, không có TTL thời gian
- Chiếm mỏ vô chủ bằng bản vẽ khai thác tiêu hao 1 lần
- Danh nghĩa chiếm: cá nhân hoặc tông môn
- Chọn danh nghĩa tông môn chỉ cho người có quyền Quản lý khai thác mỏ
- Sau khi chiếm: tạo cổng mỏ
- Bùa phá mỏ phát động chiến dịch công, có time limit
- Cổng mỏ có HP; HP về 0 → cổng vỡ
- Hết giờ công phá mà cổng chưa vỡ → cổng hồi đầy HP về ban đầu
- Cổng vỡ → người bên trong bị tele ra map random xung quanh; không tính là chết
- Last hit cổng mỏ được priority 1 phút để chiếm lại bằng bản vẽ **danh nghĩa tông môn**
- Sau 1 phút priority → mỏ về `unclaimed`, cổng biến mất
- Mỏ cạn trữ lượng → state `depleted`, biến mất; admin dùng tool sinh mỏ mới
- Cổng mỏ mới sau khi chiếm lại reset hoàn toàn về trống
- Không có cơ chế thu dọn mỏ

### Must Not Implement
- TTL thời gian cho mỏ
- Auto-respawn mỏ theo timer runtime
- Thu dọn / đóng gói mỏ
- Balance cụ thể HP cổng, tỉ lệ spawn, giá item

## Terminology

- `mineral vein`: mỏ linh thạch spawn trong zone.
- `vein gate`: cổng mỏ, lớp bảo vệ ownership giữa map ngoài và bên trong mỏ.
- `bản vẽ khai thác`: item tiêu hao 1 lần để chiếm mỏ vô chủ.
- `bùa phá mỏ`: item tiêu hao để phát động công phá mỏ.
- `priority window`: 1 phút sau khi cổng vỡ, attacker last hit được ưu tiên chiếm lại.
- `unclaimed`: mỏ vô chủ.
- `owned`: mỏ đang có chủ cá nhân hoặc tông môn.
- `depleted`: mỏ cạn trữ lượng và biến mất.

## Functional Requirements

### Spawn
- `REQ-001`: Mỏ chỉ spawn trên các map được whitelist trong config.
- `REQ-002`: Trong map whitelist, mỏ spawn random vào zone cụ thể và random vị trí trong zone đó.
- `REQ-003`: Số mỏ cùng lúc trên 1 map tối đa 3.
- `REQ-004`: Mỏ không broadcast toàn server khi spawn — player phải tự đi tìm.
- `REQ-005`: Mỏ không có TTL thời gian; chỉ biến mất khi trữ lượng về 0.

### Claim ownership
- `REQ-006`: Mỏ `unclaimed` được chiếm bằng bản vẽ khai thác; item bị tiêu hao 1 lần khi claim thành công.
- `REQ-007`: Claim mỏ phải qua cast time 1 phút theo Structure Deployment Cast Time / Setup Lock rule — trong lúc cast không bị tấn công và chưa ai vào được mỏ.
- `REQ-008`: Người claim chọn danh nghĩa `personal` hoặc `sect`.
- `REQ-009`: Danh nghĩa `sect` chỉ available nếu player thuộc tông môn và có quyền Quản lý khai thác mỏ.
- `REQ-010`: Sau claim thành công: mỏ chuyển state `owned`, server tạo cổng mỏ mới trống hoàn toàn.

### Siege
- `REQ-011`: Công phá mỏ chỉ bắt đầu khi attacker dùng bùa phá mỏ (item tiêu hao).
- `REQ-012`: Khi dùng bùa phá mỏ, attacker chọn danh nghĩa `personal` hoặc `sect`.
- `REQ-013`: Danh nghĩa `sect` khi công mỏ chỉ cho người có quyền Quản lý khai thác mỏ khởi động chiến dịch.
- `REQ-014`: Chiến dịch công có time limit config. Hết thời gian mà cổng chưa vỡ → combat kết thúc và cổng hồi đầy HP.
- `REQ-015`: Cổng mỏ có HP config. HP về 0 → cổng vỡ.
- `REQ-016`: Khi cổng vỡ: mỏ mất ownership hiện tại, tất cả người bên trong mỏ bị tele ra map random xung quanh. Đây không phải death event, không áp dụng death penalty.
- `REQ-017`: Trận pháp trong cổng mỏ bị phá hủy hoàn toàn khi cổng vỡ.
- `REQ-018`: Linh thú thủ mỏ còn sống → về túi nghỉ; đã chết → về túi nghỉ và nhận penalty theo rule linh thú.

### Priority window
- `REQ-019`: Người hoặc tông môn last hit cổng mỏ nhận priority window 60 giây để claim lại mỏ.
- `REQ-020`: Trong priority window, chỉ claim bằng bản vẽ **danh nghĩa tông môn** mới được hưởng quyền ưu tiên.
- `REQ-021`: Dùng bản vẽ danh nghĩa cá nhân trong priority window không được hưởng ưu tiên.
- `REQ-022`: Hết priority window mà chưa ai claim thành công → mỏ về state `unclaimed`, cổng mỏ biến mất.
- `REQ-023`: Sau priority window, nếu nhiều người cùng claim thì server resolve theo thứ tự request thắng trước.

### Depletion
- `REQ-024`: Khi trữ lượng mỏ về 0 → mỏ chuyển `depleted`, cổng đóng, mọi player bên trong bị tele ra, ownership mất.
- `REQ-025`: Sau khi `depleted`, server không auto-spawn mỏ mới. Admin dùng server tool sinh mỏ mới dựa theo tốc độ khai thác thực tế.

## Acceptance Criteria

- `AC-001`: Given map không nằm trong whitelist spawn, when spawn job runs, then không có mỏ nào xuất hiện ở map đó.
- `AC-002`: Given map whitelist đã có 3 mỏ active, when spawn job runs, then không spawn thêm mỏ thứ 4.
- `AC-003`: Given player có bản vẽ khai thác và claim mỏ vô chủ thành công, when cast completes, then bản vẽ bị tiêu hao, mỏ chuyển `owned`, cổng mỏ được tạo.
- `AC-004`: Given player không có quyền Quản lý khai thác mỏ, when cố claim danh nghĩa sect, then action bị reject.
- `AC-005`: Given attacker dùng bùa phá mỏ và hết time limit mà cổng chưa vỡ, when timer expires, then combat kết thúc và cổng hồi đầy HP.
- `AC-006`: Given cổng mỏ HP về 0, when break resolves, then tất cả người bên trong bị tele ra map random xung quanh và không nhận death penalty.
- `AC-007`: Given attacker last hit cổng mỏ, when priority window active, then attacker/tông môn attacker có 60 giây ưu tiên claim lại bằng bản vẽ danh nghĩa sect.
- `AC-008`: Given priority window hết mà chưa ai claim, when timer expires, then mỏ về `unclaimed`, cổng biến mất.
- `AC-009`: Given mỏ cạn trữ lượng khi đang owned, when depletion resolves, then ownership mất, cổng đóng, người bên trong bị tele ra.

## Runtime Flow

### Flow 1 — Spawn & unclaimed state
1. Server spawn mỏ ở map whitelist, zone random, vị trí random.
2. Mỏ xuất hiện `unclaimed`.
3. Player khám phá ra mỏ và dùng bản vẽ khai thác để claim.

### Flow 2 — Claim
1. Player dùng bản vẽ khai thác lên mỏ `unclaimed`.
2. Cast 1 phút với setup lock.
3. Chọn danh nghĩa personal/sect.
4. Cast complete → item tiêu hao → ownership set → cổng mỏ tạo.

### Flow 3 — Siege
1. Attacker dùng bùa phá mỏ, chọn danh nghĩa.
2. Siege timer bắt đầu; cổng mỏ nhận damage.
3. Nếu timer hết trước khi cổng vỡ → cổng full HP, kết thúc.
4. Nếu cổng HP = 0 → cổng vỡ, người trong mỏ tele ra, priority window bắt đầu.
5. Attacker last hit có 60 giây ưu tiên claim sect.
6. Hết 60 giây chưa claim → mỏ về `unclaimed`.

## State / Lifecycle

| State | Mô tả |
|---|---|
| `unclaimed` | Mỏ vô chủ, ai cũng có thể claim |
| `owned` | Mỏ có chủ cá nhân hoặc tông môn |
| `priority_window` | 60 giây sau khi cổng vỡ, attacker last hit được ưu tiên chiếm |
| `depleted` | Mỏ cạn trữ lượng, biến mất |

## Rules And Invariants

- Mỏ không có TTL thời gian.
- Claim và siege đều phải qua item tiêu hao riêng.
- Tele khi cổng vỡ không phải death event.
- Priority window chỉ ưu tiên claim sect, không ưu tiên claim cá nhân.
- Cổng mỏ mới sau khi claim lại luôn reset trống hoàn toàn.
- Không có cơ chế thu dọn mỏ.

## Data / Config Requirements

| Config key | Notes |
|---|---|
| `mineral_vein.map_whitelist` | Danh sách map được spawn |
| `mineral_vein.max_active_per_map` | Mặc định 3 |
| `mineral_vein.priority_window_seconds` | 60 |
| `mineral_vein.gate_hp` | Balance config |
| `mineral_vein.siege_duration_seconds` | Time limit công phá |
| `mineral_vein.spawn_tooling` | Ops tool spawn mỏ mới |

## Telemetry / Logs / Debug Needs

- Log spawn: map_id, zone_id, position, amount.
- Log claim: player_id, vein_id, affiliation, success/fail.
- Log siege start/end: attacker, affiliation, duration, result.
- Log gate break: last_hit, defenders_teleported_count.
- Log priority window claim winner.
- Log depletion.

## Related Systems

- `requirements/mineral-vein-mining-access.md` — khai thác, alliance, whitelist.
- `requirements/sect-core.md` — quyền Quản lý khai thác mỏ.
- `requirements/sect-pvp.md` — conceptual parallel về gate siege.
- `requirements/home-cave-defense.md` — setup lock / gate-defense model reference.
- `shared-rules.md` — Structure Deployment Cast Time / Setup Lock.

## Blocking Questions

- **None** at design level. TechDesign verify item/tooling/runtime existence.

## Known Conflicts / Drift

- Chưa confirm runtime support cho siege timer, gate entity, claim lock, priority window.

## Readiness Level

- Ready for TechDesign: **yes**
- Ready for Dev handoff: **pending** — runtime verify
- Ready for QA: **no**

## Handoff Checklist

- [x] No blocking design questions.
- [x] Acceptance criteria testable.
- [x] Scope split clear from mining-access doc.
- [x] Config/data outlined.
- [x] `handoff_ready: true`
