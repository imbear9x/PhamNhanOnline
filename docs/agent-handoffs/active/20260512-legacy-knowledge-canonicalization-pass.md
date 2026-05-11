---
title: Legacy knowledge canonicalization pass from code-evidenced audit
doc_type: handoff
status: Ready
owner: manager
last_updated: 2026-05-12
---

# Goal
Canonicalize only the domains that are already code-evidenced and judged ready-for-direct-canonicalization in `docs/qa/legacy-domain-coverage-audit.md`.

# Source of truth
- `docs/qa/legacy-domain-coverage-audit.md`
- matching extraction notes under `docs/implementation/extractions/`
- existing canonical docs under `docs/`

# Scope
Create or update canonical docs for these code-evidenced domains only:
- character creation / bootstrap
- session reconnect / resume
- combat skill execution
- skill ownership / loadout
- inventory runtime
- equipment runtime
- player stats / final stat recompute
- martial arts ownership / activation
- cultivation / breakthrough / potential allocation
- combat death recovery
- loot / ground reward / pickup
- notifications
- practice sessions
- alchemy recipe / craft flow
- random table / luck runtime

# Do not do
- do not invent any new domain from chat
- do not canonicalize portal/enemy/generic item use beyond their current unresolved conflicts
- do not resolve design ambiguity silently
- do not edit gameplay code

# Acceptance criteria
- canonical docs created/updated in appropriate repo docs locations
- any unresolved ambiguity becomes an explicit conflict/needs-review artifact instead of silent assumption
- `docs/index/legacy-knowledge-backfill-master-checklist.md` updated to reflect actual status
- report exact files changed and remaining unresolved domains

# Recommended owner
knowledge-manager
