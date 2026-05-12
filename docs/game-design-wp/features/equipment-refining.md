---
doc_type: game_design_feature
system_id: equipment-refining
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-12
updated_at: 2026-05-12
promoted_from: notes/equipment-refining.md
related_docs:
  - features/multi-stage-crafting.md
  - features/machine-system.md
  - features/crafting-talisman-formation.md
requires_code_verification: false
---

# Luyện Khí / Equipment Refining — Feature Draft

## Goal

Tạo hệ craft nguồn cho vật liệu tinh luyện trung gian và pháp khí / trang bị thành phẩm. Luyện Khí là nơi biến nguyên liệu thô thành component đầu vào trực tiếp cho các hệ craft khác (Khôi Lỗi, Trận Pháp, Phù Lục...). Các hệ đó chỉ cần khai báo component trực tiếp — chain tạo ra component đó là trách nhiệm của Luyện Khí.

## Design Summary

Luyện Khí là 1 hệ với 2 nhánh: **Refine** (tinh luyện vật liệu) và **Forge** (rèn pháp khí / trang bị). Refine không cần bản vẽ, 1 nguyên liệu thô cho ra 1 output cố định, có tỉ lệ thành công base riêng per loại nguyên liệu. Lò luyện và phụ liệu chỉ tăng tỉ lệ thành công. Fail thì mất nguyên liệu đầu vào. Forge cần bản vẽ. Cả 2 nhánh đều là luyện chế, tuân thủ framework luyện chế chung, diễn ra tại Luyện Khí Thất.

## Scope

### In Scope
- Nhánh Refine: tinh luyện vật liệu thô thành refined material
- Nhánh Forge: rèn pháp khí / trang bị từ refined material + bản vẽ
- Tỉ lệ thành công base per nguyên liệu
- Lò luyện và phụ liệu tăng tỉ lệ thành công
- Fail mất nguyên liệu đầu vào
- Output vào thẳng balo
- Diễn ra tại Luyện Khí Thất

### Out Of Scope
- Danh sách cụ thể nguyên liệu và refined material — data design
- Tỉ lệ thành công cụ thể per item — data design / balance
- Thời gian refine cụ thể — data design / balance
- Mastery / kỹ năng riêng per công đoạn
- Durability cho component
- Quality random ở mọi tầng

## Core Loop

### Refine
1. Player thu thập nguyên liệu thô từ thế giới.
2. Vào Luyện Khí Thất, chọn nguyên liệu muốn refine.
3. UI hiển thị: output sẽ ra gì, tỉ lệ thành công, thời gian, hậu quả khi fail.
4. Player có thể thêm lò luyện / phụ liệu để tăng tỉ lệ thành công.
5. Bắt đầu refine → tuân thủ framework luyện chế chung.
6. Thành công → refined material vào balo.
7. Thất bại → mất nguyên liệu đầu vào.

### Forge
1. Player có bản vẽ + refined material.
2. Vào Luyện Khí Thất, chọn bản vẽ.
3. Luyện chế theo framework luyện chế chung.
4. Thành công → pháp khí / trang bị vào balo.

## Player-Facing Rules

### 2 nhánh trong 1 hệ

| Nhánh | Cần bản vẽ | Output | Fail |
|---|---|---|---|
| Refine | Không | Refined material cố định per input | Mất nguyên liệu đầu vào |
| Forge | Có | Pháp khí / trang bị | Theo rule luyện chế chung |

### Refine — Rule chi tiết
- **Không cần bản vẽ** — player chọn nguyên liệu thô là thấy ngay output.
- **1 nguyên liệu thô → 1 output cố định**. Không có lựa chọn hướng refine.
- Mỗi loại nguyên liệu có **1 tỉ lệ thành công base riêng** → config trong DB.
- **Lò luyện** và **phụ liệu** chỉ có tác dụng **tăng tỉ lệ thành công**, không đổi output.
- **Fail → mất toàn bộ nguyên liệu đầu vào**.
- UI phải hiển thị rõ trước khi bắt đầu:
  - Output là gì
  - Tỉ lệ thành công (base + bonus từ lò / phụ liệu)
  - Thời gian refine
  - Sẽ mất gì nếu fail
- Refine là **một dạng luyện chế** — tuân thủ framework luyện chế chung (thời gian, cancel, v.v.).
- Cancel giữa chừng xử lý theo **rule luyện chế hiện tại**.
- Hoàn tất → output vào **thẳng balo**.
- Diễn ra tại **Luyện Khí Thất**.

### Forge — Rule chi tiết
- Cần **bản vẽ** (item tương tự đan phương).
- Dùng refined material làm nguyên liệu đầu vào trực tiếp.
- Tuân thủ framework luyện chế chung.
- Diễn ra tại **Luyện Khí Thất**.

### Vai trò trong hệ craft tổng
- Luyện Khí là **hệ craft nguồn** — tạo ra refined material dùng bởi các hệ khác.
- Các hệ downstream (Khôi Lỗi, Trận Pháp, Phù Lục...) chỉ khai báo **input trực tiếp** mà chúng cần.
- Chain tạo ra input đó là trách nhiệm của Luyện Khí, không nhúng vào doc hệ downstream.
- Không ép mọi vật liệu đều phải qua nhiều tầng — độ phức tạp theo data design.

## System States

Không có state machine riêng — Luyện Khí dùng chung state machine của framework luyện chế.

## Main Flows

### Flow 1 — Refine thành công
1. Player vào Luyện Khí Thất, chọn nguyên liệu thô.
2. UI hiển thị output, tỉ lệ, thời gian, rủi ro.
3. Player thêm lò / phụ liệu nếu muốn tăng tỉ lệ.
4. Bắt đầu refine.
5. Hoàn tất → refined material vào balo.

### Flow 2 — Refine thất bại
1. Như Flow 1.
2. Kết quả fail → nguyên liệu đầu vào mất, không có output.
3. Player thấy thông báo thất bại.

### Flow 3 — Forge
1. Player vào Luyện Khí Thất, chọn bản vẽ.
2. Chuẩn bị nguyên liệu (refined material + phụ liệu nếu có).
3. Luyện chế theo framework chung.
4. Thành công → thành phẩm vào balo.

## Edge Cases
- Player refine rồi cancel giữa chừng: xử lý theo rule luyện chế chung.
- Balo đầy khi refine xong: xử lý theo rule luyện chế chung (inbox / kho tạm nếu có).
- Phụ liệu không làm đổi output, chỉ tăng tỉ lệ: server validate để tránh exploit.

## Data / Config Needs
- Refine recipe: input item ID, output item ID, base success rate, thời gian → DB
- Lò luyện: bonus tỉ lệ thành công per loại lò → DB
- Phụ liệu refine: bonus tỉ lệ thành công per item phụ liệu → DB
- Forge recipe: bản vẽ ID, input list, output → DB (dùng chung recipe schema với hệ craft khác)
- Tier của refined material → DB item schema

## UI / UX Notes
- Refine UI: hiển thị rõ output, tỉ lệ thành công (base + tổng sau bonus), thời gian, cảnh báo mất nguyên liệu khi fail.
- Bonus tỉ lệ từ lò / phụ liệu hiển thị tách biệt để player thấy rõ đang được bao nhiêu.
- Forge UI: tương tự UI luyện chế đan dược / phù lục.

## Related Systems
- **Multi-stage Crafting**: Luyện Khí là hệ craft nguồn trong pipeline nhiều tầng — xem `features/multi-stage-crafting.md`
- **Khôi Lỗi**: dùng refined material từ Luyện Khí làm input trực tiếp — xem `features/machine-system.md`
- **Phù Lục / Trận Pháp**: có thể dùng refined material từ Luyện Khí — xem `features/crafting-talisman-formation.md`
- **Động Phủ**: Luyện Khí Thất là phòng chức năng trong động phủ — xem `features/home-cave-defense.md`

## Key Decisions
1. Luyện Khí là 1 hệ với 2 nhánh: Refine và Forge.
2. Refine không cần bản vẽ, 1 input → 1 output cố định.
3. Tỉ lệ thành công có base riêng per nguyên liệu; lò + phụ liệu chỉ tăng tỉ lệ.
4. Fail refine mất nguyên liệu đầu vào.
5. Forge cần bản vẽ.
6. Cả 2 nhánh tuân thủ framework luyện chế chung, diễn ra tại Luyện Khí Thất.
7. Luyện Khí là hệ craft nguồn — hệ downstream chỉ khai báo input trực tiếp.

## Open Questions
- [ ] Số tầng refine tối đa hợp lý — data design quyết định khi làm item/recipe data.
- [ ] Node refined material dùng chung giữa nhiều hệ — rà kỹ khi làm balance để tránh bottleneck.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [ ] Behavior is specific enough for `dev` to estimate.
- [ ] Acceptance criteria can be written without guessing.
- [ ] Major edge cases are covered.
- [ ] Config/data needs are listed.
- [ ] Out-of-scope items are explicit.
- [ ] Ready to promote to `requirements/`.
