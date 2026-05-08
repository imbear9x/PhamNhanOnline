# Hệ Thống Linh Thú — Design Notes

**Ngày tạo:** 2026-05-08  
**Trạng thái:** Đã chốt concept/gameplay core, còn data/balance/runtime detail

---

## Vai trò chính

Linh thú đi theo hướng:
- **Combat pet**
- **Utility pet**

Không phải mount là trọng tâm hiện tại.

---

## Bản chất hệ thống

- Linh thú là **một dạng item entity / thực thể thật** trong game
- Nó có các chỉ số giống player ở mức hệ thống: HP, mana, thần thức, speed, v.v.
- Về combat logic, pet gần với **enemy đồng minh**: có target, có skill, có runtime state
- Vì là thực thể thật, linh thú có thể chiến đấu, bị đánh, cạn tài nguyên, và bị hạ gục
- Pet **không drop ra đất**
- Nếu chủ nhân **từ bỏ pet** thì pet **biến mất**

---

## Quyền sở hữu / nguồn pet

### Quyền sở hữu
- Pet là entity thuộc sở hữu player
- Chỉ **pet cấp thấp** mới được giao dịch / đổi chủ
- **Pet cấp cao không được giao dịch**
- Ngưỡng “cấp thấp” được xác định theo **cảnh giới** của pet

### Nguồn pet
Pet có thể đến từ các nguồn:
1. **Nở từ trứng**
   - Trứng có thể mua từ NPC hoặc từ người chơi khác
2. **Thu phục pet vô chủ ngoài map**
   - Thi thoảng map xuất hiện ngẫu nhiên linh thú vô chủ
   - Người chơi bấm vào pet và chọn **Thu Phục**
   - Pet bị **khóa theo thứ tự người tương tác trước**
   - Người đến sau sẽ thấy trạng thái kiểu **đang bận / đang tranh đấu**
   - Sau đó người chơi và pet vào một **không gian/phiên chiến đấu riêng** có đếm ngược thời gian để thu phục
   - Đánh thắng → thu phục được
   - Đánh thua → pet hồi full máu, không mất
3. **Phần thưởng nhiệm vụ**
4. **Boss drop trứng**
   - Boss không drop pet trực tiếp, chỉ drop trứng

---

## Sinh sản / trứng / ấp trứng

### Rule sinh sản
- Chỉ cần **2 pet cùng loài**, không cần giới tính
- Không được phép **cận huyết**
- Mỗi pet có **1 lineage field** dùng để check cận huyết
- Pet nào có cùng lineage id thì **không được sinh sản với nhau**
- Có thể cho 2 pet phù hợp vào **phòng sinh sản**
- Sau một thời gian sẽ sinh ra **trứng pet**
- **Số lượng trứng** random trong khoảng được config

### Trứng pet
- Trứng pet có **thời gian ấp** và **tỉ lệ nở** nhất định
- Có **tỉ lệ hỏng / trượt**
- Người chơi cần vào **phòng linh thú** để thực hiện ấp trứng
- Có một số yếu tố/item/cơ chế giúp **giảm thời gian ấp**

### Kế thừa phẩm chất
- Pet con **inherit từ phẩm chất của 2 bố mẹ** nhưng theo random range
- Ví dụ: bố phẩm chất thượng cấp, mẹ trung cấp → pet con random trong khoảng **trung → thượng**
- Tỉ lệ nghiêng về phần **giữa trở lên**
- Công thức chi tiết sẽ làm ở phase **design game data / balance**

### Model / ngoại hình
- Model của pet vẫn phụ thuộc vào **giống loài** và **tu vi/cảnh giới**
- Không inherit model kiểu ngẫu nhiên từ bố mẹ

---

## Triệu hồi và mang theo

- Linh thú nằm trong **túi linh thú** của người chơi
- Khi cần, người chơi **triệu hồi** linh thú ra ngoài
- Tại một thời điểm chỉ được triệu hồi tối đa **2 linh thú**
- Có thể triệu hồi 2 linh thú bất kỳ, không bị giới hạn role
- Khi không có mục tiêu địch phù hợp, linh thú sẽ **đi theo người chơi**

### Cost / hạn chế spam
- **Triệu hồi**: tốn một lượng **mana của player**
- **Duy trì tồn tại khi đang được triệu hồi**: tốn **thần thức liên tục**
- Không cần phải đang chiến đấu mới hao — **chỉ cần đang được triệu hồi là hao liên tục**
- **Mỗi pet hao một lượng riêng** → gọi 2 pet thì hao mạnh hơn gọi 1 pet
- Vì điều khiển pet đã tốn thần thức nên pet không cần thêm một lớp chi phí điều khiển phức tạp khác
- Mục tiêu design:
  - người chơi không thể vừa spam skill vừa spam linh thú cùng lúc
  - không thể triệu hồi pet chỉ để “lòe thiên hạ” rồi dắt đi chơi quá lâu gây loạn map

### Giới hạn hiển thị / mật độ map
- Pet **luôn hiện với mọi player** trong map
- Mỗi pet được tính như **1 player entity** cho giới hạn density/map population
- Nếu map đã vượt số lượng tối đa cho phép → **không cho phép triệu hồi thêm pet**

---

## AI ưu tiên mục tiêu

Khi có mục tiêu địch, linh thú ưu tiên theo thứ tự:
1. **Kẻ địch gần nhất đang tấn công người chơi**
2. **Kẻ địch gần nhất đang tấn công chính linh thú đó**
3. **Kẻ địch mà người chơi đang target / tấn công**

Nếu không có target phù hợp → quay lại follow player.

---

## Combat / Skill

- Pet có **skill riêng**
- Combat flow của pet gần giống enemy: auto target + dùng skill theo AI/runtime rule
- Pet cũng có **mana riêng**
- Tuy nhiên mana của pet **không phải điểm quản lý chính** cho player:
  - không thiết kế theo hướng bắt player phải để ý và micromanage mana pet liên tục
  - thực tế pet hiếm khi rơi vào tình trạng hết mana hoàn toàn
- Tension chính vẫn nằm ở:
  - mana của player khi triệu hồi
  - thần thức của player để duy trì triệu hồi

### Cấu hình skill pet
- Pet **không chia role cứng** kiểu tanker / damage / support / utility
- Tính chất pet được quyết định bởi **bộ skill được cấu hình sẵn**
- Người chơi **không đổi cấu hình skill pet như player**
- Ở mỗi cảnh giới / trạng thái phát triển tương ứng, pet chỉ có **bộ skill được định sẵn**
- Pet được cấu hình bao nhiêu skill thì dùng bấy nhiêu

### Rule dùng skill
- Pet cast skill theo **điều kiện ưu tiên**
- Thứ tự ưu tiên cơ bản:
  1. **Skill đỡ đòn** trước — nếu player bật cơ chế chủ động đỡ đòn
  2. **Skill buff**
  3. **Skill attack**
- Nếu có 2 skill cùng loại → **random**

### Che chắn cho chủ
- Che chắn **không phải skill riêng**, mà là **behavior/action** của pet
- Khi pet đang được triệu hồi, còn sống, và player bị nhắm tới bởi một skill:
  - pet sẽ **tele chắn trước mặt player**
  - pet **nhận thay** skill đó
- Nếu trong lúc tele pet có thể dùng skill, và pet có skill type **đỡ đòn**, thì nó dùng skill đó luôn

---

## Thần thức / tàng hình / PvP

- Pet cũng có **thần thức** và bị ảnh hưởng bởi rule thần thức như thực thể khác
- Pet **không có nút tàng hình chủ động** như player
- Khi chiến đấu, pet có thể **tự chủ động tàng hình** theo behavior của nó
- Tuy nhiên thần thức pet thường thấp, nên hầu hết trường hợp vẫn bị người khác nhìn thấy
- Pet vẫn có thể **tấn công mục tiêu nếu chủ nhìn thấy** vì bản thân việc điều khiển pet đã tiêu hao thần thức từ chủ

### Trong PvP
- **Duel**: pet tham chiến bình thường
- **PvP Zone**: pet tấn công player đối phương theo rule ưu tiên target đã định

---

## Trạng thái bị hạ gục / thu hồi

- Nếu linh thú **hết HP** hoặc **hết thần thức duy trì từ chủ** → nó sẽ **về lại túi linh thú**
- Sau khi về túi, linh thú rơi vào trạng thái **ngủ / hồi phục**
- Cần **cooldown hồi phục** trước khi có thể triệu hồi lại

### Giá phải trả khi pet chết
- Nếu linh thú bị hạ gục trong combat → chịu penalty nặng hơn chỉ là cooldown
- **Giảm tu vi của pet theo %**
- Áp dụng giống nhau cho mọi pet
- Chết PvE hay chết khi thủ động phủ → cùng rule mất tu vi như nhau
- Sau khi hồi phục xong, pet phải **tu luyện lại** để lấy lại phần tu vi đã mất

### Thọ nguyên của pet
- Pet có **thọ nguyên** theo cảnh giới tương tự player
- Nhưng thọ nguyên của pet/enemy thường **gấp 5-10 lần player cùng cảnh giới**
- Nếu pet hết thọ nguyên:
  - đang trong túi → **biến mất**, chủ nhận thông báo
  - đang triệu hồi → **biến mất**
  - đang thủ nhà → **biến mất**
- Tức là runtime xử lý như nhau ở mọi trạng thái: hết thọ nguyên = xóa pet
- Lên tới **Hóa Thần** thì gần như bất tử

---

## Khả năng đặc biệt / utility

Linh thú có thể có các khả năng riêng như:
- **Tự động che chắn cho player**
  - Có thể **bật / tắt** trong cấu hình linh thú
- **Tự động nhặt đồ**
- **Bảo vệ động phủ**

### Auto loot
- Tạm thời: **nhặt tất cả**
- Nếu balo của player đầy → **dừng nhặt**
- Vẫn tuân theo **quyền sở hữu drop**
- Nếu 2 pet cùng gần 1 drop và cùng có chức năng nhặt → chỉ cần 1 pet nhặt được, item vẫn về tay chủ nhân
- Player đang trade / PK vẫn nhặt bình thường, chỉ refresh balo
- Nếu player chết → pet **không nhặt**, chỉ đứng yên chờ player hồi sinh hoặc biến về. Hồi sinh xong mới nhặt tiếp

### Bảo vệ động phủ
- Nếu pet được **đặt ở vị trí bảo vệ động phủ** thì nó sẽ ở lại động phủ
- Pet đang thủ động phủ **không thể triệu hồi mang theo** trừ khi thu hồi nó về túi linh thú trước
- Khi động phủ bị tấn công, pet thủ nhà xuất hiện ở **map cổng động phủ** như một phần hệ phòng thủ
- Pet thủ nhà chết → về **túi linh thú của chủ**
- Nếu chủ offline và pet chết khi thủ nhà:
  - **TH1:** người tấn công bỏ đi, không phá cổng → pet hồi sinh sau một thời gian để thủ tiếp
  - **TH2:** người tấn công phá cổng, vào động phủ cướp đồ → pet về túi của chủ và hồi sinh trong túi sau một thời gian

---

## Tăng trưởng / tu luyện pet

Pet có thể tăng trưởng bằng nhiều nguồn kết hợp:
- **Cùng người chơi farm quái / tham chiến**
- **Ăn đan dược**
- **Tự tu luyện** trong túi linh thú
- **Tự tu luyện trong động phủ**

=> Hướng hiện tại: kết hợp cả 3, không khóa pet vào một nguồn tăng trưởng duy nhất.

### Phẩm chất / tiềm năng / chỉ số
- Pet có **phẩm chất** (thiên phú tu luyện, tốc độ hấp thụ linh khí...)
- Khi tu luyện, pet cũng có **tiềm năng** và **tu vi** như player
- Tiềm năng của pet sẽ được **tự động phân bổ ngẫu nhiên theo trọng số config**
  - Ví dụ: dòng pet này có 30% ưu tiên tăng một chỉ số nào đó
- 2 pet cùng loại vẫn có thể khác nhau về **tu vi** và **chỉ số**

### Cảnh giới / điều kiện điều khiển
- Pet có progression/cảnh giới riêng giống enemy
- Player **không cần cảnh giới cao hơn pet** để sở hữu pet mạnh
- Nhưng pet có chỉ số **thần thức yêu cầu để điều khiển / triệu hồi**
- Nếu thần thức của player không đủ ngưỡng này → **không thể triệu hồi pet**
- Pet tăng cảnh giới có thể **đổi model** và **mở skill mới**

---

## UI / không gian sử dụng

- **Túi linh thú**: mở được mọi lúc, là UI tương tự inventory
- **Phòng linh thú**: là UI, chỉ mở được trong **map động phủ**
- **Phòng sinh sản**: là UI riêng, chỉ mở được trong **map động phủ**
- Các màn hình được **tách riêng**, không gộp một chỗ

---

## Còn cần thảo luận
- [ ] % tu vi pet bị mất khi chết — để phase balance/data
- [ ] Rule số lượng trứng khi sinh sản, tỉ lệ nở cụ thể, và item giảm thời gian ấp
- [ ] UI flow chi tiết cho thu phục / ấp trứng / phòng linh thú
