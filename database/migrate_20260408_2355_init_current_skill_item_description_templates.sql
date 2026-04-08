BEGIN;

-- Skills: tailor current live templates for easier client-side flow testing.
-- Current ratio_value semantics in skill data use 1.0 = 100%, 1.1 = 110%, 2.0 = 200%.
-- These starter templates intentionally point directly at effect data so description output
-- exposes formula/ratio mistakes in the skill_effect rows instead of hiding them behind hardcoded text.
UPDATE public.skills
SET description_template = CASE id
    WHEN 0 THEN 'Gay {effect1.ratio_value|ratio_percent} {effect1.formula_subject_rich} len {effect1.target_label}.'
    WHEN 1 THEN 'Gay {effect1.ratio_value|ratio_percent} {effect1.formula_subject_rich} len {effect1.target_label}.'
    WHEN 2001 THEN 'Gay {effect1.ratio_value|ratio_percent} {effect1.formula_subject_rich} len {effect1.target_label}.'
    WHEN 2002 THEN 'Gay {effect1.ratio_value|ratio_percent} {effect1.formula_subject_rich} len {effect1.target_label}.'
    WHEN 2003 THEN 'Ban ra mot cay chuy bang lanh gay sat thuong len muc tieu.'
    ELSE description_template
END
WHERE id IN (0, 1, 2001, 2002, 2003);

-- Items with structured runtime data: keep them on macros that the new description pipeline can resolve.
UPDATE public.item_templates
SET description_template = '{equipment_stats_summary}
{requirements_summary}'
WHERE item_type = 1;

UPDATE public.item_templates
SET description_template = '{use_effects_summary}'
WHERE id IN (3002, 910005);

UPDATE public.item_templates
SET description_template = '{martial_art_book_summary}'
WHERE item_type = 5;

UPDATE public.item_templates
SET description_template = '{pill_recipe_book_summary}'
WHERE item_type = 8;

UPDATE public.item_templates
SET description_template = '{herb_seed_summary}'
WHERE item_type = 9;

COMMIT;
