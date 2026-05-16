# Death Penalty — Tech Design Spec

Status: Draft / Ready for Dev Handoff
Owner: TechDesign
Last updated: 2026-05-15
Source requirement: `docs/game-design-wp/requirements/death-penalty.md`

---

## 1. Goal

Thiết kế server/runtime cho hệ thống death penalty áp dụng đồng nhất cho mọi nguyên nhân chết:
- drop linh thạch
- drop tối đa 1 item droppable
- clear buff skill
- giữ buff bùa chú / trận pháp
- penalty thọ nguyên (realm 1–18) hoặc rút ngắn countdown lôi kiếp (realm 19+)
- hồi sinh
- permanent death khi thọ nguyên về 0

Spec này bám requirement hiện tại, nhưng tận dụng tối đa runtime đã có sẵn để tránh phát minh flow mới không cần thiết.

---

## 2. Repo Grounding Summary

## 2.1 Runtime hiện có

### Đã tồn tại
1. **Combat-dead runtime state**
   - `CharacterRuntimeStateCodes.CombatDead`
   - `CharacterRuntimeService.NotifyDeathTransitionIfNeeded(...)`
   - Client đã có signal state transition reason `CombatDead`

2. **Respawn tối thiểu về home**
   - Packet: `ReturnHomeAfterCombatDeathPacket` `[73]`
   - Result: `ReturnHomeAfterCombatDeathResultPacket` `[74]`
   - Service: `CharacterCombatDeathRecoveryService`
   - Hiện tại chỉ support **1 đường hồi sinh về home/default spawn**

3. **Ground reward runtime dùng chung**
   - `GroundRewardEntity` là **in-memory runtime entity**, không phải DB row
   - Có owner window qua `OwnerCharacterId`, `FreeAtUtc`, `DestroyAtUtc`
   - Đã có pickup claim flow riêng

4. **Item schema đã có cờ drop/trade**
   - `ItemTemplateEntity.is_droppable`
   - `ItemTemplateEntity.is_tradeable`

5. **Lifespan runtime nền đã tồn tại**
   - `realm_templates.lifespan`
   - `character_base_stats.lifespan_bonus`
   - `CharacterLifespanRules`
   - `CharacterLifecycleService`

6. **Lifespan expired state đã tồn tại**
   - `CharacterRuntimeStateCodes.LifespanExpired`
   - notification + action restriction hiện đã có

### Chưa tồn tại / chưa thấy evidence đầy đủ
1. death penalty hook đầy đủ vào combat death
2. auto drop linh thạch khi chết
3. auto drop item khi chết
4. clear buff skill on death
5. checkpoint respawn option
6. permanent death delete/archive flow
7. config-driven additional penalty per death context
8. tribulation countdown penalty hook

---

## 3. Key Conflict With Current Runtime

Requirement mới nói:
- hết thọ nguyên do death penalty => **permanent death**
- permanent death **không thể hoàn tác**

Nhưng runtime hiện tại mới có:
- `LifespanExpired` state
- gửi notification
- chặn action / world access
- **chưa có flow archive/xóa nhân vật**

=> Đây là gap authority lớn nhất. TechDesign không được giả vờ state hiện tại đã đáp ứng permanent death requirement.

---

## 4. Design Approach

## 4.1 Reuse boundary

Không tạo hệ death state mới từ đầu nếu runtime hiện có đã đủ:
- dùng `CombatDead` làm death entry state
- mở rộng death resolution pipeline ngay sau lúc HP về 0
- reuse ground reward runtime để spawn đồ rơi
- reuse `ReturnHomeAfterCombatDeath` làm **Option A: Về Động Phủ / default spawn**
- bổ sung option checkpoint bằng packet/result mở rộng mới nếu map system support đủ data

## 4.2 Authority split

### Baseline death penalty
Áp dụng cho mọi death context:
- roll drop linh thạch
- roll drop 1 item
- clear buff skill
- apply lifespan/tribulation penalty

### Additional penalty
- hoàn toàn config-driven theo `death_context_type`
- không hardcode branch kiểu `if pk then ...` trong service logic

---

## 5. Proposed Runtime Architecture

## 5.1 New service boundary

### `DeathPenaltyService`
Service orchestration chính cho 1 lượt chết player.

**Responsibilities**
- nhận death context + snapshot player tại thời điểm chết
- mở inventory/character transaction phù hợp
- resolve drop linh thạch
- resolve item drop pool
- mutate inventory/currency
- apply lifespan hoặc tribulation penalty
- trả về `DeathPenaltyResolution`

**Không làm**
- không trực tiếp gửi packet network
- không giữ world runtime state
- không tự persist map reward vào DB

### `DeathRespawnService` hoặc mở rộng `CharacterCombatDeathRecoveryService`
Phần này nên tách khỏi penalty logic.

**Responsibilities**
- kiểm tra option respawn hợp lệ
- recover player về home/checkpoint
- restore HP/MP ratio theo config đang có
- publish world snapshot/state sync sau respawn

### `CharacterBuffResetService` (mới hoặc hook vào combat status runtime)
- clear **skill-origin buffs/debuffs** khi death resolve
- giữ external effects không thuộc skill runtime

## 5.2 Repository / DAO boundary

### Reuse existing repos/services
- `CharacterService`
- `ItemService`
- `BagService`
- các runtime save services hiện có

### New config/data access needed
- `death_penalty_context_configs`
- có thể thêm catalog/service đọc config này thành runtime dictionary

Không cần DAO mới cho ground reward vì reward là in-memory runtime object giống flow drop hiện tại.

---

## 6. Data Model / Schema Changes

## 6.1 Config table: `death_penalty_context_configs`

Mục đích: baseline + additional penalty theo death context.

### Proposed columns
- `id` bigint PK
- `death_context_type` varchar / int enum unique
- `drop_lingstone_rate` decimal/null
- `drop_lingstone_pct_min` decimal/null
- `drop_lingstone_pct_max` decimal/null
- `drop_lingstone_min_amount` int/null
- `drop_lingstone_max_amount` int/null
- `drop_item_rate` decimal/null
- `lifespan_penalty_seconds` int/null
- `tribulation_penalty_seconds` int/null
- `additional_drop_item_rolls` int default 0
- `additional_lingstone_pct_bonus` decimal default 0
- `additional_lifespan_penalty_seconds` int default 0
- `additional_tribulation_penalty_seconds` int default 0
- `created_at`
- `updated_at`

### Semantics
- row `baseline` luôn tồn tại
- row context-specific (pve/pk/raid/...) có thể override/add thêm trên baseline
- code merge config theo rule:
  - baseline trước
  - context override/addition sau

## 6.2 Character permanent death tracking

Tùy quyết định GD/User ở mục blocker:

### Option A — Archive preferred
Thêm cờ / metadata:
- `characters.deleted_at` nullable
- `characters.delete_reason` nullable enum/string
- `characters.is_archived` bool

Ưu điểm:
- audit tốt
- QA/debug tốt
- tránh mất dữ liệu vĩnh viễn do bug

### Option B — Hard delete
- xóa row character và data liên đới

**TechDesign preference: Option A (archive)**
trừ khi user bắt buộc hard delete.

## 6.3 Checkpoint respawn support

Hiện repo chưa thấy checkpoint runtime/model rõ ràng. Nếu map chưa có checkpoint active model, cần thêm:
- map checkpoint config table hoặc reuse portal/spawn-point system
- player runtime field ghi checkpoint active gần nhất nếu feature cần persistence

Hiện điểm này còn **blocked by code grounding + GD detail**.

---

## 7. Core Resolution Flow

## 7.1 Death entry

Trigger tại thời điểm player chuyển từ non-dead -> `CombatDead`.

### Current hook candidate
`CharacterRuntimeService.NotifyDeathTransitionIfNeeded(...)`

Nhưng method này hiện chỉ notify packet transition. Không nên nhét toàn bộ business vào đây trực tiếp.

### Preferred hook
- thêm orchestrator gọi sau khi snapshot chuyển sang `CombatDead`
- bảo đảm chỉ xử lý **1 lần** cho mỗi death transition
- tránh duplicate nếu `ApplyDamage` và `ApplyResourceDelta` đều đi qua cùng state

## 7.2 Death penalty transaction

Pseudo flow:
1. capture player + map instance + death context
2. bắt đầu inventory/character mutation boundary
3. roll linh thạch nếu có
4. trừ linh thạch từ inventory/currency nguồn
5. chọn pool item droppable từ inventory/equipment theo rule scope được chốt
6. nếu trúng, random đúng 1 item
7. remove item/currency khỏi character
8. apply lifespan penalty hoặc tribulation penalty
9. clamp floor 0
10. clear skill buffs
11. commit transaction
12. sau commit mới spawn ground reward runtime entity vào map
13. nếu lifespan về 0 => trigger permanent death flow thay vì cho respawn thường

## 7.3 Why spawn after commit

Để tránh tình huống:
- map đã thấy đồ rơi
- nhưng inventory/character DB mutation fail rollback

Ground reward phải là **post-commit runtime side effect** từ committed mutation result.

---

## 8. Drop Rules

## 8.1 Lingstone drop

### Source of truth
Cần xác nhận linh thạch hiện được lưu kiểu nào trong inventory/currency service, nhưng về authority runtime:
- roll theo config
- percent random trong `[pct_min, pct_max]`
- clamp tiếp bằng `[min_amount, max_amount]`
- nếu player không có linh thạch thì drop = 0

### Atomicity
Drop linh thạch và item drop phải cùng death penalty mutation boundary với lifespan penalty.

Nếu commit fail => không spawn gì cả.

## 8.2 Item drop pool

### Eligibility
- `ItemTemplate.IsDroppable == true`
- `ItemTemplate.IsTradeable == true` hoặc theo requirement tối thiểu: non-tradable không drop
- item chưa expired
- quantity > 0

### Scope question
Requirement nói “trên người player”. Trong codebase hiện tại cần chốt rõ có gồm:
- inventory items
- equipment items
- equipped talisman

**TD default đề xuất:**
- gồm **inventory + equipped items** nếu template droppable
- nhưng không drop item đang là runtime extension critical ownership row nếu hệ đó không safe để detach

Nếu cần, item-service side sẽ expose helper trả `droppable candidates` thay vì TD tự suy luận bằng raw query nhiều nơi.

### Output
- tối đa 1 item mỗi death
- stackable item: drop 1 stack instance đang có, không tách partial nếu requirement không yêu cầu

## 8.3 Ground reward ownership

Reuse shared ground reward runtime:
- `OwnerCharacterId = dead player.CharacterId`
- `FreeAtUtc` lấy từ **shared ownership config source duy nhất**
- `DestroyAtUtc` theo shared free-for-all destroy window

### Important
Requirement nói `death.priority_window_seconds`, nhưng handoff đã chốt phải dùng **1 config source chung với Ownership/Drop Rights shared rule**.

=> **Không tạo config key death riêng cho ownership window** nếu shared rule đã có key/canonical source.

---

## 9. Buff Clear Scope

## 9.1 Current grounding
`CombatStatusCollection` hiện lưu 3 loại runtime combat effect:
- shield list
- stat modifier list
- stun timestamp

Container hiện **chưa có source tagging** để phân biệt:
- skill
- talisman
- formation
- external/future systems

## 9.2 Authority decision
Chọn **Option B**.

Dev **bắt buộc** thêm origin/source boundary vào `CombatStatusCollection` trong scope death-penalty này. Không chấp nhận clear-all mơ hồ, vì authority gameplay đã chốt là:
- clear buff/debuff từ **skill** khi chết
- giữ buff từ **bùa chú / trận pháp**

## 9.3 Minimum tagging boundary required
Tối thiểu phải tag source cho toàn bộ state có thể bị death-clear:
- shield
- stun
- stat modifier

### Proposed enum
- `Skill`
- `Talisman`
- `Formation`
- `External`

## 9.4 Clear contract
Death resolution chỉ clear combat status entries có `source_type == Skill`.

### Important
- Không clear `Talisman`
- Không clear `Formation`
- Không clear `External`
- Reviewer phải verify mọi path add shield/stun/stat-modifier đều ghi source type đúng

---

## 10. Lifespan / Tribulation Penalty

## 10.1 Realm 1–18

Penalty mutate vào lifespan representation hiện có:
- codebase hiện đang tính lifespan bằng `realm lifespan + lifespan_bonus`
- vì vậy penalty đơn giản nhất là mutate `lifespan_bonus` âm dần

### Proposed rule
- `effective_max_lifespan_days = realm_templates.lifespan + lifespan_bonus_days`
- death penalty seconds convert ra day-equivalent/unit tương ứng system hiện dùng
- store mutation tại field bonus hoặc field dedicated delta mới

### Tech note
Nếu `lifespan_bonus` hiện đang được dùng cho cả “bonus từ tạo char / item consume”, dùng chung field để trừ penalty vẫn chạy được nhưng khó audit.

**TD preference:** thêm field riêng nếu muốn tách semantic sạch:
- `lifespan_penalty_seconds_accumulated`

Tuy nhiên nếu muốn minimal schema delta, có thể reuse `lifespan_bonus` với convention rõ ràng. Cần Dev audit nơi nào assume bonus luôn không âm.

## 10.2 Realm 19+

Requirement yêu cầu rút ngắn countdown lôi kiếp.

Dev audit đã xác nhận repo hiện **chưa có source-of-truth rõ ràng** cho countdown này. Vì vậy TD chốt luôn canonical storage/hook như sau.

### Authority decision — canonical storage
Dùng **field persisted mới**: `next_tribulation_at_utc`.

### Preferred location
Thêm vào `character_current_states` / `CharacterCurrentState`.

### Why this shape
- countdown là giá trị time-based lifecycle của từng character
- dễ tính remaining seconds bằng `next_tribulation_at_utc - utcNow`
- dễ clamp về 0 bằng cách set timestamp <= now
- dễ tích hợp world-entry / login / background checks hơn so với chỉ lưu `remaining_seconds`

### Penalty contract
Khi realm 19+ chết:
1. đọc `next_tribulation_at_utc`
2. trừ `death.tribulation_penalty_seconds`
3. nếu timestamp mới <= `utcNow`, set bằng `utcNow`
4. persisted source-of-truth luôn là timestamp mới

### Trigger hook when reaches zero
Khi penalty làm `next_tribulation_at_utc <= utcNow`:
- **không** trigger tribulation battle inline trong death-penalty transaction
- chỉ persist về `utcNow`
- giao cho **Tribulation runtime follow-up service** / existing tribulation entry hook xử lý trigger khi character next vào đúng lifecycle check point

### Defer decision
Vì hệ tribulation runtime trigger đầy đủ chưa được grounding ở repo hiện tại, `#37` được phép:
- implement **storage + penalty mutation + floor-to-now contract** ngay bây giờ
- tạo **follow-up handoff** cho tribulation trigger integration nếu repo thật sự chưa có execution hook

Tức là:
- **không block toàn bộ death-penalty slice** vì thiếu full tribulation runtime
- nhưng phải giao nợ kỹ thuật rõ ràng bằng follow-up riêng nếu trigger hook chưa tồn tại

---

## 11. Permanent Death Flow

## 11.1 Authority
Khi apply death penalty làm lifespan về 0:
- không cho respawn thường
- nhân vật đi vào `permanently_dead`
- không thể hoàn tác

## 11.2 Proposed server handling

### Preferred archive flow
1. lock character login/world entry
2. mark character archived/deleted reason = `PermanentDeath`
3. detach online session nếu đang online
4. gửi notification / terminal packet
5. client quay về character screen hoặc create-character gate

### Why archive preferred
- giữ audit log
- hỗ trợ debug/review
- tránh hard-delete cascade rủi ro

## 11.3 Name reclaim
Điểm này là **GD/user decision**. TD không tự quyết vì ảnh hưởng economy/social identity.

---

## 12. Respawn Flow

## 12.1 Current runtime
Hiện chỉ có:
- `ReturnHomeAfterCombatDeathPacket`
- recover về home/default spawn

## 12.2 Required target
Requirement cần 2 option:
- `ReturnHome`
- `ReturnCheckpoint` nếu map có checkpoint active

## 12.3 TD proposal

### Phase structure
- giữ packet cũ làm `ReturnHome`
- thêm packet/result mới cho checkpoint, ví dụ:
  - `ReturnCheckpointAfterCombatDeathPacket`
  - `ReturnCheckpointAfterCombatDeathResultPacket`

### Validation
- chỉ cho gọi khi current state = combat dead
- current map template hỗ trợ checkpoint respawn
- player có checkpoint active hợp lệ

### Fallback
- nếu không có checkpoint, client chỉ hiện option home
- nếu player chưa có home cave, reuse default-home config như requirement

---

## 13. Validation Rules

1. death penalty chỉ resolve 1 lần cho mỗi death transition
2. item non-droppable/non-tradable không vào pool
3. item expired không vào pool
4. floor lifespan / tribulation tại 0
5. ground reward chỉ spawn sau commit thành công
6. permanent death chặn respawn packet thường
7. ownership window dùng shared config source duy nhất
8. additional penalty config thiếu row context => fallback baseline only

---

## 14. Logging / Telemetry

Log bắt buộc:
- death event resolved: `player_id`, `character_id`, `death_context`, `realm`, `lingstone_drop`, `item_drop`, `lifespan_before/after`, `tribulation_before/after`
- buff clear count + source type summary
- permanent death triggered
- respawn destination chosen
- config resolution source (baseline/context override)

---

## 15. Test Plan

## 15.1 Unit tests
1. roll linh thạch clamp min/max đúng
2. no lingstone => drop 0
3. item pool excludes non-droppable/non-tradable/expired
4. chỉ drop tối đa 1 item
5. floor lifespan tại 0
6. floor tribulation tại 0
7. baseline + additional penalty merge đúng
8. permanent death when resulting lifespan == 0

## 15.2 Service/integration tests
1. death resolve commit thành công => inventory/currency giảm + ground reward spawn
2. commit fail => không spawn reward
3. owner-only pickup window đúng theo shared runtime
4. combat dead -> return home thành công với path cũ
5. permanent death -> respawn packet fail
6. disconnect while combat dead -> current recovery path vẫn consistent

## 15.3 Regression tests
1. current `ReturnHomeAfterCombatDeath` không bị vỡ
2. current ground reward pickup không bị broad regression
3. current lifespan-expired login restriction không bị vỡ
4. item drop manual flow (`DropInventoryItemHandler`) không bị ảnh hưởng

---

## 16. Authority Decisions Confirmed

## 16.1 Lifespan elixir cap
**Confirmed by user:** **không cap** theo pool cảnh giới hiện tại.

Meaning:
- pool cảnh giới vẫn là mốc baseline cho tăng trưởng khi đột phá
- nhưng item/đan dược tăng thọ nguyên có thể đẩy tổng thọ nguyên hiện tại vượt pool cảnh giới

### Example
- pool cảnh giới hiện tại: 30 ngày
- player còn 25 ngày
- uống đan +10 ngày
- kết quả mới = **35 ngày**

## 16.2 Warning threshold
**Confirmed by user:** warning threshold cố định = **10 ngày**.

Áp dụng cho:
- lifespan warning
- tribulation imminent warning (trừ khi GD/User đổi authority riêng về sau)

### Config form
Đề xuất seed/config:
- `death.lifespan_warning_threshold_seconds = 864000`
- `death.tribulation_warning_threshold_seconds = 864000`

## 16.3 Permanent death data policy
**Confirmed by user:** flow 2 bước.

### Flow authority
1. death penalty làm thọ nguyên về 0
2. character bị **lock/chặn đăng nhập vào world**
3. mỗi lần đăng nhập lại, player luôn thấy thông báo: hết thọ nguyên, vui lòng tạo nhân vật mới
4. nếu player **chưa bấm OK** mà thoát game, lần sau vào lại vẫn bị chặn và hiện lại thông báo
5. chỉ khi player **bấm OK xác nhận** thì server mới:
   - xóa hẳn toàn bộ data character cũ
   - cho quay về flow tạo character mới hoàn toàn
   - character mới không liên quan character cũ, kể cả tên

### Technical implication
- cần trạng thái persisted kiểu `pending_permanent_deletion`
- không hard-delete ngay tại thời điểm death resolve
- cần packet/ack path để client xác nhận xóa character sau khi đọc thông báo

## 16.4 Pending-delete integration contract

### Source of truth persistence
Thêm persisted flag/state canonical: `pending_permanent_deletion`.

### Preferred location
- ưu tiên thêm cờ vào bảng `characters`
- nếu repo đang gom lifecycle state ở chỗ khác thì có thể dùng lifecycle extension table
- nhưng source-of-truth phải là **persisted character-level flag**, không phải runtime-only state

### Gate points canonical
Gate tại **cả 3 layer**:
1. `GetCharacterList`
2. `GetCharacterData`
3. `EnterWorld`

#### Detailed behavior
- `GetCharacterList`: character **vẫn hiện trong list** cho đến khi user xác nhận xóa
- `GetCharacterData`: không cho vào flow bình thường; trả code pending-delete + notice packet
- `EnterWorld`: hard gate, không được recover-to-home rồi attach world như current flow

### Why character remains visible in list
Nếu ẩn khỏi list, player không còn entry point để hiểu vì sao character biến mất. Requirement user muốn mỗi lần login vẫn bị chặn và thấy thông báo.

### Confirm contract
Không reuse notification acknowledge.

Thêm packet mới, ví dụ:
- `ConfirmPermanentCharacterDeletionPacket`
- `ConfirmPermanentCharacterDeletionResultPacket`

### Confirm flow
1. client chọn character pending-delete
2. server trả pending-delete notice
3. client hiện modal bắt buộc
4. client bấm OK => gửi `ConfirmPermanentCharacterDeletionPacket { CharacterId }`
5. server hard-delete toàn bộ data character liên đới
6. server trả result success
7. client refresh character list và mở create-character flow nếu muốn

### Delete mode canonical
**Hard delete** toàn bộ dữ liệu character liên đới sau khi user confirm.

### Enter-world guard order
Trong `WorldEntryService.EnterAsync(...)`, check pending-delete **trước**:
- `PrepareSnapshotForWorldEntryAsync(...)`
- `RecoverSnapshotToHomeAsync(...)`
- `AttachPlayerSession(...)`

Mục tiêu: character pending-delete không bao giờ được attach world hoặc hưởng recovery path hiện có.

---

## 17. TechDesign Verdict

### Confirmed from code
- death state runtime có sẵn
- return-home-after-combat-death có sẵn
- ground reward runtime có sẵn
- item droppable flag có sẵn
- lifespan base runtime có sẵn

### Not yet implemented and must be added
- full death penalty orchestrator
- drop-on-death runtime integration
- skill-only buff clear contract
- lifespan/tribulation penalty hook
- permanent death archive/delete flow
- checkpoint respawn option
- additional penalty config table

### Ready level
- đủ để tạo TD spec: **yes**
- đủ để handoff dev ngay: **yes**
