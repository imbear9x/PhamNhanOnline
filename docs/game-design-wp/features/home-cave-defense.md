---
doc_type: game_design_feature
system_id: home-cave-defense
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-09
updated_at: 2026-05-12
promoted_from: notes/home-cave-defense.md
related_docs:
  - features/spirit-beast.md
  - features/death-penalty.md
  - features/spirit-sense.md
  - clarifications/home-cave-garden-herb-design-clarification.md
requires_code_verification: false
---

# Hệ Thống Động Phủ / Công Động Phủ / Cướp Bóc — Feature Draft

## Goal

Tạo hệ thống **động phủ cá nhân có thể triển khai ra thế giới**, vừa là căn cứ phát triển của người chơi, vừa là điểm phát sinh PvP bất đối xứng có rủi ro cao.

Bốn giá trị cần tạo được:
1. **Cảm giác sở hữu** — người chơi có căn cứ riêng để tu luyện, luyện chế, nuôi linh thú.
2. **Rủi ro tài sản có kiểm soát** — đồ để trong động phủ có thể bị cướp, nhưng không mất toàn bộ tiến trình.
3. **Drama PvP tự phát** — nhiều người có thể cùng đánh một động phủ và giết lẫn nhau để tranh phần thưởng.
4. **Quyết định chiến lược dài hạn** — đặt động phủ ở đâu, thủ bằng gì, mang gì khi đi cướp, lúc nào nên thu dọn.

## Design Summary

Mỗi người chơi có vòng đời động phủ gồm 2 giai đoạn:
- **Động phủ khởi đầu private**: an toàn tuyệt đối, dùng làm home ban đầu.
- **Động phủ thế giới**: mở bằng bản vẽ tại ô hợp lệ trên map, có thể bị phát hiện và tấn công nếu người khác vượt qua **Thần Thức Quan**.

Người chơi chỉ có **1 động phủ active** tại một thời điểm. Động phủ gồm:
- **Nội thất / phòng chức năng**: tu luyện, luyện chế, quản lý linh thú.
- **Khu cửa động phủ**: map phòng thủ, nơi đặt cổng, trận pháp, linh thú thủ nhà, diễn ra combat khi bị công.

Nếu phá được cổng và vào trong, kẻ tấn công có thể **cướp tài sản lưu trong động phủ**, nhưng không cướp trực tiếp đồ đang trong túi nhân vật.

## Scope

### In Scope
- Vòng đời tạo / mở / thu dọn / bị phá của động phủ
- Quy tắc triển khai động phủ lên map thế giới
- Điều kiện nhìn thấy / tương tác qua Thần Thức Quan
- Map cửa động phủ và phòng thủ cơ bản
- Xâm nhập, cướp bóc, đẩy player ra ngoài khi sụp
- Quy tắc khách được mời vào thăm
- Tương tác với linh thú thủ nhà, trận pháp, death penalty

### Out Of Scope
- Chỉ số balance cụ thể (HP cổng, thời gian hồi, tỷ lệ rớt...)
- UI chi tiết từng màn hình phòng chức năng
- Rule chi tiết luyện đan / luyện khí / ấp trứng
- Data model / backend
- Anti-abuse và anti-alt-account nâng cao

## Core Loop

### Loop của chủ động phủ
1. Nhận hoặc mua **Bản Vẽ Động Phủ**.
2. Chọn vị trí hợp lệ để mở động phủ trên map thế giới.
3. Dùng động phủ làm nơi tu luyện, luyện chế, cất tài sản, nuôi linh thú.
4. Thiết lập phòng thủ bằng cổng, trận pháp, linh thú.
5. Khi bị tấn công: online phòng thủ, chấp nhận rủi ro, hoặc thu dọn kịp thời.
6. Nếu bị phá: nhận lại bản vẽ, chọn vị trí mới dựng lại.

### Loop của người đi công
1. Phát hiện động phủ nếu vượt qua **Thần Thức Quan**.
2. Cân nhắc tấn công dựa trên rủi ro cao khi chết.
3. Vào map cửa, đối mặt cổng + trận pháp + linh thú thủ nhà + người chơi khác cũng đang cướp.
4. Phá cổng, tranh cướp tài sản.
5. Rút trước khi bị giết.

## Player-Facing Rules

### Vòng đời động phủ

**Động phủ khởi đầu:**
- Mỗi account có 1 động phủ private vĩnh viễn khi tạo mới.
- Không ai tấn công được. Đây là home mặc định ban đầu.

**Mở động phủ ra thế giới:**
- Dùng Bản Vẽ Động Phủ tại vị trí hợp lệ → cần trải qua **cast time** → động phủ xuất hiện trên map.
- Bản vẽ biến mất khỏi kho. Động phủ private ban đầu **biến mất vĩnh viễn**.

**Giới hạn:**
- Mỗi người chỉ có **1 động phủ active** tại mọi thời điểm.

**Thu dọn:**
- Chủ nhà có thể thu dọn khi động phủ **không đang bị tấn công**.
- Khi thu dọn thành công: động phủ biến mất, **Bản Vẽ Động Phủ** quay về kho đồ.

**Khi bị phá hoàn toàn:**
- Chủ nhà trong động phủ bị teleport ra ngoài ngay lập tức.
- Động phủ vào **phase sụp đổ 1 phút** — ai cũng vào được, nhặt đồ, đánh nhau.
- Sau 1 phút: tất cả bị đẩy ra, động phủ biến mất, ô cell trống, Bản Vẽ trả về kho đồ chủ nhà.

### Đặt động phủ trên map
- Chỉ đặt tại **ô cell hợp lệ** trong map được config cho phép.
- Ô không có động phủ khác.
- Chiếm 1 ô cell, hiển thị tên động phủ.
- Người đi ngang chỉ nhìn thấy nếu vượt **Thần Thức Quan**.

### Thần Thức Quan
- Cấp bản vẽ = cấp động phủ → quyết định ngưỡng Thần Thức Quan.
- Không vượt ngưỡng: không nhìn thấy, không tương tác, không tấn công được.

### Phòng chức năng bên trong
- Mật Thất, Đan Thất, Luyện Khí Thất, Linh Thú Thất, Dục Linh Thất, Cổng ra Cửa Động Phủ.
- Phải ngồi đúng phòng để dùng chức năng tương ứng.
- Sản phẩm đi thẳng vào balo — trừ **Dục Linh Thất**: trứng/sản phẩm chưa lấy vẫn nằm lại trong phòng và có thể bị cướp.
- **Rương / slot chứa đồ có giới hạn**, tăng theo phẩm cấp Bản Vẽ Động Phủ.

### Khách vào thăm
- Điều kiện: là **bạn bè** của chủ + được chủ **gửi lời mời**.
- Chủ chỉ gửi lời mời khi khách đứng cạnh động phủ.
- Không thể mời khi động phủ đang bị công.
- Khách được mời: di chuyển tự do, **không được mở rương / lấy đồ / dùng chức năng quản trị**.

### Phòng thủ cửa động phủ
- Có thể bố trí: cổng (có HP riêng), trận pháp, linh thú thủ nhà.
- Cổng tự hồi HP theo thời gian nếu chưa bị phá hẳn.
- Loại trận pháp dự kiến: tấn công, tăng thủ cho cổng, tăng Thần Thức Quan.

### Tấn công động phủ
- Cần **Bùa Phá Phủ** đúng phẩm cấp tương ứng động phủ mục tiêu.
- Nhiều người có thể cùng vào cùng lúc — **free-for-all**, ai cũng có thể đánh ai.
- Khi cổng vỡ và vào trong: **ai vào trước lấy trước**.

**Penalty người đi công:**
- Tỷ lệ rớt đồ khi chết: ~2–3 lần PvP thường.
- Penalty thọ nguyên khi chết: ~2–3 lần PK thường.
- Cooldown: sau mỗi lần công, phải chờ 1–2 ngày mới được công bất kỳ động phủ nào tiếp.
- Cooldown tính **theo người đi công**, không theo động phủ bị công.

**Đền bù cho chủ nhà:**
- Mỗi lần có người phát động tấn công hợp lệ → chủ nhà **nhận linh thạch đền bù** dù thắng hay thua.
- Trích trực tiếp từ giá Bùa Phá Phủ. Phần còn lại là cơ chế hút tiền của game.
- Ví dụ định hướng: Bùa giá 10 linh thạch → chủ nhận ~3–5 linh thạch.

### Chủ nhà phản ứng khi bị công
- Luôn nhận thông báo dù online hay offline.
- Có thể ra Cửa Động Phủ phòng thủ, hoặc không làm gì.
- Logout không làm dừng cuộc công.
- Chết khi thủ nhà: rớt nhiều hơn bình thường + **không thể hồi sinh cho đến khi tất cả kẻ tấn công rời khỏi khu vực**.
- Nếu động phủ bị phá khi chủ offline: khi login lại, chủ không còn trong động phủ cũ, được xử lý như mất nhà.

### Tài sản có thể bị cướp
- **Có thể bị cướp**: rương trong động phủ, trứng / sản phẩm tại Dục Linh Thất chưa lấy ra.
- **Không thể bị cướp trực tiếp**: đồ đang trong túi người chơi.
- **Gián tiếp**: nếu bị giết thì đồ trong túi vẫn có thể rơi theo death penalty chung.

### Linh thú thủ nhà khi động phủ bị phá
- Còn sống: quay về **túi linh thú** của chủ.
- Đã chết: về túi linh thú ở trạng thái **ngủ hồi phục**.

## System States

| State | Mô tả |
|---|---|
| Private Home | Map private, an toàn tuyệt đối, home ban đầu |
| Động Phủ Thế Giới — Bình Thường | Đã mở trên map, có thể bị phát hiện, chưa bị công |
| Đang Bị Công | Có Bùa Phá Phủ hợp lệ được kích hoạt, khóa thu dọn |
| Phase Sụp Đổ | Cổng bị phá hoàn toàn, đếm ngược 1 phút |
| Đã Biến Mất / Trả Bản Vẽ | Sau sụp đổ hoặc chủ nhà thu dọn |

**Chuyển trạng thái:**
- Private Home → Động Phủ Thế Giới: dùng Bản Vẽ tại vị trí hợp lệ.
- Bình Thường → Đang Bị Công: có lượt công hợp lệ bằng Bùa Phá Phủ.
- Đang Bị Công → Bình Thường: cuộc công kết thúc, động phủ không bị phá.
- Đang Bị Công → Phase Sụp Đổ: phòng thủ bị phá đến ngưỡng hoàn toàn.
- Phase Sụp Đổ → Đã Biến Mất: hết 1 phút.
- Bình Thường → Đã Biến Mất: chủ nhà thu dọn hợp lệ.

## Main Flows

### Flow 1 — Phát động tấn công
1. Người công vượt Thần Thức Quan, thấy động phủ.
2. Dùng Bùa Phá Phủ đúng cấp → tiêu hao 1 bùa.
3. Động phủ chuyển sang trạng thái **Đang Bị Công**.
4. Chủ nhà nhận thông báo + linh thạch đền bù.
5. Map Cửa Động Phủ kích hoạt tranh chấp.
6. Bùa có thời gian hiệu lực (tạm thời 15 phút).

### Flow 2 — Công thành công
1. Phá cổng / phòng thủ đến ngưỡng sụp.
2. Chủ nhà trong động phủ bị đẩy ra.
3. Phase sụp đổ 1 phút — nhặt đồ, đánh nhau tự do.
4. Hết 1 phút: tất cả ra ngoài, động phủ biến mất, bản vẽ trả về chủ.

### Flow 3 — Công thất bại / hết thời gian bùa
1. Bùa Phá Phủ hết hiệu lực mà chưa phá được.
2. Cuộc công kết thúc.
3. Động phủ về Bình Thường, cổng hồi phục.
4. Người công vẫn mất bùa và chịu cooldown / rủi ro đã phát sinh.

## Edge Cases
- Chủ nhà offline trong lúc bị công: cuộc công diễn ra bình thường, không dừng.
- Chủ nhà bị phá khi offline: khi login lại được teleport ra map public ngẫu nhiên.
- Người được mời vào thăm khi động phủ bị công: tự động được tính là người tham gia cuộc công.
- Linh thú thủ nhà chủ offline bị chết mà kẻ công bỏ đi: linh thú hồi sinh sau thời gian để tiếp tục thủ.
- Pet bị hạ khi thủ, kẻ công vào cướp đồ: pet về túi linh thú của chủ.
- Bùa Phá Phủ hết hiệu lực đúng lúc đang giao tranh tại cổng: cần xác định rule xử lý trạng thái (open question).

## Data / Config Needs
- Cast time dựng động phủ (`game_configs`)
- Thời gian hiệu lực Bùa Phá Phủ theo phẩm cấp (`game_configs`, tạm 15 phút)
- Giá Bùa Phá Phủ theo phẩm cấp
- Tỷ lệ linh thạch đền bù (ví dụ 30–50% giá bùa)
- Cooldown người đi công (tạm 1–2 ngày)
- Số lượng rương / slot tăng theo phẩm cấp bản vẽ
- Penalty chết khi công / thủ động phủ (hệ số so với PvP thường)
- Map / zone được phép đặt động phủ

## UI / UX Notes
- Thông báo bị công: hiển thị dù online hay offline, ưu tiên.
- Phòng chức năng: tab riêng hoặc màn hình riêng, chỉ mở được khi trong dynamic phủ.
- Cửa động phủ: map riêng, hiển thị rõ trạng thái cổng và phòng thủ.
- Bùa Phá Phủ: UI confirm trước khi dùng, hiển thị thời gian hiệu lực đang chạy.

## Related Systems
- **Linh Thú**: thủ nhà, bị hạ khi công phủ — xem `features/spirit-beast.md`
- **Trận Pháp**: lớp phòng thủ cửa động phủ — xem `features/crafting-talisman-formation.md`
- **Death Penalty**: penalty chết khi công/thủ phủ nặng hơn bình thường — xem `features/death-penalty.md`
- **Thần Thức**: quyết định nhìn thấy / tương tác động phủ — xem `features/spirit-sense.md`
- **Khai thác linh thạch**: tách biệt, không gộp vào hệ này

## Key Decisions
1. Mỗi account có 1 động phủ private ban đầu.
2. Khi mở động phủ thế giới, động phủ private ban đầu biến mất vĩnh viễn.
3. Mỗi người chỉ có 1 động phủ active tại một thời điểm.
4. Không được thu dọn khi đã có người tới tấn công.
5. Nếu bị phá: bản vẽ quay lại kho đồ chủ nhà.
6. Dựng động phủ có cast time.
7. Chỉ người vượt Thần Thức Quan mới thấy và công được.
8. Khách được mời: đi lại tự do, không đụng tài sản.
9. Tài sản bị cướp là đồ để trong động phủ, không phải đồ trong túi.
10. Rương / slot giới hạn, tăng theo phẩm cấp bản vẽ.
11. Công động phủ là PvP free-for-all.
12. Người đi công chịu penalty chết nặng hơn bình thường.
13. Muốn công phải dùng Bùa Phá Phủ đúng phẩm cấp.
14. Chủ nhà luôn nhận linh thạch đền bù dù thắng hay thua.
15. Linh thạch đền bù trích từ giá bùa, phần còn lại là sink tiền.
16. Logout không làm dừng hay hủy cuộc công.
17. Trong 1 phút phase sụp đổ: tự do vào, nhặt, đánh nhau.
18. Chủ nhà chết khi thủ: không hồi sinh ngay khi đối phương còn trong khu.

## Open Questions
- [ ] Cast time dựng động phủ cụ thể là bao lâu?
- [ ] Công thức số lượng rương / slot tăng theo từng phẩm cấp bản vẽ?
- [ ] Có phân tầng map đặt động phủ theo cấp map không?
- [ ] Người không đủ Thần Thức Quan: hoàn toàn không thấy gì, hay có hiệu ứng mơ hồ?
- [ ] Giá cụ thể của từng phẩm cấp Bùa Phá Phủ?
- [ ] Tỷ lệ linh thạch đền bù theo từng phẩm cấp?
- [ ] Thời gian hiệu lực bùa: giữ 15 phút cho mọi cấp hay tách theo phẩm cấp?
- [ ] Xử lý trạng thái thế nào nếu Bùa Phá Phủ hết hiệu lực đúng lúc đang giao tranh tại cổng?
- [ ] Giới hạn số người tham gia tối đa trong map Cửa Động Phủ?
- [ ] Điều kiện kỹ thuật chính xác để xác định một cuộc công "đã kết thúc" nếu không phá được?

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [ ] Behavior is specific enough for `dev` to estimate.
- [ ] Acceptance criteria can be written without guessing.
- [ ] Major edge cases are covered.
- [ ] Config/data needs are listed.
- [ ] Out-of-scope items are explicit.
- [ ] Ready to promote to `requirements/`.
