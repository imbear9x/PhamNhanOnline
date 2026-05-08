# Death Penalty — Design Notes

**Ngày tạo:** 2026-05-08  
**Trạng thái:** Đang bàn

---

## Nguyên tắc chung

- Penalty áp dụng **bất kể chết vì lý do gì** (PvE, Duel, PvP Zone...)
- Chết PvE / Duel / PvP Zone: penalty giống nhau hoàn toàn, không phân biệt
- **Không mất tu vi, không mất tiềm năng**
- Chỉ rơi đồ trên người + ảnh hưởng thọ nguyên/lôi kiếp nếu chết do PK

---

## Drop khi chết

### Linh Thạch
- Có tỷ lệ rớt riêng
- Nếu trúng: rơi **X% số linh thạch đang cầm**, X là random
- Có ngưỡng: **tối thiểu 1 viên, tối đa 10 viên** (tạm thời, config-driven)
- Các thông số đặt trong `game_configs`

### Item khác
- Có tỷ lệ rớt riêng (ví dụ tạm thời 5%)
- Nếu trúng: **random 1 item** trong số những đồ có thể drop được của player (item có flag droppable)
- Item non-tradable / non-droppable → **không rơi**
- Tỷ lệ cũng đặt trong `game_configs`

### Quyền nhặt đồ rơi
- Đồ rơi thuộc quyền của **player chết** trong một khoảng thời gian nhất định
- Sau thời gian đó mới mở cho người khác nhặt — giống cơ chế ground reward hiện có
- Thời gian bảo lưu đặt trong `game_configs`

---

## Buff khi chết

- **Mất buff từ skill** (buff chiến đấu, thiết giáp...)
- **Giữ buff từ bùa chú / trận pháp** — thứ bên ngoài nhân vật, không phải trạng thái nội tại

---

## Hồi sinh

Khi chết, player chọn:
- **Về Động Phủ (home)** — luôn luôn có
- **Về Checkpoint** — nếu map hiện tại có checkpoint và cho phép
- Map thường **không có checkpoint**. Checkpoint chỉ có trong dungeon/phó bản.

> Liên quan PvP: hồi sinh ở đâu quyết định có tiếp tục ở trong PvP Zone không — xem `player-interaction-group.md`

---

## Thọ Nguyên và Lôi Kiếp — penalty đặc biệt khi chết do PK

> Chết do **bất kỳ nguyên nhân** thì rơi đồ như nhau.  
> Nhưng chết do **PK** sẽ có thêm penalty vào thời gian đếm ngược thọ nguyên / lôi kiếp.

### Cơ chế Thọ Nguyên — đột phá cộng thêm phần chênh lệch

- Mỗi cảnh giới có **pool thọ nguyên riêng** (config theo cảnh giới)
- Khi đột phá → cộng thêm **phần chênh lệch** giữa pool mới và pool cũ vào số hiện còn
- Ví dụ: pool cảnh giới hiện tại = 1 tháng, đã dùng gần hết, còn 1 ngày. Pool cảnh giới mới = 2 tháng. Chênh lệch = 1 tháng. Kết quả: còn **1 tháng 1 ngày**
- Chết do PK → trừ thẳng vào số đang đếm ngược
- Hết thọ nguyên = **chết vĩnh viễn**, tạo lại nhân vật mới (với tên mới)

### Tăng thọ nguyên bằng đan dược
- Một số **đan dược hiếm** có thể tăng hoặc hồi phục thọ nguyên
- Ví dụ: còn 1 ngày, uống đan → còn 1 tuần
- Item loại này có thể hiếm, gắn với nạp tiền hoặc drop đặc biệt
- Chi tiết recipe/nguồn gốc sẽ xác định khi làm economy

### Cảnh giới dưới Hoá Thần — Thọ Nguyên
- Player có **thọ nguyên đếm ngược**
- **Khi chết do PK**: rút ngắn thọ nguyên (ví dụ -2 phút mỗi lần chết)
- Lượng rút ngắn đặt trong `game_configs`

### Cảnh giới trên Hoá Thần — Lôi Kiếp
- Không có thọ nguyên, thay vào đó có **đếm ngược đến Lôi Kiếp**
- **Khi chết do PK**: rút ngắn thời gian đếm ngược đến Lôi Kiếp (ví dụ -2 phút)
- Lượng rút ngắn đặt trong `game_configs`
- **Chết do không vượt qua Lôi Kiếp**: có penalty riêng — **chưa thiết kế, bàn sau**

---

## Còn cần thảo luận
- [ ] Penalty Lôi Kiếp thất bại — bàn sau (cảnh giới giữa game)
- [ ] Chi tiết đan dược tăng thọ nguyên — bàn khi làm economy/item
