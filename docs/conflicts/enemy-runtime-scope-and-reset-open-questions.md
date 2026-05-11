---
doc_type: conflict_report
status: open
created_by: dev
created_at: "2026-05-11"
conflict_type: partial-runtime-coverage
affected_systems:
  - monsters
  - instances
  - rewards
affected_docs:
  - docs/game-design-wp/clarifications/enemy-design-clarification.md
affected_code:
  - GameServer/Runtime/EnemySystemTypes.cs
  - GameServer/World/MapInstance.Runtime.cs
  - GameServer/World/MonsterEntity.cs
affected_configs: []
severity: medium
requires_manager_decision: true
---

# Conflict Summary

Enemy data/runtime surface hiện rộng hơn phần behavior batch-1 đã verify rõ. Cụ thể: spawn modes `Objective` / `Manual` chưa thấy trigger path chắc chắn, boss out-of-combat reset behavior giữ nguyên HP chưa có design confirmation, và enemy không có skill chưa có fallback basic attack rõ ràng.

# Intended Design From Docs

- Enemy clarification xem timer spawn là default hợp lý, nhưng `Objective` / `Manual` cần xác định nội dung gameplay nào dùng.
- Boss reset HP behavior cần quyết định explicit trước khi canonicalize.
- Enemy không có skill có thể cần basic attack fallback bắt buộc để tránh combat giả.

# Current Implementation From Code / Config

- `EnemySpawnMode.Objective` và `EnemySpawnMode.Manual` có trong enum/data model.
- `MapInstance.UpdateSpawnGroupsUnsafe(...)` ở phần verify được chỉ cho thấy runtime path chắc chắn cho `EnemySpawnMode.Timer`.
- Boss out-of-combat path hiện `ReturnToPatrol(...)` và enqueue HP event nhưng không hồi full HP như non-boss path.
- Khi enemy không có skill, runtime log một lần là thiếu attack skill/basic attack; không thấy fallback basic attack thực thi trong code đã kiểm tra.

# Runtime / QA Evidence

- Verified từ `GameServer/World/MapInstance.Runtime.cs`
- Verified từ `GameServer/World/MonsterEntity.cs`
- Verified từ `GameServer/Runtime/EnemySystemTypes.cs`

# Why This Matters

- Designers có thể author content theo capability data model mà runtime hiện chưa thật sự support đầy đủ.
- Boss reset behavior ảnh hưởng trực tiếp độ khó/exploitability của boss encounter.
- Enemy vào combat nhưng không gây damage làm lệch cảm nhận gameplay và khó debug data issues.

# Affected Systems

- enemy spawning
- boss encounters
- combat integrity
- content authoring expectations

# Related Files

## Docs

- `docs/monsters/enemy-runtime-batch1.md`

## Code

- `GameServer/Runtime/EnemySystemTypes.cs`
- `GameServer/World/MapInstance.Runtime.cs`
- `GameServer/World/MonsterEntity.cs`

## Config / DB

- spawn-group rows using `spawn_mode`
- enemy templates missing attack skill/basic attack setup

# Suggested Options

## Option A: Fix code to match docs

Implement/verify runtime paths cho `Objective` / `Manual`, define boss reset rule rõ, và enforce basic attack fallback hoặc validation cấm enemy thiếu attack capability.

## Option B: Update docs because design changed

Thu hẹp canonical supported behavior về timer spawn only, boss-no-full-reset, và content rule yêu cầu enemy phải có attack skill configured.

## Option C: Need further investigation

Audit content data hiện có để xem `Objective` / `Manual` đã được author chưa, và test boss/reset expectation với design owner.

# Recommendation

Option C trước. Batch-1 canonical docs chỉ xác nhận phần timer spawn, combat loop cơ bản, và contribution reward path.

# Questions For Manager

- Spawn mode nào thực sự được phép dùng trong production content hiện tại?
- Boss mất aggro có nên hồi máu không?
- Có muốn runtime hard-fail hoặc validation fail khi enemy không có attack capability không?
