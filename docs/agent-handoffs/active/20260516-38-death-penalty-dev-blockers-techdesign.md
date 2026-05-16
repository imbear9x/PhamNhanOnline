---
title: TechDesign — Death Penalty Dev Blocker Validation
doc_type: handoff
status: Done
owner: techdesign
source_agent: dev
last_updated: 2026-05-16
source_design_doc: docs/game-design-wp/requirements/death-penalty.md
source_tech_design_doc: docs/tech-design/death-penalty.md
expected_output: techdesign-clarification
queue_id: 38
feature_key: death-penalty
handoff_type: techdesign
source_handoff: docs/agent-handoffs/active/20260516-37-death-penalty-dev.md
response_to: docs/agent-handoffs/active/20260516-37-death-penalty-dev.md
iteration: 1
---

# Bối cảnh

Dev đã claim `#37 death-penalty-dev` và audit code grounding thực tế trong repo trước khi implement. Kết quả audit cho thấy một số phần authority trong spec hiện **chưa có grounding/runtime source-of-truth đủ rõ** để dev implement an toàn mà không tự quyết design.

Dev đã xác nhận có thể reuse các phần sau:
- `CharacterRuntimeStateCodes.CombatDead`
- `CharacterRuntimeService.NotifyDeathTransitionIfNeeded(...)`
- `CharacterCombatDeathRecoveryService`
- `ReturnHomeAfterCombatDeathPacket`
- `GroundRewardEntity` runtime
- item flags `is_droppable`, `is_tradeable`
- `CharacterLifecycleService` / lifespan-expired restriction path

Tuy nhiên có 3 blocker authority cần TechDesign chốt rõ trước khi dev tiếp tục.

---

# Blocker 1 — Tribulation countdown penalty chưa có source-of-truth rõ ràng

## Audit evidence
Dev grep/audit hiện chỉ thấy các điểm liên quan cultivation/breakthrough như:
- `GameServer/Entities/BreakthroughAttempt.cs`
- `GameServer/Repositories/BreakthroughAttemptRepository.cs`

Repo **chưa thấy** field/state/runtime nào biểu diễn:
- countdown tới Lôi Kiếp tiếp theo
- persisted timestamp/countdown để trừ `death.tribulation_penalty_seconds`
- trigger contract khi countdown về 0

## Vấn đề
Spec yêu cầu realm `19+` khi chết phải:
- rút ngắn countdown Lôi Kiếp
- floor tại 0
- nếu về 0 thì để hệ tribulation source-of-truth trigger theo contract của hệ đó

Nhưng hiện tại dev không có nơi authoritative để:
- load giá trị countdown hiện tại
- mutate/persist nó
- biết event nào sẽ xử lý mốc `0`

## TechDesign cần chốt
1. Source-of-truth cho tribulation countdown nằm ở đâu?
   - field mới trên `character_base_stats`
   - field mới trên `character_current_state`
   - bảng riêng
   - hay hệ tribulation khác đã tồn tại nhưng chưa được handoff chỉ ra
2. Dữ liệu canonical nên là gì?
   - `next_tribulation_at_utc`
   - `remaining_seconds`
   - hay shape khác
3. Khi penalty làm countdown về 0, dev phải gọi hook/service nào?
4. Nếu hệ tribulation chưa tồn tại thật, có block `#37` hoàn toàn hay cho phép defer bằng follow-up riêng?

---

# Blocker 2 — Clear skill-only buff chưa có source boundary đủ rõ

## Audit evidence
`GameServer/Runtime/CombatStatusRuntime.cs` hiện chỉ có:
- shield list
- stat modifier list
- stun timestamp
- expiry time

Hiện **không có source tagging** như:
- `Skill`
- `Talisman`
- `Formation`
- `External`

`SkillExecutionService` hiện add effect trực tiếp vào `CombatStatusCollection`, nhưng container chưa lưu origin/source type.

## Vấn đề
Authority đã chốt:
- clear buff từ **skill** khi chết
- giữ buff từ **bùa chú / trận pháp**

Nếu dev tự thêm `ClearAll()` hoặc clear theo type hiện tại thì có nguy cơ xóa nhầm effect ngoài skill trong các phase sau.

## TechDesign cần chốt
1. Có acceptable authority nào sau đây không:
   - **A.** Phase hiện tại repo chỉ có skill-derived combat statuses, nên tạm clear toàn bộ `CombatStatusCollection` và ghi rõ reviewer verify evidence
   - **B.** Bắt buộc phải thêm `source_type`/origin tagging vào `CombatStatusCollection` ngay trong scope này
   - **C.** Chỉ clear subset nào đó (`stat modifiers + stun + shield`) theo contract rõ ràng khác
2. Nếu chọn B, TechDesign xác nhận boundary tối thiểu cần tag là gì?
   - shield
   - stun
   - stat modifier
   - debuff/buff partition riêng nếu cần

Dev nghiêng về B hoặc một authority viết rõ tương đương, vì đây là boundary gameplay quan trọng.

---

# Blocker 3 — Pending permanent deletion flow cần chốt integration contract cuối

## Audit evidence
- `WorldEntryService.EnterAsync(...)` hiện luôn:
  1. load snapshot
  2. prepare lifespan state
  3. `RecoverSnapshotToHomeAsync(...)`
  4. attach player vào world
- Nếu character đang ở death-related state mà không gate sớm, current flow có thể vẫn recover/enter world.
- `GetCharacterListHandler` hiện trả list đơn giản từ account.
- `GetCharacterDataHandler` và `EnterWorldHandler` hiện chỉ biết `CharacterLifespanExpired`.
- `PlayerNotificationService.AcknowledgeAsync(...)` hiện yêu cầu `session.Player != null`, nên không phù hợp cho confirm-delete trước world entry.
- Repo có delete primitives rời rạc (`CharacterRepository`, `CharacterBaseStatRepository`, `CharacterCurrentStateRepository`, `PlayerItemRepository`, `PlayerHerbRepository`, ...), nhưng chưa có purge orchestration service trung tâm.

## Vấn đề
TechDesign spec nói pending-delete flow là canonical:
1. lifespan về 0 do death penalty
2. character bị lock / chặn login-world flow
3. mỗi lần login vẫn bị chặn và thấy thông báo
4. chỉ khi user bấm OK xác nhận mới xóa hẳn toàn bộ data character
5. sau đó quay về flow tạo character mới

Phần intent này rõ, nhưng contract tích hợp với packet/login/select-character hiện tại chưa đủ chi tiết cho dev chốt implementation shape tối thiểu.

## TechDesign cần chốt
1. Tại layer nào là canonical gate?
   - `GetCharacterList`
   - `GetCharacterData`
   - `EnterWorld`
   - cả 3
2. Character pending-delete có còn hiện trong `GetCharacterList` không?
   - hiện nhưng có code/state riêng
   - hay ẩn hoàn toàn
3. Confirm OK nên đi qua contract nào?
   - packet mới kiểu `ConfirmPermanentCharacterDeletionPacket`
   - reuse notification acknowledge là **không phù hợp** vì hiện notification acknowledge đòi player đã vào world
4. Sau khi confirm delete thành công, server nên trả về gì?
   - result packet riêng
   - refresh list character
   - code để client mở create-character flow
5. Delete mode canonical là gì?
   - hard delete toàn bộ row liên đới
   - archive + hide

Lưu ý: Requirement ghi rõ permanent death là xóa vĩnh viễn; handoff dev cũng yêu cầu delete pipeline toàn bộ data character cũ. Nếu TechDesign muốn archive thay vì hard delete, cần authority correction rõ vì hiện dev handoff đang nói delete thực.

---

# Đề xuất của Dev

Dev đề xuất TechDesign trả lời theo format ngắn, executable:

## A. Tribulation
- storage canonical = ?
- entity/schema cần thêm = ?
- service/hook khi về 0 = ?
- có defer được không = ?

## B. Buff clear
- authority chọn A/B/C ở trên = ?
- nếu B thì origin tagging tối thiểu cho state nào = ?

## C. Pending delete
- source-of-truth persistence = ?
- gate points canonical = ?
- pending character có hiện trong list không = ?
- confirm packet/result contract = ?
- delete mode = hard delete hay archive = ?

---

# TechDesign Resolution

## A. Tribulation
- **storage canonical**: `next_tribulation_at_utc`
- **entity/schema cần thêm**: ưu tiên thêm field vào `character_current_states` / `CharacterCurrentState`
- **service/hook khi về 0**: death penalty chỉ mutate timestamp về `utcNow`; tribulation trigger battle/runtime được phép follow-up riêng nếu repo chưa có hook thực
- **defer được không**: được defer **trigger integration**, nhưng không defer storage + penalty mutation contract

## B. Buff clear
- **authority chọn**: **B**
- **minimum origin tagging bắt buộc**: `shield`, `stun`, `stat modifier`
- **required enum boundary**: `Skill`, `Talisman`, `Formation`, `External`
- death clear chỉ remove entries có `source_type == Skill`

## C. Pending delete
- **source-of-truth persistence**: persisted `pending_permanent_deletion` flag/state ở cấp character
- **gate points canonical**: `GetCharacterList`, `GetCharacterData`, `EnterWorld`
- **pending character có hiện trong list không**: **có**, vẫn hiện cho tới khi user confirm xóa
- **confirm contract**: packet mới `ConfirmPermanentCharacterDeletionPacket` / `ConfirmPermanentCharacterDeletionResultPacket`
- **delete mode**: **hard delete** toàn bộ data character liên đới sau khi user confirm
- **enter-world guard order**: gate pending-delete trước mọi recovery/attach world logic

# Tác động lifecycle

- Nguồn bị chặn: `docs/agent-handoffs/active/20260516-37-death-penalty-dev.md`
- TD clarification đã đủ để Dev resume bằng handoff follow-up.
