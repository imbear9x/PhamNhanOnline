---
doc_type: game_design_feature
system_id: world-boss-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/event-system.md
  - features/death-penalty.md
  - features/player-interaction-group.md
  - shared-rules.md
requires_code_verification: true
---

# Hệ Thống Boss Thế Giới — Feature Draft

## Goal

Tạo các boss mạnh xuất hiện trên map thường hoặc map riêng, cho phép nhiều player cùng tham gia tiêu diệt. Người last hit nhận loot chính; toàn bộ người tham gia nhận reward tham gia (nếu có). Boss có thể xuất hiện thường trực hoặc theo event.

## Design Summary

Boss thế giới là enemy entity đặc biệt với HP cao, behavior phức tạp, và loot giá trị. Xuất hiện trên map thường hoặc map riêng — ai đủ điều kiện vào map đều có thể tham chiến. Không có điều kiện tham gia riêng ngoài điều kiện vào map. Loot chính thuộc về người last hit; reward tham gia (nếu có) cấp cho tất cả người đã tham chiến. Boss có thể có respawn hoặc không — config per boss. Backend đã có enemy template; behavior cụ thể của boss chưa được cấu hình — sẽ bổ sung khi data design.

## Scope

### In Scope
- Boss xuất hiện trên map thường hoặc map riêng
- Nhiều player cùng tham chiến
- Last hit nhận loot chính
- Tất cả người tham chiến nhận reward tham gia (nếu config)
- Respawn config per boss
- Boss có thể là nội dung của event (boss_event)

### Out Of Scope
- Behavior cụ thể của boss (skill, pattern, AI) — data design / TechDesign
- Balance HP, damage, loot pool — data design
- Điều kiện tham gia riêng ngoài điều kiện vào map

## Player-Facing Rules

### Tham chiến
- Ai vào được map boss xuất hiện đều có thể tấn công — **không có điều kiện tham gia riêng**.
- PvP trong map boss: theo PvP mode của map đó (normal hoặc chaos — xem PvP State Taxonomy).

### Targeting / Aggro runtime contract
- Boss dùng cùng proactive attack framework với enemy thường.
- Ưu tiên target hiện tại:
  1. player **vừa đánh boss** (qua `pendingAggroOverride` trong runtime hiện tại)
  2. nếu target đó không còn hợp lệ / không còn trong tầm → chọn **player gần nhất trong range**
- Không dùng aggro table tích lũy, không dùng first-seen target, không random combat target.
- Khi **không có ai đánh** và **không có ai trong aggro range**:
  - boss aggressive: patrol / đi tuần theo route hoặc ngẫu nhiên trong vùng patrol
  - nếu lệch khỏi patrol area: tự quay về vùng patrol
- Thiết kế world boss bổ sung một behavior riêng được user chốt ở layer feature: nếu map/boss config cho phép và không có target hợp lệ gần đó, boss **có thể chủ động đuổi đánh ngẫu nhiên mục tiêu trong map**. Rule này là config per boss, per map; TechDesign/Dev sẽ cần chỉ rõ cách graft vào framework hiện tại ở requirement stage.

### Combat reset / out-of-combat
- Boss thoát combat khi không resolve được target hợp lệ trong combat radius, ví dụ:
  - target chết
  - target offline / disconnect
  - target rời instance / map
  - target ra khỏi range
- Boss không despawn chỉ vì mất combat.
- Nếu có out-of-combat restore timer theo runtime hiện tại: boss **ReturnToPatrol** nhưng **giữ nguyên HP**; enemy thường mới restore full HP.
- Boss despawn/reset hoàn toàn theo config/event rule riêng của boss hoặc map.
### Loot
- **Người last hit**: nhận loot chính của boss (theo Ownership/Drop Rights shared rule — priority window cho người last hit).
- **Tất cả người tham chiến** (đã hit boss ít nhất 1 lần): nhận reward tham gia nếu config — cấp tự động hoặc claim tùy config per boss.
- Loot rơi đất (last hit loot): nếu túi đầy — không nhặt được, item vẫn đất trong looting window, báo túi đầy. Không vào inbox.
- Reward tham gia (cấp tự động): nếu túi đầy — vào inbox theo shared overflow rule.

### Respawn
- Boss có thể respawn hoặc không — **config per boss**.
- Nếu có respawn: cooldown config per boss.
- Boss theo event: respawn theo rule của event đó.
- Boss phase trigger được phép dùng cả 3 loại: **HP / time / event** — config per boss, per map.
### Xuất hiện
- Map thường: boss spawn tại vị trí config, ai đi qua đều gặp.
- Map riêng: boss là nội dung chính của map — vào map là có boss.
- Boss có thể là nội dung của **boss_event** — xuất hiện trong thời gian event.

## System States
- `alive`: boss đang sống, có thể tấn công.
- `dead`: boss đã chết, loot rơi ra, reward tham gia cấp.
- `respawning`: đang cooldown hồi sinh (nếu có respawn).

## Main Flows

### Flow 1 — Boss thường trực
1. Boss spawn tại vị trí config trên map.
2. Player vào map, phát hiện boss, tấn công.
3. Nhiều player tham chiến — ai hit đều được tính tham gia.
4. Boss chết → loot rơi ra (priority window cho last hit), reward tham gia cấp (nếu có).
5. Respawn cooldown (nếu config) → boss hồi sinh.

### Flow 2 — Boss event
1. Event bắt đầu → boss spawn theo config event.
2. Player tham gia qua event UI.
3. Flow tương tự boss thường — last hit loot, reward tham gia.
4. Event kết thúc → boss despawn nếu còn sống.

### Flow 3 — Nhiều player tranh last hit
1. Boss HP về thấp — nhiều player tranh đánh đòn cuối.
2. Server ghi nhận chính xác player last hit.
3. Last hit player nhận priority window cho loot.
4. Tất cả player đã hit boss nhận reward tham gia.

## Edge Cases
- Player vào map sau khi boss đã chết: không tính tham gia, không nhận reward.
- Player offline trong lúc boss chết: nếu đã hit boss trước đó — reward tham gia (loại hệ thống cấp tự động) gửi inbox khi đăng nhập lại. Last hit loot rơi đất theo looting window bình thường — nếu hết window thì mất.
- Boss chết trong map chaos PvP: loot vẫn theo Ownership/Drop Rights rule — last hit có priority window.
- Nhiều player cùng hit đòn cuối (cùng tick): server xử lý deterministic — 1 player được tính last hit.
- Boss mất target vì target rời map / disconnect / chết: runtime resolve target mới theo cùng proactive attack framework; nếu không có target hợp lệ thì boss về patrol.
- Participation reward rule không cố định toàn hệ: có boss chỉ cần hit 1 lần, có boss cần threshold khác, có boss không có participation reward — config per boss/reward set.
## Data / Config Needs
- Boss template: ID, tên, HP, behavior ref, loot pool, reward tham gia config → DB (enemy template đã có)
- Boss spawn config: map ID, vị trí, respawn flag, respawn cooldown → DB
- Reward tham gia: có/không, loại reward, cấp tự động hay claim → DB per boss
- Priority window duration cho last hit loot → `game_configs` (dùng chung Ownership/Drop Rights rule)

## UI / UX Notes
- Boss HP bar hiển thị toàn server (hoặc chỉ player trong map) khi boss alive.
- Thông báo khi boss spawn (có thể broadcast hoặc local — config per boss).
- Thông báo khi boss chết: ai last hit, reward tham gia.

## Related Systems
- **Event System** (`features/event-system.md`): boss_event dùng framework này.
- **Ownership / Drop Rights** (`shared-rules.md`): last hit nhận priority window cho loot.
- **Death Penalty** (`features/death-penalty.md`): chết khi đánh boss = death penalty bình thường.
- **PvP State Taxonomy** (`shared-rules.md`): PvP trong map boss theo mode của map.
- **Inbox** (`features/inbox-mail-system.md`): reward tham gia cấp tự động khi túi đầy.

## Key Decisions
1. Không có điều kiện tham gia riêng — vào được map là đánh được.
2. Last hit nhận loot chính (priority window theo Ownership/Drop Rights).
3. Tất cả người đã hit nhận reward tham gia — nếu config.
4. Respawn: config per boss, có thể không respawn.
5. Boss có thể xuất hiện trên map thường hoặc map riêng, hoặc là nội dung của event.

## Open Questions
- [x] Boss HP bar: chỉ hiển thị cho người trong map.
- [x] Thông báo boss spawn: broadcast toàn server đang online — config per boss.
- [x] TechDesign clarified minimum requirement-stage behavior spec must explicitly define: aggro/leash/reset, despawn/reset conditions, and skill cadence/targeting contract.
- [x] Boss phase pattern được phép dùng cả 3 loại trigger: HP / time / event — config per boss, per map.
- [x] Dev confirmed current runtime contract: pendingAggroOverride -> nearest valid target in range; timer-based round-robin skill cadence; boss out-of-combat return-to-patrol keeps HP.
## Known Conflicts / Drift
- Runtime enemy/boss framework hiện không có random combat target hay full aggro-table semantics. Nếu muốn world boss có behavior "đuổi ngẫu nhiên mục tiêu trong map khi không có target gần/hit", TechDesign/Dev cần chỉ rõ đây là extension per-boss/per-map so với framework hiện tại.
- `requires_code_verification: true` — backend đã có enemy template và Dev đã mô tả runtime contract mức cao, nhưng requirement stage vẫn cần TechDesign/Dev khóa rõ integration boundary cho boss-specific behavior extensions.
## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.