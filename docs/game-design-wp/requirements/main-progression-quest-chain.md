---
doc_type: game_design_requirement
system_id: main-progression-quest-chain
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/main-progression-quest-chain.md
related_docs:
  - features/main-progression-quest-chain.md
  - features/home-cave-defense.md
  - features/sect-system.md
  - features/npc-system.md
  - requirements/inbox-mail-system.md
  - shared-rules.md
requires_code_verification: false
handoff_ready: true
---

# Chuỗi Nhiệm Vụ Tiến Trình Chính — Requirement Spec

## Goal

Implement chuỗi nhiệm vụ tuyến tính xuyên suốt game phục vụ tiến trình phát triển ngầm của player: mở khóa tính năng, map, phó bản, và trao reward. Không kể cốt truyện tường minh — player tự cảm nhận qua hành trình.

## Source Design Summary

Canonical design lives in `features/main-progression-quest-chain.md`.

## Target Design Summary

Luôn có đúng 1 quest active. Player chơi tự nhiên, hệ thống track tiến độ objective ngầm. Tất cả objective đạt → quest auto-complete → reward cấp tự động → quest tiếp theo kích hoạt ngay. Không có quest fail, không có abandon. Balo đầy thì reward item vào inbox. Quest Panel hiển thị quest active và tiến độ.

## Current Runtime / Evidence Snapshot

- **Not confirmed**: quest system server-side đã có chưa — TechDesign cần verify.
- **Not confirmed**: objective tracking framework đã có chưa.
- **Not confirmed**: reward grant + unlock flow đã có chưa.
- **Not confirmed**: Quest Panel đã có trong client chưa.
- Inbox system: `requirements/inbox-mail-system.md` đã có — reward overflow dùng inbox.

## Scope

### Must Implement
- Quest chain data-driven: load từ DB, không hardcode
- Luôn có đúng 1 quest active per player
- Q1 tự động active khi player vào game lần đầu
- Q(n+1) tự động active ngay khi Q(n) complete
- Track tiến độ objective từ lúc quest active (tiến độ trước khi active không tính, trừ objective dạng state)
- Objective dạng state: auto-complete ngay khi quest kích hoạt nếu điều kiện đã thỏa
- Quest auto-complete khi tất cả objective đạt — không cần player nộp
- Ngoại lệ: objective `talk_to_npc` yêu cầu player chủ động tương tác với NPC target
- Reward cấp tự động khi complete: item, linh thạch, mở khóa tính năng / map / phó bản
- Item/linh thạch reward vào balo; balo đầy → inbox (shared overflow rule)
- Unlock apply ngay khi complete
- Notification nhỏ khi quest complete và khi quest mới kích hoạt
- Player acknowledge notification — chỉ dismiss, không phải claim
- Quest Panel: tên quest active, mô tả ngắn, danh sách objective + tiến độ, reward sẽ nhận
- Tiến độ lưu server, giữ nguyên khi offline

### Must Not Implement
- Quest chia nhánh
- Side quest, daily quest, repeatable quest
- Sect task / nhiệm vụ tông môn
- Quest fail / quest abandon / quest reset
- Giới hạn thời gian trên quest
- Content cụ thể (số lượng quest, nội dung từng quest) — data design
- Balance reward — data design

## Terminology

- `quest chain`: chuỗi quest tuyến tính duy nhất của game.
- `active quest`: quest duy nhất đang chạy của player tại một thời điểm.
- `objective`: điều kiện cần thỏa để quest complete.
- `state objective`: objective dạng boolean — thỏa hay chưa, không có count (ví dụ `join_sect`, `open_cave`).
- `count objective`: objective có count target (ví dụ kill 10 quái).
- `auto-complete`: quest complete tự động khi tất cả objective đạt, không cần thao tác từ player.
- `unlock`: reward loại mở khóa tính năng / map / phó bản — apply server-side ngay khi complete.

## Functional Requirements

### Quest Chain
- `REQ-001`: Hệ thống chỉ hỗ trợ đúng 1 quest active per player tại mọi thời điểm.
- `REQ-002`: Quest chain là tuyến tính — không có nhánh, không có quest song song.
- `REQ-003`: Q1 phải tự động kích hoạt khi player tạo nhân vật và vào game lần đầu.
- `REQ-004`: Khi Q(n) complete, Q(n+1) phải tự động kích hoạt ngay trong cùng request/tick — không cần thao tác từ player.
- `REQ-005`: Mỗi quest chỉ có thể complete 1 lần duy nhất — không repeatable.
- `REQ-006`: Player không thể bỏ qua, từ chối, hay reset tiến trình quest.
- `REQ-007`: Quest không có giới hạn thời gian.
- `REQ-008`: Toàn bộ quest chain data phải load từ DB — không hardcode quest hay objective trong code.

### Objective Tracking
- `REQ-009`: Tiến độ objective chỉ tính từ thời điểm quest active — các hành động trước khi quest kích hoạt không được tính (trừ state objective).
- `REQ-010`: State objective (join_sect, open_cave, ...) phải auto-complete ngay khi quest kích hoạt nếu điều kiện đã thỏa tại thời điểm đó.
- `REQ-011`: Count objective phải track server-side và persist — tiến độ không mất khi player offline hay disconnect.
- `REQ-012`: Khi tất cả objective của quest đạt → quest auto-complete ngay — không cần player thao tác thêm, **ngoại trừ** objective `talk_to_npc`.
- `REQ-013`: Objective `talk_to_npc` chỉ complete khi player chủ động tương tác với NPC target đúng — auto-complete không áp dụng cho loại này.

### Objective Types (tối thiểu)
- `REQ-014`: Hệ thống phải hỗ trợ các objective type sau:

| Type | Behavior |
|---|---|
| `kill` | Tính số quái đã giết (theo template hoặc tag) kể từ khi quest active |
| `collect` | Tính khi player sở hữu đủ item (count hoặc presence) |
| `craft` | Tính khi player hoàn thành craft item target |
| `talk_to_npc` | Tính khi player tương tác trực tiếp với NPC target — manual trigger |
| `join_sect` | State objective — đã có tông môn là thỏa |
| `kill_boss` | Tính khi player last-hit hoặc tham gia giết boss target |
| `open_cave` | State objective — đã có động phủ là thỏa |
| `travel` | Tính khi player vào map / khu vực target |

- `REQ-015`: Objective type list phải extensible — data schema cho phép thêm type mới mà không cần thay đổi code core.

### Reward
- `REQ-016`: Reward cấp tự động khi quest complete — không cần player claim.
- `REQ-017`: Item và linh thạch reward vào thẳng balo nếu còn chỗ.
- `REQ-018`: Nếu balo đầy khi cấp item/linh thạch reward: redirect vào inbox theo shared overflow rule (`requirements/inbox-mail-system.md`).
- `REQ-019`: Unlock reward (tính năng / map / phó bản) apply server-side ngay khi complete — không cần balo, không qua inbox.
- `REQ-020`: Reward list per quest phải config trong DB — không hardcode.

### Notification và UI
- `REQ-021`: Khi quest complete: hiển thị notification nhỏ, non-intrusive trên màn hình.
- `REQ-022`: Khi quest mới kích hoạt: hiển thị notification nhỏ, non-intrusive trên màn hình.
- `REQ-023`: Notification là acknowledge-only — player dismiss, không có thao tác claim.
- `REQ-024`: Quest Panel phải hiển thị: tên quest active, mô tả ngắn, danh sách objective với tiến độ, reward sẽ nhận khi hoàn thành.
- `REQ-025`: Tiến độ objective dạng count hiển thị `X/Y`. Objective dạng state hiển thị checkbox / done indicator.

## Acceptance Criteria

- `AC-001`: Given player tạo nhân vật mới và vào game, when first login completes, then Q1 tự động active và Quest Panel hiển thị Q1.
- `AC-002`: Given Q1 active, player thực hiện hành động thuộc Q1 trước khi Q1 active, when checking progress, then hành động đó không được tính vào tiến độ.
- `AC-003`: Given Q1 active với state objective `open_cave`, player đã có động phủ trước khi Q1 active, when Q1 activates, then objective `open_cave` auto-complete ngay lập tức.
- `AC-004`: Given tất cả count objective của quest đạt đủ, when last objective completes, then quest auto-complete ngay — reward cấp tự động — Q(n+1) kích hoạt trong cùng flow.
- `AC-005`: Given quest có objective `talk_to_npc`, tất cả objective khác đã đạt, when player chưa tương tác NPC, then quest chưa complete.
- `AC-006`: Given quest có objective `talk_to_npc`, player tương tác đúng NPC target, when interaction triggers, then objective complete và quest complete nếu tất cả objective khác đã đạt.
- `AC-007`: Given quest complete với item reward, balo còn chỗ, when reward grants, then item vào thẳng balo.
- `AC-008`: Given quest complete với item reward, balo đầy, when reward grants, then item redirect vào inbox; unlock reward (nếu có) vẫn apply ngay.
- `AC-009`: Given quest complete với unlock map reward, when reward grants, then map được mở khóa ngay lập tức bất kể balo.
- `AC-010`: Given player offline giữa chừng với quest active tiến độ 5/10, when player login lại, then tiến độ vẫn là 5/10.
- `AC-011`: Given Q(n) complete, when complete resolves, then Q(n+1) kích hoạt ngay — không có khoảng trống không có quest active.
- `AC-012`: Given player đang ở quest cuối cùng của chain, when complete, then không có quest mới — Quest Panel hiển thị trạng thái chain complete (hoặc ẩn nếu không còn quest).

## Runtime Flow

### Flow 1 — First login
1. Server detect new character, first login.
2. Server activate Q1 → tạo player_quest_progress record.
3. Check state objectives của Q1 → auto-complete nếu đã thỏa.
4. Client nhận Quest Panel data với Q1.
5. Notification nhỏ "Nhiệm vụ mới" xuất hiện.

### Flow 2 — Objective event (count)
1. Player thực hiện hành động (giết quái, craft, travel...).
2. Server kiểm tra quest active của player và objective type liên quan.
3. Tăng tiến độ objective tương ứng.
4. Nếu objective đạt target: mark objective complete.
5. Nếu tất cả objective complete: trigger quest complete flow.

### Flow 3 — Quest complete
1. Tất cả objective đạt → quest complete.
2. Server grant reward:
   - Item/linh thạch → balo; balo đầy → inbox.
   - Unlock → apply server-side ngay.
3. Mark quest `completed` trong player record.
4. Activate Q(n+1) nếu có.
5. Client nhận notification complete + notification quest mới.
6. Quest Panel cập nhật với Q(n+1).

### Flow 4 — Talk to NPC objective
1. Quest active với `talk_to_npc` objective.
2. Player di chuyển đến NPC target, tương tác.
3. NPC interaction event → server check quest active có `talk_to_npc` target NPC này không.
4. Nếu match → mark objective complete → check quest complete flow.

## State / Lifecycle

### Quest States
- `inactive`: chưa đến lượt, chưa kích hoạt.
- `active`: đang chạy, tracking objective.
- `completed`: đã hoàn thành, reward đã cấp.

### Objective States
- `pending`: chưa đạt.
- `completed`: đã đạt.

Không có state `failed` hay `expired`.

## Rules And Invariants

- Luôn có đúng 1 quest active — không bao giờ có 2 quest active cùng lúc.
- Tiến độ trước khi quest active không được tính (trừ state objective).
- Quest complete và activate quest tiếp theo là atomic — không có khoảng trống.
- Reward unlock apply ngay, không bị chặn bởi balo.
- Không có quest fail, abandon, hay reset.
- Quest chain data phải fully data-driven — không hardcode trong code.

## Data / Config Requirements

### Quest schema
- `quest_id`, `sequence_order`, `name`, `description`

### Objective schema
- `objective_id`, `quest_id`, `type` (enum), `target_entity_id`, `target_count`, `is_state_type`

### Reward schema
- `reward_id`, `quest_id`, `reward_type` (item / lingstone / unlock), `value`, `unlock_target_id`

### Player progress schema
- `player_id`, `current_quest_id`, `quest_status`
- Per objective: `objective_id`, `current_count`, `is_completed`

## UI / UX Requirements

- Quest Panel: truy cập từ main menu; hiển thị 1 quest active duy nhất.
- Objective list: count dạng `X/Y`, state dạng checkbox.
- Reward preview: list reward sẽ nhận, có indicator unlock nếu là unlock reward.
- Notification: nhỏ, non-intrusive, không block gameplay, tự dismiss sau vài giây hoặc player tap.
- Khi chain complete và không còn quest: Quest Panel ẩn hoặc hiển thị "Đã hoàn thành chuỗi nhiệm vụ".

## Telemetry / Logs / Debug Needs

- Log quest activate: player_id, quest_id, timestamp.
- Log objective progress: player_id, quest_id, objective_id, old_count, new_count.
- Log objective complete: player_id, quest_id, objective_id.
- Log quest complete: player_id, quest_id, reward_list, timestamp.
- Log reward grant: player_id, quest_id, reward_type, value, result (balo / inbox / unlock).
- Debug: query current quest state per player.

## Related Systems

- `features/main-progression-quest-chain.md` — canonical feature design.
- `requirements/inbox-mail-system.md` — reward overflow vào inbox.
- `requirements/npc-system.md` — `talk_to_npc` objective phụ thuộc NPC interaction.
- `features/home-cave-defense.md` — `open_cave` state objective.
- `features/sect-system.md` — `join_sect` state objective.
- `shared-rules.md` — Inventory Full / Reward Overflow rule.

## Known Conflicts / Drift

- Open question cũ trong feature doc về inbox đã resolved — `requirements/inbox-mail-system.md` đã có.
- Không có conflict design nào ghi nhận.

## Readiness Level

- Ready for TechDesign refinement: **yes**
- Ready for Dev handoff: **pending** — TechDesign verify quest system runtime existence trước
- Ready for QA: **no** — chờ implementation

## Handoff Checklist

- [x] No blocking design questions remain.
- [x] Acceptance criteria testable.
- [x] Config/data schema outlined.
- [x] Edge cases covered.
- [x] Related systems linked.
- [x] Out-of-scope explicit.
- [x] `handoff_ready: true`
