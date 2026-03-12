# Codex Persistent Memory

## DB Adjustment Workflow (PhamNhanOnline)

Trigger phrase from user: **"Dieu chinh lai db, ..."**

When this trigger appears, always do this checklist:

1. Read and clarify exactly what DB change is needed.
2. If adding/removing/changing column types in any table:
   - update related Entity
   - update related Repository
   - update related DTO
3. Update schema file:
   - `database/phamnhan_online.sql`
4. Create a runnable migration SQL file in the same folder:
   - place under `database/`
   - naming format: `migrate_YYYYMMDD_HHmm_<short_description>.sql`
   - make it executable directly so user can run once to update DB.

