# Thiết Kế Flow Enemy, Boss, Map Instance Và Reward

## Mục tiêu

File này chốt flow gameplay và luồng runtime cho các hệ:

- enemy thường
- boss
- map instance
- tiêu diệt enemy/boss
- reward khi tiêu diệt

File này chưa đi vào DB schema.
Mục tiêu là khóa trước:

- vòng đời runtime của map, enemy, boss, instance
- flow combat và reward
- những rule gameplay quan trọng cần giữ xuyên suốt

## Phạm vi

Flow này phục vụ cho:

- map public có quái farm
- map public có boss thế giới
- map private/home
- map instance
- reward rơi ra đất hoặc reward chuyển thẳng vào inventory

## Nguyên tắc thiết kế

- Enemy và boss là runtime entity sống trong RAM, không query DB mỗi tick.
- Config chỉ dùng để build definition khi server khởi động hoặc reload.
- Random service chỉ roll kết quả, không biết boss/map/instance.
- Gameplay layer quyết định:
  - ai được nhận
  - nhận kiểu gì
  - dùng bảng random nào
  - roll bao nhiêu lần
- Reward có thể tồn tại dưới 2 dạng:
  - vật phẩm/currency rơi ra tại chỗ
  - reward chuyển thẳng vào inventory/currency của người chơi

## Khái niệm chính

### 1. Map Template

Map template là định nghĩa gốc của map:

- loại map
- quái nào có thể spawn
- rule boss
- objective nếu là instance
- rule reward đặc thù nếu có

Từ một map template có thể sinh ra nhiều runtime map.

### 2. Map Runtime

Map runtime là bản map đang chạy thật.

Ví dụ:

- `Starter Plains - Zone 1`
- `Starter Plains - Zone 2`
- `Dungeon Hang Dong - Instance #A12`
- `Home Cave của player X`

Map runtime quản lý:

- danh sách player trong map
- danh sách enemy/boss đang sống
- timer runtime
- objective/runtime state nếu là instance
- ground reward đang tồn tại trong map

### 3. Enemy Runtime

Enemy runtime giữ:

- máu hiện tại
- vị trí spawn gốc
- trạng thái hiện tại: idle, patrol, combat, dead
- danh sách aggro
- danh sách contribution
- cooldown skill

Enemy có skill giống player nhưng:

- không dùng MP
- chỉ cần thỏa cooldown là có thể dùng skill
- có thời gian tối thiểu giữa 2 lần tung chiêu

### 4. Boss Runtime

Boss là một dạng enemy đặc biệt.

Boss có thể có thêm:

- phase
- trigger riêng
- reward rule riêng
- announcement
- objective liên quan đến map/instance

### 5. Instance Runtime

Instance là map runtime riêng cho:

- 1 player
- party
- nhóm đặc biệt

Phase đầu chốt:

- chỉ làm instance solo

## Phân loại map theo flow

### A. Map Public

Dùng cho:

- farm quái
- boss thế giới
- cạnh tranh nhiều người

Đặc điểm:

- có nhiều zone runtime
- player ở cùng một không gian chung
- quái respawn theo timer
- boss spawn theo timer hoặc objective/event

### B. Map Private

Dùng cho:

- home map
- động phủ
- khu vực riêng của player

Đặc điểm:

- runtime riêng cho từng player
- không có cạnh tranh nhiều người
- có thể dùng cho trồng dược liệu, NPC riêng, vật thể riêng

### C. Map Instance

Dùng cho:

- dungeon
- phụ bản boss
- challenge

Đặc điểm:

- runtime được tạo khi player yêu cầu vào
- runtime có objective riêng
- runtime bị hủy khi hoàn thành, thất bại hoặc timeout

## Flow vòng đời enemy thường

### 1. Spawn

Khi map runtime khởi tạo hoặc đến kỳ respawn:

1. server lấy definition spawn của map
2. server chọn loại enemy theo rule spawn
3. server tạo enemy runtime
4. server đặt vị trí spawn
5. server phát entity spawn tới các client quan sát được

Lưu ý:

- trong cùng một nhóm spawn có thể có nhiều loại quái
- cần có rule xác suất spawn từng loại

### 2. Idle / Patrol

Khi chưa combat:

- enemy đứng yên hoặc tuần tra trong phạm vi patrol
- có tầm phát hiện
- nếu phát hiện mục tiêu hợp lệ hoặc bị tấn công thì chuyển sang combat

### 3. Combat

Khi vào combat:

- enemy dùng tầm combat thay cho tầm patrol
- cập nhật aggro khi bị đánh hoặc bị tác động
- chọn target theo aggro rule
- nếu target hiện tại chết hoặc ra khỏi tầm combat thì chọn lại target khác
- nếu không còn aggro hợp lệ thì quay lại idle/patrol

Enemy sử dụng skill theo rule:

- duyệt các skill hiện có
- skill nào đủ cooldown thì có thể dùng
- giữa hai lần tung chiêu phải có khoảng nghỉ tối thiểu

### 4. Contribution

Trong lúc bị đánh, enemy lưu runtime contribution:

- player id
- tổng damage
- last hit time
- player còn ở trong map hay không
- player còn hợp lệ nhận reward hay không

Contribution chỉ lưu trong runtime, không lưu DB.

### 5. Chết

Khi HP về 0:

1. đánh dấu dead
2. chặn hit mới
3. snapshot contribution
4. xử lý reward
5. phát packet death/kill
6. xóa enemy entity sau delay ngắn hoặc chuyển sang corpse state
7. lên lịch respawn nếu thuộc loại respawnable

### 6. Respawn

Sau X giây:

- server tạo lại enemy runtime mới
- reset toàn bộ combat state
- spawn lại ở vị trí hợp lệ

Chốt:

- phase đầu dùng respawn theo timer cố định

## Combat leash và hồi máu

Khi player ra khỏi tầm combat:

- enemy ngừng tấn công player đó
- nếu không còn aggro nào hợp lệ thì quay về idle/patrol

Rule hồi máu:

- enemy thường:
  - giữ máu hiện tại trong một khoảng thời gian
  - nếu không bị ai tấn công tiếp thì hồi đầy máu
- boss:
  - không tự hồi đầy máu theo rule trên

## Flow boss

Boss chia làm 3 nhóm:

### 1. Boss thế giới

Flow:

1. boss spawn ở map public
2. server có thể broadcast thông báo
3. nhiều player cùng tham gia đánh
4. khi boss chết thì reward xử lý theo rule boss đó
5. boss vào cooldown respawn

### 2. Boss theo objective

Flow:

1. player vào map hoặc instance
2. hoàn thành objective trung gian
3. server spawn boss
4. giết boss để mở phase tiếp hoặc complete map

### 3. Boss instance

Flow:

1. tạo instance
2. spawn boss theo logic của run
3. player đánh boss
4. boss chết thì settlement reward của run
5. instance complete

## Flow vào instance

### 1. Yêu cầu vào instance

Client gửi yêu cầu.

Server kiểm tra:

- player có đủ điều kiện không
- có đang ở instance khác không
- có item/vé yêu cầu không
- có cooldown không

Nếu hợp lệ:

1. tạo runtime instance
2. spawn player vào instance
3. khởi tạo enemy/objective
4. gửi snapshot cho client

### 2. Trong lúc đang chạy

Instance runtime quản lý:

- player hiện tại
- objective
- danh sách enemy/boss active
- timer còn lại
- trạng thái run

### 3. Hoàn thành

Điều kiện phase đầu:

- giết boss chính

Khi complete:

1. khóa spawn mới nếu cần
2. khóa combat nếu cần
3. phát reward cuối run
4. gửi kết quả run
5. đẩy player ra ngoài hoặc chuyển sang phase rời map

### 4. Thất bại

Điều kiện fail có thể là:

- hết giờ
- objective fail
- rule riêng của instance

### 5. Hủy instance

Instance bị hủy khi:

- đã complete và hết thời gian giữ map
- đã fail và không còn ai trong instance
- timeout runtime

Ngoài ra cần tách 2 kiểu instance:

#### A. Instance có thời gian đếm ngược

Ví dụ:

- dungeon theo lượt
- challenge map
- phụ bản có đồng hồ đếm ngược

Rule:

- instance có countdown riêng
- khi hết giờ thì destroy runtime
- nếu chưa hết giờ thì dù player tạm rời map, instance vẫn giữ nguyên trạng thái

#### B. Instance farm thuần, không có countdown

Ví dụ:

- map farm riêng
- map train quái
- map instance không gắn objective thời gian

Rule:

- nếu còn player trong map thì instance tiếp tục tồn tại
- nếu không còn ai trong map thì bắt đầu tính idle timeout
- sau một khoảng thời gian không có ai vào lại thì destroy runtime
- nếu có người vào lại trước khi hết idle timeout thì instance tiếp tục dùng trạng thái cũ

## Flow reward

Reward được chốt thành 2 loại lớn.

### A. Reward Drop

Đây là reward rơi ra tại chỗ khi enemy/boss chết.

Flow:

1. boss/enemy chết
2. server roll reward
3. server tạo ground reward runtime tại vị trí chết
4. ground reward có rule sở hữu ban đầu
5. sau một khoảng thời gian:
   - nếu chủ sở hữu không nhặt thì chuyển sang trạng thái free
6. sau thêm một khoảng thời gian nữa:
   - nếu không ai nhặt thì destroy

Rule nhặt:

- khi đang còn ownership:
  - chỉ chủ sở hữu hợp lệ mới nhặt được
- khi đã free:
  - ai gửi request nhặt trước và hợp lệ thì nhặt được

Rule xác định owner là config theo từng boss/enemy:

- có con lấy last hit
- có con lấy top damage
- có con có rule khác

### B. Reward Trực Tiếp

Đây là reward không rơi ra đất.

Flow:

1. enemy/boss chết
2. server xác định player đủ điều kiện
3. server roll reward
4. server cộng thẳng item/currency vào inventory
5. server gửi packet thông báo reward

Loại này phù hợp với:

- reward clear instance
- reward thành tựu/boss đặc biệt
- reward không muốn gây tranh nhặt

## Rule xác định người nhận reward

Vì có 2 loại reward, nên rule nhận reward cũng phải config được theo nội dung.

### 1. Điều kiện đủ tư cách

Ví dụ:

- có contribution
- còn trong map
- không bị loại khỏi event

### 2. Rule chọn chủ sở hữu hoặc người nhận

Ví dụ:

- last hit
- top damage
- tất cả người đủ điều kiện
- nhiều nhóm reward khác nhau cùng tồn tại

Một boss có thể cùng lúc có:

- reward drop cho last hit hoặc top damage
- reward trực tiếp cho tất cả người đủ điều kiện

## Random system và reward flow

Random system chỉ làm:

- từ một bảng reward config, roll ra entry trúng

Gameplay layer sẽ quyết định:

- bảng nào được dùng
- ai được roll
- roll bao nhiêu lần
- entry đó map sang reward thực gì
- reward đó là drop hay direct reward

## Phần thưởng tu vi và tiềm năng theo sát thương

Ngoài reward item/currency, mỗi enemy hoặc boss còn có:

- một lượng tu vi thưởng
- một lượng tiềm năng thưởng

Flow khuyến nghị:

- khi player gây sát thương, server tính phần đóng góp
- từ lượng sát thương đó quy đổi ra phần tu vi/tiềm năng tương ứng
- chỉ cộng nếu player còn khả năng nhận tu vi/tiềm năng

Điểm này nên xem là reward runtime riêng, tách với ground drop.

## Các chốt gameplay đã rõ

### 1. Reward không đi theo một kiểu duy nhất

Chốt:

- có `reward drop`
- có `reward trực tiếp`
- một boss có thể có cả hai loại reward cùng lúc

### 2. Quái thường

Chốt:

- quái thường phase đầu ưu tiên ground drop
- enemy respawn theo timer

### 3. Boss world

Chốt:

- rule nhận thưởng phải config được
- có boss phát cho người đủ điều kiện
- có boss phát cho last hit
- có boss phát cho top contribution

### 4. Instance phase đầu

Chốt:

- chỉ làm solo
- reward phát cuối run

### 5. Enemy/Boss AI phase đầu

Chốt:

- chỉ cần patrol
- phát hiện mục tiêu hoặc bị tấn công thì vào combat
- dùng skill có sẵn theo cooldown
- không cần AI phức tạp

### 6. Player chết lúc boss chết

Chốt:

- không lấy trạng thái sống/chết làm điều kiện cứng
- vì:
  - reward drop vẫn có thể nằm trên đất và theo ownership/free timer
  - reward direct chỉ cần player đủ điều kiện nhận

## Flow khuyến nghị cho phase đầu

### Phase 1

- enemy runtime trên map public
- patrol / detect / combat cơ bản
- contribution tracking cơ bản
- ground drop cơ bản
- reward direct cơ bản
- respawn theo timer

### Phase 2

- boss world
- rule reward theo last hit / top damage / đủ điều kiện
- announcement

### Phase 3

- solo instance
- objective và complete/fail flow
- reward cuối run

### Phase 4

- boss nhiều phase
- event map
- rule reward phức tạp hơn

## Rule đã chốt thêm về player rời instance giữa chừng

### 1. Với instance có countdown

- hết giờ thì destroy
- chưa hết giờ thì vẫn giữ nguyên runtime
- player có thể vào lại nếu hệ gameplay cho phép

### 2. Với instance farm thuần không có countdown

- nếu không còn ai trong map thì không destroy ngay
- bắt đầu tính idle timeout
- nếu hết idle timeout mà vẫn không có ai vào thì destroy
- nếu có người vào lại trước khi timeout thì giữ nguyên instance

## Kết luận

Flow được chốt theo tư duy:

- `Map Template` sinh ra `Map Runtime`
- `Map Runtime` quản lý `Enemy/Boss Runtime`
- `Enemy/Boss Runtime` quản lý combat, aggro, contribution và reward trigger
- reward tách làm `ground drop` và `direct reward`
- random service chỉ làm phần roll
- instance là runtime map có objective và vòng đời riêng

Với flow hiện tại đã chốt, bước tiếp theo có thể là:

- chốt shape config cần có cho map/enemy/boss/reward
- rồi mới bắt đầu thiết kế DB/schema
