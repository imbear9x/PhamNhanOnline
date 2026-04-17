BEGIN;

ALTER TABLE public.realm_templates
    ADD COLUMN IF NOT EXISTS lifespan integer NOT NULL DEFAULT 0;

INSERT INTO public.realm_templates (
    id,
    name,
    max_cultivation,
    lifespan,
    base_breakthrough_rate,
    failure_penalty
)
VALUES
    (1, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 1', 150, 120, 100, 0),
    (2, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 2', 200, 125, 95, 0),
    (3, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 3', 280, 130, 90, 0),
    (4, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 4', 380, 135, 85, 0),
    (5, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 5', 520, 140, 80, 0),
    (6, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 6', 750, 145, 75, 0),
    (7, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 7', 1200, 150, 70, 0),
    (8, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 8', 1500, 155, 65, 0),
    (9, 'Luyá»‡n KhÃ­ Ká»³ táº§ng 9', 2000, 160, 40, 0),
    (10, 'TrÃºc CÆ¡ SÆ¡ Ká»³', 5000, 180, 40, 0),
    (11, 'TrÃºc CÆ¡ Trung Ká»³', 7000, 200, 35, 0),
    (12, 'TrÃºc CÆ¡ Háº­u Ká»³', 10000, 220, 25, 0),
    (13, 'Káº¿t Äan SÆ¡ Ká»³', 25000, 350, 30, 0),
    (14, 'Káº¿t Äan Trung Ká»³', 35000, 400, 28, 0),
    (15, 'Káº¿t Äan Háº­u Ká»³', 50000, 500, 20, 0),
    (16, 'NguyÃªn Anh SÆ¡ Ká»³', 125000, 1200, 18, 0),
    (17, 'NguyÃªn Anh Trung Ká»³', 175000, 1500, 15, 0),
    (18, 'NguyÃªn Anh Háº­u Ká»³', 245000, 2000, 10, 0),
    (19, 'HÃ³a Tháº§n SÆ¡ Ká»³', 600000, -1, 60, 0),
    (20, 'HÃ³a Tháº§n Trung Ká»³', 840000, -1, 55, 0),
    (21, 'HÃ³a Tháº§n Háº­u Ká»³', 1200000, -1, 30, 0),
    (22, 'Luyá»‡n HÆ° SÆ¡ Ká»³', 3000000, -1, 30, 0),
    (23, 'Luyá»‡n HÆ° Trung Ká»³', 4200000, -1, 25, 0),
    (24, 'Luyá»‡n HÆ° Háº­u Ká»³', 9000000, -1, 15, 0),
    (25, 'Há»£p Thá»ƒ SÆ¡ Ká»³', 20000000, -1, 20, 0),
    (26, 'Há»£p Thá»ƒ Trung Ká»³', 28000000, -1, 15, 0),
    (27, 'Há»£p Thá»ƒ Háº­u Ká»³', 40000000, -1, 10, 0),
    (28, 'Äá»™ Kiáº¿p Ká»³', 100000000, -1, 12, 0),
    (29, 'ChÃ¢n TiÃªn SÆ¡ Ká»³', 250000000, -1, 6, 0),
    (30, 'ChÃ¢n TiÃªn Trung Ká»³', 350000000, -1, 5, 0),
    (31, 'ChÃ¢n TiÃªn Háº­u Ká»³', 500000000, -1, 4, 0)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    max_cultivation = EXCLUDED.max_cultivation,
    lifespan = EXCLUDED.lifespan,
    base_breakthrough_rate = EXCLUDED.base_breakthrough_rate,
    failure_penalty = EXCLUDED.failure_penalty;

COMMIT;
