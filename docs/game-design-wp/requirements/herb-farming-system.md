---
doc_type: game_design_requirement
system_id: herb-farming-system
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: features/herb-farming-system.md
related_docs:
  - features/herb-farming-system.md
  - features/home-cave-defense.md
  - features/inbox-mail-system.md
  - features/multi-stage-crafting.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: false
---

# Hệ Thống Linh Thảo & Linh Dược — Requirement Spec

## Goal

Implement vòng lặp trồng linh thảo trong động phủ, thu hoạch cây sống vào túi, extract thành linh dược theo phẩm cấp, và dùng linh dược làm nguyên liệu crafting/alchemy. Hệ này phải hỗ trợ tiến trình theo thời gian thực, chịu ảnh hưởng bởi mật độ linh khí zone và phẩm cấp linh thổ.

## Source Design Summary

Canonical design lives in `features/herb-farming-system.md`.

Requirement-level clarifications locked for implementation:
- Linh thảo không mọc ngoài tự nhiên; nguồn là drop từ quái hoặc trồng trong vườn động phủ.
- Linh thảo có 4 trạng thái: mầm non, cây non, trưởng thành, ngàn năm.
- Chỉ cây trưởng thành và ngàn năm mới thu hoạch được.
- Thu hoạch tạo **item linh thảo sống** trong túi, có thời hạn hỏng.
- Extract biến item linh thảo sống thành **linh dược**; linh dược không có thời hạn.
- **Mỗi phẩm cấp linh dược là một item template riêng**, không dùng quality runtime trên cùng item.
- Recipe/alchemy phải gate theo item template identity, không dùng `required_herb_maturity` cũ.

## Target Design Summary

Player trồng linh thảo trong vườn động phủ, thu hoạch cây sống vào túi, sau đó extract thành linh dược để craft/alchemy. Toàn bộ vòng lặp chạy theo thời gian thực — cây lớn dù player offline. Chất lượng linh dược phụ thuộc trạng thái cây khi extract. Hệ tạo ra sự khác biệt kinh tế theo zone linh khí và phẩm cấp linh thổ.

Behavior cần đạt:
- Harvest/extract bị **reject hoàn toàn** nếu inventory không đủ chỗ — không có inbox overflow cho harvest/extract.
- Herb item hết hạn trong túi thì **xóa luôn**, không chuyển thành spoiled item.
- Mầm non để tái trồng đến từ **nhiều nguồn**: extract return, drop từ quái, mua NPC — tùy config.
- Số ô trồng per phẩm cấp bản vẽ động phủ là **fixed config** (data-driven, không cần runtime logic).

## Current Runtime / Evidence Snapshot

- **Confirmed**: `HerbService.cs` tồn tại trong backend; plot/soil/herb lifecycle entities đã có.
- **Confirmed**: `PlantExistingHerbAsync` hỗ trợ replant path.
- **Confirmed**: `AlchemyService.cs` tồn tại; hiện có `required_herb_maturity` guard đang block recipe loại này (cơ chế cũ).
- **Not yet confirmed**: Client/network handler cho garden interactions đã wired trong live build chưa.
- **Not yet confirmed**: Herb item expiry timer đã xử lý server-side khi player offline chưa.
- **Requires code verification**: Packet/UI handler cho plot actions (insert soil, plant, harvest, extract).

## Scope

### Must Implement
- Garden plot lifecycle: empty plot -> linh thổ inserted -> herb planted -> timed growth -> harvestable -> harvested item.
- Growth progression by real time.
- Growth speed modified by zone linh khí and linh thổ grade.
- Harvest restriction: chỉ cho phép ở trạng thái trưởng thành hoặc ngàn năm.
- Inventory herb item with decay timer.
- Extract flow from herb item -> one or more linh dược item outputs.
- Replant path using returned mầm non item.
- Quái drop linh thảo item by map/quái config.
- Inbox fallback when herb drop cannot enter inventory.

### Must Not Implement
- Wild herb nodes on map.
- Old recipe validation using `required_herb_maturity` as active player-facing mechanic.
- Runtime quality field on a shared linh dược item template.
- Balance values for timers, rates, yields, or drop rates beyond config wiring.

## Terminology

- `linh thảo`: living herb entity/item with growth state.
- `linh dược`: material item extracted from linh thảo, used by crafting/alchemy.
- `linh thổ`: soil item inserted into a garden plot; has grade and lifetime.
- `plot`: one garden slot in a player home cave.
- `harvest`: move a harvestable herb from plot to inventory as a living herb item.
- `extract`: convert a living herb item into one or more linh dược items, optionally returning a mầm non item.
- `zone linh khí`: server-defined density value for the zone where the home cave is placed.

## Functional Requirements

- `REQ-001`: The system shall support exactly 4 herb states: `seedling`, `young`, `mature`, `thousand_year`.
- `REQ-002`: State transition timing shall be config-driven per herb template.
- `REQ-003`: `thousand_year` is terminal; it does not auto-advance or auto-expire while planted.
- `REQ-004`: A plot shall reject planting unless valid linh thổ is currently inserted.
- `REQ-005`: Plot count shall be determined by home-cave blueprint grade/config.
- `REQ-006`: Herb growth shall progress by real time and shall continue while the player is offline.
- `REQ-007`: Growth speed shall be computed from base herb timing modified by zone linh khí and linh thổ grade modifier.
- `REQ-008`: When linh thổ lifetime expires while a herb is planted, herb growth shall pause immediately.
- `REQ-009`: When growth is paused due to expired linh thổ, the planted herb shall begin its survival countdown using the same decay model as an inventory living-herb item.
- `REQ-010`: Re-inserting valid linh thổ into a paused planted herb plot shall stop the survival countdown and resume growth from the current herb state/progress.
- `REQ-011`: Harvest shall only be allowed when herb state is `mature` or `thousand_year`.
- `REQ-012`: Successful harvest shall remove the herb from the plot immediately and create an inventory item representing that same living herb state.
- `REQ-013`: Living herb items in inventory shall have an expiration timestamp; on expiry they are deleted immediately — no spoiled-item state exists.
- `REQ-014`: Extract shall only be allowed on valid, non-expired living herb inventory items.
- `REQ-015`: Extract shall consume the living herb item and produce configured linh dược outputs.
- `REQ-016`: Extract outputs may include one or two linh dược item types per herb template/state.
- `REQ-017`: Extract yield quantity shall be config-driven per herb template and output slot.
- `REQ-018`: Linh dược output template identity shall depend on harvested herb state; at minimum `mature` and `thousand_year` can map to different output item templates.
- `REQ-019`: Extract may additionally return a mầm non item by fixed configured chance; this chance shall not vary by herb state unless future config explicitly extends it.
- `REQ-020`: Returned mầm non items shall be plantable directly through the replant flow without consuming a new seed item.
- `REQ-021`: Herb item drops from quái shall be config-driven per quái per map.
- `REQ-022`: If a herb drop reward from quái cannot fit into inventory, it shall follow shared inbox overflow behavior. Harvest and extract actions shall be **rejected entirely** if inventory is full — no inbox fallback for these two actions.
- `REQ-023`: Recipe/alchemy validation for this system shall use linh dược item templates/quantities as inputs; active requirement logic shall not depend on herb maturity fields.
- `REQ-024`: The system shall expose enough state for UI to show plot herb stage, next-stage remaining time, linh thổ remaining lifetime, and inventory herb remaining lifetime.
- `REQ-025`: Living herb items that reach their expiry timestamp shall be **deleted immediately**; no spoiled-item state exists.
- `REQ-026`: Mầm non items used for replanting may come from any configured source: extract return chance, quái drop config, or NPC purchase. All three sources use the same mầm non item template and the same replant flow.
- `REQ-027`: Plot count per home-cave blueprint grade shall be resolved from a **fixed data config table**; no runtime logic is required to compute it from blueprint object properties.

## Acceptance Criteria

- `AC-001`: Given a plot without linh thổ, when the player attempts to plant a herb, then planting is rejected and no herb state is created.
- `AC-002`: Given a planted herb below `mature`, when the player attempts harvest, then the action is rejected and the herb remains planted.
- `AC-003`: Given a `mature` planted herb, when the player harvests it, then the herb leaves the plot immediately and a living-herb inventory item of the same state is created.
- `AC-004`: Given a `thousand_year` planted herb, when no player action occurs, then it remains `thousand_year` indefinitely while planted.
- `AC-005`: Given linh thổ expires while a herb is planted, when expiration is processed, then growth pauses and the herb starts survival countdown instead of dying immediately.
- `AC-006`: Given a paused planted herb due to expired linh thổ, when valid new linh thổ is inserted, then survival countdown stops and growth resumes from prior progress.
- `AC-007`: Given a living-herb inventory item expires while the player is offline, when the player logs in and server settles timers, then the item is spoiled/removed and can no longer be extracted.
- `AC-008`: Given a valid living-herb inventory item, when the player extracts it, then the living herb is consumed and configured linh dược outputs are granted.
- `AC-009`: Given a `mature` herb item and a `thousand_year` herb item of the same herb family, when each is extracted, then their linh dược outputs may differ by item template according to config.
- `AC-010`: Given extract config includes mầm non return chance, when extract resolves, then the chance used is fixed by config and not derived from current herb state.
- `AC-011`: Given a quái drops herb while inventory is full, when loot resolves, then the herb reward is redirected to inbox per shared overflow rule.
- `AC-012`: Given an alchemy recipe requiring linh dược phẩm cấp cao, when a player provides only lower-tier linh dược template items, then recipe validation rejects the craft.
- `AC-013`: Given inventory is full, when the player attempts to harvest a mature herb from a plot, then the action is rejected and the herb remains planted.
- `AC-014`: Given inventory is full, when the player attempts to extract a living herb item, then the action is rejected and the herb item remains in inventory unchanged.
- `AC-015`: Given a living herb item reaches its expiry timestamp, when expiry is processed by the server, then the item is removed from inventory with no spoiled-item residual.
- `AC-016`: Given a player has a mầm non item regardless of source (extract return, quái drop, NPC purchase), when they plant it into a valid plot with active linh thổ, then planting succeeds using the standard replant flow.

## Runtime Flow

1. Player accesses home-cave garden plot.
2. Player inserts linh thổ into empty plot.
3. Player plants seed/mầm non into the plot.
4. Server creates/updates planted herb state and starts real-time progression.
5. Herb advances through `seedling` -> `young` -> `mature` -> `thousand_year` based on config and modifiers.
6. Player harvests at `mature` or `thousand_year`.
7. Server converts planted herb to living-herb inventory item with expiration timestamp.
8. Player later extracts that herb item.
9. Server consumes herb item, grants linh dược outputs, optionally returns mầm non item.
10. Granted linh dược items become available for crafting/alchemy recipe inputs.

## State / Lifecycle

### Planted Herb Lifecycle
- `empty_plot`
- `soil_inserted`
- `seedling_planted`
- `young_planted`
- `mature_planted`
- `thousand_year_planted`
- `growth_paused_soil_expired`
- `harvested_to_inventory`

### Inventory Herb Lifecycle
- `living_herb_valid`
- `living_herb_expired`
- `extracted`

### Soil Lifecycle
- `available`
- `inserted_active`
- `expired_under_plant`
- `replaced`

## Rules And Invariants

- A plot cannot host more than one herb at a time.
- A planted herb cannot progress without active linh thổ.
- `thousand_year` planted herbs do not auto-decay while still planted.
- Harvest preserves herb state into inventory.
- Extract is one-way; extracted herbs cannot be replanted.
- Replant uses returned mầm non item, not the extracted herb itself.
- Linh dược quality is represented by template identity, not by per-instance runtime quality state.
- Shared inbox overflow rule applies only to herb drops/rewards from quái; it does NOT apply to harvest or extract actions.
- Harvest and extract are rejected in full if inventory is full — player must free space first.
- Living herb items that expire are deleted immediately; no spoiled state exists.
- Mầm non source is irrelevant to the replant flow; all mầm non items of the same template are interchangeable.
- Plot count is resolved from fixed config table per blueprint grade; runtime logic must not compute it dynamically.

## Edge Cases

- Player harvests just before linh thổ expiry: harvest succeeds if herb was valid at action resolution time.
- Player attempts extract on an already expired herb item: reject, no outputs; the item will have been deleted at expiry processing time.
- Player inventory herb is near expiry and extract begins: expiry must be validated at action resolution, not only at UI open.
- Plot has expired soil and player does nothing for a long time: planted herb can spoil via survival countdown.
- Replacing soil on an empty plot should not create or restore a herb.
- If inventory is full when harvest or extract is attempted: reject entirely, no partial grant, no inbox fallback.
- If extract would produce multiple linh dược outputs and inventory is full: reject the entire extract action.
- Mầm non from any source (extract, quái drop, NPC) uses the same item template and replant flow — source traceability not required at runtime.

## Data / Config Requirements

- Herb template table:
  - herb template id
  - display name
  - stage durations
  - whether raw drops can appear from quái
  - extract outputs by state
  - extract mầm non return chance
  - living-herb inventory expiry duration
- Linh thổ template table:
  - soil template id
  - grade
  - growth speed modifier
  - lifetime duration
- Quái/map herb drop config:
  - map id
  - quái template id
  - herb template id/item template id
  - drop chance
  - drop state/template mapping if configurable
- Home-cave blueprint grade -> plot count config.
- Zone linh khí lookup/config source.
- Alchemy/crafting recipes must reference linh dược item template ids directly.

## UI / UX Requirements

- Garden UI must show per plot:
  - current herb state
  - next-stage remaining time
  - linh thổ remaining lifetime/state
- Inventory UI for living herbs must show:
  - herb state
  - remaining lifetime before spoil
- System should surface warning states for:
  - linh thổ nearing expiry
  - living herb nearing spoil
- Extract action should show expected outputs if preview data exists.

## Telemetry / Logs / Debug Needs

- Log plot mutations: soil inserted, herb planted, herb harvested, soil expired, soil replaced.
- Log herb item lifecycle: created from harvest, expired, extracted.
- Log extract resolution: output templates, quantities, mầm non return result.
- Log rejected actions with reason codes: no soil, invalid state, expired herb, invalid ownership, invalid recipe input.
- Debug visibility for current zone linh khí applied to a plot is recommended.

## Related Systems

- `features/herb-farming-system.md` — canonical feature design source.
- `features/home-cave-defense.md` — home cave blueprint and garden context.
- `features/inbox-mail-system.md` — overflow behavior.
- `features/multi-stage-crafting.md` — downstream crafting input usage.
- `shared-rules.md` — offline time-based activity rule and overflow rule.

## Non-Blocking Follow-Ups

- Verify whether current client/handler wiring for garden interactions is already present in live build.
- TechDesign refactor to fully remove old `required_herb_maturity` path from active alchemy validation.
- Optional future extension: per-state-specific mầm non return chance if design ever needs it.

## Blocking Questions

- None at game-design level for requirement drafting.
- Runtime wiring verification still recommended before implementation handoff is considered complete.

## Known Conflicts / Drift

- Legacy alchemy path still references `required_herb_maturity`; active implementation must migrate to linh dược template-based gating. Required migration, not optional.
- Garden server-side support exists (HerbService, AlchemyService) but client packet/UI wiring is not yet confirmed — non-blocking for TD/design work but must be resolved before Dev handoff is complete.
- Old behavior (if any) treating herb item expiry as spoiled-item is not canonical; expiry = immediate deletion.

## Readiness Level

- Ready for TechDesign refinement: **yes**
- Ready for Dev handoff: **no** — pending client/handler wiring verification and `required_herb_maturity` migration confirmation
- Ready for QA verification: **no** — implementation not yet complete
- Notes: TD should proceed with schema/recipe migration planning. Dev handoff gate is client wiring verification.

## Handoff Checklist

- [x] No blocking design questions remain.
- [x] Acceptance criteria are testable.
- [x] Config/data impacts are listed.
- [x] Edge cases are listed.
- [x] Related docs are linked.
- [x] Target design and current runtime/evidence are clearly separated.
- [x] Readiness Level is filled consistently with `handoff_ready`.
- [ ] `handoff_ready` is set correctly — currently `false` pending client wiring verification.
