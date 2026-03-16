BEGIN;

ALTER TABLE public.character_base_stats
    DROP COLUMN IF EXISTS base_physique,
    DROP COLUMN IF EXISTS bonus_physique,
    DROP COLUMN IF EXISTS physique_upgrade_count,
    ADD COLUMN IF NOT EXISTS unallocated_potential integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS cultivation_progress numeric(18,6) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS bonus_hp integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS bonus_mp integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS bonus_attack integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS bonus_speed integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS bonus_spiritual_sense integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS bonus_fortune double precision NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS hp_upgrade_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS mp_upgrade_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS attack_upgrade_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS speed_upgrade_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS spiritual_sense_upgrade_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS fortune_upgrade_count integer NOT NULL DEFAULT 0;

UPDATE public.character_base_stats
SET
    unallocated_potential = COALESCE(unallocated_potential, COALESCE(base_potential, 0)),
    cultivation_progress = COALESCE(cultivation_progress, 0);

ALTER TABLE public.character_current_state
    ADD COLUMN IF NOT EXISTS cultivation_started_at_utc timestamp without time zone NULL,
    ADD COLUMN IF NOT EXISTS last_cultivation_rewarded_at_utc timestamp without time zone NULL;

CREATE TABLE IF NOT EXISTS public.spiritual_energy_templates (
    id integer NOT NULL,
    code character varying(50) NOT NULL,
    name character varying(100) NOT NULL,
    lk_per_minute numeric(10,4) NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT spiritual_energy_templates_pkey PRIMARY KEY (id),
    CONSTRAINT spiritual_energy_templates_code_key UNIQUE (code)
);

DROP TABLE IF EXISTS public.potential_stat_templates;

CREATE TABLE IF NOT EXISTS public.potential_stat_upgrade_tiers (
    target_stat integer NOT NULL,
    tier_index integer NOT NULL,
    max_upgrade_count integer NOT NULL,
    potential_cost_per_upgrade integer NOT NULL,
    stat_gain_per_upgrade numeric(18,6) NOT NULL,
    is_enabled boolean NOT NULL DEFAULT true,
    CONSTRAINT potential_stat_upgrade_tiers_pkey PRIMARY KEY (target_stat, tier_index)
);

ALTER TABLE public.realm_templates
    ADD COLUMN IF NOT EXISTS absorption_multiplier numeric(10,4) NOT NULL DEFAULT 1.0;

ALTER TABLE public.map_templates
    ADD COLUMN IF NOT EXISTS spiritual_energy numeric(10,4) NOT NULL DEFAULT 100,
    ADD COLUMN IF NOT EXISTS supports_cave_placement boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS public.map_zone_slots (
    id integer NOT NULL,
    map_template_id integer NOT NULL,
    zone_index integer NOT NULL,
    spiritual_energy_template_id integer NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT map_zone_slots_pkey PRIMARY KEY (id),
    CONSTRAINT map_zone_slots_map_template_zone_key UNIQUE (map_template_id, zone_index),
    CONSTRAINT fk_map_zone_slots_map_template
        FOREIGN KEY (map_template_id) REFERENCES public.map_templates(id),
    CONSTRAINT fk_map_zone_slots_spiritual_energy
        FOREIGN KEY (spiritual_energy_template_id) REFERENCES public.spiritual_energy_templates(id)
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_map_templates_spiritual_energy'
    ) THEN
        ALTER TABLE public.map_templates
            ADD CONSTRAINT fk_map_templates_spiritual_energy
            FOREIGN KEY (spiritual_energy_template_id) REFERENCES public.spiritual_energy_templates(id);
    END IF;
END $$;

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
    absorption_multiplier,
    failure_penalty
)
VALUES
    (1, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 1', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 1', 150, 120, 100, 0.2, 0),
    (2, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 2', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 2', 200, 125, 95, 0.2, 0),
    (3, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 3', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 3', 280, 130, 90, 0.2, 0),
    (4, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 4', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 4', 380, 135, 85, 0.2, 0),
    (5, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 5', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 5', 520, 140, 80, 0.2, 0),
    (6, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 6', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 6', 750, 145, 75, 0.2, 0),
    (7, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 7', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 7', 1200, 150, 70, 0.2, 0),
    (8, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 8', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 8', 1500, 155, 65, 0.2, 0),
    (9, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 9', 'Luyá»‡n KhÃ­ Ká»³ táº§ng 9', 2000, 160, 40, 0.2, 0),
    (10, 'TrÃºc CÆ¡ SÆ¡ Ká»³', 'TrÃºc CÆ¡ SÆ¡ Ká»³', 5000, 180, 40, 0.3, 0),
    (11, 'TrÃºc CÆ¡ Trung Ká»³', 'TrÃºc CÆ¡ Trung Ká»³', 7000, 200, 35, 0.3, 0),
    (12, 'TrÃºc CÆ¡ Háº­u Ká»³', 'TrÃºc CÆ¡ Háº­u Ká»³', 10000, 220, 25, 0.3, 0),
    (13, 'Káº¿t Äan SÆ¡ Ká»³', 'Káº¿t Äan SÆ¡ Ká»³', 25000, 350, 30, 0.5, 0),
    (14, 'Káº¿t Äan Trung Ká»³', 'Káº¿t Äan Trung Ká»³', 35000, 400, 28, 0.5, 0),
    (15, 'Káº¿t Äan Háº­u Ká»³', 'Káº¿t Äan Háº­u Ká»³', 50000, 500, 20, 0.5, 0),
    (16, 'NguyÃªn Anh SÆ¡ Ká»³', 'NguyÃªn Anh SÆ¡ Ká»³', 125000, 1200, 18, 0.7, 0),
    (17, 'NguyÃªn Anh Trung Ká»³', 'NguyÃªn Anh Trung Ká»³', 175000, 1500, 15, 0.7, 0),
    (18, 'NguyÃªn Anh Háº­u Ká»³', 'NguyÃªn Anh Háº­u Ká»³', 245000, 2000, 10, 0.7, 0),
    (19, 'HÃ³a Tháº§n SÆ¡ Ká»³', 'HÃ³a Tháº§n SÆ¡ Ká»³', 600000, -1, 60, 1.0, 0),
    (20, 'HÃ³a Tháº§n Trung Ká»³', 'HÃ³a Tháº§n Trung Ká»³', 840000, -1, 55, 1.0, 0),
    (21, 'HÃ³a Tháº§n Háº­u Ká»³', 'HÃ³a Tháº§n Háº­u Ká»³', 1200000, -1, 30, 1.0, 0),
    (22, 'Luyá»‡n HÆ° SÆ¡ Ká»³', 'Luyá»‡n HÆ° SÆ¡ Ká»³', 3000000, -1, 30, 1.4, 0),
    (23, 'Luyá»‡n HÆ° Trung Ká»³', 'Luyá»‡n HÆ° Trung Ká»³', 4200000, -1, 25, 1.4, 0),
    (24, 'Luyá»‡n HÆ° Háº­u Ká»³', 'Luyá»‡n HÆ° Háº­u Ká»³', 9000000, -1, 15, 1.4, 0),
    (25, 'Há»£p Thá»ƒ SÆ¡ Ká»³', 'Há»£p Thá»ƒ SÆ¡ Ká»³', 20000000, -1, 20, 2.0, 0),
    (26, 'Há»£p Thá»ƒ Trung Ká»³', 'Há»£p Thá»ƒ Trung Ká»³', 28000000, -1, 15, 2.0, 0),
    (27, 'Há»£p Thá»ƒ Háº­u Ká»³', 'Há»£p Thá»ƒ Háº­u Ká»³', 40000000, -1, 10, 2.0, 0),
    (28, 'Äá»™ Kiáº¿p Ká»³', 'Äá»™ Kiáº¿p Ká»³', 100000000, -1, 12, 3.0, 0),
    (29, 'ChÃ¢n TiÃªn SÆ¡ Ká»³', 'ChÃ¢n TiÃªn SÆ¡ Ká»³', 250000000, -1, 6, 3.0, 0),
    (30, 'ChÃ¢n TiÃªn Trung Ká»³', 'ChÃ¢n TiÃªn Trung Ká»³', 350000000, -1, 5, 3.0, 0),
    (31, 'ChÃ¢n TiÃªn Háº­u Ká»³', 'ChÃ¢n TiÃªn Háº­u Ká»³', 500000000, -1, 4, 3.0, 0)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    stage_name = EXCLUDED.stage_name,
    max_cultivation = EXCLUDED.max_cultivation,
    lifespan = EXCLUDED.lifespan,
    base_breakthrough_rate = EXCLUDED.base_breakthrough_rate,
    absorption_multiplier = EXCLUDED.absorption_multiplier,
    failure_penalty = EXCLUDED.failure_penalty;

INSERT INTO public.spiritual_energy_templates (
    id,
    code,
    name,
    lk_per_minute
)
VALUES
    (1, 'low', 'Low', 0.8),
    (2, 'medium', 'Medium', 1.0),
    (3, 'high', 'High', 1.5),
    (4, 'dense', 'Dense', 2.0)
ON CONFLICT (id) DO UPDATE
SET
    code = EXCLUDED.code,
    name = EXCLUDED.name,
    lk_per_minute = EXCLUDED.lk_per_minute;

INSERT INTO public.potential_stat_upgrade_tiers (
    target_stat,
    tier_index,
    max_upgrade_count,
    potential_cost_per_upgrade,
    stat_gain_per_upgrade,
    is_enabled
)
VALUES
    (1, 1, 5, 10, 10.000000, TRUE),
    (2, 1, 5, 10, 10.000000, TRUE),
    (4, 1, 5, 1, 1.000000, TRUE),
    (5, 1, 5, 1, 1.000000, TRUE),
    (6, 1, 5, 1, 1.000000, TRUE),
    (7, 1, 5, 1, 0.010000, TRUE)
ON CONFLICT (target_stat, tier_index) DO UPDATE
SET
    max_upgrade_count = EXCLUDED.max_upgrade_count,
    potential_cost_per_upgrade = EXCLUDED.potential_cost_per_upgrade,
    stat_gain_per_upgrade = EXCLUDED.stat_gain_per_upgrade,
    is_enabled = EXCLUDED.is_enabled;

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
    supports_cave_placement,
    is_private_per_player,
    spiritual_energy
)
VALUES
    (1, 'Player Home', 0, 'map_home_01', 256, 256, 32, 96, 64, 64, 0, 1, false, true, 100),
    (2, 'Starter Plains', 1, 'map_farm_01', 1024, 1024, 64, 160, 128, 128, 20, 20, true, false, 100)
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
    supports_cave_placement = EXCLUDED.supports_cave_placement,
    is_private_per_player = EXCLUDED.is_private_per_player,
    spiritual_energy = EXCLUDED.spiritual_energy;

INSERT INTO public.map_zone_slots (
    id,
    map_template_id,
    zone_index,
    spiritual_energy_template_id
)
VALUES
    (2001, 2, 1, 1),
    (2002, 2, 2, 1),
    (2003, 2, 3, 1),
    (2004, 2, 4, 1),
    (2005, 2, 5, 1),
    (2006, 2, 6, 2),
    (2007, 2, 7, 2),
    (2008, 2, 8, 2),
    (2009, 2, 9, 2),
    (2010, 2, 10, 2),
    (2011, 2, 11, 2),
    (2012, 2, 12, 2),
    (2013, 2, 13, 2),
    (2014, 2, 14, 2),
    (2015, 2, 15, 3),
    (2016, 2, 16, 3),
    (2017, 2, 17, 3),
    (2018, 2, 18, 3),
    (2019, 2, 19, 4),
    (2020, 2, 20, 4)
ON CONFLICT (id) DO UPDATE
SET
    map_template_id = EXCLUDED.map_template_id,
    zone_index = EXCLUDED.zone_index,
    spiritual_energy_template_id = EXCLUDED.spiritual_energy_template_id;

INSERT INTO public.map_template_adjacent_maps (
    map_template_id,
    adjacent_map_template_id
)
VALUES
    (1, 2),
    (2, 1)
ON CONFLICT (map_template_id, adjacent_map_template_id) DO NOTHING;

COMMIT;
