BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'character_current_state'
          AND column_name = 'is_dead'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'character_current_state'
          AND column_name = 'is_expired'
    ) THEN
        ALTER TABLE public.character_current_state
            RENAME COLUMN is_dead TO is_expired;
    END IF;
END $$;

ALTER TABLE public.character_current_state
    ADD COLUMN IF NOT EXISTS is_expired boolean NOT NULL DEFAULT false;

UPDATE public.character_current_state
SET is_expired = (current_state = 2);

COMMIT;
