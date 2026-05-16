# TOOLS.md - Local Agent Notes

## Project

- Primary repository workspace: `/home/khoivu/Project/PhamNhanOnline`
- Solution file: `PhamNhanOnline.sln`
- Main code areas: `GameServer/`, `GameShared/`, `ClientUnity/`, `database/`, `docs/`

## Agent Notes

- This file exists for local agent prompt bootstrap.
- Keep agent-local notes here if they help ongoing engineering work.
- Do not store secrets unless explicitly asked.

## Dev Notes

If you are `dev`:

- Implement gameplay features from the combined GameDesign + TechDesign contract.
- Use GameDesign docs for player-facing rules and TechDesign docs for code/data/test structure.
- Do not run Unity client tests on this VPS unless the user explicitly asks; the user will pull code and test Unity client locally.
- Default server/shared verification commands:
  - `dotnet restore PhamNhanOnline.sln -p:EnableWindowsTargeting=true`
  - `dotnet build PhamNhanOnline.sln -p:EnableWindowsTargeting=true`
  - `dotnet test PhamNhanOnline.sln -p:EnableWindowsTargeting=true`
- Use `-p:EnableWindowsTargeting=true` because the solution contains a Windows-targeted admin tool and this VPS runs Linux.
- If full-solution verification is blocked by an unrelated utility/diagnostic project, run scoped commands for the touched projects, for example `dotnet build GameServer/GameServer.csproj --no-restore` and `dotnet build GameShared/GameShared.csproj --no-restore`, then report the full-solution blocker.
- `CientTest/InterestManagementVerifier` is treated as client-side/tooling. Ignore it for VPS server/shared verification unless the user explicitly asks to work on it.
- If verification cannot run, report the exact missing SDK/tool or failing command.

## TechDesign Notes

If you are `techdesign`:

- Use `docs/agent-workflows/techdesign-workflow.md` as the workflow source.
- Use `docs/templates/techdesign-spec-template.md` for technical specs.
- Read relevant GameDesign docs under `docs/game-design-wp/` before writing tech design.
- Check `docs/game-design-wp/shared-rules.md` for canonical shared gameplay rules.
- Check `docs/game-design-wp/consistency-audit.md` for unresolved design conflicts.
- Inspect existing code before proposing DB schema, packets, broadcasts, runtime flow, entity shape, DAO/repository/service boundaries, seed data, or tests.
- Use packet and broadcast terminology that matches this codebase.
- Write durable tech design docs under `docs/tech-design/`.
- When asked to edit local/dev DB test data, record what changed and why in the related tech design note or response.
