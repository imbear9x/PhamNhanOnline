---
title: <feature or slice name>
doc_type: techdesign-spec
status: draft
owner: techdesign
created_at: YYYY-MM-DD
updated_at: YYYY-MM-DD
source_design_docs:
  - docs/game-design-wp/features/<feature>.md
related_shared_rules:
  - docs/game-design-wp/shared-rules.md
code_grounding:
  - <code file or implementation doc inspected>
---

# <Feature Or Slice Name> — Tech Design Spec

## Goal

Describe the implementation goal for this prototype slice.

## Source Design Summary

Summarize the player-facing rules being implemented. Do not introduce new gameplay rules here.

## Scope

### In Scope

- ...

### Out Of Scope

- ...

## Code Grounding Summary

List the existing code/docs inspected and the patterns found.

Required checks:

- packet handler pattern
- response packet pattern
- broadcast/notification pattern
- entity/model pattern
- DAO/repository/service pattern
- transaction/validation boundary
- seed/test/dev-tool pattern

## Current System Fit

Explain how this feature should fit into the current architecture.

Avoid inventing new layers when existing layers fit.

## DB / Schema Plan

### New Tables

| Table | Purpose | Key columns | Notes |
|---|---|---|---|
| | | | |

### Changed Tables

| Table | Change | Reason | Migration notes |
|---|---|---|---|
| | | | |

### Indexes / Constraints

- ...

### Entity / DAO / Repository Plan

- Entity/model:
- DAO:
- Repository:
- Service:

## Config / Seed Data Plan

### Config Contracts

| Config key/table | Purpose | Example prototype value |
|---|---|---|
| | | |

### Seed Data

List the minimum data needed for a playable test.

- items:
- NPCs:
- maps:
- quests/tasks:
- recipes:
- configs:

### Local DB Test Setup

Describe any local/dev DB edits or scripts needed for repeated manual tests.

## Packet And Broadcast Flow

Use project packet terminology. Do not describe generic REST APIs unless the codebase uses them.

### Request Packets

| Packet | Sender | Handler | Validation | Result |
|---|---|---|---|---|
| | | | | |

### Response Packets

| Packet | Receiver | When sent | Payload summary |
|---|---|---|---|
| | | | |

### Broadcast Packets

| Broadcast | Receivers | Trigger | Payload summary |
|---|---|---|---|
| | | | |

## Runtime Flow

Describe the authoritative server flow step by step.

1. ...
2. ...
3. ...

## Validation And Authority

- server authority:
- client trust level:
- anti-abuse checks:
- race-condition handling:
- transaction boundary:

## Persistence Flow

- when data is written:
- dirty state / runtime flush behavior:
- rollback behavior:

## Implementation Slices

Break into dev-friendly slices.

1. ...
2. ...
3. ...

## Dev Acceptance Criteria

- [ ] ...
- [ ] ...
- [ ] ...

## Automated Test Cases

- ...

## Manual E2E Test Script

1. Start with:
2. Do:
3. Expect:

## Debug / Dev Tools Needed

- seed command:
- admin command:
- log/debug panel:
- reset/cleanup:

## Open Questions

Route gameplay questions to `gamedesign` / user.
Route code ownership questions to `dev` / manager.

- [ ] ...

## Risks

- ...
