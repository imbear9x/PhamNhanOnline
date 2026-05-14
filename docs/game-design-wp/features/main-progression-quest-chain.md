---
doc_type: game_design_feature
system_id: main-progression-quest-chain
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-12
updated_at: 2026-05-12
promoted_from: notes/quest-system.md
related_docs:
  - features/home-cave-defense.md
  - features/player-interaction-group.md
requires_code_verification: false
---

# Chuỗi Nhiệm Vụ Tiến Trình Chính — Feature Draft

## Goal

Tạo **chuỗi nhiệm vụ tiến trình chính** phục vụ **tiến trình phát triển ngầm** của người chơi. Quest không kể cốt truyện tường minh mà để player tự cảm nhận qua hành trình. Mục tiêu chính là mở khóa tính năng, map, phó bản và trao phần thưởng — không bắt buộc làm nhưng không làm thì không mở khóa được nội dung tiếp theo.

## Design Summary

Đây là hệ **chuỗi nhiệm vụ tiến trình chính** của game, không phải global quest system. Chuỗi này là 1 tuyến tính duy nhất, **xuyên suốt toàn game** và tiếp tục mở nội dung về sau. Luôn có đúng 1 quest active. Mỗi quest có thể có nhiều objective; khi tất cả objective đạt thì quest auto-complete không cần nộp. Reward cấp tự động. Quest tiếp theo kích hoạt ngay. Không có quest fail, không có quest abandon. Tiến độ lưu server và giữ nguyên khi offline.

## Scope

### In Scope
- Main progression quest chain tuyến tính
- Các loại objective: giết quái, thu thập item, luyện chế item, gặp NPC, tham gia tông môn, giết boss, mở động phủ
- Auto-complete khi đủ objective
- Reward tự động: item, linh thạch, mở khóa tính năng / map / phó bản
- Tracking tiến độ objective từ lúc quest active
- Hòm thư / inbox nhận item reward khi balo đầy
- Quest Panel hiển thị quest active + tiến độ

### Out Of Scope
- Quest chia nhánh
- Side quest
- Quest hàng ngày / sự kiện / repeatable
- Sect task / nhiệm vụ tông môn
- Quest fail / abandon
- Data cụ thể: số lượng quest, nội dung từng quest, objective type list đầy đủ — data design
- Balance reward — data design

## Objective Types

Main progression quest chain hiện hỗ trợ các nhóm objective sau:

| Nhóm | Ví dụ | Ghi chú |
|---|---|---|
| Kill | Giết X quái | Chỉ tính khi quest đang active |
| Talk | Nói chuyện NPC | NPC **đứng yên**, không dùng NPC di chuyển theo event |
| Travel | Đến map / khu vực | Có thể auto-complete khi vừa vào đúng map |
| Collect | Nhặt / sở hữu item | Chỉ tính theo rule quest cụ thể |
| Use | Dùng item | Ví dụ dùng vật phẩm mở khóa / hướng dẫn |
| Unlock | Mở khóa tính năng | Ví dụ mở động phủ, mở panel hệ thống |
| Join | Gia nhập hệ social | Ví dụ `join_sect` |
| Complete activity | Hoàn thành craft / hành vi hệ thống | Ví dụ hoàn thành 1 bước luyện chế |

## Core Loop

1. Player vào game lần đầu → Q1 tự động active.
2. Hệ thống track tiến độ objective ngầm trong khi player chơi.
3. Tất cả objective đạt → quest auto-complete → reward cấp tự động.
4. Notification nhỏ xuất hiện. Player acknowledge.
5. Quest tiếp theo tự động kích hoạt → lặp lại.

## Player-Facing Rules

### Quest Chain
- Toàn bộ quest là **1 chuỗi tuyến tính**, không chia nhánh.
- Luôn có đúng **1 quest active** tại một thời điểm.
- Không thể bỏ qua, từ chối hay xóa tiến trình — player có quyền không làm nhưng không thể reset.
- Mỗi quest chỉ làm **1 lần duy nhất**.
- Không có giới hạn thời gian.

### Điều kiện kích hoạt
- Q1 tự động active khi player vào game lần đầu.
- Q(n+1) tự động active ngay khi Q(n) complete.
- Không check cảnh giới hay tính năng đã mở — chỉ cần hoàn thành quest trước đó.

### Objective
- Một quest có thể có **nhiều objective cùng lúc**.
- Phải đạt **tất cả** objective mới complete quest.
- Tiến độ **chỉ tính từ lúc quest active**.
- **Ngoại lệ — objective dạng state**: nếu điều kiện đã thỏa trước khi quest active (ví dụ đã có tông môn, đã có động phủ) → objective đó auto-complete ngay khi quest kích hoạt.
- Tiến độ lưu server, **không mất khi offline**.

**Các loại objective được hỗ trợ:**
- `kill`: giết X con quái (loại cụ thể hoặc theo tag)
- `collect`: thu thập X item
- `craft`: luyện chế X item
- `talk_to_npc`: gặp và nói chuyện với NPC target — player phải chủ động tới trigger
- `join_sect`: tham gia tông môn — dạng state
- `kill_boss`: giết boss cụ thể
- `open_cave`: có động phủ — dạng state
- Danh sách mở rộng theo data design

### Auto-complete
- Quest tự hoàn thành khi tất cả objective đạt — **không cần nộp quest**.
- Ngoại lệ duy nhất: objective `talk_to_npc` yêu cầu player **chủ động tới gặp NPC** để trigger complete.

### Reward
- Reward cấp **tự động** khi quest complete.
- Các loại reward: item, linh thạch, mở khóa tính năng, mở khóa map, mở khóa phó bản.
- Item và linh thạch vào **thẳng balo**.
- Mở khóa tính năng / map / phó bản apply ngay.
- **Balo đầy**: item reward tự động vào **hòm thư / inbox**. Player dọn balo xong thì vào inbox lấy thủ công.
- Player **bấm acknowledge** notification — chỉ là xác nhận đã biết, không phải thao tác claim.

### Notification và UI
- Quest complete: **notification nhỏ** trên màn hình.
- Quest mới kích hoạt: **notification nhỏ** trên màn hình.
- Không có popup lớn.
- **Quest Panel** hiển thị:
  - Tên quest đang active + mô tả ngắn
  - Danh sách objective + tiến độ (ví dụ `3/10`)
  - Reward sẽ nhận khi hoàn thành

## System States

| State | Mô tả |
|---|---|
| `active` | Quest đang chạy, player đang thực hiện objective |
| `completed` | Tất cả objective đạt, reward đã cấp, quest tiếp theo kích hoạt |

Không có state `failed` hay `abandoned`.

## Main Flows

### Flow 1 — Quest thông thường (auto-complete)
1. Quest kích hoạt → notification nhỏ.
2. Player chơi tự nhiên, hệ thống track tiến độ ngầm.
3. Objective đạt đủ → quest auto-complete.
4. Reward cấp tự động vào balo.
5. Notification nhỏ xuất hiện → player acknowledge.
6. Quest tiếp theo kích hoạt.

### Flow 2 — Quest có objective gặp NPC
1. Quest kích hoạt với objective `talk_to_npc`.
2. Player di chuyển đến NPC target.
3. Player tương tác / nói chuyện với NPC → objective trigger complete.
4. Nếu tất cả objective khác đã đạt → quest auto-complete luôn.

### Flow 3 — Balo đầy khi nhận reward
1. Quest complete, reward gồm item.
2. Balo đầy → item vào **hòm thư / inbox** tự động.
3. Notification nhỏ thông báo item đang ở inbox.
4. Player dọn balo → vào inbox → lấy thủ công.

## Edge Cases
- Objective dạng state đã thỏa trước khi quest active: auto-complete objective đó ngay khi quest kích hoạt.
- Player offline giữa chừng: tiến độ lưu server, giữ nguyên khi login lại.
- Objective `talk_to_npc` mà NPC không available (combat, event lock...): player phải đợi NPC về trạng thái bình thường.
- Nhiều objective cùng lúc, 1 objective dạng state đã thỏa, 1 objective chưa: chỉ complete quest khi tất cả đạt.

## Data / Config Needs
- Quest chain data: ID, tên, mô tả, thứ tự trong chuỗi → DB quest schema
- Objective list per quest: type, target entity ID, count → DB
- Objective type enum: `kill`, `collect`, `craft`, `talk_to_npc`, `join_sect`, `kill_boss`, `open_cave`, ... → DB
- Reward list per quest: type, value, unlock flag → DB
- Unlock flag per reward (tính năng / map / phó bản ID) → DB

## UI / UX Notes
- Quest Panel: tab riêng trong main menu, hiển thị 1 quest active duy nhất.
- Tiến độ objective: hiển thị dạng `X/Y` cho objective có count; checkbox cho objective dạng state / one-time.
- Notification: non-intrusive, không block gameplay.
- Inbox/hòm thư: cần badge indicator khi có item chờ nhận.

## Related Systems
- **Tông Môn**: objective `join_sect` → xem `features/sect-system.md`
- **Động Phủ**: objective `open_cave` → xem `features/home-cave-defense.md`
- **Phó Bản**: unlock phó bản là dạng reward → xem backlog
- **NPC System**: objective `talk_to_npc` phụ thuộc NPC interaction → xem `features/npc-system.md`
- **Inbox / Hòm Thư**: nhận item reward khi balo đầy — xem `notes/inbox-mail-system.md`

> Doc này **không** định nghĩa side quest, daily, repeatable, event quest, hay sect task. Các hệ đó là system riêng nếu được mở sau.

## Key Decisions
1. Quest chain tuyến tính, không chia nhánh.
2. Luôn có đúng 1 quest active.
3. Auto-complete, không cần nộp quest.
4. Objective dạng state auto-complete ngay khi quest kích hoạt nếu đã thỏa.
5. Tiến độ chỉ tính từ lúc quest active (trừ objective dạng state).
6. Reward tự động vào balo; balo đầy thì vào inbox.
7. Không có quest fail / abandon / repeatable.

## Open Questions
- [x] Objective type list cơ bản đã chốt: kill, talk, travel, collect, use, unlock, join, complete activity.
- [x] Chuỗi quest là **dài xuyên suốt toàn game** và tiếp tục mở nội dung về sau; số lượng cụ thể do data design quyết định.
- [x] NPC objective chỉ dùng **NPC đứng yên / tương tác tĩnh**.
- [ ] Inbox / Hòm Thư cần có doc riêng khi thiết kế hệ thống đầy đủ.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
