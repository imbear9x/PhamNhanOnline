BEGIN;

INSERT INTO public.servers (id, name, status)
VALUES (1, 'Server01', 1)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    status = EXCLUDED.status;

INSERT INTO public.realm_templates (
    id,
    name,
    stage_name,
    max_cultivation,
    base_breakthrough_rate,
    failure_penalty
)
VALUES
    (1, 'luyện khí kì tầng 1', 'luyện khí kì tầng 1', 200, 100, 0),
    (2, 'luyện khí kì tầng 2', 'luyện khí kì tầng 2', 280, 100, 0)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    stage_name = EXCLUDED.stage_name,
    max_cultivation = EXCLUDED.max_cultivation,
    base_breakthrough_rate = EXCLUDED.base_breakthrough_rate,
    failure_penalty = EXCLUDED.failure_penalty;

COMMIT;
