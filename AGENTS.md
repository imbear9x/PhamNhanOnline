# AGENTS.md - Repository Guardrails

This file is the lightweight global rule source for agents working inside `PhamNhanOnline`.

Detailed role behavior lives in OpenClaw agent config/workspaces. Keep this file small so agents can load it cheaply.

## Global Rules

- Preserve user changes unless explicitly asked to overwrite them.
- Stay in your assigned role. Route cross-role ambiguity to the correct role instead of silently deciding.
- Docs are intended design; code is current implementation; tests, logs, DB state, and runtime behavior are implementation evidence.
- If docs and implementation evidence conflict, report the conflict and the authority needed to resolve it.
- Read extra workflow docs only when triggered by the current task.
- Do not preload the whole docs tree.
- Minimize context: retrieve only what is needed for the current task.
- Do not touch `.openclaw/`, agent system config, tokens, or local runtime state unless the user explicitly asks for agent-system changes.
- Do not expose secrets, credentials, tokens, or private local config.

## Workflow Router

- Handoff queue and handoff process: `docs/agent-handoffs/README.md`
- Active handoffs: `docs/agent-handoffs/active/`
- Handoff queue: `docs/agent-handoffs/QUEUE.md`
- Shared Telegram group workflow: `docs/agent-workflows/group-collaboration-workflow.md`
- TechDesign workflow: `docs/agent-workflows/techdesign-workflow.md`
- TechDesign spec template: `docs/templates/techdesign-spec-template.md`
- Dev documentation workflow: `docs/agent-workflows/dev-documentation-workflow.md`
- Significant-change threshold: `docs/agent-workflows/significant-change-threshold.md`
- Change Note workflow: `docs/agent-workflows/change-note-workflow.md`
- Retrieval scope: `docs/agent-workflows/retrieval-depth-policy.md`
- Second Brain workflow: `docs/agent-workflows/second-brain-workflow.md`

## Role Rule Sources

- `dev`: OpenClaw `dev` agent config.
- `gamedesign`: `docs/game-design-wp/AGENTS.md` plus OpenClaw `gamedesign` agent config.
- `techdesign`: OpenClaw `techdesign` agent config plus TechDesign workflow/template docs when needed.
- `qa`: OpenClaw `qa` agent config.
- `reviewer`: OpenClaw `reviewer` agent config/workspace.
- `dev-client`: OpenClaw `dev-client` agent config. Implements text-editable Unity/C# client code and writes Unity Editor wiring instructions for the user.
- `client-reviewer`: OpenClaw `client-reviewer` agent config. Reviews Unity/C# client code and the user wiring guide before the user tests in Unity.
- `knowledge-manager`: OpenClaw `knowledge-manager` workspace.

## Handoff Intake

When a role is asked to check work:

- Read `docs/agent-handoffs/QUEUE.md`.
- Only consider rows where `Status = Ready` and `Owner` matches your role.
- If exactly one matching handoff exists, read it and restate the target before acting.
- If multiple matching handoffs exist, ask the user which one to do first.
- If none exist, say there is no ready handoff for your role and do not invent work.

## Implementation Notes

- Dev implements from GameDesign intent and TechDesign spec.
- After implementation, Dev creates a Reviewer handoff and includes QA notes/test scope/known gaps. Dev does not normally hand directly to QA.
- Reviewer is the technical review gate between Dev and QA. Reviewer sends Required Fix work back to Dev, or creates the QA handoff after Pass / Pass with risks.
- TechDesign designs from existing code patterns and project packet/runtime/DB conventions.
- QA verifies expected vs actual behavior using concrete evidence after Reviewer passes the implementation, unless the user explicitly skips Reviewer.
- After QA passes a server/shared feature, TechDesign owns client-contract synthesis before any `dev-client` handoff is created.
- Dev Client implements client code from TechDesign's client handoff, then writes a User Unity Implementation Guide and creates a `client-reviewer` handoff.
- Client Reviewer reviews Dev Client's code/docs, then either returns fixes to `dev-client` or creates an `Owner = user` handoff for Unity Editor wiring and manual testing.
- If user-side Unity testing shows server/spec changes are needed, `dev-client` captures the issue with the user and creates a handoff for `techdesign`, not directly for `dev`.
- Reviewer reviews technical quality, maintainability, performance, DB/schema risk, validation, anti-cheating risk, and testability.
- GameDesign owns gameplay intent, player-facing rules, progression/economy, and design consistency.

## VPS Verification Notes

- Server/shared/data tests should be runnable on the VPS with .NET.
- Unity client tests are out of scope on the VPS unless the user explicitly asks.
- If Unity/editor-only work blocks solution-level verification, report it as client-side verification for the user's local Unity machine.
- Treat `CientTest/InterestManagementVerifier` as a client-side/tooling project. Do not treat its current compile state as a blocker for server/shared gameplay implementation unless the user explicitly assigns that tool.
