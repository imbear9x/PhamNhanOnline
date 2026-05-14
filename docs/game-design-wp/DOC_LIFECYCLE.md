# Game Design Doc Lifecycle

This file defines how `gamedesign` keeps design docs clean, promotable, and non-duplicated.

## Primary Doc Tiers

Primary design docs live in exactly one of these folders:

- `notes/`: early design capture, unresolved questions, rough system shaping.
- `features/`: structured feature design, mostly stable player-facing behavior, not coder-ready.
- `requirements/`: coder-ready requirements with acceptance criteria and implementation-facing detail.

Shared cross-feature rules live in `shared-rules.md`, not in a primary doc tier. Consistency issues and unresolved cross-doc audits live in `consistency-audit.md`.

## One Live Doc Per Tier Rule

- Each `system_id` may have only one live doc in each primary tier.
- Do not keep two notes, two features, or two requirements for the same system.
- Do not keep an old note after promoting it to a feature.
- Keep the feature doc after promoting it to a requirement.
- A feature and a requirement may coexist for the same system.
- The feature remains the canonical player-facing design source.
- The requirement is the implementation contract derived from the feature.
- The requirement must not contradict the feature or shared rules.
- Before deleting a promoted note, copy every still-relevant decision, question, risk, and source pointer into the feature doc.
- If content cannot be cleanly merged, do not promote yet. Add the ambiguity to `Open questions` or create a clarification note.

Git history is the archive for promoted notes. Do not create `old`, `v2`, `final`, `copy`, or date-suffixed duplicates for the same system.

## Allowed Exceptions

These files are not primary system docs and may coexist with primary docs:

- `notes/conversation-log.md`
- `notes/deferred-features.md`
- `notes/design-backlog-triage.md`
- `clarifications/*.md`
- folder `README.md` files
- agent identity or tool files such as `AGENTS.md`, `SOUL.md`, `TOOLS.md`, `USER.md`, and `HEARTBEAT.md`

Clarification files are bridge artifacts for audit, mismatch, or code-verification questions. They do not replace the primary doc for a system.

`shared-rules.md` is the canonical source for mechanics used by more than one feature. Feature docs should reference shared rules and only describe feature-specific application details.

When a feature has a matching requirement, the two docs must cross-reference each other. The feature owns design truth; the requirement owns coder-ready implementation acceptance details.

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
- copy or derive implementation-relevant content from the feature into the requirement doc
- keep the feature file
- add cross-links between the feature and requirement docs
- verify that the requirement does not contradict the feature or `shared-rules.md`
- if the requirement needs a rule that changes player-facing design, update `shared-rules.md` first when shared mechanics are affected, then update the feature doc, then update the requirement doc

## Update Rules

- Edit the existing live doc in the relevant tier instead of creating a second same-tier doc for a system.
- If a system already has a requirement doc, new player-facing design changes still go into the feature doc first; then update the requirement doc so it remains derived from the feature.
- If a system already has a feature doc, new rough notes go into that feature doc under `Open questions`, `Design notes`, or `Parking lot`, not into a new note.
- If a system already has a note doc, continue updating that note until promotion is justified.
- If the update touches a mechanic listed in `shared-rules.md`, update `shared-rules.md` first or confirm that the shared rule is unchanged.
- A user decision made while discussing one feature may change shared truth for other features. Before saving the update, search/read the related live primary docs for the same rule, term, mechanic, economy assumption, edge case, UI behavior, or player-facing promise.
- If the decision affects other live primary docs, update those docs in the same pass. The current discussion doc is not allowed to diverge from older notes, features, or requirements that describe the same truth.
- If implementation drift reveals that canonical gameplay truth must change, update `shared-rules.md` first when shared mechanics are affected, then update the feature doc, then sync the requirement doc. Do not let requirement-only edits redefine canonical design silently.
- If the agent cannot tell whether the new decision should overwrite older docs, write the disagreement in `Known conflicts / drift` on the affected doc when appropriate and ask the user for the canonical decision before treating the update as complete.
- When reporting back, list the docs checked and the docs updated for consistency.
- If the conflict cannot be resolved in the current turn, create or update an item in `consistency-audit.md` with affected docs, the conflict, and the user decision needed.

## Conflict Handling

If two docs disagree:

- do not silently choose one side
- write the disagreement in `Known conflicts / drift`
- resolve it during migration or ask the user
- if a requirement disagrees with its feature, treat the feature plus `shared-rules.md` as the design authority until the user explicitly changes canonical design
- if the conflict depends on implementation reality, set `requires_code_verification: true` and list the verification questions
- do not leave the disagreement hidden in separate docs after the user has made a canonical decision; propagate the decision or ask for permission/scope if the affected docs are outside the current write scope

## File Naming

Use stable kebab-case names based on `system_id`:

- `notes/<system_id>.md`
- `features/<system_id>.md`
- `requirements/<system_id>.md`

Avoid suffixes like `-new`, `-final`, `-v2`, or dates unless the file is a non-primary artifact.
