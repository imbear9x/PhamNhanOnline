BEGIN;

ALTER TABLE public.enemy_templates
    ADD COLUMN IF NOT EXISTS ai_behavior integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS patrol_pause_seconds_min numeric(10, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS patrol_pause_seconds_max numeric(10, 4) NOT NULL DEFAULT 0;

UPDATE public.enemy_templates
SET
    ai_behavior = COALESCE(ai_behavior, 1),
    patrol_pause_seconds_min = COALESCE(patrol_pause_seconds_min, 0),
    patrol_pause_seconds_max = COALESCE(patrol_pause_seconds_max, 0)
WHERE ai_behavior IS NULL
   OR patrol_pause_seconds_min IS NULL
   OR patrol_pause_seconds_max IS NULL;

ALTER TABLE public.map_enemy_spawn_groups
    ADD COLUMN IF NOT EXISTS patrol_route_type integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS patrol_radius real NOT NULL DEFAULT 0;

COMMIT;
