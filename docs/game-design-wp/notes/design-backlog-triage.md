# Design Backlog Triage

**Ngày tạo:** 2026-05-09  
**Mục đích:** Phân loại toàn bộ backlog thiết kế theo trạng thái note để biết nên bàn tiếp cái gì trước khi nâng lên requirement.

---

## 1. Đã note xong hoặc gần xong

Các mục dưới đây đã có khung system design khá rõ. Có thể chưa chốt data/balance, nhưng phần note cơ bản coi như đã đủ dùng để tham chiếu tiếp.

### Đã khá hoàn chỉnh
- **Động Phủ / Công Động Phủ / Cướp Bóc**
  - Nguồn note gốc: `notes/home-cave-defense.md`
  - Đã được đào sâu thêm thành feature draft: `features/home-cave-defense-system.md`

- **Hệ thống Thần Thức**
  - Note: `notes/spirit-sense-system.md`
  - Đã chốt core rule nhìn thấy / tàng hình / lộ diện / phá ẩn

- **Hệ thống Speed**
  - Note: `notes/speed-system.md`
  - Đã chốt core rule movement / fly / evasion theo relative speed

- **Phù Lục & Trận Pháp**
  - Note: `notes/crafting-talisman-formation.md`
  - Core concept và material quality system đã khá rõ

---

## 2. Đã có note nhưng chưa xong

Các mục này đã được bàn, nhưng vẫn còn mở nhiều quyết định hoặc đang gộp quá nhiều thứ trong một note.

### Cần hoàn tất note trước
- **Death Penalty**
  - Note: `notes/death-penalty.md`
  - Trạng thái hiện tại: đang bàn
  - Còn hở:
    - penalty Lôi Kiếp thất bại
    - chi tiết thuốc / nguồn tăng thọ nguyên
    - cần rà chặt tương thích với PvP / động phủ / pet

- **Linh Thú**
  - Note: `notes/spirit-beast-system.md`
  - Trạng thái hiện tại: chốt concept/gameplay core, nhưng chưa kín ở level note hoàn chỉnh
  - Còn hở:
    - ownership / trade threshold
    - breeding / runtime details
    - auto loot behavior
    - guard home behavior
    - thọ nguyên / tử vong / hồi phục của pet

- **Player Interaction Group**
  - Note: `notes/player-interaction-group.md`
  - Đang gộp nhiều hệ trong một file:
    - trade
    - friend / block
    - PvP state
    - chat
  - Còn hở:
    - một số runtime/server assumptions chưa xác nhận
    - group/ally/faction còn defer
    - nên cân nhắc tách thành note nhỏ sau khi bàn tiếp

---

## 3. Chưa có note riêng hoặc chưa thật sự bàn kỹ

Các mục này đang tồn tại dưới dạng backlog/pending, nhưng chưa có note design riêng đủ sâu.

### Cần mở note riêng khi bắt đầu bàn
- **Nhiệm vụ (Quest)**
- **Phó Bản (Dungeon)**
- **Tông Môn**
- **Hoạt động Tông Môn**
- **Event**
- **Trả Thù**
- **Bảng Xếp Hạng Cá Nhân**
- **Khai Thác Linh Thạch**
- **Ally System (Party / Tông Môn trong PvP)**
- **Farming / Herb loop**

### Có note liên quan nhưng chưa thành chủ đề bàn riêng
- **Death Penalty Lôi Kiếp thất bại**
  - Hiện mới là phần defer trong `notes/death-penalty.md`

- **Pháp Khí (Smithing)**
  - Có nhắc trong note crafting, nhưng chưa thành một note player-facing riêng nếu muốn đào sâu loop

- **Boss Thế Giới**
  - Chủ yếu cần confirm implementation thực tế, chưa phải note design sâu ngay

---

## 4. Các mục thiên về backlog, không cần nâng thành feature note lúc này

- `notes/deferred-features.md`
  - Dùng làm backlog / danh sách pending
- `notes/conversation-log.md`
  - Dùng làm log

---

## 5. Ưu tiên bàn tiếp đề xuất

Nếu mục tiêu là **viết note xong hết trước khi nâng requirement**, thứ tự hợp lý hiện tại là:

1. **Death Penalty**
   - vì nó ảnh hưởng chéo tới PvP, động phủ, pet

2. **Linh Thú**
   - vì đã có nhiều concept, chỉ cần khóa nốt các rule còn hở

3. **Player Interaction Group**
   - nên bàn tiếp rồi cân nhắc tách file thành:
     - trade
     - friend/block
     - PvP state
     - chat

4. **Tông Môn**
   - là một cụm lớn, nên mở note riêng khi bắt đầu

5. **Khai Thác Linh Thạch**
   - nên mở note riêng sớm vì đây là nguồn kinh tế nền và có liên quan trực tiếp tới vòng linh thạch của nhiều hệ khác

---

## 6. Nguyên tắc workflow đang dùng

- Còn đang ở **phase system design**
- Ưu tiên **viết note cho kín rule / state / flow trước**
- Chưa khóa design data / balance nếu chưa tới phase đó
- Chỉ khi note đủ chín mới nâng lên `features/` hoặc `requirements/`
