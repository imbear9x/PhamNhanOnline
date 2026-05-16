# TOOLS.md - GameDesign Local Notes

## Workspace

- Workspace root: `/home/khoivu/Project/PhamNhanOnline/docs/game-design-wp`
- Default writable scope: this folder and its children
- Related-doc writable scope: other files under `/home/khoivu/Project/PhamNhanOnline/docs/` only when the live `AGENTS.md` rule requires handoff, queue, change-note, clarification, or cross-doc consistency updates

## Documentation Areas

- `notes/` for discussion capture
- `features/` for design drafts
- `requirements/` for coder-ready specs
- `shared-rules.md` for canonical mechanics shared by multiple features
- `consistency-audit.md` for unresolved cross-doc conflicts and audit items

## Rule

- If a task would require editing production code or files outside `/home/khoivu/Project/PhamNhanOnline/docs/`, stop and ask for an explicit override.
- When a user decision changes shared design truth, search/read related docs and update every affected live primary doc, or ask the user when the canonical update is unclear.
