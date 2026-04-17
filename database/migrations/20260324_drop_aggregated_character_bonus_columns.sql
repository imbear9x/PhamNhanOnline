BEGIN;

ALTER TABLE public.character_base_stats
    DROP COLUMN IF EXISTS bonus_hp,
    DROP COLUMN IF EXISTS bonus_mp,
    DROP COLUMN IF EXISTS bonus_attack,
    DROP COLUMN IF EXISTS bonus_speed,
    DROP COLUMN IF EXISTS bonus_sense,
    DROP COLUMN IF EXISTS bonus_luck;

COMMIT;
