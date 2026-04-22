BEGIN;

UPDATE public.map_templates
SET spiritual_energy = 7.5
WHERE id = 1
   OR client_map_key = 'map_home_01';

COMMIT;
