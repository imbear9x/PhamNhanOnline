---
title: Client Dev — Herb Farming + Inventory Bag System (Unity)
doc_type: handoff
status: Ready
owner: dev-client
source_agent: techdesign
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: unity-client-implementation
queue_id: 18
feature_key: inventory-bag-system, herb-farming-system
handoff_type: client-dev
source_handoff: docs/agent-handoffs/active/20260515-17-inventory-bag-herb-random-output-fix-qa-report.md
response_to: docs/agent-handoffs/active/20260515-17-inventory-bag-herb-random-output-fix-qa-report.md
iteration: 1
---

# Tổng quan

Server-side đã pass QA hoàn toàn cho 2 system:
1. **Inventory Bag System** — bag grade 1–4, capacity enforcement, bag upgrade via NPC
2. **Herb Farming System** — trồng/thu hoạch/extract linh thảo, random output contract, ground reward pickup

Client Unity cần implement network layer + UI flow cho cả 2 system theo spec dưới đây.

> Pull code mới nhất từ server branch trước khi bắt đầu.

---

# Source Specs cần đọc

| Tài liệu | Mục đích |
|---|---|
| `docs/tech-design/inventory-bag-system.md` | Full server spec: bag grade config, slot logic, upgrade flow, capacity enforcement |
| `docs/tech-design/herb-farming-system.md` | Full server spec: herb lifecycle, plot states, harvest/extract flow, output contract |
| `GameShared/Packets/Packets/CharacterPackets.cs` | Inventory + bag packet definitions (packet IDs, fields) |
| `GameShared/Packets/Packets/WorldPackets.cs` | Ground reward packet definitions |
| `GameShared/Models/BagStateModel.cs` | `{ Grade, UsedSlots, TotalSlots, DisplayName }` |
| `GameShared/Models/InventoryItemModel.cs` | Full item model fields |
| `GameShared/Models/GroundRewardModel.cs` | Ground reward model |
| `GameShared/Messages/MessageCode.cs` | Error codes |

---

# Phần 1 — Inventory Bag System

## 1.1 Packet Reference

| Packet | ID | Dir | Fields quan trọng |
|---|---|---|---|
| `GetInventoryPacket` | 58 | C→S | _(no fields)_ |
| `GetInventoryResultPacket` | 59 | S→C | `Success`, `Code`, `BagState`, `Items`, `EquipmentSlotCount` |
| `GetBagStatePacket` | 220 | C→S | _(no fields)_ |
| `GetBagStateResultPacket` | 221 | S→C | `Success`, `Code`, `BagState` |
| `UpgradeBagPacket` | 222 | C→S | `TargetGrade` (int, required, >= 1) |
| `UpgradeBagResultPacket` | 223 | S→C | `Success`, `Code`, `BagState`, `RemainingLinhThach`, `FailureReason` |
| `EquipInventoryItemPacket` | 60 | C→S | `PlayerItemId` |
| `EquipInventoryItemResultPacket` | 61 | S→C | `Success`, `Code`, `Items` |
| `UnequipInventoryItemPacket` | 62 | C→S | `PlayerItemId` (hoặc `EquippedSlot`) |
| `UnequipInventoryItemResultPacket` | 63 | S→C | `Success`, `Code`, `Items` |
| `DropInventoryItemPacket` | 64 | C→S | `PlayerItemId`, `Quantity` |
| `DropInventoryItemResultPacket` | 65 | S→C | `Success`, `Code`, `PlayerItemId` |

> Xác nhận packet ID chính xác bằng cách đọc `[Packet(N)]` attribute trong `CharacterPackets.cs`.

## 1.2 BagStateModel

```
BagStateModel {
    Grade: int          // 1–4
    UsedSlots: int      // số item stacks đang chiếm slot
    TotalSlots: int     // tổng slot theo grade hiện tại
    DisplayName: string // tên hiển thị UI, ví dụ "Túi vải", "Túi da"
}
```

## 1.3 UI Contract — Inventory Panel

- Hiển thị `UsedSlots / TotalSlots` rõ ràng (ví dụ: `12/20`)
- Hiển thị `DisplayName` của bag grade hiện tại
- Khi `UsedSlots >= TotalSlots`: hiển thị trạng thái "Đầy túi" (visual cue)
- Item list lấy từ `GetInventoryResultPacket.Items`
- Mỗi `InventoryItemModel` có đủ field để render icon, tên, số lượng, rarity, bound/tradeable state

## 1.4 UI Contract — Bag Upgrade (NPC Shop)

- Gửi `UpgradeBagPacket { TargetGrade = currentGrade + 1 }` khi player xác nhận mua
- Khi nhận `UpgradeBagResultPacket`:
  - `Success = true`: cập nhật UI bag state, hiển thị `RemainingLinhThach`
  - `Success = false`:
    - `Code = BagUpgradeTargetInvalid (3060)`: grade target không hợp lệ (đã max hoặc không đúng thứ tự)
    - `Code = BagUpgradeCurrencyInsufficient (3061)`: không đủ linh thạch → hiển thị thông báo thiếu tiền

## 1.5 Error Codes — Inventory/Bag

| Code | Value | Hiển thị gợi ý |
|---|---|---|
| `InventoryFull` | 3059 | "Túi đồ đầy" — block action, không partial grant |
| `BagUpgradeTargetInvalid` | 3060 | "Không thể nâng cấp túi" |
| `BagUpgradeCurrencyInsufficient` | 3061 | "Không đủ linh thạch" |

---

# Phần 2 — Herb Farming System

## 2.1 Packet Reference

> Herb packets nằm trong file `GameShared/Packets/Packets/HerbPackets.cs` (file mới, IDs 200–220).
> Dev-client cần đọc file này để lấy đúng packet ID và field definitions.

Các action cần implement (tên packet theo convention `[Action]HerbPacket` / `[Action]HerbResultPacket`):

| Action | C→S Packet | S→C Result |
|---|---|---|
| Xem vườn (plot list) | `GetGardenPacket` | `GetGardenResultPacket` |
| Tra linh thổ | `InsertSoilPacket` | `InsertSoilResultPacket` |
| Trồng hạt giống | `PlantSeedPacket` | `PlantSeedResultPacket` |
| Trồng mầm non có sẵn | `PlantExistingHerbPacket` | `PlantExistingHerbResultPacket` |
| Thu hoạch (plot → inventory herb entity) | `HarvestHerbPacket` | `HarvestHerbResultPacket` |
| Chiết xuất (inventory herb → linh dược items) | `ExtractHerbPacket` | `ExtractHerbResultPacket` |

> Xác nhận tên và ID chính xác từ `HerbPackets.cs`. Nếu file chưa có, hỏi user trước khi implement.

## 2.2 Herb Lifecycle Flow (client phải follow)

```
[Plot trống]
    ↓ InsertSoil
[Plot có linh thổ, chưa có herb]
    ↓ PlantSeed hoặc PlantExistingHerb
[Herb đang lớn — Seedling / Young / Mature / ThousandYear]
    ↓ HarvestHerb (khi Mature hoặc ThousandYear)
[Herb entity trong inventory — có expire_at]
    ↓ ExtractHerb
[Nhận linh dược items + optional mầm non]
```

## 2.3 Plot State Display

| State | Hiển thị |
|---|---|
| Trống | Slot rỗng, có nút "Tra linh thổ" |
| Có linh thổ, chưa có herb | "Đất trống", có nút "Trồng" |
| Herb đang lớn | Hiển thị growth stage + progress |
| Mature / ThousandYear | Nổi bật "Có thể thu hoạch", nút "Thu hoạch" |
| Linh thổ hết hạn | Hiển thị cảnh báo, herb vẫn hiển thị nhưng dừng tăng trưởng |

## 2.4 HarvestHerb — Error Handling Contract

**Quan trọng:** Server enforce random output contract:
- Server roll toàn bộ output proc, check capacity trên full set
- Nếu túi đầy + có output → fail `InventoryFull` **trước** khi grant/clear plot
- Plot **không bị clear** khi fail → client **không được** xóa plot UI khi nhận lỗi này
- Nếu 0 proc output → harvest thành công, herb biến mất, không có item nào được grant

| Kết quả | Client phải làm |
|---|---|
| `Success = true`, có items | Cập nhật inventory, cập nhật plot về trống |
| `Success = false`, `Code = InventoryFull (3059)` | Hiển thị "Túi đầy, không thể thu hoạch" — **giữ nguyên plot state** |
| `Success = true`, không có items (0 proc) | Cập nhật plot về trống, không có item notification |

## 2.5 ExtractHerb — Error Handling Contract

- Tương tự HarvestHerb: nếu `InventoryFull` → hiển thị lỗi, herb entity vẫn còn trong inventory
- Herb entity trong inventory có `expire_at` → nên hiển thị thời gian còn lại nếu UX cho phép

---

# Phần 3 — Ground Reward (World Drop)

## 3.1 Packet Reference

| Packet | ID | Dir | Mô tả |
|---|---|---|---|
| `WorldRuntimeSnapshotPacket` | 47 | S→C | Khi vào map: snapshot toàn bộ `GroundRewards` hiện tại |
| `GroundRewardSpawnedPacket` | 51 | S→C | Broadcast: reward mới spawn trên map |
| `GroundRewardDespawnedPacket` | 52 | S→C | Broadcast: reward despawn (hết hạn hoặc ai đó pickup) |
| `PickupGroundRewardPacket` | 55 | C→S | Player request pickup theo `GroundRewardId` |
| `PickupGroundRewardResultPacket` | 56 | S→C | `Success`, `Code`, `GrantedItems` |

> Xác nhận ID trong `WorldPackets.cs`.

## 3.2 Ground Reward Flow

- Khi nhận `WorldRuntimeSnapshotPacket`: render toàn bộ `GroundRewards` trên map
- Khi nhận `GroundRewardSpawnedPacket`: thêm reward vào map runtime
- Khi nhận `GroundRewardDespawnedPacket`: xóa reward khỏi map (dù player chưa pickup)
- Khi player tap/click reward → gửi `PickupGroundRewardPacket { GroundRewardId }`

## 3.3 PickupGroundReward — Error Handling Contract

**Quan trọng:** Server enforce atomic reject:
- Nếu túi đầy → fail `InventoryFull`, reward claim **không bị consume**
- Client: không được xóa reward khỏi map khi nhận lỗi này

| Kết quả | Client phải làm |
|---|---|
| `Success = true` | Cập nhật inventory với `GrantedItems`, reward tự despawn qua broadcast |
| `Success = false`, `InventoryFull (3059)` | "Túi đầy" — reward **vẫn còn trên map** |
| `Success = false`, `GroundRewardOutOfRange (5010)` | "Quá xa" — giữ reward trên map |
| `Success = false`, `GroundRewardClaimInProgress (5011)` | Đang xử lý — không gửi lại, chờ hoặc ignore |
| `Success = false`, `GroundRewardExpired (5006)` | Reward hết hạn — xóa khỏi map UI |
| `Success = false`, các code khác | Hiển thị lỗi chung |

---

# Error Code Summary

| Code | Value | Tên | Context |
|---|---|---|---|
| `InventoryFull` | 3059 | Túi đầy | HarvestHerb, ExtractHerb, PickupGroundReward |
| `BagUpgradeTargetInvalid` | 3060 | Grade target không hợp lệ | UpgradeBag |
| `BagUpgradeCurrencyInsufficient` | 3061 | Không đủ linh thạch | UpgradeBag |
| `GroundRewardIdInvalid` | 5003 | Reward ID không tồn tại | PickupGroundReward |
| `GroundRewardNotFound` | 5004 | Reward không còn trên map | PickupGroundReward |
| `GroundRewardNotOwnedYet` | 5005 | Reward chưa thuộc về player | PickupGroundReward |
| `GroundRewardExpired` | 5006 | Reward hết hạn | PickupGroundReward |
| `GroundRewardOutOfRange` | 5010 | Quá xa | PickupGroundReward |
| `GroundRewardClaimInProgress` | 5011 | Claim đang xử lý | PickupGroundReward |

---

# Out of Scope (client lần này không làm)

- Inventory slot index / drag-and-drop sắp xếp vị trí
- Bag downgrade
- Wild herb nodes
- Herb drop từ quái (enemy reward wiring) — separate system
- NPC shop framework đầy đủ — chỉ implement flow upgrade bag
- Balance values (timer, drop rate, yield) — do Game Design quyết định
- Client UI layout/design — do UX/artist quyết định

---

# Residual Risks từ QA (cần biết)

1. Chưa có automated runtime test harness cho herb random-output combinations phía server — nếu gặp edge case lạ, báo lại
2. `ItemService.AddItemAsync` không tự enforce capacity globally — authority check do caller (server service) chịu trách nhiệm; client không cần làm gì thêm, chỉ cần handle `InventoryFull` đúng

---

# Checklist Dev-Client

- [ ] Đọc `HerbPackets.cs` để xác nhận packet IDs + fields herb
- [ ] Implement `GetInventory` flow + render bag state (UsedSlots/TotalSlots/DisplayName)
- [ ] Implement `GetBagState` standalone refresh
- [ ] Implement `UpgradeBag` flow (request → result → update UI)
- [ ] Implement herb garden panel: plot list, states, action buttons
- [ ] Implement `InsertSoil`, `PlantSeed`, `PlantExistingHerb`
- [ ] Implement `HarvestHerb` với đúng error handling (giữ plot khi InventoryFull)
- [ ] Implement `ExtractHerb` với đúng error handling
- [ ] Implement ground reward: spawn/despawn/pickup flow
- [ ] Handle tất cả error codes trong bảng trên
- [ ] Không xóa plot UI / reward UI khi nhận `InventoryFull`
