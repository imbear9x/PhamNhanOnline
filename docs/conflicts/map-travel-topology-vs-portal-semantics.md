---
doc_type: conflict_report
status: open
created_by: dev
created_at: "2026-05-11"
conflict_type: semantic-drift
affected_systems:
  - maps
  - portals
  - world travel
affected_docs:
  - docs/game-design-wp/clarifications/map-design-clarification.md
  - docs/game-design-wp/clarifications/portal-design-clarification.md
affected_code:
  - GameServer/World/MapCatalog.cs
  - GameServer/World/MapDefinition.cs
  - GameServer/Network/Handlers/TravelToMapHandler.cs
affected_configs: []
severity: medium
requires_manager_decision: true
---

# Conflict Summary

Runtime hiện gộp `configured adjacent maps` và `enabled portal target maps` vào cùng một effective adjacency list dùng cho `CanTravel`. Trong khi đó design clarification tách rõ portal travel và travel topology như hai semantic khác nhau.

# Intended Design From Docs

- Map clarification nói adjacency/travel permission nên là rule thiết kế riêng, không nên vô tình bị quyết định ngầm bởi data source khác nghĩa gameplay.
- Portal clarification nói portal là travel có ngữ cảnh theo vị trí, không nên tự động bị đồng nhất với mọi allowed-travel topology.

# Current Implementation From Code / Config

- `MapCatalog` build `AdjacentMapIds` bằng cách union:
  - adjacent maps cấu hình thủ công
  - target map của mọi portal enabled
- `MapDefinition.CanTravelTo(...)` dùng merged adjacency này.
- `TravelToMapHandler` còn giữ legacy direct travel path qua `TargetMapId`, nên merged adjacency ảnh hưởng trực tiếp ability nhảy map không đi qua portal interaction flow.

# Runtime / QA Evidence

- Verified từ `GameServer/World/MapCatalog.cs`
- Verified từ `GameServer/World/MapDefinition.cs`
- Verified từ `GameServer/Network/Handlers/TravelToMapHandler.cs`

# Why This Matters

- Có thể làm player travel được theo direct-map path chỉ vì portal target tồn tại.
- Làm semantic của portal data bị nở rộng hơn intent gameplay gốc.
- Gây khó khăn khi canonicalize route restrictions, UX portal, hoặc future locked portals.

# Affected Systems

- map travel validation
- portal UX
- legacy direct travel compatibility

# Related Files

## Docs

- `docs/maps/map-instance-and-world-entry-runtime.md`
- `docs/maps/portal-travel-runtime.md`

## Code

- `GameServer/World/MapCatalog.cs`
- `GameServer/World/MapDefinition.cs`
- `GameServer/Network/Handlers/TravelToMapHandler.cs`

## Config / DB

- portal rows in `map_portals`
- adjacent-map rows

# Suggested Options

## Option A: Fix code to match docs

Tách `portal travel graph` khỏi `direct adjacency graph`; `CanTravel` chỉ dùng adjacency canonical thật sự.

## Option B: Update docs because design changed

Xác nhận portal target map mặc định cũng là allowed direct-travel topology, rồi canonicalize rule này rõ ràng.

## Option C: Need further investigation

Review UX/client flows và nội dung map hiện có để xem legacy direct travel còn là gameplay path chủ động hay chỉ compatibility path.

# Recommendation

Option C trước, rồi nhiều khả năng đi tới Option A. Hiện chưa nên canonicalize merged adjacency như intended design cuối cùng.

# Questions For Manager

- Direct map travel có còn là gameplay path chính thức không?
- Portal target có mặc định đồng nghĩa với allowed direct travel không?
- Có cần topology riêng cho portal-locked progression không?
