# Map Design Clarification

## Intended player-facing behavior

- Người chơi tồn tại trong một **map instance** cụ thể với vị trí và zone rõ ràng.
- Khi vào world hoặc chuyển map, người chơi phải được đưa vào một instance hợp lệ và nhận lại đầy đủ state hiện tại của map đó.
- Map có thể thuộc một trong các nhóm cảm nhận từ phía người chơi:
  - **home/private map**: không gian riêng gắn với người chơi
  - **public map**: khu vực chung có nhiều zone/instance công cộng
  - **special instance map**: bản sao theo ngữ cảnh như dungeon, map sự kiện, hoặc nội dung có lifecycle riêng
- Nếu một instance kết thúc hoặc bị đóng, người chơi không nên bị kẹt; họ phải được đưa về một nơi an toàn mặc định.
- Việc vào lại world sau reconnect hoặc sync lỗi nên ưu tiên tính liên tục trải nghiệm hơn là fail cứng.

## Intended terminology

- **Map Template**: định nghĩa tĩnh của một map
- **Map Instance**: phiên runtime cụ thể của map mà người chơi đang ở trong
- **Zone**: nhánh/public shard của một map công cộng
- **Home Map**: map an toàn mặc định dùng làm điểm fallback cơ bản
- **Entry Context**: lý do người chơi đi vào map hiện tại (ví dụ: vào world, qua portal, redirect do instance đóng)
- **Configured Instance**: instance đặc thù được tạo theo data/runtime config thay vì public zone mặc định

## Intended rules

- Mỗi người chơi tại một thời điểm chỉ nên nằm trong **một map instance hợp lệ**.
- Map private/home nên gắn với chủ sở hữu; player khác không tự vào được trừ khi có system riêng cho phép.
- Public map nên hỗ trợ nhiều zone/instance để chia tải và mật độ người chơi.
- Khi auto-join public map, hệ thống nên ưu tiên vào zone đang có người nhưng chưa đầy để giữ cảm giác thế giới sống.
- Khi người chơi vào map, server phải đồng bộ lại đầy đủ:
  - map hiện tại
  - zone/instance hiện tại
  - vị trí spawn/entry
  - snapshot runtime liên quan
- Nếu player đang giữ state map không hợp lệ hoặc map không tồn tại, hệ thống nên fallback về **Home Map** thay vì để session hỏng.
- Khi một instance hết vòng đời, player bên trong phải được redirect an toàn. Hướng intent hiện tại là redirect về home/fallback mặc định, chưa thấy rule khác đã được chốt.
- Adjacency/travel permission nên là rule cấp thiết kế riêng, không nên vô tình bị quyết định ngầm bởi các data source không cùng nghĩa gameplay nếu chưa canonicalize rõ.

## Acceptable current behavior

- Catalog map được nạp sẵn, immutable lookup rõ ràng.
- Public map có cơ chế zone/public instance và auto-join zone khả dụng.
- Private map / configured instance / public instance đã được tách đường join khác nhau.
- Khi người chơi vào world hoặc chuyển map, server publish lại snapshot đầy đủ thay vì tin incremental state cũ.
- Nếu instance đóng, player được redirect về một nơi an toàn thay vì bị disconnect/kẹt map.
- Unknown/zero map id fallback về home là chấp nhận được ở giai đoạn hiện tại.

## Mismatch vs current code

- Runtime hiện tại đang gộp **adjacent map** và **portal target map** vào cùng một effective adjacency. Về design intent, đây chưa chắc là cùng một khái niệm và có thể làm travel permission bị nhập nhằng.
- Redirect khi instance đóng hiện luôn về **home map default spawn**. Đây là behavior an toàn, nhưng chưa chắc là intent cuối cho mọi loại instance (ví dụ dungeon, event, trial map).
- `DefaultSpawnPosition` khi thiếu spawn point không được clamp ở cùng đường xử lý như spawn point tường minh. Nếu data lỗi, code có thể phụ thuộc vào chất lượng data hơn là rule gameplay rõ ràng.
- `DefaultZoneIndex` đang là `0` cho private map và `1` cho map khác. Đây là quy ước runtime ổn, nhưng cần canonicalize rõ để tránh bị hiểu thành rule gameplay có ý nghĩa lớn hơn thực tế.
- Code có xử lý âm thầm tình huống player còn nằm trong instance cũ rồi xóa membership cũ trước khi add vào instance mới. Gameplay intent hợp lý là “không bao giờ tồn tại song song ở hai instance”, nhưng canonical doc nên gọi rõ đây là repair behavior của runtime.

## Unresolved design questions

- Có những loại instance nào ngoài public/home mà gameplay chính thức sẽ công nhận? Dungeon, event, quest instance, trial?
- Khi một special instance đóng, fallback luôn về Home Map có đúng intent không, hay cần hỗ trợ fallback theo từng loại instance?
- Zone có phải là khái niệm player-facing không, hay chỉ là runtime shard ẩn?
- Người chơi có được phép chủ động chọn zone/public instance hay không, hay luôn auto-assign?
- Rule travel hợp lệ giữa các map nên canonicalize theo cái gì:
  - adjacency thủ công
  - portal
  - cả hai nhưng khác semantic
- Spawn mặc định của map có phải luôn được bảo đảm nằm trong bounds bằng data contract hay server cần clamp cứng?

## Canonicalization recommendation

- Canonicalize map runtime thành 3 lớp riêng:
  1. **map data model**
  2. **instance/zone lifecycle**
  3. **world entry + redirect behavior**
- Ghi rõ rằng **zone** hiện chủ yếu là runtime/public-instance concept, chưa nên đẩy thành player-facing terminology nếu design chưa xác nhận.
- Tách bạch trong canonical docs giữa:
  - **travel permission topology**
  - **portal-based travel UX**
- Giữ behavior fallback-to-home là canonical tạm thời cho batch 1, nhưng đánh dấu là **safe default**, chưa phải rule thiết kế cuối cùng cho mọi instance type.
- Tạo một canonical note ngắn xác nhận invariant: **mỗi player chỉ thuộc một live instance tại một thời điểm**.
