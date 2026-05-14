---
doc_type: game_design_feature
system_id: npc-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-12
updated_at: 2026-05-14
promoted_from: notes/npc-system.md
related_docs:
  - features/main-progression-quest-chain.md
  - features/home-cave-defense.md
requires_code_verification: false
---

# Hệ Thống NPC — Feature Draft

## Goal

Tạo hệ thống NPC tương tác phục vụ các chức năng: hội thoại, mua bán, mở cửa map / phó bản, và các action khác theo config. NPC làm thế giới có cảm giác sống và là cổng vào nội dung. Shop NPC bán vô hạn bằng linh thạch — không dùng cơ chế tồn kho giới hạn.

## Design Summary

NPC là interaction entity đứng yên trên map theo config. Có 2 loại theo thời gian xuất hiện: permanent (luôn có) và timed (theo giờ hoặc theo event). Mỗi NPC có template riêng với tên, avatar, model, và nhóm action button. Player click vào NPC để xem danh sách action. NPC không tham gia combat — khi quest cần "đánh NPC" thì dùng boss entity riêng mượn model NPC đó.

## Scope

### In Scope
- 2 loại NPC: permanent và timed
- Action buttons: trò chuyện, mua vật phẩm, bán vật phẩm, vào phó bản, vào map
- Hội thoại static và Q&A config sẵn
- Shop bán vô hạn, tiền tệ linh thạch
- NPC template: tên, avatar, model, action group
- Countdown timer cho timed NPC
- Timed NPC ngưng tiếp khách 3 phút trước khi hết giờ
- Timed NPC trigger theo giờ cố định hoặc event/trigger

### Out Of Scope
- NPC di chuyển theo player (giải cứu NPC) — defer
- NPC tham gia combat trực tiếp
- NPC giao / nhận quest
- Hội thoại thay đổi theo tiến trình quest
- Action button ngoài danh sách trên — mở rộng theo data design sau

## Core Loop

1. Player di chuyển đến gần NPC trên map (trong interaction range).
2. Player click vào NPC → hiện danh sách action button.
3. Player chọn action → xử lý theo loại.
4. Kết thúc tương tác → player quay lại gameplay.

## Player-Facing Rules

### Loại NPC

| Loại | Mô tả |
|---|---|
| `permanent` | Luôn xuất hiện tại vị trí config, không bao giờ ẩn |
| `timed` | Chỉ xuất hiện trong khoảng thời gian nhất định, tồn tại một lúc rồi biến mất |

- Timed NPC có thể xuất hiện theo **giờ cố định trong ngày** hoặc theo **event/trigger** — xác định per NPC trong config.
- Timed NPC hiển thị **countdown thời gian còn lại** để player biết còn bao lâu.
- Timed NPC có thể có **thông báo trước khi xuất hiện** (broadcast toàn server hoặc local notification) — config per NPC.
- **3 phút trước khi hết giờ**: timed NPC vào state `closing_soon` — chỉ còn action "Trò chuyện", tất cả action khác bị block.
- Nếu player đang mở UI của action bị block trong 3 phút cuối: UI đóng ngay, giao dịch / action hủy.
- Hết giờ: NPC biến mất khỏi map.

### Spawn và vị trí
- NPC đứng yên tại vị trí được config — **không di chuyển** trong trạng thái bình thường.
- Config trong DB: NPC template data + map spawn group.
- 1 template NPC có thể spawn ở nhiều map với cùng chức năng.

### Interaction range
- Dùng **shared rule interaction range** (đã implement trong repo) — cùng rule với mọi đối tượng tương tác khác.
- Player phải trong range mới click được NPC.

### Combat
- NPC **không thể bị tấn công** ở trạng thái bình thường.
- Khi quest yêu cầu "đánh thắng NPC": dùng **boss entity riêng** mang model/skin của NPC đó. Boss entity có đầy đủ combat rule của boss thông thường.

### Action Buttons
- Player click NPC → hiện danh sách action button theo chức năng của NPC đó.
- 1 NPC có thể có nhiều action button cùng lúc theo data config.
- Nút "Vào phó bản" / "Vào map": chỉ hiển thị khi player **đã đủ điều kiện** mở khóa — ẩn luôn nếu chưa đủ.

### Hội thoại
- **2 dạng:**
  - `static`: NPC hiện 1 đoạn text cố định.
  - `branched`: danh sách câu hỏi config sẵn, player bấm câu hỏi → NPC hiện câu trả lời config sẵn.
- Hội thoại **không thay đổi** theo tiến trình quest hay điều kiện.
- NPC không giao / nhận quest — quest do hệ thống quest tự quản.

### Shop — Mua vật phẩm
- Danh sách hàng **cố định** theo config, không thay đổi theo thời gian.
- **Không giới hạn tồn kho** — tất cả item bán vô hạn, không có khái niệm hết hàng.
- Tiền tệ duy nhất: **linh thạch**.
- Mua hàng mà balo đầy: **từ chối giao dịch**, hiện thông báo — không gửi inbox.
- Nhiều player mở shop cùng NPC: **đồng thời, không giới hạn**.

### Shop — Bán vật phẩm
- Chỉ bán được cho NPC **có action "Bán vật phẩm"**.
- NPC chỉ nhận **item được config sẵn** — item ngoài danh sách không hiển thị trong sell UI.
- Giá mua lại và danh sách item nhận → config trong DB per NPC.

### Chuyển map / phó bản
- Player bấm "Vào phó bản" / "Vào map" → **chuyển map ngay lập tức**.
- NPC là **cổng vào trực tiếp**, không chỉ mở khóa quyền truy cập.
- Điều kiện chưa đủ → nút ẩn, không hiển thị.

## System States

| State | Mô tả |
|---|---|
| `visible` | NPC hiển thị trên map, có thể tương tác đầy đủ |
| `closing_soon` | Timed NPC trong 3 phút cuối — chỉ còn "Trò chuyện" |
| `hidden` | Timed NPC ngoài giờ — không hiển thị, không tương tác |

## Main Flows

### Flow 1 — Tương tác thông thường
1. Player vào interaction range, click NPC → hiện danh sách action button.
2. Player chọn action → xử lý theo loại.

### Flow 2 — Mua hàng
1. Player click NPC → chọn "Mua vật phẩm".
2. Shop UI mở, hiển thị danh sách item + giá linh thạch.
3. Player chọn item + số lượng → xác nhận.
4. Server check balo: đủ chỗ → item vào balo, trừ linh thạch.
5. Balo đầy → từ chối, hiện thông báo.

### Flow 3 — Bán hàng
1. Player click NPC → chọn "Bán vật phẩm".
2. UI hiện danh sách item NPC nhận + giá.
3. Player chọn item → xác nhận.
4. Item xóa khỏi balo, player nhận linh thạch.

### Flow 4 — Vào phó bản / map
1. Player click NPC → bấm "Vào phó bản" / "Vào map" (chỉ hiện nếu đủ điều kiện).
2. Xác nhận nếu cần.
3. Player chuyển map ngay.

### Flow 5 — Hội thoại nhánh
1. Player click NPC → chọn "Trò chuyện".
2. NPC hiện text mở đầu (nếu có).
3. Danh sách câu hỏi hiện ra.
4. Player chọn câu hỏi → NPC hiện câu trả lời.
5. Player tiếp tục hoặc đóng.

### Flow 6 — Timed NPC sắp hết giờ
1. Còn 3 phút → NPC vào state `closing_soon`.
2. Action button ngoài "Trò chuyện" bị block.
3. Player đang mở UI mua bán / action khác → UI đóng, giao dịch hủy.
4. Hết giờ → NPC biến mất.

## Edge Cases
- Balo đầy khi mua: từ chối toàn bộ giao dịch đó, không mua một phần.
- Player bán item ngoài danh sách NPC nhận: không hiển thị trong sell UI, server từ chối nếu cố bypass.
- Timed NPC vào state `closing_soon` đúng lúc player đang dở giao dịch: UI đóng ngay, item không dịch chuyển.
- Player ra ngoài interaction range khi đang mở UI shop: xử lý theo shared rule interaction range (đã implement).

## Data / Config Needs
- NPC template: ID, tên, avatar, model, action group list → DB
- NPC spawn config: template ID, map ID, vị trí, loại (permanent / timed), thời gian xuất hiện / tồn tại, trigger type (`scheduled` / `event_trigger`), notification config → DB
- Shop data per NPC: item ID, giá linh thạch (không giới hạn tồn kho) → DB
- Sell config per NPC: item ID được nhận, giá mua lại → DB
- Hội thoại: static text hoặc Q&A tree → DB
- Map / phó bản đích per action button → DB
- Điều kiện mở khóa per action button → DB

## UI / UX Notes
- Click NPC: popup / panel nhỏ hiện action button rõ ràng.
- Shop: hiển thị danh sách item + giá linh thạch; không hiển thị tồn kho (vô hạn).
- Timed NPC: countdown timer hiển thị thời gian còn lại.
- State `closing_soon`: visual indicator rõ trên NPC hoặc UI (ví dụ: màu đỏ, text "Sắp đóng cửa").
- Sell UI: chỉ hiện item NPC nhận, không hiện toàn bộ balo.

## Related Systems
- **Main Progression Quest Chain**: objective `talk_to_npc` trigger qua NPC interaction — xem `features/main-progression-quest-chain.md`
- **Shared rule interaction range**: range tương tác dùng chung, đã implement trong repo
- **Phó Bản**: NPC là cổng vào phó bản — xem backlog
- **Map System**: NPC là cổng chuyển map

## Key Decisions
1. 2 loại NPC: permanent và timed.
2. NPC không combat — dùng boss entity riêng khi quest yêu cầu.
3. Hội thoại không thay đổi theo tiến trình, NPC không giao/nhận quest.
4. Shop **không giới hạn tồn kho** — bán vô hạn; tiền tệ duy nhất là linh thạch.
5. Bán đồ chỉ cho NPC có action bán và chỉ item được config.
6. Nút vào phó bản / map ẩn khi chưa đủ điều kiện.
7. Timed NPC ngưng tiếp khách 3 phút trước khi hết giờ.
8. Timed NPC hiển thị countdown; có thể xuất hiện theo giờ cố định hoặc event/trigger.
9. 1 template dùng được ở nhiều map.
10. Mua hàng balo đầy: từ chối giao dịch — không gửi inbox.
11. Range tương tác dùng shared rule interaction range.
12. Nhiều player mở shop cùng NPC: đồng thời, không giới hạn.

## Open Questions
- [ ] Di chuyển theo player (giải cứu NPC) — defer khi có quest loại đó.
- [ ] Danh sách action button đầy đủ — mở rộng theo data design.
- [x] Notification timed NPC: local map — config per NPC khi data design.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
