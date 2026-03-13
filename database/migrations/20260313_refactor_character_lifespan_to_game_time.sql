BEGIN;

ALTER TABLE public.character_current_state
    ADD COLUMN IF NOT EXISTS lifespan_end_game_minute bigint;

WITH game_time AS (
    SELECT floor(extract(epoch FROM ((now() AT TIME ZONE 'UTC') - timestamp '2026-01-01 00:00:00')) / 60.0 * 1440)::bigint AS current_game_minute
)
UPDATE public.character_current_state ccs
SET lifespan_end_game_minute = CASE
    WHEN ccs.remaining_lifespan = -1 THEN -1
    ELSE gt.current_game_minute + (ccs.remaining_lifespan::bigint * 360 * 1440)
END
FROM game_time gt
WHERE ccs.lifespan_end_game_minute IS NULL
   OR ccs.lifespan_end_game_minute = 0;

ALTER TABLE public.character_current_state
    ALTER COLUMN lifespan_end_game_minute SET NOT NULL;

ALTER TABLE public.character_current_state
    ALTER COLUMN lifespan_end_game_minute SET DEFAULT 0;

ALTER TABLE public.character_current_state
    DROP COLUMN IF EXISTS remaining_lifespan;

COMMIT;
