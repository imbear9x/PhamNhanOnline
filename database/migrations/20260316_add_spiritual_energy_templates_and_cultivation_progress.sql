BEGIN;

CREATE TABLE IF NOT EXISTS public.spiritual_energy_templates (
    id integer NOT NULL,
    code character varying(50) NOT NULL,
    name character varying(100) NOT NULL,
    lk_per_minute numeric(10,4) NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT spiritual_energy_templates_pkey PRIMARY KEY (id),
    CONSTRAINT spiritual_energy_templates_code_key UNIQUE (code)
);

ALTER TABLE public.realm_templates
    ADD COLUMN IF NOT EXISTS absorption_multiplier numeric(10,4) NOT NULL DEFAULT 1.0;

ALTER TABLE public.map_templates
    ADD COLUMN IF NOT EXISTS spiritual_energy numeric(10,4) NOT NULL DEFAULT 100;

ALTER TABLE public.character_base_stats
    ADD COLUMN IF NOT EXISTS cultivation_progress numeric(18,6) NOT NULL DEFAULT 0;

UPDATE public.character_base_stats
SET cultivation_progress = COALESCE(cultivation_progress, 0);

INSERT INTO public.spiritual_energy_templates (
    id,
    code,
    name,
    lk_per_minute
)
VALUES
    (1, 'low', 'Low', 0.8),
    (2, 'medium', 'Medium', 1.0),
    (3, 'high', 'High', 1.5),
    (4, 'dense', 'Dense', 2.0)
ON CONFLICT (id) DO UPDATE
SET
    code = EXCLUDED.code,
    name = EXCLUDED.name,
    lk_per_minute = EXCLUDED.lk_per_minute;

UPDATE public.map_templates
SET spiritual_energy = CASE
    WHEN spiritual_energy <= 0 THEN 100
    ELSE spiritual_energy
END;

UPDATE public.realm_templates
SET absorption_multiplier = CASE
    WHEN id BETWEEN 1 AND 9 THEN 0.2
    WHEN id BETWEEN 10 AND 12 THEN 0.3
    WHEN id BETWEEN 13 AND 15 THEN 0.5
    WHEN id BETWEEN 16 AND 18 THEN 0.7
    WHEN id BETWEEN 19 AND 21 THEN 1.0
    WHEN id BETWEEN 22 AND 24 THEN 1.4
    WHEN id BETWEEN 25 AND 27 THEN 2.0
    ELSE 3.0
END;

COMMIT;
