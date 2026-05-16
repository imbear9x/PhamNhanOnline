---
title: Client Dev — Death Penalty (Character Death, Pending Permanent Deletion, Respawn)
doc_type: handoff
status: Ready
owner: dev-client
source_agent: techdesign
last_updated: 2026-05-16
source_design_doc: docs/game-design-wp/requirements/death-penalty.md
source_tech_design_doc: docs/tech-design/death-penalty.md
expected_output: unity-client-implementation
queue_id: 55
feature_key: death-penalty
handoff_type: client-dev
source_handoff: docs/agent-handoffs/active/20260516-44-death-penalty-qa-report.md
response_to: docs/agent-handoffs/active/20260516-44-death-penalty-qa-report.md
supersedes:
iteration: 1
---

# Goal

Unity client implement đầy đủ flow Death Penalty:

1. **Death transition UI** — nhận 2 packet riêng biệt (HP=0 trước, CombatDead transition sau), render đúng thứ tự
2. **Respawn UI** — cho phép player gọi `ReturnHomeAfterCombatDeath` từ màn chờ combat-dead
3. **Pending Permanent Deletion** — nhận biết character đang pending-delete từ `CharacterModel`, render warning trong character list, block vào world, hiển thị modal bắt buộc, gửi confirm-delete khi player OK
4. **Tribulation countdown** — đọc `NextTribulationAtUnixMs` từ `CharacterCurrentStateModel` và hiển thị countdown nếu realm 19+

---

# Source Authority

| Source | Purpose |
|---|---|
| `docs/agent-handoffs/active/20260516-44-death-penalty-qa-report.md` | QA passed evidence — định nghĩa behavior server đã pass |
| `docs/agent-handoffs/active/20260516-42-death-penalty-runtime-di-async-response.md` | Reviewer accepted risks — packet ordering và async fire-and-forget |
| `docs/tech-design/death-penalty.md` | TechDesign spec — canonical server contract, flow, data model |
| `GameShared/Packets/Packets/CharacterPackets.cs` | Packet IDs, fields, directions canonical từ code |
| `GameShared/Models/CharacterCurrentStateModel.cs` | Model fields canonical |
| `GameShared/Models/CharacterModel.cs` | Model fields canonical |
| `GameShared/Models/CharacterBaseStatsModel.cs` | Model fields canonical |
| `GameShared/Messages/MessageCode.cs` | Error/message code values canonical |
| `GameServer/Runtime/CharacterStateTransitionReasons.cs` | Transition reason int values canonical |

---

# Canonical Server Contract

## A. Death transition packet ordering (đã QA pass)

Khi player chết trong combat:

1. Server gửi `CharacterCurrentStateChangedPacket [4]` với `CurrentHp = 0` **trước**
2. Server gửi `CharacterStateTransitionPacket [6]` với `Reason = 2 (CombatDead)` **sau** — chỉ khi `DeathPenaltyService.ApplyOnCombatDeathAsync(...)` đã hoàn thành

> **Accepted risk (reviewer #42):** Server dùng fire-and-forget async cho death penalty DB write. Nếu DB write fail sau khi client đã nhận `CharacterCurrentStateChanged(HP=0)`, server chỉ log lỗi — penalty có thể không persist. Client không cần xử lý case này; chỉ cần render đúng thứ tự 2 packet trên.

## B. Respawn flow (đã QA pass)

Từ màn chờ `CombatDead`, player có thể gửi `ReturnHomeAfterCombatDeathPacket [73]` để hồi sinh về nhà.

Server sẽ guard: nếu character đang `PendingPermanentDeletion = true`, result sẽ fail với `CharacterPendingPermanentDeletion (3062)`.

## C. Pending permanent deletion gates (đã QA pass)

Character hết thọ nguyên → server mark `PendingPermanentDeletion = true` trên character row.

Server behavior tại từng gate:

| Flow | Server behavior |
|---|---|
| `GetCharacterList` | Character **vẫn xuất hiện** trong list, `PendingPermanentDeletion = true` trên `CharacterModel` |
| `GetCharacterData` | Fail với `CharacterPendingPermanentDeletion (3062)` nhưng **vẫn đính kèm** `Character`, `BaseStats`, `CurrentState` trong result |
| `EnterWorld` | Fail với `CharacterPendingPermanentDeletion (3062)`, không attach world |
| `ReturnHomeAfterCombatDeath` | Fail với `CharacterPendingPermanentDeletion (3062)` |

## D. Confirm permanent deletion flow (đã QA pass)

Client gửi `ConfirmPermanentCharacterDeletionPacket [224]` sau khi player bấm OK trên modal.

Server hard-delete toàn bộ data character liên đới, trả `ConfirmPermanentCharacterDeletionResultPacket [225]`:
- `Success = true`, `CharacterId = <id>` → xóa thành công
- `Success = false`, `Code = CharacterNotFound (3003)` → đã xóa rồi (double-confirm)
- `Success = false`, `Code = CharacterPendingPermanentDeletion (3062)` → character chưa pending-delete, không cho xóa

## E. Tribulation countdown (persist đã pass, trigger runtime chưa nối)

`CharacterCurrentStateModel.NextTribulationAtUnixMs` là Unix ms timestamp lúc tribulation tiếp theo.

Client đọc field này và render countdown `(NextTribulationAtUnixMs - now)` nếu realm 19+.

> **Defer:** Tribulation battle trigger thực sự (khi countdown về 0) **chưa được implement** ở server trong slice này. Client hiển thị countdown nhưng **không** tự trigger bất cứ flow nào khi về 0 — chờ handoff tribulation runtime riêng.

---

# Packet Contract

| Packet | ID | Direction | Important fields | Client handling |
|---|---:|---|---|---|
| `CharacterCurrentStateChangedPacket` | `4` | S→C (broadcast scope) | `CurrentState` (`CharacterCurrentStateModel`) | Nhận ngay → cập nhật HP/MP/state local → nếu `CurrentHp == 0` thì render death visual |
| `CharacterStateTransitionPacket` | `6` | S→C (broadcast scope) | `CharacterId`, `Reason` (int) | `Reason == 2` (CombatDead) → hiển thị màn chờ respawn sau khi đã có death visual từ packet [4] |
| `ReturnHomeAfterCombatDeathPacket` | `73` | C→S | _(không có field)_ | Gửi khi player bấm nút respawn về nhà |
| `ReturnHomeAfterCombatDeathResultPacket` | `74` | S→C | `Success`, `Code`, `BaseStats`, `CurrentState` | Success → recover về home, cập nhật stats/state; Fail → hiển thị lỗi |
| `GetCharacterListPacket` | `13` | C→S | _(không có field)_ | Gửi để lấy danh sách character sau login hoặc sau confirm-delete |
| `GetCharacterListResultPacket` | `14` | S→C | `Success`, `Code`, `Characters` (list `CharacterModel`) | Render list; với mỗi char `PendingPermanentDeletion == true` → render badge/warning |
| `GetCharacterDataPacket` | `11` | C→S | `CharacterId` | Gửi khi player chọn character để xem thông tin |
| `GetCharacterDataResultPacket` | `12` | S→C | `Success`, `Code`, `Character`, `BaseStats`, `CurrentState` | Nếu `Code == CharacterPendingPermanentDeletion (3062)` → vẫn có data đính kèm → hiển thị modal pending-delete bắt buộc (xem UI rules) |
| `EnterWorldPacket` | `9` | C→S | `CharacterId` | Gửi khi player vào game |
| `EnterWorldResultPacket` | `10` | S→C | `Success`, `Code`, `Character`, `BaseStats`, `CurrentState` | Nếu `Code == CharacterPendingPermanentDeletion (3062)` → block vào world, hiển thị modal pending-delete |
| `ConfirmPermanentCharacterDeletionPacket` | `224` | C→S | `CharacterId` | Gửi khi player bấm OK xác nhận xóa character |
| `ConfirmPermanentCharacterDeletionResultPacket` | `225` | S→C | `Success`, `Code`, `CharacterId` | Success → refresh character list; Fail → xem error handling |

---

# Model Contract

| Model | Important fields | Client render/cache usage |
|---|---|---|
| `CharacterModel` | `PendingPermanentDeletion` (bool) | Render warning/badge trong character list; block flow vào world khi `true` |
| `CharacterCurrentStateModel` | `CurrentHp`, `CurrentState` (int), `NextTribulationAtUnixMs` (long?, Unix ms), `IsExpired` (bool), `LifespanEndUnixMs` (long?, Unix ms) | HP=0 → death visual; `CurrentState` → state machine client; `NextTribulationAtUnixMs` → countdown tribulation |
| `CharacterBaseStatsModel` | `LifespanBonus` (int) | Dùng với `LifespanEndUnixMs` để hiển thị thọ nguyên còn lại |

---

# UI / State Rules

## Death visual và respawn UI

- Khi nhận `CharacterCurrentStateChangedPacket [4]` với `CurrentHp == 0`:
  - render death visual ngay lập tức (character ngã, màn đen, v.v.)
  - **chưa** hiển thị nút respawn ngay lúc này

- Khi nhận `CharacterStateTransitionPacket [6]` với `Reason == 2 (CombatDead)`:
  - hiển thị màn chờ respawn (nút "Hồi sinh về Động Phủ")
  - thứ tự 2 packet này là **guaranteed ordering** theo server contract

- Sau khi gửi `ReturnHomeAfterCombatDeathPacket [73]`:
  - **không** optimistic dismiss màn respawn
  - chờ result trả về rồi mới transition

## Pending permanent deletion UI

- Trong **character list**, với mỗi `CharacterModel` có `PendingPermanentDeletion == true`:
  - render badge/label rõ ràng: ví dụ "Hết thọ nguyên — Cần xác nhận xóa"
  - không ẩn character khỏi list

- Khi player chọn character pending-delete (qua `GetCharacterData` hoặc `EnterWorld`) và server trả `CharacterPendingPermanentDeletion (3062)`:
  - hiển thị **modal bắt buộc** (không cho dismiss mà không action):
    - Nội dung: "Nhân vật này đã hết thọ nguyên. Bấm Xác Nhận để xóa vĩnh viễn và tạo nhân vật mới."
    - Nút: **Xác Nhận** → gửi `ConfirmPermanentCharacterDeletionPacket`
    - Nếu player thoát game mà chưa confirm: lần sau vào lại vẫn bị chặn và hiện lại modal

- Sau khi gửi `ConfirmPermanentCharacterDeletionPacket [224]`:
  - **không** xóa character khỏi list trước khi có result
  - khi nhận `Success = true`: refresh character list → character đã mất, mở flow tạo character mới

## Tribulation countdown

- Nếu character realm 19+ và `NextTribulationAtUnixMs != null`:
  - hiển thị countdown UI: `NextTribulationAtUnixMs - now (ms)`
  - countdown về 0 → **chỉ hiển thị "Lôi kiếp đã đến"**, không tự trigger bất cứ flow nào
  - chờ server push packet tribulation (chưa có trong slice này)

## Reconnect / relogin

- Sau reconnect hoặc relogin, client load lại `GetCharacterList` → check lại `PendingPermanentDeletion` trên từng character
- Nếu character đang trong world bị disconnect khi `CombatDead`:
  - flow `ReturnHomeAfterCombatDeath` vẫn hợp lệ sau reconnect nếu server còn giữ state `CombatDead`
  - client cần check `CurrentState` khi reattach session để restore màn respawn nếu cần

---

# Error / Message Code Handling

| Code | Value | Context | Client behavior |
|---|---|---|---|
| `CharacterPendingPermanentDeletion` | `3062` | `GetCharacterData`, `EnterWorld`, `ReturnHomeAfterCombatDeath` result | Hiển thị modal pending-delete bắt buộc (xem UI rules) |
| `CharacterNotFound` | `3003` | `ConfirmPermanentCharacterDeletion` result | Toast: "Nhân vật không còn tồn tại." → refresh character list |
| `CharacterNotCombatDead` | `3044` | `ReturnHomeAfterCombatDeath` result khi gọi sai state | Toast: "Nhân vật không ở trạng thái chết." → reset client state về alive |
| `CharacterLifespanExpired` | `3007` | Có thể xuất hiện ở các flow khác | Toast: "Thọ nguyên đã hết." → redirect về character list |

---

# Accepted Risks / Client Tolerance

1. **Fire-and-forget death penalty DB write**: Sau khi client nhận `CharacterCurrentStateChanged(HP=0)`, penalty DB write là async fire-and-forget. Nếu fail, server log lỗi nhưng client không được thông báo. Client không cần xử lý; chỉ render đúng packet order.

2. **Tribulation trigger chưa nối**: `NextTribulationAtUnixMs` persist đã đúng, nhưng tribulation battle runtime trigger chưa implement. Client hiển thị countdown nhưng không action khi về 0. Đây là defer hợp lệ.

3. **Duplicate death transition guard là server-side**: Client không cần tự guard duplicate `CombatDead` packet; server đã có guard `wasCombatDead` check.

4. **QA verify bằng code evidence, không có live packet capture**: Evidence là implementation pass, không phải live gameplay. Nếu phát hiện lệch behavior trong integration, báo lại TechDesign.

---

# Out Of Scope

- Checkpoint respawn (`ReturnCheckpointAfterCombatDeath`) — chưa implement ở server, không có trong slice này
- Drop linh thạch khi chết — server chưa implement trong slice này
- Drop item khi chết — server chưa implement trong slice này
- Buff clear visual khi chết — server clear source `Skill` nhưng không có broadcast packet riêng cho buff list; client không cần action nếu chưa có buff display feature
- Tribulation battle trigger UI — defer sang handoff tribulation runtime riêng
- Additional death penalty context configs — server schema có nhưng chưa có gameplay trigger trong slice này
- Name reclaim sau permanent death — GD/user decision chưa chốt

---

# Dev-Client Implementation Checklist

## Network / Packet

- [ ] Register handler cho `CharacterCurrentStateChangedPacket [4]` — update local state, detect HP=0 cho death visual
- [ ] Register handler cho `CharacterStateTransitionPacket [6]` — detect `Reason == 2 (CombatDead)` để show respawn UI
- [ ] Implement gửi `ReturnHomeAfterCombatDeathPacket [73]` từ respawn UI
- [ ] Handle `ReturnHomeAfterCombatDeathResultPacket [74]` — success path và error codes
- [ ] Handle `GetCharacterListResultPacket [14]` — map `CharacterModel.PendingPermanentDeletion` vào UI state
- [ ] Handle `GetCharacterDataResultPacket [12]` — detect `Code == 3062` và hiển thị modal pending-delete
- [ ] Handle `EnterWorldResultPacket [10]` — detect `Code == 3062` và hiển thị modal pending-delete
- [ ] Implement gửi `ConfirmPermanentCharacterDeletionPacket [224]` từ modal confirm
- [ ] Handle `ConfirmPermanentCharacterDeletionResultPacket [225]` — success: refresh list; fail: error toast

## UI

- [ ] Death visual khi `CurrentHp == 0` (packet [4])
- [ ] Respawn UI / màn chờ khi `Reason == 2 CombatDead` (packet [6]) với nút "Hồi sinh về Động Phủ"
- [ ] Badge/warning trong character list với character `PendingPermanentDeletion == true`
- [ ] Modal bắt buộc pending-delete: nội dung, nút Xác Nhận, không cho dismiss tùy tiện
- [ ] Tribulation countdown display từ `NextTribulationAtUnixMs` nếu realm 19+
- [ ] Loading/disabled state khi đang chờ server result (respawn, confirm-delete)

## State / Cache

- [ ] Client state machine: `Alive` → `CombatDead` → `Alive` (sau respawn)
- [ ] Không optimistic remove character khỏi list trước khi confirm-delete result về
- [ ] Sau reconnect: check `CurrentState` để restore đúng state (respawn UI nếu còn `CombatDead`)
- [ ] Cache `PendingPermanentDeletion` từ character list, invalidate khi nhận confirm-delete success

---

# Client Self-Test Checklist

- [ ] Player chết trong combat → thấy death visual, SAU ĐÓ thấy nút respawn (2 bước riêng biệt)
- [ ] Bấm respawn → character recover về home, màn respawn đóng
- [ ] Bấm respawn khi server pending-delete → thấy modal pending-delete (không phải crash)
- [ ] Character list hiển thị badge "hết thọ nguyên" với character pending-delete
- [ ] Chọn character pending-delete → modal bắt buộc xuất hiện, không cho vào world
- [ ] Bấm OK trên modal → character biến mất khỏi list sau khi refresh
- [ ] Thoát game giữa chừng không confirm → vào lại vẫn thấy modal pending-delete
- [ ] Double-confirm (gửi lại sau khi đã xóa) → toast "Nhân vật không còn tồn tại"
- [ ] Realm 19+ character → thấy countdown tribulation từ `NextTribulationAtUnixMs`
- [ ] Countdown về 0 → không crash, không trigger flow lạ, chỉ hiển thị trạng thái "Lôi kiếp đã đến"

---

# User Manual E2E Checklist

Sau khi pull Unity client changes, user test thủ công:

1. **Combat death → respawn:**
   - Vào map, để HP về 0 bằng combat
   - Kiểm tra: màn hình có death visual rõ ràng
   - Kiểm tra: nút respawn xuất hiện SAU death visual (không xuất hiện cùng lúc hoặc trước)
   - Bấm respawn → character về home, HP được restore, có thể đi lại bình thường

2. **Character list với pending-delete:**
   - Dùng character đã được server mark `pending_permanent_deletion = true` (test data)
   - Kiểm tra: character vẫn xuất hiện trong list với badge/warning rõ ràng

3. **Pending delete block + modal:**
   - Chọn character pending-delete → server trả lỗi 3062
   - Kiểm tra: modal bắt buộc xuất hiện với nội dung hướng dẫn xóa character
   - Không cho vào world bình thường

4. **Confirm permanent deletion:**
   - Bấm OK trên modal pending-delete
   - Kiểm tra: server xóa thành công, character list refresh, character đã biến mất
   - Kiểm tra: có thể tạo character mới sau đó

5. **Tribulation countdown (realm 19+):**
   - Dùng character realm 19+ có `next_tribulation_at_utc` hợp lệ trong DB
   - Kiểm tra: UI hiển thị countdown đếm ngược đúng
   - Kiểm tra: khi countdown về 0, không có crash hoặc flow lạ kích hoạt
