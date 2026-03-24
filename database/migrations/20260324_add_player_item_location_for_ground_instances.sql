ALTER TABLE IF EXISTS public.player_items
    ADD COLUMN IF NOT EXISTS location_type integer NOT NULL DEFAULT 1;

ALTER TABLE IF EXISTS public.player_items
    ALTER COLUMN player_id DROP NOT NULL;

UPDATE public.player_items
SET location_type = 1
WHERE location_type IS DISTINCT FROM 1;
