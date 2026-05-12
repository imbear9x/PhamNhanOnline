# Game Design Workspace

This folder is the working area for the `gamedesign` agent inside the `PhamNhanOnline` repository.

## Structure

- `notes/` keeps raw discussion capture and conversation summaries
- `features/` keeps in-progress feature drafts
- `requirements/` keeps coder-ready design specs
- `clarifications/` keeps audit, mismatch, and code-verification bridge notes
- `templates/` keeps required templates for game-design docs
- `DOC_LIFECYCLE.md` defines promotion and duplicate-prevention rules
- `PROMPT_MIGRATE_EXISTING_DOCS.md` is the migration prompt for normalizing existing docs

## Scope

The GameDesign agent should work only inside this folder unless the user explicitly overrides that rule.

## Clean Docs Rule

Each gameplay system should have only one live primary doc across `notes/`, `features/`, and `requirements/`.

When a system is promoted, the lower-tier doc is migrated into the higher-tier doc and then deleted. Git history is the archive.

Use `DOC_LIFECYCLE.md` and the templates in `templates/` for all new or rewritten primary docs.
