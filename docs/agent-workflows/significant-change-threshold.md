# Significant Change Threshold

Use this policy before deciding whether the current task needs no Change Note, a short Change Note, or the full second-brain workflow.

## No Change Note Needed

- typo fixes
- formatting-only edits
- local variable renames
- local refactors that do not change behavior or meaning
- test-only churn that does not change system meaning
- implementation that follows an existing spec without creating durable new knowledge

## Short Change Note Needed

- small gameplay logic adjustments
- validation behavior changes
- packet or response behavior changes
- config value or config meaning changes
- bug fixes with durable impact that future agents should know

## Full Workflow Needed

- new system introduction
- gameplay rule changes
- economy, progression, or drop-rule changes
- DB schema changes
- state-machine changes
- server authority or validation model changes
- behavior changes across multiple systems
- docs/code conflicts or truth disagreements

## Rule Of Thumb

If future agents could implement, debug, or document the area incorrectly because this change is not recorded, do at least the short Change Note workflow.
