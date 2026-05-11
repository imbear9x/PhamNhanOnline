---
doc_type: conflict_report
status: open
created_by: dev
created_at: "2026-05-11"
conflict_type: unimplemented-design-field
affected_systems:
  - portals
  - world interaction
affected_docs:
  - docs/game-design-wp/clarifications/portal-design-clarification.md
affected_code:
  - GameServer/World/MapTravelTopologyTypes.cs
  - GameServer/Network/Handlers/TravelToMapHandler.cs
  - GameServer/DTO/NetworkModelMapper.cs
affected_configs: []
severity: medium
requires_manager_decision: true
---

# Conflict Summary

Portal data model có `Portal Interaction Mode` (`Touch` vs `Interact`), nhưng batch-1 runtime được verify chưa cho thấy `TravelToMapHandler` enforce behavior khác nhau theo mode này.

# Intended Design From Docs

- Portal clarification nói portal có thể là `Touch` hoặc `Interact`.
- Nếu field mode tồn tại trong data thì cuối cùng nó phải có meaning gameplay thật, không nên là metadata chết.

# Current Implementation From Code / Config

- Portal mode được load vào `MapPortalDefinition`.
- Portal mode được serialize ra client model.
- `TravelToMapHandler` validate portal existence, enabled flag, target map/spawn, live instance, và range; không thấy branch runtime khác nhau theo `Touch` hay `Interact`.

# Runtime / QA Evidence

- Verified từ `GameServer/Network/Handlers/TravelToMapHandler.cs`
- Verified từ `GameServer/World/MapCatalog.cs`
- Verified từ `GameServer/DTO/NetworkModelMapper.cs`

# Why This Matters

- Designer có thể tưởng `Touch` và `Interact` đang tạo UX khác nhau, nhưng runtime có thể chưa làm vậy.
- Client có thể render khác dựa trên mode, trong khi server chưa enforce semantic tương ứng.
- Dễ tạo exploit hoặc mismatch UX nếu sau này portal yêu cầu bấm tương tác nhưng direct travel vẫn đi được.

# Affected Systems

- portal interaction UX
- server-side travel validation
- map data authoring expectations

# Related Files

## Docs

- `docs/maps/portal-travel-runtime.md`

## Code

- `GameServer/World/MapTravelTopologyTypes.cs`
- `GameServer/Network/Handlers/TravelToMapHandler.cs`
- `GameServer/DTO/NetworkModelMapper.cs`

## Config / DB

- `map_portals.interaction_mode`

# Suggested Options

## Option A: Fix code to match docs

Server enforce semantic khác nhau cho `Touch` và `Interact`, đồng thời align client UX.

## Option B: Update docs because design changed

Nếu chỉ cần một mode thực tế, simplify data model/docs và bỏ semantic dư thừa.

## Option C: Need further investigation

Check client-side portal interaction UX và existing authored portal rows để xem mode đã được dùng thực tế tới đâu.

# Recommendation

Option C trước. Hiện canonical docs chỉ nên xác nhận mode field tồn tại, chưa xác nhận behavior gameplay riêng theo mode.

# Questions For Manager

- `Touch` và `Interact` có phải 2 behavior gameplay thật sự cần giữ không?
- Nếu có, server hay client sẽ là nơi bắt buộc enforce chính?
