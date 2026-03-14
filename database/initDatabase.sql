BEGIN;

INSERT INTO public.servers (id, name, status)
VALUES (1, 'Server01', 1)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    status = EXCLUDED.status;

INSERT INTO public.game_time_state (
    id,
    anchor_utc,
    anchor_game_minute,
    game_minutes_per_real_minute,
    days_per_game_year,
    runtime_save_interval_seconds,
    derived_state_refresh_interval_seconds
)
VALUES (
    1,
    TIMESTAMPTZ '2026-01-01 00:00:00+00',
    0,
    518400,
    360,
    2,
    5
)
ON CONFLICT (id) DO UPDATE
SET
    anchor_utc = EXCLUDED.anchor_utc,
    anchor_game_minute = EXCLUDED.anchor_game_minute,
    game_minutes_per_real_minute = EXCLUDED.game_minutes_per_real_minute,
    days_per_game_year = EXCLUDED.days_per_game_year,
    runtime_save_interval_seconds = EXCLUDED.runtime_save_interval_seconds,
    derived_state_refresh_interval_seconds = EXCLUDED.derived_state_refresh_interval_seconds;

INSERT INTO public.realm_templates (
    id,
    name,
    stage_name,
    max_cultivation,
    lifespan,
    base_breakthrough_rate,
    failure_penalty
)
VALUES
    (1, 'Luyện Khí Kỳ tầng 1', 'Luyện Khí Kỳ tầng 1', 150, 120, 100, 0),
    (2, 'Luyện Khí Kỳ tầng 2', 'Luyện Khí Kỳ tầng 2', 200, 125, 95, 0),
    (3, 'Luyện Khí Kỳ tầng 3', 'Luyện Khí Kỳ tầng 3', 280, 130, 90, 0),
    (4, 'Luyện Khí Kỳ tầng 4', 'Luyện Khí Kỳ tầng 4', 380, 135, 85, 0),
    (5, 'Luyện Khí Kỳ tầng 5', 'Luyện Khí Kỳ tầng 5', 520, 140, 80, 0),
    (6, 'Luyện Khí Kỳ tầng 6', 'Luyện Khí Kỳ tầng 6', 750, 145, 75, 0),
    (7, 'Luyện Khí Kỳ tầng 7', 'Luyện Khí Kỳ tầng 7', 1200, 150, 70, 0),
    (8, 'Luyện Khí Kỳ tầng 8', 'Luyện Khí Kỳ tầng 8', 1500, 155, 65, 0),
    (9, 'Luyện Khí Kỳ tầng 9', 'Luyện Khí Kỳ tầng 9', 2000, 160, 40, 0),
    (10, 'Trúc Cơ Sơ Kỳ', 'Trúc Cơ Sơ Kỳ', 5000, 180, 40, 0),
    (11, 'Trúc Cơ Trung Kỳ', 'Trúc Cơ Trung Kỳ', 7000, 200, 35, 0),
    (12, 'Trúc Cơ Hậu Kỳ', 'Trúc Cơ Hậu Kỳ', 10000, 220, 25, 0),
    (13, 'Kết Đan Sơ Kỳ', 'Kết Đan Sơ Kỳ', 25000, 350, 30, 0),
    (14, 'Kết Đan Trung Kỳ', 'Kết Đan Trung Kỳ', 35000, 400, 28, 0),
    (15, 'Kết Đan Hậu Kỳ', 'Kết Đan Hậu Kỳ', 50000, 500, 20, 0),
    (16, 'Nguyên Anh Sơ Kỳ', 'Nguyên Anh Sơ Kỳ', 125000, 1200, 18, 0),
    (17, 'Nguyên Anh Trung Kỳ', 'Nguyên Anh Trung Kỳ', 175000, 1500, 15, 0),
    (18, 'Nguyên Anh Hậu Kỳ', 'Nguyên Anh Hậu Kỳ', 245000, 2000, 10, 0),
    (19, 'Hóa Thần Sơ Kỳ', 'Hóa Thần Sơ Kỳ', 600000, -1, 60, 0),
    (20, 'Hóa Thần Trung Kỳ', 'Hóa Thần Trung Kỳ', 840000, -1, 55, 0),
    (21, 'Hóa Thần Hậu Kỳ', 'Hóa Thần Hậu Kỳ', 1200000, -1, 30, 0),
    (22, 'Luyện Hư Sơ Kỳ', 'Luyện Hư Sơ Kỳ', 3000000, -1, 30, 0),
    (23, 'Luyện Hư Trung Kỳ', 'Luyện Hư Trung Kỳ', 4200000, -1, 25, 0),
    (24, 'Luyện Hư Hậu Kỳ', 'Luyện Hư Hậu Kỳ', 9000000, -1, 15, 0),
    (25, 'Hợp Thể Sơ Kỳ', 'Hợp Thể Sơ Kỳ', 20000000, -1, 20, 0),
    (26, 'Hợp Thể Trung Kỳ', 'Hợp Thể Trung Kỳ', 28000000, -1, 15, 0),
    (27, 'Hợp Thể Hậu Kỳ', 'Hợp Thể Hậu Kỳ', 40000000, -1, 10, 0),
    (28, 'Độ Kiếp Kỳ', 'Độ Kiếp Kỳ', 100000000, -1, 12, 0),
    (29, 'Chân Tiên Sơ Kỳ', 'Chân Tiên Sơ Kỳ', 250000000, -1, 6, 0),
    (30, 'Chân Tiên Trung Kỳ', 'Chân Tiên Trung Kỳ', 350000000, -1, 5, 0),
    (31, 'Chân Tiên Hậu Kỳ', 'Chân Tiên Hậu Kỳ', 500000000, -1, 4, 0)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    stage_name = EXCLUDED.stage_name,
    max_cultivation = EXCLUDED.max_cultivation,
    lifespan = EXCLUDED.lifespan,
    base_breakthrough_rate = EXCLUDED.base_breakthrough_rate,
    failure_penalty = EXCLUDED.failure_penalty;

INSERT INTO public.map_templates (
    id,
    name,
    map_type,
    client_map_key,
    width,
    height,
    cell_size,
    interest_radius,
    default_spawn_x,
    default_spawn_y,
    max_public_zone_count,
    max_players_per_zone,
    is_private_per_player
)
VALUES
    (1, 'Player Home', 0, 'map_home_01', 256, 256, 32, 96, 64, 64, 0, 1, true),
    (2, 'Starter Plains', 1, 'map_farm_01', 1024, 1024, 64, 160, 128, 128, 2, 20, false)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    map_type = EXCLUDED.map_type,
    client_map_key = EXCLUDED.client_map_key,
    width = EXCLUDED.width,
    height = EXCLUDED.height,
    cell_size = EXCLUDED.cell_size,
    interest_radius = EXCLUDED.interest_radius,
    default_spawn_x = EXCLUDED.default_spawn_x,
    default_spawn_y = EXCLUDED.default_spawn_y,
    max_public_zone_count = EXCLUDED.max_public_zone_count,
    max_players_per_zone = EXCLUDED.max_players_per_zone,
    is_private_per_player = EXCLUDED.is_private_per_player;

INSERT INTO public.map_template_adjacent_maps (
    map_template_id,
    adjacent_map_template_id
)
VALUES
    (1, 2),
    (2, 1)
ON CONFLICT (map_template_id, adjacent_map_template_id) DO NOTHING;

COMMIT;
