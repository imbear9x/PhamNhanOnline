# Động Phủ / Công Động Phủ / Cướp Bóc — Design Notes

**Ngày tạo:** 2026-05-08
**Trạng thái:** Đã chốt cơ bản

---

## Thuật ngữ

- **Thần Thức Quan**: ngưỡng thần thức yêu cầu để một thực thể có thể nhìn thấy, tương tác, hoặc tấn công một đối tượng.

---

## 1. Vòng đời Động Phủ

### Động phủ khởi đầu
- Ngay khi tạo account, ai cũng có 1 **động phủ vĩnh viễn** dạng **private home**
- Động phủ này **không ai tấn công được** vì nằm trong map private

### Chuyển sang động phủ thật ngoài thế giới
- Đến một mốc quest nhất định, người chơi nhận **Bản Vẽ Động Phủ**
- Bản vẽ dùng để **mở động phủ** tại vị trí trong thế giới
- Trạng thái quyền mở:
  - **Chưa mở** -> bản vẽ nằm trong kho đồ
  - **Đã mở** -> bản vẽ biến mất
- Muốn mở ở nơi khác -> phải **mua bản vẽ mới từ NPC**
- Bản vẽ **không drop, không giao dịch được**

### Giới hạn
- **1 người = 1 động phủ active** tại một thời điểm

### Thu dọn động phủ
- Người chơi có thể chủ động **thu dọn** bất cứ lúc nào
- Thu dọn xong -> **bản vẽ động phủ hồi lại kho đồ**
- Phải thu dọn cái cũ trước mới được mở cái mới

### Sau khi mở động phủ thật
- Động phủ private ban đầu **biến mất vĩnh viễn**

### Khi cổng động phủ bị phá hoàn toàn
- Động phủ vào **phase sụp đổ trong 1 phút**
- Chủ nhà bị **teleport ra ngoài ngay** khi cổng vỡ
- Nếu chủ vẫn gần map đó có thể **chạy vào lại** trong 1 phút này
- Tất cả mọi người trong map cổng và động phủ vẫn có thể **đánh nhau** trong thời gian này
- Sau 1 phút: tất cả bị **đẩy ra map ngoài** nơi đặt động phủ
- Động phủ biến mất, ô cell trống trở lại
- **Bản vẽ động phủ hồi lại kho đồ** của chủ nhà
- Khi chết mà không có động phủ nào để về -> hồi sinh ngẫu nhiên ở map public

### Nâng cấp
- **Không nâng cấp được**
- Sức mạnh phụ thuộc **cấp bản vẽ** dùng để mở

---

## 2. Mở Động Phủ trên map

- Phải mở tại **ô cell hợp lệ** trong map được config cho phép
- Ô đó phải **chưa có động phủ người khác**
- Khi mở:
  - chiếm 1 ô cell trên map
  - hiển thị **tên động phủ**
  - người đi qua nhìn thấy nếu vượt được **Thần Thức Quan**

### Cấp động phủ / Thần Thức Quan
- Cấp bản vẽ = cấp động phủ
- Cấp động phủ quyết định **Thần Thức Quan** của động phủ
- Người không vượt được Thần Thức Quan -> **không nhìn thấy, không tương tác, không tấn công được**

---

## 3. Cấu trúc Động Phủ bên trong

Các UI/chức năng chỉ mở được khi ở trong động phủ:
- **Mật Thất** — tu luyện
- **Đan Thất** — luyện đan
- **Luyện Khí Thất** — luyện pháp bảo, phù lục, trận pháp
- **Linh Thú Thất** — quản lý / nuôi linh thú
- **Dục Linh Thất** — sinh sản và ấp trứng linh thú
- **Cổng ra Cửa Động Phủ** — đi ra khu map phòng thủ

### Luyện chế / tu luyện trong động phủ
- Tất cả luyện chế và tu luyện **bắt buộc player phải ngồi tại phòng**
- Luyện xong -> sản phẩm **tự vào balo ngay**, không có trạng thái "chưa claim"
- **Ngoại lệ:** ấp trứng và đẻ trứng trong **Dục Linh Thất** -> trứng/sản phẩm nằm trong rương phòng đến khi được lấy

---

## 4. Khách / thăm động phủ

### Điều kiện vào thăm bình thường
- Cần **2 yếu tố đồng thời**:
  - Là **bạn bè** của chủ nhà
  - Được chủ nhà **gửi lời mời**
- Phải **đứng cạnh động phủ** thì chủ mới gửi lời mời được
- Vào chỉ được **đi trong map**, không mở rương hay làm gì được

### Người tấn công thành công
- Sau khi phá cổng vào được bên trong:
  - có thể **mở rương chứa đồ** và cướp tài sản
  - **Trứng và sản phẩm Dục Linh Thất** chưa được lấy -> có thể bị cướp
  - Đồ **đang trong túi player** -> không lấy được trực tiếp

---

## 5. Tài sản / rule cướp bóc

### Có thể bị cướp
- Rương chứa đồ trong động phủ
- Trứng và sản phẩm Dục Linh Thất chưa được lấy

### Không thể bị cướp trực tiếp
- **Đồ đang trong túi player** — chỉ có tỉ lệ rớt nếu player bị giết

### Rủi ro cho người đi cướp
- Mang nhiều đồ thì **tỉ lệ rớt cao hơn khi bị giết**
- Mang ít đồ thì an toàn hơn nhưng yếu hơn
- Đây là tradeoff player tự quyết định
- Tỉ lệ cụ thể xác định ở phase design data / balance

---

## 6. Cửa Động Phủ / Phòng thủ

### Map Cửa Động Phủ có thể có
- **Trận pháp phòng thủ**
- **Linh thú phòng thủ**
- **Cổng động phủ** có HP riêng

### Cổng động phủ hồi HP
- Tự hồi theo thời gian sau khi bị đánh nhưng chưa vỡ
- Không cần item/tài nguyên sửa chữa

### Các loại trận pháp phòng thủ
- **Trận pháp tấn công**
- **Trận pháp tăng sức phòng thủ cho cổng**
- **Trận pháp tăng Thần Thức Quan của động phủ**

### Nhiều người công cùng lúc — Free-for-all
- Nhiều player có thể **cùng lúc** vào map Cửa Động Phủ
- **Không có phe phái, không có đồng minh** trong map này
- Mỗi người tự quyết định ưu tiên: phá trận pháp, linh thú, cổng, hay đánh nhau
- Cổng vỡ -> ai vào trước lấy trước
- Intentional design — tạo drama, cạnh tranh giữa các kẻ cướp với nhau

### Chủ nhà phòng thủ
- Nếu chủ online và đang trong động phủ -> nhận thông báo
- Chọn OK -> chuyển thẳng tới map Cửa Động Phủ để phòng thủ

### Chết khi phòng thủ
- Rơi nhiều item / linh thạch hơn bình thường
- **Không thể hồi sinh** cho tới khi người tấn công rời khỏi toàn bộ map động phủ + cửa động phủ

---

## 7. Giá phải trả khi đi công động phủ

- **Tỉ lệ rớt đồ khi chết**: gấp 2-3 lần bình thường
- **Penalty thọ nguyên khi chết**: nặng gấp 2-3 lần PK thường
- **Cooldown tấn công**: **per player** — sau mỗi lần tấn công phải chờ 1-2 ngày trước khi tấn công động phủ bất kỳ tiếp theo
- Động phủ không có cooldown riêng — cứ đủ điều kiện là có thể bị công
- Hệ số nhân cụ thể xác định ở phase balance

---

## 8. Pet thủ nhà khi động phủ bị phá

- Pet còn sống -> về túi linh thú của chủ
- Pet đã chết -> về túi linh thú của chủ và ngủ hồi phục

---

## 9. Liên quan với hệ thống khác

- **Linh thú**: có thể được để lại thủ nhà
- **Trận pháp**: có thể đặt làm lớp phòng thủ map cổng
- **Death penalty**: chết khi thủ phủ / công phủ có penalty nặng hơn bình thường
- **Thần Thức Quan**: quyết định việc nhìn thấy/tương tác với động phủ
- **Khai thác linh thạch**: tính năng riêng, không diễn ra trong động phủ
- **Practice sessions**: luyện chế xong -> tự vào balo. Dục Linh Thất là exception — trứng/sản phẩm có thể bị cướp
