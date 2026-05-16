---
handoff_id: 20260515-24
queue_id: 48
title: Main Progression Quest Chain — TechDesign Spec
type: requirement-to-techdesign
status: Ready
owner: techdesign
source_design_doc: requirements/main-progression-quest-chain.md
feature_doc: features/main-progression-quest-chain.md
created_at: 2026-05-15
created_by: gamedesign
iteration: 1
response_to: null
supersedes: null
---

# Handoff: Main Progression Quest Chain — TechDesign

## Summary

Chuỗi nhiệm vụ tiến trình chính: tuyến tính, 1 quest active mọi lúc, auto-complete, reward tự động, data-driven hoàn toàn. Không có fail/abandon. Balo đầy thì reward item vào inbox.

Requirement doc đầy đủ tại: `requirements/main-progression-quest-chain.md`

---

## TechDesign cần làm

### 1. Verify runtime existence
- Quest system server-side đã có chưa? Schema, service, event hook?
- Objective tracking framework đã có chưa? (event bus per objective type?)
- Reward grant + unlock flow đã có chưa?
- Quest Panel client đã có chưa?

### 2. Tech spec các điểm cần xác định

| Điểm | Câu hỏi |
|---|---|
| Objective event bus | Mỗi objective type cần hook vào event nào? (kill hook, collect hook, travel hook, NPC interact hook...) |
| State objective check | Khi quest activate, check state objective thế nào — query realtime hay snapshot? |
| Auto-complete atomicity | Quest complete + reward grant + next quest activate trong 1 transaction không? |
| Unlock mechanism | Unlock tính năng/map/phó bản — server-side flag hay permission table? |
| Quest Panel sync | Client poll hay server push khi objective tiến độ thay đổi? |
| Offline settle | Objective count có thể tăng offline không? (ví dụ cultivation — không; kill quái — không thể offline; travel — không thể offline). Chỉ cần persist, không cần settle. |

### 3. Objective type extensibility
- Objective type enum phải extensible trong DB — thêm type mới không cần code change core.
- Minimum types cần support: `kill`, `collect`, `craft`, `talk_to_npc`, `join_sect`, `kill_boss`, `open_cave`, `travel`.

### 4. Output
- `tech-design/main-progression-quest-chain.md` — schema, event hooks per objective type, auto-complete flow, reward flow, unlock mechanism, Quest Panel sync approach.

---

## Key design rules (không thay đổi)

- **Luôn đúng 1 quest active** — không 2 quest song song.
- **Data-driven hoàn toàn** — không hardcode quest/objective/reward trong code.
- **Auto-complete atomic**: complete + reward + activate next trong 1 flow.
- **Tiến độ trước khi active không tính** — trừ state objective.
- **State objective auto-complete ngay khi quest activate** nếu đã thỏa.
- `talk_to_npc` là **manual trigger duy nhất** — không auto.
- **Unlock reward không bị chặn bởi balo** — apply ngay.
- Reward item balo đầy → **inbox** (không mất).
- **Không có fail / abandon / reset**.

---

## Acceptance Gate

- [ ] Runtime existence confirmed.
- [ ] Objective event hook approach designed per type.
- [ ] Auto-complete atomicity approach decided.
- [ ] Unlock mechanism decided.
- [ ] Quest Panel sync approach decided.
- [ ] `tech-design/main-progression-quest-chain.md` created.
