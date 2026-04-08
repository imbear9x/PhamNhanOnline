# Hệ Thống Description Template

## Mục tiêu

Hệ thống này chuẩn hóa cách viết mô tả cho:

- `item`
- `skill`
- `martial art`

Theo hướng:

- game design author dữ liệu ở DB bằng `description_template`
- server compile template thành `Description` cuối cùng
- client chỉ render text đã compile ra UI bằng TextMesh Pro

Rule quan trọng:

- client không tự tính lại gameplay để dựng description
- server không parse hay rewrite tag icon/sprite riêng
- nếu template chứa TMP rich text thì client render nguyên văn

Ví dụ:

```text
Gây {effect1.ratio_value|ratio_percent} sát thương lên mục tiêu.
Hồi {effect2.base_value|number} HP cho bản thân.
<sprite name="icon_hp">
```

## Nguồn dữ liệu

Các bảng chính có thể author `description_template`:

- `public.item_templates`
- `public.skills`
- `public.martial_arts`

Field `description` cũ vẫn được giữ làm fallback legacy text.

## Pipeline runtime

1. Server load definition từ DB qua catalog runtime.
2. `GameplayDescriptionService` build context token theo từng domain.
3. `DescriptionTemplateCompiler` compile template thành text cuối.
4. Server map text đã compile vào model gửi cho client.
5. Client render trực tiếp vào `TMP_Text`.

Fallback order:

1. `description_template` compile thành công
2. `description` legacy text
3. template mặc định do server tự generate theo type

## Cú pháp template

Token cơ bản:

```text
{token}
{token|format}
```

`token` hiện support chữ, số, dấu `_` và dấu `.`.

Ví dụ:

```text
{name}
{cooldown_ms|duration}
{effect1.ratio_value|ratio_percent}
{equipment.slot_type_label}
```

## Format support v1

- `plain`
  - in giá trị thô
- `number`
  - số thường, ví dụ `12`, `12.5`
- `signed_number`
  - số có dấu `+` khi dương
- `percent`
  - thêm `%` vào giá trị hiện có
- `signed_percent`
  - thêm `%` và thêm `+` khi dương
- `ratio_percent`
  - hiểu giá trị như multiplier: `0.25 -> 25%`, `1.1 -> 110%`, `2.0 -> 200%`
- `signed_ratio_percent`
  - giống `ratio_percent` nhưng có `+` khi dương
- `duration`
  - đổi milliseconds thành chuỗi như `1.5s`, `3s`
- `seconds`
  - đổi milliseconds thành số giây thuần

Nếu thiếu format thì mặc định là `plain`.

## TMP rich text

Template có thể chứa trực tiếp tag của TextMesh Pro, ví dụ:

```text
<b>Hỏa Cầu</b>
Gây {effect1.ratio_value|ratio_percent} sát thương.
<sprite name="fire">
```

Rule hiện tại:

- server giữ nguyên tag TMP trong text
- client không convert `<icon=...>` hay syntax riêng khác
- game design nên dùng cú pháp TMP thật sự đang được client support

## Token chung

Những token thường dùng ở nhiều domain:

- `{name}`
- `{code}`

## Skill token

Token tổng quát:

- `{skill_level}`
- `{skill_type}`
- `{skill_type_label}`
- `{skill_category}`
- `{skill_category_label}`
- `{target_type}`
- `{target_type_label}`
- `{cast_range}`
- `{cast_time_ms}`
- `{travel_time_ms}`
- `{cooldown_ms}`
- `{effects_count}`

Macro summary:

- `{effects_summary}`

Token theo effect:

- `{effect1.summary}`
- `{effect1.effect_type}`
- `{effect1.effect_type_label}`
- `{effect1.formula_type}`
- `{effect1.formula_type_label}`
- `{effect1.formula_subject_label}`
- `{effect1.formula_subject_icon_name}`
- `{effect1.formula_subject_rich}`
- `{effect1.value_type}`
- `{effect1.value_type_label}`
- `{effect1.base_value}`
- `{effect1.ratio_value}`
- `{effect1.extra_value}`
- `{effect1.chance_value}`
- `{effect1.duration_ms}`
- `{effect1.stat_type}`
- `{effect1.stat_type_label}`
- `{effect1.resource_type}`
- `{effect1.resource_type_label}`
- `{effect1.target_scope}`
- `{effect1.target_scope_label}`
- `{effect1.target_label}`
- `{effect1.trigger_timing}`
- `{effect1.trigger_timing_label}`

`effect2.*`, `effect3.*`... hoạt động tương tự nếu skill có nhiều effect.

Template mặc định hiện tại cho `skill`:

```text
{effects_summary}
```

Ví dụ template skill author tay theo effect data:

```text
Gay {effect1.ratio_value|ratio_percent} {effect1.formula_subject_rich} len {effect1.target_label}.
```

## Martial art token

Token tổng quát:

- `{quality}`
- `{quality_label}`
- `{category}`
- `{qi_absorption_rate}`
- `{max_stage}`
- `{stages_count}`
- `{skills_count}`

Macro summary:

- `{qi_summary}`
- `{stage_summary}`
- `{unlocked_skills_summary}`
- `{stage_bonuses_summary}`

Template mặc định hiện tại cho `martial art`:

```text
{qi_summary}
{stage_summary}
{unlocked_skills_summary}
```

## Item token

Token tổng quát:

- `{item_type}`
- `{item_type_label}`
- `{rarity}`
- `{rarity_label}`
- `{max_stack}`

Macro summary:

- `{equipment_stats_summary}`
- `{requirements_summary}`
- `{use_effects_summary}`
- `{martial_art_book_summary}`
- `{pill_recipe_book_summary}`
- `{soil_summary}`
- `{herb_seed_summary}`
- `{herb_plant_summary}`

Equipment token:

- `{equipment.slot_type}`
- `{equipment.slot_type_label}`
- `{equipment.equipment_type}`
- `{equipment.equipment_type_label}`
- `{equipment.level_requirement}`

Book/recipe token:

- `{martial_art_book_id}`
- `{martial_art_book_name}`
- `{pill_recipe_id}`
- `{pill_recipe_name}`

Template mặc định hiện tại theo `item_type`:

- `Equipment` -> `{equipment_stats_summary}\n{requirements_summary}`
- `Consumable` -> `{use_effects_summary}`
- `MartialArtBook` -> `{martial_art_book_summary}`
- `PillRecipeBook` -> `{pill_recipe_book_summary}`
- `Soil` -> `{soil_summary}`
- `HerbSeed` -> `{herb_seed_summary}`
- `HerbPlant` -> `{herb_plant_summary}`

## Rule authoring

- Ưu tiên token có tên thay vì `{0}`, `{1}`.
- Ưu tiên macro summary trước, chỉ dùng token chi tiết khi thực sự cần text đặc thù.
- Với `skill`, ưu tiên để `description_template` chỉ mô tả effect; `cast_time`, `cooldown`, `range` nên hiển thị ở field UI riêng.
- Với các field dạng ratio/multiplier như `ratio_value`, ưu tiên format `ratio_percent` thay vì `percent`.
- Không encode gameplay vào client UI.
- Không viết syntax icon custom nếu client chưa support trực tiếp.
- Nếu một template cần format rất đặc biệt, hãy thêm token/macro mới ở server thay vì nhồi logic vào chuỗi UI.

## Rule mở rộng dài hạn

Khi thêm loại content mới hoặc admin tool:

- admin preview phải gọi cùng pipeline compile như server runtime
- validation template nên dựa trên cùng danh sách token thực tế
- nếu cần localization, nên localize ở layer template/token chứ không tách một formatter khác ở client

Điểm chốt:

- server là nơi authoritative cho description cuối cùng
- client chỉ là renderer TMP
- admin tool sau này chỉ là UI authoring/preview đặt lên trên cùng pipeline này
