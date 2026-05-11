# Retrieval Depth Policy

Use this policy to keep context light and task-driven.

## Default Light Task

- read the live global rule source
- read the live per-agent rule source
- inspect directly relevant files only

## Medium Task

- read one relevant workflow doc
- read the direct canonical system doc for the area
- create a Change Note only if durable knowledge changed

## Full Task

- read the direct system docs and linked dependencies that materially affect the task
- read ADRs or config contracts only when they are clearly relevant
- inspect implementation, evidence, and conflict surfaces
- report retrieved knowledge when the task depends on substantial cross-system context

## Do Not

- preload the whole `docs/` tree
- read the whole `docs/agent-workflows/` folder by default
- treat audit or summary docs as equal to canonical docs unless they are the only available evidence
- pull in large legacy bundles when a direct canonical doc already answers the question
