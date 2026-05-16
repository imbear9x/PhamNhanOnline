---
title: Dev — Death Penalty System Implementation
doc_type: handoff
status: Blocked
owner: dev
source_agent: techdesign
last_updated: 2026-05-16
source_design_doc: docs/game-design-wp/requirements/death-penalty.md
source_tech_design_doc: docs/tech-design/death-penalty.md
expected_output: server-implementation
queue_id: 37
feature_key: death-penalty
handoff_type: dev
source_handoff: docs/agent-handoffs/active/20260515-22-death-penalty-techdesign.md
response_to: docs/agent-handoffs/active/20260515-22-death-penalty-techdesign.md
iteration: 1
---

# Mục tiêu

Implement server-side **Death Penalty System** theo authority đã chốt ở:
- `docs/game-design-wp/requirements/death-penalty.md`
- `docs/tech-design/death-penalty.md`

Scope này là implementation chính cho:
- combat death penalty orchestration
- drop linh thạch khi chết
- drop tối đa 1 item droppable khi chết
- clear buff skill khi chết
- apply lifespan penalty hoặc tribulation penalty
- permanent death pending-delete flow
- respawn về home path tương thích runtime hiện có

> Lưu ý: checkpoint respawn có thể cần follow-up riêng nếu code grounding hiện tại chưa đủ nền map/checkpoint runtime. Đừng hardcode nửa vời.

---

# Authority đã chốt

## 1. Baseline áp dụng cho mọi loại chết
Không có exception theo nguyên nhân chết ở baseline.

## 2. Không mất tu vi, không mất tiềm năng
Tuyệt đối không đụng vào cultivation/progression resources ngoài scope đã nêu.

## 3. Drop item
- roll trúng thì chỉ rớt **tối đa 1 item**
- item phải `droppable`
- non-droppable / non-tradable không được vào pool

## 4. Buff clear
- chỉ clear **buff từ skill**
- buff từ bùa chú / trận pháp phải được giữ

## 5. Lifespan elixir
- **không cap** theo pool cảnh giới hiện tại
- item tăng thọ nguyên có thể đẩy tổng thọ nguyên vượt pool hiện tại

## 6. Warning threshold
- fixed threshold = **10 ngày**
- `864000` giây

## 7. Permanent death
Khi death penalty làm thọ nguyên về 0:
1. không hard-delete ngay
2. character bị lock / chặn login-world flow
3. mỗi lần player đăng nhập sẽ bị chặn và thấy thông báo hết thọ nguyên
4. nếu player thoát game mà chưa xác nhận, lần sau vào lại vẫn bị chặn và hiện lại thông báo
5. chỉ khi player **bấm OK xác nhận** thì mới xóa hẳn toàn bộ data character cũ
6. sau đó player quay về flow tạo character mới hoàn toàn; không liên quan character cũ kể cả tên

---

# Repo grounding đã được TD xác nhận

## Có sẵn để reuse
- `CharacterRuntimeStateCodes.CombatDead`
- `CharacterRuntimeService.NotifyDeathTransitionIfNeeded(...)`
- `CharacterCombatDeathRecoveryService`
- `ReturnHomeAfterCombatDeathPacket` / Result
- `GroundRewardEntity` + ground reward runtime / pickup flow
- item template flags: `is_droppable`, `is_tradeable`
- lifespan runtime nền: `realm_templates.lifespan`, `lifespan_bonus`, `CharacterLifespanRules`, `CharacterLifecycleService`

## Chưa có đầy đủ, cần Dev implement
- death penalty orchestration service
- auto drop on death
- skill-only buff clear implementation boundary
- tribulation countdown penalty hook
- pending-permanent-deletion persistence/flow
- login-block + confirmation delete flow cho permanent death

---

# Required Dev Tasks

## Task 1 — Thêm service orchestrator cho death penalty
Tạo service orchestration mới, ví dụ `DeathPenaltyService`, hoặc tên tương đương theo pattern repo.

### Service responsibilities
- resolve death context
- roll drop linh thạch
- roll item drop pool
- apply mutation inventory/currency/character trong transaction boundary
- apply lifespan hoặc tribulation penalty
- trả ra result object cho world/runtime layer dùng spawn reward sau commit

### Không làm trong service này
- không gửi packet trực tiếp
- không tự emit map broadcast trước commit

---

## Task 2 — Hook death penalty vào combat-dead transition
Hiện death transition đang được notify ở `CharacterRuntimeService`.
Dev cần thêm execution path bảo đảm:
- penalty chỉ chạy **1 lần** cho mỗi death transition
- không chạy lặp khi current state tiếp tục là dead
- không broad-break current combat dead notification path

### Expected pattern
- resolve business side-effect sau khi state chuyển sang `CombatDead`
- có guard idempotency / per-death marker nếu cần

---

## Task 3 — Implement drop linh thạch + item atomically

## 3.1 Lingthach drop
Implement theo config:
- roll rate
- random % trong min/max
- clamp theo min/max amount
- nếu player không có linh thạch thì drop = 0

## 3.2 Item drop
- build candidate pool từ item sources an toàn theo authority/spec
- exclude:
  - non-droppable
  - non-tradable
  - expired
  - invalid quantity
- nếu roll trúng: random đúng **1 item**

## 3.3 Atomicity
Linh thạch drop + item drop + lifespan/tribulation penalty phải ở cùng mutation boundary.

### Important
- nếu transaction fail => **không spawn ground reward**
- ground reward chỉ spawn **sau commit thành công**

---

## Task 4 — Reuse ground reward runtime, không tạo inbox/mail path
Drop-on-death phải dùng ground reward runtime đang có.

### Required behavior
- owner = player vừa chết
- ownership/prio window dùng **shared config source duy nhất** với ownership/drop-rights rule
- không tạo config duplicate riêng nếu source chung đã tồn tại
- không persist ground reward vào DB nếu current runtime đang dùng in-memory entity

---

## Task 5 — Clear skill buff only
Implement đúng authority:
- clear effect có nguồn từ **skill**
- giữ effect từ **talisman / formation**

### Nếu runtime hiện chưa có source-type rõ ràng
Dev phải:
- audit current combat status container
- implement minimal source tagging hoặc equivalent safe boundary
- không shortcut kiểu clear-all nếu có nguy cơ quét nhầm talisman/formation effect

Nếu phase 1 buộc phải dựa trên current runtime chỉ đang chứa skill-derived modifiers, phải ghi rõ evidence trong response để reviewer verify.

---

## Task 6 — Lifespan / Tribulation penalty hook

## 6.1 Realm 1–18
- apply lifespan penalty
- floor tại 0
- authority hiện tại cho phép tổng thọ nguyên vượt pool do item/elixir tăng thêm

### Important
Code hiện dùng `realm lifespan + lifespan_bonus`.
Dev cần quyết định implementation sạch nhất:
- reuse `lifespan_bonus` với convention rõ ràng, hoặc
- thêm field dedicated penalty delta

Nếu thêm schema mới, phải update migration + entity + mapper + load/save path đầy đủ.

## 6.2 Realm 19+
- apply tribulation countdown penalty
- floor tại 0
- nếu về 0 thì để hệ tribulation source-of-truth trigger theo contract của hệ đó

Nếu repo hiện chưa đủ tribulation countdown storage/hook rõ ràng, Dev phải nêu blocker/follow-up cụ thể thay vì hardcode sai authority.

---

## Task 7 — Permanent death pending-delete flow
Đây là phần quan trọng nhất.

### Required behavior
Khi death penalty làm lifespan về 0:
1. character chuyển sang trạng thái không được vào world bình thường
2. server persist trạng thái pending permanent deletion
3. login lần nào cũng bị chặn và hiện thông báo cho đến khi user xác nhận
4. chỉ khi user xác nhận OK thì mới xóa hẳn toàn bộ data character

### Dev cần implement tối thiểu
- persisted flag/state cho `pending_permanent_deletion`
- login/world-entry gate kiểm tra trạng thái này
- packet/result hoặc notification/ack contract để client xác nhận xóa character
- delete pipeline toàn bộ data character cũ khi user confirm

### Important
- nếu user không xác nhận mà thoát game, data cũ vẫn còn nhưng bị lock
- lần login sau vẫn hiện thông báo
- sau khi delete xong, flow trở lại create-character path hoàn toàn mới

### Delete scope
Dev phải audit và xóa sạch dữ liệu liên đới character cũ:
- character row
- base/current stats
- inventory/items/equipment
- herbs/soil/garden ownership nếu có
- notifications/mail/quest/runtime extensions theo character ownership
- các row foreign-key liên đới khác

Nếu repo hiện chưa có central character purge service, nên tạo service riêng thay vì rải delete khắp nơi.

---

## Task 8 — Respawn path

## In scope cho vòng này
- giữ path hiện có: `ReturnHomeAfterCombatDeath`
- đảm bảo player không thể dùng respawn thường nếu đã rơi vào pending permanent deletion

## Out of scope tạm chấp nhận nếu chưa có nền
- checkpoint respawn option hoàn chỉnh

Nếu chưa đủ nền checkpoint runtime, ghi rõ follow-up thay vì nhồi implementation nửa vời.

---

# Schema / Data / Config Expectations

## A. New / updated config
Nếu chưa có, thêm runtime keys / config source cho:
- death drop lingthach rate
- death drop lingthach pct min/max
- death drop lingthach amount min/max
- death drop item rate
- death lifespan penalty seconds
- death tribulation penalty seconds
- death lifespan warning threshold seconds = `864000`
- death tribulation warning threshold seconds = `864000`

## B. Additional penalty config table
Implement schema/catalog cho `death_penalty_context_configs` hoặc equivalent table theo TD spec.

## C. Permanent death persistence
Thêm persisted state/columns cần thiết cho pending-delete flow.

### Preferred shape
Một trong các hướng sau là acceptable nếu nhất quán:
1. cờ trên `characters`
2. state enum trong current-state/lifecycle row
3. bảng lifecycle extension riêng

Dev chọn hướng ít phá repo nhất, nhưng phải rõ source of truth.

---

# Validation / Invariants

Dev phải giữ các invariant sau:
1. death penalty chỉ resolve 1 lần mỗi death transition
2. drop tối đa 1 item
3. non-droppable/non-tradable không vào pool
4. lifespan/tribulation không âm
5. ground reward không spawn trước commit
6. pending permanent deletion chặn world entry
7. login lại khi chưa xác nhận vẫn thấy thông báo và vẫn bị chặn
8. chỉ khi xác nhận OK mới hard-delete data
9. không đụng tu vi/tiềm năng

---

# Reviewer Focus

Reviewer sẽ tập trung kiểm:
1. atomicity giữa inventory/currency mutation và spawn reward
2. death hook có bị chạy double không
3. clear skill-only có lỡ clear talisman/formation không
4. pending permanent deletion có thật sự persist và chặn đúng qua nhiều lần login không
5. delete pipeline có xóa sạch data character không
6. không broad-break `ReturnHomeAfterCombatDeath` hiện có
7. tribulation/lifespan floor đúng tại 0

---

# QA Focus To Expect Next

QA tối thiểu cần retest:
1. player chết thường, không trúng drop
2. player chết trúng drop lingthach
3. player chết trúng drop item droppable
4. non-droppable item không bị rơi
5. owner-only pickup window cho đồ rơi đúng
6. realm 1–18 bị trừ lifespan đúng
7. realm 19+ bị trừ tribulation countdown đúng
8. skill buff bị clear, talisman/formation buff giữ nguyên
9. lifespan về 0 => login bị chặn + popup lặp lại qua nhiều lần login nếu chưa confirm
10. bấm OK => character bị xóa sạch và tạo mới được
11. `ReturnHomeAfterCombatDeath` non-regression cho case chưa permanent-death

---

# Expected Dev Output

Dev response cần có:
- file nào sửa
- migration/schema nào thêm
- source of truth cho pending permanent deletion ở đâu
- hook death penalty nằm ở đâu
- spawn reward sau commit được đảm bảo thế nào
- clear skill-only được đảm bảo bằng container/source tagging nào
- tribulation hook dùng state/storage nào
- known gaps/follow-up nào còn lại (nếu checkpoint respawn defer)
