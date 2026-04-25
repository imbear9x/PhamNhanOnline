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
3. Thêm movement speed validation server-side cho `CharacterPositionSyncHandler`.
4. Thêm deserialize guard ở `NetworkServer.OnNetworkReceive` để malformed packet không làm văng poll loop.
5. Phủ validation cho các packet request còn thiếu, ưu tiên các packet có input từ client ảnh hưởng runtime/DB.
6. Thêm migration index cho `player_items(player_id, location_type, item_template_id)`.
7. Rà lại các flow inventory/craft có read-modify-write để đánh dấu chỗ cần row lock ở Phase 2.

Không làm trong Phase 1:

- Không refactor lớn `WorldInterestService`.
- Không tách lớn `MapInstance`.
- Không đổi toàn bộ packet architecture.
- Không thêm Redis/AOI.
- Không đổi movement protocol sang full intent-based nếu chưa có thời gian test kỹ.

Acceptance criteria:

- Client gửi vị trí giả gần portal nhưng server position còn xa thì travel bị reject.
- Client đứng xa ground reward thì pickup bị reject.
- Client gửi position delta vượt quá tốc độ hợp lệ thì server reject hoặc correction, không accept thẳng.
- Malformed packet không làm crash/văng network poll loop.
- Packet lỗi có log đủ session/player/packet context để reproduce.
- Migration index chạy được sạch trên DB hiện tại.
- Thay đổi Phase 1 không yêu cầu sửa client protocol trừ khi thật sự cần.

## 9. Progress Tracking

| Phase | Status | Verified By User | Commit | Notes |
|---|---|---|---|---|
| Phase 1: Quick Wins | Not Started | No | - | Ưu tiên anti-cheat movement/portal/pickup, packet guard, packet validation, DB index |
| Phase 2: Structural Improvements | Not Started | No | - | Chỉ làm sau khi Phase 1 đã chạy ổn |
| Phase 3: Long-term Scalability | Not Started | No | - | Chỉ làm sau khi gameplay core và architecture chính ổn |

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
