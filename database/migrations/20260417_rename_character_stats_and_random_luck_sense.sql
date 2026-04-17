DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'base_spiritual_sense'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN base_spiritual_sense TO base_sense;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'base_fortune'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN base_fortune TO base_luck;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'spiritual_sense_upgrade_count'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN spiritual_sense_upgrade_count TO sense_upgrade_count;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'fortune_upgrade_count'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN fortune_upgrade_count TO luck_upgrade_count;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'bonus_spiritual_sense'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN bonus_spiritual_sense TO bonus_sense;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'bonus_fortune'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN bonus_fortune TO bonus_luck;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'game_random_tables' AND column_name = 'fortune_enabled'
    ) THEN
        ALTER TABLE public.game_random_tables RENAME COLUMN fortune_enabled TO luck_enabled;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'game_random_tables' AND column_name = 'fortune_bonus_parts_per_million_per_fortune_point'
    ) THEN
        ALTER TABLE public.game_random_tables RENAME COLUMN fortune_bonus_parts_per_million_per_fortune_point TO luck_bonus_parts_per_million_per_luck_point;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'game_random_tables' AND column_name = 'fortune_max_bonus_parts_per_million'
    ) THEN
        ALTER TABLE public.game_random_tables RENAME COLUMN fortune_max_bonus_parts_per_million TO luck_max_bonus_parts_per_million;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'game_random_fortune_tags'
    ) THEN
        ALTER TABLE public.game_random_fortune_tags RENAME TO game_random_luck_tags;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'game_random_fortune_tags_pkey'
    ) THEN
        ALTER TABLE public.game_random_luck_tags RENAME CONSTRAINT game_random_fortune_tags_pkey TO game_random_luck_tags_pkey;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'game_random_fortune_tags_table_tag_key'
    ) THEN
        ALTER TABLE public.game_random_luck_tags RENAME CONSTRAINT game_random_fortune_tags_table_tag_key TO game_random_luck_tags_table_tag_key;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_game_random_fortune_tags_table'
    ) THEN
        ALTER TABLE public.game_random_luck_tags RENAME CONSTRAINT fk_game_random_fortune_tags_table TO fk_game_random_luck_tags_table;
    END IF;
END $$;
