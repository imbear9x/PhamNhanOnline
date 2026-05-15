---
doc_type: game_design_requirement
system_id: sect-shop
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/sect-system.md
related_docs:
  - features/sect-system.md
  - requirements/sect-core.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: true
---

# Tông Môn — Shop — Requirement Spec

## Goal

Implement shop tông môn mở 24/7 cho cả thành viên và người ngoài: 30 slot bán ra + 30 slot thu mua, tiền tệ duy nhất là linh thạch, realtime notify khi có thay đổi, lịch sử giao dịch 1 ngày.

**Prerequisite:** `requirements/sect-core.md`.

## Source Design Summary

Canonical design: `features/sect-system.md` — section J.
Shared rules: `shared-rules.md` — Escrow.

## Target Design Summary

Shop tông môn có 2 bảng độc lập: bán ra và thu mua. Hàng bán lấy từ bảo khố, tiền thu về thẳng bảo khố. Buy order escrow linh thạch ngay khi tạo, item bán vào order đi thẳng vào bảo khố. Khi bị tấn công bằng bùa phá tông môn, shop tạm đóng. Khi giải tán, hàng + escrow về bảo khố.

## Current Runtime / Evidence Snapshot

- **Not confirmed**: sect shop đã có runtime chưa.
- **Not confirmed**: realtime notify infra đã có chưa.
- **Confirmed**: escrow rule canonical — shared-rules.md.

## Scope

### Must Implement
- Shop mở 24/7 cho cả thành viên và người ngoài
- Tiền tệ duy nhất: linh thạch
- Tab bán ra: 30 slot cố định; mỗi loại item = 1 slot; stackable theo item system
- Tab thu mua: 30 slot cố định; độc lập với bán ra
- Đưa hàng bán: item trừ khỏi bảo khố ngay khi đưa lên shop
- Mua hàng: trả linh thạch → nhận item → tiền vào bảo khố
- Tạo buy order: escrow linh thạch ngay khi tạo đơn
- Bán vào buy order: player nộp item → nhận linh thạch từ escrow → item vào bảo khố
- Chỉnh giá / số lượng buy order: không support edit trực tiếp — phải hủy và tạo lại
- Realtime notify cho người đang mở giao diện khi shop thay đổi
- Lịch sử giao dịch lưu 1 ngày gần nhất, tự xóa sau đó
- Khi under_attack: shop tạm đóng, người ngoài thấy thông báo
- Khi giải tán: shop đóng, hàng + escrow về bảo khố

### Must Not Implement
- Phí giao dịch
- Đấu giá
- Chỉnh sửa trực tiếp buy order
- Tiền tệ khác ngoài linh thạch

## Functional Requirements

- `REQ-001`: Shop có 2 bảng độc lập: `sell_board` và `buy_board`, mỗi bảng 30 slot cố định.
- `REQ-002`: `sell_board`: mỗi loại item chiếm 1 slot riêng. Stackable theo item system.
- `REQ-003`: Khi người có quyền đưa item lên `sell_board`, item bị trừ khỏi bảo khố ngay lập tức.
- `REQ-004`: Người mua (thành viên hoặc người ngoài) mua item từ `sell_board` bằng linh thạch. Tiền vào bảo khố ngay.
- `REQ-005`: `buy_board`: khi tạo order, linh thạch bị escrow khỏi bảo khố ngay lập tức.
- `REQ-006`: Player bán item vào `buy_board`: item đi vào bảo khố, player nhận linh thạch từ escrow.
- `REQ-007`: Không hỗ trợ edit trực tiếp giá/số lượng buy order. Muốn đổi phải hủy order cũ rồi tạo order mới.
- `REQ-008`: Shop đang `under_attack`: người ngoài không thể mua/bán; UI hiển thị thông báo "Tông môn đang bị tấn công, vui lòng quay lại sau".
- `REQ-009`: Người đang mở giao diện shop phải nhận realtime notify khi có thay đổi, và UI refresh.
- `REQ-010`: Lịch sử giao dịch lưu 1 ngày gần nhất rồi tự xóa.
- `REQ-011`: Khi tông môn giải tán: shop đóng ngay; hàng `sell_board` + escrow `buy_board` hoàn về bảo khố trước khi bảo khố xử lý tiếp theo flow giải tán.

## Acceptance Criteria

- `AC-001`: Given shop bán ra có 30 slot đầy, when người có quyền cố thêm item loại mới, then bị block — hết slot.
- `AC-002`: Given item đã đưa lên sell_board, when một người mua mua thành công, then item rời shop, linh thạch vào bảo khố.
- `AC-003`: Given buy order tạo 100 LS escrow, when order created, then 100 LS bị lock khỏi bảo khố ngay.
- `AC-004`: Given player bán item vào buy order, when giao dịch thành công, then item vào bảo khố, player nhận linh thạch từ escrow.
- `AC-005`: Given người có quyền muốn đổi giá buy order, when thao tác edit trực tiếp, then bị block; chỉ có thể hủy và tạo lại.
- `AC-006`: Given player đang mở UI shop, when người khác mua 1 item làm shop thay đổi, then player đầu tiên nhận notify realtime và UI refresh.
- `AC-007`: Given tông môn đang under_attack, when người ngoài cố mở shop, then nhận thông báo shop tạm đóng.
- `AC-008`: Given tông môn giải tán khi còn hàng bán và buy order escrow, when dissolve resolves, then hàng + escrow hoàn về bảo khố trước khi xử lý tài sản tiếp theo.

## Runtime Flow

### Bán ra
1. Người có quyền chọn item trong bảo khố → đưa lên sell_board.
2. Server trừ item khỏi bảo khố, chiếm 1 slot.
3. Người mua mở shop, chọn item, trả linh thạch.
4. Server trừ linh thạch người mua, cộng vào bảo khố, chuyển item cho người mua.
5. Realtime notify cho người đang mở UI.

### Thu mua
1. Người có quyền tạo buy order: chọn item target, số lượng, giá.
2. Server escrow linh thạch khỏi bảo khố.
3. Player bán item vào order.
4. Server chuyển item vào bảo khố, giải ngân linh thạch escrow cho player.
5. Nếu order full hoặc bị hủy → resolve escrow phần chưa dùng.

## Rules And Invariants

- Chỉ linh thạch là tiền tệ hợp lệ.
- Sell board và buy board độc lập, mỗi board 30 slot.
- Edit trực tiếp buy order không được phép.
- Shop under_attack thì đóng cho người ngoài.
- Realtime notify là bắt buộc cho người đang mở UI.
- Hàng + escrow phải hoàn về bảo khố trước khi giải tán hoàn tất.

## Data / Config Requirements

| Config key | Default |
|---|---|
| `sect.shop_sell_slots` | 30 |
| `sect.shop_buy_slots` | 30 |
| `sect.shop_transaction_log_days` | 1 |

- Sell slot schema: sect_id, item_template_id, quantity, price, created_by.
- Buy order schema: sect_id, item_template_id, remaining_qty, unit_price, escrow_lingstone, created_by.
- Transaction log schema: sect_id, side, player_id, item_template_id, quantity, unit_price, timestamp.

## Telemetry / Logs / Debug Needs

- Log add/remove sell item, create/cancel buy order, trade success/fail.
- Log escrow create/resolve for buy orders.
- Log under_attack shop access deny.

## Related Systems

- `requirements/sect-core.md` — prerequisite.
- `shared-rules.md` — Escrow rule.
- `requirements/sect-pvp.md` — under_attack state source.

## Blocking Questions

- **None** at design level. TechDesign verify realtime notify mechanism existing or not.

## Known Conflicts / Drift

- Không có conflict. Requires code verification cho realtime notify implementation approach.

## Readiness Level

- Ready for TechDesign: **yes**
- Ready for Dev handoff: **pending** — cần sect-core + runtime verify
- Ready for QA: **no**

## Handoff Checklist

- [x] No blocking design questions.
- [x] Acceptance criteria testable.
- [x] Config/data outlined.
- [x] Prerequisite explicit.
- [x] `handoff_ready: true`
