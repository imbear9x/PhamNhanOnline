BEGIN;

CREATE TABLE IF NOT EXISTS public.character_current_state (
    character_id uuid NOT NULL,
    current_hp integer NOT NULL DEFAULT 100,
    current_mp integer NOT NULL DEFAULT 100,
    current_stamina integer NOT NULL DEFAULT 100,
    remaining_lifespan integer NOT NULL DEFAULT 100,
    current_map_id integer,
    current_pos_x real NOT NULL DEFAULT 0,
    current_pos_y real NOT NULL DEFAULT 0,
    is_dead boolean NOT NULL DEFAULT false,
    current_state integer NOT NULL DEFAULT 0,
    last_saved_at timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT character_current_state_pkey PRIMARY KEY (character_id),
    CONSTRAINT fk_character_current_state_character FOREIGN KEY (character_id) REFERENCES public.characters(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_character_current_state_map_id
    ON public.character_current_state USING btree (current_map_id);

INSERT INTO public.character_current_state (
    character_id,
    current_hp,
    current_mp,
    current_stamina,
    remaining_lifespan,
    current_map_id,
    current_pos_x,
    current_pos_y,
    is_dead,
    current_state,
    last_saved_at
)
SELECT
    c.id,
    COALESCE(cbs.base_hp, 100),
    COALESCE(cbs.base_mp, 100),
    100,
    100,
    NULL,
    0,
    0,
    false,
    0,
    now()
FROM public.characters c
LEFT JOIN public.character_base_stats cbs ON cbs.character_id = c.id
ON CONFLICT (character_id) DO NOTHING;

COMMIT;
