BEGIN;

DROP TABLE IF EXISTS public.map_enemy_spawn_group_waypoints;

ALTER TABLE public.map_enemy_spawn_groups
    ADD COLUMN IF NOT EXISTS patrol_radius real NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS patrol_route_type integer NOT NULL DEFAULT 1,
    DROP COLUMN IF EXISTS waypoint_traversal_mode;

UPDATE public.map_enemy_spawn_groups
SET patrol_route_type = 1
WHERE patrol_route_type NOT IN (1, 2);

COMMIT;
