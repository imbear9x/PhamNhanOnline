BEGIN;

UPDATE public.skills
SET description_template = '{effects_summary}'
WHERE description_template = '{effects_summary}
{range_summary}
{cast_summary}
{cooldown_summary}';

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

COMMIT;
