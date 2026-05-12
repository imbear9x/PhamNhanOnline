---
doc_type: change_note
status: inbox
created_by: "dev"
created_at: "2026-05-12"
task_id: "gamedesign-doc-lifecycle-migration"
agent: "dev"
change_type: "workflow"
affected_systems:
  - game-design-workspace
affected_docs:
  - docs/game-design-wp/README.md
  - docs/game-design-wp/DOC_LIFECYCLE.md
  - docs/game-design-wp/PROMPT_MIGRATE_EXISTING_DOCS.md
  - docs/game-design-wp/templates/
  - docs/game-design-wp/features/
  - docs/game-design-wp/notes/
  - docs/game-design-wp/clarifications/
affected_code: []
affected_configs: []
affected_db: []
requires_knowledge_manager: true
---

# Change Summary

Added a structured lifecycle/template system for `gamedesign` docs and migrated existing primary docs into the new one-live-primary-doc layout.

# What Changed

- Added a one-live-primary-doc rule for each `system_id` across `notes/`, `features/`, and `requirements/`.
- Added promotion rules requiring lower-tier docs to be migrated and deleted after promotion.
- Added required templates for design notes, feature drafts, requirement specs, and clarifications.
- Added a migration prompt for normalizing existing `game-design-wp` docs.
- Promoted mature notes into `features/`, deleted the lower-tier source notes, and updated live references to the promoted docs.
- Updated folder READMEs and `DOCS_INDEX.md` to expose the lifecycle/template structure.

# Why

The existing game-design workspace had useful content but weak structure. Notes, feature drafts, and requirement-ready content could coexist for the same system and drift apart. The new lifecycle prevents duplicate live docs, makes promotion explicit, and keeps current links pointed at the live primary docs.

# Affected Systems

- Game design documentation workflow
- Agent handoff quality for gameplay requirements

# Affected Docs

- `docs/game-design-wp/README.md`
- `docs/game-design-wp/DOC_LIFECYCLE.md`
- `docs/game-design-wp/PROMPT_MIGRATE_EXISTING_DOCS.md`
- `docs/game-design-wp/templates/`
- `docs/game-design-wp/notes/README.md`
- `docs/game-design-wp/features/README.md`
- `docs/game-design-wp/requirements/README.md`
- `docs/game-design-wp/features/*.md`
- `docs/game-design-wp/notes/*.md`
- `docs/game-design-wp/clarifications/player-stats-design-clarification.md`
- `docs/DOCS_INDEX.md`

# Affected Code

- None.

# Config / Data Changes

- None.

# DB Changes

- None.

# QA / Test Notes

- Docs-only workflow and migration change. Verify by checking staged diff, duplicate `system_id` scan, stale promoted-doc reference scan, and whitespace checks.

# Potential Conflicts / Risks

- Existing `promoted_from` values intentionally reference deleted source docs as migration audit trail.
- Some feature drafts still contain open questions and are not yet requirement-ready.

# Questions For Manager

- None.
