# Portal Design Clarification

## Intended player-facing behavior

- Portal là điểm chuyển map/điểm chuyển khu vực được đặt sẵn trên map.
- Người chơi tương tác với portal tại vị trí đúng trên map hiện tại để đi tới map đích và spawn point đích.
- Sau khi dùng portal thành công, người chơi phải xuất hiện ở map đích tại spawn point hợp lệ và nhận world snapshot mới.
- Portal nên là **cách travel có ngữ cảnh trong world**, tức là gắn với vị trí, không chỉ là “teleport bằng id map”.
- Tùy thiết kế sau này, portal có thể là:
  - đi qua khi chạm vào (**Touch**)
  - hoặc cần bấm tương tác (**Interact**)
  nhưng đây cần được canonicalize rõ vì code hiện chưa enforce hoàn chỉnh.

## Intended terminology

- **Portal**: điểm travel đặt trên map
- **Source Map**: map đang đứng trước khi đi portal
- **Target Map**: map đích của portal
- **Target Spawn Point**: vị trí đích được portal đưa tới
- **Portal Interaction Mode**: kiểu kích hoạt portal (`Touch` hoặc `Interact`)
- **Portal Interaction Radius**: phạm vi hợp lệ để sử dụng portal

## Intended rules

- Portal phải thuộc đúng **source map** và dẫn đến **target map + target spawn point** hợp lệ.
- Portal chỉ dùng được nếu portal đang **enabled**.
- Người chơi phải ở đủ gần portal để dùng.
- Travel bằng portal nên sinh ra **entry context = Portal** để downstream systems biết đây là travel qua cổng, không phải login/world restore.
- Private target map nên vào private context tương ứng; public target map nên vào public zone hợp lệ.
- Portal là thực thể travel có vị trí rõ ràng trên map; validation chính nên dựa trên vị trí runtime chứ không chỉ packet input từ client.
- Interaction mode (`Touch` vs `Interact`) nếu tồn tại trong data thì cuối cùng phải có ý nghĩa gameplay thật, không nên chỉ là metadata chết.
- Legacy direct map travel, nếu còn giữ, nên được xem là compatibility path hoặc GM/system path, không nên mặc định thay thế hoàn toàn world portal UX.

## Acceptable current behavior

- Portal data đã có đủ các trường cốt lõi: source, target, radius, mode, enabled, order.
- Portal được nạp sẵn, group theo map, sort ổn định và gửi cho client trong map model.
- Travel handler đã validate các điều kiện nền tảng quan trọng:
  - player/session hợp lệ
  - portal tồn tại và enabled
  - player đang ở trong live instance
  - khoảng cách đến portal hợp lệ
- Travel thành công sẽ cập nhật vị trí/map, set `MapEntryContext` với lý do `Portal`, rồi republish world snapshot.
- Target map private ép về zone `0`, target không private auto-join public zone là chấp nhận được ở thời điểm hiện tại.

## Mismatch vs current code

- `Portal Interaction Mode` được load và serialize nhưng chưa thấy được enforce trong `TravelToMapHandler`. Đây là mismatch rõ giữa data model và gameplay behavior thực tế.
- Cùng một packet đang hỗ trợ cả **portal travel** lẫn **legacy direct travel by target map id**. Điều này làm ranh giới giữa “đi qua cổng trong world” và “nhảy map bằng logic cũ” chưa sạch.
- Effective adjacency của map hiện đang tính cả portal target vào topology travel. Nếu canonical portal là cơ chế travel có tương tác/vị trí riêng, việc trộn nó vào adjacency có thể làm sai semantic.
- Optional packet fields `CurrentPosX/CurrentPosY` chỉ được log mà không dùng làm validation trong handler. Nếu client UX đang gửi nó như dữ liệu meaningful, runtime hiện chưa tận dụng.
- Chỉ một portal row lỗi cũng có thể làm fail startup catalog. Điều này ổn ở góc integrity, nhưng canonical docs nên nêu rõ portal data hiện là cấu hình “hard-fail” chứ không phải soft-invalid content.

## Unresolved design questions

- `Touch` và `Interact` có thật sự là hai mode gameplay khác nhau không? Nếu có, khác nhau cụ thể thế nào ở UX và anti-misclick?
- Legacy direct map travel có nên tiếp tục tồn tại cho gameplay thường không, hay chỉ giữ cho compatibility/admin/internal flow?
- Người chơi có được nhìn thấy thông tin mô tả portal/label của portal trên client hay không, và nó có ảnh hưởng UX lựa chọn travel không?
- Có cần loại portal một chiều / hai chiều / portal có điều kiện mở khóa không?
- Có cần rule anti-combat/anti-exploit khi dùng portal (ví dụ đang bị control, đang cast, đang trong trạng thái PK đặc biệt)?
- Portal validation có cần dựa thêm trên line-of-access/collision hay chỉ radius là đủ?

## Clarification status (audit-driven)

_Last updated against `docs/qa/legacy-domain-coverage-audit.md` — portal domain is `needs-review`._

### Open conflict 1 — Portal Interaction Mode (`Touch` vs `Interact`)

**Conflict doc:** `docs/conflicts/portal-interaction-mode-runtime-gap.md`

- Code evidence: `interaction_mode` field exists in portal data model, is loaded into `MapPortalDefinition`, is serialized to client. `TravelToMapHandler` does **not** branch behavior by this field — no verified runtime difference between `Touch` and `Interact` in the batch-1 code review.
- Design question: Are `Touch` and `Interact` genuinely two different gameplay UX behaviors? If yes, which side enforces the distinction — server validation, client input gate, or both?
- **This question must be answered before portal coverage can move from `needs-review` to `good`.**
- Acceptable interim stance: document that `interaction_mode` field is present and transmitted to client; do not canonicalize any behavioral difference between modes until this decision is made.

### Open conflict 2 — Travel topology vs portal semantics

**Conflict doc:** `docs/conflicts/map-travel-topology-vs-portal-semantics.md`

- Code evidence: `MapCatalog` builds `AdjacentMapIds` by unioning manually configured adjacent maps **plus** all enabled portal target maps. `MapDefinition.CanTravelTo(...)` uses this merged set. Legacy direct travel via `TargetMapId` in `TravelToMapPacket` still works through this merged adjacency.
- Design question: Is a portal target map automatically an allowed-travel topology entry, or should portal travel and map adjacency be separate graphs?
- Secondary question: Is direct map travel (without going through a positioned portal) an intended player-facing gameplay path, or should it be restricted to system/compatibility use only?
- **This question must be answered before topology rules can be canonicalized cleanly.**
- Acceptable interim stance: note that merged adjacency exists and is the current runtime behavior; do not present it as the final intended design.

### What is already stable (can be canonicalized now)

- Portal is a positioned travel point on source map with enabled flag, interaction radius, target map, and target spawn point.
- Travel validation checks: portal enabled, target map/spawn valid, player in live instance, player within interaction radius (server-side, not client-trusted).
- Successful travel sets `MapEntryContext = Portal` and republishes world snapshot.
- Private target map → zone 0; public target map → auto-selected public zone.
- Portal data is hard-fail at catalog startup (one invalid row can fail startup integrity).

## Canonicalization recommendation

- Canonicalize portal thành một doc riêng cho:
  1. **portal data contract**
  2. **portal interaction/travel flow**
- Ghi rõ rằng batch 1 chỉ có thể xác nhận chắc intent tối thiểu là:
  - portal là travel point theo vị trí
  - có enabled flag
  - có range validation
  - có source/target/spawn point rõ ràng
- Đánh dấu `Touch` vs `Interact` là **design-intent-present-but-runtime-not-fully-confirmed**.
- Đánh dấu legacy direct travel là **compatibility behavior cần canonical decision sau**, không nên mô tả như intended primary UX nếu chưa có quyết định design.
- Khi canonicalize topology, tách riêng khái niệm:
  - **portal travel**
  - **map adjacency / allowed travel graph**
