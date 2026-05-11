# Rule Consolidation Report

Date: `2026-05-11`

## Scope

This report covers the rule-boundary cleanup requested after the rule-bloat audit. The goal was to reduce fragmentation, mark live vs shadow sources, keep Second Brain workflow trigger-based, and avoid rewriting the OpenClaw runtime setup.

## Files Changed

- `AGENTS.md`
- `WORKFLOW_RULES.md`
- `docs/archive/legacy-docs-agents-guide.md`
- `docs/game-design-wp/AGENTS.md`
- `docs/agent-workflows/change-note-workflow.md`
- `docs/agent-workflows/second-brain-workflow.md`
- `docs/agent-workflows/dev-documentation-workflow.md`
- `docs/agent-workflows/knowledge-manager-workflow.md`
- `docs/agent-workflows/significant-change-threshold.md`
- `docs/agent-workflows/retrieval-depth-policy.md`
- `docs/agent-workflows/token-efficiency-policy.md`
- `docs/rules/knowledge-ownership.md`
- `docs/qa/rule-source-map.md`
- `docs/change-notes/inbox/2026-05-11-2345-dev-rule-consolidation-cleanup.md`
- `/home/vm-01/.openclaw/workspaces/dev/AGENTS.md`
- `/home/vm-01/.openclaw/workspaces/gamedesign/AGENTS.md`
- `/home/vm-01/.openclaw/workspaces/manager/AGENTS.md`
- `/home/vm-01/.openclaw/workspaces/devops/AGENTS.md`
- `/home/vm-01/.openclaw/workspaces/knowledge-manager/AGENTS.md`

## Live Rule Sources After Cleanup

- Global behavior source: `AGENTS.md`
- Startup router: `WORKFLOW_RULES.md`
- Live `dev` rule: `AGENTS.md`
- Live `gamedesign` rule: `docs/game-design-wp/AGENTS.md`
- Live `manager` rule: `/home/vm-01/.openclaw/workspaces/manager/AGENTS.md`
- Live `devops` rule: `/home/vm-01/.openclaw/workspaces/devops/AGENTS.md`
- Live `knowledge-manager` rule: `/home/vm-01/.openclaw/workspaces/knowledge-manager/AGENTS.md`

## Shadow Files Marked Or Archived

- Marked non-live shadow:
  - `/home/vm-01/.openclaw/workspaces/dev/AGENTS.md`
  - `/home/vm-01/.openclaw/workspaces/gamedesign/AGENTS.md`
- Archived legacy mixed-scope guide:
  - `docs/archive/legacy-docs-agents-guide.md`

## Conflicts Resolved

- `docs/AGENTS.md` was removed so `docs/` no longer exposes a second AGENTS file at all
- `WORKFLOW_RULES.md` no longer duplicates broad policy from root `AGENTS.md`
- `gamedesign` rule now states a clear default write scope and a separate docs-level grounding policy
- `dev` vs `knowledge-manager` ownership is clearer: `dev` creates implementation-side truth and significant Change Notes; `knowledge-manager` reconciles canonical docs from Change Notes

## Duplicate Policies Removed

- Removed the broad duplicate policy bundle from `WORKFLOW_RULES.md`
- Removed the extra `docs/AGENTS.md` surface entirely and kept only the archived legacy reference
- Collapsed repeated token-efficiency guidance into one global line plus `docs/agent-workflows/token-efficiency-policy.md`
- Replaced implicit Change Note guessing with `docs/agent-workflows/significant-change-threshold.md`
- Replaced implicit retrieval sprawl with `docs/agent-workflows/retrieval-depth-policy.md`

## Remaining Risks

- Older support docs and indexes may still use pre-cleanup wording
- External workspace agent rules still exist outside the repo and can still drift if edited casually
- `manager`, `devops`, and `knowledge-manager` are not auto-mounted inside the repo, so they rely on their own live files plus pointers back to the repo-global source
- This pass did not normalize every secondary doc that references the old layout

## Recommended Next Steps

- Align older support docs such as `docs/DOCS_INDEX.md` and selected indexes with the new lighter rule layout
- Keep new rule additions in root `AGENTS.md` or `docs/agent-workflows/`, not in a second AGENTS file under `docs/`
- If future audit noise appears, prefer marking non-live files earlier instead of adding more explanation layers
