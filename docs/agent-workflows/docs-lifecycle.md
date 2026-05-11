# Docs Lifecycle

## Status meanings

- `draft`: working note, not yet trusted as canonical
- `reviewed`: structure is acceptable, but code/runtime verification may still be partial
- `verified`: checked against code, config, or runtime evidence
- `deprecated`: retained for history, not the current source of truth

## Lifecycle

1. Create in `draft`.
2. Review scope, owner, and links.
3. Verify against code/runtime.
4. Promote to `verified` when grounded.
5. Mark `deprecated` instead of deleting when replacing a doc.

## Non-destructive rule

Do not erase history just to make the tree cleaner. Prefer status changes, replacement links, and change notes.
