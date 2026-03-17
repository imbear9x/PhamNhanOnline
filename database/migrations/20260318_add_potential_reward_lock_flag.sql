ALTER TABLE public.character_base_stats
    ADD COLUMN IF NOT EXISTS potential_reward_locked boolean NOT NULL DEFAULT false;

UPDATE public.character_base_stats
SET potential_reward_locked = COALESCE(potential_reward_locked, false);
