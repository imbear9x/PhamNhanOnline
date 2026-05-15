---
title: Reviewer Handoff — Herb Farming Drop Reject Fix
doc_type: handoff
status: Done
owner: reviewer
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: review
queue_id: 31
feature_key: herb-farming-system
handoff_type: review
source_handoff: docs/agent-handoffs/active/20260515-29-herb-farming-drop-reject-fix-dev.md
response_to: docs/agent-handoffs/active/20260515-29-herb-farming-drop-reject-fix-dev.md
supersedes: docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md
iteration: 1
---

# Goal

Reviewer xác minh correction round `#29` đã áp đúng authority mới cho **herb drop từ quái khi đầy túi** mà không làm regression baseline herb farming đã pass QA trước đó.

# Dev Verdict

**Verdict 1 — Runtime fix applied**

Audit cho thấy repo hiện **có runtime herb enemy-drop path thật** qua `EnemyRewardRuntimeService`:
- enemy reward rules có `DeliveryType.DirectGrant`
- direct-grant path trước khi sửa gọi thẳng `ItemService.AddItemAsync(...)` qua `GrantDirectRewardItemsAsync(...)`
- path này **không có pre-check bag capacity riêng cho herb reward** và cũng **không surface inventory-full signal** cho client
- runtime hiện **không có inbox fallback path** trong enemy reward service; correction scope là chặn herb direct-grant khi full bag và gửi signal full bag

# Implementation Summary

## Runtime fix đã áp

Sửa `GameServer/Runtime/EnemyRewardRuntimeService.cs`:

1. Inject `INetworkSender` để runtime có thể surface inventory-full signal cho player online.
2. Resolve `BagService` trong reward processing scope.
3. Trước khi grant `DirectGrant` items:
   - lọc riêng `herbDirectGrantItems`
   - herb reward hiện được nhận diện bằng `ItemType.HerbSeed` hoặc `ItemType.HerbPlant`
4. Chạy `BagService.CheckCapacityForAsync(...)` trên **full herb grant set** của lượt direct-grant đó.
5. Nếu không fit:
   - gửi `PickupGroundRewardResultPacket { Success = false, Code = MessageCode.InventoryFull, RewardId = null }`
   - **remove toàn bộ herb reward khỏi `directGrantItems`**
   - không grant herb item, không spawn ground reward workaround, không inbox fallback
6. Nếu còn non-herb direct-grant items thì vẫn grant như cũ.

## Vì sao fix này bám authority mới

- `REQ-022` / `AC-011` yêu cầu herb drop từ quái full bag phải **reject entirely**
- runtime hiện không có inbox fallback, nên fix đúng là chặn grant ở direct path + notify `InventoryFull`
- correction handoff `#29` cũng yêu cầu không broad-break reward systems khác, nên chỉ lọc và reject **herb-related direct grants**, giữ nguyên non-herb rewards

# Files / Modules Touched

- `GameServer/Runtime/EnemyRewardRuntimeService.cs`

# Build / Test Result

Focused build đã chạy:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: **pass**, `0 error`

Observed warning còn lại:
- `CS8032` từ `Humanizer.Analyzers.NamespaceMigrationAnalyzer`
- thiếu `System.Collections.Immutable, Version=9.0.0.0`
- warning môi trường/analyzer, không phải blocker logic của correction này

# DB / Schema / Seed Changes

- Không có DB/schema/seed change ở correction round này.

# Packet / Broadcast / Runtime Changes

- Không thêm packet mới.
- Tái dùng packet có sẵn để surface inventory-full signal:
  - `PickupGroundRewardResultPacket`
  - `Code = MessageCode.InventoryFull`
  - `RewardId = null`
- Runtime behavior change chỉ nằm ở enemy reward `DirectGrant` herb path.

# Evidence / Audit Notes

## Path tồn tại thật

- `EnemyRewardRuntimeService.ProcessPendingEvents(...)` roll reward theo `EnemyRewardRuleDefinition`
- `RewardDeliveryType.DirectGrant` gom item vào `directGrantItems`
- `GrantDirectRewardItemsAsync(...)` nhóm item rồi gọi `ItemService.AddItemAsync(...)`
- trước fix không có herb-specific capacity gate ở path này

## Inbox fallback không tồn tại trong runtime hiện tại

- enemy reward runtime chỉ có 2 mode:
  - `GroundDrop`
  - `DirectGrant`
- không có inbox/mail route trong `EnemyRewardRuntimeService`

## Contract preservation

- `PickupGroundRewardHandler` ground reward pickup path vẫn giữ logic cũ, vẫn check inventory capacity atomically cho ground reward pickup
- correction này không sửa:
  - `HarvestAsync`
  - `ExtractHerbAsync`
  - garden handlers
  - herb expiry sweep
  - alchemy migration

# QA / Reviewer Retest Scope

## Reviewer nên verify

1. Herb enemy-drop runtime path đúng là tồn tại ở `DirectGrant`, không phải chỉ spec-only.
2. Herb item type filter (`HerbSeed` / `HerbPlant`) đúng với authority “mầm non/drop herb” và không ăn sang linh dược `HerbMaterial`.
3. Khi herb direct-grant không fit bag:
   - herb reward không được grant
   - không spawn ground reward fallback
   - không có inbox path
   - client nhận `MessageCode.InventoryFull`
4. Non-herb direct-grant rewards vẫn grant như cũ.
5. Build pass.

## QA tối thiểu nên retest

1. Full bag + enemy herb drop → không nhận herb item, client thấy inventory full.
2. Full bag + harvest herb → non-regression, herb vẫn ở plot.
3. Full bag + extract herb → non-regression, herb vẫn ở inventory.
4. Mixed enemy reward có herb + non-herb direct grant → herb bị reject theo rule mới, non-herb vẫn giữ baseline hiện tại.

# Known Gaps / Risks

- Runtime signal hiện dùng lại `PickupGroundRewardResultPacket` vì repo chưa có packet business-error chung cho enemy direct-grant path. Đây là lựa chọn **minimal extension-free** để reviewer quyết định có chấp nhận contract reuse hay cần follow-up packet chuẩn hóa.
- Fix hiện áp cho herb-related direct-grant items được map bằng `ItemType.HerbSeed` và `ItemType.HerbPlant`. Nếu tương lai design coi thêm item type khác là “herb drop từ quái” thì cần mở rộng filter theo authority mới.
- Mixed direct-grant rule hiện giữ non-herb rewards nguyên trạng để tránh broad regression; correction này không biến toàn bộ reward set thành atomic-all-or-nothing.
