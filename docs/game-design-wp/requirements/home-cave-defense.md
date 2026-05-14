---
doc_type: game_design_requirement
system_id: home-cave-defense
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/home-cave-defense.md
related_docs:
  - features/home-cave-defense.md
  - features/spirit-beast.md
  - features/death-penalty.md
  - features/spirit-sense.md
  - features/crafting-talisman-formation.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: false
---

# Hệ Thống Động Phủ / Công Động Phủ / Cướp Bóc — Requirement Spec

## Goal

Implement hệ động phủ cá nhân có thể triển khai ra map thế giới, bị phát hiện qua Thần Thức Quan, bị tấn công bằng Bùa Phá Phủ, và khi bị phá có thể làm rơi ngẫu nhiên tài sản lưu trong động phủ trong một looting window PvP ngắn. Hệ này phải hỗ trợ cả loop sở hữu/cất trữ/phòng thủ của chủ nhà lẫn loop công phủ/cướp bóc rủi ro cao của người đi công.

## Source Design Summary

Canonical design lives in `features/home-cave-defense.md`.

Requirement-level clarifications locked for implementation:
- Mỗi account có 1 động phủ private ban đầu; khi mở động phủ thế giới thì private home ban đầu biến mất vĩnh viễn.
- Mỗi người chỉ có 1 động phủ active tại một thời điểm.
- Động phủ thế giới chỉ nhìn thấy / tương tác / tấn công được nếu người chơi vượt ngưỡng Thần Thức Quan của động phủ.
- Mở động phủ ra thế giới cần cast time 1 phút; trong lúc cast không ai vào được và không bị tấn công.
- Tấn công động phủ cần Bùa Phá Phủ đúng phẩm cấp; cuộc công tồn tại theo thời gian hiệu lực của bùa.
- Tối đa 10 người trong map Cửa Động Phủ; đủ người thì người đến sau bị chặn.
- Nếu hết thời gian công mà chưa phá xong: cổng hồi đầy HP ngay và toàn bộ attacker bị teleport ra ngoài.
- Nếu phá thành công: item trong động phủ rơi ngẫu nhiên theo Structure Loot Drop Rate; phần còn lại đính vào bản vẽ trả về chủ.
- Looting window kéo dài 1 phút, PvP tự do, attacker không được rời map trong thời gian này; offline trong map làm rơi toàn bộ đồ vừa nhặt.

## Target Design Summary

Chủ nhà dùng động phủ như căn cứ phát triển và kho tài sản có rủi ro. Khi triển khai ra map thế giới, động phủ trở thành mục tiêu PvP bất đối xứng: chỉ người đủ Thần Thức Quan mới thấy và công được; người công phải tiêu hao Bùa Phá Phủ, chịu death penalty nặng hơn, và tranh chấp free-for-all với nhau. Nếu công thành công, chỉ tài sản lưu trong động phủ mới có thể bị rơi/cướp; đồ trong túi nhân vật không bị cướp trực tiếp bởi cơ chế phá phủ.

Behavior cần đạt:
- Chủ nhà có thể thu dọn động phủ khi không bị công; thu dọn trả toàn bộ nội dung vào bản vẽ, không mất gì.
- Động phủ đang bị công thì khóa thu dọn.
- Người không đủ Thần Thức Quan chỉ thấy vùng mờ vô danh, không thấy tên, không tương tác, không tấn công.
- Chủ nhà luôn nhận thông báo bị công dù online hay offline, và nhận linh thạch đền bù khi có lượt công hợp lệ.
- Nếu động phủ sụp: phe thủ bị đẩy ra ngoài ngay; attacker vào looting window 1 phút để tranh nhau đồ rơi.
- Hết looting window: tất cả bị tele ra map random; người sống giữ đồ nhặt được.

## Current Runtime / Evidence Snapshot

- **Not yet confirmed**: hệ động phủ private/world, map cửa động phủ, và trạng thái công phủ đã có schema/runtime riêng trong code hay chưa.
- **Not yet confirmed**: Bùa Phá Phủ, compensation linh thạch, cooldown attacker, và 10-player cap đã có code hay chưa.
- **Not yet confirmed**: looting window 1 phút với rule “không rời map / offline làm rơi đồ” đã có runtime support hay chưa.
- **Requires code verification**: toàn bộ state machine động phủ, detection by spirit-sense threshold, teleport / map-lock / collapse flow, storage attachment to blueprint.

## Scope

### Must Implement
- Private-home initial state for new account.
- World deployment flow via blueprint at valid world cell with 1-minute cast time.
- Single active home-cave invariant.
- Spirit-sense visibility gate for seeing/interacting/attacking world home caves.
- Home-cave attack initiation via same-grade phá phủ charm.
- Door-defense combat map with 10-player max occupancy.
- Active attack timer based on charm duration.
- Collapse success flow, random structure loot drop, 1-minute looting window, forced teleports.
- Cleanup/pack-back-to-blueprint flow when not under attack.
- Storage theft limitation: only structure-stored assets, never direct theft from player inventory.
- Guest invitation restrictions.
- Owner compensation payout on valid attack start.

### Must Not Implement
- Exact balance values for HP, rates, cooldowns, compensation ratios.
- Detailed room-by-room UI layout.
- Deep anti-abuse / anti-alt-account protections beyond explicit rules here.
- Open-map private-home attackability.

## Terminology

- `private home`: initial absolute-safe home cave state granted to a new account.
- `world home cave`: deployed cave visible on world map if spirit-sense threshold is met.
- `blueprint`: non-tradeable structure carrier containing home cave layout/content when packed.
- `door-defense map`: combat map for breaking into the cave.
- `phá phủ charm`: required consumable to initiate an attack on a target cave of matching grade.
- `looting window`: 1-minute post-collapse PvP window where dropped structure loot can be picked up.
- `Structure Loot Drop Rate`: shared rule controlling random drop-out of structure-stored assets on collapse.

## Functional Requirements

- `REQ-001`: Each account shall start with exactly one private home cave in a fully safe state.
- `REQ-002`: Deploying a world home cave shall permanently replace the initial private home state.
- `REQ-003`: A player/account shall not have more than one active home cave at the same time.
- `REQ-004`: World home cave deployment shall only succeed on valid configured world cells that are currently unoccupied by another home cave.
- `REQ-005`: World home cave deployment shall require a 1-minute cast time.
- `REQ-006`: During deployment cast time, the cave shall not be attackable and no player shall be allowed to enter it.
- `REQ-007`: Home cave grade from blueprint shall determine at minimum: spirit-sense threshold, inner capacity scaling, and gate HP scaling.
- `REQ-008`: A player who does not meet the spirit-sense threshold shall see only an unnamed blurred area and shall not be able to inspect, interact with, or attack the cave.
- `REQ-009`: A player who meets the spirit-sense threshold shall be able to see the cave normally and initiate allowed interactions.
- `REQ-010`: Packing/cleanup shall only be allowed when the cave is not under attack.
- `REQ-011`: Successful cleanup shall attach the full cave contents/layout back into the blueprint with no item loss.
- `REQ-012`: Home cave attack initiation shall require one phá phủ charm of matching target grade.
- `REQ-013`: Starting a valid attack shall consume one charm, start the attack state, notify the owner, and pay owner compensation in linh thạch.
- `REQ-014`: Owner compensation value shall be data-driven and sourced from the charm economy rule, not arbitrary per runtime event.
- `REQ-015`: While a cave is under attack, cleanup shall be locked.
- `REQ-016`: The door-defense map shall enforce a maximum of 10 players; entrants beyond capacity shall be rejected.
- `REQ-017`: Home cave attack PvP mode inside the contested defense context shall be free-for-all.
- `REQ-018`: If the charm timer expires before cave destruction, the attack shall end immediately, gate HP shall restore to full, and all attackers shall be teleported out.
- `REQ-019`: If attackers destroy the cave defense to collapse threshold, all defenders inside the cave shall be teleported out immediately.
- `REQ-020`: On collapse, cave entry from outside shall be closed to additional entrants.
- `REQ-021`: On collapse, structure-stored assets shall roll random drops according to Structure Loot Drop Rate; non-dropped remainder shall be attached back into the blueprint.
- `REQ-022`: Looting window duration shall be exactly 1 minute.
- `REQ-023`: During the looting window, only players already in the map on the attacker side shall be eligible to loot dropped cave assets.
- `REQ-024`: During the looting window, players who picked up loot shall not be allowed to leave the map early.
- `REQ-025`: If a player goes offline while inside the looting window map, all loot they picked up in that window shall immediately drop back to the ground.
- `REQ-026`: If a player dies inside the looting window, normal death-penalty rules shall apply to items currently carried.
- `REQ-027`: When the looting window ends, all remaining players in the map shall be teleported to a random public map; surviving players keep loot they still carry.
- `REQ-028`: After looting-window resolution, the blueprint containing the remaining cave contents shall be returned to the owner inventory.
- `REQ-029`: Player inventory items currently carried by the owner shall never be directly lootable via cave collapse; only structure-stored assets are in scope.
- `REQ-030`: Guest access shall require friendship plus explicit owner invitation while the guest is standing near the cave.
- `REQ-031`: Guest invitation shall be disabled while the cave is under attack.
- `REQ-032`: Guests may move freely inside but may not access storage, take items, or use management/admin functions.
- `REQ-033`: Home-defense pets that survive collapse shall return to the owner pet bag; dead pets shall return in sleeping/recovering state.
- `REQ-034`: Owner logout shall not pause or cancel an ongoing attack.
- `REQ-035`: If the owner dies while defending, they shall not be allowed to respawn until all attackers leave the relevant area.
- `REQ-036`: If the cave is destroyed while the owner is offline, login recovery shall place the owner outside the destroyed cave, not back inside it.
- `REQ-037`: Room/storage capacity inside the cave shall scale by blueprint grade using data-driven steps; room types themselves remain fixed.

## Acceptance Criteria

- `AC-001`: Given a new account, when initial home state is provisioned, then the account has a private home cave that cannot be attacked.
- `AC-002`: Given a player starts deploying a world cave on a valid cell, when the 1-minute cast is still in progress, then no one can attack or enter that cave.
- `AC-003`: Given a target cave is already present on a cell, when another player tries to deploy onto the same cell, then deployment is rejected.
- `AC-004`: Given a player lacks the required spirit-sense threshold, when they pass by a world cave, then they see only a blurred anonymous area and cannot interact with it.
- `AC-005`: Given a player has the correct phá phủ charm and meets visibility requirements, when they initiate attack, then the charm is consumed, the cave enters attack state, and the owner receives compensation and notification.
- `AC-006`: Given a cave is under attack, when the owner attempts cleanup, then cleanup is rejected.
- `AC-007`: Given the attack timer expires before collapse, when timeout resolves, then gate HP becomes full immediately and all attackers are teleported out.
- `AC-008`: Given attackers destroy the cave to collapse threshold, when collapse triggers, then all defenders inside are teleported out and the looting window starts.
- `AC-009`: Given collapse has triggered, when a structure-stored asset is evaluated, then it either drops to the map according to Structure Loot Drop Rate or remains attached to the returned blueprint.
- `AC-010`: Given a player in the looting window picked up dropped loot, when they try to leave the map before the minute ends, then the exit is blocked.
- `AC-011`: Given a player in the looting window goes offline, when disconnect resolves, then all loot they picked up in that window drops immediately.
- `AC-012`: Given the looting window reaches 1 minute, when the timer ends, then all remaining players are teleported to a random map and the owner blueprint is returned with undropped contents attached.
- `AC-013`: Given an invited guest is inside a cave, when they try to open storage or use admin functions, then the action is rejected.
- `AC-014`: Given the cave is destroyed while the owner is offline, when the owner logs back in, then they appear outside the old cave location with destroyed-home recovery handling.
- `AC-015`: Given the door-defense map already has 10 players, when an 11th player tries to enter, then entry is rejected.

## Runtime Flow

### Deploy world cave
1. Player has blueprint and chooses a valid world cell.
2. Player starts 1-minute deployment cast.
3. During cast, cave is reserved/not attackable/not enterable.
4. Cast completes; world cave becomes active and visible based on spirit-sense checks.
5. Initial private home state is retired permanently.

### Normal cleanup
1. Owner requests cleanup while cave is not under attack.
2. Server validates safe state.
3. Layout and contents are attached back into blueprint.
4. World cave disappears from map.
5. Blueprint returns to owner inventory.

### Start attack
1. Attacker detects cave by spirit-sense threshold.
2. Attacker uses matching-grade phá phủ charm.
3. Server consumes charm and starts attack timer.
4. Owner receives notification and compensation.
5. Door-defense map contest becomes active.

### Attack timeout failure
1. Attack timer reaches zero before collapse.
2. Server ends attack state.
3. Gate restores to full HP immediately.
4. All attackers are teleported out.
5. Cave returns to normal world state.

### Successful collapse
1. Attackers break defense to collapse threshold.
2. Defenders inside are teleported out immediately.
3. Outer gate closes to new entry.
4. Structure assets roll random drops.
5. 1-minute looting window starts.
6. Attackers fight/loot under free-for-all rules.
7. Offline during this window drops picked loot instantly.
8. Window ends; all are teleported out to random map.
9. Owner receives blueprint back with undropped remainder attached.

## State / Lifecycle

- `private_home_safe`
- `world_cave_deploying`
- `world_cave_normal`
- `world_cave_under_attack`
- `world_cave_collapse_window`
- `world_cave_packed_or_destroyed`

Transitions:
- `private_home_safe` -> `world_cave_deploying`
- `world_cave_deploying` -> `world_cave_normal`
- `world_cave_normal` -> `world_cave_under_attack`
- `world_cave_under_attack` -> `world_cave_normal` (timeout / attackers fail)
- `world_cave_under_attack` -> `world_cave_collapse_window` (successful collapse)
- `world_cave_collapse_window` -> `world_cave_packed_or_destroyed`
- `world_cave_normal` -> `world_cave_packed_or_destroyed` (owner cleanup)

## Rules And Invariants

- Only one active cave per player/account at a time.
- Private initial home is absolutely safe and never attackable.
- World cave visibility and attackability are both gated by spirit-sense threshold.
- Cleanup is impossible while under attack.
- Matching-grade phá phủ charm is mandatory for starting a valid attack.
- Cave collapse never directly steals items from a player inventory; only structure-stored assets are in scope.
- Looting window duration is fixed at 1 minute.
- Attack timeout fully restores gate HP and ejects attackers.
- Owner compensation always triggers on valid attack start, regardless of eventual attack outcome.
- Max occupancy of the defense map is 10.
- Guests can visit but can never access storage or admin actions.

## Edge Cases

- Owner is offline during attack start: attack continues normally; notification is queued/shown on login if needed.
- Invited guest is inside when attack begins: treat according to contested-map rules; they do not gain storage rights.
- Charm timer expires during active combat at the gate: timeout rule still wins immediately; attackers are ejected and gate fully heals.
- Owner attempts cleanup exactly as an attacker starts a valid attack: server must serialize and deterministically choose one valid state transition.
- Crash/restart during looting window: server must be able to recover remaining timer, dropped loot state, and picked-loot ownership/rules safely.
- Blueprint return fails due to inventory full: must use a deterministic fallback path consistent with non-tradeable critical ownership assets; TD must define exact delivery path.

## Data / Config Requirements

- Deployment cast time.
- Home-cave blueprint grades.
- Spirit-sense threshold per cave grade.
- Gate HP / defense scaling per grade.
- Internal storage capacity scaling per grade.
- Valid deployment maps/cells.
- Phá phủ charm grade table and duration per grade.
- Compensation ratio/value config tied to charm grade/economy.
- Attacker cooldown config after each attack attempt.
- Door-defense map max occupancy.
- Looting window duration.
- Collapse drop rate source via Structure Loot Drop Rate.
- Respawn/teleport target config for defenders and post-window ejection.

## UI / UX Requirements

- Deployment UI must show valid/invalid placement feedback and 1-minute cast progress.
- Non-qualified observers must see only blurred anonymous cave presence.
- Attack-start UI must confirm charm consumption and show attack duration.
- Owner alert must be high-priority and visible online/offline.
- Door-defense UI should expose gate state and active contested status.
- Looting window UI should show countdown and warn that leaving is blocked.

## Telemetry / Logs / Debug Needs

- Log cave deployment start/finish/fail.
- Log attack start with attacker id, target id, charm grade, compensation paid.
- Log cleanup attempts and rejection reasons.
- Log collapse resolution, dropped-assets roll results, and blueprint-return contents.
- Log looting-window pickup, forced drop on offline, and final teleport-out.
- Log gate-heal-on-timeout and attacker ejection events.

## Related Systems

- `features/home-cave-defense.md` — canonical feature design source.
- `features/spirit-beast.md` — defense pets.
- `features/crafting-talisman-formation.md` — formations/traps on defense side.
- `features/death-penalty.md` — stronger death penalty overlays.
- `features/spirit-sense.md` — visibility threshold.
- `shared-rules.md` — Structure Loot Drop Rate and related shared rules.

## Non-Blocking Follow-Ups

- Clarify exact fallback delivery path if returned blueprint cannot fit inventory.
- Clarify whether invited guests present at attack start count as defenders or neutral entrants in the contested map runtime.
- TechDesign may split this requirement into sub-specs: deployment/state machine, defense map runtime, collapse/loot resolution.

## Blocking Questions

- None at game-design level for requirement drafting.

## Known Conflicts / Drift

- Feature design is clear, but current runtime evidence is weak; major parts of this system require code verification before Dev handoff.
- Blueprint return path on inventory-full is not yet explicitly grounded in an existing shared rule because blueprint is a critical non-tradeable ownership asset.
- Attacker cooldown is specified in feature intent but not yet locked to concrete persistence/runtime behavior.

## Readiness Level

- Ready for TechDesign refinement: **yes**
- Ready for Dev handoff: **no** — pending runtime/code verification and TD state-machine/spec split
- Ready for QA verification: **no** — implementation/spec not yet grounded enough
- Notes: TD should first turn this into a technical state-machine + map-runtime spec before any Dev handoff.

## Handoff Checklist

- [x] No blocking design questions remain.
- [x] Acceptance criteria are testable.
- [x] Config/data impacts are listed.
- [x] Edge cases are listed.
- [x] Related docs are linked.
- [x] Target design and current runtime/evidence are clearly separated.
- [x] Readiness Level is filled consistently with `handoff_ready`.
- [ ] `handoff_ready` is set correctly — currently `false` pending TD refinement and code verification.
