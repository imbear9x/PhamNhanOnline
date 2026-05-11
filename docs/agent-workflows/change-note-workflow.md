# Change Note Workflow

Create a change note for meaningful technical or design changes that future agents should be able to scan quickly.

Use `docs/agent-workflows/significant-change-threshold.md` to decide whether the current task needs no Change Note, a short Change Note, or the full workflow.
For implementation work, pair this with `docs/agent-workflows/dev-documentation-workflow.md` so `dev` knows when a Change Note is enough and when a canonical doc or implementation note must also be updated.

## Good candidates

- workflow changes
- architecture changes
- contract changes
- DB / config meaning changes
- validation / transaction rule changes
- rollout / migration steps
- important fixes with operational impact

## Not required for

- trivial typo fixes
- obvious formatting-only edits
- local implementation churn with no durable doc value
