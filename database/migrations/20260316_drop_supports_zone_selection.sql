BEGIN;

ALTER TABLE public.map_templates
    DROP COLUMN IF EXISTS supports_zone_selection;

UPDATE public.map_templates m
SET supports_cave_placement = EXISTS (
    SELECT 1
    FROM public.map_zone_slots z
    WHERE z.map_template_id = m.id
);

COMMIT;
