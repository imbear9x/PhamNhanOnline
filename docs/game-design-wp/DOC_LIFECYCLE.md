# Game Design Doc Lifecycle

This file defines how `gamedesign` keeps design docs clean, promotable, and non-duplicated.

## Primary Doc Tiers

Primary design docs live in exactly one of these folders:

- `notes/`: early design capture, unresolved questions, rough system shaping.
- `features/`: structured feature design, mostly stable player-facing behavior, not coder-ready.
- `requirements/`: coder-ready requirements with acceptance criteria and implementation-facing detail.

## One Live Primary Doc Rule

- Each `system_id` may have only one live primary doc across `notes/`, `features/`, and `requirements/`.
- The live primary doc must be in the highest maturity tier currently reached.
- Do not keep an old note after promoting it to a feature.
- Do not keep an old feature after promoting it to a requirement.
- Before deleting the lower-tier doc, copy every still-relevant decision, question, risk, and source pointer into the promoted doc.
- If content cannot be cleanly merged, do not promote yet. Add the ambiguity to `Open questions` or create a clarification note.

Git history is the archive for promoted primary docs. Do not create `old`, `v2`, `final`, `copy`, or date-suffixed duplicates for the same system.

## Allowed Exceptions

These files are not primary system docs and may coexist with primary docs:

- `notes/conversation-log.md`
- `notes/deferred-features.md`
- `notes/design-backlog-triage.md`
- `clarifications/*.md`
- folder `README.md` files
- agent identity or tool files such as `AGENTS.md`, `SOUL.md`, `TOOLS.md`, `USER.md`, and `HEARTBEAT.md`

Clarification files are bridge artifacts for audit, mismatch, or code-verification questions. They do not replace the primary doc for a system.

## Required Metadata

Every primary design doc must include front matter with:

- `doc_type`
- `system_id`
- `status`
- `maturity`
- `owner`
- `created_at`
- `updated_at`
- `promoted_from`
- `related_docs`
- `requires_code_verification`

Use the templates in `templates/`.

## Promotion Rules

### Note To Feature

Promote a note only when:

- the core gameplay goal is clear
- the player-facing loop is understandable
- key terms are defined
- major alternatives have been resolved or listed as open questions
- the feature still needs design/detail work before implementation

Promotion action:

- create or update the feature doc using `templates/feature-draft-template.md`
- move all relevant note content into the feature doc
- delete the old note file
- update any references that pointed to the old note

### Feature To Requirement

Promote a feature only when:

- behavior is specific enough for `dev` to estimate and implement
- acceptance criteria are explicit
- important edge cases are covered
- config/data needs are listed
- out-of-scope items are explicit
- open questions are non-blocking or clearly marked as blockers

Promotion action:

- create or update the requirement doc using `templates/requirement-spec-template.md`
- move all relevant feature content into the requirement doc
- delete the old feature file
- update any references that pointed to the old feature

## Update Rules

- Edit the existing live primary doc for a system instead of creating a second one.
- If a system already has a requirement doc, new design changes go into that requirement doc unless they are only audit/code clarification.
- If a system already has a feature doc, new rough notes go into that feature doc under `Open questions`, `Design notes`, or `Parking lot`, not into a new note.
- If a system already has a note doc, continue updating that note until promotion is justified.

## Conflict Handling

If two docs disagree:

- do not silently choose one side
- write the disagreement in `Known conflicts / drift`
- resolve it during migration or ask the user
- if the conflict depends on implementation reality, set `requires_code_verification: true` and list the verification questions

## File Naming

Use stable kebab-case names based on `system_id`:

- `notes/<system_id>.md`
- `features/<system_id>.md`
- `requirements/<system_id>.md`

Avoid suffixes like `-new`, `-final`, `-v2`, or dates unless the file is a non-primary artifact.
