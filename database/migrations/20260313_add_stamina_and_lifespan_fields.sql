BEGIN;

ALTER TABLE public.character_base_stats
    ADD COLUMN IF NOT EXISTS base_stamina integer DEFAULT 100,
    ADD COLUMN IF NOT EXISTS base_lifespan integer DEFAULT 100;

UPDATE public.character_base_stats
SET
    base_hp = COALESCE(base_hp, 100),
    base_mp = COALESCE(base_mp, 100),
    base_physique = COALESCE(base_physique, 10),
    base_attack = COALESCE(base_attack, 10),
    base_speed = COALESCE(base_speed, 10),
    base_spiritual_sense = COALESCE(base_spiritual_sense, 10),
    base_stamina = COALESCE(base_stamina, 100),
    base_lifespan = COALESCE(base_lifespan, 100),
    base_fortune = COALESCE(base_fortune, 0.01),
    base_potential = COALESCE(base_potential, 0)
WHERE base_hp IS NULL
   OR base_mp IS NULL
   OR base_physique IS NULL
   OR base_attack IS NULL
   OR base_speed IS NULL
   OR base_spiritual_sense IS NULL
   OR base_stamina IS NULL
   OR base_lifespan IS NULL
   OR base_fortune IS NULL
   OR base_potential IS NULL;

ALTER TABLE public.character_current_state
    ADD COLUMN IF NOT EXISTS current_stamina integer NOT NULL DEFAULT 100,
    ADD COLUMN IF NOT EXISTS remaining_lifespan integer NOT NULL DEFAULT 100;

UPDATE public.character_current_state ccs
SET
    current_stamina = COALESCE(ccs.current_stamina, cbs.base_stamina, 100),
    remaining_lifespan = COALESCE(ccs.remaining_lifespan, cbs.base_lifespan, 100)
FROM public.character_base_stats cbs
WHERE cbs.character_id = ccs.character_id
  AND (
      ccs.current_stamina IS NULL
      OR ccs.remaining_lifespan IS NULL
  );

COMMIT;
