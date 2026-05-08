# Hệ Thống Thần Thức — Design Notes

**Ngày tạo:** 2026-05-08  
**Trạng thái:** Đã chốt cơ bản

---

## Concept

Thần Thức là chỉ số của **mọi thực thể sống** trong game:
- Người chơi
- Quái vật / Boss
- Trận Nhãn

Quyết định: **nhìn thấy, tương tác, tấn công** giữa các thực thể.

---

## Chỉ số Thần Thức

Giống HP/MP/ATK/Speed — đã có trong hệ thống stat hiện tại:
- Tăng khi tu luyện và phân bổ tiềm năng
- Tăng thêm từ trang bị (equip)
- Tăng tạm thời từ skill (buff nhất thời)
- Có **thần thức tối đa** và **thần thức hiện tại** (giống HP/MP)

### Hồi Thần Thức
- **Tự hồi theo thời gian** — chậm, luôn xảy ra
- **Item/skill hồi nhanh** — tốc độ phục hồi cao hơn, cần tài nguyên
- Chi tiết tốc độ hồi xác định khi làm balance

### Thần Thức Quái / Boss
- Fixed theo **template** trong DB, không scale theo map
- Map cao có quái thần thức cao → player tàng hình vẫn có thể bị lộ nếu không đầu tư thần thức đủ cao

---

## Rule Nhìn Thấy

**Config toàn server**: ngưỡng X% (ví dụ 40%)

**Rule:**
- Nếu thần thức hiện tại của A **< X% thần thức của B** → A không nhìn thấy B khi B đang ẩn
- B luôn có thần thức lớn hơn X% của A → B luôn nhìn thấy A
- Rule một chiều: kẻ yếu hơn mù trước kẻ mạnh hơn

**Ví dụ** (X = 40%):
- A thần thức 100, B thần thức 249 → 100 > 40% × 249 (≈99.6) → A vẫn nhìn thấy B
- A thần thức 100, B thần thức 251 → 100 < 40% × 251 (≈100.4) → A không nhìn thấy B khi B ẩn

---

## Tàng Hình Chủ Động

- Phải **chủ động bật** — không tự động
- Khi bật: **tiêu hao thần thức hiện tại** liên tục (giống mana drain)
- Thần thức tụt xuống dưới ngưỡng tối thiểu → **tự động tắt**, lộ diện
- Chờ thần thức hồi phục → có thể bật lại
- Tốc độ tiêu hao + ngưỡng tắt → config trong `game_configs`

**Ý nghĩa gameplay:**
- Thần thức cao = ẩn được lâu hơn
- Tu luyện thần thức có giá trị kép: nâng ngưỡng nhìn thấy + kéo dài thời gian ẩn

---

## Cơ Chế Lộ Diện Khi Bị Tấn Công

- Đang tàng hình bị tấn công → **lộ diện trong X giây** (config)
- Trong thời gian lộ diện: tất cả thực thể đều nhìn thấy, tương tác, tấn công được
- Hết X giây: nếu thần thức hiện tại còn đủ → tự động tàng hình lại
- Bị tấn công tiếp khi đang lộ diện → reset timer lộ diện

**Ví dụ:**
- C (thần thức 1000) thấy cả A và B. C tấn công B → B lộ diện → A (vốn không thấy B) giờ nhìn thấy và có thể tấn công. Sau X giây B lại tàng hình trong mắt A nếu còn thần thức
- Kẻ thứ 3 có thể vô tình hoặc cố ý "reveal" target — tạo gameplay phối hợp

---

## Phá Tàng Hình Chủ Động

- Không có skill/item phá tàng hình trực tiếp
- Cách duy nhất: dùng **skill nâng thần thức nhất thời** để vượt ngưỡng X% của đối phương → nhìn thấy
- Không vượt ngưỡng → không thấy dù biết đối phương đang ở đó
- Counter hợp lý: đầu tư thần thức cao → ẩn lâu, ẩn sâu; đối thủ muốn counter phải đầu tư skill/item nâng thần thức

---

## Áp Dụng

### Trận Pháp
- Trận Nhãn có chỉ số thần thức bảo vệ
- Thực thể có thần thức < X% thần thức Trận Nhãn → không nhìn thấy, không tấn công được
- Dùng chung rule X% toàn server

### Phù Lục / Ẩn Trận
- Linh Ẩn Phù và Ẩn Trận cung cấp buff thần thức tạm thời hoặc nâng ngưỡng ẩn
- Dùng chung rule X%
