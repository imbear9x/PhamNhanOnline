---
title: Enemy runtime batch 1
doc_type: system
status: reviewed
owner: dev
code_status: code-verified-with-open-design-questions
last_verified: 2026-05-11
source_of_truth:
  - GameServer/Runtime/EnemyDefinitionCatalog.cs
  - GameServer/World/MapInstance.Runtime.cs
  - GameServer/World/MapInstance.Combat.cs
  - GameServer/World/MonsterEntity.cs
  - GameServer/Runtime/EnemyRewardRuntimeService.cs
  - docs/implementation/extractions/enemy-runtime-extraction.md
related_docs:
  - docs/game-design-wp/clarifications/enemy-design-clarification.md
  - docs/maps/map-instance-and-world-entry-runtime.md
  - docs/data-design/config-contracts/world-map-runtime-configs-batch1.md
  - docs/conflicts/enemy-runtime-scope-and-reset-open-questions.md
related_code:
  - GameServer/World/MapManager.cs
  - GameServer/World/MapInstance.Events.cs
  - GameShared/Packets/Packets/WorldPackets.cs
  - GameServer/DTO/NetworkModelMapper.cs
tags:
  - second-brain
  - monsters
  - enemies
  - combat
  - rewards
---

# Summary

Canonical batch-1 runtime cho enemy spawn scope, patrol/combat loop, death/despawn, và reward distribution theo contribution.

**Graph links:** [[runtime-knowledge-map]] · [[server-runtime-architecture]] · [[skill-combat-runtime]] · [[map-instance-and-world-entry-runtime]]

## Purpose

Chuẩn hóa phần behavior enemy nào đã đủ evidence rõ trong code hiện tại, đồng thời tách riêng các chỗ data/design rộng hơn runtime đã confirm.

## Scope

- enemy catalog loading
- spawn-group selection theo runtime scope
- patrol/combat/death loop
- reward progression + loot distribution
- boss-completion related behavior ở mức hiện trạng runtime

## Non-goals

- không chốt toàn bộ tương lai của `Objective` / `Manual` spawn modes
- không khẳng định boss reset HP behavior là design cuối cùng

# Architecture / Flow

## Inputs

- enemy templates
- enemy skill loadouts
- reward rules + random tables
- spawn groups + spawn entries
- map instance config
- live players trong map instance
- item definitions + game random tables cho reward roll

## Outputs

- live enemy runtime entities trong `MapInstance`
- enemy spawn/despawn/hp/movement packets
- skill cast / impact runtime events
- progression rewards và ground rewards

## Runtime flow

1. `EnemyDefinitionCatalog` load template, loadout skill, reward rule, spawn group, spawn entry, instance config và random-table binding tại startup.
2. `MapManager` chọn spawn group cho mỗi instance theo runtime scope (`Any/Public/Private/Instance`) và, với public map, có thể lọc thêm theo `ZoneIndex`.
3. `MapInstance.Update(...)` chạy tick world theo thứ tự chính:
   - queue due skill events
   - update enemy states
   - update spawn groups
   - update ground rewards
   - update completion state
4. Timer spawn group fill quái ban đầu tới `MaxAlive`, sau đó respawn dần từng con theo weighted entry selection.
5. Enemy sống ở state `Patrol` hoặc `Combat`:
   - aggressive -> tự acquire target gần nhất trong aggro range
   - passive -> chỉ giữ target hợp lệ hiện có hoặc phản ứng sau khi bị đánh
6. Damage path ghi contribution + last hit, ép enemy vào combat, và nếu chết thì enqueue death event + respawn scheduling.
7. Enemy chết còn linger khoảng 2 giây trước khi despawn khỏi runtime collection.
8. `EnemyRewardRuntimeService` xử lý death queue sau tick: chia cultivation/potential theo contribution, sau đó roll direct grant hoặc ground drop theo reward rules.
9. Với configured instance có completion rule kiểu `KillBoss`, instance chỉ complete khi không còn boss sống và các boss spawn group đã hoàn tất initial fill.

# Rules / Invariants

- Enemy template phải có `MaxHp > 0`.
- Spawn group được spawn thực tế phải có ít nhất một spawn entry.
- Timer spawning là behavior đã verify rõ cho batch 1.
- Aggressive enemy tự tìm target; passive enemy không chủ động gây hấn nhưng vẫn vào combat khi bị đánh.
- Damage contribution là nền tảng để chia reward progression và chọn target loot rule downstream.
- Non-boss enemy có out-of-combat restore path về full HP nếu config/template cho phép.
- Boss completion runtime chỉ dựa trên việc boss còn sống hay không, không chờ corpse despawn biến mất.

# Data / Contracts

## Config

Xem `docs/data-design/config-contracts/world-map-runtime-configs-batch1.md` cho các key ground reward/loot timing đang tác động trực tiếp vào reward runtime.

## DB

Enemy runtime phụ thuộc các nhóm data chính:

- enemy templates
- enemy template skills
- enemy reward rules
- map enemy spawn groups
- map enemy spawn entries
- map instance configs
- game random tables

## Network / messages

- snapshot: `WorldRuntimeSnapshotPacket.Enemies`
- incremental: `EnemySpawnedPacket`, `EnemyDespawnedPacket`, `EnemyHpChangedPacket`, `EnemyMovementDecisionPacket`
- combat side effects: `SkillCastStartedPacket`, `SkillImpactResolvedPacket`

# Operational Notes

## Failure modes

- reward rule tham chiếu random table thiếu -> catalog build fail
- spawn group không có entry nhưng bị spawn -> runtime exception
- enemy không có skill -> runtime tiếp tục combat window nhưng chỉ log một lần là thiếu attack skill/basic attack
- target combat không còn hợp lệ -> enemy quay về patrol ở tick kế tiếp

## Monitoring / logs

Hiện có log khi enemy không thể tấn công do không có skill/basic attack configured, và log lỗi nếu reward runtime processing thất bại.

# Verification

## Code checked

- `GameServer/Runtime/EnemyDefinitionCatalog.cs`
- `GameServer/World/MapInstance.Runtime.cs`
- `GameServer/World/MapInstance.Combat.cs`
- `GameServer/World/MonsterEntity.cs`
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GameServer/World/MapManager.cs`

## Docs checked

- `docs/implementation/extractions/enemy-runtime-extraction.md`
- `docs/game-design-wp/clarifications/enemy-design-clarification.md`

## Gaps / drift

- `EnemySpawnMode.Objective` và `EnemySpawnMode.Manual` có trong data model nhưng chưa được verify có runtime trigger path rõ trong batch này; xem `docs/conflicts/enemy-runtime-scope-and-reset-open-questions.md`.
- Boss out-of-combat hiện quay về patrol nhưng giữ nguyên HP; chưa đủ evidence để xem đây là design rule cuối cùng.
- Runtime không cho thấy fallback basic attack thực sự khi enemy không có skill.
- 2-second corpse linger hiện được ghi nhận là current runtime behavior, chưa canonicalize như player-facing rule bất biến.
