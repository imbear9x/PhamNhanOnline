BEGIN;

ALTER TABLE public.character_base_stats
    ADD COLUMN IF NOT EXISTS base_move_speed numeric(10, 4) NOT NULL DEFAULT 100.0;

UPDATE public.character_base_stats
SET base_move_speed = COALESCE(base_move_speed, 100.0)
WHERE base_move_speed IS NULL;

ALTER TABLE public.enemy_templates
    ADD COLUMN IF NOT EXISTS base_move_speed numeric(10, 4) NOT NULL DEFAULT 100.0;

UPDATE public.enemy_templates
SET base_move_speed = COALESCE(base_move_speed, 100.0)
WHERE base_move_speed IS NULL;

UPDATE public.enemy_templates
SET base_move_speed = 0
WHERE code = 'wood_doll';

COMMIT;
