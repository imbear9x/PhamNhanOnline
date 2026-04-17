CREATE TABLE IF NOT EXISTS public.game_random_tables (
    id integer NOT NULL,
    table_id character varying(100) NOT NULL,
    mode integer NOT NULL DEFAULT 0,
    luck_enabled boolean NOT NULL DEFAULT false,
    luck_bonus_parts_per_million_per_luck_point integer NOT NULL DEFAULT 0,
    luck_max_bonus_parts_per_million integer NOT NULL DEFAULT 0,
    none_entry_id character varying(100) NOT NULL DEFAULT '__none__',
    description text NULL,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT game_random_tables_pkey PRIMARY KEY (id),
    CONSTRAINT game_random_tables_table_id_key UNIQUE (table_id)
);

CREATE TABLE IF NOT EXISTS public.game_random_entries (
    id integer NOT NULL,
    game_random_table_id integer NOT NULL,
    entry_id character varying(100) NOT NULL,
    chance_parts_per_million integer NOT NULL,
    is_none boolean NOT NULL DEFAULT false,
    order_index integer NOT NULL DEFAULT 0,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT game_random_entries_pkey PRIMARY KEY (id),
    CONSTRAINT game_random_entries_table_entry_key UNIQUE (game_random_table_id, entry_id),
    CONSTRAINT game_random_entries_table_order_key UNIQUE (game_random_table_id, order_index),
    CONSTRAINT fk_game_random_entries_table
        FOREIGN KEY (game_random_table_id) REFERENCES public.game_random_tables(id)
);

CREATE TABLE IF NOT EXISTS public.game_random_entry_tags (
    id integer NOT NULL,
    game_random_entry_id integer NOT NULL,
    tag character varying(50) NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT game_random_entry_tags_pkey PRIMARY KEY (id),
    CONSTRAINT game_random_entry_tags_entry_tag_key UNIQUE (game_random_entry_id, tag),
    CONSTRAINT fk_game_random_entry_tags_entry
        FOREIGN KEY (game_random_entry_id) REFERENCES public.game_random_entries(id)
);

CREATE TABLE IF NOT EXISTS public.game_random_luck_tags (
    id integer NOT NULL,
    game_random_table_id integer NOT NULL,
    tag character varying(50) NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT game_random_luck_tags_pkey PRIMARY KEY (id),
    CONSTRAINT game_random_luck_tags_table_tag_key UNIQUE (game_random_table_id, tag),
    CONSTRAINT fk_game_random_luck_tags_table
        FOREIGN KEY (game_random_table_id) REFERENCES public.game_random_tables(id)
);

INSERT INTO public.game_random_tables (
    id,
    table_id,
    mode,
    luck_enabled,
    luck_bonus_parts_per_million_per_luck_point,
    luck_max_bonus_parts_per_million,
    none_entry_id,
    description
)
VALUES (
    1,
    'monster.drop.demo_slime',
    0,
    true,
    2500,
    150000,
    '__none__',
    'Bang random demo slime drop duoc migrate tu gameRandomConfig.json'
)
ON CONFLICT (id) DO UPDATE
SET
    table_id = EXCLUDED.table_id,
    mode = EXCLUDED.mode,
    luck_enabled = EXCLUDED.luck_enabled,
    luck_bonus_parts_per_million_per_luck_point = EXCLUDED.luck_bonus_parts_per_million_per_luck_point,
    luck_max_bonus_parts_per_million = EXCLUDED.luck_max_bonus_parts_per_million,
    none_entry_id = EXCLUDED.none_entry_id,
    description = EXCLUDED.description;

INSERT INTO public.game_random_entries (
    id,
    game_random_table_id,
    entry_id,
    chance_parts_per_million,
    is_none,
    order_index
)
VALUES
    (1, 1, 'item.demo_herb', 50000, false, 1),
    (2, 1, 'currency.spirit_stone_small', 100000, false, 2)
ON CONFLICT (id) DO UPDATE
SET
    game_random_table_id = EXCLUDED.game_random_table_id,
    entry_id = EXCLUDED.entry_id,
    chance_parts_per_million = EXCLUDED.chance_parts_per_million,
    is_none = EXCLUDED.is_none,
    order_index = EXCLUDED.order_index;

INSERT INTO public.game_random_entry_tags (id, game_random_entry_id, tag)
VALUES
    (1, 1, 'item_drop'),
    (2, 2, 'currency_drop')
ON CONFLICT (id) DO UPDATE
SET
    game_random_entry_id = EXCLUDED.game_random_entry_id,
    tag = EXCLUDED.tag;
