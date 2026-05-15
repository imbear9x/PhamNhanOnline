---
handoff_id: 20260515-23
queue_id: 23
title: Cultivation Penalty Refactor — TechDesign Spec
type: requirement-to-techdesign
status: Ready
owner: techdesign
source_design_doc: requirements/cultivation-penalty-refactor.md
feature_doc: features/cultivation-and-breakthrough.md
created_at: 2026-05-15
created_by: gamedesign
iteration: 1
response_to: null
supersedes: null
---

# Handoff: Cultivation Penalty Refactor — TechDesign

## Summary

Refactor penalty flow khi tụt cảnh giới: bỏ `potential_reward_locked`, thay bằng **potential revert deterministic** theo Cultivation Penalty Rule (shared-rules.md). Core cultivation loop không thay đổi — chỉ thay penalty handler.

Requirement doc đầy đủ tại: `requirements/cultivation-penalty-refactor.md`

---

## TechDesign cần làm

### 1. Verify `potential_reward_locked` dependencies
**Đây là việc đầu tiên và quan trọng nhất.**

- Flag `potential_reward_locked` hiện đang được dùng trong penalty flow.
- Cần verify: flag này có được dùng cho **reconnect / session retry / anti-abuse** flow nào không?
  - Nếu **không có dependency**: có thể remove hoàn toàn khỏi penalty flow.
  - Nếu **có dependency**: tách logic — giữ flag cho mục đích đó nhưng tách hoàn toàn khỏi penalty handler. Report lại GameDesign nếu cần adjust spec.

### 2. Verify DB schema
Cần confirm tồn tại trong `realm_templates`:
- `min_cultivation_threshold` per realm — ngưỡng tối thiểu để không tụt cảnh giới.
- `potential_reward` per realm — lượng potential cần revert khi tụt.

Nếu chưa có: cần thêm column, đây là prerequisite trước Dev handoff.

### 3. Quyết định cultivation mapping khi realm drop
Khi player tụt từ realm N → realm N-1, cultivation sau trừ cần được map sang realm N-1. Cần chốt công thức:
- Option A: giữ nguyên giá trị tuyệt đối (nếu cultivation mới vẫn hợp lệ trong realm N-1).
- Option B: map theo tỉ lệ max_cultivation (cultivation_mới / max_realm_N × max_realm_N-1).
- Option khác nếu có.

Raise lại GameDesign nếu cần confirm intent design.

### 4. Spec potential revert flow
- Deterministic sort: stat có `upgrade_count` cao nhất trước. Tiebreak: cần chọn 1 rule cố định (ví dụ: stat_id ascending).
- Edge case: `upgrade_count` = 0 cho tất cả stat, nhưng còn `unallocated_potential` → trừ thẳng.
- Edge case: potential cần revert > tổng potential đã dùng + unallocated → floor behavior (không âm).

### 5. Đảm bảo atomicity
Realm drop + potential revert + cultivation update phải trong cùng 1 DB transaction. Không partial state.

### 6. Output
- `tech-design/cultivation-penalty-refactor.md` — flow diagram, DB changes (nếu có), transaction scope, potential_reward_locked removal plan.

---

## Key design rules (không thay đổi)

- Core cultivation loop (accumulation, roll, breakthrough_attempts) **không thay đổi**.
- Potential revert là **deterministic** — không random, không player-choice.
- Realm drop + revert là **atomic** — 1 transaction.
- **Không bình cảnh** sau realm drop — không cooldown, không block flag.
- Rule áp dụng đồng nhất: đột phá thất bại và Lôi Kiếp thất bại dùng **cùng penalty handler** từ bước realm drop trở đi.

---

## Blocking Questions cần raise lại GameDesign nếu cần

1. Cultivation mapping formula khi realm drop — Option A hay B?
2. Potential revert khi player có ít potential hơn cần revert — floor ở đâu?

---

## Acceptance Gate

- [ ] `potential_reward_locked` dependency verified.
- [ ] DB schema confirmed (`min_cultivation_threshold`, `potential_reward`).
- [ ] Cultivation mapping formula decided.
- [ ] Potential revert tiebreak rule decided.
- [ ] Atomicity approach decided.
- [ ] `tech-design/cultivation-penalty-refactor.md` created.
