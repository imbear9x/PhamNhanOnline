---
doc_type: game_design_feature
system_id: multi-stage-crafting
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-11
updated_at: 2026-05-12
promoted_from: notes/multi-stage-crafting-system.md
related_docs:
  - features/crafting-talisman-formation.md
  - features/machine-system.md
requires_code_verification: false
---

# Hệ Thống Luyện Chế Nhiều Tầng — Feature Draft

## Goal

Biến hệ luyện chế từ mô hình công thức phẳng thành hệ **nhiều tầng sản xuất**, nơi nguyên liệu thô cấp thấp thường không dùng trực tiếp được, phải qua một hoặc nhiều tầng xử lý trung gian. Mục tiêu là tạo progression kinh tế rõ ràng hơn, giá trị cho item trung gian, và cơ hội chuyên môn hóa sản xuất giữa người chơi.

## Design Summary

Hệ luyện chế nhiều tầng không hard-code theo loại item mà để **game data quyết định** vai trò của từng item trong pipeline sản xuất. Một item có thể là base material, component trung gian, thành phẩm — hoặc đóng nhiều vai trò cùng lúc. Tuy nhiên, **mỗi feature/system chỉ nên khai báo component đầu vào trực tiếp mà nó cần**. Còn component đó được tạo ra bằng chain nào thì được quản lý ở **hệ craft nguồn**, không nhúng ngược toàn bộ chuỗi upstream vào doc của hệ đang dùng nó.

## Scope

### In Scope
- Tầng 0–3 của chuỗi sản xuất (Base → Processed → Advanced → Final)
- Item có thể đa vai trò (dùng được + làm nguyên liệu)
- Recipe graph thay vì recipe phẳng
- Áp dụng cho các hệ: Pháp Khí, Phù Lục, Trận Pháp, Khôi Lỗi, Đan Dược

### Out Of Scope
- Tên gọi cụ thể trong game của từng tier
- Mastery / kỹ năng riêng per công đoạn
- Durability cho component
- Quality random ở mọi tầng (chỉ áp dụng khi hệ cụ thể cần)
- UI dependency chain
- Cách migrate data từ hệ recipe cũ sang mới

## Core Loop

1. Player farm / khai thác **Base Material** từ thế giới.
2. Luyện chế Base Material thành **Processed Component** (tầng 1).
3. Luyện chế tiếp nếu cần thành **Advanced Component** (tầng 2).
4. Dùng component làm nguyên liệu cho **Final Product** (tầng 3).
5. Final Product có thể dùng ngay hoặc làm nguyên liệu cho recipe cao hơn.

## Player-Facing Rules

### Nguyên tắc data-driven
- Không hard-code "raw material thì luôn không dùng được" hay "component thì luôn không dùng được".
- Game data quyết định mỗi item:
  - Có thể dùng trực tiếp không?
  - Có thể làm material cho recipe nào?
  - Thuộc tier nào?
  - Bị consume khi dùng không?

### Default design direction
- Base material cấp thấp nhất **thường không dùng trực tiếp được** — cần qua luyện chế.
- Đây là default, không phải cấm tuyệt đối. Game data vẫn có thể cho phép ngoại lệ.

### Cấu trúc 4 tầng tham chiếu

**Tầng 0 — Base Material:**
- Khai thác / loot / thu thập trực tiếp từ thế giới.
- Ví dụ: quặng thô, gỗ linh, da thú, xương yêu thú, thảo dược thô.
- Chủ yếu là input cho recipe, thường không dùng trực tiếp.
- Có thể stack lớn.

**Tầng 1 — Processed Component:**
- Đã qua một bước xử lý cơ bản.
- Ví dụ: kim phôi, linh dịch tinh luyện, da thuộc, mộc phôi.
- Có thể là nguyên liệu chính cho nhiều nhánh recipe.
- Một số item tầng này đã có thể dùng được.
- Đây là lớp quan trọng để tạo thị trường bán thành phẩm.

**Tầng 2 — Advanced Component:**
- Linh kiện / vật liệu tinh luyện cao hơn.
- Ví dụ: tinh kim, trận hạch, phù phôi cao cấp, lõi khôi lỗi, mạch dẫn linh lực.
- Thường yêu cầu recipe phức tạp hơn.
- Tạo độ giao thoa giữa các ngành craft.

**Tầng 3 — Final Product:**
- Thành phẩm player dùng trực tiếp hoặc triển khai.
- Ví dụ: đan dược, pháp khí, phù lục, trận pháp, khôi lỗi hoàn chỉnh.
- "Final ở recipe này" không có nghĩa là "không thể làm input cho recipe khác".

### Dependency trực tiếp thay vì nhúng ngược toàn bộ chain
- Ở level hệ thống tổng, crafting vẫn có thể được hiểu như một **recipe graph** liên kết nhiều ngành.
- Nhưng ở level từng feature doc, chỉ nên khai báo **input trực tiếp** của recipe đó.
- Nếu một recipe cần `Hỏa Kim Tinh Luyện`, thì doc của hệ đó chỉ cần nói nó cần `Hỏa Kim Tinh Luyện`.
- Việc `Hỏa Kim Tinh Thô -> Hỏa Kim Tinh Luyện` là trách nhiệm của hệ luyện khí / refining nguồn.

**Ví dụ:**
- Doc Khôi Lỗi chỉ cần biết nó cần `Hỏa Kim Tinh Luyện`.
- Doc Trận Pháp chỉ cần biết nó cần `Trận Hạch` hoặc `Mạch Dẫn Linh Lực`.
- Doc Phù Lục chỉ cần biết nó cần `Giấy Phù Sơ Chế` hoặc `Mực Linh`.
- Cách tạo ra các component đó được quản lý ở doc craft nguồn tương ứng.

### Quan hệ với các hệ craft hiện có

**Đan Dược:**
- Có thể bắt đầu dùng một số component trung gian thay vì chỉ raw herb.
- Nhưng doc Đan Dược chỉ cần khai báo component đầu vào trực tiếp của recipe cuối.

**Pháp Khí:**
- Là một trong các hệ craft nguồn phù hợp để xử lý refine material.
- Ví dụ: quặng thô -> kim phôi -> tinh kim là chuyện của nhánh luyện khí / refining.

**Phù Lục:**
- Có thể dùng các component như giấy phù sơ chế, mực linh, hồn ấn.
- Doc Phù Lục không cần nhúng ngược chain tạo các component đó.

**Trận Pháp:**
- Có thể dùng các component như trận cơ, trận hạch, mạch dẫn linh lực.
- Doc Trận Pháp chỉ cần nêu các input trực tiếp.

**Khôi Lỗi:**
- Gần như bắt buộc dùng component nhiều tầng vì bản chất cấu kiện.
- Nhưng doc Khôi Lỗi chỉ nên nêu các component đầu vào trực tiếp như khung, lõi, vật liệu tinh luyện, module điều khiển.

### Rule đơn giản ở phase đầu
- Chưa cần mastery riêng per công đoạn.
- Chưa cần durability cho component.
- Chưa cần quality random ở mọi tầng.
- Trước mắt chỉ cần: recipe nhiều tầng + item trung gian làm input + một số recipe cho ra item usable ngay.
- Chiều sâu đến từ **chuỗi sản xuất**, chưa dồn hết vào simulator crafting.

### Độ phức tạp theo cấp đồ
- Đồ phổ thông: ít tầng hơn.
- Đồ hiếm / mạnh / hệ đặc thù: nhiều tầng hơn.

## System States
- Không có state machine riêng — hệ này là data design của recipe và item.

## Edge Cases
- Item vừa là thành phẩm vừa là nguyên liệu: cả hai vai trò đều active cùng lúc.
- Base material có ngoại lệ dùng được trực tiếp: cho phép bằng data flag.
- Node dùng chung bởi nhiều ngành: nếu hiếm thì trở thành bottleneck — cần rà kỹ khi làm balance.

## Data / Config Needs
- Item: flag `usable_directly`, `craftable_material`, `tier`, `consumed_on_use` → DB item schema
- Recipe: input list với `success_rate_bonus`, `effect_quality_bonus`, `is_optional` → DB recipe schema
- Tier của item → DB item schema (không phải hard-code trong code)
- Danh sách recipe per ngành craft → DB

## UI / UX Notes
- Recipe viewer: hiển thị dependency chain (A cần B, B cần C...).
- Nên có cách search/filter theo tier hoặc output item.
- Inventory: cần cân nhắc tab/group theo tier hoặc loại để tránh balo rác — bàn sau.

## Related Systems
- **Phù Lục / Trận Pháp**: sẽ dần dùng component nhiều tầng — xem `features/crafting-talisman-formation.md`
- **Khôi Lỗi**: nguyên liệu sẽ có component nhiều tầng — xem `features/machine-system.md`

## Key Decisions
1. Hệ luyện chế chuyển sang mô hình nhiều tầng.
2. Base material cấp thấp nhất thường không dùng trực tiếp — default, không cấm tuyệt đối.
3. Item ở mỗi tầng có thể: dùng được, làm nguyên liệu, hoặc cả hai.
4. Tất cả data-driven, không hard-code theo loại item.
5. Crafting là recipe graph, không phải recipe phẳng rời rạc.
6. Một node trong chuỗi có thể phục vụ nhiều ngành.
7. Phase đầu: ưu tiên pipeline sản xuất gọn, chưa cần mastery/durability/quality random per tầng.
8. Mỗi feature doc chỉ khai báo **input trực tiếp**; chain upstream thuộc về hệ craft nguồn.

## Open Questions
- [ ] Tên gọi cụ thể trong game của từng tier.
- [ ] Mức độ nhiều tầng tối đa cho từng ngành craft.
- [ ] Có cần kho nguyên liệu riêng / UI riêng không.
- [ ] Cách migrate từ hệ recipe hiện tại sang hệ mới trong data.
- [ ] Cần xác định hệ craft nguồn nào chịu trách nhiệm refine các material trung gian chính.
- [ ] UI recipe / hiển thị dependency chain cụ thể như thế nào.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [ ] Behavior is specific enough for `dev` to estimate.
- [ ] Acceptance criteria can be written without guessing.
- [ ] Major edge cases are covered.
- [ ] Config/data needs are listed.
- [ ] Out-of-scope items are explicit.
- [ ] Ready to promote to `requirements/`.
