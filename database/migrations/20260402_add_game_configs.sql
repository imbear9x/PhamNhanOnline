BEGIN;

CREATE TABLE IF NOT EXISTS public.game_configs (
    config_key character varying(100) PRIMARY KEY,
    config_value character varying(100) NOT NULL,
    description text NULL,
    created_at timestamp without time zone NOT NULL DEFAULT now(),
    updated_at timestamp without time zone NOT NULL DEFAULT now()
);

INSERT INTO public.game_configs (config_key, config_value, description)
VALUES
    ('network.reconnect_resume_window_seconds', '3', 'So giay server giu resume token/session sau khi mat ket noi de reconnect.'),
    ('world.portal_validation_buffer_server_units', '4', 'Buffer them vao interaction radius khi server validate di qua portal.'),
    ('combat.skill_range_grace_buffer_units', '12', 'Buffer them vao tam cast skill khi server validate attack target.'),
    ('combat_death.return_home_recovery_ratio', '0.8', 'Ti le HP/MP hoi lai khi player combat dead va duoc dua ve home.'),
    ('item_drop.player_drop_ownership_seconds', '10', 'So giay item vut tu inventory con ownership rieng cho nguoi vut.'),
    ('item_drop.player_drop_free_for_all_seconds', '50', 'So giay sau giai doan ownership de item vut tu inventory ton tai o free-for-all.'),
    ('item_drop.enemy_drop_default_ownership_seconds', '30', 'So giay ownership mac dinh cho ground reward roi tu enemy khi reward rule khong chi dinh.'),
    ('item_drop.enemy_drop_default_free_for_all_seconds', '30', 'So giay free-for-all mac dinh cho ground reward roi tu enemy khi reward rule khong chi dinh.'),
    ('world.empty_public_instance_lifetime_seconds', '120', 'So giay mot public instance rong duoc giu truoc khi bi huy.'),
    ('cultivation.potential_per_cultivation_point', '1', 'So potential duoc quy doi tu moi cultivation point khi settle cultivation.'),
    ('cultivation.settlement_interval_seconds', '300', 'Chu ky settle cultivation theo giay.'),
    ('character.home_garden_plot_count', '8', 'So o vuon mac dinh khi tao home cave moi.'),
    ('character.starter_basic_skill_id', '0', 'Skill id basic mac dinh duoc grant cho nhan vat moi.'),
    ('character.starter_basic_skill_slot_index', '1', 'Loadout slot mac dinh dung de gan starter basic skill.'),
    ('skill.max_loadout_slot_count', '5', 'So slot loadout skill toi da cua nhan vat.')
ON CONFLICT (config_key) DO UPDATE
SET
    config_value = EXCLUDED.config_value,
    description = EXCLUDED.description,
    updated_at = now();

COMMIT;
