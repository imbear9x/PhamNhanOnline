BEGIN;

ALTER TABLE public.character_current_state
    ADD COLUMN IF NOT EXISTS current_zone_index integer NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS public.map_templates (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    map_type integer NOT NULL,
    client_map_key character varying(100) NOT NULL,
    width real NOT NULL DEFAULT 0,
    height real NOT NULL DEFAULT 0,
    cell_size real NOT NULL DEFAULT 1,
    interest_radius real NOT NULL DEFAULT 0,
    default_spawn_x real NOT NULL DEFAULT 0,
    default_spawn_y real NOT NULL DEFAULT 0,
    max_public_zone_count integer NOT NULL DEFAULT 0,
    max_players_per_zone integer NOT NULL DEFAULT 1,
    is_private_per_player boolean NOT NULL DEFAULT false,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT map_templates_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.map_template_adjacent_maps (
    map_template_id integer NOT NULL,
    adjacent_map_template_id integer NOT NULL,
    CONSTRAINT map_template_adjacent_maps_pkey PRIMARY KEY (map_template_id, adjacent_map_template_id),
    CONSTRAINT fk_map_template_adjacent_maps_source FOREIGN KEY (map_template_id) REFERENCES public.map_templates(id) ON DELETE CASCADE,
    CONSTRAINT fk_map_template_adjacent_maps_target FOREIGN KEY (adjacent_map_template_id) REFERENCES public.map_templates(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_character_current_state_map_zone
    ON public.character_current_state USING btree (current_map_id, current_zone_index);

INSERT INTO public.map_templates (
    id,
    name,
    map_type,
    client_map_key,
    width,
    height,
    cell_size,
    interest_radius,
    default_spawn_x,
    default_spawn_y,
    max_public_zone_count,
    max_players_per_zone,
    is_private_per_player
)
VALUES
    (1, 'Player Home', 0, 'map_home_01', 256, 256, 32, 96, 64, 64, 0, 1, true),
    (2, 'Starter Plains', 1, 'map_farm_01', 1024, 1024, 64, 160, 128, 128, 2, 20, false)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    map_type = EXCLUDED.map_type,
    client_map_key = EXCLUDED.client_map_key,
    width = EXCLUDED.width,
    height = EXCLUDED.height,
    cell_size = EXCLUDED.cell_size,
    interest_radius = EXCLUDED.interest_radius,
    default_spawn_x = EXCLUDED.default_spawn_x,
    default_spawn_y = EXCLUDED.default_spawn_y,
    max_public_zone_count = EXCLUDED.max_public_zone_count,
    max_players_per_zone = EXCLUDED.max_players_per_zone,
    is_private_per_player = EXCLUDED.is_private_per_player;

INSERT INTO public.map_template_adjacent_maps (map_template_id, adjacent_map_template_id)
VALUES
    (1, 2),
    (2, 1)
ON CONFLICT (map_template_id, adjacent_map_template_id) DO NOTHING;

COMMIT;
