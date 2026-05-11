# Home Cave / Garden / Herb Design Clarification

_Evidence base: `docs/qa/legacy-domain-coverage-audit.md` (domain: home cave / garden / herb farming, status: `partial`) and `docs/implementation/extractions/home-cave-garden-herb-runtime-extraction.md`._

---

## What the code confirms exists server-side

From `docs/implementation/extractions/home-cave-garden-herb-runtime-extraction.md`:

- **Home cave creation**: each character gets one owner-bound home cave. Garden plots are created at character-creation time, count configured by `character.home_garden_plot_count`.
- **Garden plots**: plots are owned by the character's cave. Empty plots can have soil inserted; soil-equipped plots can have herb seeds planted.
- **Soil insertion** (`InsertSoilAsync`): validates soil item type, soil template match, plot ownership; clears depleted soil if present; binds soil to plot.
- **Seed planting** (`PlantSeedAsync`): requires inserted soil, empty herb slot, valid seed item; consumes seed; creates `PlayerHerb` row at seedling stage bound to plot (transactional).
- **Herb replanting** (`PlantExistingHerbAsync`): existing inventory herb can be re-placed into a plot without consuming a new seed item.
- **Herb to inventory** (`MoveHerbToInventoryAsync`): materializes herb progress, detaches herb and soil from plot, updates soil state, moves herb to inventory state.
- **Ownership enforcement**: `RequireOwnedCaveAsync`, `RequireOwnedPlotAsync`, `RequireOwnedHerbAsync` — all garden operations are player-bound.

**Code source files:** `GameServer/Services/HerbService.cs`, `GameServer/Services/CharacterService.cs`, `GameServer/Entities/PlayerGardenPlotEntity.cs`, `PlayerHerbEntity.cs`, `SoilTemplateEntity.cs`.

---

## What is NOT confirmed from code

- **No dedicated network handlers** for garden/cave operations were found in the reviewed handler set. The audit explicitly notes: _"No dedicated network handlers were visible in the inspected set for cave/garden operations."_
- This means the server-side runtime **may not be wired to live client packet surface** in the current build. The system exists in the persistence/service layer, but player-facing accessibility is unconfirmed.

---

## Player-facing intent questions (needs design answer)

### Question 1 — Is this system currently player-accessible?

- Is the home cave / garden / herb system exposed to players in the live client build?
- If not: is it intentionally deferred (no player-facing UX yet) or should packets/UI exist but are missing?
- **Needed answer before canonicalization:** explicit confirmation of whether there is a live client UI path for cave/garden/herb actions, or a decision that this is server-present but player-inaccessible as of this build.

### Question 2 — Intended player-facing flow

Assuming the system becomes (or already is) accessible, what is the intended player experience?

- How does a player access their home cave / garden? (Via home map? Special UI panel? Explicit navigation?)
- How does a player get soil items? (Crafted, bought, found as loot, quest reward?)
- How does a player get seed items?
- Is herb tending a manual player action (visit garden, check growth, harvest) or does progress happen passively over real time?
- Is there a growth timer visible to the player?

### Question 3 — Herb maturity and harvest

- What does a player receive when they harvest a herb? (Item in inventory? Multiple items based on growth level? Random yield?)
- Does herb maturity (seedling → mature → overgrown/depleted) have player-visible states?
- Is there a penalty for leaving herbs too long?

### Question 4 — Soil lifecycle

- When soil becomes "depleted," what does that mean to a player? (Used up? Needs replacement? Needs treatment?)
- Can soil be reused, or is it consumed per planting cycle?
- Who handles the depleted state — does the player need to manually remove and replace soil?

### Question 5 — Plot count and progression

- Is the starting plot count (`character.home_garden_plot_count`) the final count, or can players unlock more plots over time?
- Is plot count character-bound or account-bound?

---

## Accessible current behavior (what can be stated now)

- Server-side garden state exists per character: one home cave, N owned plots (N = config value at creation time).
- Plots follow a lifecycle: empty → soil inserted → herb planted (seedling) → herb mature → herb in inventory.
- All mutations are transactional and ownership-gated.
- Home cave creation occurs at character creation or on-demand via `EnsureHomeCaveAsync`.
- Herb-replanting from inventory back to plot is supported without seed consumption.

---

## What must be resolved before canonicalization

1. **Confirm live packet/handler surface** — verify whether dedicated garden/herb network handlers exist (possibly in a different handler module not yet reviewed). If none exist, mark system as server-present but player-inaccessible pending wiring.
2. **Answer Questions 1–5 above** (player-facing flow, soil lifecycle, harvest output, growth timer, plot progression).
3. **Determine canonical home** — canonicalize as part of `docs/systems/home-cave-and-garden-runtime.md` only after accessibility and intent are confirmed.

---

## Canonicalization recommendation

- Do **not** create a canonical player-facing doc for this system until packet/UI surface is verified.
- If the system is intentionally deferred, create a short "server-present but player-inaccessible" note in the canonical area to prevent confusion.
- If the system is live, confirm the above questions and canonicalize as a dedicated home-cave/garden system doc.
