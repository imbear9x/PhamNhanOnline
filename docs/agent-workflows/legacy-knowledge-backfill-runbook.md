---
title: Legacy knowledge backfill runbook
doc_type: workflow
status: reviewed
owner: devops
last_verified: 2026-05-11
tags:
  - workflow
  - second-brain
  - legacy
---

# Legacy Knowledge Backfill Runbook

Runbook này chuẩn hóa cách hấp thụ **toàn bộ legacy systems đã code từ trước** vào second-brain mà không bắt một agent gánh hết.

## Roles

### manager
Vai trò dùng trong runbook này: **Legacy System Analyst**

Làm:
- đọc code/runtime cũ
- tạo extraction notes
- không chốt design intent
- không canonicalize

### gamedesign
Làm:
- đọc extraction notes
- chốt gameplay intent / terminology / intended rules
- ghi mismatch vs current code nếu có
- không tự khẳng định code truth

### knowledge-manager
Làm:
- canonicalize từ extraction + clarification
- tạo canonical docs repo-level
- tạo conflict note nếu lệch hoặc chưa đủ rõ
- không tự bịa truth

## Storage locations

### Extraction notes
- `docs/implementation/extractions/`

### Design clarification notes
- `docs/game-design-wp/clarifications/`

### Canonical docs
- `docs/maps/`
- `docs/monsters/`
- `docs/systems/`
- `docs/rules/`
- `docs/skills/`
- `docs/cultivation/`
- `docs/inventory/`
- `docs/data-design/config-contracts/`
- `docs/conflicts/` when needed

## One domain lifecycle

1. `manager` creates extraction note
2. `gamedesign` creates clarification note
3. `knowledge-manager` canonicalizes or marks needs-review
4. update `docs/index/legacy-knowledge-backfill-master-checklist.md`

## Never-skip rule

Không canonicalize domain lớn chỉ từ draft note hoặc trí nhớ.

Ít nhất phải có:
- code/runtime extraction
- hoặc evidence doc + explicit uncertainty marking

## Done definition for the full legacy sweep

Full sweep chỉ được coi là done khi:
- không còn domain nền quan trọng ở trạng thái `missing`
- các domain còn lại ít nhất ở `needs-review`
- các domain nền đã có canonical docs đủ để feature sau này link tới

## Priority note

Nếu muốn an tâm phát triển về sau, bắt buộc hoàn tất trước các domain nền:
- map
- portal / world transition
- enemy
- player stats
- equipment
