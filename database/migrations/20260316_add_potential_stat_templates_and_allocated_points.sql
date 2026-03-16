ALTER TABLE public.character_base_stats
    DROP COLUMN IF EXISTS base_physique,
    DROP COLUMN IF EXISTS bonus_physique,
    DROP COLUMN IF EXISTS physique_upgrade_count,
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

DROP TABLE IF EXISTS public.potential_stat_templates;
DROP TABLE IF EXISTS public.potential_stat_upgrade_tiers;

CREATE TABLE public.potential_stat_upgrade_tiers
(
    target_stat integer NOT NULL,
    tier_index integer NOT NULL,
    max_upgrade_count integer NOT NULL,
    potential_cost_per_upgrade integer NOT NULL,
    stat_gain_per_upgrade numeric(18, 6) NOT NULL,
    is_enabled boolean NOT NULL DEFAULT TRUE,
    CONSTRAINT potential_stat_upgrade_tiers_pkey PRIMARY KEY (target_stat, tier_index)
);

INSERT INTO public.potential_stat_upgrade_tiers (
    target_stat,
    tier_index,
    max_upgrade_count,
    potential_cost_per_upgrade,
    stat_gain_per_upgrade,
    is_enabled)
VALUES
    (1, 1, 5, 10, 10.000000, TRUE),
    (2, 1, 5, 10, 10.000000, TRUE),
    (4, 1, 5, 1, 1.000000, TRUE),
    (5, 1, 5, 1, 1.000000, TRUE),
    (6, 1, 5, 1, 1.000000, TRUE),
    (7, 1, 5, 1, 0.010000, TRUE)
ON CONFLICT (target_stat, tier_index) DO UPDATE
SET max_upgrade_count = EXCLUDED.max_upgrade_count,
    potential_cost_per_upgrade = EXCLUDED.potential_cost_per_upgrade,
    stat_gain_per_upgrade = EXCLUDED.stat_gain_per_upgrade,
    is_enabled = EXCLUDED.is_enabled;
