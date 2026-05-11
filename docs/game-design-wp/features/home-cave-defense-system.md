# Hệ Thống Động Phủ / Công Động Phủ / Cướp Bóc — Feature Draft

**Ngày tạo:** 2026-05-09  
**Nguồn gốc:** Chuẩn hóa từ `notes/home-cave-defense.md`  
**Trạng thái:** Draft đã cấu trúc, chưa phải handoff coder-ready

---

## 1. Goal

Tạo một hệ thống **động phủ cá nhân có thể triển khai ra thế giới**, vừa là căn cứ phát triển của người chơi, vừa là điểm phát sinh PvP bất đối xứng có rủi ro cao.

Hệ này phải tạo được 4 giá trị chính:

1. **Cảm giác sở hữu**: người chơi có một căn cứ riêng để tu luyện, luyện chế, nuôi linh thú.
2. **Rủi ro tài sản có kiểm soát**: đồ để trong động phủ có thể bị cướp, nhưng không phải mất toàn bộ tiến trình.
3. **Drama PvP tự phát**: nhiều người có thể cùng đánh một động phủ và cũng có thể giết lẫn nhau để tranh phần thưởng.
4. **Quyết định chiến lược dài hạn**: đặt động phủ ở đâu, thủ bằng gì, mang gì khi đi cướp, và lúc nào nên thu dọn.

---

## 2. Design Summary

Mỗi người chơi có vòng đời động phủ gồm 2 giai đoạn:

- **Động phủ khởi đầu private**: an toàn tuyệt đối, dùng làm home ban đầu.
- **Động phủ thế giới**: được mở bằng bản vẽ tại một ô hợp lệ trên map, có thể bị người khác phát hiện và tấn công nếu vượt qua ngưỡng **Thần Thức Quan**.

Người chơi chỉ có **1 động phủ active** tại một thời điểm. Khi đã triển khai động phủ ra thế giới, đây trở thành căn cứ chính của họ. Động phủ có phần:

- **Nội thất / phòng chức năng** để tu luyện, luyện chế, quản lý linh thú.
- **Khu cửa động phủ** là map phòng thủ, nơi đặt cổng, trận pháp, linh thú thủ nhà, và nơi diễn ra combat khi bị công.

Nếu người tấn công phá được cổng và vào trong, họ có thể **cướp tài sản lưu trong động phủ**, nhưng không cướp trực tiếp đồ đang nằm trong túi nhân vật.

---

## 3. Feature Scope

### Trong scope
- Vòng đời tạo / mở / thu dọn / bị phá của động phủ
- Quy tắc triển khai động phủ lên map thế giới
- Điều kiện nhìn thấy / tương tác với động phủ qua Thần Thức Quan
- Map cửa động phủ và phòng thủ cơ bản
- Xâm nhập, cướp bóc, đẩy người chơi ra ngoài khi động phủ sụp
- Quy tắc khách được mời vào thăm
- Tương tác với linh thú thủ nhà, trận pháp, death penalty

### Ngoài scope của draft này
- Chỉ số balance cụ thể (HP cổng, thời gian hồi, tỷ lệ rớt chính xác...)
- UI chi tiết từng màn hình phòng chức năng
- Rule chi tiết luyện đan / luyện khí / ấp trứng
- Chi tiết data model/backend
- Anti-abuse và anti-alt-account nâng cao

---

## 4. Core Loop

### Loop của chủ động phủ
1. Nhận hoặc mua **Bản Vẽ Động Phủ**.
2. Chọn vị trí hợp lệ để mở động phủ trên map thế giới.
3. Dùng động phủ làm nơi tu luyện, luyện chế, cất tài sản, nuôi linh thú.
4. Thiết lập phòng thủ bằng cổng, trận pháp, linh thú.
5. Khi bị tấn công, lựa chọn:
   - online phòng thủ
   - chấp nhận rủi ro mất tài sản để trong động phủ
   - thu dọn động phủ trước khi gặp nguy cơ lớn (nếu kịp)
6. Nếu động phủ bị phá hủy hoàn toàn, nhận lại bản vẽ vào kho đồ và chọn vị trí mới để dựng lại.

### Loop của người đi công động phủ
1. Phát hiện được động phủ nếu vượt qua **Thần Thức Quan**.
2. Quyết định có nên tấn công dựa trên rủi ro cao khi chết.
3. Vào map cửa động phủ, đối mặt với:
   - cổng
   - trận pháp
   - linh thú thủ nhà
   - các người chơi khác cũng đang cướp
4. Phá cổng để vào trong.
5. Tranh cướp tài sản với chủ nhà hoặc các kẻ cướp khác.
6. Rút lui an toàn trước khi bị giết và rơi đồ nặng.

---

## 5. Vòng Đời Động Phủ

### 5.1. Động phủ khởi đầu
- Mỗi account khi tạo mới có 1 **động phủ vĩnh viễn dạng private home**.
- Động phủ này nằm trong map private.
- Không ai khác có thể tấn công.
- Đây là điểm hồi sinh/home mặc định ban đầu.

### 5.2. Mở động phủ ra thế giới
- Tại một mốc quest nhất định, người chơi nhận **Bản Vẽ Động Phủ**.
- Khi dùng bản vẽ tại vị trí hợp lệ trên map, người chơi phải trải qua **thời gian dựng / cast time** rồi động phủ mới được mở ra thế giới.
- Việc có cast time giúp hành vi dựng động phủ có trọng lượng hơn, khiến người chơi phải cân nhắc vị trí và trân trọng việc bảo vệ động phủ của mình.
- Sau khi mở thành công:
  - bản vẽ biến mất khỏi kho đồ
  - vị trí đó bị chiếm bởi động phủ
  - động phủ private ban đầu **biến mất vĩnh viễn**

### 5.3. Giới hạn active
- Mỗi người chơi chỉ có **1 động phủ active** tại mọi thời điểm.
- Muốn chuyển vị trí phải **thu dọn động phủ hiện tại trước**.

### 5.4. Thu dọn động phủ
- Chủ nhà có thể chủ động **thu dọn** động phủ khi không ở trạng thái bị tấn công.
- **Không được thu dọn khi đã có người tới tấn công động phủ**.
- Khi thu dọn thành công:
  - động phủ biến mất khỏi map
  - **Bản Vẽ Động Phủ** quay về kho đồ
- Sau đó người chơi có thể mở lại ở vị trí khác.

### 5.5. Khi động phủ bị phá hoàn toàn
- Khi cổng và trạng thái phòng thủ bị phá đến ngưỡng sụp đổ hoàn toàn:
  - động phủ vào **phase sụp đổ trong 1 phút**
  - chủ nhà đang ở bên trong bị **teleport ra khu ngoài ngay lập tức**
  - nếu vẫn còn gần khu đó, chủ nhà có thể chạy vào lại trong 1 phút sụp đổ
  - trong 1 phút này, người ở map cửa động phủ và phần động phủ vẫn có thể tiếp tục giao tranh
- Sau 1 phút:
  - toàn bộ người chơi bị đẩy ra ngoài map đặt động phủ
  - động phủ biến mất
  - ô cell trở về trạng thái trống
  - **Bản Vẽ Động Phủ** trả về kho đồ chủ nhà
- Nếu người chơi chết trong lúc không còn động phủ nào để hồi về, họ hồi sinh ngẫu nhiên ở map public.

---

## 6. Quy Tắc Đặt Động Phủ Trên Map

### Điều kiện đặt
- Chỉ được đặt tại **ô cell hợp lệ** trong các map được config cho phép.
- Ô đó phải **không có động phủ khác**.
- Có thể cần thêm rule cấm đặt gần một số công trình hoặc khu đặc biệt, nhưng chưa chốt ở draft này.

### Hiệu ứng khi đặt
- Động phủ chiếm 1 ô cell trên map.
- Hiển thị **tên động phủ** hoặc định danh tương ứng.
- Người đi ngang chỉ nhìn thấy và tương tác được nếu vượt qua **Thần Thức Quan**.

---

## 7. Thần Thức Quan Và Khả Năng Phát Hiện

### Định nghĩa
**Thần Thức Quan** là ngưỡng yêu cầu để một thực thể có thể nhìn thấy, tương tác hoặc tấn công động phủ.

### Rule cơ bản
- **Cấp bản vẽ = cấp động phủ**.
- **Cấp động phủ quyết định Thần Thức Quan** của động phủ đó.
- Người không vượt qua ngưỡng này sẽ:
  - không nhìn thấy cổng / động phủ
  - không tương tác được
  - không tấn công được

### Ý nghĩa thiết kế
- Đây là lớp phân tầng PvP và tài nguyên theo progression.
- Người chơi cấp thấp không bị buộc tương tác với căn cứ vượt quá tầng phát triển của họ.
- Đồng thời tạo giá trị cho progression liên quan đến thần thức.

---

## 8. Cấu Trúc Bên Trong Động Phủ

Các phòng/chức năng chỉ mở khi người chơi đang ở trong động phủ:

- **Mật Thất** — tu luyện
- **Đan Thất** — luyện đan
- **Luyện Khí Thất** — luyện pháp bảo, phù lục, trận pháp
- **Linh Thú Thất** — quản lý / nuôi linh thú
- **Dục Linh Thất** — sinh sản và ấp trứng linh thú
- **Cổng ra Cửa Động Phủ** — đi ra khu map phòng thủ

### Rule sử dụng phòng
- Mọi hoạt động tu luyện và luyện chế yêu cầu người chơi **phải ngồi đúng tại phòng tương ứng**.
- Khi hoàn thành:
  - sản phẩm đi **thẳng vào balo**
  - không có trạng thái “chưa claim” chung cho mọi loại chế tạo
- **Ngoại lệ:** tại **Dục Linh Thất**, trứng hoặc sản phẩm chưa lấy vẫn nằm lại trong kho/phòng và có thể trở thành tài sản bị cướp.
- **Rương / slot chứa đồ trong động phủ có giới hạn**, và giới hạn này **tăng theo phẩm cấp Bản Vẽ Động Phủ**.

---

## 9. Khách Vào Thăm Động Phủ

### Điều kiện vào thăm thường
Khách chỉ được vào nếu đồng thời thỏa cả 2 điều kiện:

1. Là **bạn bè** của chủ nhà.
2. Được chủ nhà **gửi lời mời**.

### Rule gửi lời mời
- Chủ nhà chỉ có thể gửi lời mời khi khách đang **đứng cạnh động phủ**.
- **Không thể mời bạn vào nhà khi động phủ đang bị công**.
- Khi động phủ đang bị công, người ngoài nếu đi vào khu liên quan thì được xử lý như **người tham gia cuộc công**, không còn là khách tham quan an toàn.

### Quyền của khách được mời
- Được vào tham quan và di chuyển trong map.
- **Không được** mở rương, lấy đồ, hoặc dùng các chức năng quản trị/tài sản của động phủ.

---

## 10. Cửa Động Phủ Và Phòng Thủ

### Thành phần phòng thủ có thể có
Map cửa động phủ có thể chứa:
- **Cổng động phủ** có HP riêng
- **Trận pháp phòng thủ**
- **Linh thú thủ nhà**

### Hồi phục cổng
- Nếu cổng bị đánh nhưng chưa bị phá hẳn, cổng **tự hồi HP theo thời gian**.
- Không yêu cầu vật liệu sửa chữa thủ công.

### Loại trận pháp dự kiến
- Trận pháp tấn công
- Trận pháp tăng thủ cho cổng
- Trận pháp tăng **Thần Thức Quan** của động phủ

### Ý nghĩa thiết kế
- Chủ nhà không chỉ phòng thủ bằng chỉ số nhân vật mà còn bằng chuẩn bị từ trước.
- Kẻ tấn công phải cân nhắc giữa phá cổng nhanh, phá lớp phòng thủ phụ, hay giết đối thủ khác trước.

---

## 11. Tấn Công Động Phủ Và Free-for-All PvP

### Nhiều người có thể cùng công
- Nhiều người chơi có thể cùng lúc vào map cửa động phủ.
- Không có cơ chế chia phe mặc định cho bên công.
- Đây là **free-for-all**: ai cũng có thể đánh ai.

### Hệ quả gameplay
Người tấn công phải tự chọn ưu tiên:
- phá trận pháp
- giết linh thú thủ nhà
- tập trung vào cổng
- giết người chơi khác để giảm cạnh tranh cướp đồ

### Rule chiếm lợi ích
- Khi cổng vỡ và vào được bên trong, **ai vào trước lấy trước**.
- Thiết kế này cố ý tạo tranh chấp giữa các kẻ cướp, không biến nó thành raid PvE thuần túy.

---

## 12. Chủ Nhà Phản Ứng Khi Bị Công

- Khi động phủ bị tấn công, **chủ nhà luôn nhận được thông báo**, kể cả đang online hay offline.
- Nếu chủ nhà online, họ có thể **đi tới map Cửa Động Phủ để phòng thủ hoặc quấy rối bên tấn công**.
- Chủ nhà **không bị ép phải về thủ**; nếu không muốn phòng thủ thì có thể tiếp tục đi nơi khác và làm việc khác.
- Nếu chủ nhà logout trong lúc bị công thì **cuộc công vẫn tiếp tục bình thường**, không bị hủy hay tạm dừng.

### Khi chủ nhà chết lúc phòng thủ
- Chịu penalty nặng hơn chết thường:
  - rơi nhiều item / linh thạch hơn
  - không thể hồi sinh cho đến khi toàn bộ người tấn công rời khỏi khu vực động phủ + cửa động phủ

### Nếu chủ nhà offline trong lúc động phủ bị phá
- Cuộc công vẫn diễn ra đầy đủ như bình thường.
- Nếu động phủ bị phá hoàn toàn trong lúc chủ nhà offline, thì khi chủ nhà login lại:
  - không còn xuất hiện trong động phủ cũ nữa
  - được xử lý như trạng thái **mất nhà / không còn động phủ đang tồn tại**
  - có thể bị đưa ra một **map public ngẫu nhiên** theo rule hồi vị trí an toàn tạm thời
- Mục tiêu là tránh việc logout trở thành cách né hậu quả bị công phá.

### Ý nghĩa thiết kế
- Chủ nhà luôn biết động phủ đang gặp nguy hiểm, kể cả khi không ở trong game loop hiện tại.
- Chủ nhà có quyền chọn mức độ can thiệp: về thủ thật sự, chỉ quấy rối để làm chậm, hoặc bỏ mặc để giảm tổn thất cá nhân.
- Logout không phải công cụ dừng hay né cuộc công.
- Việc không được hồi sinh ngay ngăn loop thủ vô hạn.

---

## 13. Tài Sản Có Thể Bị Cướp

### Có thể bị cướp
- Rương chứa đồ trong động phủ
- Trứng và sản phẩm tại **Dục Linh Thất** chưa được lấy ra

### Không thể bị cướp trực tiếp
- Đồ đang nằm trong túi người chơi

### Nhưng vẫn có rủi ro gián tiếp
- Nếu người chơi bị giết, đồ trong túi vẫn có thể rơi theo **death penalty** chung.

### Ý đồ thiết kế
- Khuyến khích người chơi cân nhắc tồn kho trong động phủ.
- Không biến một lần thua thành mất sạch mọi thứ đang mang trên người một cách vô điều kiện.

---

## 14. Giá Phải Trả Khi Đi Công Động Phủ

Người đi công phải chịu rủi ro cao hơn PvP thường, đồng thời phải trả **chi phí khởi tạo cuộc công kích**.

### Bùa Phá Phủ
- Muốn tấn công động phủ, người chơi cần 1 item tên là **Bùa Phá Phủ**.
- **Bùa Phá Phủ** được mua bằng **linh thạch**.
- Mỗi lần phát động tấn công tiêu hao 1 Bùa Phá Phủ.
- **Phẩm cấp Bùa Phá Phủ** tương ứng với **phẩm cấp động phủ** có thể tấn công.
- Muốn công động phủ phẩm cấp nào thì phải dùng đúng Bùa Phá Phủ của phẩm cấp đó.

### Penalty dự kiến
- **Tỷ lệ rớt đồ khi chết**: nặng hơn bình thường, khoảng 2–3 lần PvP thường
- **Penalty thọ nguyên khi chết**: nặng hơn PK thường, khoảng 2–3 lần
- **Cooldown tấn công động phủ**: theo từng người chơi, sau mỗi lần tấn công phải chờ 1–2 ngày mới được tấn công tiếp bất kỳ động phủ nào

### Rule cooldown
- Cooldown là **theo người chơi đi công**, không phải theo động phủ bị công.
- Động phủ không có cooldown miễn công mặc định; nếu đủ điều kiện thì vẫn có thể tiếp tục bị người khác công.

### Đền bù cho động phủ bị công
- Mỗi lần động phủ bị phát động tấn công hợp lệ, chủ động phủ **luôn nhận được một khoản linh thạch đền bù**.
- Khoản đền bù này được nhận **dù động phủ có bị phá hay không**.
- Khoản đền bù được **trích trực tiếp từ giá mua Bùa Phá Phủ**, không phải hệ thống tự sinh thêm.
- Ví dụ định hướng: nếu Bùa Phá Phủ giá **10 linh thạch** thì chủ động phủ có thể nhận **3–5 linh thạch** đền bù.
- Phần còn lại trở thành **khoản hút tiền của game**.
- Mục đích là:
  - an ủi chủ nhà khi bị quấy rối
  - tạo chi phí thật cho bên công
  - giảm động cơ chain attack chỉ để phá rối
  - bổ sung cơ chế sink linh thạch

### Ý nghĩa thiết kế
- Chặn việc spam công liên tục bằng tài khoản chính.
- Tạo thêm một lớp chi phí kinh tế trước khi vào combat thật.
- Buộc người chơi chỉ chọn các mục tiêu đáng để mạo hiểm.
- Biến hành vi quấy rối thành hành vi tốn tài nguyên.
- Tạo cơ chế **hút tiền** thay vì bơm thêm linh thạch vào nền kinh tế.

---

## 15. Linh Thú Thủ Nhà Khi Động Phủ Bị Phá

- Nếu linh thú còn sống khi động phủ bị phá: quay về **túi linh thú** của chủ nhà.
- Nếu linh thú đã chết: vẫn quay về túi linh thú nhưng ở trạng thái **ngủ hồi phục**.

Điểm này giúp chủ nhà không mất vĩnh viễn pet thủ nhà chỉ vì một trận thủ thất bại.

---

## 16. State Machine Động Phủ

Mục tiêu của phần này là chốt **trạng thái hệ thống** của động phủ để khi sang phase requirement hoặc implementation không bị hiểu lệch flow.

### 16.1. Các trạng thái chính

#### A. Private Home Khởi Đầu
- Trạng thái mặc định của account mới.
- Nằm trong map private.
- Không thể bị người khác phát hiện hay tấn công.
- Dùng làm home ban đầu.

#### B. Động Phủ Thế Giới — Bình Thường
- Động phủ đã được mở trên một ô hợp lệ ngoài thế giới.
- Có thể bị phát hiện nếu người khác vượt **Thần Thức Quan**.
- Chủ nhà có thể ra vào, sử dụng phòng chức năng, cất tài sản, bố trí phòng thủ.
- Có thể thu dọn nếu chưa bị tấn công.

#### C. Động Phủ Đang Bị Công
- Bắt đầu từ lúc có người phát động tấn công hợp lệ bằng **Bùa Phá Phủ**.
- Trong trạng thái này:
  - động phủ bị khóa hành vi **thu dọn**
  - chủ nhà nhận thông báo bị công
  - map Cửa Động Phủ trở thành khu PvP active liên quan đến cuộc công
- Trạng thái này kéo dài cho đến khi cuộc công kết thúc theo điều kiện kết thúc tương ứng.

#### D. Phase Sụp Đổ
- Kích hoạt khi cổng/phòng thủ bị phá đến ngưỡng động phủ sụp hoàn toàn.
- Kéo dài **1 phút**.
- Trong thời gian này:
  - chủ nhà bên trong bị đẩy ra ngoài ngay khi bắt đầu phase
  - mọi người vẫn có thể vào
  - còn gì thì nhặt
  - thích đánh ai thì đánh
- Đây là trạng thái “cướp vét và hỗn chiến cuối”.

#### E. Đã Biến Mất / Trả Bản Vẽ
- Kết thúc sau phase sụp đổ.
- Động phủ biến mất khỏi map.
- Ô cell trở lại trạng thái trống.
- **Bản Vẽ Động Phủ** quay về kho đồ chủ nhà.
- Người chơi phải mở lại ở vị trí mới nếu muốn tiếp tục có động phủ ngoài thế giới.

### 16.2. Chuyển trạng thái

- **Private Home Khởi Đầu** -> **Động Phủ Thế Giới — Bình Thường**  
  Khi người chơi dùng Bản Vẽ Động Phủ tại vị trí hợp lệ.

- **Động Phủ Thế Giới — Bình Thường** -> **Động Phủ Đang Bị Công**  
  Khi có một lượt tấn công hợp lệ được kích hoạt bằng Bùa Phá Phủ.

- **Động Phủ Đang Bị Công** -> **Động Phủ Thế Giới — Bình Thường**  
  Khi cuộc công kết thúc mà động phủ không bị phá hoàn toàn.

- **Động Phủ Đang Bị Công** -> **Phase Sụp Đổ**  
  Khi cổng/phòng thủ bị phá tới ngưỡng sụp hoàn toàn.

- **Phase Sụp Đổ** -> **Đã Biến Mất / Trả Bản Vẽ**  
  Khi hết 1 phút phase sụp đổ.

- **Động Phủ Thế Giới — Bình Thường** -> **Đã Biến Mất / Trả Bản Vẽ**  
  Khi chủ nhà chủ động thu dọn hợp lệ.

### 16.3. Điều chưa khóa ở level state machine
- Điều kiện kỹ thuật chính xác để xác định một cuộc công “đã kết thúc” nếu động phủ không bị phá.
- Có cần trạng thái trung gian kiểu “đang mở / đang dựng động phủ” hay không.
- Có cần trạng thái cooldown riêng cho động phủ hay chỉ cooldown theo người đi công.

## 17. Luồng Tấn Công Động Phủ

Phần này mô tả **attack flow ở cấp hệ thống**, chưa đi vào số balance cụ thể.

### 17.1. Điều kiện mở một cuộc công
Người chơi muốn công động phủ phải thỏa các điều kiện cơ bản:

1. Nhìn thấy / xác định được động phủ mục tiêu thông qua rule **Thần Thức Quan**.
2. Có **Bùa Phá Phủ** đúng phẩm cấp tương ứng với động phủ mục tiêu.
3. Không nằm trong cooldown cấm tấn công động phủ.
4. Động phủ mục tiêu đang tồn tại ở trạng thái có thể bị công.

### 17.2. Khi phát động tấn công thành công
Ngay khi lượt công được kích hoạt hợp lệ, tức là **ngay lúc dùng Bùa Phá Phủ thành công**:

- tiêu hao **1 Bùa Phá Phủ**
- bắt đầu tính **thời gian hiệu lực của Bùa Phá Phủ**
- động phủ mục tiêu chuyển sang trạng thái **Đang Bị Công**
- chủ nhà nhận **thông báo bị công**
- chủ nhà được nhận **linh thạch đền bù**
- động phủ bị khóa hành vi **thu dọn**
- map **Cửa Động Phủ** bước vào trạng thái tranh chấp active

### 17.3. Giai đoạn công cổng
Bên tấn công tiến vào map Cửa Động Phủ và có thể:

- đánh cổng
- phá trận pháp
- đánh linh thú thủ nhà
- đánh các người chơi khác cũng đang ở đó
- chặn hoặc quấy rối chủ nhà nếu chủ nhà quay lại thủ

Đây là giai đoạn combat hỗn hợp, không phải một bài kiểm tra DPS đơn thuần lên cổng.

### 17.4. Nếu bên công thất bại hoặc rút lui
Cuộc công được xem là thất bại nếu **không phá được động phủ trước khi thời gian hiệu lực của Bùa Phá Phủ kết thúc**.

Kết quả hệ thống ở mức logic:
- động phủ quay về trạng thái **Bình Thường**
- cổng có thể hồi lại theo rule hồi phục
- chủ nhà vẫn giữ khoản **linh thạch đền bù** đã nhận
- người đi công vẫn mất **Bùa Phá Phủ** và chịu cooldown / rủi ro đã phát sinh

### 17.5. Vai trò người mở công và người tham gia
- **Người mở công** là người trực tiếp dùng **Bùa Phá Phủ** để kích hoạt cuộc công.
- Hành vi này dùng để ghi nhận ai là người phát động chính của lượt công.
- Những người **đi vào map Cửa Động Phủ** trong thời gian cuộc công đang còn hiệu lực được tính là **người tham gia**.
- Người tham gia không nhất thiết là đồng minh của người mở công; họ vẫn có thể tự do PvP với nhau theo rule free-for-all.
- Khi đã vào khu công động phủ, mỗi người tự quyết định hành vi của mình: đánh cổng, đánh chủ nhà, đánh kẻ cướp khác, hoặc thậm chí quay sang đánh chính người mở công.

### 17.6. Nếu bên công phá được động phủ
Khi vượt qua được ngưỡng phòng thủ cuối cùng:

- động phủ chuyển sang **Phase Sụp Đổ** kéo dài 1 phút
- chủ nhà trong động phủ bị đẩy ra ngoài ngay
- người chơi có thể lao vào nhặt tài sản còn sót
- tất cả tiếp tục tự do PvP với nhau trong thời gian còn lại

### 17.7. Kết thúc cuộc công bằng sụp đổ hoàn toàn
Khi hết 1 phút phase sụp đổ:

- tất cả người chơi bị đẩy ra ngoài
- động phủ biến mất
- ô cell giải phóng
- Bản Vẽ Động Phủ trả về kho đồ chủ nhà

### 17.8. Các mốc cần rõ khi lên requirement
- **Thời gian hiệu lực mặc định tạm thời** của Bùa Phá Phủ là **15 phút**.
- Thời gian này hiện chỉ là mốc system design / ví dụ vận hành, chưa khóa balance cuối cùng.
- Nếu Bùa Phá Phủ hết thời gian đúng lúc đang giao tranh tại cổng thì ưu tiên xử lý trạng thái thế nào?
- Có giới hạn số người tham gia tối đa trong map Cửa Động Phủ hay không?
- Ghi nhận phần thưởng, log chiến đấu, hay thống kê công thủ sẽ dựa theo người mở công hay toàn bộ người tham gia?


## 18. Liên Kết Với Hệ Khác

### Linh thú
- Có thể bố trí làm thủ vệ cho động phủ.
- Cần đồng bộ với spec hệ linh thú sau này.

### Trận pháp
- Có thể dùng làm lớp phòng thủ map cửa động phủ.
- Có ít nhất 3 vai trò: tấn công, thủ cổng, tăng ẩn/Thần Thức Quan.

### Death penalty
- Chết khi thủ động phủ hoặc đi công động phủ nặng hơn bình thường.
- Cần đồng bộ với `notes/death-penalty.md` để tránh lệch logic PK thường.

### Thần thức
- Quyết định việc nhìn thấy, tương tác và tấn công động phủ.
- Cần nối tiếp với spec của hệ thần thức.

### Khai thác linh thạch
- Không diễn ra trong động phủ.
- Là feature riêng, không gộp vào hệ này.

---

## 19. Key Decisions Already Locked

1. Mỗi account có 1 động phủ private ban đầu.
2. Khi mở động phủ thế giới, động phủ private ban đầu biến mất vĩnh viễn.
3. Mỗi người chỉ có 1 động phủ active tại một thời điểm.
4. Chủ nhà có thể thu dọn để lấy lại bản vẽ, **nhưng không được thu dọn khi đã có người tới tấn công**.
5. Nếu động phủ bị phá hoàn toàn, bản vẽ quay lại kho đồ chủ nhà.
6. Động phủ không nâng cấp tại chỗ; sức mạnh phụ thuộc vào **cấp bản vẽ**.
7. Dựng động phủ có **cast time**, không phải thao tác tức thời.
8. Chỉ người vượt **Thần Thức Quan** mới thấy và công được động phủ.
9. Người được mời vào thăm chỉ đi lại, không được đụng tài sản.
10. Tài sản bị cướp chủ yếu là đồ để trong động phủ, không phải đồ đang nằm trong túi.
11. Rương / slot chứa đồ trong động phủ có giới hạn và tăng theo **phẩm cấp Bản Vẽ Động Phủ**.
12. Công động phủ là PvP free-for-all, không tự động liên minh giữa bên công.
13. Người đi công chịu penalty chết nặng hơn bình thường.
14. Muốn công động phủ phải dùng **Bùa Phá Phủ** mua bằng linh thạch.
15. **Phẩm cấp Bùa Phá Phủ** tương ứng với **phẩm cấp động phủ** mục tiêu.
16. Động phủ bị công luôn nhận **linh thạch đền bù**, dù có bị phá hay không.
17. Linh thạch đền bù được **trích từ giá Bùa Phá Phủ**, phần còn lại là cơ chế hút tiền của game.
18. Chủ nhà luôn nhận được thông báo khi động phủ bị công, kể cả online hay offline.
19. Logout của chủ nhà không làm hủy, dừng, hay hoãn cuộc công.
20. Không thể mời bạn vào nhà khi động phủ đang bị công; người vào khu này được tính là người tham gia cuộc công.
21. Trong 1 phút phase sụp đổ, ai cũng có thể vào, nhặt những gì còn lại, và đánh bất kỳ ai.
22. Thời gian hiệu lực tạm thời của **Bùa Phá Phủ** là **15 phút** ở phase system design.
23. Chủ nhà chết khi thủ nhà thì không được hồi sinh ngay trong lúc đối phương còn ở đó.

---

## 20. Open Questions

### Cần chốt để tiến lên requirement
- [ ] Cast time dựng động phủ cụ thể là bao lâu?
- [ ] Công thức số lượng rương / slot chứa tăng theo từng **phẩm cấp Bản Vẽ Động Phủ** như thế nào?
- [ ] Có cho người chơi đặt động phủ ở mọi map hợp lệ hay cần phân tầng theo khu vực / cấp map?
- [ ] Người không đủ Thần Thức Quan có hoàn toàn không thấy gì, hay có cần hiệu ứng mơ hồ nào không?
- [ ] Công thức giá của từng **phẩm cấp Bùa Phá Phủ** là bao nhiêu?
- [ ] Tỷ lệ hoặc khoảng **linh thạch đền bù** theo từng phẩm cấp là bao nhiêu? (ví dụ 30%–50% giá bùa)
- [ ] Sau này có giữ 15 phút cho mọi phẩm cấp bùa hay tách thời lượng theo phẩm cấp?

---

## 21. Recommended Next Step

Có 2 hướng hợp lý tiếp theo:

1. **Tiếp tục chốt các open question còn lại**, nhất là:
   - cast time cụ thể
   - công thức slot theo phẩm cấp
   - phạm vi map được đặt động phủ
   - giá bùa / tỷ lệ đền bù

2. Sau khi chốt, nâng tài liệu này thành **requirement coder-ready** với:
   - player-facing behavior
   - state machine rõ ràng
   - acceptance criteria
   - edge cases / exploit cases
