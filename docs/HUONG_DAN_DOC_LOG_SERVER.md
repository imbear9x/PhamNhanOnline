# Hướng Dẫn Đọc Log Server

Tài liệu này dùng để đọc các log và metrics hiện tại của server, đặc biệt là sau Phase 2.
Mục tiêu là giúp phát hiện sớm các dấu hiệu lag, tick bị trễ, queue packet bị dồn và server bắt đầu quá tải.

## Log đang nằm ở đâu

- Log được ghi vào thư mục `Logs/`.
- Mỗi ngày có một file log theo tên ngày.
- Ngoài ra khi chạy server, một phần thông tin cũng hiện trên console.

Code liên quan:
- [Logger.cs](/e:/PhamNhan/PhamNhanOnline/GameShared/Logging/Logger.cs)
- [Program.cs](/e:/PhamNhan/PhamNhanOnline/GameServer/Program.cs)

## Log nào cần chú ý nhất

Sau Phase 2, server sẽ định kỳ ghi một dòng `ServerMetrics ...`.
Dòng này do `ServerMetricsLoggerService` ghi ra mỗi 30 giây.

Code liên quan:
- [ServerMetricsLoggerService.cs](/e:/PhamNhan/PhamNhanOnline/GameServer/Diagnostics/ServerMetricsLoggerService.cs)
- [ServerMetricsService.cs](/e:/PhamNhan/PhamNhanOnline/GameServer/Diagnostics/ServerMetricsService.cs)

## Cách đọc dòng `ServerMetrics`

Một dòng metrics hiện tại sẽ có các nhóm số chính sau:

- `OnlinePlayers`
  - số player đang online trên server
- `ActiveInboundSessions`
  - số session đang được theo dõi queue inbound
- `QueuedInboundPackets`
  - tổng số packet đang nằm trong queue, chưa xử lý xong
- `MaxQueueDepth`
  - độ sâu queue lớn nhất từng quan sát được
- `InboundEnqueued`
  - tổng số packet đã được enqueue
- `InboundProcessed`
  - tổng số packet đã xử lý xong
- `InboundDropped`
  - tổng số packet bị drop trước khi vào xử lý
- `InboundExceptions`
  - tổng số lỗi trong lúc xử lý packet
- `AvgInboundMs`
  - thời gian xử lý packet trung bình
- `MaxInboundMs`
  - thời gian xử lý packet lâu nhất từng ghi nhận
- `WorldTicks`
  - tổng số tick của `GameLoop`
- `WorldTickOverruns`
  - số tick world bị chạy quá thời gian mục tiêu
- `AvgWorldTickMs`
  - thời gian chạy world tick trung bình
- `MaxWorldTickMs`
  - thời gian chạy world tick lâu nhất
- `WorldInstances`
  - số lượng map instance được world loop nhìn thấy ở lần ghi log gần nhất
- `MaintenanceTicks`
  - tổng số tick của maintenance loop
- `MaintenanceTickOverruns`
  - số tick maintenance bị quá thời gian mục tiêu
- `AvgMaintenanceTickMs`
  - thời gian maintenance tick trung bình
- `MaxMaintenanceTickMs`
  - thời gian maintenance tick lâu nhất
- `MaintenanceSaves`
  - số lần maintenance đã chạy save định kỳ
- `MaintenanceRefreshes`
  - số lần maintenance đã chạy refresh time-derived state

## Dấu hiệu server đang khỏe

Khi server đang ổn, thường thấy:

- `QueuedInboundPackets` gần `0` hoặc tăng rồi về lại thấp
- `MaxQueueDepth` không tăng liên tục theo thời gian
- `InboundDropped = 0`
- `InboundExceptions = 0`
- `AvgWorldTickMs` thấp hơn khá xa so với mốc tick mục tiêu
- `WorldTickOverruns` tăng rất ít hoặc gần như không tăng
- `AvgMaintenanceTickMs` thấp và ổn định

## Dấu hiệu bắt đầu có vấn đề

### 1. Queue packet bị dồn

Dấu hiệu:

- `QueuedInboundPackets` tăng dần
- `MaxQueueDepth` tăng dần
- `InboundProcessed` tăng chậm hơn `InboundEnqueued`

Ý nghĩa:

- Packet vào nhanh hơn tốc độ xử lý.
- Có thể một số handler quá chậm.
- Có thể DB hoặc logic trong handler đang kéo dài thời gian xử lý.

Nên kiểm tra:

- handler nào đang làm việc nặng
- packet loại nào bị spam nhiều
- có chỗ nào đang gọi DB trực tiếp trong đường xử lý nóng hay không

### 2. World tick bị trễ

Dấu hiệu:

- `WorldTickOverruns` tăng liên tục
- `AvgWorldTickMs` tăng bất thường
- `MaxWorldTickMs` nhảy cao

Ý nghĩa:

- `GameLoop` không kịp hoàn thành trong thời gian tick mục tiêu
- simulation đang nặng hơn khả năng xử lý hiện tại

Nên kiểm tra:

- số map instance có tăng mạnh không
- mỗi `instance.Update()` có đang làm quá nhiều việc không
- có logic nào mới được thêm vào world loop không

### 3. Maintenance loop bị trễ

Dấu hiệu:

- `MaintenanceTickOverruns` tăng
- `AvgMaintenanceTickMs` hoặc `MaxMaintenanceTickMs` cao

Ý nghĩa:

- save định kỳ hoặc refresh định kỳ đang quá nặng
- maintenance có thể bắt đầu cạnh tranh tài nguyên với gameplay

Nên kiểm tra:

- save DB có chậm không
- số player dirty state có tăng mạnh không
- `RefreshTimeDerivedStateForOnlinePlayersAsync` có đang làm quá nhiều việc không

### 4. Packet processing có lỗi

Dấu hiệu:

- `InboundExceptions` tăng
- log có các dòng:
  - `Unhandled packet exception`
  - `Inbound processor crashed`

Ý nghĩa:

- Có bug trong handler hoặc middleware
- Có packet hợp lệ nhưng logic xử lý chưa an toàn

Nên kiểm tra:

- loại packet gây lỗi
- session nào bị lỗi
- stack trace trong log

## Tick mục tiêu hiện tại là bao nhiêu

Hiện tại:

- `GameLoop` chạy với mục tiêu `50ms` mỗi tick, tức khoảng `20 TPS`
- `RuntimeMaintenanceService` cũng dùng chu kỳ nền `50ms`
- metrics logger ghi log mỗi `30 giây`

Code liên quan:
- [GameLoop.cs](/e:/PhamNhan/PhamNhanOnline/GameServer/Runtime/GameLoop.cs)
- [RuntimeMaintenanceService.cs](/e:/PhamNhan/PhamNhanOnline/GameServer/Runtime/RuntimeMaintenanceService.cs)
- [ServerMetricsLoggerService.cs](/e:/PhamNhan/PhamNhanOnline/GameServer/Diagnostics/ServerMetricsLoggerService.cs)

## Khi nào cần debug sâu hơn

Không phải cứ thấy một con số cao là phải debug ngay.
Nên debug sâu khi có một trong các pattern sau:

- `QueuedInboundPackets` tăng qua nhiều lần log liên tiếp
- `WorldTickOverruns` tăng đều
- `MaintenanceTickOverruns` tăng đều
- `InboundDropped` khác `0`
- `InboundExceptions` khác `0`
- người chơi bắt đầu cảm nhận lag hoặc thao tác phản hồi chậm

Lúc đó mới nên:

- soi lại handler liên quan
- thêm log chi tiết hơn quanh packet hoặc subsystem đang nghi ngờ
- profile những đoạn code nặng

## Một cách đọc log thực dụng

Thứ tự đọc nhanh khi nghi server chậm:

1. Tìm dòng `ServerMetrics` mới nhất.
2. Xem `QueuedInboundPackets`, `MaxQueueDepth`.
3. Xem `WorldTickOverruns`, `AvgWorldTickMs`, `MaxWorldTickMs`.
4. Xem `MaintenanceTickOverruns`, `AvgMaintenanceTickMs`.
5. Xem `InboundDropped`, `InboundExceptions`.
6. Nếu có bất thường, mới quay lại các dòng lỗi chi tiết trước đó trong log.

## Ghi nhớ quan trọng

- Log là nơi phát hiện vấn đề đầu tiên.
- Debug code là bước sau khi log cho thấy dấu hiệu bất thường.
- Nếu không có metrics, mình chỉ đoán.
- Nếu có metrics nhưng không đọc định kỳ, mình sẽ phát hiện quá muộn.
