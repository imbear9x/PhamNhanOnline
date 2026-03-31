ALTER TABLE IF EXISTS public.map_portals
    ADD COLUMN IF NOT EXISTS interaction_mode integer;

UPDATE public.map_portals
SET interaction_mode = 1
WHERE interaction_mode IS NULL;

ALTER TABLE IF EXISTS public.map_portals
    ALTER COLUMN interaction_mode SET DEFAULT 1;

ALTER TABLE IF EXISTS public.map_portals
    ALTER COLUMN interaction_mode SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_map_portals_interaction_mode_valid'
    ) THEN
        ALTER TABLE public.map_portals
            ADD CONSTRAINT ck_map_portals_interaction_mode_valid
                CHECK (interaction_mode in (1, 2));
    END IF;
END $$;
