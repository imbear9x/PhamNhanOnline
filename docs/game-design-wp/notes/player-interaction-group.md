# Player Interaction Group — Design Notes

**Ngày bắt đầu:** 2026-05-08  
**Trạng thái:** Đang bàn bạc  
**Thứ tự thiết kế:** Trade → Friend → PK (Thách đấu) → Chat

---

## Nhóm tính năng

Ba tính năng sẽ nằm chung trong một nhóm tương tác người chơi:
1. **Giao dịch (Trade)**
2. **Kết bạn (Friend)**
3. **Thách đấu (PK)**

---

## Entry Point chung

Khi 2 người chơi ở cùng khu vực có thể nhìn thấy nhau:
- Double click vào nhân vật đối phương **hoặc** dùng Basic Attack button để select target
- Hiện ra **action list**: Kết bạn / Giao dịch / Thách đấu
- Người chơi chọn action → flow tương ứng bắt đầu

> Note: cần xác nhận server đã support việc select player target hay chưa. Hiện tại targeting system trong combat pipeline target enemy, chưa rõ có target player entity không.

---

## Tính năng 1: Giao dịch (Trade)

### Flow tổng quát

1. **Người A** chọn Giao dịch từ action list trên người chơi B
2. **Người B** nhận thông báo: "Người chơi [A] muốn giao dịch với bạn" → Đồng ý / Từ chối
3. Nếu **Từ chối** → flow kết thúc, thông báo cho A
4. Nếu **Đồng ý** → mở Trade UI cho cả 2 bên

### Trade UI flow

- Mỗi bên có **ô offer** để đặt item/vật phẩm muốn đưa
- Mỗi bên có nút **Khoá (Lock)** — sau khi lock thì bên kia nhìn thấy offer đã xác nhận
- Giao dịch thành công khi: **cả 2 bên đều lock** + **cả 2 bên đều ấn Xác nhận**
- Nếu 1 bên huỷ / đóng UI → giao dịch thất bại, không có item nào dịch chuyển

### Điều kiện giao dịch
- **Phạm vi:** Cùng map + cùng zone. Radius check sẽ bổ sung sau — server đã có chỗ chờ check này.
- **Item non-tradable:** Item có flag không được trade/drop sẽ **không hiển thị** trong trade UI và bị block ở server nếu cố tình gửi.
- **Currency:** Linh thạch là item trong game, trade bình thường như item khác (nếu không bị flag non-tradable).
- **Disconnect / cancel:** Nếu 1 bên mất kết nối hoặc cancel **trước khi cả 2 bên xác nhận hoàn thành** → trade cancel, không có gì thay đổi. Item vẫn nằm nguyên trong inventory mỗi người — UI trade chỉ là temp display, không phải escrow thật.
- **Khi nào item mới thực sự dịch chuyển:** Chỉ khi server xử lý bước xác nhận cuối — lúc đó mới mutation inventory 2 bên.

- **Lock = commit, không có unlock.** Nếu muốn sửa thì phải huỷ giao dịch và bắt đầu lại. Giữ flow sạch, tránh bị lợi dụng.
- **Slot limit:** Tối đa 10 slot mỗi lần trade. Đặt trong `game_configs`. Client validate trước (chỉ cho chọn đến 10). Server validate lại khi nhận lock request.
- **Cooldown:** 15 giây sau khi kết thúc giao dịch, bất kể thành công hay huỷ/thất bại. Đặt trong `game_configs`. Server enforce — trong cooldown không được gửi trade request mới.

---

## Tính năng 4: Chat

### Phân loại kênh chat

#### 1. Friend Chat (1-1)
- Nhắn tin riêng giữa 2 người đã là bạn bè
- Người bị block không gửi được
- **Lưu 50 tin gần nhất** phía server
- Free, không tốn tài nguyên

#### 2. Group Chat
- Chat nội bộ trong **Tông Môn** hoặc **Tổ Đội**
- Free, không tốn tài nguyên
- Lưu lịch sử server (cần xác định giới hạn sau)
- *Phụ thuộc Tông Môn / Tổ Đội system — chưa có hiện tại*

#### 3. Zone Chat
- Tất cả player trong **cùng map + cùng zone** đều nhận được
- Free, không tốn tài nguyên
- **Server bắn xong không lưu** — client tự lưu trong session, mất khi thoát

#### 4. World Chat (Chat Thế Giới)
- Toàn server đều thấy
- **Tốn 1 Truyền Cáo Phù** (item) mỗi lần gửi. Không có item → không chat được
- **Lưu 20 tin gần nhất** phía server. Client mới vào nhận được 20 tin này

---

### Anti-spam (chung cho tất cả kênh)
- Cooldown giữa các tin nhắn — đặt trong `game_configs`
- Không cho gửi 2 tin giống nhau hoặc gần giống nhau trong 1 khoảng thời gian
- Filter nội dung không phù hợp — không hiển thị / không cho gửi

---

### Chat UI
- **Tab riêng** cho từng kênh: Friend / Group / Zone / World
- Lịch sử hiển thị theo tab

---

### Tóm tắt lưu trữ

| Kênh | Lưu server | Lưu client |
|---|---|---|
| Friend Chat | Có (50 tin gần nhất) | Theo server |
| Group Chat | Có (giới hạn xác định sau khi có Tông Môn/Party) | Theo server |
| Zone Chat | Không | Session only |
| World Chat | Có (20 tin gần nhất) | Theo server |

---

- **Truyền Cáo Phù**: tự craft hoặc mua từ NPC. Chi tiết recipe/giá xác định khi làm economy.
- **Filter nội dung**: tự quản blacklist từ khoá, lưu trong DB.
- **Whisper người lạ**: không có. Muốn nhắn riêng phải là bạn. Gặp nhau ngoài field thì dùng Zone Chat.

---

## Tính năng 2: Kết bạn (Friend)

### Flow tổng quát

1. **Người A** chọn Kết bạn từ action list trên người chơi B (có thể qua action list trong world hoặc tìm theo tên/ID nhân vật)
2. **Người B** nhận thông báo: “[A] muốn kết bạn với bạn” → Đồng ý / Từ chối
3. Từ chối → flow kết thúc, thông báo cho A
4. Đồng ý → 2 người thêm nhau vào danh sách bạn bè

### Phạm vi gửi request
- Không giới hạn khu vực. Gửi được qua tên nhân vật hoặc ID (khác Trade và PK).

### Danh sách bạn bè
- Hiển thị trạng thái **online / offline** của từng bạn
- Không hiển thị địa điểm (đang ở map nào)
- Trong tương lai: hỗ trợ chat giữa bạn bè (chưa scope hiện tại)
- Giới hạn **50 bạn**, đặt trong `game_configs`

### Block
- Mỗi người chơi có danh sách chặn riêng
- Khi bị chặn: không nhận được yêu cầu kết bạn lẫn giao dịch từ người đó
- **Transparent block**: người gửi yêu cầu sẽ nhận được thông báo rõ là đã bị chặn (intentional, không phải silent block)
- Muốn bỏ chặn thì vào danh sách chặn và xóa

### Unfriend
- Ai cũng có thể unfriend bất kỳ lúc nào
- **Silent**: không có thông báo cho bên kia. Bên kia tự phát hiện khi vào friend list.

### Block
- Chặn = **tự động unfriend luôn** nếu 2 người đang là bạn
- Người bị chặn không thể: gửi yêu cầu kết bạn, gửi yêu cầu giao dịch, gửi tin nhắn chat
- Transparent: người bị chặn nhận được thông báo rõ là đã bị chặn khi cố gửi request
- Có danh sách đã chặn. Bỏ chặn → 2 người trở lại là người lạ, không tự động thêm lại bạn bè

---

## Tính năng 3: PvP State System (bao gồm Thách đấu)

### Concept tổng quát

PK không chỉ là “thách đấu 1v1” mà là một **PvP State System** rộng hơn. Mỗi player có một PvP state tại mỗi thời điểm, quyết định khi nào có thể tấn công/bị tấn công bởi player khác.

Enemy luôn luôn có thể bị tấn công — không liên quan đến PvP state system này.

---

### Các PvP State

| State | Mô tả | Kích hoạt bởi |
|---|---|---|
| `Safe` | Không thể tấn công / bị tấn công bởi player | Town, home instance |
| `Neutral` | Mặc định ngoài field — không tự đánh nhau | Map thường |
| `Duel` | 2 player đồng ý thách đấu 1v1, chỉ 2 người này PK nhau | Lời mời thách đấu từ action list |
| `PvP Zone` | Whole map cho phép đánh nhau tự do giữa player | Map config server-side |

---

### Kết thúc PvP

- Không có khái niệm “thắng / thua” — chỉ có **kết thúc**
- Điều kiện kết thúc phụ thuộc vào **rule của từng PvP type**:
  - **Duel**: 1 bên chết → duel kết thúc, cả 2 trở về `Neutral`
  - **PvP Zone**: chết rồi hồi sinh lại vẫn trong map → vẫn ở trạng thái PvP Zone, tiếp tục đánh được
  - **Thoát map**: rời khỏi map PvP Zone → mất state, không thấy nhau không đánh được
- Hậu quả khi chết trong PvP → **sẽ thiết kế riêng trong Death Penalty doc** (phụ thuộc loại PvP + map rule)

---

### Ally (Dồng đội) System

- Friend list **không** quyết định đồng đội trong PvP
- Đồng đội được xác định bởi **Ally group** — phụ thuộc PvP type:
  - **Duel**: chỉ 2 người, không có ally
  - **Party PvP** (ví dụ: tạo phòng train): các thành viên trong party là ally, không đánh nhau
  - **Faction PvP** (ví dụ: event Tông Môn): cùng tông môn = ally, không đánh nhau; khác tông môn = địch
  - **Free-for-all**: không có ally, đánh tất cả
- Ally rule được định nghĩa bửi **map config hoặc PvP session config**, không hard-code

---

### PvP Zone (Map Config)

- Map có flag `pvp_mode` trong config: `none` / `duel_only` / `free_for_all` / `faction` / `party`
- Server đọc flag này khi player vào map → gán PvP state tương ứng
- Không phải event-driven hot-toggle (phù hợp với giới hạn config hiện tại của server)

---

### Duel Flow (Thách đấu)

1. Người A chọn “Thách đấu” từ action list trên người chơi B (cùng map + zone)
2. Người B nhận thông báo → Đồng ý / Từ chối
3. Đồng ý → cả 2 chuyển sang state `Duel`, chỉ có thể tấn công nhau
4. 1 bên chết → Duel kết thúc, cả 2 về `Neutral`
5. Hồi sinh sau duel: nếu map là `Neutral` → giữ `Neutral`, không bị buộc PK tiếp

---

### Trong trạng thái PK — Targeting rule

**Có thể target:**
- Đối thủ PK (player đang trong session PK với mình)
- Quai/enemy trong map (vì quai cũng là target bình thường)

**Không thể target:**
- Player bình thường khác
- NPC

**Action bị khóa khi đang PK:**
- Giao dịch
- Kết bạn
- Thách đấu mới với người khác
- Nói chung: mọi social interaction với player ngoài session PK hiện tại

### Kết thúc Duel
- **Không có timeout** — chán thì tự rời map
- Rời khỏi zone đang duel → **kết thúc duel**
- Ngoại lệ: trong map dungeon đặc biệt (PvP Zone) — gặp nhau ở đâu trong map đó cũng đánh được, không phụ thuộc zone

### Ally System — defer
- Ally rule (party, tông môn) sẽ bổ sung sau khi có hệ thống Tông Môn / Party
- Hiện tại thiết kế PvP State System trước, ally là extension sau

---

## Hệ thống hiện có liên quan

| Thành phần | Tình trạng | Ghi chú |
|---|---|---|
| Targeting enemy | Có thật (combat pipeline) | Cần mở rộng để target player entity |
| Inventory/item mutation | Có thật, server authoritative + advisory lock per player | Foundation tốt cho trade item transfer |
| Notification system | Có thật | Dùng được cho trade request |
| Ground reward / direct grant | Có thật | Không dùng trực tiếp, nhưng cùng inventory mutation layer |
| Player-to-player packet | Chưa rõ | Cần confirm server có routing packet giữa 2 player không |

---

## Open Questions

- Server hiện có support select/target player entity không?
- Có packet nào route từ player A → player B không (ngoài broadcast)?
- Trade session cần server-side state machine hay có thể dùng pattern tương tự practice session?
- Item binding policy cần định nghĩa trước khi làm trade
