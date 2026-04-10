BEGIN;

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
    description_template,
    created_at
)
VALUES
    (
        3003,
        'bo_huyet_dan',
        'Bổ Huyết Đan',
        2,
        1,
        99,
        TRUE,
        TRUE,
        TRUE,
        'item_bo_huyet_dan',
        'bg_item_common',
        'Đan dược trị thương cơ bản, dùng để hồi sinh lực trong chiến đấu hoặc sau khi giao chiến.',
        '{use_effects_summary}',
        timezone('utc', now())
    ),
    (
        6001,
        'bo_huyet_thao_la',
        'Lá Bổ Huyết Thảo',
        10,
        1,
        999,
        TRUE,
        TRUE,
        TRUE,
        'item_bo_huyet_thao_la',
        'bg_item_common',
        'Lá linh thảo có dược tính bồi bổ khí huyết, thường được dùng để luyện Bổ Huyết Đan.',
        'Duoc lieu dung de luyen Bo Huyet Dan.',
        timezone('utc', now())
    ),
    (
        8002,
        'dan_phuong_bo_huyet_dan',
        'Đan Phương - Bổ Huyết Đan',
        8,
        1,
        1,
        TRUE,
        TRUE,
        TRUE,
        'item_dan_phuong_bo_huyet_dan',
        'bg_item_common',
        'Ghi chép phương pháp luyện chế Bổ Huyết Đan.',
        '{pill_recipe_book_summary}',
        timezone('utc', now())
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

INSERT INTO public.pill_templates (
    item_template_id,
    pill_category,
    usage_type,
    cooldown_ms
)
VALUES
    (
        3003,
        1,
        1,
        5000
    )
ON CONFLICT (item_template_id) DO UPDATE
SET
    pill_category = EXCLUDED.pill_category,
    usage_type = EXCLUDED.usage_type,
    cooldown_ms = EXCLUDED.cooldown_ms;

INSERT INTO public.pill_effects (
    id,
    pill_template_id,
    effect_type,
    order_index,
    value_type,
    base_value,
    ratio_value,
    duration_ms,
    stat_type,
    note
)
VALUES
    (
        3,
        3003,
        1,
        0,
        1,
        50,
        NULL,
        0,
        0,
        'Hoi 50 HP'
    )
ON CONFLICT (id) DO UPDATE
SET
    pill_template_id = EXCLUDED.pill_template_id,
    effect_type = EXCLUDED.effect_type,
    order_index = EXCLUDED.order_index,
    value_type = EXCLUDED.value_type,
    base_value = EXCLUDED.base_value,
    ratio_value = EXCLUDED.ratio_value,
    duration_ms = EXCLUDED.duration_ms,
    stat_type = EXCLUDED.stat_type,
    note = EXCLUDED.note;

INSERT INTO public.pill_recipe_templates (
    id,
    code,
    name,
    recipe_book_item_template_id,
    result_pill_item_template_id,
    description,
    craft_duration_seconds,
    base_success_rate,
    success_rate_cap,
    mutation_rate,
    mutation_rate_cap,
    created_at
)
VALUES
    (
        2,
        'dp_bo_huyet_dan',
        'Bổ Huyết Đan',
        8002,
        3003,
        'Luyện chế đan dược cơ bản giúp hồi 50 HP sau khi sử dụng.',
        10,
        80,
        100,
        0,
        0,
        timezone('utc', now())
    )
ON CONFLICT (id) DO UPDATE
SET
    code = EXCLUDED.code,
    name = EXCLUDED.name,
    recipe_book_item_template_id = EXCLUDED.recipe_book_item_template_id,
    result_pill_item_template_id = EXCLUDED.result_pill_item_template_id,
    description = EXCLUDED.description,
    craft_duration_seconds = EXCLUDED.craft_duration_seconds,
    base_success_rate = EXCLUDED.base_success_rate,
    success_rate_cap = EXCLUDED.success_rate_cap,
    mutation_rate = EXCLUDED.mutation_rate,
    mutation_rate_cap = EXCLUDED.mutation_rate_cap;

INSERT INTO public.pill_recipe_inputs (
    id,
    pill_recipe_template_id,
    required_item_template_id,
    required_quantity,
    consume_mode,
    is_optional,
    success_rate_bonus,
    mutation_bonus_rate,
    required_herb_maturity
)
VALUES
    (
        2,
        2,
        6001,
        3,
        1,
        FALSE,
        0,
        0,
        0
    )
ON CONFLICT (id) DO UPDATE
SET
    pill_recipe_template_id = EXCLUDED.pill_recipe_template_id,
    required_item_template_id = EXCLUDED.required_item_template_id,
    required_quantity = EXCLUDED.required_quantity,
    consume_mode = EXCLUDED.consume_mode,
    is_optional = EXCLUDED.is_optional,
    success_rate_bonus = EXCLUDED.success_rate_bonus,
    mutation_bonus_rate = EXCLUDED.mutation_bonus_rate,
    required_herb_maturity = EXCLUDED.required_herb_maturity;

COMMIT;
