# Đặc Tả Luồng Sử Dụng Vật Phẩm

## Phạm vi

Tài liệu này chốt hướng thiết kế cho luồng `use item` trong inventory.

- Generic:
  - `Equipment`
  - `MartialArtBook`
  - `PillRecipeBook`
  - `Consumable`
- Chuyên biệt:
  - `Soil`
  - `HerbSeed`
  - `HerbPlant`
  - `Talisman`

Ghi chú cho phase hiện tại:

- `PillRecipeBook` hiện đã đi qua generic `UseItemPacket`.
- `Soil`, `HerbSeed`, `HerbPlant`, `Talisman` vẫn là nhóm cần packet chuyên biệt vì thiếu context nếu ép dùng generic packet.

## Packet generic

### Request

`UseItemPacket(playerItemId, quantity)`

### Response

`UseItemResultPacket(success, code, playerItemId, requestedQuantity, appliedQuantity, items, baseStats, currentState, learnedMartialArt, cultivationPreview)`

### Nguyên tắc chung

- Validate ownership trước mọi logic.
- Validate item đang ở `Inventory`.
- Validate quantity hợp lệ.
- Validate item definition hợp lệ.
- Nếu fail:
  - `success = false`
  - `appliedQuantity = 0`
  - không được có side effect
- Nếu action có đổi inventory:
  - trả `items`
- Nếu action có đổi stats:
  - trả `baseStats`
- Nếu action có đổi HP/MP/stamina/state:
  - trả `currentState`
- Nếu action học công pháp:
  - trả `learnedMartialArt`
- Nếu action đổi cultivation preview:
  - trả `cultivationPreview`

## Nhóm generic

| Type | Action | Packet | Validation | Expected Response | Test Cases |
|---|---|---|---|---|---|
| `Equipment` | Trang bị item vào đúng slot của item | `UseItemPacket` | Item tồn tại; thuộc player; đang ở inventory; chưa expired; `quantity == 1`; item type đúng là `Equipment`; có `EquipmentSlotType`; slot metadata hợp lệ | `success=true`; `items` updated; `baseStats` updated; `currentState` được clamp nếu max stat thay đổi; `appliedQuantity=1` | Trang bị thành công; trang bị khi slot đang có item; trang bị item sai metadata slot; trang bị item không thuộc player; trang bị item expired; trang bị với `quantity > 1` |
| `MartialArtBook` | Học công pháp từ sách | `UseItemPacket` | Item tồn tại; thuộc player; đang ở inventory; chưa expired; `quantity == 1`; item type là `MartialArtBook`; mapping sang martial art hợp lệ; player chưa học martial art đó | `success=true`; item bị consume; `items` updated; `baseStats` updated; `learnedMartialArt` filled; `cultivationPreview` updated; `appliedQuantity=1` | Học thành công; học sách trùng martial art đã có; item không phải martial art book; item expired; `quantity > 1`; inventory sync sau consume |
| `Consumable` | Dùng trực tiếp, áp effect self-use | `UseItemPacket` | Item tồn tại; thuộc player; đang ở inventory; chưa expired; `quantity >= 1`; quantity không vượt stack; item type là `Consumable`; effect nằm trong danh sách support của phase hiện tại; nếu có cooldown thì cooldown pass | `success=true`; item giảm stack hoặc bị xóa; `items` updated; `currentState` updated nếu hồi HP/MP/Stamina; `baseStats` chỉ trả nếu consumable đổi stat lâu dài; `appliedQuantity = quantity thực dùng` | Dùng 1 item; dùng nhiều item trong stack; dùng quantity > stack; dùng item unsupported effect; dùng khi full HP/MP; stack về 0 sau khi dùng |
| `PillRecipeBook` | Học công thức luyện đan từ sách | `UseItemPacket` | Item tồn tại; thuộc player; đang ở inventory; chưa expired; `quantity == 1`; item type là `PillRecipeBook`; mapping sang recipe hợp lệ; người chơi chưa học recipe đó | `success=true`; item bị consume; `items` updated; `appliedQuantity=1` | Học thành công; học trùng recipe; item không phải recipe book; item expired; `quantity > 1`; verify inventory sync sau consume |

## Consumable phase 1

Phase đầu chỉ support nhóm consumable tự dùng có effect hồi tài nguyên cơ bản.

| Subtype | Rule | Expected Response | Test Cases |
|---|---|---|---|
| Recover HP | Cộng HP theo effect, clamp về `FinalHp` | `currentState` mới | Đang mất máu; đang full HP; dùng nhiều item cùng lúc |
| Recover MP | Cộng MP theo effect, clamp về `FinalMp` | `currentState` mới | Đang thiếu MP; đang full MP; dùng nhiều item cùng lúc |
| Recover HP + MP | Cộng đồng thời HP và MP | `currentState` mới | Item có 2 effects |
| Buff stat / breakthrough / clear debuff / special | Để phase sau | `success=false`, `code=ItemUseUnsupported` | Verify không consume item khi unsupported |

## Nhóm chuyên biệt

| Type | Action | Packet | Validation | Expected Response | Test Cases |
|---|---|---|---|---|---|
| `Soil` | Chèn soil vào plot | `InsertSoilPacket(playerItemId, caveId, plotIndex)` | Item tồn tại; thuộc player; đang ở inventory; item type là `Soil`; plot thuộc player; plot chưa có soil active; soil chưa được insert ở chỗ khác; soil template hợp lệ | `InsertSoilResultPacket`; `success=true`; inventory updated hoặc reload; garden plot state updated | Chèn thành công; plot đã có soil; soil đang ở plot khác; item không phải soil; cave không thuộc player |
| `HerbSeed` | Trồng seed vào plot | `PlantHerbSeedPacket(playerItemId, caveId, plotIndex)` | Item tồn tại; thuộc player; đang ở inventory; item type là `HerbSeed`; plot có soil; plot chưa có herb; seed map được tới herb template hợp lệ | `PlantHerbSeedResultPacket`; inventory updated; garden/herb runtime updated | Trồng thành công; plot chưa có soil; plot đã có herb; item không phải seed; seed mapping invalid |
| `HerbPlant` | Trồng lại hoặc harvest/move | `PlantExistingHerbPacket(playerHerbId, caveId, plotIndex)`; `HarvestHerbPacket(playerHerbId)`; `MoveHerbToInventoryPacket(playerHerbId)` | Herb thuộc player; herb đúng state; plot có soil nếu trồng lại; herb đủ stage nếu harvest; output config hợp lệ | Result packet riêng theo từng action; inventory updated nếu harvest; garden/herb state updated | Trồng lại herb vào plot; trồng lại khi plot đầy; harvest stage `Mature`; harvest stage chưa đủ; move herb về inventory |
| `Talisman` | Kích hoạt theo target/context | `UseTalismanPacket(playerItemId, targetType, targetId/pos)` | Item tồn tại; thuộc player; đang ở inventory; item type là `Talisman`; talisman definition hợp lệ; target hợp lệ; range/cooldown/state pass | `UseTalismanResultPacket`; inventory updated nếu consume; `currentState` hoặc world/combat state updated tùy effect | Self-cast; cast vào enemy; target invalid; out of range; cooldown; consume success/fail |

## Đề xuất tên packet cho nhóm chuyên biệt

- `InsertSoilPacket` / `InsertSoilResultPacket`
- `PlantHerbSeedPacket` / `PlantHerbSeedResultPacket`
- `PlantExistingHerbPacket` / `PlantExistingHerbResultPacket`
- `HarvestHerbPacket` / `HarvestHerbResultPacket`
- `MoveHerbToInventoryPacket` / `MoveHerbToInventoryResultPacket`
- `UseTalismanPacket` / `UseTalismanResultPacket`

## Những type không nên có nút Use chung

Những type này không nên đi qua `UseItemPacket(playerItemId, quantity)` ở UI inventory thông thường:

- `Material`
- `HerbMaterial`
- `Currency`
- `QuestItem`

Hướng xử lý:

- Client disable nút `Use`
- Không roundtrip lên server chỉ để nhận `unsupported`

## Thứ tự ưu tiên khi làm

1. `Consumable`
2. `Soil`
3. `HerbSeed`
4. `HerbPlant`
5. `Talisman`

## Checklist chung cho mỗi type

- Validate ownership
- Validate `LocationType == Inventory` nếu là inventory item
- Validate quantity
- Validate item type và definition
- Không được có side effect nếu fail
- Inventory sync đúng sau action
- Stat/state sync đúng nếu có thay đổi
- Client UI disable đúng các type chưa support

## Ghi chú triển khai

- Generic packet chỉ phù hợp với item "self-contained".
- Các item cần thêm context như plot, target, vị trí, state combat thì nên giữ packet chuyên biệt riêng.
- Không nên có một generic packet phình to cho mọi trường hợp nếu semantic action đã khác nhau rõ ràng.
