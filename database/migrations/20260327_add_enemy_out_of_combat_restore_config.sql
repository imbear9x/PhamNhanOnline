BEGIN;

ALTER TABLE public.enemy_templates
    ADD COLUMN IF NOT EXISTS enable_out_of_combat_restore boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS out_of_combat_restore_delay_seconds integer NOT NULL DEFAULT 20;

UPDATE public.enemy_templates
SET
    enable_out_of_combat_restore = COALESCE(enable_out_of_combat_restore, true),
    out_of_combat_restore_delay_seconds = COALESCE(out_of_combat_restore_delay_seconds, 20)
WHERE enable_out_of_combat_restore IS NULL
   OR out_of_combat_restore_delay_seconds IS NULL;

UPDATE public.enemy_templates
SET
    enable_out_of_combat_restore = true,
    out_of_combat_restore_delay_seconds = 20
WHERE code = 'wood_doll';

COMMIT;
