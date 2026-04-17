BEGIN;

ALTER TABLE public.realm_templates
    DROP COLUMN IF EXISTS stage_name;

COMMIT;
