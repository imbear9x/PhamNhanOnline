---
title: Inventory Bag System — Fix HarvestHerb Random Output Capacity Contract
doc_type: handoff
status: Done
owner: dev
source_agent: techdesign
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: implementation
queue_id: 14
feature_key: inventory-bag-system
handoff_type: required-fix
source_handoff: docs/agent-handoffs/active/20260515-13-inventory-bag-system-qa-followup-fail.md
response_to: docs/agent-handoffs/active/20260515-13-inventory-bag-system-qa-followup-fail.md
iteration: 4
---

# Mục Tiêu

Sửa `HarvestHerbAsync` để capacity check bao phủ **toàn bộ output thực tế sẽ được granted** (sau khi roll), không chỉ guaranteed subset. Đây là required fix để đạt contract active-action reject của TechDesign spec.

# TechDesign Verdict

**Loại A — Spec đã đủ, fix direction rõ.**

Spec hiện tại tại `docs/tech-design/inventory-bag-system.md` đã ghi rõ:
- `capacity must be checked before active actions mutate state`
- `Active action grant: if not fit: abort action with InventoryFull`

Defect là implementation chưa follow đúng spec: chỉ pre-check `guaranteedOutputs` thay vì full proc set.

Spec đã được **bổ sung thêm section "Random output handling in active actions"** để hướng dẫn rõ contract cho case này. Dev phải đọc và implement theo section đó.

# Bối Cảnh

- QA report #13 xác nhận `HarvestHerbAsync` chỉ pre-check capacity cho `lockedOutputs.Where(x => x.OutputChance >= 1d)`.
- Sau đó vẫn loop toàn bộ `lockedOutputs`, gọi `_itemService.AddItemAsync(...)` cho từng output proc — không có capacity guard.
- `AddItemAsync` không tự enforce bag capacity.
- Kết quả: herb harvest là active action nhưng có đường dẫn overflow slot sau pre-check.

# Contract Phải Implement

Theo spec section **"Random output handling in active actions"** trong `docs/tech-design/inventory-bag-system.md`:

1. **Roll all outputs trước** — trong cùng inventory lock/transaction, roll random cho toàn bộ `lockedOutputs`. Kết quả là `procOutputs`: tập output thực tế sẽ được granted.
2. **Check capacity trên full `procOutputs`** — gọi `CheckCapacityForAsync(playerId, procOutputs)` trên toàn bộ `procOutputs`, không chỉ guaranteed subset.
3. **Nếu không fit → reject entirely** — throw `GameException(MessageCode.InventoryFull)`. Không grant bất kỳ item nào, không xóa herb, không xóa plot link. Transaction rollback toàn bộ.
4. **Nếu fit → grant toàn bộ `procOutputs`** — gọi `AddItemAsync` cho từng item trong `procOutputs`, sau đó mới clear plot link và delete herb.
5. **Nếu `procOutputs` rỗng (0 proc)** — không có item cần grant, vẫn xóa herb/plot bình thường (hành động thành công, không có gì để overflow).

# File Cần Sửa

- `GameServer/Services/HerbService.cs`
  - method `HarvestHerbAsync(...)`
  - section trong `_inventoryTransactions.ExecuteAsync(...)` callback

Không cần thêm file mới. Không cần thay đổi schema/DB.

# Scope

## Phải làm
- Sửa logic trong `HarvestHerbAsync`: roll all outputs trước → check capacity toàn bộ proc set → grant hoặc reject.
- Đảm bảo toàn bộ nằm trong cùng `_inventoryTransactions.ExecuteAsync(...)` (đã có sẵn).
- Build pass, 0 error, 0 warning.

## Không làm
- Không sửa `AddItemAsync` để tự enforce capacity (không phải scope fix này).
- Không sửa `PickupGroundRewardHandler` (đã pass QA).
- Không thay đổi schema hoặc packets.
- Không thay đổi behavior của passive reward path.

# Pseudo Code Tham Khảo

```csharp
// Trong _inventoryTransactions.ExecuteAsync callback:

// 1. Reload + materialize (đã có)
var lockedHerb = await RequireOwnedHerbAsync(playerId, playerHerbId, ct);
lockedHerb = await MaterializeHerbProgressAsync(lockedHerb, ct);
var lockedOutputs = ResolveHarvestOutputs(lockedHerbDefinition, ...);

// 2. Roll ALL outputs trước — materialize proc set
var procOutputs = lockedOutputs
    .Where(output => _randomService.CheckChance(ToPartsPerMillion(output.OutputChance)).Success)
    .Select(output => new ItemGrantRequest(output.ResultItemTemplateId, output.ResultQuantity, false, null))
    .ToArray();

// 3. Check capacity trên full proc set (kể cả rỗng)
if (procOutputs.Length > 0)
{
    var capacityCheck = await _bagService.CheckCapacityForAsync(playerId, procOutputs, ct);
    if (!capacityCheck.CanFit)
        throw new GameException(MessageCode.InventoryFull);
}

// 4. Grant toàn bộ proc outputs
foreach (var grant in procOutputs)
{
    var createdItems = await _itemService.AddItemAsync(
        playerId, grant.ItemTemplateId, grant.Quantity, grant.IsBound, grant.ExpireAtUtc, ct);
    created.AddRange(createdItems);
}

// 5. Clear plot + delete herb (đã có, không thay đổi)
```

> **Lưu ý:** Pseudo code trên là định hướng logic. Dev có thể điều chỉnh chi tiết triển khai miễn đáp ứng đúng contract spec (roll trước → check full set → grant hoặc reject entirely).

# Acceptance Criteria

- [ ] `HarvestHerb` với bag full và ít nhất 1 random output proc → action reject `InventoryFull`, herb không bị xóa, plot không bị clear, không có item nào được grant.
- [ ] `HarvestHerb` với bag đủ chỗ và random outputs proc → toàn bộ proc outputs được granted, herb bị xóa, plot bị clear.
- [ ] `HarvestHerb` với 0 output proc (tất cả fail roll) → action thành công, herb bị xóa, plot bị clear, không có item grant.
- [ ] `HarvestHerb` với bag full và 0 output proc → action thành công (không có item cần grant nên không overflow), herb bị xóa, plot bị clear.
- [ ] `PickupGroundReward` full bag vẫn pass (không bị regression).
- [ ] `dotnet build GameServer/GameServer.csproj -v minimal` pass, 0 error, 0 warning.

# Retest Scope Cho QA

Sau khi Dev sửa và Reviewer pass:

1. `HarvestHerb` full bag với random outputs nhiều proc → reject `InventoryFull`, herb còn nguyên, plot còn nguyên.
2. `HarvestHerb` full bag với 0 proc → thành công, herb/plot bị xóa đúng.
3. `HarvestHerb` bag đủ chỗ với random outputs → grant đúng, herb/plot xóa đúng.
4. `PickupGroundReward` full bag → vẫn fail atomically, claim còn nguyên (không regression).
5. Build pass.

# Source Chain

- QA fail report: `docs/agent-handoffs/active/20260515-13-inventory-bag-system-qa-followup-fail.md`
- TechDesign spec (updated): `docs/tech-design/inventory-bag-system.md` — section "Random output handling in active actions"
- Reviewer TOCTOU fix context: `docs/agent-handoffs/active/20260515-reviewer-bag-capacity-precheck-race-response.md`

# Lifecycle Update Required On Completion

1. Khi bắt đầu: cập nhật queue row #14 từ `Ready` → `In Progress`.
2. Khi xong: tạo handoff `reviewer` với implementation summary, files touched, build result, và retest scope cho QA.
3. Cập nhật queue row #14 → `Done`.
4. Thêm queue row mới cho reviewer handoff với `Owner = reviewer`, `Status = Ready`.
