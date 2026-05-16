---
title: Client Dev - <Feature/Slice Name>
doc_type: handoff
status: Ready
owner: dev-client
source_agent: techdesign
last_updated: YYYY-MM-DD
source_design_doc: docs/game-design-wp/requirements/<feature>.md
source_tech_design_doc: docs/tech-design/<feature>.md
expected_output: unity-client-implementation
queue_id: <next queue id>
feature_key: <feature-key>
handoff_type: client-dev
source_handoff: docs/agent-handoffs/active/<qa-passed-report>.md
response_to: docs/agent-handoffs/active/<qa-passed-report>.md
supersedes:
iteration: <n>
---

# Goal

Describe the exact Unity/client outcome expected from this handoff.

# Source Authority

| Source | Purpose |
|---|---|
| `<QA passed report>` | Defines what server behavior has evidence and is release/client-ready |
| `<TechDesign spec>` | Defines server contract, packets, data, and runtime authority |
| `<Requirement/design doc>` | Defines player-facing behavior |
| `<Relevant code files>` | Confirms packet IDs, fields, model names, and error codes |

# Canonical Server Contract

Summarize only the server behavior that has passed QA or has explicit accepted-risk status.

# Packet Contract

| Packet | ID | Direction | Important fields | Client handling |
|---|---:|---|---|---|
| `<PacketName>` | `<id>` | C->S / S->C / broadcast | `<fields>` | `<handling>` |

Use packet IDs from code attributes, not stale docs.

# Model Contract

| Model | Important fields | Client render/cache usage |
|---|---|---|
| `<ModelName>` | `<fields>` | `<usage>` |

# UI / State Rules

- Success behavior:
- Failure behavior:
- Refresh/reload strategy:
- Optimistic update rules:
- State that must not be removed on failure:
- Reconnect/relogin/map-change rules, if relevant:

# Error / Message Code Handling

| Code | Context | Client behavior |
|---|---|---|
| `<MessageCode>` | `<when returned>` | `<toast/UI/state rule>` |

# Accepted Risks / Client Tolerance

List backend accepted risks the client must tolerate without treating them as broken state.

# Out Of Scope

- `<explicitly excluded client behavior>`

# Dev-Client Implementation Checklist

- [ ] `<network/packet task>`
- [ ] `<UI task>`
- [ ] `<state/cache task>`
- [ ] `<error handling task>`

# Client Self-Test Checklist

- [ ] `<happy path>`
- [ ] `<failure path>`
- [ ] `<state preservation case>`
- [ ] `<relogin/map/retry case if relevant>`

# User Manual E2E Checklist

Steps the user can run manually after pulling the Unity/client changes.
