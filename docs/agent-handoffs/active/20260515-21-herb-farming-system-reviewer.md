---
title: Reviewer Handoff — Herb Farming System Implementation
doc_type: handoff
status: Done
owner: reviewer
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: review
queue_id: 21
feature_key: herb-farming-system
handoff_type: implementation
source_handoff: docs/agent-handoffs/active/20260514-herb-farming-system-dev.md
response_to: docs/agent-handoffs/active/20260514-herb-farming-system-dev.md
iteration: 3
---

# Goal

Reviewer xác minh implementation herb farming của handoff `#2` đã bám đúng TechDesign đã chốt ở `#19`: flow 2 bước Harvest/Extract, expiry inventory herb, packet/handler garden, và contract lỗi/message code.

# Implementation Summary

Dev đã hoàn thiện slice herb farming chính trong GameServer theo spec:

1. **Schema + entity/runtime foundation**
   - Thêm `player_herbs.expire_at` trong `database/initDatabase.sql`.
   - Thêm `ExpireAt` vào `GameServer/Entities/PlayerHerbEntity.cs`.
   - Mở rộng runtime herb với `ExpireAtUtc`.
   - Bổ sung config keys:
     - `herb.inventory_expiry_seconds`
     - `herb.expiry_sweep_interval_seconds`

2. **Enum/spec alignment**
   - `HerbGrowthStage` đổi `Perfect -> ThousandYear`, thêm `Young`.
   - Giữ alchemy maturity enum riêng, chỉ bỏ guard maturity cũ trong `AlchemyService` theo spec.
   - Thêm `MessageCode` garden range `6000–6011`.

3. **HerbService refactor sang flow 2 bước**
   - `HarvestAsync(...)`: chỉ nhổ herb khỏi plot và đưa herb entity vào inventory, set `ExpireAt`, không grant item output.
   - `ExtractHerbAsync(...)`: chỉ chạy cho herb đang ở inventory, check expiry, roll output, check bag capacity trên full proc set, grant item, xóa herb entity.
   - `ExtractHerbAsync(...)` nay trả `HerbExtractionResult` gồm:
     - `Items`
     - `MamNonReturned`
   - Thêm helper/service surface:
     - `GetGardenPlotHerbStateAsync(...)`
     - `GetNextStageRemainingSecondsAsync(...)`
   - Chuẩn hóa nhiều nhánh lỗi sang `GameException(MessageCode.*)` thay cho `InvalidOperationException` ở các flow garden chính.

4. **Packet/model/handler garden**
   - Tạo `GameShared/Packets/Packets/HerbPackets.cs`.
   - Tạo `GameShared/Models/GardenPlotStateModel.cs`.
   - Thêm mapper `ToGardenPlotStateModel(...)` trong `NetworkModelMapper`.
   - Tạo và register 6 handlers:
     - `GetGardenPlotsHandler`
     - `InsertSoilHandler`
     - `PlantHerbSeedHandler`
     - `PlantExistingHerbHandler`
     - `HarvestHerbHandler`
     - `ExtractHerbHandler`
   - `GetGardenPlotsHandler`, `PlantHerbSeedHandler`, `PlantExistingHerbHandler` nay trả `NextStageRemainingSeconds` thực tế thay vì placeholder `0`.
   - `ExtractHerbHandler` nay trả đúng `MamNonReturned` từ service.
   - Các handler chính đã có `catch (GameException)` để trả `Code = ex.Code`.

5. **Expiry cleanup runtime**
   - Tạo `GameServer/Runtime/HerbExpiryBackgroundService.cs`.
   - Gọi từ `RuntimeMaintenanceService` để sweep inventory herbs đã hết hạn.

# Files / Modules Touched

## Modified
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/Config/GameConfigValues.cs`
- `GameServer/DTO/NetworkModelMapper.cs`
- `GameServer/Entities/PlayerHerbEntity.cs`
- `GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs`
- `GameServer/Extensions/ServiceCollectionExtensions.cs`
- `GameServer/Repositories/PlayerHerbRepository.cs`
- `GameServer/Runtime/AlchemySystemTypes.cs`
- `GameServer/Runtime/RuntimeMaintenanceService.cs`
- `GameServer/Services/AlchemyService.cs`
- `GameServer/Services/HerbService.cs`
- `GameShared/Messages/MessageCode.cs`
- `database/initDatabase.sql`

## New files
- `GameShared/Models/GardenPlotStateModel.cs`
- `GameShared/Packets/Packets/HerbPackets.cs`
- `GameServer/Runtime/HerbExpiryBackgroundService.cs`
- `GameServer/Network/Handlers/GetGardenPlotsHandler.cs`
- `GameServer/Network/Handlers/InsertSoilHandler.cs`
- `GameServer/Network/Handlers/PlantHerbSeedHandler.cs`
- `GameServer/Network/Handlers/PlantExistingHerbHandler.cs`
- `GameServer/Network/Handlers/HarvestHerbHandler.cs`
- `GameServer/Network/Handlers/ExtractHerbHandler.cs`

## Generated after packet registration
- `GameShared/Generated/PacketGenerator/PacketGenerator/PacketGeneratedRegistry.g.cs`
- các generated packet `.g.cs` tương ứng cho packet herb mới

# Build / Test Result

Focused build đã chạy:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: **pass**, `0 error`

Observed warning còn tồn tại:
- `CS8032` từ `Humanizer.Analyzers.NamespaceMigrationAnalyzer`
- thiếu assembly `System.Collections.Immutable, Version=9.0.0.0`
- Đây là warning môi trường/analyzer, không phải compile error từ diff herb.

Không có automated integration test trong lượt này; verification hiện ở mức code-path inspection + compile.

# DB / Schema / Seed Changes

Có thay đổi DB/config seed trong `database/initDatabase.sql`:
- thêm cột `public.player_herbs.expire_at timestamp without time zone NULL`
- thêm `ALTER TABLE ... ADD COLUMN IF NOT EXISTS expire_at`
- thêm game config:
  - `herb.inventory_expiry_seconds`
  - `herb.expiry_sweep_interval_seconds`

Không thêm schema `herb_templates.survival_seconds_without_soil` hay `inventory_expiry_seconds` per-template; cố ý theo TechDesign validation `#19`.

# Packet / Broadcast / Runtime Contract Changes

## Packet surface mới
- `GetGardenPlotsPacket` / `GetGardenPlotsResultPacket`
- `InsertSoilPacket` / `InsertSoilResultPacket`
- `PlantHerbSeedPacket` / `PlantHerbSeedResultPacket`
- `PlantExistingHerbPacket` / `PlantExistingHerbResultPacket`
- `HarvestHerbPacket` / `HarvestHerbResultPacket`
- `ExtractHerbPacket` / `ExtractHerbResultPacket`

## Runtime behavior
- `Harvest` và `Extract` đã tách đúng 2 bước.
- Inventory herb có `ExpireAt`; herb expired trong inventory bị sweep silent ở maintenance loop.
- `ExtractHerbResultPacket` có trả `MamNonReturned` thực tế từ service.
- `GardenPlotStateModel` có `NextStageRemainingSeconds` và `HerbExpireAtUnixMs`.
- Không thêm broadcast packets; vẫn request/response only theo spec.

# QA Notes / Retest Guidance

Reviewer nếu pass thì QA/reviewer nên retest tối thiểu:

1. **GetGardenPlots**
   - cave hợp lệ trả đủ plots
   - plot có herb đang trồng trả đúng `HerbStage`, `SoilRemainingSeconds`, `NextStageRemainingSeconds`
   - herb trong inventory không còn gắn plot, có `HerbExpireAtUnixMs`

2. **InsertSoil / PlantHerbSeed / PlantExistingHerb**
   - plot không có soil → lỗi `GardenPlotNoSoil`
   - plot đã có herb → lỗi `GardenPlotAlreadyHasHerb`
   - soil/seed/item không hợp lệ hoặc không thuộc player → lỗi inventory/garden code đúng contract

3. **HarvestAsync**
   - herb đang trồng đủ stage → chuyển sang inventory, plot bị clear, không grant output item
   - herb chưa đủ stage → fail đúng message code

4. **ExtractHerbAsync**
   - herb inventory chưa hết hạn → grant output, trả đúng `Items`
   - herb inventory hết hạn → xóa herb và fail `GardenHerbExpired`
   - full bag khi proc outputs có item → fail `GardenInventoryFull`, không grant nửa chừng
   - `MamNonReturned` phản ánh đúng nhánh roll hiện tại

5. **Expiry sweep**
   - herb inventory quá hạn bị xóa silent ở maintenance loop

# Test Scope Completed By Dev

- Đọc handoff `#2`, TechDesign herb farming, và validation authority `#19`
- Audit code hiện trạng herb/inventory/runtime/handler
- Implement foundation + packet/handler + cleanup runtime
- Polish contract gaps sau build đầu tiên:
  - tính `NextStageRemainingSeconds`
  - trả `MamNonReturned`
  - map thêm `GameException` cho garden flows
- Focused compile verification cho `GameServer`

# Known Gaps

- Chưa có automated test coverage cho garden packets/runtime.
- Một số `InvalidOperationException` nội bộ vẫn còn trong `HerbService` cho các trường hợp data corruption / missing definition (`soil template`, `herb template`, quantity invalid, owner null). Dev giữ lại vì đây thiên về invariant/server-data fault hơn là lỗi business do client action.
- Chưa chạm vào flow herb drop từ quái; đúng out-of-scope theo handoff `#2`.
- Generated packet files mới xuất hiện sau build; reviewer nên xác nhận chúng được expected track trong repo workflow hiện tại.

# Risks / Blockers

- Không có blocker hiện tại cho reviewer.
- Rủi ro chính còn lại:
  1. thiếu runtime/integration test cho packet flows thực tế
  2. phần phân tách business error vs invariant fault trong `HerbService` có thể còn cần reviewer rà kỹ thêm ở các nhánh ít gặp
  3. warning analyzer `CS8032` vẫn tồn tại ngoài phạm vi feature
