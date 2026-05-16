# User Unity Handoff Template

---
title: <feature> Unity wiring and manual test
doc_type: handoff
status: Ready
owner: user
source_agent: client-reviewer
last_updated: YYYY-MM-DD
source_design_doc: <path>
source_tech_design_doc: <path>
expected_output: unity-editor-wiring-and-manual-test
queue_id: <id>
feature_key: <feature-key>
handoff_type: user-unity-test
source_handoff: <client-reviewer handoff>
response_to: <dev-client handoff>
supersedes: <optional previous user handoff>
iteration: <n>
---

# Goal

<What the user/outside Unity agent must wire and test.>

# Implementation Guide

- `<path>`

# Unity Editor Tasks

- [ ] create/edit prefab: `<name>`
- [ ] add component: `<component>`
- [ ] assign Inspector field: `<field>`
- [ ] wire button/event: `<event>`
- [ ] place prefab/UI in scene: `<scene/path>`

# Manual Test Scope

- [ ] <test case>
- [ ] <test case>

# Expected Result

<What should happen in Unity when the feature works.>

# Known Limitations

- <not verified on VPS>
- <accepted risk>

# If It Fails

Collect:

- screenshot/video if useful
- Unity console error
- server log line/time
- packet/action that triggered the issue

Then ask `dev-client` to classify and create a `techdesign` handoff if server/spec changes are needed.

