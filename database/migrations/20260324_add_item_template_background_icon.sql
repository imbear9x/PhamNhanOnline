ALTER TABLE IF EXISTS public.item_templates
    ADD COLUMN IF NOT EXISTS background_icon character varying(255) NULL;
