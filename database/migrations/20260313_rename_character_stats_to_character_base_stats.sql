BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'character_stats'
    ) THEN
        ALTER TABLE public.character_stats RENAME TO character_base_stats;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'hp'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN hp TO base_hp;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'mp'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN mp TO base_mp;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'physique'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN physique TO base_physique;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'attack'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN attack TO base_attack;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'speed'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN speed TO base_speed;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'spiritual_sense'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN spiritual_sense TO base_spiritual_sense;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'fortune'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN fortune TO base_fortune;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'character_base_stats' AND column_name = 'potential'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME COLUMN potential TO base_potential;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'character_stats_pkey'
    ) THEN
        ALTER TABLE public.character_base_stats RENAME CONSTRAINT character_stats_pkey TO character_base_stats_pkey;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_stats_character'
    ) THEN
        ALTER TABLE public.character_base_stats
            RENAME CONSTRAINT fk_stats_character TO fk_character_base_stats_character;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'character_stats_character_id_not_null'
    ) THEN
        ALTER TABLE public.character_base_stats
            RENAME CONSTRAINT character_stats_character_id_not_null TO character_base_stats_character_id_not_null;
    END IF;
END $$;

COMMIT;
