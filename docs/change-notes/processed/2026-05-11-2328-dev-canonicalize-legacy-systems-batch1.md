---
title: Canonicalize legacy systems batch 1
status: proposed
created_at: 2026-05-11
created_by: dev
change_type: documentation
areas:
  - maps
  - portals
  - monsters
  - rules
  - config-contracts
related_docs:
  - docs/maps/map-instance-and-world-entry-runtime.md
  - docs/maps/portal-travel-runtime.md
  - docs/monsters/enemy-runtime-batch1.md
  - docs/rules/world-instance-membership-invariant.md
  - docs/data-design/config-contracts/world-map-runtime-configs-batch1.md
  - docs/conflicts/map-travel-topology-vs-portal-semantics.md
  - docs/conflicts/portal-interaction-mode-runtime-gap.md
  - docs/conflicts/enemy-runtime-scope-and-reset-open-questions.md
---

# Summary

Canonicalized legacy systems batch 1 bằng second-brain workflow dựa trên extraction notes, design clarification notes, và code verification trực tiếp. Chỉ các phần evidence đủ rõ mới được đưa vào canonical docs; các điểm còn mâu thuẫn hoặc chưa đủ rõ được tách sang conflict reports.

# What changed

- Added canonical map/world-entry runtime doc.
- Added canonical portal-travel runtime doc.
- Added canonical enemy runtime batch-1 doc.
- Added focused world-instance membership invariant doc.
- Added batch-1 config-contract doc cho map/portal/enemy runtime keys.
- Added conflict notes cho travel-topology semantic drift, portal interaction mode gap, và enemy runtime coverage/reset open questions.

# Why

Legacy notes đã đủ dày để chuyển batch 1 sang repo-level second-brain docs, nhưng không đủ sạch để tự động hợp nhất mọi semantic thành truth cuối cùng.

# Verification

- Checked code in `GameServer/World/*`, `GameServer/Runtime/*`, `GameServer/Network/Handlers/TravelToMapHandler.cs`, `GameServer/Config/*`.
- Checked source notes under `docs/implementation/extractions/` and `docs/game-design-wp/clarifications/`.

# Follow-up

- Manager/design decision needed cho direct-travel vs portal-topology semantics.
- Manager/design decision needed cho portal interaction mode meaning.
- Manager/design decision needed cho enemy spawn-mode support, boss reset HP, và no-skill enemy policy.
