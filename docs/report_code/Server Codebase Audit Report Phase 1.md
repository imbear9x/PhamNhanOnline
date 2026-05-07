# Server Codebase Audit Report Phase 1

## 1. Executive Summary

**Điểm mạnh:** server đã có hướng authoritative, packet contract tách qua `GameShared`, DI rõ, runtime world có lock theo `MapInstance`, config/gameplay template được compile thành catalog in-memory, có metrics và packet incident capture.

**Điểm yếu lớn nhất:** movement/player position hiện vẫn tin client quá nhiều. Đây là rủi ro anti-cheat nghiêm trọng nhất, kéo theo portal travel, combat range và pickup reward đều có thể bị khai thác.

**Sẵn sàng dài hạn:** code đang ổn cho prototype/vertical slice, nhưng nếu thêm quest, dungeon, boss event, auction thì sẽ phình nhanh vì `Service`, `Runtime`, `World` đang chứa nhiều class quá lớn và transaction/concurrency rule chưa đủ chặt.

## 2. Architecture Overview

- Startup: `GameServer/Program.cs` dựng DI, chạy `NetworkServer`, `GameLoop`, `RuntimeMaintenanceService`, `ServerMetricsLoggerService`.
- Network flow: LiteNetLib -> `NetworkServer.OnNetworkReceive` -> `PacketSerializer` -> per-session inbound queue -> `PacketDispatcher` -> middleware -> packet handler.
- Runtime flow: `GameLoop` tick 50ms, lấy snapshot instances, update enemy/spawn/skill/reward rồi publish packets qua `WorldInterestService`.
- DB flow: `LinqToDB` qua `GameDb`, repository mỏng, service/action service mở transaction khi cần.
- Cache flow: không có Redis. Cache hiện tại là singleton catalog load từ DB lúc boot: map, item, combat, enemy, random config.

## 3. Critical Issues

| Severity | Area | File/Class | Issue | Impact | Recommendation |
|---|---|---|---|---|---|
| Critical | Anti-cheat | `GameServer/Network/Handlers/CharacterPositionSyncHandler.cs` | Client gửi tọa độ, server chỉ `ClampPosition` rồi accept | Teleport/speed hack ảnh hưởng combat, portal, pickup | Chuyển sang movement intent hoặc validate delta bằng server speed/time |
| Critical | Portal | `GameServer/Network/Handlers/TravelToMapHandler.cs` | Portal validation dùng `packet.CurrentPosX/Y` nếu client gửi | Client có thể giả vị trí gần portal | Validate bằng server-trusted `player.Position`; packet pos chỉ dùng debug |
| High | Loot | `GameServer/Network/Handlers/PickupGroundRewardHandler.cs` | Pickup không check distance; claim runtime trước rồi mới move DB item | Nhặt đồ từ xa, DB/runtime lệch nếu DB fail | Check range + transaction/reservation trước khi remove reward |
| High | Validation | `GameServer/Extensions/ServiceCollectionExtensions.cs` | Chỉ vài packet có validator; nhiều handler dùng `!.Value` | Malformed packet gây exception/log incident thay vì trả lỗi sạch | Thêm generic annotation validator cho toàn bộ packet request |
| High | DB race | `GameServer/Services/CraftService.cs` | Validate craft trước transaction, update quantity read-modify-write | Double spend/lost update khi spam/concurrent session | `SELECT ... FOR UPDATE` hoặc atomic update `quantity >= n` |
| High | Memory DoS | `GameServer/Network/ConnectionSession.cs` | Inbound channel unbounded | Client spam làm tăng RAM trước khi xử lý | Bounded queue, max bytes/sec, disconnect khi vượt ngưỡng |

## 4. Detailed Findings

### Architecture

- Layer đã có nhưng chưa sắc: `Network/Handlers` đang orchestration trực tiếp, `Services` vừa query vừa mutate, `Runtime` vừa domain vừa notifier.
- `WorldInterestService` 401 dòng vừa join/leave world, visibility, snapshot, broadcast, observer packets. Nên tách `WorldSnapshotPublisher`, `VisibilityService`, `WorldEventPublisher`.
- `MapInstance.Runtime.cs` và `MonsterEntity.cs` đang ôm spawn, patrol, combat AI, skill due, reward cleanup. Khi thêm boss event/dungeon phase sẽ dễ dẫm chân.

### Network / Packet

- `NetworkServer.OnNetworkReceive` deserialize packet không có try/catch tại biên nhận packet. Malformed binary có thể văng exception trong poll path.
- `PacketSerializer` dùng `MemoryStream`, `ToArray`, `MethodInfo.Invoke` mỗi serialize/deserialize. Với realtime packets sẽ tạo GC pressure.
- `PacketModelSerializer` dùng reflection, `Activator.CreateInstance`, materialize list. Nên thay bằng source-generated serializer cho model hoặc generated delegates.
- `RateLimitMiddleware` chỉ drop sau khi packet đã deserialize và enqueue; dictionary `_lastPacketTicks` không cleanup theo disconnect. Rate limit hiện là giảm handler spam, chưa phải chống DoS.

### Security / Anti-cheat

- Movement chưa authoritative: `CharacterPositionSyncHandler` gọi thẳng `UpdatePosition`.
- Portal travel đặc biệt nguy hiểm vì `TravelToMapHandler` ưu tiên packet pos.
- Pickup reward thiếu range ở `MapInstance.GroundRewards.cs`. Ownership có kiểm tra, nhưng khoảng cách không có.
- Combat range dựa trên `player.Position`; khi position bị client điều khiển, range check không còn đủ an toàn.

### Database

- Không thấy Redis usage. `GameServer.csproj` có `LinqToDB`, `Npgsql`; không có `Dapper`, không có `StackExchange.Redis`.
- `player_items` schema không có index cho query nóng `player_id + location_type + item_template_id` dù `PlayerItemRepository.ListByTemplateIdAsync` dùng liên tục.
- Inventory/craft/equip đang nhiều read-modify-write không lock row. Các flow `ItemService.AddItemAsync`, `MoveGroundItemToInventoryAsync`, `CraftService.ExecuteCraftAsync` cần transaction + row lock.
- Config/catalog load tại boot bằng nhiều `GetAllAsync().GetAwaiter().GetResult()`. Ổn cho boot, nhưng admin chỉnh DB phải restart server mới có hiệu lực.

### Performance / GC

- `MapManager.GetAllInstancesSnapshot()` tạo `List` mỗi tick 50ms. `MapInstance.Events.DrainQueueUnsafe()` tạo `List` mỗi event queue drain.
- `WorldInterestService.RefreshVisibility` dùng snapshot/toHashSet và `TryGetPlayerByCharacterId` O(n) qua toàn bộ online players. Ổn ít người, không ổn MMO.
- `BroadcastToInstancePlayers` gửi packet tới toàn instance, chưa có AOI cho enemies/rewards. Sau này map đông sẽ tốn bandwidth.

### Concurrency

- Per-session inbound processing là single reader, đây là điểm tốt.
- Nhưng duplicate login/reconnect/disconnect có nhiều `.GetAwaiter().GetResult()` trong `NetworkServer`, có thể block network poll.
- Runtime state có dirty flags tốt, nhưng save loop và game loop chạy thread riêng, cần tiếp tục giữ lock ordering rõ khi refactor.

### Logging / Error Handling

- Có `PacketIncidentCapture` tốt, ghi payload base64 + packet JSON khi handler exception.
- `Logger` mở file mới mỗi log và lock global. Với portal spam/metrics/incident nhiều sẽ block threads.
- Một số handler catch `Exception`, gửi UnknownError rồi rethrow. Có lợi cho incident, nhưng sẽ spam log nếu validation thiếu.

## 5. Refactor Roadmap

### Phase 1: Quick Wins

- Movement: server reject delta quá tốc độ: `maxDistance = baseMoveSpeed * dt + grace`.
- Portal: bỏ packet position khỏi validation, chỉ dùng `player.Position`.
- Pickup: thêm distance check với `reward.Position`.
- Packet validation: đăng ký generic validator cho mọi packet có DataAnnotations.
- Network: wrap deserialize bằng try/catch, record invalid packet metric, disconnect nếu lỗi lặp.
- DB: thêm index `player_items(player_id, location_type, item_template_id)`.

### Phase 2: Structural Improvements

- Tách `WorldInterestService` thành snapshot/visibility/event publisher.
- Tách `MapInstance` enemy AI/spawn/reward/skill scheduler thành subsystem nhỏ.
- Tạo `PlayerInventoryTransactionService` cho add/remove/move/consume item với row lock.
- Chuẩn hóa packet handler: handler chỉ parse/session check, action service làm transaction owner.
- Thay `PacketSerializer` reflection invoke bằng generated static delegates.

### Phase 3: Long-term Scalability

- AOI grid cho player/enemy/reward, không broadcast full instance.
- Runtime command/event bus nội bộ cho world events, có replay/debug.
- Redis hoặc distributed cache cho session/resume token/rate limit nếu scale nhiều process.
- Hot-reload catalog theo version admin config, có checksum và rollback.
- Load test packet bandwidth và GC budget theo target CCU.

### Phase 4: Abuse Resistance / Queue Hardening

- Chống client cheat speed rồi spam packet phá queue/server: bounded inbound queue theo connection, giới hạn tổng bytes/packet pending, disconnect hoặc cooldown khi vượt ngưỡng.
- Tách rate limit theo nhóm packet: realtime movement, interaction action, inventory/craft, auth/admin; interaction nên có single-flight hoặc replace/cancel policy rõ.
- Thêm per-player abuse score: spam malformed packet, spam action khi đang chờ movement, spam khi restricted/dead, gửi position target đổi liên tục bất thường.
- Với action cần chờ server position như portal/pickup/attack: chỉ cho một pending interaction tại một thời điểm; request sau có thể bị reject, hoặc hủy request cũ theo policy đã chọn.
- Thêm log/metric để thấy queue depth, pending wait duration, dropped packet count, abuse score theo connection/player.
- Sau này nếu scale nhiều process, đưa rate limit/session abuse state sang Redis hoặc service tập trung.

## 6. Suggested Target Architecture

```text
GameServer/
  Bootstrap/
  Network/
    Packets, Middleware, Handlers, Validation, IncidentReplay
  Application/
    Character, Inventory, Combat, World, Alchemy
  Domain/
    Character, Inventory, Combat, Enemy, Map, Reward
  Runtime/
    WorldLoop, InstanceRuntime, Aoi, Schedulers
  Persistence/
    Repositories, Transactions, Migrations
  Config/
    Catalogs, HotReload, AdminConfigVersioning
```

Pattern đề xuất: packet handler -> application command -> domain service -> repository transaction -> runtime notifier sau commit. Với world runtime realtime, command vào `MapInstance` qua API nhỏ, không để handler chọc nhiều object con.

## 7. Final Verdict

Code hiện tại **không bẩn**, nhưng đang ở giai đoạn "feature chạy được" hơn là "MMO server scale được". Nền tảng có hướng đúng: authoritative server, shared contracts, runtime catalogs, metrics, packet incident.

Rủi ro phình code là có thật, chủ yếu ở `WorldInterestService`, `MapInstance`, `SkillService`, `ItemService`, `CharacterCultivationService`.

Không nên đập đi xây lại. Ưu tiên sửa ngay: **movement/portal/pickup anti-cheat**, **packet validation phủ toàn bộ**, **inventory DB transaction/row lock**, rồi mới dọn architecture.

## 8. Phase 1 Implementation Checklist

Mục tiêu Phase 1 là vá các rủi ro gameplay/security trực tiếp và thêm guard nền tảng, không refactor lớn kiến trúc.

Làm theo thứ tự ưu tiên:

1. Fix portal validation để chỉ dùng server-trusted `player.Position`.
2. Fix pickup reward để bắt buộc check khoảng cách từ `player.Position` tới `reward.Position`.
3. Thêm movement speed clamp server-side: `CharacterPositionSyncHandler` chỉ ghi nhận target client muốn đi tới, `GameLoop` mới cập nhật server position theo tốc độ hợp lệ.
4. Thêm deserialize guard ở `NetworkServer.OnNetworkReceive` để malformed packet không làm văng poll loop.
5. Phủ validation cho các packet request còn thiếu, ưu tiên các packet có input từ client ảnh hưởng runtime/DB.
6. Thêm migration index cho `player_items(player_id, location_type, item_template_id)`.
7. Rà lại các flow inventory/craft có read-modify-write để đánh dấu chỗ cần row lock ở Phase 2.

Không làm trong Phase 1:

- Không refactor lớn `WorldInterestService`.
- Không tách lớn `MapInstance`.
- Không đổi toàn bộ packet architecture.
- Không thêm Redis/AOI.
- Không mở rộng movement protocol sang prediction/reconciliation phức tạp; Phase 1 chỉ dùng desired target nội bộ server để tránh snap correction.

Acceptance criteria:

- Client gửi vị trí giả gần portal nhưng server position còn xa thì server chờ server position đi tới portal theo tốc độ hợp lệ; nếu không tới được trong timeout thì travel bị reject.
- Client đứng xa ground reward hoặc target combat thì server chờ server position đi tới range hợp lệ; nếu không tới được trong timeout thì action bị reject.
- Client gửi position delta vượt quá tốc độ hợp lệ thì server không accept thẳng và không snap client; server position chỉ tiến dần theo base/effective move speed hợp lệ.
- Malformed packet không làm crash/văng network poll loop.
- Packet lỗi có log đủ session/player/packet context để reproduce.
- Migration index chạy được sạch trên DB hiện tại.
- Thay đổi Phase 1 không yêu cầu sửa client protocol trừ khi thật sự cần.

## 9. Progress Tracking

| Phase | Status | Verified By User | Commit | Notes |
|---|---|---|---|---|
| Phase 1: Quick Wins | Completed | No | `3d58b62` | Đã vá portal/pickup/combat target/movement theo hướng server-authoritative desired movement: client speed hack không làm server position chạy nhanh, portal/pickup/attack sẽ chờ server position vào range thay vì snap/reject im lặng. Death sẽ clear movement target, wait bị hủy bởi death không spam failure action, client chỉ force-snap khi chuyển sang defeated. Enemy/boss không còn gây damage im lặng nếu thiếu skill/basic attack. Đã thêm deserialize guard, fallback DataAnnotations validation, thêm/apply `database/migrate_20260427_1200_server_phase1_security_guards.sql`, thêm migration cập nhật mô tả config movement. Build server pass qua output tạm vì DLL runtime đang bị debugger lock; cần user test runtime. Inventory/craft row lock chuyển Phase 2 |
| Phase 1.2: Pending Movement Hazard / Action Ordering | Completed | Yes | - | User đã test khá ngon. Đã thêm `WorldRuntimeSettlementService`, game loop và pre-action portal/pickup/attack dùng chung settlement để drain enemy cast/release/impact/death/reward trước executor. Skill due events được sort theo thời điểm rồi `ExecutionId` để action mới không chen trước action cũ cùng tick. Đã thêm interaction movement catch-up có giới hạn qua `character.position_sync_catchup_multiplier=1.3` và `character.position_sync_catchup_max_seconds=0.75`, chỉ dùng khi wait target interaction, không đổi tốc độ gameplay chính |
| Phase 1.5: World Interaction Pipeline | Completed | Yes | - | User đã test khá ổn. Đã thêm `WorldTargetRef`, `WorldTargetSnapshot`, `WorldTargetResolver`, `WorldInteractionGate`; chuyển `PlayerInteractionMovementWait` sang runtime helper dùng bởi gate. Portal/GroundReward/CombatTarget đều đi qua gate chung cho player state/death/stun/cultivating/practicing/casting, target resolve, range/catch-up/wait, pre-action settlement và recheck trước executor. Handler chỉ còn parse packet, xử lý rule riêng và executor. Build server pass |
| Phase 2.1: Inventory/Equipment Transaction Safety | Completed | No | - | Đã thêm `PlayerInventoryTransactionService` dùng PostgreSQL transaction-level advisory lock theo `playerId`. `ItemService` mutation chính (`add/remove/consume/split/drop/move ground item`) và `EquipmentActionService`/`ItemUseService` đã đi qua gate chung. Build server pass. User chưa test. Lưu ý: ground reward idempotency/cross-player claim recovery vẫn để Phase 2.3 |
| Phase 2.2: Craft Transaction Safety | Completed | No | - | Đã đưa `CraftService.ExecuteCraftAsync`, alchemy practice start, practice cancel refund, alchemy practice completion grant vào `PlayerInventoryTransactionService`. Validation craft/alchemy start nay chạy trong cùng lock/transaction với consume input. Completion re-read active session trong transaction trước khi grant output để tránh double completion. Build server pass. User chưa test |
| Phase 2.3: Reward/Currency/Exp Safety | Completed | No | - | Đã đổi ground reward pickup thành reserve -> DB grant transaction -> complete/cancel, tránh mất reward khỏi runtime nếu DB grant lỗi. Direct enemy item grant được group trong inventory transaction theo player. Enemy cultivation/potential reward flush DB ngay sau khi apply runtime. Build server pass. User chưa test. Chưa thêm durable reward ledger cross-restart; để long-term nếu cần idempotency tuyệt đối |
| Phase 2.4: Repository Cleanup / Transaction Boundary | Completed | No | - | Đã rà transaction owner sau Phase 2.1-2.3, dọn dependency thừa ở `AlchemyService`, ghi rõ transaction ownership matrix. Inventory/equipment/craft/reward item mutation nay đi qua `PlayerInventoryTransactionService`; account/character/skill/herb vẫn là feature-local transaction owner có ghi chú. Build server pass. User chưa test |
| Phase 3: Long-term Scalability | Not Started | No | - | Chỉ làm sau khi gameplay core và architecture chính ổn |
| Phase 4: Abuse Resistance / Queue Hardening | Not Started | No | - | Thiết kế chống client cheat speed x50 rồi spam loạn packet: bounded inbound queue, single-flight interaction wait, per-packet/per-action rate limit, abuse score, disconnect/cooldown policy, metric/log queue depth |

## 9.1. Phase 1.2 Scope: Pending Movement Hazard / Action Ordering

Phase 1.2 xử lý bug ordering giữa `server position đang chạy bù theo tốc độ hợp lệ`, `enemy chủ động tấn công`, và `player action mới` như attack/pickup/portal.

Case lỗi đã thấy khi test:

1. Player chỉ còn 10 HP.
2. Client cheat speed x50 chạy ngang qua quái chủ động có sát thương 30, về logic server-authoritative thì nếu server position đi tới vùng đó player phải chết sau một hit.
3. Trước khi server kịp gửi packet quái tấn công/trừ máu/death, client tiếp tục attack một quái khác.
4. Hiện tại action attack mới có thể success, quái chết và rơi reward trước; sau đó packet bị đánh/trừ máu/chết từ quái trước mới tới sau.

Vấn đề:

- Queue per-connection bảo đảm request xử lý lần lượt, nhưng world runtime/combat AI vẫn chạy song song với handler.
- `PlayerInteractionMovementWait` bảo đảm player server-position đi tới range của action hiện tại, nhưng chưa bảo đảm mọi hazard/death phát sinh trên quãng đường trước đó đã được resolve trước khi executor của action mới chạy.
- Nếu handler trực tiếp execute attack/pickup/portal trong lúc world tick còn pending enemy attack/impact/death, thứ tự gameplay có thể lệch: action sau được chấp nhận trước death đáng lẽ xảy ra trước.

Hướng sửa đề xuất:

1. Thêm bước `PreActionWorldSettle` trước executor của các action có target: portal, pickup, combat skill, NPC sau này.
2. Bước settle phải đảm bảo các pending enemy skill casts/impacts/death transition có hạn xử lý trước hoặc bằng thời điểm action được accept đã được drain/apply trước khi action mới được execute.
3. Nếu trong lúc settle player chuyển sang `CombatDead`/`Defeated`, action hiện tại phải bị hủy im lặng hoặc trả reason death, không được enqueue skill/pickup/travel.
4. Về lâu dài nên chuyển player action realtime thành world command xử lý trong `GameLoop`/`MapInstance` theo thứ tự tick: movement -> enemy AI/hazard -> due impacts/death -> player commands -> broadcasts. Như vậy ordering nằm một chỗ, không phụ thuộc handler chạy trên thread network.
5. Khi làm Phase 1.5, `WorldInteractionGate` phải gọi hoặc sở hữu bước settle này. Nếu Phase 1.5 chỉ gom state/range check mà không settle pending world hazard thì chưa đủ để hết bug này.

Acceptance criteria:

- Với case 10 HP chạy ngang quái sát thương 30 rồi attack quái khác, nếu player đáng lẽ chết trước thì attack sau không được success, không có reward từ quái bị attack sau.
- Client phải nhận presentation combat/death theo thứ tự dễ hiểu: quái tấn công -> trừ máu -> death snap/popup; không có skill success/reward chen trước death.
- Portal/pickup/attack đều dùng cùng rule settle trước executor, không chỉ fix riêng `AttackEnemyHandler`.
- Có log debug đủ để xác nhận action bị hủy vì `CharacterDefeated` trong pre-action settle.

## 9.2. Phase 1.5 Scope: World Interaction Pipeline

Phase 1.5 là bước dọn đúng phần đang bắt đầu lặp sau Phase 1: portal, pickup reward, attack/skill target đều có cùng nhóm rule `target tồn tại`, `cùng map/instance`, `player còn được hành động`, `range hợp lệ`, `có cần chờ server position vào range không`, `action bị hủy nếu chết/stun/map đổi`.

Mục tiêu:

1. Thêm `WorldTargetRef` làm identity chung cho target tương tác: `Player`, `Enemy`, `Boss`, `Npc`, `Portal`, `GroundReward`, `GroundPoint`.
2. Thêm `WorldTargetSnapshot` làm snapshot server-side chung: kind, position, map/instance/zone, alive/interactable, owner/permission metadata nếu có.
3. Thêm `WorldTargetResolver` để resolve target từ ref thay vì từng handler tự tìm portal/reward/enemy/player.
4. Thêm `WorldInteractionGate` để gom rule chung trước khi tương tác: player state, death, stun, cultivating/practicing/casting, map/instance, range, ownership/permission.
5. `WorldInteractionGate` phải tích hợp hoặc gọi bước Phase 1.2 `PreActionWorldSettle`, để mọi pending hazard/death được resolve trước khi executor chạy.
6. Giữ executor nghiệp vụ riêng cho từng action: portal travel, pickup reward, combat skill, NPC dialog sau này.
7. `PlayerInteractionMovementWait` chuyển thành helper dùng bởi gate/pipeline, không để từng handler gọi trực tiếp lâu dài.

Không làm trong Phase 1.5:

- Không đổi toàn bộ packet protocol nếu chưa cần.
- Không gom executor thành một god service.
- Không refactor lớn combat skill effect pipeline.
- Không thêm hệ thống quest/dialog đầy đủ.

Acceptance criteria:

- Portal, ground reward, attack/skill target đều đi qua cùng interaction gate cho các rule chung.
- Thêm một rule chung mới trước khi tương tác target chỉ cần sửa ở gate/pipeline, không sửa từng handler.
- Handler chỉ còn parse packet, gọi pipeline/executor, trả result.
- Death/stun/map đổi trong lúc chờ tương tác hủy action nhất quán.
- Test lại case Phase 1.2: cheat speed chạy ngang quái đủ damage để chết rồi attack quái khác; action sau phải bị hủy nếu death đáng lẽ xảy ra trước.
- Target-specific rule vẫn nằm ở executor hoặc resolver tương ứng, không nhồi hết vào gate.

## 9.3. Enemy/Boss Attack Rule

Rule đã chốt cho combat presentation và server authority:

- Enemy/Boss chỉ được gây damage qua skill execution pipeline.
- Mỗi enemy/boss muốn tấn công phải có ít nhất một skill/basic attack mặc định trong data.
- Nếu không resolve được skill hợp lệ thì enemy/boss không gây damage; server chỉ log cảnh báo seed/config thiếu.
- Không dùng nhánh basic attack im lặng gây damage trực tiếp lên player.
- Client phải nhận packet hành động combat trước damage/death, ví dụ `SkillCastStartedPacket`, rồi mới tới impact/state/death packet.

Lý do: tránh case player bị trừ máu hoặc chết mà client không có presentation trước đó để giải thích vì sao bị đánh.

## 9.4. Phase 2 Scope: Structural Transaction Safety

Phase 2 không phải refactor lớn toàn server. Mục tiêu là đóng các lỗ read-modify-write trước, rồi mới dọn module. Nếu bị gián đoạn, session sau làm tiếp theo đúng thứ tự trong `Progress Tracking`.

### Phase 2.1: Inventory/Equipment Transaction Safety

Mục tiêu:

1. Thêm một transaction/gate chung theo `playerId` cho mutation inventory/equipment.
2. `equip`, `unequip`, `drop inventory item`, `use item`, `add/remove/consume item`, `split stack`, `move ground item to inventory` phải đi qua gate này hoặc được ghi rõ vì sao chưa thể.
3. Transaction boundary nằm ở action/service layer, không để handler tự chắp nhiều repository call.
4. Re-check item owner/location/quantity/equipped state bên trong transaction/gate, không chỉ validate trước transaction.
5. Không đổi packet protocol và không refactor UI/client.

Acceptance criteria:

- Hai request song song cùng player không thể consume/drop cùng một stack vượt quá số lượng thật.
- Không thể equip một item vừa bị drop/remove/consume bởi request khác.
- Equip/unequip không để nhiều item cùng chiếm một slot do race trong cùng player.
- Build server pass.
- Nếu còn flow item chưa đưa vào gate, phải ghi rõ ở note Phase 2.1.

Kết quả đã làm:

- Thêm `PlayerInventoryTransactionService` để mở transaction khi cần và gọi `pg_advisory_xact_lock` theo `playerId`.
- `ItemService` public mutation chính đã wrap qua transaction/gate: add, remove, consume, split stack, move inventory item to ground, move ground item to inventory.
- `EquipmentActionService` không tự mở transaction rời rạc nữa; equip/unequip/equip-first-available đi qua gate chung.
- `ItemUseService.UseAsync` wrap toàn bộ use item vào gate để martial art book, pill recipe book, consumable và use-equipment không validate/consume rời rạc.
- Chưa xử lý triệt để idempotency reward/currency/exp và recovery nếu runtime ground reward đã claim nhưng DB grant lỗi; phần đó thuộc Phase 2.3.

### Phase 2.2: Craft Transaction Safety

Mục tiêu:

1. Rà `CraftService`, `AlchemyCraftActionService`, `AlchemyPracticeService`, `PracticeService`.
2. Validation craft chỉ là preview; khi execute phải re-check item quantity/owner/location/equipped state trong transaction/gate.
3. Consume input, roll success/mutation, create output, update mastery/progress phải có transaction owner rõ.
4. Không để craft validate xong rồi item bị drop/use trước khi consume.

Acceptance criteria:

- Spam craft/use/drop cùng lúc không tạo output nếu input đã bị consume.
- Craft fail vẫn consume đúng input theo rule, không mất/cộng thừa item.
- Log lỗi đủ recipeId/playerId/input item để debug.

Kết quả đã làm:

- `CraftService.ExecuteCraftAsync` wrap validation, consume input, roll success/mutation, create output và build result trong cùng `PlayerInventoryTransactionService`.
- `AlchemyCraftActionService.StartCraftAsync` wrap recipe detail, blocking-session recheck, validate input, consume input và create practice session trong cùng inventory transaction theo player.
- `PracticeService.CancelAsync` wrap cancel session và refund consumed input trong cùng inventory transaction.
- `AlchemyPracticeService.CompleteSessionIfDueAsync` re-read active session trong inventory transaction trước khi roll/grant/update session/notification, tránh hai completion song song grant item hai lần.
- Phần reward/currency/exp idempotency tổng quát, recovery khi DB lỗi giữa runtime reward và DB grant vẫn thuộc Phase 2.3.

### Phase 2.3: Reward/Currency/Exp Safety

Mục tiêu:

1. Rà enemy reward, ground reward pickup, item reward, currency/exp/stat delta.
2. Gom reward grant vào service có idempotency hoặc transaction owner rõ.
3. Tránh retry hoặc packet duplicate làm nhận reward hai lần.
4. Đồng bộ runtime reward và DB item/currency theo thứ tự recover được nếu lỗi giữa chừng.

Acceptance criteria:

- Một reward chỉ claim được một lần.
- Nếu DB grant lỗi, runtime không mất reward im lặng hoặc có log/recovery rõ.
- Currency/exp/item reward không bị update rời rạc dẫn tới nửa thành công nửa thất bại.

Kết quả đã làm:

- Ground reward pickup chuyển từ `TryClaimGroundReward` remove-ngay sang 2 bước: `TryBeginGroundRewardClaim` reserve runtime, DB move item vào inventory trong `PlayerInventoryTransactionService`, rồi `CompleteGroundRewardClaim`.
- Nếu DB grant pickup lỗi, handler gọi `CancelGroundRewardClaim`, log lỗi và reward vẫn còn trong runtime để retry hoặc despawn theo timer; không bị mất im lặng.
- Thêm trạng thái claim in-progress trên `GroundRewardEntity` và `MessageCode.GroundRewardClaimInProgress` để packet trùng/claim song song không nhận reward hai lần.
- Direct enemy item reward được gom theo item/bound và grant trong một inventory transaction theo player thay vì mỗi roll là một transaction rời.
- Cultivation/potential reward từ enemy death sau khi apply runtime sẽ gọi `CharacterRuntimeSaveService.FlushPlayerAsync` ngay để giảm nguy cơ mất tiến độ nếu server crash trước kỳ periodic save.
- Chưa thêm durable reward ledger/idempotency key cross-restart cho enemy death runtime event. Nếu sau này cần chống duplicate tuyệt đối qua crash/retry nhiều process, đưa vào Phase 3/4 bằng reward ledger hoặc Redis/idempotency store.

### Phase 2.4: Repository Cleanup / Transaction Boundary

Mục tiêu:

1. Repository chỉ còn query/insert/update/delete, không chứa nghiệp vụ.
2. Action service/application service là nơi sở hữu transaction.
3. Giảm service gọi chéo tạo transaction lồng nhau khó hiểu.
4. Đặt naming rõ: `QueryService` cho đọc, `ActionService` cho mutation.

Acceptance criteria:

- Nhìn một packet handler biết transaction owner nằm ở service nào.
- Không có mutation flow quan trọng mở transaction ở nhiều tầng nếu không có lý do rõ.
- Những flow còn nợ phải có TODO/docs cụ thể, không để mơ hồ.

Kết quả đã làm:

- Dọn dependency thừa ở `AlchemyService`: service này chỉ còn query/validation recipe input, không nhận `GameDb`, `ItemService`, `IGameRandomService` khi không dùng.
- Sau Phase 2.1-2.3, transaction owner hiện tại:
  - `PlayerInventoryTransactionService`: inventory/equipment/use item/craft/alchemy input/refund/completion item grant/ground reward pickup/direct enemy item reward.
  - `AccountService`: transaction account + credential khi register/link credential. Đây là account-domain owner, không liên quan runtime game.
  - `CharacterService`: transaction character create và update runtime snapshot; đây là character-domain owner.
  - `SkillService`: skill/loadout/equipment-granted-skill sync. Có check ambient transaction (`_db.Transaction`) để khi gọi từ equipment flow thì không mở transaction lồng thêm.
  - `HerbService`: garden/herb flow vẫn là feature-local transaction owner. Các điểm gọi `ItemService` sẽ dùng ambient DB transaction + inventory advisory lock. Chưa tách thành `HerbActionService` trong Phase 2 vì ngoài phạm vi inventory/craft/reward test hiện tại.
- Repository hiện tại vẫn chỉ làm data access trực tiếp qua `GameDb`; chưa thấy repository chứa business rule lớn cần tách ngay trong Phase 2.
- Nợ sau Phase 2 nếu muốn dọn tiếp: tách `HerbService` thành query/action, tách `SkillService` query/action, và tạo base transaction helper chung nếu ngoài inventory cũng bắt đầu cần advisory lock/domain lock.

## 9.5. Phase 4 Scope: Abuse Resistance / Queue Hardening

Phase 4 xử lý nhóm vấn đề không phải correctness gameplay đơn lẻ, mà là client cố tình phá server bằng cheat speed, spam packet, đổi target liên tục, hoặc tạo hàng đợi dài để tiêu tốn RAM/CPU.

Mục tiêu:

1. Đổi inbound queue từ unbounded sang bounded theo connection. Khi queue vượt ngưỡng thì drop packet không quan trọng, throttle, hoặc disconnect tùy severity.
2. Thêm giới hạn pending bytes/pending packet count theo connection để tránh DoS bộ nhớ trước khi handler kịp xử lý.
3. Chuẩn hóa rate limit theo traffic class: realtime movement nhẹ hơn, business action nghiêm hơn, auth/admin nghiêm nhất.
4. Với các interaction có `PlayerInteractionMovementWait`, áp dụng single-flight: một player chỉ có một pending interaction wait. Request mới trong lúc đang wait phải có policy rõ: reject ngay, replace request cũ, hoặc cancel request cũ.
5. Thêm abuse score theo player/connection: malformed packet, packet quá nhanh, action khi dead/restricted, đổi movement target quá dày, spam action trong lúc wait.
6. Thêm progressive penalty: log -> throttle -> temporary action lock -> disconnect. Không ban cứng ở bước đầu.
7. Ghi metric/log đủ để debug: queue depth, oldest pending age, dropped packet count, active wait target, wait duration, abuse score, packet type gây spam.
8. Nếu nhiều process/server shard, chuyển rate limit và abuse state quan trọng sang Redis hoặc service tập trung để reconnect không reset sạch abuse state.

Acceptance criteria:

- Client spam packet không làm RAM tăng không giới hạn.
- Một player không thể tạo nhiều interaction wait chạy song song hoặc queue hàng loạt action đắt tiền.
- Khi đang chờ portal/pickup/attack, request sau được xử lý theo policy nhất quán và có log.
- Có metric nhìn được connection nào đang spam và packet nào gây tải.
- Behavior bình thường của player lag thật không bị phạt quá tay.

## 10. Session Handoff Rule

Khi một session sau bắt đầu làm theo report này, trước tiên phải đọc mục `Progress Tracking` để biết phase nào đã xong và phase tiếp theo là gì.

Khi hoàn thành một phase, Codex phải tự cập nhật file này, không chờ user tự đánh dấu:

1. Đổi `Status` của phase vừa làm từ `Not Started` hoặc `In Progress` sang `Completed`.
2. Nếu user đã test và nói ổn, đổi `Verified By User` sang `Yes`.
3. Ghi commit hash vào cột `Commit` nếu code đã được commit.
4. Ghi note ngắn ở cột `Notes`: đã sửa gì chính, còn rủi ro nào, hoặc phase sau cần chú ý gì.
5. Nếu phase mới làm một phần, để `Status` là `In Progress` và ghi rõ phần đã xong/phần còn lại trong `Notes`.

Quy ước status:

- `Not Started`: chưa bắt đầu phase.
- `In Progress`: đã sửa một phần nhưng chưa đủ acceptance criteria.
- `Completed`: đã hoàn thành acceptance criteria của phase.
- `Blocked`: đang bị chặn bởi lỗi, thiếu dữ liệu, thiếu quyết định thiết kế, hoặc cần user xác nhận.

Quy tắc quan trọng: session sau không được đoán phase hiện tại chỉ dựa vào git diff. Phải ưu tiên đọc `Progress Tracking`, sau đó mới kiểm tra git log/diff/code để xác nhận trạng thái thực tế.
