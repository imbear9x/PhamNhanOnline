# Alchemy — `required_herb_maturity` Deferred Behavior Clarification

_Evidence base: `docs/qa/legacy-domain-coverage-audit.md` (domain: alchemy crafting, status: `partial`) and `docs/implementation/extractions/alchemy-runtime-extraction.md`._

---

## What the code confirms

From `docs/implementation/extractions/alchemy-runtime-extraction.md`:

- `AlchemyService.ValidateCraftPillAsync` explicitly **rejects** any craft request involving a recipe that has `required_herb_maturity` inputs:
  > _"Required-herb-maturity inputs are explicitly rejected as phase-later functionality."_
- This is a deliberate runtime guard — not a silent failure. The system knows about this recipe type and consciously blocks it.
- The field exists in the data model (recipes can be authored with `required_herb_maturity` set), but the runtime treats such recipes as inactive.

**Code source files:** `GameServer/Services/AlchemyService.cs`, `GameServer/Services/AlchemyCraftQueryService.cs`.

---

## The deferred behavior question

The current runtime defers `required_herb_maturity` recipes to a later phase. This clarification note captures the design decision needed to move this from deferred to active.

---

## Open questions (needs design answer)

### Question 1 — Scope of deferral

- Is `required_herb_maturity` deferred because:
  a. The home cave / garden / herb farming system (the source of mature herbs) is not yet player-accessible?
  b. The maturity mechanic itself (checking a herb's growth stage as a recipe prerequisite) is not yet designed in detail?
  c. Both?
- **Needed answer:** clarify whether herb maturity recipes are blocked purely because the input supply chain isn't ready, or because the maturity-checking mechanic itself still needs design work.

### Question 2 — Intended player-facing behavior when active

When this feature is un-deferred, what should happen?

- Does a recipe with `required_herb_maturity` require the player to supply a herb item that has reached a specific maturity stage (e.g., must be "fully mature," not seedling)?
- Is the maturity check done at craft-start validation (reject if herb is too young), or is there a quality/yield modifier based on maturity?
- Can the player see a herb's maturity stage before selecting it as a crafting ingredient?
- Can two herbs of the same type but different maturity levels both be valid inputs, or only the mature one?

### Question 3 — Relationship to garden herb system

- Are `required_herb_maturity` recipes only satisfiable with player-grown herbs (from the home cave / garden system), or can they also be satisfied with herbs obtained from other sources (loot, trading, purchase)?
- If player-grown only: is this a deliberate economic/progression gate linking alchemy mastery to garden investment?

### Question 4 — When to un-defer

- What milestone or system-readiness condition should trigger this feature being un-deferred?
  - Garden/herb system fully accessible to players?
  - Specific maturity stages designed and implemented in `PlayerHerbEntity`?
  - Client-side herb maturity display ready?
- **Needed answer:** explicit readiness criteria so dev knows when to remove the block.

### Question 5 — Recipes already in data

- Are there existing authored pill recipes in the current database/config that have `required_herb_maturity` set? If yes, those recipes are currently invisible/uncraftable to players even if learned.
- **Design consideration:** are players expected to be able to see these recipes in their recipe list, or should they be hidden until the feature is active?

---

## Acceptable current behavior (what can be stated now)

- Recipes with `required_herb_maturity` exist in the data model as a declared future capability.
- The runtime explicitly blocks crafting of such recipes — this is an intentional phase guard, not a bug.
- This guard can remain in place until design and system readiness conditions are confirmed.
- All other alchemy crafting behavior (learned recipe ownership, preview, ingredient allocation, success-rate planning, practice-session handoff) is unaffected by this deferral.

---

## What must be resolved before this can be un-deferred

1. Confirm why it is deferred (Question 1) and whether it depends on garden system accessibility.
2. Define the maturity-check mechanic (Question 2) — how is maturity evaluated at craft time?
3. Define the supply chain intent (Question 3) — garden-only or broader?
4. Set explicit readiness criteria (Question 4).
5. Decide player visibility of deferred recipes (Question 5).

---

## Canonicalization recommendation

- Current canonical alchemy docs should explicitly note that `required_herb_maturity` recipes are phase-later, blocked at runtime, and not to be represented as active player functionality.
- When un-deferred, add a dedicated section to the alchemy canonical doc (or a sub-doc) covering herb-ingredient validation and maturity semantics.
