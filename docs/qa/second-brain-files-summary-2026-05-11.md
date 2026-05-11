---
title: Second brain files summary
doc_type: qa-note
status: reviewed
owner: devops
last_verified: 2026-05-11
tags:
  - summary
  - second-brain
  - docs
---

# Second Brain Files Summary

> Bản tóm tắt cực ngắn để nhìn một lượt biết hệ thống second-brain đã làm tới đâu.

## Core system / entry

| File | Tóm tắt ngắn |
|---|---|
| `docs/README.md` | Giải thích docs hub mới, source-of-truth model, và rule migration. |
| `docs/AGENTS.md` | Vá rule second brain, Change Note, và cách agent ghi tri thức. |
| `docs/DOCS_INDEX.md` | Mục lục docs đã mở rộng cho second brain và workflow mới. |
| `docs/index/project-map.md` | Bản đồ tổng quan knowledge system và các điểm vào chính. |
| `docs/index/second-brain-index.md` | Index ngắn cho toàn bộ lớp second brain trong repo. |

## Migration / graph / indexing

| File | Tóm tắt ngắn |
|---|---|
| `docs/index/legacy-knowledge-inventory.md` | Inventory toàn bộ docs legacy trước khi canonicalize. |
| `docs/index/legacy-doc-classification.md` | Phân loại docs cũ: canonical, draft, derived, split-needed. |
| `docs/index/legacy-path-mapping.md` | Mapping từ docs cũ sang canonical docs mới. |
| `docs/index/legacy-path-mapping-review.md` | Review hardening cho mapping trước bulk migration. |
| `docs/index/knowledge-graph-entry.md` | Điểm vào chính để mở Obsidian graph theo cụm tri thức. |
| `docs/index/runtime-knowledge-map.md` | Hub note cho cụm runtime, world, combat, inventory. |
| `docs/index/architecture-knowledge-map.md` | Hub note cho cụm server/client architecture và integration. |
| `docs/index/config-and-contract-map.md` | Hub note cho config contracts và docs phụ thuộc config. |
| `docs/index/workflow-and-governance-map.md` | Hub note cho governance, workflow, queue, và audits. |

## Governance / workflow / templates

| File | Tóm tắt ngắn |
|---|---|
| `docs/rules/second-brain-governance.md` | Governance lõi cho canonical docs, verification, ownership, migration. |
| `docs/rules/knowledge-ownership.md` | Chốt ai sở hữu implementation truth, design truth, stewardship, ops. |
| `docs/rules/retrieval-strategy.md` | Chốt retrieval hiện tại: repo-docs-first, memory-as-aid. |
| `docs/agent-workflows/second-brain-workflow.md` | Workflow tạo và bảo trì canonical project memory. |
| `docs/agent-workflows/docs-lifecycle.md` | Lifecycle docs từ draft tới reviewed, verified, conflict. |
| `docs/agent-workflows/change-note-workflow.md` | Workflow phát hành Change Note khi có thay đổi đáng kể. |
| `docs/agent-workflows/knowledge-manager-workflow.md` | Trách nhiệm stewardship tổng quát của Knowledge Manager. |
| `docs/agent-workflows/semi-automatic-knowledge-manager-workflow.md` | Workflow inbox → processed / needs-review cho Change Note. |
| `docs/templates/system-doc-template.md` | Template chuẩn cho system/runtime docs canonical. |
| `docs/templates/config-contract-template.md` | Template chuẩn cho config contract. |
| `docs/templates/change-note-template.md` | Template chuẩn cho Change Note inbox workflow. |
| `docs/templates/conflict-report-template.md` | Template chuẩn cho conflict report. |

## Canonical runtime / systems / rules

| File | Tóm tắt ngắn |
|---|---|
| `docs/systems/phase1-runtime-flow.md` | Flow phase 1 tổng quát đã canonicalize và ground bằng code. |
| `docs/systems/auth-character-world-phase1.md` | Flow login → character → enter world → bootstrap scene World. |
| `docs/systems/world-scene-readiness-runtime.md` | Mô hình readiness/load cycle chống race condition trong scene World. |
| `docs/systems/phase1-feature-flow-index.md` | Index rút gọn feature flows phase 1 và trỏ sang docs domain. |
| `docs/systems/world-observer-and-movement-runtime.md` | Local movement, sync policy, observer packets, remote presentation. |
| `docs/rules/client-state-sync-runtime.md` | Rule canonical cho client state sync ownership và bootstrap model. |
| `docs/rules/server-validation-and-runtime-rules.md` | Index canonical cho validation/runtime guards phía server. |
| `docs/rules/server-transaction-boundary.md` | Rule transaction boundary phía server cho write-flow và notifier safety. |

## Canonical domain docs

| File | Tóm tắt ngắn |
|---|---|
| `docs/combat/skill-combat-runtime.md` | Flow combat skill runtime phía server, từ validate tới impact. |
| `docs/inventory/item-use-flow.md` | Flow generic UseItemPacket, lock, transaction, route theo item type. |

## Config / implementation / architecture

| File | Tóm tắt ngắn |
|---|---|
| `docs/data-design/config-contracts/game-configs-phase1.md` | Contract canonical cho `game_configs` và key phase 1 đã verify. |
| `docs/implementation/knowledge-manager-bootstrap.md` | Ghi lại cách bootstrap agent Knowledge Manager vào OpenClaw. |
| `docs/implementation/unity-shared-sync-and-build-guide.md` | Rule sync GameShared sang Unity và verify build. |
| `docs/implementation/server-runtime-architecture.md` | Kiến trúc runtime server: network, services, world tick, persistence. |
| `docs/implementation/client-runtime-architecture.md` | Kiến trúc runtime client: states/services, world presentation, UI. |

## QA / audit / conflict / queue

| File | Tóm tắt ngắn |
|---|---|
| `docs/qa/doc-status-conventions.md` | Quy ước status draft, reviewed, verified, conflict cho docs. |
| `docs/qa/knowledge-acceptance-checklist.md` | Checklist để chấp nhận một doc vào second brain. |
| `docs/qa/knowledge-audit-process.md` | Quy trình audit docs và knowledge hygiene. |
| `docs/qa/retrieval-smoke-test.md` | Các query smoke test cho retrieval / memory. |
| `docs/qa/knowledge-audit-2026-05-11-initial.md` | Audit đầu tiên sau khi dựng second brain nền. |
| `docs/qa/canonicalization-status-audit-2026-05-11.md` | Audit repo-wide: cái gì canonicalized, cái gì còn legacy. |
| `docs/qa/canonicalization-status-audit-2026-05-11-final-pass.md` | Audit sau final pass, phản ánh backlog legacy đã giảm. |
| `docs/conflicts/item-use-notifier-ordering-review.md` | Review conflict nhẹ về notifier ordering trong item use flow. |
| `docs/change-notes/README.md` | Hướng dẫn queue inbox/processed/needs-review cho Change Notes. |
| `docs/change-notes/needs-review/2026-05-11-0930-gamedesign-linh-thach-mining-rule.md` | Sample note đã được KM review và đẩy sang needs-review. |

## Kết luận ngắn

- **Đã có framework second brain hoàn chỉnh**
- **Đã có canonical docs cốt lõi cho runtime/combat/inventory/config/architecture**
- **Đã có workflow + Knowledge Manager + Change Note queue**
- **Đã có Obsidian graph entry và hub notes**
- **Chưa full mọi domain gameplay, nhưng hệ đã live và dùng được thật**
