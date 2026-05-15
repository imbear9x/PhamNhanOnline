---
doc_type: game_design_requirement
system_id: inbox-mail-system
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/inbox-mail-system.md
related_docs:
  - features/inbox-mail-system.md
  - features/main-progression-quest-chain.md
  - features/sect-system.md
  - shared-rules.md
requires_code_verification: false
handoff_ready: true
---

# Hệ Thống Inbox / Hòm Thư — Requirement Spec

## Goal

Implement kênh server → player để nhận reward overflow, admin grant, và các item hệ thống không thể đặt trực tiếp vào balo. Inbox là kênh một chiều — player không gửi được cho nhau.

## Source Design Summary

Canonical design lives in `features/inbox-mail-system.md`.

## Target Design Summary

Inbox nhận item khi balo đầy lúc claim reward hệ thống, hoặc khi server chủ động gửi (admin batch, event hàng loạt). Item trong inbox không hết hạn. Player claim thủ công; server check balo trước khi claim. Inbox phân tab theo nguồn, badge thông báo trên icon, retention giới hạn 50 unread + 50 read (1 ngày) per tab.

**Các nguồn hợp lệ vào inbox:**
- Reward overflow: quest, event, sect welfare, dungeon completion, crafting output — khi balo đầy lúc claim
- Admin grant: bồi thường server, quà sự kiện hàng loạt — gửi thẳng bất kể balo
- Các reward hệ thống khác được user chốt sau

**Không vào inbox:**
- Loot rơi đất từ quái/boss — item vẫn ở đất, player nhặt lại khi dọn balo
- Herb drop từ quái, harvest, extract — reject hoàn toàn (xem `requirements/herb-farming-system.md`)
- Mua hàng NPC — reject toàn bộ giao dịch

## Current Runtime / Evidence Snapshot

- **Not confirmed**: inbox system đã có trong server hay chưa — cần TechDesign verify.
- **Not confirmed**: packet/handler cho inbox claim đã wired chưa.
- **Not confirmed**: tab/retention logic đã có chưa.

## Scope

### Must Implement
- Server-side inbox storage per player, per tab
- Push item vào inbox khi balo đầy (reward overflow) hoặc admin grant
- Claim single entry
- Claim All (toàn bộ entries trong tab hoặc tất cả tab)
- Server check balo trước khi claim — reject toàn bộ nếu không đủ chỗ
- Phân tab theo loại nguồn (ít nhất: Hệ Thống, Tông Môn, Sự Kiện)
- Badge thông báo trên icon inbox khi có unread entry
- Text mô tả nguồn đính kèm mỗi entry
- Retention: 50 unread + 50 read (1 ngày) per tab; cũ nhất bị đẩy khi overflow
- Player xóa entry thủ công

### Must Not Implement
- Player gửi item/mail cho nhau
- Expire timer trên item trong inbox
- Warning hết hạn
- Claim một phần (partial claim trong Claim All)
- Inbox redirect cho loot rơi đất hoặc herb action

## Terminology

- `entry`: một bản ghi inbox gồm item(s), text mô tả, timestamp, trạng thái (unread/read/claimed).
- `tab`: nhóm entries theo loại nguồn (Hệ Thống, Tông Môn, Sự Kiện, ...).
- `overflow push`: server push item vào inbox khi balo đầy lúc reward grant.
- `admin grant`: server push trực tiếp vào inbox bất kể balo.
- `claim`: player nhận item từ entry vào balo.
- `retention limit`: giới hạn số entry per tab; cũ nhất bị xóa khi vượt ngưỡng.

## Functional Requirements

- `REQ-001`: Inbox là kênh server → player một chiều; player không thể tạo entry hay gửi item cho player khác.
- `REQ-002`: Khi server grant reward mà balo không đủ chỗ (quest, event, sect, dungeon, crafting output), item phải được push vào inbox thay vì mất.
- `REQ-003`: Admin grant phải push thẳng vào inbox bất kể balo còn chỗ hay không.
- `REQ-004`: Item trong inbox không có expiry timestamp — tồn tại cho đến khi player claim hoặc bị đẩy ra do retention overflow.
- `REQ-005`: Player có thể claim từng entry (single) hoặc claim tất cả (claim all).
- `REQ-006`: Trước khi claim, server phải check balo player có đủ slot cho tất cả items trong lần claim đó.
- `REQ-007`: Nếu balo không đủ chỗ khi claim: từ chối toàn bộ lần claim đó — không partial grant, không inbox re-push. Client nhận thông báo balo đầy.
- `REQ-008`: Claim thành công: item vào balo, entry chuyển trạng thái "đã đọc / claimed".
- `REQ-009`: Inbox phân tab theo loại nguồn. Các tab tối thiểu: Hệ Thống, Tông Môn, Sự Kiện. Tab bổ sung khi build UI nếu cần.
- `REQ-010`: Mỗi entry phải có text mô tả nguồn được server sinh tự động theo loại event (không phải text tự do từ player).
- `REQ-011`: Badge thông báo chỉ hiển thị trên icon inbox khi có ít nhất 1 entry chưa đọc. Badge là chấm đỏ, không hiển thị số đếm.
- `REQ-012`: Retention per tab: tối đa 50 entry chưa đọc; khi vượt ngưỡng, entry cũ nhất bị xóa (kể cả nếu chưa claim — item trong entry đó mất vĩnh viễn).
- `REQ-013`: Retention per tab: tối đa 50 entry đã đọc; entry đã đọc quá 1 ngày hoặc vượt 50 thì bị xóa tự động.
- `REQ-014`: Player có thể xóa entry thủ công bất kỳ lúc nào (kể cả chưa claim — item mất).
- `REQ-015`: Claim All không giới hạn số entry per lần — claim toàn bộ entries eligible trong lần đó.
- `REQ-016`: Entry vẫn vào inbox khi player offline — player nhận khi đăng nhập lại.

## Acceptance Criteria

- `AC-001`: Given balo đầy khi player claim quest reward, when server processes reward, then item is pushed to inbox with text mô tả nguồn; balo không thay đổi.
- `AC-002`: Given admin creates a batch grant, when server processes it, then all targeted players receive entry in inbox regardless of balo state.
- `AC-003`: Given player has an inbox entry with item, when player claims it with sufficient balo space, then item enters balo and entry becomes claimed/read state.
- `AC-004`: Given player attempts to claim inbox entry but balo is full, when claim is processed, then claim is rejected entirely; item remains in inbox; client receives balo-full notification.
- `AC-005`: Given player attempts Claim All with 3 entries requiring 5 slots but balo only has 3 free, when Claim All is processed, then entire claim is rejected — no partial grant.
- `AC-006`: Given a tab has 50 unread entries and a new entry arrives, when server pushes new entry, then the oldest unread entry is removed (item lost) and the new entry takes its place.
- `AC-007`: Given a read entry is older than 1 day, when retention cleanup runs, then the entry is deleted automatically.
- `AC-008`: Given player is offline when admin sends batch grant, when player logs in, then the inbox entry is present and claimable.
- `AC-009`: Given player deletes an unclaimed entry manually, when deletion is confirmed, then entry and its items are permanently removed.
- `AC-010`: Given inbox has unread entries, when player opens inbox, then badge is visible on inbox icon. Given all entries are read or inbox is empty, then badge is hidden.
- `AC-011`: Given a loot item drops on the ground from a quái and player's balo is full, when player attempts to pick it up, then pick-up is rejected; item remains on ground; inbox is NOT involved.

## Runtime Flow

### Flow 1 — Reward overflow
1. Server attempts to grant item(s) to player (quest complete, event claim, etc.).
2. Server checks balo capacity.
3. Balo has space → item enters balo directly.
4. Balo full → server creates inbox entry with item(s) + auto-generated source text.
5. Client receives badge notification.

### Flow 2 — Admin batch grant
1. Admin creates batch grant (compensation, event gift).
2. Server pushes inbox entry to each targeted player directly.
3. On next login (or live push if online), player sees badge notification.

### Flow 3 — Player claims from inbox
1. Player opens inbox, selects tab.
2. Player taps Claim (single) or Claim All.
3. Server checks balo capacity for all items in this claim.
4. Sufficient space → items enter balo; entries move to read/claimed state.
5. Insufficient space → reject entire claim; return inventory-full error to client.
6. Player frees balo space and retries.

### Flow 4 — Retention cleanup
1. Server periodically (or on push) checks tab entry count per player.
2. Unread > 50: oldest unread entry deleted (items lost).
3. Read entries older than 1 day or read count > 50: oldest read entries deleted.

## State / Lifecycle

### Entry States
- `unread`: pushed to inbox, player has not opened it.
- `read`: player has opened/seen the entry, item not yet claimed.
- `claimed`: item successfully moved to balo.
- `deleted_by_player`: player manually deleted.
- `deleted_by_retention`: auto-deleted due to retention overflow or read expiry.

## Rules And Invariants

- Inbox is strictly server → player; no player-to-player mail path exists.
- Item in inbox never expires on its own; retention overflow is the only auto-removal path.
- Claim is always atomic for the selected set: all-or-nothing, never partial.
- Ground loot pick-up failure never triggers inbox push.
- Herb harvest/extract/drop rejection never triggers inbox push.
- NPC buy rejection never triggers inbox push.
- Admin grant bypasses balo check on push side (entry always created); balo check only happens on claim.

## Data / Config Requirements

| Config key | Default | Notes |
|---|---|---|
| `inbox.max_unread_per_tab` | 50 | Max unread entries per tab |
| `inbox.max_read_per_tab` | 50 | Max read entries per tab |
| `inbox.read_retention_days` | 1 | Days to retain read entries before auto-delete |

- Entry schema: player_id, tab_id, item_list (template + qty), source_text, created_at, state.
- Tab list: configurable; minimum set = Hệ Thống, Tông Môn, Sự Kiện.
- Source text templates per reward type → server-side string config.

## UI / UX Requirements

- Badge: chấm đỏ, chỉ trên icon inbox, ẩn khi không có unread.
- Tab bar: Hệ Thống / Tông Môn / Sự Kiện (extendable).
- Entry row: icon item, số lượng, source text, timestamp.
- Nút Claim (per entry) và Claim All (per tab hoặc tất cả tab).
- Claim All chỉ active khi có ít nhất 1 entry chưa claim.
- Khi reject claim do balo đầy: toast/popup ngắn gọn, không block toàn màn hình.
- Player có thể xóa entry (với confirm nếu chưa claim).

## Telemetry / Logs / Debug Needs

- Log mỗi lần push vào inbox: player_id, tab, source_type, item_list, reason (overflow/admin).
- Log retention deletion: player_id, entry_id, reason (overflow/expiry), items_lost.
- Log claim: player_id, entry_id, result (success/rejected), reason nếu rejected.
- Log manual delete: player_id, entry_id, claimed_state tại thời điểm xóa.

## Related Systems

- `features/inbox-mail-system.md` — canonical feature design source.
- `shared-rules.md` — Inventory Full / Reward Overflow canonical rule.
- `features/main-progression-quest-chain.md` — quest reward overflow dùng inbox.
- `features/sect-system.md` — sect welfare/task reward overflow dùng inbox.
- `requirements/herb-farming-system.md` — herb actions không dùng inbox (exception đã chốt).
- `requirements/npc-system.md` — NPC buy không dùng inbox (exception đã chốt).

## Known Conflicts / Drift

- Chưa confirm inbox system đã có trong server runtime hay chưa — TechDesign cần verify trước khi Dev handoff.
- Không có conflict design nào đã ghi nhận.

## Readiness Level

- Ready for TechDesign refinement: **yes**
- Ready for Dev handoff: **pending** — TechDesign verify runtime existence trước
- Ready for QA verification: **no** — chờ implementation

## Handoff Checklist

- [x] No blocking design questions remain.
- [x] Acceptance criteria are testable.
- [x] Config/data impacts are listed.
- [x] Edge cases are covered.
- [x] Related docs are linked.
- [x] Rules and invariants are explicit.
- [x] Exceptions (herb, NPC buy, ground loot) are called out explicitly.
- [x] `handoff_ready: true`
