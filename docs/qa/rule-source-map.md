# Rule Source Map

Date: `2026-05-11`

This report maps which rule files are live, which are shadow, and which are documentation-only references after the rule-boundary cleanup.

## OpenClaw Runtime Source

Primary runtime mapping comes from:

- `/home/vm-01/.openclaw/openclaw.json`

## Live Rule Files

| Path | Status | Used By |
| --- | --- | --- |
| `AGENTS.md` | Live global behavior source | Repo-wide policy; live rule file for `dev` |
| `docs/game-design-wp/AGENTS.md` | Live per-agent rule | `gamedesign` |
| `/home/vm-01/.openclaw/workspaces/manager/AGENTS.md` | Live per-agent rule | `manager` |
| `/home/vm-01/.openclaw/workspaces/devops/AGENTS.md` | Live per-agent rule | `devops` |
| `/home/vm-01/.openclaw/workspaces/knowledge-manager/AGENTS.md` | Live per-agent rule | `knowledge-manager` |
| `WORKFLOW_RULES.md` | Live router only | Startup routing; not a second policy source |

## Shadow Or Non-Live Rule Files

| Path | Status | Notes |
| --- | --- | --- |
| `/home/vm-01/.openclaw/workspaces/dev/AGENTS.md` | Non-live shadow | Marked with `NON-LIVE SHADOW RULE FILE`; live source is repo root `AGENTS.md` |
| `/home/vm-01/.openclaw/workspaces/gamedesign/AGENTS.md` | Non-live shadow | Marked with `NON-LIVE SHADOW RULE FILE`; live source is `docs/game-design-wp/AGENTS.md` |
| `docs/archive/legacy-docs-agents-guide.md` | Non-live legacy reference | Preserved historical mixed-scope guide moved out of live docs path |

## Docs Knowledge Files, Not Behavioral Rule Sources

| Path | Status | Purpose |
| --- | --- | --- |
| `docs/agent-workflows/*.md` | Workflow knowledge | Trigger-based workflow detail, not default preload behavior |
| `docs/rules/*.md` | Project knowledge | Ownership, governance, and system rules for the second-brain layer |
| `docs/qa/*.md` | QA and audit notes | Evidence, audit, and consolidation reporting; not direct behavior rules |

## Agent To Rule Mapping

| Agent | Rule Source |
| --- | --- |
| `dev` | `AGENTS.md` |
| `gamedesign` | `docs/game-design-wp/AGENTS.md` |
| `manager` | `/home/vm-01/.openclaw/workspaces/manager/AGENTS.md` |
| `devops` | `/home/vm-01/.openclaw/workspaces/devops/AGENTS.md` |
| `knowledge-manager` | `/home/vm-01/.openclaw/workspaces/knowledge-manager/AGENTS.md` |

## Notes

- `WORKFLOW_RULES.md` remains intentionally small and routes to the live sources above.
- There is no live `docs/AGENTS.md`; the old mixed-scope content lives only in `docs/archive/legacy-docs-agents-guide.md`.
- The cleanup did not rewrite OpenClaw runtime topology; it only clarified and reduced rule surfaces.
