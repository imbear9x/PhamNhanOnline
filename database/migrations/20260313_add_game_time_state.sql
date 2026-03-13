CREATE TABLE IF NOT EXISTS public.game_time_state (
    id integer NOT NULL,
    anchor_utc timestamp with time zone NOT NULL,
    anchor_game_minute bigint NOT NULL,
    game_minutes_per_real_minute double precision NOT NULL,
    days_per_game_year integer NOT NULL,
    runtime_save_interval_seconds integer NOT NULL DEFAULT 2,
    derived_state_refresh_interval_seconds integer NOT NULL DEFAULT 5,
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT game_time_state_pkey PRIMARY KEY (id)
);

INSERT INTO public.game_time_state (
    id,
    anchor_utc,
    anchor_game_minute,
    game_minutes_per_real_minute,
    days_per_game_year,
    runtime_save_interval_seconds,
    derived_state_refresh_interval_seconds,
    updated_at
)
VALUES (
    1,
    TIMESTAMPTZ '2026-01-01 00:00:00+00',
    0,
    518400,
    360,
    2,
    5,
    now()
)
ON CONFLICT (id) DO NOTHING;
