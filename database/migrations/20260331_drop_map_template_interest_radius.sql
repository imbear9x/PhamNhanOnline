BEGIN;

ALTER TABLE public.map_templates
    DROP COLUMN IF EXISTS interest_radius;

COMMIT;
