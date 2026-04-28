BEGIN;

INSERT INTO public.game_configs (config_key, config_value, description)
VALUES
    ('character.position_sync_catchup_multiplier', '1.3', 'He so bu tru movement sync khi xu ly interaction. Chi dung de server advance hop le ve phia target, khong doi toc do gameplay chinh.'),
    ('character.position_sync_catchup_max_seconds', '0.75', 'So giay toi da duoc dung cho bu tru movement sync khi xu ly interaction.')
ON CONFLICT (config_key) DO UPDATE
SET
    config_value = EXCLUDED.config_value,
    description = EXCLUDED.description,
    updated_at = now();

COMMIT;
