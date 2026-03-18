ALTER TABLE public.character_base_stats
    ADD COLUMN IF NOT EXISTS active_martial_art_id integer NULL;
