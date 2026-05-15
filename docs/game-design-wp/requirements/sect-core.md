---
doc_type: game_design_requirement
system_id: sect-core
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/sect-system.md
related_docs:
  - features/sect-system.md
  - requirements/sect-task-welfare.md
  - requirements/sect-shop.md
  - requirements/sect-pvp.md
  - requirements/inbox-mail-system.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: true
---

# Tông Môn — Core (Thành lập / Vai trò / Bảo khố / Gia nhập / Chat) — Requirement Spec

## Goal

Implement nền tảng tông môn: thành lập, giải tán, phân quyền, kế thừa, bảo khố, gia nhập/rời, và chat nội bộ. Đây là prerequisite cho sect-task-welfare, sect-shop, sect-pvp.

## Source Design Summary

Canonical design: `features/sect-system.md` — sections A, B, C, D, E, F.
Shared rules: `shared-rules.md` — Escrow, Blueprint/Bản vẽ, Structure Deployment Cast Time.

## Target Design Summary

Player mua bản vẽ tông môn tại NPC → chọn vị trí map public → cast 1 phút → nộp ≥ 10 linh thạch vào bảo khố → tông môn tồn tại. Môn chủ phân quyền thủ công cho đệ tử. Bảo khố là tài khoản chung — escrow phúc lợi + thưởng tự động lock. Kế thừa tự động khi môn chủ offline > 100 ngày theo thứ tự ưu tiên.

## Current Runtime / Evidence Snapshot

- **Not confirmed**: sect system đã có trong server runtime chưa — TechDesign verify.
- **Not confirmed**: bản vẽ tông môn item schema đã có chưa.
- **Not confirmed**: bảo khố entity đã có chưa.
- **Confirmed**: blueprint/bản vẽ model dùng chung với home-cave — xem shared-rules.md.
- **Confirmed**: Escrow rule canonical — shared-rules.md.

## Scope

### Must Implement
- Mua bản vẽ tông môn tại NPC → thành lập tông môn với cast 1 phút + nộp ≥ 10 linh thạch
- Tối đa 1 bản vẽ / người, không giao dịch được
- Tối đa 200 thành viên
- Ngưỡng tồn tại: bảo khố < 10 linh thạch → warning 24h → giải tán tự động
- Giải tán chủ động (chỉ môn chủ) → tài sản bảo khố về tay môn chủ
- Giải tán tự động (bảo khố cạn / không còn ai kế thừa) → tài sản xử lý theo rule tương ứng
- 5 quyền hạn (xem REQ bên dưới): phân thủ công per thành viên, không gắn danh hiệu
- 3 danh hiệu thuần túy: Phó tông chủ, Trưởng lão, Đệ tử — không có quyền mặc định
- Chuyển giao môn chủ tự nguyện: ngay lập tức, không cần accept
- Kế thừa tự động khi môn chủ offline > 100 ngày: ưu tiên Phó tông chủ → người có quyền bảo khố gia nhập sớm nhất → thành viên gia nhập sớm nhất; nếu tất cả offline > 100 ngày → giải tán
- Gia nhập qua cổng tông môn: gửi đơn → người có quyền phê duyệt; không có phí gia nhập
- Gia nhập giữa tuần: không tham gia nhiệm vụ bắt buộc, không nhận phúc lợi tuần đó
- Rời tông môn: tự rời bất kỳ lúc nào, cooldown 1 ngày trước khi gia nhập tông môn khác
- Kick thành viên: người có quyền Quản lý thành viên; không kick được đệ tử đang giữ NV bắt buộc chưa xong; không kick được người cùng có quyền này
- Bảo khố: tất cả thành viên xem được; rút linh thạch bởi người có quyền, floor ngưỡng 10 LS sau khi rút
- Chat kênh All (toàn bộ thành viên) + kênh Private (add thủ công)

### Must Not Implement
- Liên minh tông môn in-game
- Phí gia nhập
- Cấp bậc tông môn
- Quyền tự động gắn theo danh hiệu
- Balance cụ thể (giá bản vẽ, %, HP...)

## Terminology

- `môn chủ`: người thành lập hoặc được kế thừa — toàn quyền quản lý.
- `phó tông chủ`: danh hiệu, ưu tiên số 1 trong chuỗi kế thừa tự động.
- `trưởng lão`: danh hiệu thuần túy, không có quyền mặc định.
- `đệ tử`: tên gọi chung tất cả thành viên bình thường.
- `bảo khố`: kho tài sản chung — linh thạch và vật phẩm.
- `ngưỡng tồn tại`: 10 linh thạch tối thiểu trong bảo khố.
- `bản vẽ tông môn`: item 1/người, không giao dịch — lưu mô hình + nội dung tông môn.
- `escrow`: tài sản bị lock tự động khi tạo nhiệm vụ / phúc lợi. Xem `shared-rules.md`.

## Functional Requirements

### Thành lập
- `REQ-001`: Player cần có bản vẽ tông môn (mua NPC) để thành lập. Mỗi người tối đa 1 bản vẽ — không giao dịch.
- `REQ-002`: Thành lập yêu cầu chọn vị trí hợp lệ trên map public + cast time 1 phút (Structure Deployment Cast Time rule — không bị tấn công, không ai vào được trong lúc cast).
- `REQ-003`: Sau cast: nộp ≥ 10 linh thạch vào bảo khố → tông môn tồn tại. Người thành lập = môn chủ.
- `REQ-004`: Tối đa 200 thành viên. Server reject gia nhập khi đã đủ 200.

### Ngưỡng tồn tại & Giải tán
- `REQ-005`: Khi bảo khố < 10 linh thạch: server gửi thông báo toàn tông môn + bắt đầu đếm ngược 24h.
- `REQ-006`: Nếu bảo khố không được bổ sung về ≥ 10 linh thạch trong 24h: tự động giải tán — tài sản bảo khố (trừ escrow đang lock) về tay môn chủ.
- `REQ-007`: Giải tán chủ động (chỉ môn chủ): tài sản bảo khố về tay môn chủ ngay lập tức.
- `REQ-008`: Giải tán khi không còn ai kế thừa (tất cả offline > 100 ngày): tài sản mất hết — không về tay ai.

### Vai trò & Quyền hạn
- `REQ-009`: 5 quyền hạn độc lập — phân thủ công per thành viên, không gắn với danh hiệu:
  1. Quản lý thành viên (phê duyệt gia nhập, sa thải)
  2. Quản lý bảo khố & shop (rút/nạp bảo khố, quản lý shop)
  3. Đặt nhiệm vụ tông môn (tạo NV bắt buộc và tự nguyện)
  4. Quản lý chat nhóm (add thành viên vào kênh private)
  5. Quản lý khai thác mỏ (giao chỉ tiêu, whitelist, dùng danh nghĩa tông môn)
- `REQ-010`: 3 danh hiệu thuần túy: Phó tông chủ, Trưởng lão, Đệ tử. Danh hiệu không tự cấp quyền.
- `REQ-011`: Người có quyền Quản lý thành viên không sa thải được người cùng có quyền đó — chỉ môn chủ sa thải được họ.
- `REQ-012`: Không được kick đệ tử đang giữ instance nhiệm vụ bắt buộc chưa hoàn thành. Server block action.

### Kế thừa
- `REQ-013`: Chuyển giao tự nguyện: môn chủ chọn người → chuyển ngay lập tức, không cần accept. Môn chủ cũ trở thành đệ tử thường.
- `REQ-014`: Kế thừa tự động khi môn chủ offline > 100 ngày: hệ thống kiểm tra theo thứ tự ưu tiên và chọn người đầu tiên chưa offline > 100 ngày: (1) Phó tông chủ → (2) người có quyền Quản lý bảo khố gia nhập sớm nhất → (3) thành viên gia nhập sớm nhất.
- `REQ-015`: Nếu tất cả ứng viên kế thừa đều offline > 100 ngày: tự động giải tán, tài sản mất.

### Gia nhập & Rời
- `REQ-016`: Gia nhập qua cổng tông môn: gửi đơn → người có quyền Quản lý thành viên phê duyệt. Không có phí gia nhập.
- `REQ-017`: Đệ tử gia nhập giữa tuần: không được nhận instance nhiệm vụ bắt buộc tuần đó, không nhận phúc lợi tuần đó. Tính từ tuần tiếp theo.
- `REQ-018`: Rời tông môn: tự do, bất kỳ lúc nào. Cooldown 1 ngày trước khi gia nhập tông môn khác.
- `REQ-019`: Bị kick: bản vẽ động phủ của đệ tử (nếu đang đặt trong khu vực tông môn) trả về tay đệ tử nguyên vẹn — item không mất.

### Bảo khố
- `REQ-020`: Tất cả thành viên xem được nội dung bảo khố (số linh thạch, vật phẩm).
- `REQ-021`: Rút linh thạch: chỉ người có quyền Quản lý bảo khố. Server enforce: bảo khố sau khi rút phải ≥ 10 linh thạch.
- `REQ-022`: Escrow phúc lợi và escrow thưởng nhiệm vụ bị lock tự động — không ai rút được phần đó. Xem `shared-rules.md` Escrow rule.

### Chat
- `REQ-023`: Kênh All: tất cả thành viên thấy và gửi được.
- `REQ-024`: Kênh Private: chỉ vào được khi được người có quyền Quản lý chat nhóm add thủ công.

## Acceptance Criteria

- `AC-001`: Given player có bản vẽ tông môn, chọn vị trí hợp lệ, nộp ≥ 10 LS sau cast 1 phút, then tông môn tồn tại và player trở thành môn chủ.
- `AC-002`: Given player đã có 1 bản vẽ tông môn, when player cố mua thêm bản vẽ, then bị reject — tối đa 1/người.
- `AC-003`: Given bảo khố giảm xuống 9 LS, when server detects, then toàn tông môn nhận thông báo và đếm ngược 24h bắt đầu.
- `AC-004`: Given đếm ngược 24h kết thúc mà bảo khố vẫn < 10 LS, when timer expires, then tông môn tự động giải tán, tài sản về môn chủ.
- `AC-005`: Given môn chủ giải tán chủ động, when confirmed, then tài sản bảo khố về môn chủ ngay lập tức.
- `AC-006`: Given đệ tử A được giao quyền Quản lý thành viên, when A cố kick đệ tử B cũng có quyền đó, then action bị block.
- `AC-007`: Given đệ tử đang giữ NV bắt buộc chưa hoàn thành, when người có quyền cố kick, then action bị block.
- `AC-008`: Given môn chủ offline > 100 ngày và có Phó tông chủ chưa offline > 100 ngày, when succession triggers, then Phó tông chủ trở thành môn chủ mới.
- `AC-009`: Given tất cả ứng viên kế thừa offline > 100 ngày, when succession check runs, then tông môn tự động giải tán, tài sản mất.
- `AC-010`: Given đệ tử gia nhập giữa tuần, when weekly check runs, then đệ tử không thấy NV bắt buộc trong pool tuần hiện tại và không nhận phúc lợi tuần đó.
- `AC-011`: Given đệ tử rời tông môn, when re-join another sect within 1 day, then bị block — cooldown chưa hết.
- `AC-012`: Given người có quyền bảo khố cố rút linh thạch khiến bảo khố còn 5 LS, when rút, then bị reject — phải để lại ≥ 10 LS.

## Runtime Flow

### Thành lập
1. Player mua bản vẽ tại NPC.
2. Chọn vị trí map public → server validate vị trí hợp lệ.
3. Cast 1 phút (setup lock — không bị tấn công, không ai vào).
4. Player nộp ≥ 10 LS vào bảo khố → server tạo sect entity.
5. Player = môn chủ. Bản vẽ đính vào tông môn.

### Kế thừa tự động
1. Server job check offline time của môn chủ mỗi ngày (hoặc on-login trigger).
2. Nếu offline > 100 ngày: duyệt danh sách theo thứ tự ưu tiên.
3. Người đầu tiên chưa offline > 100 ngày → trở thành môn chủ mới.
4. Nếu không tìm được: tự động giải tán.

## State / Lifecycle

| State | Mô tả |
|---|---|
| `active` | Tông môn đang vận hành bình thường |
| `warning` | Bảo khố < 10 LS, đếm ngược 24h |
| `under_attack` | Cổng đang bị tấn công (xem sect-pvp) |
| `looting_window` | Cổng vỡ, 1 phút looting (xem sect-pvp) |
| `dissolved` | Đã giải tán |

## Rules And Invariants

- Tối đa 1 bản vẽ / người — không giao dịch.
- Tối đa 200 thành viên.
- Quyền không tự động gắn theo danh hiệu.
- Người cùng quyền Quản lý thành viên không kick được nhau.
- Không kick đệ tử đang giữ NV bắt buộc chưa xong.
- Bảo khố không thể rút xuống dưới 10 LS.
- Escrow lock là bất khả xâm phạm — chỉ resolve khi task/phúc lợi xử lý xong.
- Không có liên minh tông môn.

## Data / Config Requirements

| Config key | Default | Notes |
|---|---|---|
| `sect.min_treasury` | 10 | Ngưỡng tồn tại linh thạch |
| `sect.max_members` | 200 | Tối đa thành viên |
| `sect.dissolution_countdown_hours` | 24 | Đếm ngược khi bảo khố cạn |
| `sect.offline_succession_days` | 100 | Ngưỡng offline kế thừa |
| `sect.cooldown_rejoin_hours` | 24 | Cooldown sau khi rời |

- Sect entity schema: sect_id, name, founder_id, leader_id, treasury_lingstone, treasury_items, state, created_at.
- Member schema: sect_id, player_id, title, permissions_bitmask, joined_at, last_active.
- Blueprint item schema: blueprint_type=sect, owner_id, sect_data (serialized).

## Telemetry / Logs / Debug Needs

- Log thành lập / giải tán: sect_id, reason, timestamp.
- Log quyền hạn thay đổi: sect_id, target_player_id, old_perms, new_perms, by_player_id.
- Log kế thừa: sect_id, old_leader, new_leader, trigger_reason.
- Log gia nhập / rời / kick: sect_id, player_id, action, by_player_id.

## Related Systems

- `requirements/sect-task-welfare.md` — nhiệm vụ + phúc lợi (phụ thuộc sect-core).
- `requirements/sect-shop.md` — shop tông môn (phụ thuộc sect-core).
- `requirements/sect-pvp.md` — cổng + PvP (phụ thuộc sect-core).
- `requirements/inbox-mail-system.md` — phúc lợi / reward overflow vào inbox.
- `shared-rules.md` — Escrow, Blueprint, Structure Deployment Cast Time.

## Blocking Questions

- **None** — tất cả design đã chốt. TechDesign verify runtime existence trước khi Dev handoff.

## Known Conflicts / Drift

- Sect system chưa confirm có trong runtime hay chưa.
- Blueprint model dùng chung với home-cave — TechDesign cần confirm shared schema hay separate.

## Readiness Level

- Ready for TechDesign: **yes**
- Ready for Dev handoff: **pending** — verify runtime existence
- Ready for QA: **no**

## Handoff Checklist

- [x] No blocking design questions.
- [x] Acceptance criteria testable.
- [x] Config/data outlined.
- [x] Related requirements linked.
- [x] `handoff_ready: true`
