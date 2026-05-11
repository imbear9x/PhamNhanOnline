---
doc_type: change_note
status: needs-review
created_by: "Game Design Agent"
created_at: "2026-05-11 09:30"
task_id: "test-linh-thach-mining"
agent: "game-design"
change_type: "gameplay_rule"
affected_systems:
  - linh-thach-mining
  - cultivation
  - player-state
  - economy
affected_docs:
  - docs/resource-mining/linh-thach-mining.md
  - docs/cultivation/cultivation-runtime-rules.md
  - docs/rules/player-state-rules.md
  - docs/data-design/config-contracts/mining-linh-thach-max-per-character.md
affected_code: []
affected_configs:
  - mining.linh_thach.max_per_character
affected_db: []
requires_knowledge_manager: true
knowledge_manager_status: needs-review
knowledge_manager_reviewed_at: "2026-05-11 15:10"
knowledge_manager_reason: "Rule intent is clear, but canonical processing is blocked by unresolved scope for the 10-item limit and missing canonical domain docs."
knowledge_manager_findings:
  - "The note is structurally valid and relevant to gameplay/system knowledge."
  - "The proposed cap '10 linh thạch' does not specify whether it is per day, per character lifetime, per mining session, or per node/area."
  - "The state-conflict rule 'Mining and Cultivating cannot happen at the same time' is directionally clear but still needs canonical player-state semantics."
  - "Affected canonical docs referenced by the note do not exist yet in the repo."
  - "No code/config evidence was provided, so this cannot be marked as implemented or verified."
---

# Change Summary

Đề xuất rule cho hệ thống khai thác linh thạch:
- Mỗi nhân vật chỉ được khai thác tối đa 10 linh thạch.
- Trong lúc khai thác linh thạch thì không thể tu luyện.

# What Changed

- Thêm rule giới hạn số linh thạch tối đa mỗi nhân vật.
- Thêm rule state conflict: Mining và Cultivating không được đồng thời.

# Why

- Giới hạn nguồn cung linh thạch.
- Tránh player vừa farm tài nguyên vừa tăng tu vi cùng lúc.
- Tạo lựa chọn gameplay giữa khai thác tài nguyên và tu luyện.

# Affected Systems

- linh-thach-mining
- cultivation
- player-state
- economy

# Affected Docs

- docs/resource-mining/linh-thach-mining.md
- docs/cultivation/cultivation-runtime-rules.md
- docs/rules/player-state-rules.md
- docs/data-design/config-contracts/mining-linh-thach-max-per-character.md

# Affected Code

- Unknown. Coder Agent cần inspect sau.

# Config / Data Changes

Cần Config Contract:

- config_key: mining.linh_thach.max_per_character
- default_value: 10
- type: int
- scope: character
- enforced_by: server

# DB Changes

Unknown.

# QA / Test Notes

Cần test:
- player đang Mining thì không thể StartCultivation.
- player đang Cultivating thì không thể StartMining.
- giới hạn 10 linh thạch được enforce server-side.
- config đổi được mà không hardcode.

# Potential Conflicts / Risks

- Có thể code CultivationService hiện tại chưa check Mining state.
- Có thể player state hiện tại chưa có trạng thái Mining.

# Questions For Manager

- Giới hạn 10 là theo ngày, theo phiên, theo nhân vật vĩnh viễn hay theo mỏ?
- Có cần cooldown sau khi khai thác không?

# Knowledge Manager Review Outcome

## Result

Needs review.

## Why not processed yet

- Chưa đủ rõ để tạo canonical rule mà không tự suy diễn thiết kế.
- Chưa có canonical doc hiện hữu cho mining / cultivation / player-state trong domain mới để update trực tiếp.
- Chưa có code/config evidence để chuyển thành implementation-grounded knowledge.

## Recommended next action

Manager hoặc GameDesign chốt rõ:

1. phạm vi của giới hạn `10`
2. semantics của state conflict Mining ↔ Cultivating
3. có hay không cooldown / reset rule

Sau khi chốt, Knowledge Manager có thể:

- tạo canonical docs đầu tiên cho domain này
- tạo config contract tương ứng
- chuyển note sang `processed/`
