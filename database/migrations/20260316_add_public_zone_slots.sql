BEGIN;

ALTER TABLE public.map_templates
    ADD COLUMN IF NOT EXISTS supports_cave_placement boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS public.map_zone_slots (
    id integer NOT NULL,
    map_template_id integer NOT NULL,
    zone_index integer NOT NULL,
    spiritual_energy_template_id integer NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT map_zone_slots_pkey PRIMARY KEY (id),
    CONSTRAINT map_zone_slots_map_template_zone_key UNIQUE (map_template_id, zone_index),
    CONSTRAINT fk_map_zone_slots_map_template
        FOREIGN KEY (map_template_id) REFERENCES public.map_templates(id),
    CONSTRAINT fk_map_zone_slots_spiritual_energy
        FOREIGN KEY (spiritual_energy_template_id) REFERENCES public.spiritual_energy_templates(id)
);

UPDATE public.map_templates
SET
    supports_cave_placement = CASE id
        WHEN 1 THEN false
        WHEN 2 THEN true
        ELSE supports_cave_placement
    END;

UPDATE public.map_templates
SET max_public_zone_count = 20
WHERE id = 2;

INSERT INTO public.map_zone_slots (
    id,
    map_template_id,
    zone_index,
    spiritual_energy_template_id
)
VALUES
    (2001, 2, 1, 1),
    (2002, 2, 2, 1),
    (2003, 2, 3, 1),
    (2004, 2, 4, 1),
    (2005, 2, 5, 1),
    (2006, 2, 6, 2),
    (2007, 2, 7, 2),
    (2008, 2, 8, 2),
    (2009, 2, 9, 2),
    (2010, 2, 10, 2),
    (2011, 2, 11, 2),
    (2012, 2, 12, 2),
    (2013, 2, 13, 2),
    (2014, 2, 14, 2),
    (2015, 2, 15, 3),
    (2016, 2, 16, 3),
    (2017, 2, 17, 3),
    (2018, 2, 18, 3),
    (2019, 2, 19, 4),
    (2020, 2, 20, 4)
ON CONFLICT (id) DO UPDATE
SET
    map_template_id = EXCLUDED.map_template_id,
    zone_index = EXCLUDED.zone_index,
    spiritual_energy_template_id = EXCLUDED.spiritual_energy_template_id;

COMMIT;
