---
doc_type: game_design_feature
system_id: dungeon-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/main-progression-quest-chain.md
  - features/npc-system.md
  - features/death-penalty.md
  - features/spirit-beast.md
  - features/machine-system.md
  - shared-rules.md
requires_code_verification: false
---

# Hệ Thống Phó Bản (Dungeon) — Feature Draft

## Goal

Tạo hệ thống phó bản đa dạng phục vụ nhiều nhu cầu chơi khác nhau: tiến trình cá nhân, sự kiện thời hạn, và khám phá/farm open-world. Phó bản là không gian tách biệt với map chính, có rule riêng về PvP, instance, và mục tiêu — nhưng vẫn tuân thủ các shared rule cốt lõi của game.

## Design Summary

Có 3 loại phó bản: **cá nhân**, **sự kiện**, và **world**. Mỗi loại có model instance, mục tiêu, và điều kiện vào khác nhau — tất cả đều config-driven qua game design data. Mỗi phó bản có thuộc tính PvP mode: bình thường (tuân thủ PvP state taxonomy như ngoài map) hoặc hỗn loạn (free-for-all, tấn công được bất kỳ thứ gì). Chết trong phó bản vẫn áp dụng death penalty bình thường. Loot, reward, thời gian giới hạn, checkpoint, và giới hạn lần vào đều config per dungeon.

## Scope

### In Scope
- 3 loại phó bản: cá nhân, sự kiện, world
- PvP mode per dungeon: bình thường hoặc hỗn loạn
- Instance model: solo (cá nhân), shared (world), per-trigger (sự kiện)
- Map con bên trong phó bản — liên thông với nhau
- Phó bản world: tele random vào map con khi vào
- Mục tiêu hoàn thành config per dungeon — có thể không có mục tiêu
- Thoát và vào lại — giữ nguyên trạng thái
- Companion (Linh Thú, Khôi Lỗi) có thể vào
- Reward claim thủ công, overflow vào inbox
- Giới hạn lần vào, thời gian giới hạn, checkpoint — tất cả config per dungeon

### Out Of Scope
- Rating / xếp hạng S/A/B ảnh hưởng reward
- Party system — chưa có (phó bản cá nhân là solo; world là shared open)
- Friendly fire trong nhóm — chưa có party
- Nội dung cụ thể của từng phó bản — data design

## Dungeon Types

### Phó Bản Cá Nhân
- **Solo hoàn toàn** — nhưng **không có private instance riêng**. Phó bản cá nhân dùng map/zone chia sẻ; player tự di chuyển tới chỗ muốn.
- Vào qua NPC hoặc cổng map theo config → teleport vào **map khởi đầu** hoặc map theo rule phó bản.
- Điều kiện vào do data design (cảnh giới, quest progress, item, v.v.).
- Có thể có giới hạn lần vào mỗi ngày — config per dungeon.
- Có thể có countdown thời gian hoàn thành — config per dungeon.
- Có thể có checkpoint — config per dungeon.
- Thoát ra được bất kỳ lúc nào; vào lại: tele vào map khởi đầu hoặc map theo rule phó bản — tiến độ (quái/boss đã chết) **giữ nguyên** miễn là đủ điều kiện vào và phó bản còn mở.

### Phó Bản Sự Kiện
- **Shared instance** — nhiều player vào cùng 1 phó bản trong thời gian sự kiện diễn ra.
- Xuất hiện theo thời gian cố định hoặc event trigger do admin bật — cả 2 kiểu đều hợp lệ, config per dungeon.
- Điều kiện vào do data design.
- Có thể có giới hạn lần vào mỗi ngày — config per dungeon.
- Phó bản đóng khi hết thời gian sự kiện — player bên trong bị teleport ra.

### Phó Bản World
- **Shared instance, open** — mọi player đủ điều kiện đều vào được cùng 1 instance.
- Vào phó bản → **tele random vào 1 map con** trong danh sách config. Danh sách map con và tỉ lệ tele được config bởi game design data.
- Các map con **liên thông với nhau** — player có thể đi bộ / di chuyển từ map này sang map kia qua cổng/zone transition.
- Mục đích chơi đa dạng, không bắt buộc: farm quái, tìm linh dược/tài liệu, training, săn boss.
- Có boss xuất hiện trong phó bản — boss có loot riêng.
- Không nhất thiết có điều kiện hoàn thành — player tự quyết định khi nào thoát.
- Phó bản world có thể có giới hạn thời gian mở (đóng cửa theo schedule) hoặc giới hạn lần vào — config per dungeon.

## PvP Mode

Mỗi phó bản có 1 trong 2 PvP mode — thuộc tính config per dungeon, không phải player chọn:

**`normal` — Bình thường:**
- Tuân thủ hoàn toàn **PvP state taxonomy** như ngoài map (xem `shared-rules.md`).
- Muốn đánh nhau phải có đồng thuận (duel) hoặc đang trong context PvP hợp lệ.
- PK vẫn bị penalty bổ sung như thường.

**`chaos` — Hỗn loạn:**
- Tấn công được bất kỳ thứ gì — player, quái, boss — không cần đồng thuận.
- Bị bất kỳ thứ gì tấn công lại — kể cả đồng môn.
- Không phân biệt PK hay lawful PvP trong context này — mọi combat đều hợp lệ.
- **Death penalty vẫn áp dụng bình thường** — chết trong `chaos` vẫn trừ thọ nguyên / drop item / mất buff như ngoài map. Không có miễn penalty.

## Player-Facing Rules

### Entry
- Vào qua NPC hoặc cổng map — đã implement theo shared rule interaction range.
- Điều kiện vào config per dungeon (cảnh giới, quest progress, item tiêu hao, v.v.).
- Phó bản world: vào → tele random map con.

### Thoát và vào lại
- Player có thể thoát ra bất kỳ lúc nào (manual exit hoặc bị teleport ra khi phó bản đóng).
- Vào lại: player tele vào **map khởi đầu hoặc map theo rule phó bản**, rồi tự di chuyển tới chỗ muốn. **Không có private instance** — game không lưu vị trí cũ của player.
- Tiến độ phó bản (quái/boss đã chết) **giữ nguyên** miễn là đủ điều kiện vào và phó bản còn mở.
- Điều kiện vào lại vẫn phải thỏa mãn (đủ điều kiện, còn lượt, phó bản còn mở).
- Phó bản đóng trong khi player đang ở trong: player bị **teleport ra ngoài ngay lập tức**.

### Chết trong phó bản
- Áp dụng **death penalty bình thường** theo shared rule (drop + thọ nguyên / lôi kiếp) — không có ngoại lệ riêng cho phó bản.
- Checkpoint (nếu có): player hồi sinh tại checkpoint thay vì bị đẩy ra — config per dungeon.
- Không có checkpoint: behavior khi chết (đẩy ra, hồi sinh tại cổng, v.v.) config per dungeon.

### Companion
- Linh Thú và Khôi Lỗi có thể theo vào phó bản — không có loại phó bản nào cấm mặc định.
- Nếu cần cấm companion trong phó bản cụ thể: config per dungeon.

### Loot và reward
- Loot từ quái/boss trong phó bản: mặc định giống ngoài map; có thể config loot pool riêng per dungeon.
- Reward hoàn thành phó bản (nếu có): **player phải tự claim** — không cấp tự động.
- Reward overflow khi balo đầy: vào **inbox** theo shared overflow rule.

### Giới hạn lần vào
- Config per dungeon — có thể không giới hạn, hoặc giới hạn X lần/ngày.
- Reset theo ngày server.

## System States

- `open`: phó bản đang mở, đủ điều kiện có thể vào.
- `active`: player đang ở trong.
- `closed`: phó bản đã đóng — không vào được, player bên trong bị teleport ra.
- `cooldown`: player đã hết lượt vào trong ngày — không vào được cho đến khi reset.

## Main Flows

### Flow 1 — Vào phó bản cá nhân
1. Player đến NPC hoặc cổng → chọn vào phó bản.
2. Server check điều kiện + còn lượt.
3. Teleport vào map khởi đầu hoặc map theo rule phó bản.
4. Player tự di chuyển tới điểm muốn, chơi, thoát ra khi muốn.
5. Vào lại: tele lại từ đầu — tiến độ giữ nguyên.

### Flow 2 — Vào phó bản world
1. Player đến NPC hoặc cổng → chọn vào phó bản world.
2. Server check điều kiện.
3. Player teleport vào **1 map con ngẫu nhiên** trong danh sách config.
4. Player tự do khám phá — di chuyển giữa các map con qua zone transition.
5. Thoát khi muốn hoặc khi phó bản đóng.

### Flow 3 — Phó bản đóng khi player đang trong
1. Phó bản hết thời gian / admin đóng.
2. Tất cả player đang trong → teleport ra ngay lập tức.
3. State chuyển sang `closed`.

### Flow 4 — Chết trong phó bản có checkpoint
1. Player chết → death penalty áp dụng bình thường.
2. Hồi sinh tại checkpoint gần nhất.
3. Tiến độ phó bản giữ nguyên.

### Flow 5 — Claim reward hoàn thành
1. Phó bản hoàn thành (nếu có điều kiện hoàn thành).
2. Reward panel hiện ra — player nhấn claim.
3. Server check balo: đủ chỗ → vào balo; đầy → vào inbox.

## Edge Cases
- Player thoát ra trong lúc đang combat: combat hủy, player teleport ra, death penalty không áp dụng (không phải chết).
- Phó bản đóng đúng lúc player đang claim reward: reward vào inbox nếu chưa kịp nhận.
- Hết lượt vào trong ngày nhưng player đang ở trong: player vẫn ở trong bình thường, chỉ bị block khi cố vào lại sau khi đã thoát.
- Companion chết trong phó bản: áp dụng companion death rule bình thường của từng loại (Linh Thú / Khôi Lỗi).
- Phó bản world: nhiều player cùng tele vào cùng 1 map con — bình thường, không giới hạn density (theo shared companion slot rule).

## Data / Config Needs
- Dungeon template: ID, tên, type (`solo` / `event` / `world`), pvp_mode (`normal` / `chaos`) → DB
- Map con list per dungeon + tỉ lệ tele (world dungeon) → DB
- Zone transition config giữa các map con → DB
- Điều kiện vào per dungeon → DB
- Giới hạn lần vào per day per dungeon → DB
- Thời gian giới hạn hoàn thành per dungeon → DB
- Checkpoint config per dungeon → DB
- Behavior khi chết (không có checkpoint) per dungeon → DB
- Loot pool config per dungeon (override hoặc dùng default) → DB
- Reward hoàn thành per dungeon → DB
- Trigger type sự kiện: `scheduled` / `admin_trigger` → DB
- Companion allowed flag per dungeon → DB
- Boss respawn flag + cooldown per boss/dungeon → DB
- Cảnh báo đóng phó bản: mặc định 5 phút trước khi đóng → `game_configs`

## UI / UX Notes
- NPC / cổng vào phó bản hiển thị: tên phó bản, điều kiện vào, lượt còn lại trong ngày, thời gian còn lại (nếu có countdown).
- Trong phó bản: minimap hiển thị các map con và vị trí zone transition.
- Phó bản sắp đóng: notification / countdown hiển thị trước X phút (config).
- Reward panel hiện rõ từng item, nút claim từng cái hoặc claim all.

## Related Systems
- **Death Penalty**: shared death rule áp dụng đầy đủ — xem `features/death-penalty.md`
- **PvP State Taxonomy**: mode `normal` dùng taxonomy này — xem `shared-rules.md`
- **NPC System**: NPC là cổng vào phó bản — xem `features/npc-system.md`
- **Inbox**: reward overflow — xem `features/inbox-mail-system.md`
- **Linh Thú / Khôi Lỗi**: companion theo vào phó bản — xem `features/spirit-beast.md`, `features/machine-system.md`
- **Main Progression Quest Chain**: quest mở khóa phó bản, objective trong phó bản — xem `features/main-progression-quest-chain.md`

## Key Decisions
1. 3 loại phó bản: cá nhân (solo instance), sự kiện (shared, thời hạn), world (shared, open).
2. PvP mode là thuộc tính per dungeon — `normal` hoặc `chaos`.
3. Chết trong phó bản: death penalty bình thường, không miễn.
4. Thoát và vào lại: trạng thái giữ nguyên miễn là đủ điều kiện và phó bản còn mở.
5. Phó bản world: tele random map con khi vào; các map con liên thông.
6. Mục tiêu hoàn thành, giới hạn lần vào, countdown, checkpoint: tất cả config per dungeon — có thể không có.
7. Reward claim thủ công; overflow vào inbox.
8. Không có rating / xếp hạng ảnh hưởng reward.
9. Companion (Linh Thú, Khôi Lỗi) có thể vào — config per dungeon nếu cần cấm.

## Open Questions
- [x] Boss trong phó bản: có thể hồi sinh hoặc không — config per dungeon/boss. Cooldown cũng config per dungeon.
- [x] Zone transition giữa các map con: qua **portal** — hệ chuyển map đã done trong repo.
- [x] Cảnh báo trước khi phó bản đóng: **5 phút** — notification cho tất cả player đang trong.
- [x] Không có private instance cho phó bản cá nhân — vào lại tele từ đầu, tiến độ giữ nguyên.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/` — 4 open questions cần chốt trước khi promote.
