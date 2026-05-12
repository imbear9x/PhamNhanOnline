---
doc_type: game_design_note
system_id: player-interaction-group
status: draft
maturity: note
owner: gamedesign
created_at: 2026-05-08
updated_at: 2026-05-12
promoted_from: null
related_docs:
  - features/death-penalty.md
requires_code_verification: true
---

# Player Interaction Group — Design Note

## Purpose

Thiết kế nhóm tính năng tương tác giữa người chơi: Giao dịch, Kết bạn, PvP State, và Chat. Đây là nền cho các interaction layer trên PvP, kinh tế, và cộng đồng.

## Current Understanding

Nhóm 4 tính năng liên quan:
1. **Giao dịch (Trade)**
2. **Kết bạn (Friend)**
3. **PvP State System (bao gồm Duel)**
4. **Chat**

Tất cả chia sẻ một **entry point chung**: double click hoặc Basic Attack button để select target → action list.

## Core Fantasy / Player Value

- Giao dịch tạo nền kinh tế player-to-player.
- Kết bạn + block tạo lớp xã hội cơ bản.
- PvP state system tạo ranh giới rõ ràng giữa các tình huống chiến đấu.
- Chat tạo kênh giao tiếp theo ngữ cảnh.

## Key Terms

- `Safe`: không thể tấn công / bị tấn công bởi player.
- `Neutral`: mặc định ngoài field, không tự đánh nhau.
- `Duel`: 2 player đồng ý, chỉ 2 người này PK nhau.
- `PvP Zone`: whole map cho phép đánh nhau tự do.
- `Lineage id`: field dùng để check cận huyết pet, không liên quan PvP.
- `Truyền Cáo Phù`: item tiêu hao để gửi World Chat.

## Draft Rules

### Entry Point chung
- Double click vào nhân vật hoặc Basic Attack button để select target → hiện **action list**: Kết bạn / Giao dịch / Thách đấu.
- *Cần xác nhận server đã support select player entity chưa — xem `requires_code_verification`.*

---

### Tính năng 1: Giao dịch (Trade)

**Flow:**
1. A chọn Giao dịch → B nhận thông báo → Đồng ý / Từ chối.
2. Đồng ý → Trade UI mở cho cả 2.
3. Mỗi bên có ô offer + nút **Lock**. Sau khi lock thì bên kia thấy offer đã xác nhận.
4. **Cả 2 bên đều lock + ấn Xác nhận** → giao dịch thành công.
5. 1 bên hủy / đóng → giao dịch thất bại, không có item nào dịch chuyển.

**Điều kiện:**
- Cùng map + cùng zone. Radius check bổ sung sau.
- Item non-tradable / non-droppable: không hiển thị trong Trade UI, blocked ở server.
- Linh thạch là item, trade bình thường nếu không bị flag non-tradable.
- Mất kết nối / cancel trước khi cả 2 xác nhận → trade cancel, item không dịch chuyển.
- Item thực sự dịch chuyển **chỉ khi server xử lý bước xác nhận cuối**.

**Rule bổ sung:**
- **Lock = commit, không có unlock**. Muốn sửa → hủy và bắt đầu lại.
- **Slot limit**: tối đa 10 slot per trade → `game_configs`. Client validate + server validate.
- **Cooldown**: 15 giây sau mỗi lần kết thúc (dù thành công hay thất bại) → `game_configs`. Server enforce.

---

### Tính năng 2: Kết bạn (Friend)

**Flow:**
1. A gửi request → B nhận thông báo → Đồng ý / Từ chối.
2. Đồng ý → 2 người thêm nhau vào danh sách.

**Phạm vi gửi request:** Không giới hạn khu vực, có thể gửi qua tên nhân vật / ID.

**Danh sách bạn bè:**
- Hiển thị trạng thái online / offline.
- **Không** hiển thị địa điểm.
- Giới hạn 50 bạn → `game_configs`.

**Unfriend:** Im lặng, không thông báo cho bên kia.

**Block:**
- Tự động unfriend nếu đang là bạn.
- Người bị chặn không thể: gửi yêu cầu kết bạn, giao dịch, nhắn tin.
- **Transparent**: người bị chặn nhận thông báo rõ là bị chặn khi cố gửi request.
- Bỏ chặn → 2 người trở lại là người lạ, không tự động thêm lại bạn bè.

---

### Tính năng 3: PvP State System

**Các PvP State:**

| State | Mô tả | Kích hoạt |
|---|---|---|
| `Safe` | Không tấn công / bị tấn công bởi player | Town, home instance |
| `Neutral` | Mặc định ngoài field | Map thường |
| `Duel` | 2 player đồng ý, chỉ 2 người PK nhau | Lời mời từ action list |
| `PvP Zone` | Whole map tự do | Map config server-side |

Enemy luôn có thể bị tấn công — không liên quan PvP state.

**Không có thắng / thua** — chỉ có kết thúc:
- **Duel**: 1 bên chết → duel kết thúc, cả 2 về Neutral.
- **PvP Zone**: chết rồi hồi sinh lại trong map → vẫn ở PvP Zone.
- **Thoát map PvP Zone** → mất state.

**Duel Flow:**
1. A chọn Thách đấu → B nhận thông báo → Đồng ý / Từ chối.
2. Đồng ý → cả 2 vào state Duel.
3. 1 bên chết → Duel kết thúc, cả 2 về Neutral.
4. **Không có timeout** — chán thì rời map.
5. Rời zone đang duel → kết thúc duel.

**Trong trạng thái PK — targeting rule:**
- Có thể target: đối thủ PK, quái/enemy.
- Không thể target: player bình thường khác, NPC.
- Action bị khóa: giao dịch, kết bạn, thách đấu mới với người khác.

**PvP Zone (Map Config):**
- Map có flag `pvp_mode`: `none` / `duel_only` / `free_for_all` / `faction` / `party`.
- Server đọc flag khi player vào map → gán PvP state.
- Không hot-toggle.

**Ally System — defer:**
- Ally rule (party, tông môn) bổ sung sau khi có Tông Môn / Party system.

---

### Tính năng 4: Chat

**4 kênh:**

| Kênh | Scope | Chi phí | Lưu server |
|---|---|---|---|
| Friend Chat (1-1) | Giữa 2 người đã là bạn | Free | 50 tin gần nhất |
| Group Chat | Tông Môn / Tổ Đội | Free | Xác định sau khi có hệ Tông Môn/Party |
| Zone Chat | Cùng map + zone | Free | Không lưu, client session only |
| World Chat | Toàn server | 1 Truyền Cáo Phù per tin | 20 tin gần nhất |

**Anti-spam (chung):**
- Cooldown giữa các tin → `game_configs`.
- Không gửi 2 tin giống / gần giống trong khoảng thời gian.
- Filter nội dung không phù hợp.

**Rule bổ sung:**
- Người bị block không gửi được Friend Chat.
- Whisper người lạ: không có. Muốn nhắn riêng phải là bạn bè.
- Truyền Cáo Phù: tự craft hoặc mua từ NPC. Chi tiết recipe / giá xác định khi làm economy.
- Filter: tự quản blacklist từ khoá, lưu trong DB.

**UI:**
- Tab riêng per kênh: Friend / Group / Zone / World.

---

## Design Decisions

### Locked
- Lock = commit, không unlock trong Trade.
- Block là transparent, không phải silent.
- PvP state system tách biệt với friend list.
- Ally rule defer đến Tông Môn / Party system.
- World Chat tốn Truyền Cáo Phù.

### Tentative
- Radius check cụ thể cho Trade — bổ sung sau.
- Số slot Trade tối đa (tạm 10).
- Trade cooldown (tạm 15 giây).
- Giới hạn danh sách bạn bè (tạm 50).

## Related Systems
- **Death Penalty**: "chết do PK" cần định nghĩa chính xác liên quan đến PvP state — xem `features/death-penalty.md`.
- **Tông Môn / Party**: Ally rule và Group Chat phụ thuộc — chưa có hệ này.

## Open Questions
- [ ] Server hiện có support select / target player entity không?
- [ ] Có packet nào route từ player A → player B không (ngoài broadcast)?
- [ ] Trade session cần server-side state machine hay có thể dùng pattern tương tự practice session?
- [ ] Item binding policy cần định nghĩa trước khi làm trade.
- [ ] Giới hạn Group Chat lưu server — xác định sau khi có Tông Môn/Party.

## Risks / Watchouts
- Nếu server chưa support target player entity, toàn bộ entry point chung bị block — cần verify sớm.
- Trade phải có server-side validation để tránh race condition khi 2 bên cùng cancel.
- PvP state "chết do PK" cần được đồng bộ chính xác với Death Penalty để tránh edge case penalty sai.

## Promotion Checklist
- [ ] Core gameplay goal is clear.
- [ ] Player-facing loop is understandable.
- [ ] Key terms are defined.
- [ ] Major alternatives are resolved or listed as open questions.
- [ ] Ready to promote to `features/`.
