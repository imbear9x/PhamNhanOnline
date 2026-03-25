ALTER TABLE public.martial_arts
    ADD COLUMN IF NOT EXISTS icon character varying(100) NULL;

UPDATE public.martial_arts
SET icon = code
WHERE icon IS NULL OR btrim(icon) = '';
