# Lộ Trình Phase 3 Cho Hệ Trình Diễn Kỹ Năng

> Lưu ý: đây là tài liệu định hướng tương lai, không phải danh sách những gì project đã implement ở thời điểm hiện tại.

## Mục tiêu của Phase 3

Phase 3 không nhằm thay hệ thống Phase 1-2.

Mục tiêu của nó là:

- giữ nguyên trục kiến trúc hiện tại
- mở rộng để chịu được skill phức tạp hơn
- tránh việc khi thêm summon, multi-hit, channeling thì phải đập lại service/presenter cũ

## Khi nào nên bắt đầu Phase 3

Chỉ nên vào Phase 3 khi roadmap thật sự cần các skill có đặc điểm như:

- một skill nhiều hit
- một skill có nhiều impact theo thời gian
- một skill niệm lâu, có thể bị ngắt
- skill summon ra actor phụ
- skill có projectile nảy, xuyên, tách nhánh
- skill cần marker release chính xác theo animation
- combat có nhiều caster dùng cùng skill gần như cùng lúc

Nếu game hiện chỉ có:

- chém cận chiến
- đạn chưởng bay thẳng
- phép rơi 1 impact vào target
- buff/self cast

thì Phase 1-2 là đủ lâu.

## Phase 3 sẽ làm gì

### 1. Chuẩn hóa execution timeline thành nhiều event hơn

Hiện tại execution mới có:

- cast started
- released
- impact resolved
- completed

Phase 3 sẽ mở rộng để support:

- `ChannelStarted`
- `ChannelTick`
- `ReleaseMarkerReached`
- `ProjectileSpawned`
- `ProjectileArrived`
- `ImpactResolved`
- `ImpactWaveResolved`
- `ExecutionCancelled`

Mục đích:

- không phải bẻ phase cũ
- mỗi loại skill phức tạp có phase riêng để UI/VFX/anim bám vào

### 2. Tách execution state nội bộ thành model giàu dữ liệu hơn

Hiện `ClientSkillPresentationState` đang đủ cho Phase 1-2.

Phase 3 sẽ cần thêm:

- danh sách hit con trong cùng execution
- danh sách projectile con
- summon actor id gắn với execution
- trạng thái `interrupted / cancelled / partially resolved`

Khuyến nghị triển khai:

- giữ `SkillExecutionKey` hiện tại
- thêm `SubExecutionId` hoặc `PhaseSequence` nếu cần
- không đổi key cũ để tránh phá compatibility

### 3. Bổ sung server contract cho phase nâng cao

Phase 3 lý tưởng nên có thêm packet/data từ server:

- `ReleaseMarkerUnixMs`
- `ExecutionCancelledPacket`
- `SkillProjectileSpawnedPacket`
- `SkillImpactWaveResolvedPacket`
- `SummonSpawnedPacket` hoặc execution-to-entity link

Không nhất thiết phải làm hết ngay, nhưng nên roadmap theo hướng đó.

## Cách làm Phase 3 mà không đập code cũ

### Bước 1. Giữ nguyên `ClientCombatService`

Không đẩy logic FX vào đây.

`ClientCombatService` vẫn chỉ nên:

- nhận packet
- publish notice combat
- giữ cooldown/cast authoritative

### Bước 2. Nâng cấp `ClientSkillPresentationService`

Thay vì chỉ quản `CastStarted -> Released -> ImpactResolved`,
nó sẽ trở thành timeline orchestrator đầy đủ hơn.

Các phần cần mở rộng:

- internal scheduler mạnh hơn
- queue event theo execution
- xử lý partial completion
- xử lý cancel và timeout

### Bước 3. Tách behavior đặc biệt theo strategy

Khi Phase 3 tới, không nên nhồi tất cả vào `CharacterSkillPresenter`.

Nên tách thêm một lớp strategy như:

- `ISkillPresentationStrategy`
- `MeleeSwingPresentationStrategy`
- `ProjectilePresentationStrategy`
- `SummonStrikePresentationStrategy`
- `ChannelingPresentationStrategy`

`ClientSkillPresentationService` sẽ resolve strategy theo archetype hoặc definition.

Như vậy:

- presenter chỉ còn lo chạm animator/socket/fx
- strategy lo điều phối behavior timeline phức tạp

### Bước 4. Tách projectile/summon thành actor presentation riêng

Nếu skill có projectile hoặc summon phức tạp, không nên giữ chúng chỉ là `GameObject` con của caster mãi.

Nên có:

- `SkillProjectilePresenter`
- `SkillSummonPresenter`
- registry theo `SkillExecutionKey`

Để:

- update độc lập
- cleanup đúng execution
- dễ debug

### Bước 5. Thêm animation event bridge

Một số skill cần marker cực chuẩn:

- vung kiếm đến frame nào thì thả kiếm khí
- kéo cung đến frame nào thì nhả tên
- niệm phép đến frame nào thì bật magic circle

Lúc đó nên thêm:

- `SkillAnimationEventRelay`
- event name chuẩn hóa
- mapping event -> execution phase/action

Mục tiêu:

- artist chỉnh timing trong animation clip
- dev không hardcode hết bằng time ms

## Danh sách hạng mục Phase 3 nên làm

### Nhóm A. Timeline và state

- thêm event cancel/interrupted
- thêm sub-hit/sub-projectile state
- thêm timeout/recovery rule cho packet đến muộn

### Nhóm B. Strategy

- trích logic ra khỏi service thành strategy theo archetype
- cho phép definition chọn strategy override khi cần

### Nhóm C. Projectile/Summon actor

- projectile visual có vòng đời riêng
- summon actor có registry riêng
- execution có thể giữ reference tới actor presentation đang sống

### Nhóm D. Animation marker

- bridge animation event -> execution timeline
- release marker chính xác theo clip
- support combo chain tốt hơn

### Nhóm E. UI nâng cao

- HUD timeline cho skill channeling
- icon trạng thái summon
- indicator vùng impact nhiều nhịp
- debug overlay theo `SkillExecutionId`

## Rủi ro nếu làm Phase 3 sai cách

### 1. Chôn logic vào presenter

Nếu `CharacterSkillPresenter` vừa spawn FX vừa điều phối timeline phức tạp, class này sẽ phình rất nhanh và khó bảo trì.

### 2. Chôn logic vào HUD

HUD chỉ nên hiển thị trạng thái.
Không nên trở thành nơi quyết định projectile/summon/cancel chạy ra sao.

### 3. Dùng `SkillId` thay cho execution identity

Đây là lỗi rất nguy hiểm khi combat đông người:

- 2 caster cùng dùng 1 skill
- 1 caster spam cùng 1 skill nhiều lần

Khi đó `SkillId` không đủ để ghép đúng timeline.

Luôn ưu tiên `SkillExecutionId`.

## Kế hoạch triển khai Phase 3 gợi ý

### Phase 3A

- thêm `ExecutionCancelled`
- thêm strategy interface
- refactor service để dùng strategy

### Phase 3B

- thêm projectile presenter riêng
- support nhiều projectile trong một execution

### Phase 3C

- thêm summon presenter
- thêm execution-to-summon tracking

### Phase 3D

- thêm animation event bridge
- cho phép release marker theo clip thay vì chỉ theo thời gian packet

## Kết luận

Phase 1-2 hiện tại là nền rất tốt để đi tiếp.

Nếu sau này vào Phase 3, nguyên tắc quan trọng nhất là:

- giữ `ClientCombatService` sạch
- giữ `SkillExecutionId` là identity chuẩn
- thêm lớp strategy/actor mới thay vì phá presenter cũ
- coi `ClientSkillPresentationState` là trung tâm timeline thay vì nhét state lung tung vào scene object
