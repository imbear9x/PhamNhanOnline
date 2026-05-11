---
title: World and map runtime configs batch 1
doc_type: config-contract
status: reviewed
owner: dev
source_of_truth:
  - GameServer/Config/GameConfigKeys.cs
  - GameServer/Config/GameConfigValues.cs
  - GameServer/Network/Handlers/TravelToMapHandler.cs
  - GameServer/World/MapManager.cs
  - GameServer/Runtime/EnemyRewardRuntimeService.cs
consumers:
  - docs/maps/map-instance-and-world-entry-runtime.md
  - docs/maps/portal-travel-runtime.md
  - docs/monsters/enemy-runtime-batch1.md
last_verified: 2026-05-11
tags:
  - second-brain
  - config-contract
  - maps
  - world
  - monsters
---

# Purpose

Gom các game config đã verify trực tiếp là ảnh hưởng đến batch-1 canonical docs cho map, portal, public-instance cleanup, và enemy ground reward timing.

# Contract

| Field | Type | Required | Default | Rules | Source |
|---|---|---|---|---|---|
| `world.portal_validation_buffer_server_units` | float | yes | `4` | Cộng thêm vào portal interaction radius khi server validate khoảng cách dùng portal. Giá trị âm bị clamp gián tiếp tại use-site bằng `MathF.Max(0f, ...)`. | `TravelToMapHandler` |
| `world.empty_public_instance_lifetime_seconds` | int | yes | `120` | Dùng để dọn public instance rỗng lâu sau khi không còn player. | `MapManager.CleanupExpiredInstances(...)` |
| `item_drop.enemy_drop_default_ownership_seconds` | int | yes | `30` | Default ownership duration cho ground drop từ enemy khi reward rule không override. | `EnemyRewardRuntimeService` |
| `item_drop.enemy_drop_default_free_for_all_seconds` | int | yes | `30` | Default FFA duration cho ground drop từ enemy khi reward rule không override. | `EnemyRewardRuntimeService` |
| `item_drop.ground_spawn_offset_server_units` | float | yes | `30` | Offset spawn ground reward quanh vị trí death/player-side rule trong reward runtime. | `EnemyRewardRuntimeService` |

# Validation Rules

- Runtime đọc các key này vào snapshot `GameConfigValues`.
- Thay đổi DB không hot-reload tự động; cần restart server để nạp lại snapshot config.
- Use-site hiện clamp một số giá trị về non-negative khi áp dụng vào runtime math, nhưng canonical expectation vẫn nên giữ dữ liệu cấu hình hợp lệ ngay từ nguồn.

# Load / Usage Flow

- Producer: `public.game_configs` -> `GameConfigKeys` / `GameConfigValues`
- Consumer:
  - portal travel validation
  - public-instance cleanup
  - enemy loot ownership / free-for-all timing
- Runtime impact:
  - nới hoặc siết khoảng cách dùng portal
  - kéo dài/rút ngắn tuổi thọ public instance rỗng
  - thay đổi cảm giác loot ownership và thời điểm FFA của enemy drop

# Drift Risks

- Nếu docs gameplay nói portal range hoặc loot timing khác, nhưng key runtime chưa đổi, người chơi sẽ thấy behavior theo config hiện hành chứ không theo doc.
- Nếu sau này thêm config cho boss reset/spawn modes mà không canonicalize riêng, doc enemy runtime sẽ dễ bị thiếu contract quan trọng.

# Verification

- Code paths checked:
  - `GameServer/Config/GameConfigKeys.cs`
  - `GameServer/Config/GameConfigValues.cs`
  - `GameServer/Network/Handlers/TravelToMapHandler.cs`
  - `GameServer/World/MapManager.cs`
  - `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- Example data checked:
  - default values từ code snapshot, không verify trực tiếp DB row cụ thể trong lượt này.
