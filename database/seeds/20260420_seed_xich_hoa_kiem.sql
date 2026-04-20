BEGIN;

INSERT INTO public.skills (
    id,
    code,
    name,
    skill_group_code,
    skill_level,
    skill_type,
    skill_category,
    target_type,
    cast_range,
    cast_time_ms,
    travel_time_ms,
    cooldown_ms,
    description,
    description_template
)
VALUES (
    2004,
    'xich_hoa_kiem_tram',
    'Xích Hỏa Kiếm Trảm',
    'xich_hoa_kiem_tram',
    1,
    1,
    2,
    2,
    1000,
    0,
    500,
    3000,
    'Ngưng tụ hỏa linh thành kiếm quang, bắn ra một quả cầu lửa chém mục tiêu, gây 120% ATK.',
    'Gay {effect1.ratio_value|ratio_percent} {effect1.formula_subject_rich} len {effect1.target_label}.'
)
ON CONFLICT (id) DO UPDATE
SET
    code = EXCLUDED.code,
    name = EXCLUDED.name,
    skill_group_code = EXCLUDED.skill_group_code,
    skill_level = EXCLUDED.skill_level,
    skill_type = EXCLUDED.skill_type,
    skill_category = EXCLUDED.skill_category,
    target_type = EXCLUDED.target_type,
    cast_range = EXCLUDED.cast_range,
    cast_time_ms = EXCLUDED.cast_time_ms,
    travel_time_ms = EXCLUDED.travel_time_ms,
    cooldown_ms = EXCLUDED.cooldown_ms,
    description = EXCLUDED.description,
    description_template = EXCLUDED.description_template;

DELETE FROM public.skill_effects
WHERE skill_id = 2004;

INSERT INTO public.skill_effects (
    id,
    skill_id,
    effect_type,
    order_index,
    formula_type,
    value_type,
    ratio_value,
    target_scope,
    trigger_timing
)
VALUES (
    2004,
    2004,
    1,
    1,
    2,
    2,
    1.20,
    1,
    2
);

INSERT INTO public.item_templates (
    id,
    code,
    name,
    item_type,
    rarity,
    max_stack,
    is_tradeable,
    is_droppable,
    is_destroyable,
    icon,
    background_icon,
    description,
    description_template
)
VALUES (
    910012,
    'xich_hoa_kiem',
    'Xích Hỏa Kiếm',
    1,
    4,
    1,
    true,
    true,
    true,
    'item_xich_hoa_kiem',
    'bg_item_epic',
    'Hỏa kiếm nung từ xích tinh. Tăng 50 ATK và đi kèm Xích Hỏa Kiếm Trảm.',
    '{equipment_stats_summary}+{requirements_summary}'
)
ON CONFLICT (id) DO UPDATE
SET
    code = EXCLUDED.code,
    name = EXCLUDED.name,
    item_type = EXCLUDED.item_type,
    rarity = EXCLUDED.rarity,
    max_stack = EXCLUDED.max_stack,
    is_tradeable = EXCLUDED.is_tradeable,
    is_droppable = EXCLUDED.is_droppable,
    is_destroyable = EXCLUDED.is_destroyable,
    icon = EXCLUDED.icon,
    background_icon = EXCLUDED.background_icon,
    description = EXCLUDED.description,
    description_template = EXCLUDED.description_template;

INSERT INTO public.equipment_templates (
    item_template_id,
    equipment_type,
    level_requirement
)
VALUES (
    910012,
    1,
    18
)
ON CONFLICT (item_template_id) DO UPDATE
SET
    equipment_type = EXCLUDED.equipment_type,
    level_requirement = EXCLUDED.level_requirement;

DELETE FROM public.equipment_template_stats
WHERE equipment_template_id = 910012;

INSERT INTO public.equipment_template_stats (
    equipment_template_id,
    stat_type,
    value,
    value_type
)
VALUES (
    910012,
    4,
    50,
    1
);

INSERT INTO public.equipment_template_skill_grants (
    equipment_template_id,
    skill_id,
    required_realm_template_id,
    display_order
)
VALUES (
    910012,
    2004,
    NULL,
    0
)
ON CONFLICT (equipment_template_id, skill_id) DO UPDATE
SET
    required_realm_template_id = EXCLUDED.required_realm_template_id,
    display_order = EXCLUDED.display_order,
    updated_at = now();

COMMIT;
