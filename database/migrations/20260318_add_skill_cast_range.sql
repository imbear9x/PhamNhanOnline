ALTER TABLE public.skills
ADD COLUMN IF NOT EXISTS cast_range numeric(10, 4) NOT NULL DEFAULT 1.0;
