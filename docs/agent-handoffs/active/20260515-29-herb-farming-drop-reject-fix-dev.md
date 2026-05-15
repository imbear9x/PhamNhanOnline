---
title: Dev Follow-up — Herb Farming Drop Reject Fix (Post-QA Authority Correction)
doc_type: handoff
status: Done
owner: dev
source_agent: techdesign
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: server-fix
queue_id: 29
feature_key: herb-farming-system
handoff_type: dev-fix
source_handoff: docs/agent-handoffs/active/20260515-20-herb-farming-drop-reject-fix-techdesign.md
response_to: docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md
supersedes: docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md
iteration: 1
---

# Context

Đây là **post-QA authority correction** cho herb farming release candidate đã pass QA tại `#25`.

- `#25` hiện **Blocked**, không được release theo authority cũ nữa.
- Mọi behavior herb farming đã pass QA trước đó **được giữ nguyên làm baseline**.
- Fix round này **chỉ sửa scope authority drift mới được GameDesign chốt**:
  - **Herb drop từ quái khi đầy túi phải reject hoàn toàn**
  - **Không inbox fallback**
  - **Client/server phải có tín hiệu inventory-full phù hợp**

## Baseline preserved (không được làm regression)

Các behavior dưới đây đã pass QA và phải giữ nguyên:
1. `HarvestAsync` là 2-step plot → inventory herb
2. `ExtractHerbAsync` grant outputs + delete herb
3. Harvest full bag → reject, herb vẫn ở plot
4. Extract full bag → reject, herb vẫn ở inventory
5. Herb inventory expiry + background sweep
6. `Young` stage runtime path
7. `required_herb_maturity` guard đã remove khỏi `AlchemyService`
8. Garden packet/handler wiring hiện có

---

# Canonical Rule To Implement

Theo `requirements/herb-farming-system.md` hiện tại:

> Nếu herb drop reward từ quái không fit inventory, thì **reject toàn bộ**.
> Không grant item, không redirect inbox, và client phải nhận inventory-full signal phù hợp.

Lưu ý:
- Rule này áp cho **herb-related action only**
- Không yêu cầu thay đổi shared inbox system
- Không yêu cầu đổi behavior reward khác ngoài herb path

---

# TechDesign Findings From Repo

## 1. Current runtime does NOT have inbox fallback path in enemy reward runtime

`GameServer/Runtime/EnemyRewardRuntimeService.cs` hiện chỉ có 2 delivery modes:
- `DirectGrant`
- `GroundDrop`

Không thấy inbox path ở runtime hiện tại.

## 2. Correction scope likely lands in herb-specific reward resolution / delivery decision

Vì code hiện không có inbox fallback sẵn ở enemy reward runtime, Dev cần audit:
- herb reward từ quái hiện đang đi bằng `DirectGrant` hay `GroundDrop`
- ở đâu đang cho phép herb reward bypass inventory-full rule riêng của herb
- nếu herb reward đang tạo `GroundDrop`, cần xác nhận canonical behavior mới là:
  - **không spawn herb reward đó** khi path đó được coi là herb grant cho player nhưng inventory full
  - return / notify inventory-full theo contract được chốt ở dưới

Nếu trong repo hiện tại **chưa có herb-specific enemy reward wiring**, Dev phải:
- xác nhận “không có code cần sửa” bằng evidence cụ thể
- nhưng vẫn phải align any TD/code comments/spec assumptions để không ai hiểu sai authority cũ nữa

---

# Required Dev Tasks

## Task 1 — Audit herb enemy-drop path authority

Xác nhận bằng code evidence:
1. Herb reward từ quái hiện đã implemented chưa?
2. Nếu đã implemented, path nào xử lý inventory fit / spawn / notify?
3. Có path nào còn hiểu theo authority cũ “full bag → inbox fallback” không?

## Task 2 — Implement reject-on-full for herb enemy-drop path

Nếu herb enemy-drop path tồn tại:
- Khi herb reward không fit inventory:
  - **reject toàn bộ herb reward đó**
  - **không** grant item
  - **không** route inbox
  - **không** tạo ground reward như workaround nếu path đó đáng lẽ là direct herb grant
  - gửi / surface inventory-full signal cho client theo packet/runtime contract sẵn có hoặc minimal extension được reviewer chấp nhận

Nếu herb enemy-drop path chưa tồn tại:
- không tự chế nửa vời
- ghi rõ evidence là correction hiện tại chỉ là authority/spec alignment; không có runtime delta cần sửa ở phần chưa tồn tại
- nhưng nếu có comment / TODO / stub hiểu sai authority cũ, sửa lại

## Task 3 — Preserve baseline

Không được làm regression các behavior đã pass ở `#25`, đặc biệt:
- `HarvestAsync`
- `ExtractHerbAsync`
- `GardenInventoryFull`
- expiry sweep
- packet handlers garden

---

# Implementation Guidance

## Preferred behavior

### Case A — herb enemy reward path exists and is direct-to-inventory
- precompute herb grants
- capacity check on full herb grant set
- fail atomically nếu không fit
- notify client inventory full
- no inbox

### Case B — herb enemy reward path exists and is mixed with other reward types
- herb reward entries phải follow herb authority rule riêng
- non-herb rewards giữ nguyên behavior cũ theo system authority riêng
- tránh làm broad behavior regression cho tất cả enemy rewards

### Case C — herb enemy reward path chưa implemented
- return code evidence + no-op runtime delta acceptable
- nhưng phải dọn drift assumptions để round này đóng authority gap sạch

---

# Expected Evidence From Dev

Dev handoff response cần nêu rõ một trong 2 verdict:

## Verdict 1 — Runtime fix applied
- file/path nào sửa
- herb enemy-drop full-bag giờ reject ở đâu
- inventory-full signal gửi thế nào
- vì sao không regression baseline #25

## Verdict 2 — No runtime delta needed yet
- herb enemy-drop runtime path thực tế chưa tồn tại
- authority drift chỉ nằm ở docs/spec/assumption
- file/comment nào đã align (nếu có)
- vì sao #25 vẫn phải giữ Blocked cho tới khi correction round này reviewer/QA xác nhận xong

---

# Reviewer / QA Scope To Expect Next

## Reviewer should verify
1. Dev có chứng minh đúng herb enemy-drop path hiện tồn tại / không tồn tại
2. Nếu tồn tại: reject-on-full đúng authority mới, không inbox
3. Không broad-break enemy reward systems khác
4. Baseline #25 preserved

## QA should retest minimum
1. herb enemy-drop full bag
2. harvest full bag non-regression
3. extract full bag non-regression
4. build pass

---

# Source Files To Inspect

- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GameServer/Runtime/EnemyDefinitionCatalog.cs`
- `GameServer/Entities/EnemyRewardRuleEntity.cs`
- `GameServer/Repositories/EnemyRewardRuleRepository.cs`
- `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
- `docs/tech-design/herb-farming-system.md`
- `docs/game-design-wp/requirements/herb-farming-system.md`
- `docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md`
- `docs/agent-handoffs/active/20260515-20-herb-farming-drop-reject-fix-techdesign.md`

---

# Notes

- Đây là **correction round merge vào release candidate cũ**, không phải feature độc lập.
- Release cuối phải dựa trên **baseline #25 + correction round này**.
- Nếu Dev thấy authority mới đòi hỏi packet/message mới mà current runtime không có chỗ surface, nêu rõ trong response để Reviewer/QA/User quyết định minimal acceptable contract.
