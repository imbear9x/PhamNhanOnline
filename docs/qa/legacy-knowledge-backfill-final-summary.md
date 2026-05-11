# Legacy knowledge backfill final summary

## Scope

Final consolidation of code-evidenced runtime/gameplay domains currently present in `PhamNhanOnline`, their canonical knowledge status in `docs/`, and any remaining unresolved areas after the latest audit/canonicalization pass.

## Evidence basis

This summary is based on:
- `docs/qa/legacy-domain-coverage-audit.md`
- extraction notes under `docs/implementation/extractions/`
- clarification notes under `docs/game-design-wp/clarifications/`
- canonical docs now present under `docs/systems/`, `docs/combat/`, `docs/inventory/`, `docs/loot/`, `docs/progression/`, `docs/alchemy/`, `docs/maps/`, `docs/monsters/`, `docs/rules/`, and `docs/data-design/config-contracts/`

## Domains in code and already canonicalized / good

- `auth / account / login / register`
- `character creation / bootstrap`
- `session reconnect / resume`
- `world entry / world snapshot publish`
- `map instances / world membership / zone selection`
- `world movement / observer sync`
- `combat skill execution`
- `skill ownership / loadout / equipment-granted skills`
- `inventory / item read model / inventory transactions`
- `equipment / equipment slots / stat modifiers`
- `player stats / final stat recompute / state clamping`
- `martial arts / active martial art progression hooks`
- `cultivation / breakthrough`
- `potential allocation`
- `combat death recovery / return home`
- `loot / ground reward / pickup`
- `notifications / inbox / acknowledgement`
- `alchemy crafting / recipe preview / craft start`
- `game config contract surface`
- `server validation / transaction / runtime rule layer`
- `random tables / reward RNG`

Representative new/updated canonical targets from this pass:
- `docs/systems/character-creation-and-bootstrap-runtime.md`
- `docs/systems/reconnect-and-session-resume-runtime.md`
- `docs/systems/player-stats-runtime.md`
- `docs/systems/player-notification-runtime.md`
- `docs/systems/practice-session-runtime.md`
- `docs/combat/skill-ownership-and-loadout-runtime.md`
- `docs/combat/combat-death-and-return-home-runtime.md`
- `docs/inventory/inventory-runtime.md`
- `docs/inventory/equipment-runtime.md`
- `docs/progression/martial-art-ownership-and-activation-runtime.md`
- `docs/progression/cultivation-breakthrough-and-potential-runtime.md`
- `docs/loot/ground-reward-runtime.md`
- `docs/alchemy/alchemy-recipe-and-craft-runtime.md`
- `docs/rules/random-table-and-luck-runtime.md`

## Domains in code but still partial

- `practice sessions / pause-resume-cancel / result acknowledgement`
  - Canonical lifecycle doc now exists.
  - Still partial because intended taxonomy/scope is not fully resolved: generic shared system vs alchemy-only surfaced use.
- `descriptions / template text compilation`
  - Extraction exists and code evidence is real.
  - Still partial because canonical second-brain ownership is undecided: gameplay-runtime knowledge vs reference/spec-only infrastructure.
- `metrics / diagnostics / server observability`
  - Extraction exists and code evidence is real.
  - Still partial because canonical doc scope is undecided: gameplay second-brain vs ops-only knowledge.

## Domains in code but still needs-review

- `portal travel`
  - Canonical doc exists, but portal interaction mode and topology semantics are still unresolved in conflict artifacts.
- `enemy runtime / spawn / patrol / death / rewards`
  - Canonical doc exists, but objective/manual spawn and boss reset semantics remain open.
- `generic item use`
  - Canonical flow exists, but notifier ordering conflict remains unresolved.
- `home cave / garden / herb farming`
  - Server-side runtime is evidenced in code and extracted.
  - Still needs-review because live packet/UI accessibility was not confirmed from the reviewed handler surface, so a player-facing canonical doc would overstate certainty.

## Is any coded system still absent from knowledge?

Short answer: **no fully uncovered coded system remains in this audited set**.

For every code-evidenced domain currently listed in the audit, the repo now has at least one of:
- canonical docs,
- extraction notes,
- clarification notes,
- or explicit conflict / needs-review artifacts.

## If something is still incomplete, why?

Remaining incompleteness is not due to total absence of knowledge artifacts. It is due to one of these reasons:

1. **Design ambiguity still open**
   - portal travel
   - enemy runtime
   - practice session scope
   - home cave/garden/herb accessibility

2. **Conflict explicitly documented but not yet resolved**
   - generic item use notifier ordering
   - portal topology/interaction semantics
   - enemy reset/runtime scope questions

3. **Canonical ownership/scope decision not yet made**
   - description templates
   - metrics / diagnostics

## Net result

- Audit now reflects **only code-evidenced domains**.
- Previously code-heavy gaps now have extraction notes.
- Most major runtime domains now also have canonical docs.
- Remaining unresolved areas are concentrated in explicit `partial` or `needs-review` buckets rather than hidden gaps.
- There is **no audited code-evidenced domain still completely outside the repo knowledge system**.
