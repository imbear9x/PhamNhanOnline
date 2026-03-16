BEGIN;

ALTER TABLE public.character_base_stats
    ADD COLUMN IF NOT EXISTS unallocated_potential integer NOT NULL DEFAULT 0;

UPDATE public.character_base_stats
SET unallocated_potential = COALESCE(unallocated_potential, COALESCE(base_potential, 0));

ALTER TABLE public.character_current_state
    ADD COLUMN IF NOT EXISTS cultivation_started_at_utc timestamp without time zone NULL,
    ADD COLUMN IF NOT EXISTS last_cultivation_rewarded_at_utc timestamp without time zone NULL;

CREATE INDEX IF NOT EXISTS idx_character_current_state_runtime_state
    ON public.character_current_state USING btree (current_state);

COMMIT;
