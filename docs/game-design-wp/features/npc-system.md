---
doc_type: game_design_feature
system_id: npc-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-12
updated_at: 2026-05-12
promoted_from: notes/npc-system.md
related_docs:
  - features/quest-system.md
  - features/home-cave-defense.md
requires_code_verification: false
---

# Hệ Thống NPC — Feature Draft

## Goal

Tạo hệ thống NPC tương tác phục vụ các chức năng: hội thoại, mua bán, mở cửa map / phó bản, và các action khác theo config. NPC làm thế giới có cảm giác sống, đồng thời là cổng vào nội dung và là cơ chế kiểm soát nguồn cung tài nguyên qua shop giới hạn.

## Design Summary

NPC là interaction entity đứng yên trên map theo config. Có 2 loại theo thời gian xuất hiện: permanent (luôn có) và timed (theo giờ). Mỗi NPC có template riêng với tên, avatar, model, và nhóm action button. Player click vào NPC để xem danh sách action. NPC không tham gia combat — khi quest cần "đánh NPC" thì dùng boss entity riêng mượn model NPC đó.

## Scope

### In Scope
- 2 loại NPC: permanent và timed
- Action buttons: trò chuyện, mua vật phẩm, bán vật phẩm, vào phó bản, vào map
- Hội thoại static và Q&A config sẵn
- Shop có giới hạn tồn kho, reset bằng server data tool
- NPC template: tên, avatar, model, action group
- Countdown timer cho timed NPC
- Timed NPC ngưng tiếp khách 3 phút trước khi hết giờ

### Out Of Scope
- NPC di chuyển theo player (giải cứu NPC) — defer
- NPC tham gia combat trực tiếp
- NPC giao / nhận quest
- Hội thoại thay đổi theo tiến trình quest
- Action button ngoài danh sách trên — mở rộng theo data design sau

## Core Loop

1. Player di chuyển đến gần NPC trên map.
2. Player click vào NPC → hiện danh sách action button.
3. Player chọn action → xử lý theo loại.
4. Kết thúc tương tác → player quay lại gameplay.

## Player-Facing Rules

### Loại NPC

| Loại | Mô tả |
|---|---|
| `permanent` | Luôn xuất hiện tại vị trí config, không bao giờ ẩn |
| `timed` | Chỉ xuất hiện trong khoảng thời gian nhất định trong ngày, tồn tại một lúc rồi biến mất |

- Timed NPC hiển thị **countdown thời gian còn lại** để player biết còn bao lâu.
- **3 phút trước khi hết giờ**: timed NPC ngưng tiếp khách — chỉ còn action "Trò chuyện", tất cả action khác bị block.
- Nếu player đang mở UI của action bị block trong 3 phút cuối: UI đóng ngay, giao dịch / action hủy.
- Hết giờ: NPC biến mất khỏi map.

### Spawn và vị trí
- NPC đứng yên tại vị trí được config — **không di chuyển** trong trạng thái bình thường.
- Config trong DB: NPC template data + map spawn group.
- 1 template NPC có thể spawn ở nhiều map với cùng chức năng.

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
- Mỗi item có **giới hạn tồn kho**.
- Tồn kho về 0 → item đó không mua được cho đến khi server reset.
- Tồn kho được **làm mới bằng server data tool** (ví dụ: reset theo tuần). Mục đích: kiểm soát nguồn cung, tạo khan hiếm.
- Shop UI hiển thị **số lượng tồn kho còn lại** per item.

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
1. Player click NPC → hiện danh sách action button.
2. Player chọn action → xử lý theo loại.

### Flow 2 — Mua hàng
1. Player click NPC → chọn "Mua vật phẩm".
2. Shop UI mở, hiển thị item + tồn kho còn lại.
3. Player chọn item + số lượng → xác nhận.
4. Item vào balo, tồn kho giảm.
5. Tồn kho về 0 → item đó grey out, không mua được.

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
- Shop hết tồn kho 1 item: chỉ item đó không mua được, shop vẫn mở.
- Player bán item ngoài danh sách NPC nhận: không hiển thị trong sell UI, server từ chối nếu cố bypass.
- Timed NPC vào state `closing_soon` đúng lúc player đang dở giao dịch: UI đóng ngay, item không dịch chuyển.
- Race condition khi nhiều player mua item tồn kho cuối: server enforce, client nhận thông báo hết hàng.

## Data / Config Needs
- NPC template: ID, tên, avatar, model, action group list → DB
- NPC spawn config: template ID, map ID, vị trí, loại (permanent/timed), thời gian xuất hiện/tồn tại → DB map spawn group
- Shop data per NPC: item ID, giá, tồn kho tối đa, tồn kho hiện tại → DB
- Sell config per NPC: item ID được nhận, giá mua lại → DB
- Hội thoại: static text hoặc Q&A tree → DB
- Map / phó bản đích per action button → DB
- Điều kiện mở khóa per action button → DB
- Server data tool để reset tồn kho shop → ops tooling

## UI / UX Notes
- Click NPC: popup / panel nhỏ hiện action button rõ ràng.
- Shop: hiển thị tồn kho còn lại per item, item hết hàng grey out.
- Timed NPC: countdown timer hiển thị thời gian còn lại.
- State `closing_soon`: visual indicator rõ trên NPC hoặc UI (ví dụ: màu đỏ, text "Sắp đóng cửa").
- Sell UI: chỉ hiện item NPC nhận, không hiện toàn bộ balo.

## Related Systems
- **Quest System**: objective `talk_to_npc` trigger qua NPC interaction — xem `features/quest-system.md`
- **Phó Bản**: NPC là cổng vào phó bản — xem backlog
- **Map System**: NPC là cổng chuyển map
- **Economy**: shop giới hạn tồn kho ảnh hưởng trực tiếp supply tài nguyên

## Key Decisions
1. 2 loại NPC: permanent và timed.
2. NPC không combat — dùng boss entity riêng khi quest yêu cầu.
3. Hội thoại không thay đổi theo tiến trình, NPC không giao/nhận quest.
4. Shop có giới hạn tồn kho, reset bằng server data tool.
5. Bán đồ chỉ cho NPC có action bán và chỉ item được config.
6. Nút vào phó bản / map ẩn khi chưa đủ điều kiện.
7. Timed NPC ngưng tiếp khách 3 phút trước khi hết giờ.
8. Timed NPC hiển thị countdown.
9. 1 template dùng được ở nhiều map.

## Open Questions
- [ ] Di chuyển theo player (giải cứu NPC) — defer khi có quest loại đó.
- [ ] Danh sách action button đầy đủ — mở rộng theo data design.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [ ] Behavior is specific enough for `dev` to estimate.
- [ ] Acceptance criteria can be written without guessing.
- [ ] Major edge cases are covered.
- [ ] Config/data needs are listed.
- [ ] Out-of-scope items are explicit.
- [ ] Ready to promote to `requirements/`.
