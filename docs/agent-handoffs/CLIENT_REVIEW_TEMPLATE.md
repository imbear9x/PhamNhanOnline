# Client Review Handoff Template

---
title: <feature> client implementation review
doc_type: handoff
status: Ready
owner: client-reviewer
source_agent: dev-client
last_updated: YYYY-MM-DD
source_design_doc: <path>
source_tech_design_doc: <path>
expected_output: client-code-review
queue_id: <id>
feature_key: <feature-key>
handoff_type: client-review
source_handoff: <dev-client source handoff>
response_to: <dev-client source handoff>
supersedes: <optional previous client-review handoff>
iteration: <n>
---

# Review Target

<What client code/docs were changed and why.>

# Source Authority

- TechDesign client handoff: `<path>`
- QA report: `<path>`
- Server/client contract docs: `<path>`

# Changed Files

- `<path>`: <reason>

# User Unity Implementation Guide

- Guide path: `<path>`

# Dev Client Notes

- <known limitation>
- <manual Unity step required>
- <not verified on VPS>

# Reviewer Checklist

- [ ] packet DTO/handler matches contract
- [ ] state updates are safe for duplicate/delayed packets
- [ ] UI/controller/service boundaries are maintainable
- [ ] event subscriptions are cleaned up
- [ ] message/error mapping is complete
- [ ] guide matches implemented code
- [ ] user manual test scope is clear

