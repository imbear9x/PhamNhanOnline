ALTER TABLE public.herb_templates
    ADD COLUMN IF NOT EXISTS replant_item_template_id integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'herb_templates_replant_item_template_key'
    ) THEN
        ALTER TABLE public.herb_templates
            ADD CONSTRAINT herb_templates_replant_item_template_key UNIQUE (replant_item_template_id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_herb_templates_replant_item_template'
    ) THEN
        ALTER TABLE public.herb_templates
            ADD CONSTRAINT fk_herb_templates_replant_item_template
            FOREIGN KEY (replant_item_template_id) REFERENCES public.item_templates(id);
    END IF;
END $$;
