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
| Phase 2: Structural Improvements | Not Started | No | - | Chỉ làm sau khi Phase 1 đã chạy ổn |
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

## 9.4. Phase 4 Scope: Abuse Resistance / Queue Hardening

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
