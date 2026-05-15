---
doc_type: game_design_requirement
system_id: mineral-vein-mining-access
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/mineral-vein-system.md
related_docs:
  - features/mineral-vein-system.md
  - requirements/mineral-vein-ownership-and-siege.md
  - requirements/sect-core.md
  - requirements/sect-task-welfare.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: true
---

# Mỏ Linh Thạch — Mining Access & Interior Rules — Requirement Spec

## Goal

Implement phần truy cập và khai thác bên trong mỏ: liên minh khai thác, safe interior, khai thác passive theo MP, ownership access rules, và 2-tier access cho mỏ thuộc tông môn (nhiệm vụ / whitelist).

## Source Design Summary

Canonical design: `features/mineral-vein-system.md` — sections Bảo vệ mỏ, Liên minh khai thác, Khai thác, Khai Thác Mỏ Tông Môn.

## Target Design Summary

Chỉ chủ mỏ và danh sách được phép mới vào được bên trong mỏ. Bên trong mỏ là safe zone. Player đứng khai thác thì bị khóa toàn bộ thao tác khác, không di chuyển; linh thạch tự ra theo tốc độ dựa trên MP. Với mỏ cá nhân, output vào balo. Với mỏ tông môn, có 2 tier độc lập:
- **Khai thác nhiệm vụ**: đang giữ NV khai thác active → khai thác đến hết quota → output về bảo khố, NV tự done
- **Whitelist thoải mái**: được add whitelist → không giới hạn, output vào túi cá nhân

Nếu player vừa có NV vừa trong whitelist: xong NV tiếp tục khai thác vào túi, không bị out.

## Current Runtime / Evidence Snapshot

- **Not confirmed**: passive mining runtime đã có chưa.
- **Not confirmed**: MP-based output formula runtime đã có chưa.
- **Not confirmed**: safe-zone interior đã có chưa.
- **Not confirmed**: alliance/whitelist access runtime đã có chưa.

## Scope

### Must Implement
- Liên minh khai thác: tối đa 10 người cùng lúc (config)
- Chủ mỏ mời / kick thành viên liên minh
- Nếu chủ mỏ offline: mỏ vẫn hoạt động, alliance vẫn giữ nguyên
- Cổng mỏ cho phép vào interior chỉ với chủ mỏ / alliance / người đủ điều kiện tier sect
- Bên trong mỏ là safe zone — không thể bị tấn công khi đang khai thác
- Khai thác là passive, chỉ khi đứng bên trong mỏ
- Khi khai thác: khóa toàn bộ thao tác khác, không di chuyển
- Thoát khỏi mỏ → dừng khai thác ngay
- Tốc độ khai thác khác nhau giữa player, dựa theo MP
- Khai thác tự do / whitelist → linh thạch vào balo
- Mỏ tông môn khai thác theo nhiệm vụ → linh thạch về bảo khố tự động, NV tự done, không cần báo cáo
- 2-tier access độc lập: nhiệm vụ khai thác và whitelist thoải mái
- Hết quota NV, không trong whitelist → bị out khỏi mỏ
- Vừa có NV vừa whitelist → xong NV tiếp tục khai thác vào túi
- Không giới hạn lượng khai thác per phiên nếu là khai thác tự do / whitelist và mỏ còn trữ lượng
- Tối đa 1 trận pháp che mắt + 1 phòng ngự + 1 tấn công trong cổng mỏ
- Mỗi thành viên liên minh được đặt 1 linh thú thủ mỏ

### Must Not Implement
- PvP bên trong interior
- Move while mining
- Multi-mine session by one player in multiple veins simultaneously
- Balance công thức MP cụ thể

## Terminology

- `alliance`: chủ mỏ + danh sách người được mời khai thác.
- `vein interior`: khu vực bên trong mỏ, safe zone.
- `whitelist thoải mái`: danh sách thành viên tông môn được khai thác vào túi cá nhân, không cần NV.
- `khai thác nhiệm vụ`: đệ tử có NV khai thác active → output về bảo khố, NV tự done.

## Functional Requirements

### Alliance & access
- `REQ-001`: Liên minh khai thác tối đa 10 người cùng lúc (config).
- `REQ-002`: Chủ mỏ có quyền mời và kick thành viên liên minh.
- `REQ-003`: Nếu chủ mỏ offline, mỏ vẫn hoạt động bình thường; alliance list vẫn có hiệu lực.
- `REQ-004`: Chỉ chủ mỏ, alliance, hoặc người đủ điều kiện access tier của mỏ tông môn mới vào được interior.
- `REQ-005`: Nếu alliance đã đủ 10 người, muốn mời thêm phải kick người cũ trước.

### Interior / mining behavior
- `REQ-006`: Bên trong mỏ là safe zone — không player nào bị tấn công khi đang ở interior khai thác.
- `REQ-007`: Player chỉ khai thác được khi đang đứng trong interior.
- `REQ-008`: Khi bắt đầu khai thác, player bị khóa toàn bộ thao tác khác và không thể di chuyển.
- `REQ-009`: Khi player rời interior hoặc bị tele ra, khai thác dừng ngay lập tức.
- `REQ-010`: Tốc độ khai thác dựa trên MP của player theo công thức config-driven.
- `REQ-011`: Khai thác tự do không có giới hạn lượng per phiên — miễn mỏ còn trữ lượng.
- `REQ-012`: Linh thạch khai thác tự do hoặc whitelist vào thẳng balo player.

### Sect-owned vein — 2 tier access
- `REQ-013`: Mỏ thuộc tông môn hỗ trợ 2 tier truy cập độc lập:
  - `task_mining`: có NV khai thác active
  - `free_whitelist`: có trong whitelist thoải mái
- `REQ-014`: Player có NV khai thác active được vào interior và khai thác cho đến hết quota NV; output về bảo khố tự động; NV tự done.
- `REQ-015`: Player có trong whitelist thoải mái được vào interior, khai thác không giới hạn, output vào túi cá nhân.
- `REQ-016`: Nếu player vừa có NV vừa trong whitelist: trong thời gian chưa xong quota NV, output về bảo khố; xong NV thì tiếp tục khai thác vào túi cá nhân mà không bị out.
- `REQ-017`: Nếu player chỉ có NV, không trong whitelist: khi hết quota NV thì bị out khỏi mỏ ngay.
- `REQ-018`: Môn chủ / người có quyền Quản lý khai thác mỏ quản lý whitelist thoải mái.
- `REQ-019`: Player không cần về sect báo cáo khi xong NV khai thác — hệ thống tự done + notify.
- `REQ-020`: Muốn nhận thêm NV khai thác mới, player phải về sect map để nhận.

### Gate defense loadout
- `REQ-021`: Trong cổng mỏ, chủ mỏ/alliance được đặt tối đa 1 trận pháp che mắt, 1 trận pháp phòng ngự, 1 trận pháp tấn công.
- `REQ-022`: Mỗi thành viên liên minh được đặt tối đa 1 linh thú thủ mỏ.

## Acceptance Criteria

- `AC-001`: Given alliance hiện có 10 người, when chủ mỏ cố mời người thứ 11, then bị reject.
- `AC-002`: Given chủ mỏ offline, when thành viên alliance vào mỏ, then vẫn vào được bình thường.
- `AC-003`: Given player đứng trong interior và bắt đầu khai thác, when mining active, then player bị khóa thao tác và không di chuyển được.
- `AC-004`: Given player đang khai thác, when player rời interior, then khai thác dừng ngay.
- `AC-005`: Given player A có MP cao hơn player B, when cả hai khai thác cùng thời gian, then output của A cao hơn theo công thức config.
- `AC-006`: Given mỏ thuộc tông môn, đệ tử có NV khai thác active, when quota chưa đủ, then output về bảo khố và NV tiến độ tăng.
- `AC-007`: Given đệ tử có NV active và đồng thời có trong whitelist, when quota NV vừa đủ, then từ tick tiếp theo output chuyển từ bảo khố sang túi cá nhân, player không bị out.
- `AC-008`: Given đệ tử chỉ có NV active, không trong whitelist, when quota NV đủ, then player bị out khỏi mỏ.
- `AC-009`: Given player không thuộc alliance/whitelist và không có NV, when cố vào interior, then bị reject.

## Runtime Flow

### Khai thác cá nhân / alliance
1. Chủ mỏ hoặc thành viên alliance vào interior.
2. Bắt đầu khai thác → thao tác lock.
3. Server tick output theo công thức MP.
4. Output vào túi player.
5. Player rời interior / mỏ cạn / cổng vỡ → dừng khai thác.

### Khai thác mỏ tông môn
1. Đệ tử nhận NV khai thác từ sect.
2. Đệ tử vào mỏ nếu có NV active hoặc có trong whitelist.
3. Nếu đang trong quota NV → output về bảo khố.
4. Quota đủ → NV tự done, notify player.
5. Nếu có whitelist → tiếp tục output vào túi.
6. Nếu không có whitelist → bị out khỏi mỏ.

## Rules And Invariants

- Interior là safe zone — không combat trong lúc khai thác.
- Mining lock chặn mọi thao tác khác và di chuyển.
- Access tier cho mỏ sect là độc lập và có thể chồng nhau.
- Output NV khai thác luôn tự về bảo khố; không có báo cáo thủ công.
- Không giới hạn lượng khai thác tự do per phiên.

## Data / Config Requirements

| Config key | Notes |
|---|---|
| `mineral_vein.max_alliance_members` | Mặc định 10 |
| `mineral_vein.base_mining_rate` | Rate base |
| `mineral_vein.mining_rate_formula_by_mp` | Công thức / curve theo MP |
| `mineral_vein.max_camouflage_formation` | 1 |
| `mineral_vein.max_defense_formation` | 1 |
| `mineral_vein.max_attack_formation` | 1 |
| `mineral_vein.max_pet_guard_per_member` | 1 |

- Alliance schema: vein_id, player_id, role/owner, added_at.
- Whitelist schema for sect veins: vein_id/sect_id, player_id, added_by, added_at.
- Mining session schema: player_id, vein_id, start_time, output_mode (personal/treasury/task).

## Telemetry / Logs / Debug Needs

- Log alliance invite/kick.
- Log whitelist add/remove.
- Log mining start/stop, reason stop.
- Log output per mining session: player_id, vein_id, mode, total_output.
- Log task auto-done from mining quota.

## Related Systems

- `requirements/mineral-vein-ownership-and-siege.md` — ownership/siege prerequisite.
- `requirements/sect-core.md` — permission Quản lý khai thác mỏ.
- `requirements/sect-task-welfare.md` — NV khai thác active source.
- `features/spirit-beast.md` — linh thú thủ mỏ rule reference.
- `features/crafting-talisman-formation.md` — trận pháp thủ mỏ.

## Blocking Questions

- **None** at design level. TechDesign verify passive mining runtime, safe-zone enforcement, MP formula integration.

## Known Conflicts / Drift

- Chưa confirm runtime support cho passive mining lock, alliance membership, whitelist, auto-output-to-treasury.

## Readiness Level

- Ready for TechDesign: **yes**
- Ready for Dev handoff: **pending** — ownership/siege prerequisite + runtime verify
- Ready for QA: **no**

## Handoff Checklist

- [x] No blocking design questions.
- [x] Acceptance criteria testable.
- [x] Scope split clear from ownership-and-siege doc.
- [x] Config/data outlined.
- [x] `handoff_ready: true`
