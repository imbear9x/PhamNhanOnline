ALTER TABLE public.martial_arts
    ADD COLUMN IF NOT EXISTS qi_absorption_rate numeric(10,4) NOT NULL DEFAULT 1.0;

UPDATE public.martial_arts
SET qi_absorption_rate = COALESCE(qi_absorption_rate, 1.0)
WHERE qi_absorption_rate IS NULL;
