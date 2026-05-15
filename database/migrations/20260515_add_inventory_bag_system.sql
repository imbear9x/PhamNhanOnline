CREATE TABLE IF NOT EXISTS bag_grade_configs (
    grade INTEGER PRIMARY KEY,
    slot_count INTEGER NOT NULL,
    upgrade_cost_linh_thach BIGINT NOT NULL DEFAULT 0,
    display_name VARCHAR(100) NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS player_bags (
    player_id UUID PRIMARY KEY REFERENCES characters(id) ON DELETE CASCADE,
    grade INTEGER NOT NULL REFERENCES bag_grade_configs(grade),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

INSERT INTO bag_grade_configs (grade, slot_count, upgrade_cost_linh_thach, display_name)
VALUES
    (1, 24, 0, 'Túi Sơ Cấp'),
    (2, 36, 100, 'Túi Trung Cấp'),
    (3, 48, 300, 'Túi Cao Cấp'),
    (4, 60, 700, 'Túi Linh Phẩm')
ON CONFLICT (grade) DO UPDATE
SET slot_count = EXCLUDED.slot_count,
    upgrade_cost_linh_thach = EXCLUDED.upgrade_cost_linh_thach,
    display_name = EXCLUDED.display_name;

INSERT INTO player_bags (player_id, grade, updated_at)
SELECT c.id, 1, NOW()
FROM characters c
LEFT JOIN player_bags pb ON pb.player_id = c.id
WHERE pb.player_id IS NULL;

INSERT INTO game_configs (config_key, config_value, description)
VALUES ('inventory.bag_upgrade_currency_code', 'currency.spirit_stone_small', 'Currency item code used for bag upgrade cost.')
ON CONFLICT (config_key) DO UPDATE
SET config_value = EXCLUDED.config_value,
    description = EXCLUDED.description;