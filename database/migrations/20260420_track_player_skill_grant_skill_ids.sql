BEGIN;

ALTER TABLE IF EXISTS public.player_skill_grant_sources
    ADD COLUMN IF NOT EXISTS granted_skill_id integer NULL;

UPDATE public.player_skill_grant_sources AS source
SET granted_skill_id = player_skill.skill_id
FROM public.player_skills AS player_skill
WHERE source.player_skill_id = player_skill.id
  AND source.granted_skill_id IS NULL;

ALTER TABLE IF EXISTS public.player_skill_grant_sources
    ALTER COLUMN granted_skill_id SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_player_skill_grant_sources_granted_skill'
    ) THEN
        ALTER TABLE public.player_skill_grant_sources
            ADD CONSTRAINT fk_player_skill_grant_sources_granted_skill
            FOREIGN KEY (granted_skill_id) REFERENCES public.skills(id);
    END IF;
END $$;

COMMIT;
