---
title: Client Dev Follow-up — Herb Farming Canonical Release Bundle (Baseline + Drop Reject Correction)
doc_type: handoff
status: Ready
owner: dev-client
source_agent: techdesign
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: unity-client-implementation
queue_id: 36
feature_key: herb-farming-system
handoff_type: client-dev
source_handoff: docs/agent-handoffs/active/20260515-35-herb-farming-drop-reject-fix-qa-report.md
response_to: docs/agent-handoffs/active/20260515-35-herb-farming-drop-reject-fix-qa-report.md
supersedes: docs/agent-handoffs/active/20260515-18-herb-bag-client-dev.md
iteration: 1
---

# Mục tiêu

Đây là handoff cho **Unity client** để chốt **herb-farming-system** theo **canonical release bundle**:

- **baseline đã pass QA trước đó**
- **+ correction round đã pass QA tại `#35`**

> Release authority cho client không còn dùng `#25` đơn lẻ nữa.
> Canonical source là: **`#25 baseline` + `#35 correction`**, trong đó `#35` supersedes release decision cũ.

Handoff này chỉ tập trung vào **client contract cho Herb Farming**, đặc biệt bổ sung behavior mới cho **enemy herb direct-grant reject-on-full**.

---

# Nguồn authority cần đọc

## Handoffs / reports
- `docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md`
- `docs/agent-handoffs/active/20260515-35-herb-farming-drop-reject-fix-qa-report.md`
- `docs/agent-handoffs/active/20260515-18-herb-bag-client-dev.md` *(tham chiếu baseline client scope cũ, nhưng không còn đầy đủ authority cho herb correction)*

## Specs / packets / models
- `docs/tech-design/herb-farming-system.md`
- `GameShared/Packets/Packets/HerbPackets.cs`
- `GameShared/Packets/Packets/WorldPackets.cs`
- `GameShared/Models/GardenPlotStateModel.cs`
- `GameShared/Models/InventoryItemModel.cs`
- `GameShared/Messages/MessageCode.cs`

---

# Canonical gameplay rules client phải reflect

## 1) Garden actions full-bag reject
Áp dụng cho herb-related actions:
- harvest herb
- extract herb
- enemy herb direct-grant correction path

Rule chung:
- **đầy túi thì reject**
- **không inbox fallback**
- client phải **giữ nguyên state hiện tại** khi action fail do full bag, trừ khi server broadcast/result nói ngược lại

## 2) Enemy herb direct-grant correction
Theo QA `#35`:
- herb reward từ quái ở direct-grant path nếu không fit inventory thì:
  - **không grant herb item**
  - **không inbox fallback**
  - **không ground reward workaround**
  - server gửi **`PickupGroundRewardResultPacket` + `MessageCode.InventoryFull`** làm minimal signal path
- nếu reward mix herb + non-herb:
  - herb bị reject
  - non-herb vẫn grant bình thường
  - client vẫn có thể nhận `InventoryFull`

> Đây là **accepted contract hiện tại**, dù packet signal chưa phải semantic đẹp nhất. Client phải handle đúng thay vì tự suy diễn đây là pickup ground reward thất bại kiểu thông thường.

---

# Packet contract cần implement / verify

## A. Garden packets (`HerbPackets.cs`)

### 1. Get garden plots
- `GetGardenPlotsPacket` `[Packet(200)]`
- `GetGardenPlotsResultPacket` `[Packet(210)]`

Fields quan trọng:
- request: `CaveId`
- result: `Success`, `Code`, `CaveId`, `Plots`

### 2. Insert soil
- `InsertSoilPacket` `[Packet(201)]`
- `InsertSoilResultPacket` `[Packet(211)]`

Fields quan trọng:
- request: `SoilPlayerItemId`, `CaveId`, `PlotIndex`
- result: `Success`, `Code`, `Plot`

### 3. Plant seed
- `PlantHerbSeedPacket` `[Packet(202)]`
- `PlantHerbSeedResultPacket` `[Packet(212)]`

Fields quan trọng:
- request: `SeedPlayerItemId`, `CaveId`, `PlotIndex`
- result: `Success`, `Code`, `PlayerHerbId`, `Plot`

### 4. Plant existing herb
- `PlantExistingHerbPacket` `[Packet(203)]`
- `PlantExistingHerbResultPacket` `[Packet(213)]`

Fields quan trọng:
- request: `PlayerHerbId`, `CaveId`, `PlotIndex`
- result: `Success`, `Code`, `Plot`

### 5. Harvest herb
- `HarvestHerbPacket` `[Packet(204)]`
- `HarvestHerbResultPacket` `[Packet(214)]`

Fields quan trọng:
- request: `PlayerHerbId`
- result: `Success`, `Code`, `PlayerHerbId`, `ExpireAtUnixMs`

### 6. Extract herb
- `ExtractHerbPacket` `[Packet(205)]`
- `ExtractHerbResultPacket` `[Packet(215)]`

Fields quan trọng:
- request: `PlayerHerbId`
- result: `Success`, `Code`, `Items`, `MamNonReturned`

---

## B. World / reward packets cần correlate

Client phải re-check `WorldPackets.cs` để handle đúng các packet world reward đang tồn tại, đặc biệt:
- `PickupGroundRewardPacket`
- `PickupGroundRewardResultPacket`
- `GroundRewardSpawnedPacket`
- `GroundRewardDespawnedPacket`
- `WorldRuntimeSnapshotPacket`

### Important correction behavior
Khi client nhận:
- `PickupGroundRewardResultPacket`
- `Success = false`
- `Code = InventoryFull`

thì cần support **2 khả năng hợp lệ**:
1. ground reward pickup thật sự bị full bag
2. **enemy herb direct-grant correction path** đang reuse packet này làm notify tối thiểu

=> Client **không được** tự assume là có ground reward map object tương ứng để xóa/sửa UI.

---

# UI / UX contract

## 1. Garden plot rendering
Dùng `GardenPlotStateModel` để render:
- plot trống
- plot có soil nhưng chưa trồng
- herb đang tăng trưởng
- herb đã mature / thousand-year có thể harvest
- herb inventory expiry data nếu model expose trong response/list

Nếu model có `HerbExpireAtUnixMs`, hiển thị countdown hoặc expiry text khi phù hợp.

## 2. Harvest success
Khi `HarvestHerbResultPacket.Success = true`:
- cập nhật plot UI theo dữ liệu refresh tiếp theo hoặc local optimistic update phù hợp
- đưa herb sang inventory-living-herb presentation
- lưu / hiển thị `ExpireAtUnixMs` nếu UI inventory herb có chỗ hiện

## 3. Harvest full-bag fail
Khi `HarvestHerbResultPacket.Success = false` và `Code` là full-bag herb authority hiện hành:
- show message kiểu: **"Túi đầy, không thể thu hoạch"**
- **không xóa plot khỏi UI**
- **không giả định herb đã bị move**

## 4. Extract full-bag fail
Khi `ExtractHerbResultPacket.Success = false` và `Code` báo full bag:
- show message kiểu: **"Túi đầy, không thể chiết xuất"**
- **không xóa herb entity khỏi inventory UI**
- không hiển thị reward items giả

## 5. Enemy herb direct-grant fail via world packet
Khi nhận `PickupGroundRewardResultPacket` với `InventoryFull` trong ngữ cảnh combat/world reward:
- show generic message kiểu: **"Túi đầy"**
- **không tự xóa ground reward map object** nếu client không có despawn packet thực tế
- **không mở inbox/mail UI**
- nếu reward mix herb + non-herb, client phải chấp nhận khả năng:
  - đã nhận non-herb item update
  - nhưng vẫn đồng thời thấy `InventoryFull` vì herb bị reject

## 6. Mixed reward UX note
Nếu player vừa nhận một phần reward non-herb vừa thấy `InventoryFull`, đây là **behavior hợp lệ theo authority hiện tại**.
Client không được coi đó là inconsistent state.

---

# Error-code handling tối thiểu

Client cần map ít nhất các case sau:

## Garden / herb
- `GardenInventoryFull` hoặc full-bag code cùng authority herb hiện hành
  - harvest/extract fail message
- các code validate khác từ garden packets
  - cave invalid / plot invalid / herb not owned / inventory item invalid

## World reward
- `InventoryFull`
  - hiển thị lỗi túi đầy
  - không force-despawn UI nếu không có packet despawn
- `GroundRewardExpired`
  - có thể xóa reward local nếu UI còn giữ object
- `GroundRewardOutOfRange`
  - show lỗi quá xa
- `GroundRewardClaimInProgress`
  - tránh spam resend

> Dev-client phải đọc `MessageCode.cs` thực tế để map đúng enum name/value, không hardcode theo tài liệu nếu code đã đổi tên.

---

# Required implementation tasks

## Task 1 — Align packet names/IDs từ code thật
Đọc `HerbPackets.cs` và `WorldPackets.cs`, update client transport registry / message routing theo packet thực tế.

## Task 2 — Finish herb garden flow UI/network
Support end-to-end các action:
- load plots
- insert soil
- plant seed
- plant existing herb
- harvest herb
- extract herb

## Task 3 — Handle full-bag authority correctly
Đảm bảo client không optimistic-remove state sai ở các case:
- harvest full bag fail
- extract full bag fail
- enemy herb direct-grant `InventoryFull`

## Task 4 — Handle accepted correction contract for enemy herb direct-grant
`PickupGroundRewardResultPacket + InventoryFull` phải được client xử lý an toàn dù không có corresponding ground reward object mutation.

## Task 5 — Non-regression with existing bag/inventory UI
Nếu project đã làm phần inventory/bag từ `#18`, không được break:
- inventory refresh flow
- item toast/list update
- world reward visuals

---

# Test checklist cho dev-client tự verify

## Garden
1. mở vườn và load plot list thành công
2. tra linh thổ thành công
3. trồng seed thành công
4. trồng herb inventory thành công
5. harvest thành công nhận herb inventory entity
6. harvest full bag fail → plot giữ nguyên
7. extract thành công nhận item outputs
8. extract full bag fail → herb inventory vẫn còn

## Correction round
9. enemy herb reward full bag → thấy `InventoryFull`, không inbox UI, không ground despawn giả
10. mixed reward herb + non-herb when herb rejected → non-herb update vẫn hiển thị ổn, đồng thời báo `InventoryFull`

## Stability
11. relog / map change không làm mất state UI sai
12. duplicate packet / delayed result không làm client xóa plot hoặc reward sai

---

# Release note cho client

Herb farming chỉ được coi là **done phía client** khi dev-client xác nhận support đầy đủ:
- baseline herb farming packets
- harvest/extract full-bag authority
- correction round enemy herb direct-grant reject-on-full

Nếu gặp chỗ packet signal chưa đủ semantic để làm UX sạch hơn, ghi rõ trong response nhưng **không tự đổi server contract**.
