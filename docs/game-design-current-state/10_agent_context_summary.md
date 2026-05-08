# 10. Agent Context Summary

`PhamNhanOnline` hiện là một game online tu tiên với client Unity và server C# authoritative. Loop hiện có là: đăng nhập -> vào world -> di chuyển/chọn target -> đánh quái bằng skill -> nhặt/drop item -> tăng tiến qua equipment, martial art, cultivation, breakthrough, potential allocation, và alchemy. Home/private instance là nơi trọng tâm cho cultivation và alchemy. Combat, movement resolution, loot, inventory mutation, và progression state đều do server chốt.

Các system đã chạy tương đối đầy đủ gồm: auth + reconnect, character creation/load, world entry, map/zone travel, movement intent + server clamp, combat skill pipeline data-driven (`skills` + `skill_effects`), enemy spawn/runtime/reward, inventory/equipment/item use, martial arts, cultivation/breakthrough/potential, alchemy practice session, notification, và combat-death return home. Các system còn partial/prototype: herb/farming dù server data layer đã có, smithing/talisman chỉ có panel/local portal placeholder, quest/guild chưa có gameplay flow thật, breakthrough conditions table chưa được runtime dùng.

DB hiện chia làm hai lớp setup thực tế: `database/phamnhan_online.sql` chứa nền account/character/map/realm/game time; `database/initDatabase.sql` mở rộng skill/inventory/alchemy/enemy/reward/config. Persistent runtime chính nằm ở `characters`, `character_base_stats`, `character_current_state`, `player_items`, `player_skills`, `player_martial_arts`, `player_pill_recipes`, `player_practice_sessions`, `player_notifications`. Config chính nằm ở `map_*`, `realm_templates`, `skills`, `skill_effects`, `item_templates`, `enemy_*`, `pill_recipe_*`, `game_random_*`, `game_configs`, `game_time_state`.

Server architecture: `NetworkServer` + middleware + packet handlers gọi service/runtime layer. `WorldManager` giữ online players; `MapManager` giữ public/private/solo instances; `GameLoop` tick world 50ms; `RuntimeMaintenanceService` save dirty state, settle cultivation, complete alchemy sessions, refresh derived state. Inventory writes dùng advisory lock per player. Combat temporary state và many runtime instance objects chỉ nằm trong RAM, không persist DB.

Client architecture: `ClientRuntime` là service locator. World scene dùng `WorldSceneController` để inject presenters cho map, local player, remote players, enemies, targeting, portals, ground rewards. UI thật đang mạnh nhất ở inventory, cultivation, potential, combat HUD, zone switching, alchemy crafting, notification popups. `WorldMenuController` vẫn còn tab placeholder cho quest/guild và text placeholder cho một phần inventory/equipment legacy navigation.

Flow quan trọng:
- login -> `LoginPacket` -> account auth -> resume token
- enter world -> load snapshot -> join instance -> receive `MapJoined` + `WorldRuntimeSnapshot`
- move -> client gửi position-derived intent -> server clamp/tick move
- attack -> resolve equipped skill -> enqueue cast -> runtime settle -> impact packet
- enemy death -> contribution/reward -> direct grant hoặc ground reward
- pickup/drop -> server inventory mutation + ground reward sync
- cultivation -> chỉ ở private home + cần active martial art
- breakthrough -> roll chance ở server, ghi `breakthrough_attempts`
- alchemy -> preview inputs -> start practice -> complete async -> notification + reward

Giới hạn hiện tại cần nhớ:
- movement authority đã harden tốt hơn nhưng chưa thấy collision/path authority phức tạp
- AoE/all-map skill target types chưa support
- herb maturity recipe input bị defer
- generic practice types khác alchemy chưa end-to-end
- `game_configs` có key mismatch với seed SQL
- `initDatabase.sql` không phải bootstrap full schema one-shot

Những câu hỏi design nên bàn tiếp:
- quest/guild có nằm trong near-term scope không
- smithing/talisman nên ẩn hay làm MVP
- farming loop có phải ưu tiên tiếp theo cho alchemy economy không
- multi-character/account có còn là hướng đi không
- anti-cheat/telemetry có cần một lớp tập trung trước khi thêm economy/social

Primary sources:
- `GameServer/Program.cs`
- `GameServer/Services/WorldEntryService.cs`
- `GameServer/Runtime/GameLoop.cs`
- `GameServer/Runtime/WorldRuntimeSettlementService.cs`
- `GameServer/Runtime/CharacterCultivationService.cs`
- `GameServer/Services/AlchemyCraftActionService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Core/Application/ClientRuntime.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldService.cs`
- `database/phamnhan_online.sql`
- `database/initDatabase.sql`
