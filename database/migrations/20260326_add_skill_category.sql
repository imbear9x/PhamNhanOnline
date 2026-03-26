ALTER TABLE IF EXISTS public.skills
    ADD COLUMN IF NOT EXISTS skill_category integer NOT NULL DEFAULT 2;
