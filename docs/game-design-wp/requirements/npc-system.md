---
doc_type: game_design_requirement
system_id: npc-system
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/npc-system.md
related_docs:
  - features/npc-system.md
  - features/main-progression-quest-chain.md
  - features/home-cave-defense.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: false
---

# Hệ Thống NPC — Requirement Spec

## Goal

Implement hệ NPC tương tác đứng yên trên map để phục vụ các chức năng trò chuyện, shop mua/bán, chuyển map, và vào phó bản. Hệ phải hỗ trợ NPC thường trực và NPC có thời gian xuất hiện, với action gating rõ ràng, interaction range thống nhất, và shop bán vô hạn bằng linh thạch.

## Source Design Summary

Canonical design lives in `features/npc-system.md`.

Requirement-level clarifications locked for implementation:
- NPC là interaction entity đứng yên trên map; không tham gia combat.
- Có 2 loại xuất hiện: `permanent` và `timed`.
- Timed NPC có countdown, có thể xuất hiện theo lịch hoặc event trigger, và vào `closing_soon` trước khi hết giờ 3 phút.
- Trong `closing_soon`, chỉ còn action `Trò chuyện`; các action khác bị block ngay cả khi UI đang mở.
- Shop NPC bán vô hạn, tiền tệ duy nhất là linh thạch.
- Mua hàng khi balo đầy: reject toàn bộ giao dịch, không gửi inbox.
- Nhiều player có thể tương tác/mở shop cùng một NPC đồng thời.
- Nút vào map/phó bản chỉ hiển thị khi player đủ điều kiện.
- NPC không giao/nhận quest; nếu quest cần “đánh NPC” thì dùng boss entity riêng mang model NPC.

## Target Design Summary

Player đi đến gần NPC, mở panel action, và chọn đúng hành động mà NPC đó cho phép. NPC đóng vai trò cổng vào nội dung chứ không phải actor chiến đấu. Timed NPC tạo nhịp hoạt động thế giới bằng countdown và state đóng cửa mềm trước khi biến mất.

Behavior cần đạt:
- Player phải ở trong shared interaction range mới mở tương tác được.
- Action list hiển thị theo config per NPC và theo điều kiện unlock của player.
- Shop mua là stock vô hạn; không có race condition giữa nhiều player cùng mua.
- Sell UI chỉ hiển thị item NPC thực sự nhận mua.
- Timed NPC khi còn 3 phút cuối phải đóng toàn bộ action ngoài `Trò chuyện`, kể cả đang dở giao dịch.
- Hết giờ thì NPC biến mất và không còn tương tác được.

## Current Runtime / Evidence Snapshot

- **Confirmed by design history**: shared interaction range đã là shared rule canonical và đã được ghi là implement trong repo.
- **Not yet confirmed**: current backend/client already supports NPC timed states (`visible` / `closing_soon` / `hidden`) hay chưa.
- **Not yet confirmed**: NPC shop currently supports unlimited stock + linh thạch-only currency hay chưa.
- **Not yet confirmed**: sell list filtering per NPC và action unlock gating đã có runtime support chưa.
- **Requires code verification**: NPC action dispatch, timed NPC countdown/closing behavior, shop concurrency, map/dungeon transfer action wiring.

## Scope

### Must Implement
- Static NPC entity placed by config on maps.
- Two NPC availability types: permanent and timed.
- NPC action panel with configured action list.
- Supported actions in this phase:
  - trò chuyện
  - mua vật phẩm
  - bán vật phẩm
  - vào phó bản
  - vào map
- Static and branched Q&A dialogue.
- Timed NPC countdown and `closing_soon` state at T-3 minutes.
- Shop buy flow with unlimited stock and linh thạch currency.
- Shop sell flow with per-NPC accepted item list.
- Condition-based visibility for map/dungeon entry buttons.
- Concurrent multi-player interaction with same NPC.

### Must Not Implement
- NPC movement following player.
- NPC direct combat participation.
- Quest hand-in / quest turn-in logic on NPC.
- Dialogue that changes based on quest progress.
- Arbitrary extra action buttons beyond configured supported action set in this phase.

## Terminology

- `permanent NPC`: always spawned at configured location.
- `timed NPC`: appears only during configured schedule or trigger window.
- `closing_soon`: final 3-minute timed NPC state where only dialogue remains available.
- `action button`: one selectable player-facing interaction entry on the NPC panel.
- `branched dialogue`: configured list of player questions mapped to configured NPC answers.
- `unlock-gated action`: action button shown only if player meets the required condition.

## Functional Requirements

- `REQ-001`: NPCs shall be stationary interaction entities placed at configured map positions.
- `REQ-002`: NPCs in this system shall not be valid combat targets in normal state.
- `REQ-003`: NPC availability type shall support exactly `permanent` and `timed`.
- `REQ-004`: A player shall only be able to initiate NPC interaction while within the shared interaction range rule.
- `REQ-005`: Interacting with an NPC shall open an action panel listing only that NPC's configured actions.
- `REQ-006`: One NPC template may be spawned in multiple maps with the same base functionality.
- `REQ-007`: Timed NPCs shall support spawn control by fixed daily schedule or event/trigger config.
- `REQ-008`: Timed NPCs shall expose a visible countdown of remaining availability time.
- `REQ-009`: At exactly 3 minutes before despawn, a timed NPC shall enter `closing_soon` state.
- `REQ-010`: In `closing_soon`, only the `Trò chuyện` action shall remain available.
- `REQ-011`: When a timed NPC enters `closing_soon`, any currently open buy/sell/teleport-related UI from that NPC shall close immediately and the in-progress action shall be canceled without item or currency movement.
- `REQ-012`: When timed NPC availability ends, the NPC shall enter `hidden` state and no longer be visible or interactable.
- `REQ-013`: Dialogue shall support `static` and `branched` modes from config.
- `REQ-014`: Branched dialogue shall present only configured questions and answers; dialogue content shall not change dynamically by quest progress in this phase.
- `REQ-015`: NPC shop buy inventory shall be fixed by config and treated as unlimited stock.
- `REQ-016`: NPC shop buy currency shall be linh thạch only.
- `REQ-017`: Buy transactions shall be rejected entirely if the player's bag does not have enough capacity; no partial buy and no inbox fallback.
- `REQ-018`: Multiple players shall be able to open and use the same NPC shop concurrently without affecting stock or each other’s session state.
- `REQ-019`: Sell action shall only be available on NPCs configured with sell capability.
- `REQ-020`: Sell UI shall show only items explicitly accepted by that NPC's config.
- `REQ-021`: Server shall reject sell attempts for non-accepted items even if the client is bypassed.
- `REQ-022`: Entry actions (`Vào map`, `Vào phó bản`) shall be shown only when the player meets configured unlock conditions.
- `REQ-023`: Selecting a valid entry action shall transfer the player immediately to the configured destination.
- `REQ-024`: If a quest requires “defeat NPC”, implementation shall use a separate combat entity/boss that may reuse NPC visuals, not the NPC interaction entity itself.
- `REQ-025`: Timed NPC notifications may be local-map scoped per NPC config.

## Acceptance Criteria

- `AC-001`: Given a player stands outside shared interaction range, when they click an NPC, then interaction does not open.
- `AC-002`: Given a player stands inside interaction range, when they click an NPC, then only that NPC's configured action buttons are shown.
- `AC-003`: Given a timed NPC has more than 3 minutes remaining, when a player opens it, then all configured eligible actions remain available.
- `AC-004`: Given a timed NPC reaches 3 minutes remaining, when state transitions, then only `Trò chuyện` remains available and all other actions are blocked.
- `AC-005`: Given a player is browsing an NPC shop, when that NPC enters `closing_soon`, then the shop UI closes immediately and no transaction is completed.
- `AC-006`: Given a timed NPC reaches end time, when despawn resolves, then the NPC is hidden and cannot be interacted with.
- `AC-007`: Given two players open the same NPC shop at the same time, when both buy the same item, then both purchases succeed independently if each player has enough currency and bag space.
- `AC-008`: Given a player's bag is full, when they attempt to buy from NPC shop, then the purchase is rejected entirely and no inbox fallback occurs.
- `AC-009`: Given an NPC only buys configured item A, when the player opens sell UI with item B in bag, then item B is not shown as sellable.
- `AC-010`: Given a bypassed client sends sell request for non-accepted item, when the server validates the request, then the request is rejected.
- `AC-011`: Given a player does not meet entry conditions for an NPC teleport action, when they open the NPC, then the corresponding action button is hidden.
- `AC-012`: Given a player meets entry conditions, when they press `Vào map` or `Vào phó bản`, then the player is transferred immediately to the configured destination.

## Runtime Flow

### Normal interaction
1. Player enters interaction range.
2. Player clicks NPC.
3. Server/client validate range and NPC visibility state.
4. Action panel opens with configured eligible actions.
5. Player selects one action.

### Buy flow
1. Player selects `Mua vật phẩm`.
2. Shop UI shows configured item list and linh thạch prices.
3. Player chooses item and quantity.
4. Server validates NPC state, item config, currency, and bag capacity.
5. If valid: deduct linh thạch, grant item.
6. If invalid/full bag: reject full transaction.

### Sell flow
1. Player selects `Bán vật phẩm`.
2. UI shows only items that this NPC accepts.
3. Player selects item and quantity.
4. Server validates sell config and inventory ownership.
5. If valid: remove item, grant linh thạch.

### Entry flow
1. Player opens NPC.
2. Unlock-gated entry action is visible only if conditions pass.
3. Player presses entry action.
4. Server validates destination and eligibility.
5. Player transfers immediately.

### Timed NPC closing
1. Timed NPC counts down normally while visible.
2. At T-3 minutes, NPC enters `closing_soon`.
3. All actions except `Trò chuyện` are disabled.
4. Existing non-dialogue UIs close immediately.
5. At end time, NPC enters `hidden` and despawns.

## State / Lifecycle

- `visible`
- `closing_soon`
- `hidden`

Transitions:
- `hidden` -> `visible` (schedule/trigger start)
- `visible` -> `closing_soon` (3 minutes remaining)
- `closing_soon` -> `hidden` (time expires)
- `visible` -> `hidden` (force end / event cleanup if applicable)

## Rules And Invariants

- NPC interaction always uses shared interaction range.
- NPC interaction entity never becomes a combat actor.
- Unlimited stock means one player's purchase never reduces availability for another player.
- Timed NPC `closing_soon` must hard-stop all non-dialogue actions.
- Buy when bag full is always reject, never inbox fallback.
- Hidden unlock-gated actions are not shown pre-eligibility.
- Sell eligibility is both UI-filtered and server-validated.

## Edge Cases

- Player leaves interaction range while shop/dialogue UI is open: follow shared interaction range handling consistently.
- Timed NPC despawns exactly while a player clicks an action: server must validate NPC state at action resolution time.
- Player loses unlock condition after panel open but before entry click: server revalidates before transfer.
- Event-triggered NPC ends early by event cleanup: state may go directly to `hidden`; non-dialogue UIs must close safely.
- Multiple players buy simultaneously from same NPC: no locking by stock count should block valid purchases.

## Data / Config Requirements

- NPC template table:
  - npc_template_id
  - display_name
  - avatar
  - model
  - action_group_id or action list
- NPC spawn config:
  - template id
  - map id
  - position
  - availability type (`permanent` / `timed`)
  - trigger mode (`scheduled` / `event_trigger`)
  - start/end schedule or trigger binding
  - notification config
- Shop buy config per NPC:
  - item template id
  - linh thạch price
  - display order
- Shop sell config per NPC:
  - accepted item template id
  - buyback price
- Dialogue config:
  - static text
  - Q&A question/answer pairs
- Entry action config:
  - destination map/dungeon id
  - unlock conditions

## UI / UX Requirements

- NPC click opens a compact action panel.
- Shop buy UI shows item list and linh thạch prices only; no stock count is shown.
- Sell UI shows only accepted items.
- Timed NPC UI shows remaining time countdown.
- `closing_soon` should have clear visual indication.
- Hidden entry actions should not appear as disabled clutter; they are simply absent.

## Telemetry / Logs / Debug Needs

- Log NPC interaction start with npc id/template id and player id.
- Log timed NPC state transitions (`visible`, `closing_soon`, `hidden`).
- Log buy/sell transaction results and reject reasons.
- Log entry transfer attempts and rejection reasons.
- Debug visibility for current NPC availability state and remaining timer is recommended.

## Related Systems

- `features/npc-system.md` — canonical feature design source.
- `features/main-progression-quest-chain.md` — `talk_to_npc` objective integration.
- `features/home-cave-defense.md` — blueprint/NPC shop tie-ins where relevant.
- `shared-rules.md` — shared interaction range.

## Non-Blocking Follow-Ups

- Future support for extra NPC action button types can extend this requirement without changing current core behavior.
- Future quest-driven dialogue variants can be layered later if quest system needs it.
- Rescue/follow NPC behavior remains deferred and should be a separate feature if revisited.

## Blocking Questions

- None at game-design level for requirement drafting.

## Known Conflicts / Drift

- Current code/runtime coverage for timed NPC lifecycle and non-dialogue UI force-close is not yet verified.
- Current shop implementation, if any, must be checked to ensure linh thạch-only currency and unlimited-stock semantics match design.
- Map/dungeon entry gating may currently be implemented elsewhere; TD should ground whether NPC is dispatcher only or contains direct transition logic.

## Readiness Level

- Ready for TechDesign refinement: **yes**
- Ready for Dev handoff: **no** — pending code verification of NPC runtime/shop/timed state handling
- Ready for QA verification: **no** — implementation/spec not yet grounded enough
- Notes: TD should verify existing NPC interaction framework and split reusable base-NPC runtime from per-action modules.

## Handoff Checklist

- [x] No blocking design questions remain.
- [x] Acceptance criteria are testable.
- [x] Config/data impacts are listed.
- [x] Edge cases are listed.
- [x] Related docs are linked.
- [x] Target design and current runtime/evidence are clearly separated.
- [x] Readiness Level is filled consistently with `handoff_ready`.
- [ ] `handoff_ready` is set correctly — currently `false` pending TD refinement and code verification.
