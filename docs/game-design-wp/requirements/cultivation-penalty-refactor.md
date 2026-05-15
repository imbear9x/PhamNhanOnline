---
doc_type: game_design_requirement
system_id: cultivation-penalty-refactor
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/cultivation-and-breakthrough.md
related_docs:
  - features/cultivation-and-breakthrough.md
  - features/tribulation-system.md
  - features/death-penalty.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: true
---

# Cultivation Penalty Refactor — Requirement Spec

## Goal

Refactor cơ chế penalty khi tụt cảnh giới từ `potential_reward_locked` (thiết kế cũ, không còn canonical) sang **Cultivation Penalty Rule** đã chốt trong `shared-rules.md`: trừ tu vi → tụt cảnh giới → potential revert deterministic.

**Scope của requirement này**: chỉ phần penalty khi tụt cảnh giới. Core cultivation loop (tích lũy, đột phá roll, breakthrough_attempts) đã implement và không thay đổi.

## Source Design Summary

Canonical penalty rule lives in `shared-rules.md` — Cultivation Penalty (Realm Drop + Potential Revert).
Grounding feature: `features/cultivation-and-breakthrough.md`.

## Target Design Summary

Khi đột phá cảnh giới thất bại và cultivation tụt dưới ngưỡng cảnh giới hiện tại, server phải:
1. Tụt player về cảnh giới dưới với cultivation tương ứng (không reset về 0/max).
2. Tính lượng potential cần revert theo cảnh giới bị mất.
3. Đảo ngược stat upgrades theo thứ tự deterministic (stat nâng nhiều nhất trước) cho đến khi đủ potential revert.
4. Không dùng `potential_reward_locked` flag trong bất kỳ bước nào của flow trên.

Behavior phải nhất quán cho **tất cả** nguồn gây tụt cảnh giới: đột phá thất bại, Lôi Kiếp thất bại (khi implement), và các penalty tương lai.

## Current Runtime / Evidence Snapshot

- **Confirmed**: `potential_reward_locked` flag đang được dùng trong code để handle penalty khi breakthrough thất bại.
- **Confirmed**: `upgrade_count` per stat lưu số lần nâng — đảo ngược là deterministic có thể implement.
- **Confirmed**: `unallocated_potential` field lưu potential chưa dùng.
- **Confirmed**: `potential_stat_upgrade_tiers` — tier cost tăng dần per stat.
- **Not confirmed**: `potential_reward_locked` hiện có được dùng cho reconnect / retry / anti-abuse flow không — TechDesign phải verify trước khi remove.
- **Not confirmed**: realm drop flow (tụt cảnh giới khi cultivation âm) đã implement chưa hay chỉ có breakthrough failure penalty trừ cultivation flat.

## Scope

### Must Implement
- Cultivation penalty flow: trừ cultivation (%) → check ngưỡng cảnh giới → tụt cảnh giới nếu cần
- Realm drop: đặt player về cảnh giới dưới, cultivation = giá trị sau khi trừ (mapped sang cảnh giới mới)
- Potential revert: tính potential cần hoàn trả per cảnh giới bị mất → đảo ngược stat upgrades theo thứ tự (nâng nhiều nhất trước) cho đến đủ lượng revert
- Không bình cảnh: sau khi tụt, không block đột phá lại
- Remove / deprecate `potential_reward_locked` flag khỏi penalty flow (sau khi verify không có dependency khác)

### Must Not Implement
- Thay đổi breakthrough roll logic — giữ nguyên
- Thay đổi cultivation accumulation — giữ nguyên
- Thay đổi potential allocation UI/flow — chỉ thay phần revert
- Penalty Lôi Kiếp thất bại — cùng rule nhưng trigger từ tribulation system, scope riêng
- Balance cụ thể (% cultivation trừ, potential per realm) — data config, không hardcode

## Terminology

- `cultivation penalty`: trừ cultivation (%) khi đột phá thất bại hoặc penalty tương tự.
- `realm drop`: tụt 1 cảnh giới khi cultivation sau khi trừ dưới ngưỡng tối thiểu cảnh giới hiện tại.
- `potential revert`: hoàn trả potential về pool `unallocated_potential` bằng cách đảo ngược stat upgrades.
- `upgrade_count`: số lần nâng per stat — lưu trong DB, dùng để tính reverse deterministic.
- `potential_reward_locked`: flag cũ — không còn là cơ chế canonical, cần remove khỏi penalty flow.

## Functional Requirements

### Cultivation Penalty
- `REQ-001`: Khi đột phá cảnh giới thất bại, server phải trừ cultivation của player theo % config per martial art stage (`breakthrough_exp_penalty`). Kết quả cultivation không thể âm — floor tại 0.
- `REQ-002`: Sau khi trừ cultivation, nếu cultivation hiện tại < ngưỡng tối thiểu của cảnh giới hiện tại, server phải trigger realm drop.
- `REQ-003`: Nếu cultivation sau khi trừ ≥ ngưỡng tối thiểu, không có realm drop — player ở nguyên cảnh giới, có thể thử đột phá lại ngay.

### Realm Drop
- `REQ-004`: Khi realm drop xảy ra, player bị đặt về cảnh giới ngay dưới (realm - 1).
- `REQ-005`: Cultivation sau realm drop = giá trị cultivation đã trừ, mapped sang scale của cảnh giới mới (không reset về 0, không reset về max cảnh giới mới).
- `REQ-006`: Không có bình cảnh — player có thể đột phá lại ngay khi đủ cultivation ở cảnh giới mới, không có cooldown hay block flag.
- `REQ-007`: Realm drop phải trigger potential revert ngay trong cùng transaction.

### Potential Revert
- `REQ-008`: Khi realm drop xảy ra, server phải tính lượng potential cần revert tương ứng với potential reward của cảnh giới bị mất (config per realm).
- `REQ-009`: Server đảo ngược stat upgrades theo thứ tự: **stat có `upgrade_count` cao nhất trước**. Trong trường hợp tie: thứ tự deterministic theo stat id hoặc enum order (TechDesign quyết định tiebreak cụ thể).
- `REQ-010`: Mỗi bước đảo ngược: trừ 1 lần nâng của stat đó (giảm chỉ số tương ứng, hoàn trả tier cost về `unallocated_potential`), giảm `upgrade_count` stat đó đi 1.
- `REQ-011`: Lặp lại REQ-010 cho đến khi tổng potential hoàn trả đủ lượng cần revert.
- `REQ-012`: Nếu player chưa dùng potential (toàn bộ là `unallocated_potential`): trừ thẳng từ `unallocated_potential`, không cần đảo ngược stat.
- `REQ-013`: Potential revert không thể khiến `unallocated_potential` âm và không thể khiến stat xuống dưới base value của cảnh giới mới — floor tại base nếu cần.

### Deprecate potential_reward_locked
- `REQ-014`: `potential_reward_locked` flag không được dùng trong penalty flow sau refactor. TechDesign phải verify flag này không được dùng cho reconnect / retry / anti-abuse trước khi remove. Nếu có dependency: tách riêng, giữ flag cho mục đích đó nhưng tách khỏi penalty logic.
- `REQ-015`: Sau refactor, penalty flow phải hoạt động đúng mà không cần check hay set `potential_reward_locked`.

## Acceptance Criteria

- `AC-001`: Given player đột phá thất bại, cultivation sau khi trừ ≥ ngưỡng tối thiểu cảnh giới hiện tại, when penalty resolves, then player ở nguyên cảnh giới, cultivation giảm đúng %, không realm drop, không potential revert.
- `AC-002`: Given player đột phá thất bại, cultivation sau khi trừ < ngưỡng tối thiểu, when penalty resolves, then player tụt 1 cảnh giới, cultivation mapped sang giá trị tương ứng ở cảnh giới mới.
- `AC-003`: Given realm drop xảy ra, player có 10 unallocated potential và đã nâng 5 điểm stat A (upgrade_count=5), khi potential revert cần 3 điểm, then server đảo ngược 3 lần nâng từ stat A, stat A giảm tương ứng, unallocated_potential tăng đúng tier cost của 3 lần đó.
- `AC-004`: Given realm drop xảy ra, player chưa nâng bất kỳ stat nào (toàn bộ là unallocated_potential), when potential revert, then server trừ thẳng từ unallocated_potential — không đảo ngược stat nào.
- `AC-005`: Given realm drop xảy ra, player có nhiều stat đã nâng, when potential revert, then stat có upgrade_count cao nhất bị đảo ngược trước.
- `AC-006`: Given player vừa tụt cảnh giới do đột phá thất bại, when player đủ cultivation ở cảnh giới mới, then player có thể thực hiện đột phá lại ngay — không bị block bởi cooldown hay flag.
- `AC-007`: Given refactor hoàn thành, when đột phá thất bại trigger penalty, then `potential_reward_locked` không được set hay check trong flow đó.
- `AC-008`: Given cultivation sau trừ = 0 (floor), when penalty resolves, then cultivation = 0, không âm; realm drop vẫn check ngưỡng bình thường.

## Runtime Flow

### Đột Phá Thất Bại — Penalty Flow (sau refactor)
1. Breakthrough roll fail.
2. Server lấy `breakthrough_exp_penalty` % từ martial art stage config.
3. Tính cultivation mới = max(0, cultivation_hiện_tại - cultivation_hiện_tại × penalty%).
4. So sánh cultivation mới với `min_cultivation_threshold` của cảnh giới hiện tại.
5. **Nếu cultivation mới ≥ threshold**: cập nhật cultivation, kết thúc. Không realm drop.
6. **Nếu cultivation mới < threshold**: trigger realm drop.
   - Đặt player về realm - 1.
   - Map cultivation mới sang cảnh giới mới.
   - Tính `potential_to_revert` = potential reward của cảnh giới vừa mất (config per realm).
   - Chạy potential revert loop (REQ-008 → REQ-013).
   - Cập nhật tất cả fields trong cùng 1 transaction.
7. Ghi `breakthrough_attempts` log.
8. Notify client: kết quả penalty, realm mới (nếu có), cultivation mới, stat thay đổi (nếu có).

## Rules And Invariants

- Cultivation không âm — floor tại 0.
- Potential revert là deterministic — cùng state cho ra cùng kết quả, không random.
- Realm drop và potential revert là atomic — cùng 1 transaction, không partial.
- Không bình cảnh sau realm drop — không cooldown, không flag block.
- `potential_reward_locked` không được dùng trong penalty flow sau refactor.
- Rule này áp dụng đồng nhất cho mọi nguồn realm drop — đột phá thất bại và Lôi Kiếp thất bại (khi implement) dùng cùng flow từ bước realm drop trở đi.

## Data / Config Requirements

| Config | Notes |
|---|---|
| `breakthrough_exp_penalty` | % cultivation trừ per martial art stage — đã có trong DB |
| `realm_templates.min_cultivation_threshold` | Ngưỡng tối thiểu cultivation per realm — cần verify có trong DB không |
| `realm_templates.potential_reward` | Potential reward per realm để tính revert amount — cần verify |
| `potential_stat_upgrade_tiers` | Tier cost per nâng per stat — đã có |
| `upgrade_count` per stat per player | Đã có trong DB |

## Blocking Questions (cần chốt trước Dev handoff)

1. **`potential_reward_locked` dependency**: flag này có đang được dùng cho reconnect / retry / anti-abuse flow không? TechDesign cần verify và report trước khi remove. Nếu có: cần tách logic, giữ flag cho mục đích riêng.
2. **`min_cultivation_threshold` per realm**: đã có trong `realm_templates` DB chưa? Hay cần thêm column mới?
3. **`potential_reward` per realm**: đã có trong `realm_templates` chưa? Hay đang implicit (ví dụ: cố định per realm tier)?
4. **Cultivation mapping khi realm drop**: cultivation sau trừ map sang cảnh giới mới theo công thức nào? (ví dụ: giữ nguyên giá trị tuyệt đối, hay map theo tỉ lệ max cultivation giữa 2 cảnh giới?) — cần chốt ở TechDesign layer.

## Known Conflicts / Drift

- `potential_reward_locked` là cơ chế cũ đang chạy trong code — không canonical. Phải remove khỏi penalty flow nhưng cần verify dependency trước.
- `breakthrough_conditions` table tồn tại trong DB nhưng runtime chưa đọc — out of scope refactor này, không đụng vào.
- Core cultivation loop (accumulation, roll, attempts log) không thay đổi — chỉ thay penalty handler.

## Readiness Level

- Ready for TechDesign refinement: **yes**
- Ready for Dev handoff: **pending** — 4 blocking questions cần TechDesign verify và raise lại nếu cần
- Ready for QA: **no** — chờ implementation

## Handoff Checklist

- [x] Scope rõ ràng — chỉ penalty flow, không đụng core cultivation.
- [x] Acceptance criteria testable.
- [x] Config/data impacts listed.
- [x] Blocking questions explicit.
- [x] Shared rule referenced (shared-rules.md — Cultivation Penalty).
- [x] `handoff_ready: true`
