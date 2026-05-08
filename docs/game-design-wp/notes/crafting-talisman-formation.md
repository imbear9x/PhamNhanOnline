# Luyện Chế Phù Lục & Trận Pháp — Design Notes

**Ngày tạo:** 2026-05-08  
**Trạng thái:** Đã chốt cơ bản — chờ bàn Hệ thống Thần Thức

---

## Bối cảnh

Game có 4 loại luyện chế:
1. Đan dược — **đã làm xong**
2. Pháp khí (vũ khí, trang bị) — công thức cố định, oke
3. **Phù Lục (Bùa chú)** — đã chốt
4. **Trận Pháp** — đã chốt

Alchemy hiện tại đã có foundation: `pill_recipe_inputs` có `success_rate_bonus`, `mutation_bonus_rate`, `is_optional` per input. Phù lục và trận pháp kế thừa pattern này, mở rộng thêm chiều **hiệu quả vật liệu**.

---

## Phù Lục (Bùa Chú) — ĐÃ CHỐT

### Concept
- Item dùng 1 lần, hiệu ứng chỉ lên **người sử dụng**
- Mỗi phù có bộ stats riêng (duration + các chỉ số hiệu ứng)

### Material Quality System
- **Chiều 1 — Tỉ lệ thành công**: base rate + sum(success_rate_bonus từng vật liệu)
- **Chiều 2 — Hiệu quả**: sum(effect_quality_bonus) nhân vào tất cả stats của phù

### Thành công / Thất bại
- Cả hai đều mất vật liệu
- Cancel giữa chừng: refund theo threshold tiến độ (giống đan dược)

### Sử dụng
- Không stack: dùng đè cùng loại → refresh duration + lấy giá trị tốt nhất từng stat
- Ví dụ: Phù A (ngưỡng 100, còn 2 phút) + Phù B (ngưỡng 120, 5 phút) → ngưỡng 120, 5 phút. Nếu Phù B ngưỡng 80 → giữ ngưỡng 100, duration refresh 5 phút

### Mastery
- Không có — chỉ vật liệu quyết định tỉ lệ (giống đan dược, pháp khí, trận pháp)

---

## Trận Pháp — ĐÃ CHỐT

### Concept
- Item dùng 1 lần — đặt xuống là mất
- Tạo **vùng hiệu ứng (AOE entity)** tại vị trí đặt trên map
- Có thời gian hiệu lực — hết thì tan
- Cố định vị trí sau khi đặt, không di chuyển được
- Boss/enemy dùng chung system trận pháp này, không có loại riêng

### Trận Nhãn
- Mỗi trận pháp có **Trận Nhãn** — điểm sống của trận
- Trận Nhãn có **HP** (base + tăng theo vật liệu) và **chỉ số bảo vệ thần thức**
- Thực thể có thần thức **cao hơn** ngưỡng bảo vệ → nhìn thấy, trỏ được, tấn công được
- Thực thể có thần thức **thấp hơn** → không nhìn thấy, không tương tác được
- Trận Nhãn không tự hồi HP
- Trận Nhãn hết HP → trận tan. Hết duration → trận tan. Cả hai đều hỏng hết, không drop gì

### Các loại hiệu ứng trận
- **Damage trận**: gây sát thương liên tục cho thực thể trong vùng
- **Buff trận**: tăng stat cho thực thể trong vùng
- **Ẩn trận**: che giấu thực thể/vật thể trong vùng (player, đồng minh, cửa động phủ...) khỏi thực thể có thần thức thấp hơn ngưỡng trận
- **Debuff/kiểm soát trận**: suy yếu quái, ngăn quái chủ động tấn công...
- Một trận có thể kết hợp nhiều loại hiệu ứng

### Stats của Trận Pháp
| Stat | Mô tả |
|---|---|
| Duration | Thời gian hiệu lực |
| Radius | Bán kính vùng hiệu ứng |
| Cường độ hiệu ứng | Damage/buff/ngưỡng thần thức che giấu... |
| Trận Nhãn HP | Độ bền trước khi bị phá — base + bonus từ vật liệu |
| Trận Nhãn thần thức bảo vệ | Ngưỡng thần thức để nhìn thấy/phá trận |

Tất cả stats bị ảnh hưởng bởi **effect_quality_bonus** từ vật liệu

### Target Rule (config theo từng công thức)
- Chỉ tấn công kẻ địch trong vùng
- Chỉ buff đồng đội trong vùng
- Chỉ tác động lên loại thực thể cụ thể (player / enemy / boss / công trình như động phủ...)
- Có thể kết hợp nhiều rule cùng lúc

### Material Quality System
- Base success rate + sum(success_rate_bonus từng vật liệu)
- effect_quality_bonus → nhân vào tất cả stats (duration, radius, cường độ, Trận Nhãn HP...)
- **Linh thạch là nguyên liệu bắt buộc** — đắt, khó spam

### Giới hạn đặt trận
- Không giới hạn số trận player sở hữu
- Giới hạn theo **ô trên map**: mỗi ô chỉ chứa 1 trận pháp
- Ô đã có trận → không đặt thêm được
- Số ô config trong DB theo từng map/zone — đã có sẵn

### Stack
- Không stack: 2 trận cùng loại đặt cùng vùng → lấy giá trị tốt nhất từng stat, duration refresh (giống phù lục)

---

## Liên quan — Hệ thống Thần Thức
> Sẽ bàn riêng — ảnh hưởng trực tiếp đến Ẩn trận, Linh Ẩn Phù, và cơ chế tàng hình toàn game

- Thần thức là stat riêng của thực thể
- Quyết định khả năng nhìn thấy / tương tác / tấn công thực thể/trận pháp ẩn
- Rule chung: thần thức thấp hơn ngưỡng nhất định → không nhìn thấy nếu đối phương bật ẩn mình
