# Game Design Current State Docs

## Recommended reading order

1. `12_prompt_ready_handoff.md`
   Use this when you need a short copy-paste handoff for the next Game Design Agent.

2. `10_agent_context_summary.md`
   Use this for fast context loading. It is the shortest full-project summary in the pack.

3. `11_design_agent_review_addendum.md`
   Read this next. It explains what is truly playable today, what is placeholder-only, which assumptions are safe, and which design conversations are immediately actionable.

4. `01_game_overview.md`
   High-level game identity, current core loop, implemented systems, and missing systems.

5. `02_system_inventory.md`
   Per-system map with `Done / Partial / Prototype / Unknown`, related files, DB tables, config, and open questions.

6. `03_feature_flows.md`
   End-to-end feature flows from client trigger to server validation, persistence, response, and UI update.

7. `05_server_architecture.md`
   Source of truth for runtime authority, packet handling, services, persistence, validation, and anti-cheat posture.

8. `06_client_architecture.md`
   Source of truth for Unity scene/UI/network/state-sync behavior.

9. `04_database_design.md`
   Read this when you need to know where data lives, who writes it, who reads it, and which tables are runtime vs config.

10. `07_validation_and_rules.md`
    Read this when discussing hard game rules, exploit surface, gating, cooldowns, permissions, or failure conditions.

11. `08_error_and_block_cases.md`
    Read this when discussing UX failure states, error recovery, and technical edge cases.

12. `09_design_gaps_and_questions.md`
    Use this as the backlog for future design and architecture conversations.

## How to use this pack

- If you are a new Game Design Agent, start with `12`, `10`, and `11` first.
- If you are changing progression or combat balance, also read `02`, `03`, `04`, and `07`.
- If you are proposing a new system, check `11_design_agent_review_addendum.md` first to see whether the required backend/client foundations already exist.
- If code and DB seem inconsistent, treat the cited source files as the primary reference and mark the issue explicitly instead of guessing.

## Notes

- This pack avoids secrets. No password, token, or connection secret should be copied into downstream prompts.
- Unknown areas are intentionally labeled `Unknown / Need confirmation`.
- Current DB setup appears split across `database/phamnhan_online.sql` and `database/initDatabase.sql`; read the inconsistency notes before assuming bootstrap order.

## Primary sources

- `GameServer/*`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/*`
- `database/phamnhan_online.sql`
- `database/initDatabase.sql`
- `docs/reference-and-specs/*`
