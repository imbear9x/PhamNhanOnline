# Hệ Thống Skill Combat

## Mục tiêu

Từ phase này, server không còn coi skill là một hành động hardcode kiểu:

- bấm skill
- tính ra một số damage cố định
- tới impact thì trừ máu trực tiếp

Thay vào đó, skill được chuẩn hóa theo hướng data-driven:

- `skills` định nghĩa skill gốc
- `skill_effects` định nghĩa các effect của skill
- server chỉ có một pipeline chung để:
  - validate
  - schedule cast/travel
  - resolve effect theo `trigger_timing`
  - áp effect lên mục tiêu

Mục tiêu là:

- phần lớn skill không cần code riêng
- game design chỉ cần config data
- chỉ những skill thật sự đặc biệt mới cần custom behavior ở phase sau

## Thành phần chính

### 1. Skill Definition

Server load dữ liệu từ:

- `public.skills`
- `public.skill_effects`

Qua:

- `GameServer/Runtime/CombatDefinitionCatalog.cs`

Các field cốt lõi đang được dùng:

- `target_type`
- `cast_range`
- `cast_time_ms`
- `travel_time_ms`
- `cooldown_ms`
- `skill_effects.effect_type`
- `skill_effects.formula_type`
- `skill_effects.value_type`
- `skill_effects.base_value`
- `skill_effects.ratio_value`
- `skill_effects.extra_value`
- `skill_effects.chance_value`
- `skill_effects.duration_ms`
- `skill_effects.stat_type`
- `skill_effects.resource_type`
- `skill_effects.target_scope`
- `skill_effects.trigger_timing`

### 2. Runtime Combat Status

Trạng thái combat runtime chỉ nằm trong RAM, không lưu xuống DB.

Hiện được giữ bởi:

- `GameServer/Runtime/CombatStatusRuntime.cs`

Mỗi `PlayerSession` và mỗi `MonsterEntity` đều có một `CombatStatusCollection`.

Collection này giữ:

- shield đang còn
- stun đang còn
- các buff/debuff stat đang còn hiệu lực

Điểm quan trọng:

- combat state không được persist xuống DB
- disconnect/reconnect sẽ mất các effect combat tạm thời
- đây là chủ đích để tránh DB bị spam bởi trạng thái combat

### 3. Pending Skill Execution

Khi player bấm skill, server không apply effect ngay.

Server tạo một `PendingSkillExecution` trong:

- `GameServer/World/SkillExecutionRuntimeTypes.cs`

Execution hiện giữ:

- caster
- skill id
- player skill id
- slot skill
- `target_type`
- stat snapshot của caster tại thời điểm bắt đầu cast
- mốc thời gian:
  - `CastStartedAtUtc`
  - `CastCompletedAtUtc`
  - `ImpactAtUtc`

### 4. Scheduler của Map

`MapInstance` chỉ còn làm nhiệm vụ scheduler:

- giữ danh sách `PendingSkillExecution`
- đến `CastCompletedAtUtc` thì phát event `SkillCastReleaseRuntimeEvent`
- đến `ImpactAtUtc` thì phát event `SkillImpactDueRuntimeEvent`

File:

- `GameServer/World/MapInstance.cs`

### 5. Skill Execution Service

Toàn bộ logic áp effect chung được dồn vào:

- `GameServer/Runtime/SkillExecutionService.cs`

Service này chịu trách nhiệm:

- resolve effect theo `trigger_timing`
- resolve target theo `target_scope`
- tính magnitude theo `formula_type`
- áp effect lên player hoặc enemy

## Luồng runtime hiện tại

### Bước 1. Client gửi yêu cầu dùng skill

Packet:

- `AttackEnemyPacket`

Hiện packet này đã nới ra để:

- skill không còn mang riêng `EnemyRuntimeId`
- thay vào đó dùng `CombatTarget`
- `CombatTarget` có thể đại diện cho:
  - người chơi
  - quái
  - boss
  - hình nộm
  - NPC
  - điểm trên mặt đất
- skill `Self` có thể không cần target

Nhưng tên packet vẫn là `AttackEnemyPacket` để không phá flow cũ.

### Bước 2. Handler validate và enqueue

Handler:

- `GameServer/Network/Handlers/AttackEnemyHandler.cs`

Handler hiện làm các việc:

- check player đã vào world chưa
- check không đang tu luyện
- check không đang casting
- check không đang bị stun
- resolve skill đang equip ở slot
- check cooldown
- check target/range nếu là `EnemySingle`
- check target/range theo `CombatTarget` nếu là `EnemySingle` hoặc `AllySingle`
- tạo `PendingSkillExecution`
- nếu có `cast_time_ms > 0` thì set state runtime sang `Casting`

Handler không còn tự tính damage hardcode nữa.

### Bước 3. GameLoop nhận event cast release

Game loop:

- `GameServer/Runtime/GameLoop.cs`

Khi tới `CastCompletedAtUtc`:

- gọi `SkillExecutionService.ResolveCastRelease(...)`
- áp các effect có `trigger_timing = OnCast`
- sau đó clear trạng thái `Casting` của player nếu còn đang là `Casting`

### Bước 4. GameLoop nhận event impact

Khi tới `ImpactAtUtc`:

- gọi `SkillExecutionService.ResolveImpact(...)`
- service áp các effect có `trigger_timing = OnHit`
- kết quả được đưa vào `SkillImpactResolvedRuntimeEvent`
- `WorldInterestService` broadcast packet xuống client

### Bước 5. Client nhận packet đồng bộ

Các packet đang dùng:

- `SkillCastStartedPacket`
- `SkillImpactResolvedPacket`
- `EnemyHpChangedPacket`
- `ObservedCharacterCurrentStateChangedPacket`

Ý nghĩa:

- `SkillCastStartedPacket`: client biết lúc nào bắt đầu cast, lúc nào kết thúc cast, lúc nào impact
- `SkillImpactResolvedPacket`: client biết skill resolve thành công hay thất bại, damage chính là bao nhiêu
- `EnemyHpChangedPacket`: client cập nhật máu quái
- `ObservedCharacterCurrentStateChangedPacket`: client cập nhật HP/MP/Stamina của player nếu skill tác động lên player

## Các effect đã hỗ trợ ngay

### Damage

- áp damage lên enemy hoặc player
- có shield absorb trước khi trừ HP thật
- damage số hiển thị lấy theo damage thực sau shield/overkill

### Heal

- hồi HP cho player
- hồi HP cho enemy

### ResourceReduce

- trừ `Hp`, `Mp`, hoặc `Stamina` của player
- với enemy hiện chỉ map `Hp` là có ý nghĩa

### ResourceRestore

- hồi `Hp`, `Mp`, hoặc `Stamina` cho player
- với enemy hiện chỉ map `Hp` là có ý nghĩa

### Shield

- thêm lá chắn runtime
- shield tồn tại trong RAM
- nếu có `duration_ms` thì hết hạn theo thời gian
- nếu không có `duration_ms` thì tồn tại cho tới khi bị đánh vỡ

### Stun

- stun runtime cho player hoặc enemy
- player bị stun sẽ không thể:
  - cast skill
  - di chuyển
  - travel map
  - switch zone
- enemy bị stun sẽ không thể attack trong thời gian stun

### BuffStat / DebuffStat

Hiện đã hỗ trợ ổn cho:

- `Attack`
- `Speed`
- `SpiritualSense`
- `Fortune`

Khuyến nghị hiện tại:

- nếu muốn tăng/giảm HP hoặc MP tức thời, dùng `Heal`, `ResourceReduce`, `ResourceRestore`
- chưa nên dùng `BuffStat`/`DebuffStat` cho `Hp`, `Mp`, `Stamina` cho tới khi có thêm tầng clamp/max-resource combat đầy đủ

## Công thức tính magnitude

Server đang tính magnitude theo công thức chung:

`magnitude = base_value + extra_value + source_stat * ratio_value`

Trong đó `source_stat` phụ thuộc `formula_type`:

- `Flat`: không lấy stat nguồn
- `AttackRatio`: lấy `caster.Attack`
- `MaxHpRatio`: lấy `caster.MaxHp`
- `MaxMpRatio`: lấy `caster.MaxMp`

Sau đó:

- `Damage`, `Heal`, `ResourceReduce`, `ResourceRestore`, `Shield` lấy phần nguyên không âm
- `BuffStat`, `DebuffStat` giữ được giá trị thập phân để dùng cho `Flat/Ratio/Percent`

## Target support hiện tại

### Đã hỗ trợ

- `SkillTargetType.Self`
- `SkillTargetType.EnemySingle`

### Chưa hỗ trợ, đã chặn rõ bằng code

- `EnemyArea`
- `AllySingle`
- `AllyArea`
- `GroundArea`

Khi config các loại này ở phase hiện tại, handler sẽ trả:

- `MessageCode.SkillTargetTypeNotSupported`

## Target scope support hiện tại

### Đã hỗ trợ

- `Self`
- `Primary`

Quy ước:

- với skill `Self`, `Primary` cũng chính là caster
- với skill `EnemySingle`, `Primary` là enemy target

### Tạm để dành cho phase sau

- `Splash`
- `All`

## Trigger timing support hiện tại

### Đã hỗ trợ

- `OnCast`
- `OnHit`

### Chưa hỗ trợ

- `OnExpire`

`OnExpire` sẽ được làm khi game có thêm lifecycle đầy đủ cho buff/debuff và hook xử lý lúc effect hết hạn.

## Quy ước config gợi ý cho game design

### Ví dụ 1. Đòn đánh thường

- `target_type = EnemySingle`
- `cast_time_ms = 0`
- `travel_time_ms = 0`
- 1 effect:
  - `effect_type = Damage`
  - `formula_type = AttackRatio`
  - `ratio_value = 1.0`
  - `target_scope = Primary`
  - `trigger_timing = OnHit`

### Ví dụ 2. Chưởng có gồng rồi bay

- `target_type = EnemySingle`
- `cast_time_ms = 300`
- `travel_time_ms = 500`
- 1 effect:
  - `Damage`
  - `AttackRatio`
  - `ratio_value = 1.2`
  - `Primary`
  - `OnHit`

### Ví dụ 3. Tự buff lá chắn ngay lúc thi triển

- `target_type = Self`
- `cast_time_ms = 200`
- `travel_time_ms = 0`
- 1 effect:
  - `Shield`
  - `Flat`
  - `base_value = 120`
  - `duration_ms = 5000`
  - `target_scope = Self`
  - `trigger_timing = OnCast`

### Ví dụ 4. Tự hồi mana

- `target_type = Self`
- `cast_time_ms = 0`
- `travel_time_ms = 0`
- 1 effect:
  - `ResourceRestore`
  - `resource_type = Mp`
  - `formula_type = Flat`
  - `base_value = 80`
  - `target_scope = Self`
  - `trigger_timing = OnHit`

### Ví dụ 5. Làm choáng mục tiêu

- `target_type = EnemySingle`
- 1 effect:
  - `Stun`
  - `duration_ms = 1500`
  - `chance_value = 30`
  - `target_scope = Primary`
  - `trigger_timing = OnHit`

### Ví dụ 6. Ăn mòn công kích mục tiêu

- `target_type = EnemySingle`
- 1 effect:
  - `DebuffStat`
  - `stat_type = Attack`
  - `value_type = Percent`
  - `base_value = 15`
  - `duration_ms = 4000`
  - `target_scope = Primary`
  - `trigger_timing = OnHit`

## Chỗ nào vẫn còn TODO

### 1. AOE / Ground Target / Ally Target

Cần thêm:

- packet target tổng quát hơn
- target resolver vùng
- có thể thêm bán kính hoặc shape vào schema

### 2. OnExpire

Cần thêm:

- lifecycle hook lúc status effect hết hạn
- optional event để client hiện VFX kết thúc buff/debuff

### 3. Buff/Debuff lên Max HP / Max MP / Max Stamina

Hiện combat runtime đã có stat modifier framework, nhưng phần clamp/max resource combat chưa mở rộng đầy đủ cho case thay đổi max resource động.

### 4. Custom Skill Behavior

Hiện phần lớn skill đã chạy được bằng data.

Phase sau có thể thêm:

- `behavior_key`
- `custom handler`

cho những skill đặc biệt như:

- teleport ra sau mục tiêu
- nổ lan nhiều nhánh
- hiến tế HP để tăng damage

## Kết luận

Server hiện đã chuyển sang mô hình:

- scheduler generic
- effect resolver generic
- combat status in-memory

Nhờ đó:

- damage/heal/mana/shield/stun/debuff không cần code riêng từng skill
- game design chỉ cần config `skills` và `skill_effects`
- code riêng chỉ nên viết cho những skill thật sự đặc biệt ở phase sau
