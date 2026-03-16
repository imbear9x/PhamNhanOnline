BEGIN;

ALTER TABLE public.map_templates
    ADD COLUMN IF NOT EXISTS spiritual_energy numeric(10,4) NOT NULL DEFAULT 100;

UPDATE public.map_templates
SET spiritual_energy = CASE
    WHEN spiritual_energy <= 0 THEN 100
    ELSE spiritual_energy
END;

UPDATE public.map_templates m
SET max_public_zone_count = zone_counts.zone_count
FROM (
    SELECT map_template_id, COUNT(*)::integer AS zone_count
    FROM public.map_zone_slots
    GROUP BY map_template_id
) zone_counts
WHERE m.id = zone_counts.map_template_id
  AND m.supports_cave_placement = true;

ALTER TABLE public.map_templates
    DROP CONSTRAINT IF EXISTS fk_map_templates_spiritual_energy;

ALTER TABLE public.map_templates
    DROP COLUMN IF EXISTS spiritual_energy_template_id;

COMMIT;
