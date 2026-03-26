ALTER TABLE IF EXISTS public.skills
    ADD COLUMN IF NOT EXISTS skill_group_code character varying(50);

ALTER TABLE IF EXISTS public.skills
    ADD COLUMN IF NOT EXISTS skill_level integer NOT NULL DEFAULT 1;

UPDATE public.skills
SET skill_group_code = code
WHERE skill_group_code IS NULL OR btrim(skill_group_code) = '';

ALTER TABLE IF EXISTS public.skills
    ALTER COLUMN skill_group_code SET NOT NULL;

ALTER TABLE IF EXISTS public.skills
    ALTER COLUMN skill_level SET DEFAULT 1;

CREATE UNIQUE INDEX IF NOT EXISTS ux_skills_skill_group_code_level
    ON public.skills (skill_group_code, skill_level);

ALTER TABLE IF EXISTS public.player_skills
    ADD COLUMN IF NOT EXISTS source_skill_group_code character varying(50);

UPDATE public.player_skills ps
SET source_skill_group_code = s.skill_group_code
FROM public.skills s
WHERE s.id = ps.skill_id
  AND (ps.source_skill_group_code IS NULL OR btrim(ps.source_skill_group_code) = '');

WITH ranked AS (
    SELECT
        ps.id,
        first_value(ps.id) OVER (
            PARTITION BY ps.player_id, ps.source_martial_art_id, ps.source_skill_group_code
            ORDER BY s.skill_level DESC, ps.updated_at DESC NULLS LAST, ps.id DESC
        ) AS keeper_id,
        row_number() OVER (
            PARTITION BY ps.player_id, ps.source_martial_art_id, ps.source_skill_group_code
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
            PARTITION BY ps.player_id, ps.source_martial_art_id, ps.source_skill_group_code
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
            PARTITION BY ps.player_id, ps.source_martial_art_id, ps.source_skill_group_code
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

ALTER TABLE IF EXISTS public.player_skills
    ALTER COLUMN source_skill_group_code SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_player_skills_player_martial_art_group
    ON public.player_skills (player_id, source_martial_art_id, source_skill_group_code);

DROP TABLE IF EXISTS public.martial_art_skill_scalings;
