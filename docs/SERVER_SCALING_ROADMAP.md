# Lộ Trình Gia Cố Server

Tài liệu này bám theo codebase hiện tại của `PhamNhanOnline`.
Mục tiêu là gia cố nền tảng sớm để sau này thêm movement, multiplayer, combat, quest mà không làm vỡ kiến trúc.

## Cách dùng roadmap

- Làm theo thứ tự từ trên xuống dưới.
- Không nên nhảy qua phase sau nếu phase trước chưa ổn.
- Mỗi mục nên được code, test và merge xong rồi mới qua mục tiếp theo.
- Ưu tiên các thay đổi làm giảm rủi ro kiến trúc trước khi thêm tính năng.

## Nguyên tắc thiết kế

- Server là authoritative.
- Tách rõ networking, simulation và persistence.
- Chỉ gửi dữ liệu cho người cần nhận.
- Không để world update bị block bởi DB.
- Packet realtime và packet nghiệp vụ không dùng chung một kiểu truyền.

## Phase 0 - Khóa nền kiến trúc

Mục tiêu:
- Có tài liệu và ranh giới rõ ràng trước khi thêm tính năng lớn.

Việc cần làm:
- Chốt conventions cho packet:
  - packet request/result
  - packet event/broadcast
  - packet state sync
- Chốt conventions cho runtime state:
  - dữ liệu nào authoritative trên server
  - dữ liệu nào chỉ để hiển thị trên client
- Chốt conventions cho `Map`, `MapInstance`, `PlayerSession`, `CharacterRuntimeState`.
- Viết tài liệu flow cơ bản:
  - đăng nhập
  - vào character
  - vào world
  - runtime save
  - state change

Xong phase khi:
- Người mới vào đọc docs là hiểu server đang vận hành theo flow nào.

## Phase 1 - Tách workload nguy hiểm khỏi main loop

Mục tiêu:
- Không để packet receive và world update bị block bởi thao tác chậm.

Việc cần làm:
- Refactor `NetworkServer` để không block luồng nhận packet bởi handler chậm.
- Giữ packet của cùng một player được xử lý theo thứ tự, nhưng không khóa cả server.
- Tách persistence khỏi `GameLoop`:
  - world loop chỉ đánh dấu việc cần save
  - worker riêng lo batch save xuống DB
- Tách `RefreshTimeDerivedStateForOnlinePlayersAsync` khỏi đường đi dễ gây hitch nếu sau này nó nặng.
- Thêm cancellation, shutdown sequence và flush an toàn.

Xong phase khi:
- Một thao tác DB chậm không làm cả world tick dừng lại.
- Packet của player A chậm không làm player B cảm thấy lag rõ.

## Phase 2 - Chuẩn hóa tick và scheduler

Mục tiêu:
- Có simulation tick ổn định và dễ đo đạc.

Việc cần làm:
- Thay `Thread.Sleep(...)` bằng loop có đo elapsed time.
- Ghi nhận tick duration, tick overrun, queue depth.
- Tách:
  - simulation tick
  - network send tick
  - persistence tick
- Chốt tần số tick mặc định:
  - world tick
  - movement snapshot tick
  - save tick
- Thêm metrics/log có cấu trúc cho:
  - số player online
  - số map instance
  - packet in/out mỗi giây
  - thời gian xử lý packet

Xong phase khi:
- Có thể nhìn log/metrics để biết server đang chậm ở đâu.

## Phase 3 - Chiến lược transport cho packet

Mục tiêu:
- Dùng đúng delivery mode cho từng loại dữ liệu.

Việc cần làm:
- Phân loại packet:
  - reliable ordered cho login, inventory, trade, quest, state transition quan trọng
  - realtime channel cho movement, rotation, animation, state tần suất cao
- Thêm packet categories trong code thay vì mọi thứ đều gửi cùng một kiểu.
- Thêm correlation id cho action packet sẽ cần sau này:
  - skill cast
  - attack
  - interaction
  - trade request
- Review lại rate limit:
  - bỏ rate limit chung theo connection
  - thay bằng rate limit theo packet type hoặc bucket

Xong phase khi:
- Có quy tắc rõ ràng packet nào dùng kênh nào.
- Movement sau này không bị thiết kế buộc phải đi qua reliable ordered.

## Phase 4 - Interest management và spatial partition

Mục tiêu:
- Không broadcast toàn map hoặc toàn server một cách mù quáng.

Việc cần làm:
- Thêm khái niệm `watchers` hoặc `observers` cho entity.
- Chốt cách xác định ai nhìn thấy ai:
  - cùng map instance
  - trong tầm nhìn
  - trong cell hoặc grid
- Bổ sung spatial partition:
  - grid là lựa chọn đơn giản và hợp lý cho giai đoạn đầu
- Thêm các packet:
  - entity spawned
  - entity despawned
  - entity moved
  - entity state changed
- Chỉ gửi sự kiện cho những client có liên quan.

Xong phase khi:
- Số packet gửi ra tăng theo khu vực có người, không tăng theo tổng số player toàn server.

## Phase 5 - Runtime state và dirty replication

Mục tiêu:
- Không gửi lại cả state khi chỉ đổi một phần nhỏ.

Việc cần làm:
- Chia state thành nhóm:
  - stats cơ bản
  - current state
  - movement state
  - combat state
  - appearance state
- Có dirty flags riêng cho network replication, không chỉ cho DB persistence.
- Hỗ trợ delta update thay vì gửi full state mỗi lần.
- Gom packet nhỏ thành batch hợp lý nếu cần.

Xong phase khi:
- Thay đổi nhỏ chỉ phát sinh update nhỏ.

## Phase 6 - Chiến lược persistence

Mục tiêu:
- Lưu DB an toàn mà không làm chậm gameplay.

Việc cần làm:
- Chuyển sang save queue hoặc background worker.
- Gom save theo batch.
- Chốt chính sách save:
  - periodic save
  - disconnect flush
  - critical event flush
- Phân tách dữ liệu:
  - dữ liệu cần save ngay
  - dữ liệu có thể save trễ
- Xem xét snapshot cộng event log cho các hệ thống lớn về sau.

Xong phase khi:
- Online player tăng lên mà world tick vẫn ổn.

## Phase 7 - Nền tảng movement multiplayer

Mục tiêu:
- Có nền để nhìn thấy và di chuyển cùng nhau trong map.

Việc cần làm:
- Chốt model movement server-authoritative hoặc hybrid có reconciliation.
- Thêm packet:
  - move input
  - move snapshot
  - teleport hoặc correction
- Client interpolation cho player khác.
- Client prediction có kiểm soát cho player local nếu cần.
- Rate limit riêng cho movement.

Xong phase khi:
- 2 đến 10 player cùng map di chuyển mượt mà không spam packet vô tội vạ.

## Phase 8 - Hạ tầng sẵn sàng cho combat

Mục tiêu:
- Sẵn sàng cho skill và combat mà không phải sửa ngược kiến trúc.

Việc cần làm:
- Thêm action id hoặc command id cho combat packet.
- Tách:
  - request cast
  - result accept hoặc reject
  - combat event broadcast
- Chuẩn hóa cooldown timing theo server authority.
- Chuẩn hóa target validation, range validation và state validation.
- Thêm combat event queue nếu cần.

Xong phase khi:
- Có thể thêm skill đầu tiên mà không phải đổi lại network model.

## Phase 9 - Quest, trade, social

Mục tiêu:
- Thêm hệ thống nghiệp vụ trên nền đã ổn định.

Việc cần làm:
- Quest packets theo reliable flow.
- Trade request, result, cancel, confirm.
- Friend, guild, chat theo packet type riêng.
- Logging, audit và anti-abuse cho nghiệp vụ quan trọng.

Xong phase khi:
- Các hệ thống nghiệp vụ không ảnh hưởng xấu tới realtime gameplay.

## Phase 10 - Quan sát vận hành và load test

Mục tiêu:
- Biết server chịu được đến đâu trước khi đưa thêm tính năng lớn.

Việc cần làm:
- Thêm counters và metrics có thể quan sát được.
- Viết bot hoặc client giả lập:
  - login
  - vào map
  - di chuyển
  - spam packet hợp lệ
- Chạy load test theo mốc:
  - 10 player
  - 50 player
  - 100 player
  - nhiều instance
- Ghi kết quả và bottleneck của từng mốc.

Xong phase khi:
- Mỗi thay đổi lớn đều có cách đo ảnh hưởng performance.

## Thứ tự ưu tiên nên làm ngay

Nếu chỉ chọn những việc nên làm rất sớm, ưu tiên theo thứ tự này:

1. Viết docs và rule nền kiến trúc trong Phase 0.
2. Tách DB save khỏi `GameLoop`.
3. Sửa model xử lý packet để không block toàn server.
4. Chuẩn hóa delivery mode và packet categories.
5. Bỏ rate limit thô hiện tại, thay bằng rate limit theo packet type.
6. Thêm metrics và log để biết server đang chậm ở đâu.
7. Thiết kế interest management trước khi làm multiplayer movement thật sự.

## Cách nhờ Codex làm lần lượt

Sau này có thể nhắn theo dạng:

- `Làm Phase 0, mục 1`
- `Làm tiếp Phase 1, tách DB save khỏi GameLoop`
- `Làm Phase 3, thiết kế packet categories`
- `Làm metrics cơ bản cho tick và packet`

## Ghi chú cuối

Roadmap này tránh tối ưu sớm một cách mơ hồ.
Nó tập trung vào những điểm mà nếu bỏ qua, sau này thêm movement, combat, quest rất dễ làm vỡ hệ thống.
