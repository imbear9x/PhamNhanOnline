---
handoff_id: 20260515-20
queue_id: 20
title: Herb Farming — Drop Reject Fix (Remove Inbox Fallback for Quái Drop)
type: design-change
status: Ready
owner: techdesign
source_design_doc: requirements/herb-farming-system.md
feature_doc: features/herb-farming-system.md
created_at: 2026-05-15
created_by: gamedesign
iteration: 1
response_to: null
supersedes: null
---

# Handoff: Herb Farming — Quái Drop Full Reject (No Inbox Fallback)

## Summary

Design change: herb drops from quái khi inventory full **không còn redirect vào inbox**. Thay vào đó, drop bị **reject hoàn toàn** và server phải **notify client túi đầy**.

Đây là breaking change so với spec cũ (REQ-022, AC-011 trong phiên bản trước của requirement doc).

---

## Canonical Rule (đã chốt bởi GameDesign)

> **Bất kỳ action nào liên quan đến herb đều reject hoàn toàn nếu inventory full — không có inbox fallback ở bất kỳ đâu:**
> - Harvest từ plot → reject, herb vẫn trong plot
> - Extract herb item trong túi → reject, herb item vẫn trong túi
> - **Drop linh thảo từ quái → reject hoàn toàn, item không được cấp, không vào inbox**
>
> Client phải nhận notification inventory-full trong tất cả 3 trường hợp.

---

## Thay đổi so với spec cũ

| Điểm | Spec cũ | Spec mới (canonical) |
|---|---|---|
| Quái drop herb, túi đầy | Redirect vào inbox per shared overflow rule | Reject hoàn toàn, không redirect, notify client |
| REQ-022 | Herb drop → inbox fallback | Herb drop → reject + inventory-full error |
| AC-011 | Herb drop → inbox | Herb drop → rejected, client notified |
| Scope | "Inbox fallback when herb drop cannot enter inventory" | Removed |
| Related docs | Linked `features/inbox-mail-system.md` | Removed link |

---

## TechDesign cần làm

1. **Xác nhận** trong code hiện tại: `HerbService` hoặc loot handler có path nào redirect herb drop vào inbox không — nếu có, phải remove/disable.
2. **Spec server behavior**: khi loot resolution thấy herb không fit inventory, return error code `INVENTORY_FULL` (hoặc tương đương) — không gọi inbox grant path.
3. **Spec client notification**: server gửi packet notify túi đầy về client sau khi reject drop. Client hiển thị thông báo phù hợp.
4. **Verify**: không có nhánh code nào còn gọi inbox fallback cho herb drop.
5. **Update tech-design/herb-farming-system.md** nếu đã có — propagate change này vào spec kỹ thuật.

---

## Files đã cập nhật (GameDesign)

- `requirements/herb-farming-system.md` — REQ-022, AC-011, Rules and Invariants, Edge Cases, Scope, Related Systems, Known Conflicts/Drift đã update
- `features/herb-farming-system.md` — Edge Cases line 132, Related Systems line 153 đã update

---

## Không cần làm (out of scope handoff này)

- Không thay đổi harvest reject logic (đã đúng spec từ trước)
- Không thay đổi extract reject logic (đã đúng spec từ trước)
- Không thay đổi inbox system
- Balance, drop rate config — data design, không phải tech spec

---

## Acceptance (TechDesign verify xong thì mark Done)

- [ ] Confirm code path herb-drop-to-inbox đã không tồn tại hoặc đã remove.
- [ ] Spec server loot handler: reject + return INVENTORY_FULL khi herb không fit.
- [ ] Spec client packet: notify inventory-full sau reject.
- [ ] `tech-design/herb-farming-system.md` updated nếu có.
