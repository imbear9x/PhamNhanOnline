BEGIN;

ALTER TABLE IF EXISTS public.item_templates
    ADD COLUMN IF NOT EXISTS description_template text NULL;

ALTER TABLE IF EXISTS public.skills
    ADD COLUMN IF NOT EXISTS description_template text NULL;

ALTER TABLE IF EXISTS public.martial_arts
    ADD COLUMN IF NOT EXISTS description_template text NULL;

UPDATE public.skills
SET description_template = '{effects_summary}'
WHERE COALESCE(BTRIM(description_template), '') = '';

UPDATE public.martial_arts
SET description_template = '{qi_summary}
{stage_summary}
{unlocked_skills_summary}'
WHERE COALESCE(BTRIM(description_template), '') = '';

UPDATE public.item_templates
SET description_template = CASE
    WHEN item_type = 1 THEN '{equipment_stats_summary}
{requirements_summary}'
    WHEN item_type = 2 THEN '{use_effects_summary}'
    WHEN item_type = 5 THEN '{martial_art_book_summary}'
    WHEN item_type = 8 THEN '{pill_recipe_book_summary}'
    WHEN item_type = 9 THEN '{herb_seed_summary}'
    WHEN item_type = 11 THEN '{soil_summary}'
    WHEN item_type = 12 THEN '{herb_plant_summary}'
    WHEN COALESCE(BTRIM(description), '') <> '' THEN description
    WHEN item_type = 6 THEN 'Tien te co ban duoc dung trong giao dich va chi tieu.'
    WHEN item_type = 3 THEN 'Nguyen lieu co the dung de che tao hoac luyen dan.'
    WHEN item_type = 4 THEN 'Vat pham dac thu can target/context rieng de su dung.'
    ELSE ''
END
WHERE COALESCE(BTRIM(description_template), '') = '';

COMMIT;
