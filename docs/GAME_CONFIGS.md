# Cấu Hình Game

## Mục đích

Bảng `public.game_configs` dùng để chứa các giá trị gameplay có thể tinh chỉnh, thay cho phần trước đây đang hardcode trong `GameServer`.

## Cấu trúc bảng

- `config_key`: khóa cấu hình duy nhất.
- `config_value`: giá trị cấu hình lưu dạng chuỗi, parse sang kiểu số trong server.
- `description`: mô tả ngắn ý nghĩa của key.
- `created_at`: thời điểm tạo row.
- `updated_at`: thời điểm cập nhật row gần nhất.

## Mapping key DB và property code

| `config_key` | Property trong code | Kiểu | Ý nghĩa | Giá trị mặc định |
|---|---|---:|---|---:|
| `network.reconnect_resume_window_seconds` | `GameConfigValues.NetworkReconnectResumeWindowSeconds` | `int` | Số giây server giữ resume token/session sau khi mất kết nối để reconnect. | `3` |
| `world.portal_validation_buffer_server_units` | `GameConfigValues.WorldPortalValidationBufferServerUnits` | `float` | Buffer cộng thêm vào `interaction_radius` khi server validate dùng portal. | `4` |
| `combat.skill_range_grace_buffer_units` | `GameConfigValues.CombatSkillRangeGraceBufferUnits` | `float` | Buffer cộng thêm vào `CastRange` khi server validate target combat. | `12` |
| `combat_death.return_home_recovery_ratio` | `GameConfigValues.CombatDeathReturnHomeRecoveryRatio` | `double` | Tỷ lệ HP/MP hồi lại khi player combat dead và được đưa về home. | `0.8` |
| `item_drop.player_drop_ownership_seconds` | `GameConfigValues.ItemDropPlayerOwnershipSeconds` | `int` | Số giây item vứt từ inventory còn ownership riêng cho người vứt. | `10` |
| `item_drop.player_drop_free_for_all_seconds` | `GameConfigValues.ItemDropPlayerFreeForAllSeconds` | `int` | Số giây tồn tại thêm sau giai đoạn ownership của item vứt từ inventory. | `50` |
| `item_drop.enemy_drop_default_ownership_seconds` | `GameConfigValues.ItemDropEnemyDefaultOwnershipSeconds` | `int` | Ownership mặc định cho item rơi từ enemy nếu reward rule không cấu hình riêng. | `30` |
| `item_drop.enemy_drop_default_free_for_all_seconds` | `GameConfigValues.ItemDropEnemyDefaultFreeForAllSeconds` | `int` | Free-for-all mặc định cho item rơi từ enemy nếu reward rule không cấu hình riêng. | `30` |
| `item_drop.ground_spawn_offset_server_units` | `GameConfigValues.ItemDropGroundSpawnOffsetServerUnits` | `float` | Khoảng lệch spawn item trên mặt đất tính theo đơn vị server khi văng reward ra map. | `30` |
| `world.empty_public_instance_lifetime_seconds` | `GameConfigValues.WorldEmptyPublicInstanceLifetimeSeconds` | `int` | Số giây một public instance rỗng được giữ trước khi bị hủy. | `120` |
| `cultivation.potential_per_cultivation_point` | `GameConfigValues.CultivationPotentialPerCultivationPoint` | `int` | Số potential quy đổi trên mỗi cultivation point khi settle cultivation. | `1` |
| `cultivation.settlement_interval_seconds` | `GameConfigValues.CultivationSettlementIntervalSeconds` | `int` | Chu kỳ settle cultivation theo giây. | `300` |
| `character.home_garden_plot_count` | `GameConfigValues.CharacterHomeGardenPlotCount` | `int` | Số ô vườn mặc định khi tạo home cave mới. | `8` |
| `character.starter_basic_skill_id` | `GameConfigValues.CharacterStarterBasicSkillId` | `int` | `public.skills.id` của basic skill hệ thống cấp cho nhân vật mới. | `0` |
| `character.starter_basic_skill_slot_index` | `GameConfigValues.CharacterStarterBasicSkillSlotIndex` | `int` | Slot loadout mặc định dùng để gắn starter basic skill. | `1` |
| `skill.max_loadout_slot_count` | `GameConfigValues.SkillMaxLoadoutSlotCount` | `int` | Số slot loadout skill tối đa của nhân vật. | `5` |

## Nơi server đọc config

- Loader: [ServiceCollectionExtensions.cs](/F:/PhamNhanOnline/GameServer/Extensions/ServiceCollectionExtensions.cs)
- Snapshot typed: [GameConfigValues.cs](/F:/PhamNhanOnline/GameServer/Config/GameConfigValues.cs)
- Khóa config: [GameConfigKeys.cs](/F:/PhamNhanOnline/GameServer/Config/GameConfigKeys.cs)
- Entity DB: [GameConfigEntity.cs](/F:/PhamNhanOnline/GameServer/Entities/GameConfigEntity.cs)
- Repository DB: [GameConfigRepository.cs](/F:/PhamNhanOnline/GameServer/Repositories/GameConfigRepository.cs)

## Ví dụ sửa trong DB

Ví dụ muốn đổi tỷ lệ hồi về home sau combat dead từ `80%` thành `60%`:

```sql
UPDATE public.game_configs
SET config_value = '0.6',
    updated_at = now()
WHERE config_key = 'combat_death.return_home_recovery_ratio';
```

Ví dụ muốn đổi reconnect window từ `3` giây thành `5` giây:

```sql
UPDATE public.game_configs
SET config_value = '5',
    updated_at = now()
WHERE config_key = 'network.reconnect_resume_window_seconds';
```

## Lưu ý

- Hiện tại `GameConfigValues` được load một lần lúc server khởi động.
- Sau khi sửa `public.game_configs`, cần restart server để giá trị mới có hiệu lực.

