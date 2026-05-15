---
handoff_id: 20260515-22
queue_id: 22
title: Death Penalty — TechDesign Spec
type: requirement-to-techdesign
status: Ready
owner: techdesign
source_design_doc: requirements/death-penalty.md
feature_doc: features/death-penalty.md
created_at: 2026-05-15
created_by: gamedesign
iteration: 1
response_to: null
supersedes: null
---

# Handoff: Death Penalty — TechDesign

## Summary

Death Penalty là hệ thống trừng phạt khi chết — gồm drop đồ, penalty thọ nguyên/lôi kiếp, buff clear, hồi sinh. Áp dụng đồng nhất cho mọi nguyên nhân chết. Hết thọ nguyên = xóa nhân vật vĩnh viễn.

Requirement doc đầy đủ tại: `requirements/death-penalty.md`

---

## TechDesign cần làm

### 1. Verify runtime existence
Confirm từng thành phần đã có hay chưa:

| Thành phần | Cần verify |
|---|---|
| Death event hook | Đã có death event server-side chưa? |
| Drop logic khi chết | Đã có roll + spawn ground item chưa? |
| Buff clear on death | Buff skill đã bị clear khi chết chưa? |
| Lifespan penalty | Đã hook trừ thọ nguyên vào death event chưa? |
| Tribulation countdown penalty | Đã hook rút ngắn countdown vào death event chưa? |
| Respawn/revive flow | Popup chọn hồi sinh đã có chưa? |
| Permanent death trigger | Đã handle thọ nguyên = 0 chưa? |

### 2. Tech spec các điểm cần xác định

| Điểm | Câu hỏi |
|---|---|
| Drop pool | Droppable flag lưu ở đâu trong item schema? |
| Drop atomicity | Drop linh thạch + item có cùng 1 transaction không? |
| Ground item | Ground item entity tồn tại thế nào — DB hay in-memory? Looting window sync với shared-rules? |
| Permanent death | Xóa hẳn hay archive? Tên nhân vật có reclaim được không? |
| Additional penalty | Config table cho penalty bổ sung per death context — schema thế nào? |
| Buff clear scope | Buff từ bùa chú/trận pháp lưu riêng với buff từ skill thế nào? |
| Lifespan thọ nguyên cap | Khi uống đan dược tăng thọ nguyên — có cap tại pool cảnh giới không? (Cần raise lại GameDesign nếu chưa chốt) |

### 3. Blocking questions cần raise lại GameDesign
*(Nếu TechDesign cần answer để spec — hỏi qua group hoặc trực tiếp user)*

1. **Thọ nguyên max khi uống đan dược**: cap tại pool cảnh giới hay không cap?
2. **Warning threshold**: bao nhiêu % hoặc giây?
3. **Permanent death data**: xóa hay archive? Tên có reclaim không?

### 4. Output
- `tech-design/death-penalty.md` — death event flow, drop schema, buff scope, lifespan penalty hook, permanent death handling, respawn flow, config table.

---

## Key design rules (không thay đổi)

- Baseline penalty áp dụng **mọi** loại chết — không exception.
- **Không mất tu vi, không mất tiềm năng.**
- Rớt tối đa **1 item** per lần chết — random từ pool droppable.
- **Buff bùa chú/trận pháp không bị clear** — chỉ buff skill bị clear.
- Thọ nguyên / Lôi Kiếp countdown **floor tại 0** — không âm.
- Permanent death **không thể hoàn tác**.
- Additional penalty hoàn toàn **config-driven** — không hardcode per context trong code.
- Priority window dùng chung config với Ownership/Drop Rights shared rule — **1 config source duy nhất**.

---

## Acceptance Gate (TechDesign verify trước khi Dev handoff)

- [ ] Runtime existence confirmed cho từng thành phần.
- [ ] Drop pool + atomicity approach decided.
- [ ] Ground item / looting window approach decided.
- [ ] Buff scope (skill vs bùa chú) confirmed in code.
- [ ] Permanent death approach decided.
- [ ] Additional penalty config schema designed.
- [ ] 3 blocking questions raised / answered.
- [ ] `tech-design/death-penalty.md` created.
