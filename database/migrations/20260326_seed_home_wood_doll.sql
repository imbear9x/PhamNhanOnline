BEGIN;

INSERT INTO public.enemy_templates (
    id,
    code,
    name,
    kind,
    max_hp,
    base_attack,
    patrol_radius,
    detection_radius,
    combat_radius,
    enable_out_of_combat_restore,
    out_of_combat_restore_delay_seconds,
    minimum_skill_interval_ms,
    cultivation_reward_total,
    potential_reward_total,
    description)
VALUES (
    1003,
    'wood_doll',
    'Nguoi Rom Luyen Cong',
    1,
    1000,
    0,
    0,
    0,
    0,
    true,
    20,
    1000,
    0,
    0,
    'Hinh nom dung yen trong dong phu de test damage, skill va VFX.')
ON CONFLICT (code) DO UPDATE
SET
    name = EXCLUDED.name,
    kind = EXCLUDED.kind,
    max_hp = EXCLUDED.max_hp,
    base_attack = EXCLUDED.base_attack,
    patrol_radius = EXCLUDED.patrol_radius,
    detection_radius = EXCLUDED.detection_radius,
    combat_radius = EXCLUDED.combat_radius,
    enable_out_of_combat_restore = EXCLUDED.enable_out_of_combat_restore,
    out_of_combat_restore_delay_seconds = EXCLUDED.out_of_combat_restore_delay_seconds,
    minimum_skill_interval_ms = EXCLUDED.minimum_skill_interval_ms,
    cultivation_reward_total = EXCLUDED.cultivation_reward_total,
    potential_reward_total = EXCLUDED.potential_reward_total,
    description = EXCLUDED.description;

INSERT INTO public.map_enemy_spawn_groups (
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
    description)
VALUES (
    3003,
    'home_wood_doll_spawn',
    'Home Wood Doll Spawn',
    1,
    2,
    NULL,
    1,
    FALSE,
    1,
    2,
    0,
    500,
    125,
    0,
    'Sinh 1 nguoi rom trong dong phu rieng cua moi nhan vat.')
ON CONFLICT (code) DO UPDATE
SET
    name = EXCLUDED.name,
    map_template_id = EXCLUDED.map_template_id,
    runtime_scope = EXCLUDED.runtime_scope,
    zone_index = EXCLUDED.zone_index,
    spawn_mode = EXCLUDED.spawn_mode,
    is_boss_spawn = EXCLUDED.is_boss_spawn,
    max_alive = EXCLUDED.max_alive,
    respawn_seconds = EXCLUDED.respawn_seconds,
    initial_spawn_delay_seconds = EXCLUDED.initial_spawn_delay_seconds,
    center_x = EXCLUDED.center_x,
    center_y = EXCLUDED.center_y,
    spawn_radius = EXCLUDED.spawn_radius,
    description = EXCLUDED.description;

INSERT INTO public.map_enemy_spawn_entries (
    spawn_group_id,
    enemy_template_id,
    weight,
    order_index)
SELECT
    spawn_group.id,
    enemy_template.id,
    1,
    0
FROM public.map_enemy_spawn_groups spawn_group
JOIN public.enemy_templates enemy_template
    ON enemy_template.code = 'wood_doll'
WHERE spawn_group.code = 'home_wood_doll_spawn'
ON CONFLICT (spawn_group_id, order_index) DO UPDATE
SET
    enemy_template_id = EXCLUDED.enemy_template_id,
    weight = EXCLUDED.weight;

COMMIT;
