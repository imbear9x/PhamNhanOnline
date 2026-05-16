---
title: Reviewer — Death Penalty Blocker Response Implementation
doc_type: handoff
status: Done
owner: reviewer
source_agent: dev
last_updated: 2026-05-16
source_design_doc: docs/game-design-wp/requirements/death-penalty.md
source_tech_design_doc: docs/tech-design/death-penalty.md
expected_output: review
queue_id: 40
feature_key: death-penalty
handoff_type: reviewer
source_handoff: docs/agent-handoffs/active/20260516-39-death-penalty-dev-blockers-response.md
response_to: docs/agent-handoffs/active/20260516-39-death-penalty-dev-blockers-response.md
iteration: 1
---

# Tóm tắt implementation

Đã hoàn tất dev follow-up cho `#39 death-penalty-dev-blockers-response` theo authority TechDesign đã chốt:
- thêm persisted foundation cho `pending_permanent_deletion`
- thêm persisted foundation cho `next_tribulation_at_utc`
- gate pending-delete tại data/world-entry path
- thêm confirm permanent deletion packet/result + handler + hard-delete orchestration tối thiểu an toàn
- thêm combat-status source tagging và skill-only clear contract
- thêm runtime death-penalty mutation tối thiểu cho combat-dead transition:
  - clear `Skill` statuses
  - realm 19+ giảm `next_tribulation_at_utc`
  - realm 1–18 giảm lifespan qua `LifespanBonus`
  - nếu lifespan hết hạn thì mark `PendingPermanentDeletion`
- chặn `ReturnHomeAfterCombatDeath` khi character đã pending permanent deletion

# Files / modules đã chạm

## Persistence / DTO / model
- `GameServer/Entities/Character.cs`
- `GameServer/Entities/CharacterCurrentState.cs`
- `GameServer/DTO/CharacterDto.cs`
- `GameServer/DTO/CharacterCurrentStateDto.cs`
- `GameShared/Models/CharacterModel.cs`
- `GameShared/Models/CharacterCurrentStateModel.cs`
- `GameServer/DTO/NetworkModelMapper.cs`
- `database/initDatabase.sql`

## Runtime / services
- `GameServer/Runtime/CombatStatusRuntime.cs`
- `GameServer/Runtime/SkillExecutionService.cs`
- `GameServer/World/MonsterEntity.cs`
- `GameServer/Runtime/CharacterRuntimeService.cs`
- `GameServer/Services/WorldEntryService.cs`
- `GameServer/Services/CharacterService.cs`
- `GameServer/Services/ItemService.cs`
- `GameServer/Services/DeathPenaltyService.cs`
- `GameServer/Services/PermanentCharacterDeletionService.cs`

## Repositories purge helpers
- `GameServer/Repositories/BreakthroughAttemptRepository.cs`
- `GameServer/Repositories/PlayerMartialArtRepository.cs`
- `GameServer/Repositories/PlayerSkillRepository.cs`
- `GameServer/Repositories/PlayerSkillGrantSourceRepository.cs`
- `GameServer/Repositories/PlayerSkillLoadoutRepository.cs`
- `GameServer/Repositories/PlayerNotificationRepository.cs`
- `GameServer/Repositories/PlayerPracticeSessionRepository.cs`
- `GameServer/Repositories/PlayerPillRecipeRepository.cs`
- `GameServer/Repositories/PlayerCaveRepository.cs`
- `GameServer/Repositories/PlayerGardenPlotRepository.cs`
- `GameServer/Repositories/PlayerHerbRepository.cs`
- `GameServer/Repositories/PlayerItemRepository.cs`

## Network / packet
- `GameShared/Packets/Packets/CharacterPackets.cs`
- `GameServer/Network/Handlers/GetCharacterDataHandler.cs`
- `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`
- `GameServer/Network/Handlers/ConfirmPermanentCharacterDeletionHandler.cs`
- `GameShared/Messages/MessageCode.cs`
- `GameServer/Extensions/ServiceCollectionExtensions.cs`
- `GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs`
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/Config/GameConfigValues.cs`

# Build / test kết quả

Đã chạy build focused:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Kết quả: **passed** (`0 Error(s)`)

Warning còn lại:
- `CS8032 Humanizer.Analyzers.NamespaceMigrationAnalyzer`
- nguyên nhân cũ: thiếu `System.Collections.Immutable, Version=9.0.0.0`
- không phải warning mới do slice này tạo ra

# DB / schema / seed changes

Đã có / đã dùng trong chain `#39`:
- thêm `characters.pending_permanent_deletion boolean NOT NULL DEFAULT false`
- thêm `character_current_states.next_tribulation_at_utc`
- thêm seed config:
  - `death.lifespan_penalty_seconds`
  - `death.tribulation_penalty_seconds`

# Packet / broadcast / runtime contract changes

## Packet mới
- `ConfirmPermanentCharacterDeletionPacket` `[224]`
- `ConfirmPermanentCharacterDeletionResultPacket` `[225]`

## Runtime changes
- `CombatStatusCollection` đã có source tagging:
  - `Skill`
  - `Talisman`
  - `Formation`
  - `External`
- death resolve chỉ clear `CombatStatusSourceType.Skill`
- `CharacterRuntimeService` hiện trigger `DeathPenaltyService` khi transition sang `CombatDead`
- `ReturnHomeAfterCombatDeath` reject nếu character đã `PendingPermanentDeletion`
- `GetCharacterData` reject với `MessageCode.CharacterPendingPermanentDeletion` nhưng vẫn trả snapshot payload cho notice flow
- `EnterWorld` reject trước recovery/attach nếu pending-delete
- `GetCharacterList` vẫn hiển thị character; client đọc flag `CharacterModel.PendingPermanentDeletion`

# QA / reviewer notes

Reviewer cần kiểm kỹ các điểm sau:
1. `GetCharacterList` không filter pending-delete character khỏi list
2. `GetCharacterData` / `EnterWorld` / `ReturnHomeAfterCombatDeath` gate đúng message code
3. `ConfirmPermanentCharacterDeletion` xóa được graph tối thiểu theo thứ tự phụ thuộc hiện có
4. các path add status đã source-tag đúng cho scope hiện có:
   - player skill shield/stun/stat modifier
   - monster skill shield/stun/stat modifier
5. `DeathPenaltyService` chỉ mutate phần được TD cho phép:
   - không inline trigger tribulation battle
   - không chạy drop item/lingstone trong follow-up này

# Test scope đã cover

Đã cover bằng compile/build + code audit targeted:
- packet id uniqueness pass
- DI registration compile pass
- confirm-delete flow compile pass
- pending-delete gate compile pass
- death penalty runtime mutation compile pass

# Known gaps / deferred follow-up

1. **Tribulation trigger integration** chưa được nối runtime thực.
   - Theo TD authority của `#39`, phần này được phép defer.
   - Hiện contract chỉ persist/floor `next_tribulation_at_utc`.

2. **Drop linh thạch / drop item khi chết** từ scope gốc `#37` chưa được nối trong blocker-response slice này.
   - `#39` tập trung phần authority blockers đã chốt.
   - Nếu reviewer yêu cầu, cần follow-up riêng hoặc reopen against `#37` scope gốc.

3. `CharacterRuntimeService.NotifyDeathTransitionIfNeeded(...)` hiện gọi `DeathPenaltyService.ApplyOnCombatDeathAsync(player).GetAwaiter().GetResult()`.
   - Build pass nhưng đây là điểm rủi ro sync-over-async trong runtime path.
   - Reviewer nên đánh giá có cần Required Fix để đưa sang async-safe orchestration hay không.

# Risks / blockers

- Rủi ro lớn nhất còn lại là sync-over-async tại hook death transition.
- Hard-delete graph hiện đã có orchestration tối thiểu an toàn hơn trước, nhưng chưa có integration test DB thật để chứng minh không còn FK nhánh ẩn.
- Không có blocker compile hiện tại.
