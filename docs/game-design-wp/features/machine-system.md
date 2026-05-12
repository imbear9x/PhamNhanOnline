---
doc_type: game_design_feature
system_id: machine-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-11
updated_at: 2026-05-12
promoted_from: notes/machine-system.md
related_docs:
  - features/spirit-beast.md
  - features/home-cave-defense.md
  - features/multi-stage-crafting.md
requires_code_verification: false
---

# Hệ Thống Khôi Lỗi (Machine System) — Feature Draft

## Goal

Tạo hệ **Khôi Lỗi** là companion máy móc do người chơi **chủ động điều khiển**, luyện chế từ bản vẽ và nguyên liệu. Khác biệt rõ với Linh Thú ở bản chất (không phải sinh vật), cách điều khiển (player ra lệnh chủ động thay vì AI tự hành), và mô hình hao mòn (độ bền giảm dần, về 0 thì mất hẳn).

## Design Summary

Khôi Lỗi là thực thể đồng minh chiếm **slot thần thức** của player khi triệu hồi, và tiêu hao **linh thạch** liên tục khi đang hoạt động. Người chơi điều khiển qua 2 nút: **Tấn công** (dùng skill nhắm vào target) và **Phòng thủ** (đứng trước mặt player gánh đòn). Mỗi lần ra lệnh tiêu hao **năng lượng** và có cooldown. Chi phí linh thạch (quy đổi thành năng lượng) là cơ chế kiềm chế tự nhiên — người chơi tự cân nhắc khi nào nên triệu hồi. Sức mạnh cố định từ lúc craft — không tiến hóa. Độ bền giảm mỗi lần bị hạ trong combat; về 0 thì mất hẳn; hồi độ bền bằng cách thay thế nguyên liệu gốc.

## Scope

### In Scope
- Luyện chế từ bản vẽ + nguyên liệu
- Triệu hồi và duy trì bằng thần thức
- 2 mode điều khiển: Tấn công / Phòng thủ
- Cơ chế cooldown và tài nguyên per lệnh
- Độ bền và cách hồi phục
- Nguồn bản vẽ

### Out Of Scope
- Balance cụ thể (slot thần thức, năng lượng per phút / per lệnh, cooldown, độ bền)
- Tỉ lệ quy đổi linh thạch → năng lượng — phase data design
- Taxonomy bản vẽ chi tiết và danh sách skill mẫu — phase data design
- UI/UX điều khiển chi tiết
- Rule nguyên liệu hồi độ bền cụ thể — phase data design
- Khôi Lỗi thủ nhà — không hỗ trợ

## Core Loop

1. Player có bản vẽ + nguyên liệu → luyện chế Khôi Lỗi.
2. Nạp linh thạch vào Khôi Lỗi → quy đổi thành **năng lượng** của nó.
3. Triệu hồi → chiếm slot thần thức, tiêu hao năng lượng liên tục theo phút.
4. Bấm Tấn công / Phòng thủ → tiêu hao thêm năng lượng, có cooldown.
5. Hết năng lượng → Khôi Lỗi ngừng hoạt động, phải nạp linh thạch tiếp.
6. Khôi Lỗi bị hạ → về túi, hồi theo thời gian, giảm độ bền.
7. Độ bền về 0 → mất hẳn, phải craft lại.

## Player-Facing Rules

### Bản chất
- Là **đồ máy luyện chế ra**, không phải sinh vật.
- **Không thông minh** — không tự hành theo AI.
- Hoàn toàn do **player điều khiển qua giao diện**.

### Chỉ số
- **HP**: máu chiến đấu.
- **ATK**: sát thương khi tấn công.
- **Speed**: tốc độ di chuyển / phản ứng.
- **Độ bền**: chỉ số dài hạn, giảm dần theo thời gian sử dụng — tách biệt với HP.
- Không có Thần Thức, Cơ Duyên, Defense riêng.

### Thần thức và năng lượng

**Triệu hồi:**
- Chiếm một lượng **slot thần thức cố định** của player — không hao theo thời gian.
- Không đủ slot → không cho triệu hồi thêm, phải thu hồi trước.
- Có thể triệu hồi đồng thời cả Linh Thú lẫn Khôi Lỗi miễn đủ slot.

**Năng lượng (nhiên liệu của Khôi Lỗi):**
- Khôi Lỗi không dùng linh thạch trực tiếp.
- Player **nạp linh thạch** vào Khôi Lỗi — linh thạch quy đổi thành **năng lượng** nội tại.
- Ví dụ: 1 linh thạch = 10 năng lượng (tỉ lệ cụ thể → phase data design).
- Khôi Lỗi chỉ hoạt động bằng năng lượng, không kết nối trực tiếp vào kho linh thạch của player.
- **Tiêu hao theo phút** khi đang triệu hồi (duy trì hoạt động).
- Tiêu hao thêm khi ra lệnh Tấn công / Phòng thủ.
- Hết năng lượng → Khôi Lỗi **ngừng hoạt động tại chỗ** — phải nạp linh thạch tiếp hoặc thu hồi.
- Cơ chế kiềm chế tự nhiên: người chơi tự cân nhắc khi nào nên triệu hồi vì tốn linh thạch.

**Ra lệnh:**
- Mỗi lần bấm Tấn công / Phòng thủ tiêu hao thêm **năng lượng** cố định.
- Có **cooldown** — hồi đủ cooldown + còn năng lượng mới bấm được.

**Giới hạn số lượng:**
- Không giới hạn cứng.
- Giới hạn thực tế là **slot thần thức còn dư** và **linh thạch để nạp**.

### Mode Tấn công
- Bấm → Khôi Lỗi dùng **skill duy nhất** nhắm vào target player đang ngắm.
- Mỗi Khôi Lỗi chỉ có **1 skill tấn công**, do bản vẽ quyết định.
- Cooldown + tiêu hao **năng lượng** mỗi lần bấm.
- Bản vẽ thường: không có skill riêng → **đánh thường**.
- Bản vẽ cao cấp: có skill tấn công được gắn vào lệnh này.

### Mode Phòng thủ
- Bấm → Khôi Lỗi **xuất hiện trước mặt player**, gánh toàn bộ sát thương nhắm vào player.
- Không giới hạn thời gian — giữ cho đến khi player tắt hoặc Khôi Lỗi chết.
- Gánh **toàn bộ** sát thương.
- Nếu HP Khôi Lỗi không đủ → **phần sát thương thừa vẫn tác động lên player**.
- Bản vẽ thường: không có skill → chỉ làm **lá chắn máu**.
- Bản vẽ cao cấp: có skill phòng thủ → kích hoạt skill đó khi bấm.
- Cooldown + tiêu hao **năng lượng** mỗi lần bấm.

### Skill theo bản vẽ
- Mỗi Khôi Lỗi tối đa **1 skill** (tấn công hoặc phòng thủ), do bản vẽ gắn vào.
- Bản vẽ thường: không có skill.
- Bản vẽ cao cấp: có 1 skill tương ứng với mode (tấn công hoặc phòng thủ).

### Độ bền

**HP vs Độ bền:**
- HP = máu chiến đấu, hồi lại sau khi về túi.
- Độ bền = chỉ số dài hạn, **giảm mỗi lần Khôi Lỗi chết trong combat**.

**Khi HP về 0 (chết trong combat):**
- Khôi Lỗi **biến mất tạm thời** vào túi trữ vật.
- Cần **thời gian hồi phục** trước khi triệu hồi lại.
- Sau hồi phục: HP đầy nhưng độ bền vẫn là con số đã giảm.
- Lượng độ bền giảm per lần chết → phase data design.

**Khi độ bền về 0%:**
- Khôi Lỗi **mất hẳn**, không thể dùng tiếp.
- Phải craft lại từ đầu.

**Hồi phục độ bền:**
- Dùng lại **nguyên liệu đầu vào trực tiếp** của công thức Khôi Lỗi đó để sửa chữa.
- Khôi Lỗi chỉ quan tâm tới component đầu vào trực tiếp trong recipe của nó; chuỗi tạo ra component đó thuộc hệ craft nguồn.
- Lượng hồi theo từng loại nguyên liệu → phase data design.

### Luyện chế và bản vẽ

**Luyện chế:**
- Cần **bản vẽ** (item giống đan phương) + **nguyên liệu**.
- Ai cũng có thể craft nếu có đủ — không cần nhánh nghề đặc biệt.
- Sức mạnh quyết định bởi: **cấp độ bản vẽ + chất lượng nguyên liệu**.
- **Không tiến hóa** sau khi craft xong.

**Nguồn bản vẽ:**
- Mua từ **NPC**.
- **Phần thưởng nhiệm vụ**.
- **Drop trong dungeon** / địa điểm đặc biệt.
- **Phần thưởng boss**.

**Nguyên liệu:**
- Dùng chung pool nguyên liệu với các hệ craft khác (Pháp Khí, Trận Pháp...).
- Recipe Khôi Lỗi chỉ nên khai báo **component đầu vào trực tiếp** mà nó cần.
- Component đó được tạo ra thế nào là trách nhiệm của hệ craft nguồn, không cần nhúng ngược toàn bộ chain vào doc Khôi Lỗi.
- Không ép cứng loại nào — **game data design** quyết định theo từng bản vẽ.

## System States

| State | Mô tả |
|---|---|
| Trong túi (bình thường) | Chưa triệu hồi, sẵn sàng |
| Đang triệu hồi | Chiếm slot thần thức, tiêu hao năng lượng theo phút |
| Đang phòng thủ | Đứng trước mặt player, gánh đòn |
| Trong túi (hồi phục) | Vừa bị hạ, đang cooldown |
| Đã mất | Độ bền về 0, không dùng được nữa |

## Main Flows

### Flow 1 — Tấn công
1. Player ngắm mục tiêu.
2. Bấm nút Tấn công (đủ cooldown + còn năng lượng).
3. Khôi Lỗi dùng skill nhắm vào target, tiêu hao năng lượng.
4. Cooldown bắt đầu đếm.

### Flow 2 — Phòng thủ
1. Player bấm nút Phòng thủ (đủ cooldown + còn năng lượng).
2. Khôi Lỗi xuất hiện trước mặt player, tiêu hao năng lượng.
3. Toàn bộ sát thương nhắm vào player được chuyển sang Khôi Lỗi.
4. Nếu HP Khôi Lỗi không đủ → phần thừa tác động lên player.
5. Khôi Lỗi ở mode phòng thủ cho đến khi bị tắt hoặc bị hạ.

### Flow 3 — Khôi Lỗi bị hạ
1. HP về 0.
2. Khôi Lỗi về túi trữ vật.
3. Độ bền giảm X.
4. Cooldown hồi phục bắt đầu đếm.
5. Sau khi hồi xong: HP đầy, có thể triệu hồi lại.

## Edge Cases
- Khôi Lỗi đang phòng thủ, HP không đủ gánh 1 đòn: phần thừa hit player, Khôi Lỗi về túi ngay.
- Nhiều Khôi Lỗi cùng ở mode phòng thủ: cần xác định rule gánh đòn khi nhiều shield cùng lúc (open question).
- Khôi Lỗi hết năng lượng giữa trận: ngừng hoạt động tại chỗ, không về túi — player phải nạp linh thạch tiếp hoặc thu hồi.
- Bản vẽ cao cấp, skill phòng thủ có cooldown dài: player bấm phòng thủ khi skill chưa hồi → chỉ làm lá chắn máu, không dùng skill.

## Data / Config Needs
- Slot thần thức chiếm per Khôi Lỗi → phase balance
- Tỉ lệ quy đổi linh thạch → năng lượng → phase data design
- Năng lượng tiêu hao per phút + per lệnh → phase balance
- Cooldown Tấn công / Phòng thủ → phase balance
- Lượng độ bền giảm per lần chết → phase data design
- Lượng độ bền hồi theo từng loại nguyên liệu → phase data design
- Thời gian hồi phục sau khi Khôi Lỗi bị hạ → phase balance

## UI / UX Notes
- 2 nút điều khiển rõ ràng: Tấn công / Phòng thủ, kèm cooldown indicator.
- Hiển thị HP và Độ bền tách biệt trong UI companion.
- Khi Khôi Lỗi đang hồi phục: hiển thị timer countdown.
- Bản vẽ trong craft UI: hiển thị rõ skill gắn vào và chỉ số output theo nguyên liệu.

## Related Systems
- **Linh Thú**: companion cùng dùng chung thần thức của player — xem `features/spirit-beast.md`
- **Động Phủ**: Khôi Lỗi **không thủ nhà** — chỉ triệu hồi khi player online và chủ động trong chiến đấu
- **Crafting nhiều tầng**: nguyên liệu Khôi Lỗi sẽ dùng component nhiều tầng — xem `features/multi-stage-crafting.md`

## Key Decisions
1. Khôi Lỗi do player chủ động điều khiển, không tự hành.
2. 2 mode: Tấn công (skill + cooldown) / Phòng thủ (lá chắn, có thể có skill).
3. Triệu hồi chiếm slot thần thức. Hoạt động tiêu hao **năng lượng** (quy đổi từ linh thạch nạp vào).
4. Không giới hạn cứng số lượng — giới hạn thực tế là slot thần thức và linh thạch để nạp.
5. Độ bền tách biệt với HP — giảm per lần chết, về 0 mất hẳn.
6. Hồi độ bền bằng nguyên liệu gốc.
7. Sức mạnh cố định từ lúc craft, không tiến hóa.
8. Bản vẽ tối đa gắn 1 skill.
9. Phần sát thương thừa khi Khôi Lỗi không đủ HP vẫn hit player (giống rule Linh Thú).
10. Ai cũng có thể craft nếu có bản vẽ và nguyên liệu.

## Open Questions
- [ ] Taxonomy bản vẽ: có bao nhiêu cấp, mỗi cấp khác nhau thế nào?
- [ ] Danh sách skill mẫu cho từng loại bản vẽ.
- [ ] Khi nhiều Khôi Lỗi cùng phòng thủ: rule gánh đòn như thế nào?
- [ ] Slot thần thức chiếm per Khôi Lỗi, mana per lệnh, cooldown cụ thể — phase balance.
- [ ] Lượng độ bền giảm per lần chết — phase data design.
- [x] Khôi Lỗi **không thủ nhà** — chỉ triệu hồi khi player online và chủ động.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [ ] Behavior is specific enough for `dev` to estimate.
- [ ] Acceptance criteria can be written without guessing.
- [ ] Major edge cases are covered.
- [ ] Config/data needs are listed.
- [ ] Out-of-scope items are explicit.
- [ ] Ready to promote to `requirements/`.
