BEGIN;

ALTER TABLE public.map_enemy_spawn_groups
    ADD COLUMN IF NOT EXISTS patrol_radius real NOT NULL DEFAULT 0;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns column_info
        WHERE column_info.table_schema = 'public'
          AND column_info.table_name = 'enemy_templates'
          AND column_info.column_name = 'patrol_radius') THEN
        EXECUTE '
            UPDATE public.map_enemy_spawn_groups spawn_group
            SET patrol_radius = legacy_template_radius.patrol_radius
            FROM (
                SELECT
                    spawn_entry.spawn_group_id,
                    COALESCE(MAX(enemy_template.patrol_radius)::real, 0) AS patrol_radius
                FROM public.map_enemy_spawn_entries spawn_entry
                JOIN public.enemy_templates enemy_template
                    ON enemy_template.id = spawn_entry.enemy_template_id
                GROUP BY spawn_entry.spawn_group_id
            ) legacy_template_radius
            WHERE spawn_group.id = legacy_template_radius.spawn_group_id
              AND spawn_group.patrol_radius = 0';
    END IF;
END $$;

ALTER TABLE public.enemy_templates
    DROP COLUMN IF EXISTS patrol_radius;

COMMIT;
