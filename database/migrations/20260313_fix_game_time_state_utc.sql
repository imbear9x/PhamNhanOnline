ALTER TABLE public.game_time_state
    ALTER COLUMN anchor_utc TYPE timestamp with time zone
    USING CASE
        WHEN anchor_utc = TIMESTAMP '2026-01-01 00:00:00'
            THEN TIMESTAMPTZ '2026-01-01 00:00:00+00'
        ELSE anchor_utc AT TIME ZONE 'Asia/Saigon'
    END;

ALTER TABLE public.game_time_state
    ALTER COLUMN updated_at TYPE timestamp with time zone
    USING updated_at AT TIME ZONE 'Asia/Saigon';

ALTER TABLE public.game_time_state
    ALTER COLUMN updated_at SET DEFAULT now();
