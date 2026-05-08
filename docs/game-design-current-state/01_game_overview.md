# 01. Game Overview

## Game đang làm về gì

`PhamNhanOnline` hiện là một game online phong cách tu tiên / cultivation RPG với client Unity và server C# authoritative cho phần world runtime, combat, inventory, cultivation, alchemy, loot, và persistence. Người chơi đăng nhập, chọn hoặc tạo nhân vật, vào world, di chuyển giữa map/khu, đánh quái bằng skill, nhặt đồ, tăng tiến cảnh giới qua tu luyện và đột phá, sau đó mở thêm progression qua công pháp, skill, trang bị và luyện đan.

Source: `README.md`, `GameServer/Program.cs`, `GameServer/Services/WorldEntryService.cs`, `ClientUnity/PhamNhanOnline/docs/game-design-client-overview.md`

## Thể loại

- `Online RPG / cultivation RPG`
- `Top-down / side-view 2D Unity world presentation`
- `Server-authoritative shared world + private home instance`
- `Progression-heavy game loop` gồm combat, loot, cultivation, alchemy

Source: `GameServer/World/MapCatalog.cs`, `GameServer/World/MapManager.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneController.cs`

## Core loop hiện tại

1. Đăng nhập tài khoản và vào world với nhân vật đầu tiên khả dụng.
2. Di chuyển trong map hoặc ở home map/private home instance.
3. Chọn quái hoặc target tương tác, dùng basic skill hoặc skill đã equip.
4. Nhận reward progression khi hạ quái: cultivation, potential, item direct grant hoặc ground drop.
5. Mở inventory để dùng item, equip đồ, học công pháp, học recipe.
6. Về home để tu luyện, đột phá, phân phối potential, hoặc luyện đan qua practice session.
7. Quay lại combat với stat/skill/loadout mới.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs`, `GameServer/Network/Handlers/AttackEnemyHandler.cs`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Services/AlchemyCraftActionService.cs`

## Người chơi làm gì

- Đăng ký / đăng nhập tài khoản bằng credential kiểu password.
- Tạo đúng 1 nhân vật cho mỗi account trong build hiện tại.
- Vào world và nhận snapshot map/enemy/ground reward.
- Di chuyển bằng input local; client gửi movement intent, server tự tiến vị trí theo move speed hợp lệ.
- Tấn công target bằng slot skill.
- Đổi map bằng portal, đổi khu bằng zone panel nếu map hỗ trợ.
- Nhặt đồ từ ground reward hoặc vứt đồ từ inventory ra đất.
- Mặc / tháo trang bị.
- Dùng consumable, martial art book, pill recipe book.
- Chọn active martial art và bắt đầu/dừng cultivation.
- Đột phá cảnh giới khi đủ điều kiện progress hiện tại.
- Nâng chỉ số bằng unallocated potential.
- Luyện đan bằng recipe đã học, chờ session chạy, nhận notification và acknowledge kết quả.
- Nếu combat dead thì chỉ có flow hồi về home.

Source: `GameServer/Services/AccountService.cs`, `GameServer/Services/CharacterService.cs`, `GameServer/Network/Handlers/TravelToMapHandler.cs`, `GameServer/Network/Handlers/SwitchMapZoneHandler.cs`, `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`, `GameServer/Network/Handlers/UseItemHandler.cs`, `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`

## Các hệ thống đã có

- Account auth + password hashing + reconnect resume token.
- Character creation/selection/load snapshot.
- World entry, map instance join, visibility sync, enemy/world snapshot.
- Movement sync theo desired target, server clamp tốc độ.
- Combat skill pipeline data-driven qua `skills` + `skill_effects`.
- Enemy spawn/runtime/AI cơ bản, reward split theo contribution rule.
- Ground reward ownership/free-for-all/despawn.
- Inventory, equipment, item use.
- Martial art ownership + active martial art selection.
- Cultivation, breakthrough, potential allocation.
- Alchemy recipe list/detail/preview/craft/practice/notification.
- Game time config và runtime maintenance save/settlement.
- Combat death recovery về home.

Source: `GameServer/Extensions/ServiceCollectionExtensions.cs`, `GameServer/Runtime/GameLoop.cs`, `GameServer/Runtime/RuntimeMaintenanceService.cs`, `GameServer/Runtime/WorldRuntimeSettlementService.cs`

## Các hệ thống còn thiếu hoặc mới partial

- Quest: client có tab placeholder, server chưa thấy packet/service/domain flow hoàn chỉnh.
- Guild: client có tab placeholder, server chưa thấy gameplay flow.
- NPC gameplay thực: `Npc` hiện chủ yếu được client dùng như local interaction target cho portal giả trong home scene; chưa thấy server-side NPC content system đầy đủ.
- Smithing / Talisman crafting: client có panel, local portal, enum/practice type; gameplay flow hiện mới placeholder.
- Herb / soil / garden farming: server đã có service + schema khá sâu, nhưng chưa thấy packet handlers/client UI flow public để người chơi dùng.
- Breakthrough conditions config: có table/repository nhưng chưa thấy runtime dùng để validate breakthrough.
- Dedicated anti-cheat subsystem: chưa thấy service riêng; hiện chủ yếu là authoritative validation ở movement/range/state gate.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/LocalFixPortalPresenter.cs`, `GameServer/Services/HerbService.cs`, `GameServer/Repositories/BreakthroughConditionRepository.cs`

## Assumptions

- Assumption: game đang đi theo hướng "1 account = 1 nhân vật chính" ở phase hiện tại, vì `CreateCharacterAsync` chặn nếu account đã có nhân vật, dù API list character vẫn tồn tại để mở rộng sau.  
  Source: `GameServer/Services/CharacterService.cs`

- Assumption: home map/private home là trung tâm cho progression phi-combat như cultivation và alchemy, vì cả hai flow đều yêu cầu player đang ở private home instance.  
  Source: `GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Services/AlchemyCraftActionService.cs`, `GameServer/Services/PracticeService.cs`

- Assumption: current MVP tập trung vào combat + loot + progression trước, còn social/quest/live-event chưa phải phạm vi hoàn thiện.  
  Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`, `GameServer/Extensions/ServiceCollectionExtensions.cs`

## Inconsistencies / Need confirmation

- `database/initDatabase.sql` là schema bootstrap hiện hành cho nhiều hệ thống gameplay mới, nhưng không tự tạo các bảng nền như `accounts`, `characters`, `character_base_stats`, `character_current_state`; các bảng này lại có trong `database/phamnhan_online.sql`. Cần xác nhận pipeline setup DB chuẩn hiện tại là chạy file nào trước file nào.
- `GameConfigKeys` và `GameConfigValues` có key `item_drop.ground_spawn_offset_server_units` và `alchemy.practice_cancel_refund_progress_threshold`, nhưng `database/initDatabase.sql` chưa seed 2 row này vào `public.game_configs`.

Source: `database/initDatabase.sql`, `database/phamnhan_online.sql`, `GameServer/Config/GameConfigKeys.cs`, `GameServer/Config/GameConfigValues.cs`
