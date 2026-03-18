ALTER TABLE public.herb_growth_stage_configs
    DROP COLUMN IF EXISTS age_years;

ALTER TABLE public.player_herbs
    DROP COLUMN IF EXISTS current_age_years;
