BEGIN;

ALTER TABLE public.realm_templates
    ADD COLUMN IF NOT EXISTS lifespan integer NOT NULL DEFAULT 0;

INSERT INTO public.realm_templates (
    id,
    name,
    stage_name,
    max_cultivation,
    lifespan,
    base_breakthrough_rate,
    failure_penalty
)
VALUES
    (1, 'Luyện Khí Kỳ tầng 1', 'Luyện Khí Kỳ tầng 1', 150, 120, 100, 0),
    (2, 'Luyện Khí Kỳ tầng 2', 'Luyện Khí Kỳ tầng 2', 200, 125, 95, 0),
    (3, 'Luyện Khí Kỳ tầng 3', 'Luyện Khí Kỳ tầng 3', 280, 130, 90, 0),
    (4, 'Luyện Khí Kỳ tầng 4', 'Luyện Khí Kỳ tầng 4', 380, 135, 85, 0),
    (5, 'Luyện Khí Kỳ tầng 5', 'Luyện Khí Kỳ tầng 5', 520, 140, 80, 0),
    (6, 'Luyện Khí Kỳ tầng 6', 'Luyện Khí Kỳ tầng 6', 750, 145, 75, 0),
    (7, 'Luyện Khí Kỳ tầng 7', 'Luyện Khí Kỳ tầng 7', 1200, 150, 70, 0),
    (8, 'Luyện Khí Kỳ tầng 8', 'Luyện Khí Kỳ tầng 8', 1500, 155, 65, 0),
    (9, 'Luyện Khí Kỳ tầng 9', 'Luyện Khí Kỳ tầng 9', 2000, 160, 40, 0),
    (10, 'Trúc Cơ Sơ Kỳ', 'Trúc Cơ Sơ Kỳ', 5000, 180, 40, 0),
    (11, 'Trúc Cơ Trung Kỳ', 'Trúc Cơ Trung Kỳ', 7000, 200, 35, 0),
    (12, 'Trúc Cơ Hậu Kỳ', 'Trúc Cơ Hậu Kỳ', 10000, 220, 25, 0),
    (13, 'Kết Đan Sơ Kỳ', 'Kết Đan Sơ Kỳ', 25000, 350, 30, 0),
    (14, 'Kết Đan Trung Kỳ', 'Kết Đan Trung Kỳ', 35000, 400, 28, 0),
    (15, 'Kết Đan Hậu Kỳ', 'Kết Đan Hậu Kỳ', 50000, 500, 20, 0),
    (16, 'Nguyên Anh Sơ Kỳ', 'Nguyên Anh Sơ Kỳ', 125000, 1200, 18, 0),
    (17, 'Nguyên Anh Trung Kỳ', 'Nguyên Anh Trung Kỳ', 175000, 1500, 15, 0),
    (18, 'Nguyên Anh Hậu Kỳ', 'Nguyên Anh Hậu Kỳ', 245000, 2000, 10, 0),
    (19, 'Hóa Thần Sơ Kỳ', 'Hóa Thần Sơ Kỳ', 600000, -1, 60, 0),
    (20, 'Hóa Thần Trung Kỳ', 'Hóa Thần Trung Kỳ', 840000, -1, 55, 0),
    (21, 'Hóa Thần Hậu Kỳ', 'Hóa Thần Hậu Kỳ', 1200000, -1, 30, 0),
    (22, 'Luyện Hư Sơ Kỳ', 'Luyện Hư Sơ Kỳ', 3000000, -1, 30, 0),
    (23, 'Luyện Hư Trung Kỳ', 'Luyện Hư Trung Kỳ', 4200000, -1, 25, 0),
    (24, 'Luyện Hư Hậu Kỳ', 'Luyện Hư Hậu Kỳ', 9000000, -1, 15, 0),
    (25, 'Hợp Thể Sơ Kỳ', 'Hợp Thể Sơ Kỳ', 20000000, -1, 20, 0),
    (26, 'Hợp Thể Trung Kỳ', 'Hợp Thể Trung Kỳ', 28000000, -1, 15, 0),
    (27, 'Hợp Thể Hậu Kỳ', 'Hợp Thể Hậu Kỳ', 40000000, -1, 10, 0),
    (28, 'Độ Kiếp Kỳ', 'Độ Kiếp Kỳ', 100000000, -1, 12, 0),
    (29, 'Chân Tiên Sơ Kỳ', 'Chân Tiên Sơ Kỳ', 250000000, -1, 6, 0),
    (30, 'Chân Tiên Trung Kỳ', 'Chân Tiên Trung Kỳ', 350000000, -1, 5, 0),
    (31, 'Chân Tiên Hậu Kỳ', 'Chân Tiên Hậu Kỳ', 500000000, -1, 4, 0)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    stage_name = EXCLUDED.stage_name,
    max_cultivation = EXCLUDED.max_cultivation,
    lifespan = EXCLUDED.lifespan,
    base_breakthrough_rate = EXCLUDED.base_breakthrough_rate,
    failure_penalty = EXCLUDED.failure_penalty;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'character_base_stats'
          AND column_name = 'base_lifespan'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'character_base_stats'
          AND column_name = 'lifespan_bonus'
    ) THEN
        EXECUTE 'ALTER TABLE public.character_base_stats RENAME COLUMN base_lifespan TO lifespan_bonus';
    END IF;
END $$;

ALTER TABLE public.character_base_stats
    ADD COLUMN IF NOT EXISTS lifespan_bonus integer DEFAULT 0;

ALTER TABLE public.character_base_stats
    ALTER COLUMN lifespan_bonus SET DEFAULT 0;

UPDATE public.character_base_stats
SET lifespan_bonus = 0
WHERE lifespan_bonus IS NULL
   OR lifespan_bonus <> 0;

UPDATE public.character_current_state ccs
SET remaining_lifespan = CASE
    WHEN rt.lifespan = -1 THEN -1
    ELSE rt.lifespan + COALESCE(cbs.lifespan_bonus, 0)
END
FROM public.character_base_stats cbs
JOIN public.realm_templates rt ON rt.id = cbs.realm_id
WHERE cbs.character_id = ccs.character_id;

COMMIT;
