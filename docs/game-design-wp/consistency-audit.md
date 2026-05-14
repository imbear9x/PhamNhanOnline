---
doc_type: game_design_consistency_audit
status: active
owner: gamedesign
created_at: 2026-05-13
updated_at: 2026-05-13
---

# Game Design Consistency Audit

This file tracks cross-doc consistency issues. It is not the canonical rule source; canonical shared rules live in `shared-rules.md`.

Use this file when:

- two docs define the same mechanic differently
- a new user decision may supersede old design truth
- a feature introduces a rule that should probably become shared truth
- the agent cannot safely update every affected doc without a user decision

## Status Values

- `open`: conflict or audit item needs work
- `needs-user`: waiting for user to choose canonical truth
- `needs-code`: waiting for implementation/runtime verification
- `ready-to-propagate`: canonical decision exists, docs need updating
- `resolved`: docs have been updated or the issue is no longer relevant

## Audit Items

### AUDIT-001 — Spirit Sense: slot/bandwidth vs consumable resource

Status: `resolved`

Affected docs:

- `features/spirit-sense.md`
- `features/spirit-beast.md`
- `features/machine-system.md`
- `features/home-cave-defense.md`
- `features/crafting-talisman-formation.md`

Issue:

- `features/spirit-sense.md` describes Thần Thức as slot / bandwidth.
- The same doc also contains resource-like language such as recovery, dropping to zero, or being consumed by stealth.
- Other companion docs depend on Thần Thức as fixed slot capacity.

Decision needed:

- Is Thần Thức strictly a slot/bandwidth stat, with stealth consuming mana only?
- Or is Thần Thức also a recoverable resource?

Suggested direction:

- Keep Thần Thức as slot/bandwidth for clarity.
- Let stealth consume mana.
- Remove recovery / zero / continuous-consumption language from Thần Thức unless a separate temporary modifier system is introduced.

Resolution (2026-05-13):

- User confirmed Thần Thức is strictly slot/bandwidth.
- Stealth consumes mana.
- Shared rule updated and propagated to affected docs.

### AUDIT-002 — Death taxonomy and PK definition

Status: `resolved`

Affected docs:

- `features/death-penalty.md`
- `features/home-cave-defense.md`
- `features/sect-system.md`
- `features/player-interaction-group.md`

Issue:

- Death penalty currently mixes normal death, Duel, PK, PvP Zone, cave raid, and sect war.
- `features/death-penalty.md` says PK is extra penalty, but an edge case says Duel death applies PK-like thọ nguyên / lôi kiếp penalty.

Decision needed:

- Which death states trigger only normal item/linh thạch drop?
- Which states trigger thọ nguyên / lôi kiếp penalty?
- Which states apply heavier raid multipliers?

Suggested direction:

- Define a PvP/death taxonomy in `shared-rules.md`.
- Make Duel low-risk unless the game intentionally wants hardcore duels.
- Treat cave raid / sect war as lawful PvP with configured multipliers, not generic PK.

Resolution (2026-05-13):

- User chose a stronger canonical rule: all death causes apply the same baseline penalty (drop + thọ nguyên / lôi kiếp reduction).
- Special death contexts may add extra penalties configured by game design data.
- Duel is not exempt from baseline penalty.

### AUDIT-003 — Quest System scope name

Status: `resolved`

Affected docs:

- `features/main-progression-quest-chain.md`
- `features/sect-system.md`
- `features/npc-system.md`

Issue:

- `features/main-progression-quest-chain.md` defines a single linear quest chain with one active quest.
- `features/sect-system.md` defines sect tasks/quests.
- Future side quests, dailies, repeatables, events, and NPC tasks may conflict with a broad "Quest System" name.

Decision needed:

- Is this doc the global quest system, or specifically the main progression quest chain?

Suggested direction:

- Rename or re-scope to `main-progression-quest-chain`.
- Keep sect tasks and future repeatable quests as separate systems sharing objective/reward primitives where needed.

Resolution (2026-05-13):

- User chose Option A.
- `features/quest-system.md` was promoted/renamed to `features/main-progression-quest-chain.md`.
- Canonical scope is now explicitly the main progression quest chain only.

### AUDIT-004 — Looting window shared behavior

Status: `resolved`

Affected docs:

- `features/home-cave-defense.md`
- `features/sect-system.md`
- `features/death-penalty.md`

Issue:

- Cave destruction and sect gate destruction both use a 1-minute looting window.
- The shared behavior is similar but currently repeated in feature docs.
- Death/drop behavior during looting needs one canonical rule.

Decision needed:

- Should looting-window behavior be fully shared, with feature docs only defining the loot pool and recovery path?

Suggested direction:

- Keep one canonical looting-window rule in `shared-rules.md`.
- Feature docs define only structure-specific loot source, teleport target, and recovery path.

Resolution (2026-05-13):

- User confirmed the shared looting-window rule.
- Shared rule updated; feature docs keep structure-specific application details.

### AUDIT-005 — Inventory full / reward overflow

Status: `resolved`

Affected docs:

- `features/main-progression-quest-chain.md`
- `features/sect-system.md`

Issue:

- Quest rewards overflow to inbox/hòm thư.
- Sect welfare gives a 5-minute cleanup window and then the reward is lost.

Decision needed:

- Should all reward overflow use inbox?
- Or should welfare/task rewards be allowed to expire while main quest rewards are protected?

Suggested direction:

- Use inbox for guaranteed progression rewards.
- Use timed claim/expiry only for optional or economy-sensitive rewards, and document this distinction in `shared-rules.md`.

Resolution (2026-05-13):

- User chose one unified rule: reward overflow goes to inbox/hòm thư chờ nhận.
- Sect welfare timeout-on-full rule was removed and replaced with inbox behavior.

### AUDIT-006 — Companion density and slot consistency

Status: `resolved`

Affected docs:

- `features/spirit-beast.md`
- `features/machine-system.md`
- `features/spirit-sense.md`

Issue:

- Linh Thú counts as one player-like entity for map density.
- Khôi Lỗi docs do not clearly say whether machines also count toward map density.
- Both use Thần Thức slots, but have different energy models.

Decision needed:

- Do all active companions count toward density, or only visible living pets?

Suggested direction:

- Count all active companion entities toward density, including Linh Thú and Khôi Lỗi, unless Khôi Lỗi is implemented as non-entity effects.

Resolution (2026-05-13):

- User chose a different canonical rule: remove companion density-map limits entirely.
- Linh Thú and Khôi Lỗi are both constrained by Thần Thức slot model plus their own resource model, not by a shared map density cap.

### AUDIT-007 — Escrow resolve behavior

Status: `resolved`

Affected docs:

- `features/sect-system.md`
- `shared-rules.md`

Issue:

- `shared-rules.md` had escrow as draft with no clear resolve/return behavior.
- `sect-system.md` used escrow in 4 places (welfare, voluntary task, member-posted task, buy order) with inconsistent language.
- No explicit rule on whether expired/pool-returned tasks release escrow.

Resolution (2026-05-13):

- Canonical rule established: escrow is locked immediately on creation; it remains locked if task expires or returns to pool; it is only returned to source when a task is **cancelled explicitly**.
- On cancel: return to source container (bảo khố or creator inventory depending on task type).
- On system dissolve: escrow returns to source container, then processed by dissolve flow.
- `shared-rules.md` Escrow section updated to canonical status.
- `sect-system.md` voluntary task, member-posted task, and edge case sections updated to reflect explicit cancel-return rule.

---

### AUDIT-008 — Blueprint canonical + structure loot drop rate

Status: `resolved`

Affected docs:

- `shared-rules.md`
- `features/home-cave-defense.md`
- `features/sect-system.md`

Issue:

- Blueprint rule was draft; no canonical rule for drop rate on destruction.
- Sect system implied buying a new blueprint to re-establish, contradicting blueprint-stores-content design.
- Drop rate vague ("theo tỷ lệ nhất định") with no counting model.

Resolution (2026-05-13):

- Blueprint canonical: non-tradable, max 1/person, repurchasable at NPC if lost, pack-up loses nothing, destruction returns blueprint with remaining contents.
- Sect blueprint reuse: after sect destruction, owner uses returned blueprint to re-establish at new location. Disciples can return, all activity resumes.
- Structure Loot Drop Rate canonical rule added to shared-rules.md: random range per-structure, roll independently for lingstone and items, each stack unit = 1 unit for dice roll.
- Structure drops are public immediately (no priority owner).
- home-cave-defense.md and sect-system.md updated to reference shared rule.

---

### AUDIT-009 — Ownership / Drop Rights + Pet Auto-Loot

Status: `resolved`

Affected docs:

- `shared-rules.md`
- `features/death-penalty.md`
- `features/spirit-beast.md`
- `features/home-cave-defense.md`

Issue:

- Drop priority window behavior not described (visible? hidden? error message?).
- Pet auto-loot behavior in looting windows unspecified.
- Pet loot polling interval unspecified (risk of spam behavior).

Resolution (2026-05-13):

- Drop priority: item visible to all; others get "cannot pick up yet, wait X sec" message during owner’s priority window.
- Looting window does not override death-drop priority rule.
- Structure drops (from destroyed cave/sect) are public immediately — no priority owner.
- Pet auto-loot: polls every ~1 second (game_configs), obeys same Ownership/Drop Rights rule, cannot loot items still in another player’s priority window.
- All docs updated to reference shared-rules.md.

---

### AUDIT-010 — PvP State Taxonomy

Status: `resolved`

Affected docs:

- `shared-rules.md`
- `features/player-interaction-group.md`
- `features/death-penalty.md`

Issue:

- PvP state taxonomy was listed as “needs user decision” with no canonical states.
- Unclear whether PvP Zone deaths count as PK.
- Duel flee behavior unspecified.
- Mineral conflict death category unspecified.

Resolution (2026-05-13):

- Canonical states: neutral, duel, pvp_zone, cave_raid, sect_war, mineral_conflict, pk.
- PvP Zone death = not PK. Mineral conflict death = not PK. Both apply baseline penalty only.
- pk = attack without consent outside allowed contexts; adds extra penalty per game design data.
- Duel flee/exit map: duel ends immediately, no penalty.
- player-interaction-group.md updated with duel flee rule, mineral conflict clarification, PvP Zone clarification.

---

### AUDIT-011 — Offline time-based activities shared rule

Status: `resolved`

Affected docs:

- `shared-rules.md`
- `notes/cultivation-and-breakthrough.md`
- `features/mineral-vein-system.md`

Issue:

- Multiple features (cultivation, mining, crafting) implied different online/offline behavior; no shared rule existed.

Resolution (2026-05-14):

- Canonical rule: all time-based activities count time offline as long as the player started the activity at the required location.
- Server settles on login; player remains in active state.
- `shared-rules.md` updated with Offline Time-Based Activities section.

---

### AUDIT-012 — Cultivation penalty: realm drop + potential revert

Status: `resolved`

Affected docs:

- `shared-rules.md`
- `notes/cultivation-and-breakthrough.md`
- `features/tribulation-system.md`
- `features/death-penalty.md`

Issue:

- `potential_reward_locked` was old design; no canonical rule for what happens to potential when realm drops.
- Breakthrough failure penalty was vague (flat vs %).
- No shared rule for realm-drop behavior across breakthrough failure and Lôi Kiếp failure.

Resolution (2026-05-14):

- Canonical: cultivation trừ % hiện tại khi penalty xảy ra; nếu tụt xuống dưới ngưỡng cảnh giới thì tụt 1 cảnh giới.
- Khi tụt cảnh giới: potential revert ngược lại theo số lần upgrade đã lưu, chỉ số giảm tương ứng.
- Không có bình cảnh khi leo lại sau khi tụt.
- `potential_reward_locked` old design flagged as non-canonical; TechDesign to refactor.
- Rule applies to: breakthrough failure, Lôi Kiếp failure, and future penalties of same type.

---

### AUDIT-013 — Structure deployment cast time / setup lock

Status: `resolved`

Affected docs:

- `shared-rules.md`
- `features/home-cave-defense.md`
- `features/sect-system.md`
- `features/mineral-vein-system.md`

Issue:

- Home cave, sect, and mineral vein deployment all needed a consistent setup-time rule.

Resolution (2026-05-14):

- Canonical rule: deployment cast time is 1 minute for home cave, sect, and mineral vein claim.
- During cast: cannot be attacked; cannot enter the structure/area yet.
- Shared rule added and propagated.

---

### AUDIT-014 — Finalize cross-feature open questions batch

Status: `resolved`

Affected docs:

- `features/home-cave-defense.md`
- `features/machine-system.md`
- `features/multi-stage-crafting.md`
- `features/tribulation-system.md`
- `features/player-interaction-group.md`
- `features/spirit-sense.md`

Issue:

- Multiple live docs had lingering non-balance open questions around deployment, shield ordering, crafting layers, tribulation, and interaction runtime.

Resolution (2026-05-14):

- Home cave: cast time 1 minute, step-ladder slot growth, all cave blueprints valid on any allowed map, blurred detection below sense threshold, ranked attack-charm durations, immediate gate reset+tele on expiry, 10-player cap in cave gate map.
- Machine: shield order = first-enabled guard machine takes hits first; machine blueprint tiers map to major realm stages; respawn cooldown defaults to 30 minutes, scaling by tier.
- Multi-stage crafting: no hard tier labels, no hard cap on chain depth, inventory uses normal bag, upstream craft-source remains data-driven.
- Tribulation: same map template across realms; failure does not drop items/lingstone.
- Player interaction: trade cancels immediately on disconnect/map leave/range break/manual cancel; group chat keeps 100 latest messages or 7 days and deletes immediately on dissolution.

---

### AUDIT-015 — Cultivation / breakthrough note promoted to feature

Status: `resolved`

Affected docs:

- `features/cultivation-and-breakthrough.md`
- `features/tribulation-system.md`
- `shared-rules.md`
- `notes/deferred-features.md`
- `notes/design-backlog-triage.md`

Issue:

- Cultivation / breakthrough note had enough clarified design truth and repo grounding to become a live feature doc.
- Needed to ensure references moved with the promotion and to avoid creating requirement/handoff for already-implemented runtime.

Resolution (2026-05-14):

- `notes/cultivation-and-breakthrough.md` promoted to `features/cultivation-and-breakthrough.md`.
- References in related docs updated.
- Feature doc explicitly notes that core runtime already exists; only the penalty refactor (`potential_reward_locked` -> potential revert rule) remains a future TechDesign concern.

---

### AUDIT-016 — Tribulation note promoted to feature

Status: `resolved`

Affected docs:

- `features/tribulation-system.md`
- `features/death-penalty.md`
- `features/cultivation-and-breakthrough.md`
- `shared-rules.md`

Resolution (2026-05-14):

- Final open questions closed: item use allowed, offline hold-at-zero behavior, postponement via countdown-extending item.
- `notes/tribulation-system.md` promoted to `features/tribulation-system.md`.
- Related references updated.

---

### AUDIT-017 — Player interaction group note promoted to feature

Status: `resolved`

Affected docs:

- `features/player-interaction-group.md`
- `features/death-penalty.md`
- `features/main-progression-quest-chain.md`
- `features/sect-system.md`
- `shared-rules.md`

Resolution (2026-05-14):

- Final open questions closed: trade transport assumption, simple trade confirm flow, binding policy, inventory-full cancel behavior, private/friend chat retention, block scope, no temporary ally/party in current scope.
- `notes/player-interaction-group.md` promoted to `features/player-interaction-group.md`.
- Related references updated.

---

### AUDIT-018 — Home cave / machine / quest final clarification batch

Status: `resolved`

Affected docs:

- `features/home-cave-defense.md`
- `features/machine-system.md`
- `features/main-progression-quest-chain.md`
- `notes/inbox-mail-system.md`

Resolution (2026-05-14):

- Home cave: attack charm price fixed per charm tier, compensation clarified as system-paid to owner, blueprint tier only increases storage + gate HP, blurred unnamed detection below sense threshold, separate 10-player attacker and defender caps.
- Machine: templates are fully data-defined (name/type/tier/skill/stats), higher tiers recover slower, durability loss scales by blueprint tier, fallback guard behavior clarified.
- Main progression quest chain: objective family list locked, NPC objectives limited to static NPCs, chain continues across the full game, inbox split into its own grounding note.
- `notes/inbox-mail-system.md` created as a shared grounding note for reward overflow and future mail design.

---

## Agent Checklist For Future Updates

Before finishing any design task, `gamedesign` should report:

- docs checked for related shared rules
- docs updated for consistency
- shared rules updated, if any
- audit items created or resolved
- conflicts or canonical decisions needed from the user
