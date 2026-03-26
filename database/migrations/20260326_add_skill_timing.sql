ALTER TABLE IF EXISTS public.skills
    ADD COLUMN IF NOT EXISTS cast_time_ms integer NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS public.skills
    ADD COLUMN IF NOT EXISTS travel_time_ms integer NOT NULL DEFAULT 0;
