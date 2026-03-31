BEGIN;

ALTER TABLE IF EXISTS public.map_portals
    DROP CONSTRAINT IF EXISTS map_portals_source_order_key;

COMMIT;
