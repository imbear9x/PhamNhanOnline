# Player Stats Design Clarification

## Intended player-facing behavior

- Mỗi nhân vật có một bộ chỉ số xác định sức mạnh, khả năng sinh tồn, và tương tác trong thế giới.
- Chỉ số được chia thành 2 lớp từ góc nhìn người chơi:
  - **Base stats**: chỉ số nền gốc của nhân vật, tăng qua tu luyện, đột phá, phân bổ tiềm năng.
  - **Final stats**: chỉ số thực tế có hiệu lực sau khi cộng thêm từ trang bị và võ học.
- Người chơi nên nhìn thấy được cả hai để hiểu "nền của mình là bao nhiêu" và "thực tế đang dùng là bao nhiêu".
- Khi chỉ số nền thay đổi (đột phá, phân bổ), HP/MP/Stamina hiện tại phải được clamp lại về không vượt max mới.
- Tu luyện xảy ra theo thời gian, kể cả offline, và tích lũy progression theo đơn vị cảnh giới.
- Đột phá lên cảnh giới mới là sự kiện có rủi ro/thành công/thất bại.
- Phân bổ tiềm năng là hành động chủ động của người chơi để tăng chỉ số cụ thể.

## Intended terminology

- **Base Stats**: chỉ số nền trước khi cộng trang bị/võ học
- **Final Stats**: chỉ số cuối cùng sau tất cả modifier — đây là con số combat thực tế
- **Current State**: trạng thái hiện tại như HP/MP/Stamina hiện tại, trạng thái sống/chết
- **Realm (Cảnh Giới)**: tầng tu luyện của nhân vật
- **Cultivation**: quá trình tích lũy tu vi theo thời gian
- **Breakthrough (Đột Phá)**: hành động nâng cảnh giới khi đủ điều kiện
- **Potential (Tiềm Năng)**: điểm nhận được qua tu luyện, dùng để phân bổ tăng stat cụ thể
- **Martial Art (Võ Học / Pháp Môn)**: phương pháp tu luyện đang kích hoạt, ảnh hưởng tốc độ hấp thụ và stat bonus theo giai đoạn
- **Stat Modifier**: lượng cộng thêm vào base stat từ trang bị, tiềm năng, hoặc pháp môn

## Intended rules

- Final stat = base stat + tiềm năng flat bonus + trang bị modifier + pháp môn stage bonus.
- Trang bị và pháp môn chỉ ảnh hưởng final stat, không làm thay đổi base stat.
- Khi base stat thay đổi, current HP/MP/Stamina phải bị clamp không vượt max mới.
- Tu luyện chỉ được bắt đầu khi nhân vật đang ở **private home instance**, không đang tu luyện, và có pháp môn hợp lệ.
- Tu luyện có thể tích lũy offline; khi player login, settlement được xử lý trước khi gửi data về client.
- Khi đạt tới giới hạn tu vi của cảnh giới hiện tại, tu luyện tự dừng; để tiến tiếp phải đột phá.
- Đột phá thành công → tăng cảnh giới. Đột phá thất bại → có penalty (hướng intent là penalty cultivation, chưa chốt chi tiết).
- Phân bổ tiềm năng hoạt động theo **tier trong cảnh giới**: không thể phân bổ vượt tier đang mở, không thể nhảy tier.
- Nhân vật có thể rơi vào trạng thái `CombatDead` hoặc `LifespanExpired` tùy theo nguồn gốc HP về 0 hoặc thọ nguyên cạn.
- Thần Thức (`Sense`) là một trong các stat có `max` và `current` riêng, ảnh hưởng đến tầm nhìn/tàng hình theo `features/spirit-sense.md`.
- Speed ảnh hưởng đến movement và evasion theo `features/speed-system.md`.
- Luck (`Cơ Duyên`) là stat có kiểu `double`, ảnh hưởng tỉ lệ random outcomes (loot, crafting...).

## Acceptable current behavior

- Final stat recompute từ 3 nguồn (tiềm năng, trang bị, pháp môn) là đúng hướng.
- Client nhận cả raw base stats lẫn final stats là chấp nhận được, miễn canonical docs giải thích rõ ý nghĩa từng field.
- Settlement cultivation offline rồi apply trước khi gửi data client là đúng hướng.
- Cultivation tự dừng khi đạt cap cảnh giới là hành vi đúng.
- Clamping HP/MP/Stamina về max mới sau base stat mutation là đúng.
- Phân bổ tiềm năng theo tier-local và recompute final stat ngay sau khi phân bổ là đúng hướng.
- `checked` arithmetic cho integer final stat để throw thay vì overflow là chấp nhận được.

## Mismatch vs current code

- **Percent modifier format không thống nhất**: code đang normalize `0..100` về `0..1` khi tính, nghĩa là data có thể ghi theo 2 format khác nhau mà không bị báo lỗi. Đây là rủi ro data quality và cần canonical doc ghi rõ format chuẩn phải là gì.
- **Breakthrough thay đổi base stats nhưng không tự recompute final stats**: `CharacterCultivationService.BreakthroughAsync` push base stat thay đổi trực tiếp, còn việc recompute final stat chỉ xảy ra trong action-service wrapper khi reply đột phá thành công. Caller nào không đi qua wrapper đó có thể sẽ có final stats lệch tạm thời.
- **Penalty khi đột phá thất bại chưa có rule design rõ ràng**: code extract cho thấy có penalty path, nhưng semantic gameplay của penalty đó chưa được xác nhận trong design note hiện có.
- **Stat `Luck` là `double`** trong khi các stat khác là `int`; điều này không phải mismatch về behavior, nhưng canonical docs cần ghi rõ để tránh downstream code xử lý sai kiểu.
- **Client nhận cả raw và final stats**: nếu downstream code hoặc client dùng nhầm raw thay vì final trong combat context, sẽ bị sai. Canonical docs phải nói rõ field nào dùng cho combat, field nào chỉ dùng cho display/preview.

## Unresolved design questions

- Penalty khi đột phá thất bại cụ thể là gì? Mất bao nhiêu cultivation progress? Có ảnh hưởng stat không? Có cooldown không?
- Cơ duyên (`Luck`) ảnh hưởng đến những outcome nào chính thức? Loot rate, craft rate, breakthrough rate, hay tất cả?
- Thần thức có hiển thị hai thanh `max/current` riêng cho người chơi không, hay chỉ hiển thị một chỉ số?
- Có phải tất cả các stat đều có thể được tăng bằng tiềm năng, hay chỉ một tập con được chọn?
- Format chuẩn cho percent modifier trong data là `0..1` hay `0..100`? Cần chốt để không tiếp tục dùng 2 format song song.
- Stat bonus từ pháp môn áp dụng theo giai đoạn (`stage`) cụ thể thế nào từ góc nhìn người chơi? Có phải tự động theo cảnh giới không?
- Khi nào final stat cần được recompute proactive (không chỉ khi có action rõ ràng)?

## Canonicalization recommendation

- Canonicalize player stats theo 3 doc riêng:
  1. **stat model**: các stat hiện có, kiểu dữ liệu, layer (base/current/final), và cách tính final stat
  2. **cultivation + breakthrough runtime**: settlement logic, cap, đột phá thành công/thất bại
  3. **potential allocation**: tier system, rule phân bổ, ảnh hưởng lên final stat
- Ghi rõ trong canonical doc rằng **final stat là con số combat thực tế**, raw base stat chỉ dùng cho display/progression preview.
- Canonicalize format percent modifier là `0..1` hoặc `0..100`, chọn một, ghi vào data-design contract.
- Đánh dấu **breakthrough failure penalty** là `needs design decision` — không canonicalize chi tiết cho đến khi có quyết định design.
- Ghi rõ trong canonical doc về Luck rằng đây là stat kiểu `double`, có ý nghĩa khác với stat `int` thông thường.
- Nối sang `features/spirit-sense.md` và `features/speed-system.md` trong game-design-wp như referenced design docs khi canonicalize stat domain.
