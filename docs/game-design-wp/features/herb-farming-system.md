---
doc_type: game_design_feature
system_id: herb-farming-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/home-cave-defense.md
  - features/multi-stage-crafting.md
  - features/mineral-vein-system.md
  - shared-rules.md
requires_code_verification: true
---

# Hệ Thống Linh Thảo & Linh Dược — Feature Draft

## Goal

Tạo vòng lặp trồng trọt — thu hoạch — chế tạo linh dược khép kín trong động phủ. Linh thảo là sinh vật sống với 4 giai đoạn phát triển; linh dược là nguyên liệu chế tạo đan dược và các recipe khác. Tốc độ phát triển phụ thuộc mật độ linh khí của vị trí đặt động phủ — tạo giá trị kinh tế khác nhau giữa các vùng map.

## Design Summary

Linh thảo không mọc ngoài tự nhiên — chỉ thu được qua farm quái (tỉ lệ drop per quái per map) hoặc trồng trong vườn động phủ. Vườn có số ô trồng phụ thuộc phẩm cấp bản vẽ động phủ. Mỗi ô cần linh thổ — đất đặc biệt mua từ NPC, có phẩm cấp, có thời hạn sử dụng theo thực tế. Linh thảo phát triển qua 4 trạng thái; tốc độ phụ thuộc mật độ linh khí zone. Chỉ thu hoạch được ở trạng thái trưởng thành và ngàn năm. Sau khi thu hoạch, linh thảo là item sống trong túi với thời hạn — player phải extract thành linh dược trước khi hỏng. Linh dược là nguyên liệu crafting, phẩm cấp phụ thuộc trạng thái cây khi extract.

## Scope

### In Scope
- Linh thảo: 4 trạng thái, tốc độ lớn theo linh khí, drop từ quái
- Linh thổ: đất trồng, có phẩm cấp, có thời hạn, mua NPC
- Vườn động phủ: số ô theo phẩm cấp bản vẽ
- Thu hoạch linh thảo vào túi — item sống có thời hạn
- Extract linh thảo → linh dược (có thể ra 2 loại: ví dụ hoa + lá)
- Linh dược là nguyên liệu crafting, phẩm cấp ảnh hưởng output đan dược
- Tái trồng từ mầm non nhận lại khi extract

### Out Of Scope
- Linh thảo mọc ngoài tự nhiên — không có
- `required_herb_maturity` trong recipe (cơ chế cũ, đã deprecated)
- Balance tỉ lệ drop, thời gian phát triển, tỉ lệ mầm tái trồng — data design
- Model data linh dược theo phẩm cấp — đang thảo luận với TechDesign

## Linh Thảo — Vòng Đời

### 4 trạng thái
1. **Mầm non** — vừa trồng, chưa dùng được
2. **Cây non** — đang phát triển, chưa dùng được
3. **Cây trưởng thành** — có thể thu hoạch
4. **Cây ngàn năm** — có thể thu hoạch, cho linh dược phẩm cấp cao hơn

- Thời gian chuyển trạng thái: config per loại linh thảo.
- Cây ngàn năm **không chuyển tiếp** — giữ nguyên trạng thái mãi cho đến khi bị thu hoạch.

### Nguồn linh thảo
- **Drop từ quái**: tỉ lệ drop config per quái per map — nhận trực tiếp vào túi ở trạng thái mầm non hoặc cao hơn tùy config.
- **Trồng trong vườn**: cách chính để có linh thảo số lượng lớn và kiểm soát phẩm cấp.

### Tốc độ phát triển
- Phụ thuộc **mật độ linh khí** của zone map nơi động phủ đặt.
- Mật độ linh khí: server random per zone per map — đã implement trong backend.
- Động phủ trong tông môn: linh khí theo vị trí tông môn trong zone map.
- Linh thổ phẩm cấp cao hơn → tốc độ lớn nhanh hơn.

## Linh Thổ

- Đất đặc biệt dùng để trồng linh thảo trong ô vườn.
- **Mua từ NPC** — không craft, không loot.
- Có **phẩm cấp**: ảnh hưởng tốc độ lớn của linh thảo bên trong.
- Có **thời hạn sử dụng** tính theo thời gian thực tế mà linh thảo tồn tại bên trên (không tính số lần trồng).
- Khi linh thổ **hết hạn**:
  - Cây đang trồng **dừng phát triển**.
  - Cây bắt đầu đếm ngược thời gian tồn tại — như khi để trong túi.
  - Player cần **bổ sung linh thổ mới** để dừng đếm ngược và tiếp tục phát triển.
- Linh thổ hết không làm cây chết ngay — chỉ dừng và đếm ngược.

## Vườn Động Phủ

- Mỗi động phủ có số **ô trồng** phụ thuộc **phẩm cấp bản vẽ động phủ** — cấp càng cao càng nhiều ô.
- Mỗi ô cần linh thổ trước khi có thể trồng linh thảo.
- Backend đã có: plot → soil (linh thổ) → herb lifecycle (`HerbService.cs`).
- **Requires code verification**: network handlers/client UI chưa được xác nhận wired trong build hiện tại.

### Lifecycle một ô trồng
1. Ô trống → lắp linh thổ → trồng hạt giống (hoặc mầm non từ tái trồng).
2. Cây phát triển theo thời gian thực + mật độ linh khí.
3. Player thu hoạch khi cây đạt trưởng thành hoặc ngàn năm → cây vào túi dưới dạng item.
4. Ô trở về trạng thái có linh thổ — có thể trồng tiếp (nếu linh thổ còn hạn).

## Thu Hoạch và Extract

### Thu hoạch (harvest)
- Player ấn thu hoạch → cây **rời ô trồng ngay lập tức**, vào túi dưới dạng **item linh thảo sống**.
- Item linh thảo trong túi có **thời hạn** — thường rất lâu nhưng có giới hạn. Hết hạn → hỏng, mất.
- Chỉ thu hoạch được ở trạng thái **trưởng thành** hoặc **ngàn năm**.
- Cây trong túi giữ nguyên trạng thái (trưởng thành / ngàn năm).

### Extract — thu thập nguyên liệu
- Player ấn "thu thập nguyên liệu" trên item linh thảo trong túi → linh thảo **biến thành linh dược**, không thể trồng lại.
- Extract cho ra **1 hoặc 2 loại linh dược** per cây (ví dụ: hoa + lá) — config per loại linh thảo.
- Số lượng linh dược nhận được: config per loại linh thảo.
- **Phẩm cấp linh dược** phụ thuộc trạng thái cây khi extract:
  - Trưởng thành → linh dược phẩm cấp thường
  - Ngàn năm → linh dược phẩm cấp cao hơn
- Khi extract: có tỉ lệ nhận lại **mầm non** để tái trồng — tỉ lệ cố định, không phụ thuộc trạng thái cây.

### Tái trồng
- Mầm non nhận lại từ extract có thể trồng thẳng vào ô có linh thổ — không cần tiêu thụ hạt giống mới.
- Backend đã hỗ trợ: `PlantExistingHerbAsync`.

## Linh Dược

- Là **item nguyên liệu** — đầu vào cho alchemy (đan dược) và các recipe crafting khác.
- Phẩm cấp linh dược quyết định phẩm cấp đan dược đầu ra và các recipe có thể dùng.
- Recipe nhận **linh dược theo phẩm cấp** — không nhận thẳng linh thảo (cơ chế `required_herb_maturity` cũ đã deprecated).
- Model data linh dược theo phẩm cấp: đang thảo luận với TechDesign — xem Open Questions.

## Mật Độ Linh Khí

- Mật độ linh khí ảnh hưởng:
  - **Tốc độ phát triển linh thảo** trong vườn động phủ.
  - **Tốc độ tu luyện** của player (đã implement trong cultivation formula).
- Server random mật độ per zone per map — đã implement trong backend.
- Động phủ nhận mật độ linh khí của zone nơi nó đặt.

## Edge Cases
- Linh thổ hết trong lúc player offline: cây dừng phát triển, bắt đầu đếm ngược. Player login lại thấy trạng thái hiện tại.
- Cây trong túi hết hạn khi offline: hỏng khi server settle — player mất item.
- Ô trồng thiếu linh thổ: không thể trồng, hiển thị "cần linh thổ".
- Extract cây trưởng thành trong túi đã gần hết hạn: vẫn extract được — linh dược không có thời hạn.
- Drop linh thảo từ quái khi túi đầy: vào inbox theo shared overflow rule.

## Data / Config Needs
- Linh thảo template: ID, tên, thời gian per giai đoạn, linh dược output (loại + số lượng per trạng thái), tỉ lệ mầm tái trồng → DB
- Linh thổ template: ID, tên, phẩm cấp, tốc độ modifier, thời hạn sử dụng → DB
- Drop rate linh thảo per quái per map → DB
- Số ô trồng per phẩm cấp bản vẽ động phủ → DB
- Thời hạn item linh thảo trong túi → DB per loại
- Mật độ linh khí per zone: server-managed, đã implement

## UI / UX Notes
- Vườn động phủ: hiển thị từng ô với trạng thái cây, thời gian còn lại đến giai đoạn tiếp theo, trạng thái linh thổ (còn hạn / sắp hết / đã hết).
- Item linh thảo trong túi: hiển thị trạng thái cây, thời hạn còn lại.
- Cảnh báo khi linh thổ sắp hết hoặc cây sắp hỏng trong túi.
- Extract: confirm dialog nếu cây còn lâu hỏng (không cần vội).

## Related Systems
- **Home Cave Defense** (`features/home-cave-defense.md`): vườn nằm trong động phủ, phẩm cấp bản vẽ quyết định số ô.
- **Multi-Stage Crafting / Alchemy** (`features/multi-stage-crafting.md`): linh dược là nguyên liệu đầu vào.
- **Cultivation & Breakthrough** (`features/cultivation-and-breakthrough.md`): mật độ linh khí ảnh hưởng tu luyện.
- **Offline Time-Based Activities** (`shared-rules.md`): cây phát triển offline.
- **Inbox** (`features/inbox-mail-system.md`): drop linh thảo overflow khi túi đầy.

## Key Decisions
1. Linh thảo không mọc ngoài tự nhiên — chỉ drop từ quái hoặc trồng trong vườn.
2. 4 trạng thái: mầm non → cây non → trưởng thành → ngàn năm (ngàn năm không chuyển tiếp).
3. Chỉ thu hoạch được ở trưởng thành và ngàn năm.
4. Thu hoạch → item linh thảo sống trong túi có thời hạn.
5. Extract → linh dược (không thể trồng lại); phẩm cấp linh dược theo trạng thái cây.
6. Linh thổ có phẩm cấp và thời hạn thực tế; mua từ NPC; hết → cây dừng + đếm ngược.
7. Số ô trồng per phẩm cấp bản vẽ động phủ.
8. Tốc độ lớn phụ thuộc linh khí zone + phẩm cấp linh thổ.
9. Recipe nhận linh dược theo phẩm cấp — không nhận thẳng linh thảo (`required_herb_maturity` deprecated).
10. Tỉ lệ mầm tái trồng cố định khi extract — không phụ thuộc trạng thái cây.

## Open Questions
- [x] Mỗi phẩm cấp linh dược là 1 item riêng (item template riêng per phẩm cấp). Lý do: inventory/alchemy hiện tại chạy theo item_template_id; hướng này sạch hơn và ít bug hơn so với quality runtime.
- [x] TechDesign confirmed migration caveat: phải migrate/remove `required_herb_maturity` checks khỏi recipe/validation path; quality gating chuyển sang dựa trên item template identity.
- [ ] Client UI/handler cho vườn động phủ chưa được xác nhận wired — currently accepted as non-blocking for requirement promotion by user.

## Known Conflicts / Drift
- `required_herb_maturity` trong `AlchemyService.cs` là cơ chế cũ, hiện bị block có chủ ý. Quyết định canonical mới: **mỗi phẩm cấp linh dược là một item template riêng**. TechDesign cần refactor để recipe nhận linh dược theo item template/phẩm cấp thay vì maturity field.
- Clarification notes `alchemy-required-herb-maturity-clarification.md` và `home-cave-garden-herb-design-clarification.md` ghi nhận hệ này chưa wired client — cần verify trước khi promote requirements.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
