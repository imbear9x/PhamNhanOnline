BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM public.characters c
        JOIN public.account_credentials ac ON ac.account_id = c.account_id
        WHERE ac.provider_user_id = 'admin02'
    ) THEN
        RAISE EXCEPTION 'Khong tim thay character nao cua account admin02.';
    END IF;
END
$$;

WITH target_character AS (
    SELECT c.id AS character_id
    FROM public.characters c
    JOIN public.account_credentials ac ON ac.account_id = c.account_id
    WHERE ac.provider_user_id = 'admin02'
    ORDER BY c.created_at ASC
    LIMIT 1
),
delete_old_leaf_stacks AS (
    DELETE FROM public.player_items pi
    WHERE pi.player_id IN (SELECT character_id FROM target_character)
      AND pi.item_template_id = 6001
)
INSERT INTO public.player_items (
    player_id,
    item_template_id,
    quantity,
    is_bound,
    acquired_at,
    expire_at,
    updated_at
)
SELECT
    tc.character_id,
    6001,
    50,
    FALSE,
    timezone('utc', now()),
    NULL,
    timezone('utc', now())
FROM target_character tc;

WITH target_character AS (
    SELECT c.id AS character_id
    FROM public.characters c
    JOIN public.account_credentials ac ON ac.account_id = c.account_id
    WHERE ac.provider_user_id = 'admin02'
    ORDER BY c.created_at ASC
    LIMIT 1
)
INSERT INTO public.player_pill_recipes (
    player_id,
    pill_recipe_template_id,
    learned_at,
    total_craft_count,
    current_success_rate_bonus,
    updated_at
)
SELECT
    tc.character_id,
    2,
    timezone('utc', now()),
    0,
    0,
    timezone('utc', now())
FROM target_character tc
ON CONFLICT (player_id, pill_recipe_template_id) DO UPDATE
SET
    updated_at = EXCLUDED.updated_at;

COMMIT;
