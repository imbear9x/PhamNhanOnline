# Retrieval Smoke Test

Use this after seeding memory docs.

## Preconditions

- workspace `memory/` exists
- index has been built with `openclaw memory index`

## Checks

1. Search for core domain terms.
2. Search for a config/system name.
3. Search for a decision or ADR keyword.
4. Confirm top hits point to canonical docs, not random stale notes.

## Suggested commands

```bash
openclaw memory status --json
openclaw memory search --query "combat" --max-results 5
openclaw memory search --query "config contract" --max-results 5
```
