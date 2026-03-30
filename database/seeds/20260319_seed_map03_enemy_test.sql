begin;

delete from public.map_enemy_spawn_entries
where spawn_group_id in (3001, 3002);

delete from public.map_enemy_spawn_groups
where id in (3001, 3002);

delete from public.enemy_template_skills
where enemy_template_id in (1001, 1002);

delete from public.enemy_reward_rules
where enemy_template_id in (1001, 1002);

delete from public.game_random_entries
where game_random_table_id in (1001, 1002);

delete from public.game_random_tables
where id in (1001, 1002);

delete from public.skill_effects
where id in (2001, 2002);

insert into public.skills (
    id,
    code,
    name,
    skill_type,
    target_type,
    cast_range,
    cooldown_ms,
    description,
    created_at
)
values
    (
        2001,
        'hoa_dan_soi',
        'Hỏa đạn Sói',
        1,
        2,
        2000,
        2000,
        'Skill test của Sói lang băng. Gây 110% ATK lên một mục tiêu.',
        timezone('utc', now())
    ),
    (
        2002,
        'dam_xa',
        'Đấm xa',
        1,
        2,
        2000,
        2000,
        'Skill test của Gấu nâu tinh. Gây 120% ATK lên một mục tiêu.',
        timezone('utc', now())
    )
on conflict (id) do update
set
    code = excluded.code,
    name = excluded.name,
    skill_type = excluded.skill_type,
    target_type = excluded.target_type,
    cast_range = excluded.cast_range,
    cooldown_ms = excluded.cooldown_ms,
    description = excluded.description;

insert into public.skill_effects (
    id,
    skill_id,
    effect_type,
    order_index,
    formula_type,
    value_type,
    base_value,
    ratio_value,
    extra_value,
    chance_value,
    duration_ms,
    stat_type,
    resource_type,
    target_scope,
    trigger_timing,
    created_at
)
values
    (
        2001,
        2001,
        1,
        0,
        2,
        2,
        null,
        1.10,
        null,
        null,
        null,
        null,
        null,
        1,
        2,
        timezone('utc', now())
    ),
    (
        2002,
        2002,
        1,
        0,
        2,
        2,
        null,
        1.20,
        null,
        null,
        null,
        null,
        null,
        1,
        2,
        timezone('utc', now())
    )
on conflict (id) do update
set
    skill_id = excluded.skill_id,
    effect_type = excluded.effect_type,
    order_index = excluded.order_index,
    formula_type = excluded.formula_type,
    value_type = excluded.value_type,
    base_value = excluded.base_value,
    ratio_value = excluded.ratio_value,
    extra_value = excluded.extra_value,
    chance_value = excluded.chance_value,
    duration_ms = excluded.duration_ms,
    stat_type = excluded.stat_type,
    resource_type = excluded.resource_type,
    target_scope = excluded.target_scope,
    trigger_timing = excluded.trigger_timing;

insert into public.enemy_templates (
    id,
    code,
    name,
    kind,
    max_hp,
    base_attack,
    base_move_speed,
    patrol_radius,
    detection_radius,
    combat_radius,
    minimum_skill_interval_ms,
    cultivation_reward_total,
    potential_reward_total,
    description,
    created_at
)
values
    (
        1001,
        'enemy_soi_lang_bang',
        'Sói lang băng',
        1,
        100,
        10,
        100,
        0,
        2000,
        2000,
        2000,
        10,
        0,
        'Enemy test map 03. Model key lấy trực tiếp từ code.',
        timezone('utc', now())
    ),
    (
        1002,
        'enemy_gau_nau_tinh',
        'Gấu nâu tinh',
        1,
        150,
        15,
        100,
        0,
        2000,
        2000,
        2000,
        20,
        0,
        'Enemy test map 03. Model key lấy trực tiếp từ code.',
        timezone('utc', now())
    )
on conflict (id) do update
set
    code = excluded.code,
    name = excluded.name,
    kind = excluded.kind,
    max_hp = excluded.max_hp,
    base_attack = excluded.base_attack,
    base_move_speed = excluded.base_move_speed,
    patrol_radius = excluded.patrol_radius,
    detection_radius = excluded.detection_radius,
    combat_radius = excluded.combat_radius,
    minimum_skill_interval_ms = excluded.minimum_skill_interval_ms,
    cultivation_reward_total = excluded.cultivation_reward_total,
    potential_reward_total = excluded.potential_reward_total,
    description = excluded.description;

insert into public.enemy_template_skills (
    enemy_template_id,
    skill_id,
    order_index
)
values
    (1001, 2001, 1),
    (1002, 2002, 1);

insert into public.game_random_tables (
    id,
    table_id,
    mode,
    fortune_enabled,
    fortune_bonus_parts_per_million_per_fortune_point,
    fortune_max_bonus_parts_per_million,
    none_entry_id,
    description,
    created_at
)
values
    (
        1001,
        'enemy.drop.soi_lang_bang',
        0,
        false,
        0,
        0,
        '__none__',
        'Bang drop test cho Sói lang băng: 50% rơi 1-3 linh thạch.',
        timezone('utc', now())
    ),
    (
        1002,
        'enemy.drop.gau_nau_tinh',
        0,
        false,
        0,
        0,
        '__none__',
        'Bang drop test cho Gấu nâu tinh: 20% rơi 1-3 linh thạch.',
        timezone('utc', now())
    )
on conflict (id) do update
set
    table_id = excluded.table_id,
    mode = excluded.mode,
    fortune_enabled = excluded.fortune_enabled,
    fortune_bonus_parts_per_million_per_fortune_point = excluded.fortune_bonus_parts_per_million_per_fortune_point,
    fortune_max_bonus_parts_per_million = excluded.fortune_max_bonus_parts_per_million,
    none_entry_id = excluded.none_entry_id,
    description = excluded.description;

insert into public.game_random_entries (
    id,
    game_random_table_id,
    entry_id,
    chance_parts_per_million,
    is_none,
    order_index,
    created_at
)
values
    (10011, 1001, 'item_code:linh_thach:1', 166666, false, 1, timezone('utc', now())),
    (10012, 1001, 'item_code:linh_thach:2', 166667, false, 2, timezone('utc', now())),
    (10013, 1001, 'item_code:linh_thach:3', 166667, false, 3, timezone('utc', now())),
    (10021, 1002, 'item_code:linh_thach:1', 66666, false, 1, timezone('utc', now())),
    (10022, 1002, 'item_code:linh_thach:2', 66667, false, 2, timezone('utc', now())),
    (10023, 1002, 'item_code:linh_thach:3', 66667, false, 3, timezone('utc', now()))
on conflict (id) do update
set
    game_random_table_id = excluded.game_random_table_id,
    entry_id = excluded.entry_id,
    chance_parts_per_million = excluded.chance_parts_per_million,
    is_none = excluded.is_none,
    order_index = excluded.order_index;

insert into public.enemy_reward_rules (
    enemy_template_id,
    delivery_type,
    target_rule,
    game_random_table_id,
    roll_count,
    ownership_duration_seconds,
    free_for_all_duration_seconds,
    minimum_damage_parts_per_million,
    order_index,
    created_at
)
values
    (1001, 1, 1, 1001, 1, 30, 30, 0, 1, timezone('utc', now())),
    (1002, 1, 1, 1002, 1, 30, 30, 0, 1, timezone('utc', now()));

insert into public.map_enemy_spawn_groups (
    id,
    code,
    name,
    map_template_id,
    runtime_scope,
    zone_index,
    spawn_mode,
    is_boss_spawn,
    max_alive,
    respawn_seconds,
    initial_spawn_delay_seconds,
    center_x,
    center_y,
    spawn_radius,
    description,
    created_at
)
values
    (
        3001,
        'map03_soi_lang_bang_spawn',
        'Map 03 - Sói lang băng',
        3,
        1,
        null,
        1,
        false,
        1,
        5,
        0,
        500,
        500,
        0,
        'Spawn 1 Sói lang băng ở giữa map 03.',
        timezone('utc', now())
    ),
    (
        3002,
        'map03_gau_nau_tinh_spawn',
        'Map 03 - Gấu nâu tinh',
        3,
        1,
        null,
        1,
        false,
        1,
        5,
        0,
        700,
        500,
        0,
        'Spawn 1 Gấu nâu tinh lệch 1/5 chiều ngang map 03 so với Sói lang băng.',
        timezone('utc', now())
    )
on conflict (id) do update
set
    code = excluded.code,
    name = excluded.name,
    map_template_id = excluded.map_template_id,
    runtime_scope = excluded.runtime_scope,
    zone_index = excluded.zone_index,
    spawn_mode = excluded.spawn_mode,
    is_boss_spawn = excluded.is_boss_spawn,
    max_alive = excluded.max_alive,
    respawn_seconds = excluded.respawn_seconds,
    initial_spawn_delay_seconds = excluded.initial_spawn_delay_seconds,
    center_x = excluded.center_x,
    center_y = excluded.center_y,
    spawn_radius = excluded.spawn_radius,
    description = excluded.description;

insert into public.map_enemy_spawn_entries (
    spawn_group_id,
    enemy_template_id,
    weight,
    order_index
)
values
    (3001, 1001, 1, 1),
    (3002, 1002, 1, 1);

commit;
