UPDATE public.game_configs
SET description = 'Khoang dung sai khi log nghi van speed hack. Server position van chi tien theo toc do hop le.',
    updated_at = now()
WHERE config_key = 'character.position_sync_grace_server_units';

UPDATE public.game_configs
SET description = 'So giay toi da moi tick duoc tinh khi server tien vi tri player toi movement target.',
    updated_at = now()
WHERE config_key = 'character.position_sync_max_elapsed_seconds';

UPDATE public.game_configs
SET description = 'He so dung sai khi log nghi van speed hack tren intent client gui len. Khong tang toc server position.',
    updated_at = now()
WHERE config_key = 'character.position_sync_max_speed_multiplier';
