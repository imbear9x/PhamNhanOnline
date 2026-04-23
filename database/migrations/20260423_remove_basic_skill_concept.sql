BEGIN;

UPDATE public.skills
SET skill_category = 2
WHERE skill_category = 1;

INSERT INTO public.game_configs (
    config_key,
    config_value,
    description
)
VALUES (
    'character.starter_skill_id',
    COALESCE(
        (SELECT config_value
         FROM public.game_configs
         WHERE config_key = 'character.starter_basic_skill_id'),
        '0'),
    'Skill id mac dinh duoc grant cho nhan vat moi.')
ON CONFLICT (config_key) DO UPDATE
SET
    config_value = EXCLUDED.config_value,
    description = EXCLUDED.description,
    updated_at = now();

DELETE FROM public.game_configs
WHERE config_key IN (
    'character.starter_basic_skill_id',
    'character.starter_basic_skill_slot_index');

COMMIT;
