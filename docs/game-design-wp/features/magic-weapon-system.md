---
doc_type: game_design_feature
system_id: magic-weapon-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/equipment-refining.md
  - features/cultivation-and-breakthrough.md
  - features/death-penalty.md
  - shared-rules.md
requires_code_verification: false
---

# Hệ Thống Pháp Khí — Feature Draft

## Goal

Pháp khí là trang bị có thể mặc lên người, cung cấp chỉ số và skill. Có 2 loại: loại tĩnh (chỉ số cố định) và loại sinh trưởng (có exp, lên cấp, mở khóa chỉ số và skill theo cấp). Tạo thêm chiều sâu cho equipment progression, đặc biệt loại sinh trưởng tạo sự gắn kết dài hạn giữa player và pháp khí.

## Design Summary

Pháp khí là equipment trang bị được, chế tạo qua hệ Forge trong `equipment-refining.md`. Công thức chế tạo cố định — không tự phát minh. Nguyên liệu là khoáng thạch hoặc bất kỳ item nào được data design chỉ định.

**Loại 1 — Tĩnh**: chỉ số cố định sau khi chế tạo, không thay đổi theo thời gian. Giao dịch thoải mái.

**Loại 2 — Sinh trưởng**: có exp và cấp. Exp tăng khi trang bị trên người và tu luyện hoặc farm quái (theo cultivation rule). Lên cấp mở khóa chỉ số và/hoặc skill theo data design. Có penalty cấp riêng. Khi giao dịch: tụt về cấp 0, có cảnh báo trước.

## Scope

### In Scope
- 2 loại pháp khí: tĩnh và sinh trưởng
- Chế tạo qua Forge — công thức cố định
- Chỉ số và skill per cấp (loại sinh trưởng)
- Exp từ tu luyện và farm quái khi đang trang bị
- Penalty cấp pháp khí (loại sinh trưởng)
- Giao dịch: loại tĩnh tự do; loại sinh trưởng tụt cấp 0 khi giao dịch
- Tối đa 4 slot trang bị pháp khí (đã config trong backend)

### Out Of Scope
- Balance chỉ số, skill cụ thể per cấp — data design
- Tỉ lệ exp nhận được — data design
- Số cấp tối đa per pháp khí — data design
- Danh sách nguyên liệu chế tạo cụ thể — data design

## Pháp Khí Types

### Loại 1 — Tĩnh
- Chỉ số cố định ngay sau khi chế tạo.
- Không có exp, không có cấp, không thay đổi theo thời gian trang bị.
- **Giao dịch tự do** — không bị ảnh hưởng gì khi sang tay.

### Loại 2 — Sinh Trưởng
- Có **exp** và **cấp**.
- Exp tăng khi: đang trang bị trên người **và** đang tu luyện hoặc farm quái — tuân thủ cùng rule cultivation/farming.
- Mỗi cấp mở khóa thêm **chỉ số** hoặc **skill** (active/passive) hoặc cả hai — data design xác định per cấp per pháp khí.
- **Giao dịch**: tụt về **cấp 0** ngay khi giao dịch hoàn tất. Có **cảnh báo rõ ràng** trước khi xác nhận giao dịch.
- Người nhận pháp khí đã giao dịch: nhận ở cấp 0, tự tăng lại từ đầu.

## Cultivation Rule cho Pháp Khí Sinh Trưởng

Pháp khí sinh trưởng nhận exp theo cùng điều kiện tu luyện:
- Phải **đang được trang bị trên người**.
- Exp đến khi **đang tu luyện** (meditation/cultivation activity) hoặc **đang farm quái** (quái chết khi player tham gia tiêu diệt).
- Offline time: nếu player offline trong lúc đang tu luyện, pháp khí vẫn nhận exp theo offline time-based activities rule (xem `shared-rules.md`).
- Tháo pháp khí ra khỏi slot: không nhận exp cho đến khi trang bị lại.

## Penalty Pháp Khí

Pháp khí sinh trưởng có **penalty cấp riêng** — xảy ra trong các trường hợp được data design cấu hình (ví dụ: chết, thất bại trong một số hoạt động, v.v.).

Canonical rule penalty pháp khí:
- Khi penalty xảy ra: **exp bị trừ theo % exp hiện tại** của cấp đó — tỉ lệ config per penalty type.
- Nếu exp tụt xuống dưới 0 của cấp hiện tại → **tụt 1 cấp**, exp về mức tương ứng.
- Tụt cấp: **chỉ số và skill đã mở khóa ở cấp đó bị khóa lại** cho đến khi lên lại.
- Không có "bình cảnh" — sau khi tụt có thể lên lại ngay khi đủ exp.
- Rule này là shared rule riêng cho pháp khí — không dùng chung cultivation penalty rule của player.

## Player-Facing Rules

### Slot trang bị
- Tối đa **4 slot** pháp khí trên người — đã config trong backend.
- Có thể mix loại tĩnh và sinh trưởng trong 4 slot.

### Chế tạo
- Qua hệ **Forge** trong `equipment-refining.md`.
- Công thức cố định — không tự phát minh.
- Nguyên liệu: khoáng thạch và/hoặc item bất kỳ theo data design.

### Giao dịch
- **Loại tĩnh**: giao dịch tự do, không điều kiện.
- **Loại sinh trưởng**: giao dịch được, nhưng pháp khí **tụt về cấp 0** ngay khi giao dịch hoàn tất.
  - Cảnh báo hiển thị trước khi xác nhận: "Pháp khí sẽ trở về cấp 0 sau khi giao dịch."
  - Player phải xác nhận lần 2 mới hoàn tất.

### Skill từ pháp khí
- Skill mở khóa theo cấp — có thể là **active** (dùng trong combat) hoặc **passive** (buff thường trực).
- Skill bị khóa lại nếu pháp khí tụt cấp xuống dưới cấp mở khóa skill đó.

## System States — Loại Sinh Trưởng
- `equipped_growing`: đang trang bị, đang nhận exp.
- `equipped_idle`: đang trang bị nhưng không trong trạng thái nhận exp (không tu luyện, không farm).
- `unequipped`: trong kho, không nhận exp.
- `penalized`: vừa nhận penalty, exp/cấp đã giảm.

## Main Flows

### Flow 1 — Chế tạo pháp khí
1. Player mở Forge, chọn công thức pháp khí.
2. Đưa nguyên liệu vào → bắt đầu luyện chế.
3. Hoàn thành → nhận pháp khí (loại tĩnh: chỉ số đã cố định; loại sinh trưởng: cấp 0).

### Flow 2 — Tăng cấp pháp khí sinh trưởng
1. Player trang bị pháp khí loại sinh trưởng.
2. Tu luyện hoặc farm quái → pháp khí nhận exp.
3. Đủ exp → lên cấp tự động → chỉ số/skill mới mở khóa.
4. Tháo ra → exp dừng lại, cấp giữ nguyên.

### Flow 3 — Giao dịch pháp khí sinh trưởng
1. Player mở trade, chọn pháp khí sinh trưởng.
2. Cảnh báo hiện ra: "Pháp khí sẽ trở về cấp 0 sau khi giao dịch."
3. Player xác nhận lần 2.
4. Giao dịch hoàn tất → pháp khí về cấp 0 trong tay người nhận.

### Flow 4 — Penalty pháp khí
1. Penalty event xảy ra (chết hoặc trigger khác theo data design).
2. Server tính % exp trừ per penalty type.
3. Exp trừ; nếu về âm → tụt cấp, skill/chỉ số cấp đó bị khóa.
4. Player thấy notification penalty.

## Edge Cases
- Tháo pháp khí ra trong lúc đang tu luyện: pháp khí dừng nhận exp ngay lập tức.
- Balo đầy khi nhận pháp khí từ Forge: vào inbox theo shared overflow rule.
- Pháp khí sinh trưởng đang ở cấp 0 bị penalty: exp không thể âm hơn — giữ ở 0, không tụt thêm.
- Giao dịch pháp khí sinh trưởng trong lúc đang tu luyện: tháo ra khỏi slot trước khi giao dịch (server enforce).
- Player offline khi tu luyện với pháp khí trang bị: exp vẫn tích lũy theo offline time-based activities rule.

## Data / Config Needs
- Pháp khí template: ID, tên, loại (static/growing), chỉ số base, công thức chế tạo → DB
- Cấp + chỉ số/skill mở khóa per cấp (loại sinh trưởng) → DB
- Exp required per cấp (loại sinh trưởng) → DB
- Penalty type list + % exp trừ per type → DB
- Slot pháp khí max: 4 → đã config backend
- Cảnh báo giao dịch flag per item type → DB

## UI / UX Notes
- Pháp khí trong inventory: hiển thị loại (tĩnh/sinh trưởng), cấp hiện tại, exp bar (loại sinh trưởng).
- Khi trang bị: hiện chỉ số và skill đang active.
- Skill bị khóa do tụt cấp: hiển thị rõ "Khóa — cần cấp X".
- Cảnh báo giao dịch: popup 2 bước, không thể bỏ qua.
- Notification penalty: ngắn gọn, rõ pháp khí nào bị ảnh hưởng.

## Related Systems
- **Equipment Refining** (`features/equipment-refining.md`): Forge là nơi chế tạo pháp khí.
- **Cultivation & Breakthrough** (`features/cultivation-and-breakthrough.md`): exp pháp khí theo cùng cultivation rule.
- **Death Penalty** (`features/death-penalty.md`): chết có thể trigger penalty pháp khí.
- **Offline Time-Based Activities** (`shared-rules.md`): exp pháp khí tích lũy offline khi đang tu luyện.
- **Inbox** (`features/inbox-mail-system.md`): overflow khi nhận pháp khí.

## Key Decisions
1. 2 loại: tĩnh (chỉ số cố định) và sinh trưởng (exp/cấp).
2. Exp sinh trưởng: chỉ khi đang trang bị + đang tu luyện hoặc farm quái.
3. Penalty pháp khí: trừ % exp hiện tại; tụt cấp nếu về âm; skill/chỉ số bị khóa khi tụt.
4. Giao dịch sinh trưởng: tụt về cấp 0, cảnh báo 2 bước bắt buộc.
5. Giao dịch tĩnh: tự do.
6. Công thức cố định — không tự phát minh.
7. Tối đa 4 slot — đã config backend.
8. Offline: exp vẫn tích lũy nếu đang tu luyện khi tháo.

## Open Questions
- [ ] Penalty pháp khí xảy ra trong những trường hợp nào ngoài chết — data design xác định per penalty type.
- [ ] Số cấp tối đa per pháp khí — data design.
- [ ] Exp rate khi tu luyện vs farm quái có khác nhau không — data design.

## Known Conflicts / Drift
- Penalty pháp khí là **rule riêng**, không dùng cultivation penalty rule của player (xem `shared-rules.md` — Cultivation Penalty). Cần đảm bảo TechDesign implement 2 hệ penalty này độc lập.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/` — open questions là data design, không block; Known Conflict cần TechDesign lưu ý.
