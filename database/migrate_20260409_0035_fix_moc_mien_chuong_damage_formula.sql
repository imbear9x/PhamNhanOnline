BEGIN;

UPDATE public.skill_effects
SET
    formula_type = 2,
    value_type = 2,
    base_value = NULL,
    ratio_value = 1.0,
    extra_value = 30,
    target_scope = 1,
    trigger_timing = 2
WHERE skill_id = 1
  AND order_index = 0;

UPDATE public.skills
SET description_template = 'Gay them {effect1.extra_value|number} sat thuong len {effect1.target_label}.'
WHERE id = 1;

COMMIT;
