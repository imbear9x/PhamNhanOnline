---
title: Inventory Bag System — Reviewer Handoff
doc_type: handoff
status: Ready
owner: reviewer
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: review
---

# Goal

Mời Reviewer review implementation bag system sau khi dev hoàn thiện thêm enforcement và fix reviewer-required issues.

# Implementation Summary

Dev đã hoàn thiện thêm các phần còn thiếu chính của inventory bag system so với slice trước:

- Fix reviewer-required issues:
  - `BagState.UsedSlots` không còn tính item inventory đã hết hạn.
  - `EnsureDefaultBagAsync` đã được làm idempotent hơn để tránh fail duplicate-key khi nhiều request đầu phiên cùng tạo bag mặc định.
- Active capacity rejection:
  - `PickupGroundRewardHandler`: pre-check bag capacity trước khi claim item từ ground reward; nếu full thì reject với `InventoryFull` và cancel claim runtime.
  - `CraftService`: pre-check bag capacity trước khi consume input / grant output craft; nếu không fit thì fail sớm, không tiêu nguyên liệu.
  - `HerbService.HarvestHerbAsync`: pre-check bag capacity cho guaranteed harvest outputs; nếu full thì reject bằng `InventoryFull` trước khi harvest commit.
- Passive overflow hook:
  - `AlchemyPracticeService`: khi practice completion reward không fit bag, reward không bị mất; thay vào đó completion notification vẫn được tạo với payload reward và message báo đã chuyển sang thông báo nhận thưởng.
- Error/result polish:
  - `UpgradeBag` dùng `BagUpgradeTargetInvalid` cho target grade không hợp lệ / không cao hơn grade hiện tại.
  - `UpgradeBag` dùng `BagUpgradeCurrencyInsufficient` khi thiếu linh thạch.

Lưu ý: passive overflow hiện mới được nối ở practice completion path; chưa có inbox item-claim storage thực thụ, hiện dùng notification payload làm overflow sink cho slice này.

# Files / Modules Touched

## Bag core
- `GameServer/Services/BagService.cs`
- `GameServer/DTO/BagDtos.cs`
- `GameServer/Repositories/PlayerItemRepository.cs`
- `GameServer/Repositories/PlayerBagRepository.cs`
- `GameServer/Repositories/BagGradeConfigRepository.cs`
- `GameServer/Entities/PlayerBagEntity.cs`
- `GameServer/Entities/BagGradeConfigEntity.cs`
- `GameServer/Entities/PhamnhanOnlineDb.PlayerBags.cs`

## Character / packet / wiring
- `GameServer/Services/CharacterService.cs`
- `GameServer/Network/Handlers/GetInventoryHandler.cs`
- `GameServer/Network/Handlers/GetBagStateHandler.cs`
- `GameServer/Network/Handlers/UpgradeBagHandler.cs`
- `GameServer/Extensions/ServiceCollectionExtensions.cs`
- `GameShared/Packets/Packets/CharacterPackets.cs`
- `GameShared/Models/BagStateModel.cs`
- generated packet/model files under `GameShared/Generated/...`

## Active/passive capacity enforcement
- `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
- `GameServer/Services/CraftService.cs`
- `GameServer/Services/HerbService.cs`
- `GameServer/Services/AlchemyPracticeService.cs`

## Config / schema
- `GameServer/Config/GameConfigValues.cs`
- `GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs`
- `database/initDatabase.sql`
- related migration files already created in current bag worktree

# Build / Test Results

Focused verification run:
- `dotnet build GameServer/GameServer.csproj -v minimal` ✅ pass

Observed warning only:
- `CS8032` from `Humanizer.Analyzers.NamespaceMigrationAnalyzer` due missing `System.Collections.Immutable 9.0.0.0` in local analyzer load path.
- Đây là warning môi trường analyzer, không block build output của `GameServer`.

Không chạy full end-to-end runtime suite trong lượt này.

# DB / Schema / Seed Changes

Bag feature worktree đang bao gồm:
- new table `bag_grade_configs`
- new table `player_bags`
- seed bag grades 1..4
- bag upgrade currency config
- backfill/default bag init cho character cũ/mới

Trong lượt hoàn thiện này **không thêm schema mới ngoài phần bag đã có trước đó**; chủ yếu sửa runtime/service logic.

# Packet / Broadcast / Runtime Changes

- `GetInventory` response tiếp tục trả `BagState`.
- `GetBagState` / `UpgradeBag` packets vẫn là entry chính cho bag state và upgrade.
- `PickupGroundReward` giờ có thêm fail path `InventoryFull` trước khi chuyển item ground vào inventory.
- Alchemy practice completion notification có thể mang message overflow bag, báo reward được chuyển qua notification payload thay vì add trực tiếp inventory.

# QA Notes

Reviewer nên rà kỹ các case sau:
1. Character có item expired trong inventory:
   - `GetInventory.BagState.UsedSlots`
   - `GetBagState.UsedSlots`
   phải không tính item expired.
2. Character chưa có `player_bags` row:
   - spam đồng thời `GetInventory` / `GetBagState`
   - không được fail duplicate key trên `player_bags`.
3. Ground reward full bag:
   - pickup phải fail `InventoryFull`
   - reward runtime không bị consume mất.
4. Craft full bag:
   - fail sớm, không mất nguyên liệu.
5. Herb harvest full bag:
   - guaranteed-output case phải fail trước commit.
6. Practice completion full bag:
   - completion không crash
   - notification có reward payload + message overflow
   - reward không vào inventory trực tiếp.

# Test Scope Completed By Dev

Đã tự verify ở mức code/build:
- compile pass toàn bộ `GameServer`
- rà call sites chính cho active reject:
  - ground reward pickup
  - direct craft execution
  - herb harvest
- rà passive overflow path cho alchemy practice completion
- rà error code mapping bag upgrade

Chưa tự verify manual/runtime:
- concurrency repro thực tế trên DB cho bag init race
- packet roundtrip thực tế với client
- harvest probabilistic edge cases (output chance < 100%)
- full acceptance matrix cho tất cả item grant call sites ngoài các path trên

# Known Gaps / Risks / Blockers

1. **Passive overflow chưa phải inbox item-claim hoàn chỉnh**
   - Hiện đang dùng notification payload làm overflow sink ở `AlchemyPracticeService`.
   - Nếu product/spec yêu cầu mailbox claimable item rows thực thụ, cần slice tiếp theo.

2. **Herb harvest mới pre-check cho guaranteed outputs**
   - Với output chance < 100%, pre-check hiện không cover toàn bộ tổ hợp random outcomes để tránh false reject.
   - Đây là lựa chọn an toàn tối thiểu cho path active; Reviewer nên xác nhận mức chấp nhận được theo spec hiện tại.

3. **Chưa cắm enforcement cho mọi call site add item toàn server**
   - Lượt này tập trung các flow bag-related / được nêu rõ trong pending list.
   - Có thể còn passive/active grant path khác ngoài bag slice này chưa hook capacity.

4. **Analyzer warning môi trường**
   - `Humanizer.Analyzers` warning không block build nhưng còn tồn tại.

# Suggested Reviewer Focus

- Tính đúng của slot counting với expired items.
- Idempotency / race handling khi self-heal bag row.
- Tính atomic của active reject paths (không mất input khi full bag).
- Mức phù hợp của passive overflow implementation hiện tại so với TechDesign wording `inbox/notification path`.
