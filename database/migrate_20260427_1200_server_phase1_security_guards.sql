BEGIN;

INSERT INTO public.game_configs (config_key, config_value, description)
VALUES
    ('character.position_sync_grace_server_units', '45', 'Khoang dung sai khi log nghi van speed hack. Server position van chi tien theo toc do hop le.'),
    ('character.position_sync_max_elapsed_seconds', '1.5', 'So giay toi da moi tick duoc tinh khi server tien vi tri player toi movement target.'),
    ('character.position_sync_max_speed_multiplier', '1.25', 'He so dung sai khi log nghi van speed hack tren intent client gui len. Khong tang toc server position.'),
    ('ground_reward.pickup_radius_server_units', '120', 'Ban kinh toi da de player nhat ground reward tinh tu vi tri server-authoritative.')
ON CONFLICT (config_key) DO UPDATE
SET
    config_value = EXCLUDED.config_value,
    description = EXCLUDED.description,
    updated_at = now();

CREATE INDEX IF NOT EXISTS idx_player_items_player_location_template
    ON public.player_items(player_id, location_type, item_template_id);

COMMIT;
