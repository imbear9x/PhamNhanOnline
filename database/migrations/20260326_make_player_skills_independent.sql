DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'player_skills'
          AND column_name = 'source_skill_group_code'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'player_skills'
          AND column_name = 'skill_group_code'
    ) THEN
        ALTER TABLE public.player_skills
            RENAME COLUMN source_skill_group_code TO skill_group_code;
    END IF;
END $$;

ALTER TABLE IF EXISTS public.player_skills
    ADD COLUMN IF NOT EXISTS skill_group_code character varying(50);

UPDATE public.player_skills ps
SET skill_group_code = s.skill_group_code
FROM public.skills s
WHERE s.id = ps.skill_id
  AND (ps.skill_group_code IS NULL OR btrim(ps.skill_group_code) = '');

ALTER TABLE IF EXISTS public.player_skills
    ALTER COLUMN skill_group_code SET NOT NULL;

ALTER TABLE IF EXISTS public.player_skills
    ADD COLUMN IF NOT EXISTS source_type integer NOT NULL DEFAULT 1;

UPDATE public.player_skills
SET source_type = 2
WHERE source_type IS NULL
   OR source_type <= 0;

ALTER TABLE IF EXISTS public.player_skills
    ALTER COLUMN source_martial_art_id DROP NOT NULL;

ALTER TABLE IF EXISTS public.player_skills
    ALTER COLUMN source_martial_art_skill_id DROP NOT NULL;

WITH ranked AS (
    SELECT
        ps.id,
        first_value(ps.id) OVER (
            PARTITION BY ps.player_id, ps.skill_group_code
            ORDER BY s.skill_level DESC, ps.updated_at DESC NULLS LAST, ps.id DESC
        ) AS keeper_id,
        row_number() OVER (
            PARTITION BY ps.player_id, ps.skill_group_code
            ORDER BY s.skill_level DESC, ps.updated_at DESC NULLS LAST, ps.id DESC
        ) AS rn
    FROM public.player_skills ps
    INNER JOIN public.skills s ON s.id = ps.skill_id
),
losers AS (
    SELECT id, keeper_id
    FROM ranked
    WHERE rn > 1
)
UPDATE public.player_skill_loadouts psl
SET player_skill_id = losers.keeper_id,
    updated_at = now()
FROM losers
WHERE psl.player_skill_id = losers.id
  AND NOT EXISTS (
      SELECT 1
      FROM public.player_skill_loadouts existing
      WHERE existing.player_id = psl.player_id
        AND existing.player_skill_id = losers.keeper_id
  );

WITH ranked AS (
    SELECT
        ps.id,
        row_number() OVER (
            PARTITION BY ps.player_id, ps.skill_group_code
            ORDER BY s.skill_level DESC, ps.updated_at DESC NULLS LAST, ps.id DESC
        ) AS rn
    FROM public.player_skills ps
    INNER JOIN public.skills s ON s.id = ps.skill_id
),
losers AS (
    SELECT id
    FROM ranked
    WHERE rn > 1
)
DELETE FROM public.player_skill_loadouts
WHERE player_skill_id IN (SELECT id FROM losers);

WITH ranked AS (
    SELECT
        ps.id,
        row_number() OVER (
            PARTITION BY ps.player_id, ps.skill_group_code
            ORDER BY s.skill_level DESC, ps.updated_at DESC NULLS LAST, ps.id DESC
        ) AS rn
    FROM public.player_skills ps
    INNER JOIN public.skills s ON s.id = ps.skill_id
),
losers AS (
    SELECT id
    FROM ranked
    WHERE rn > 1
)
DELETE FROM public.player_skills
WHERE id IN (SELECT id FROM losers);

ALTER TABLE IF EXISTS public.player_skills
    DROP CONSTRAINT IF EXISTS player_skills_player_source_skill_key;

DROP INDEX IF EXISTS public.ux_player_skills_player_martial_art_group;

ALTER TABLE IF EXISTS public.player_skills
    DROP CONSTRAINT IF EXISTS player_skills_player_group_key;

ALTER TABLE IF EXISTS public.player_skills
    ADD CONSTRAINT player_skills_player_group_key UNIQUE (player_id, skill_group_code);
