---
title: Dev Follow-up — Death Penalty Blocker Response
doc_type: handoff
status: Done
owner: dev
source_agent: techdesign
last_updated: 2026-05-16
source_design_doc: docs/game-design-wp/requirements/death-penalty.md
source_tech_design_doc: docs/tech-design/death-penalty.md
expected_output: server-implementation
queue_id: 39
feature_key: death-penalty
handoff_type: dev-followup
source_handoff: docs/agent-handoffs/active/20260516-38-death-penalty-dev-blockers-techdesign.md
response_to: docs/agent-handoffs/active/20260516-37-death-penalty-dev.md
iteration: 1
---

# Mục tiêu

Resume `#37 death-penalty-dev` theo authority clarification mới từ TechDesign. Handoff này chỉ trả lời 3 blocker dev đã raise, không reset scope gốc.

Nguồn authority cập nhật:
- `docs/tech-design/death-penalty.md`
- `docs/agent-handoffs/active/20260516-38-death-penalty-dev-blockers-techdesign.md`

---

# A. Tribulation countdown — canonical decision

## Storage canonical
Dùng field persisted mới:
- `next_tribulation_at_utc`

## Preferred location
- thêm vào `character_current_states` / entity `CharacterCurrentState`

## Why
- countdown là lifecycle time-based state của character
- dễ derive remaining seconds
- dễ floor về 0 bằng cách set về `utcNow`

## Death penalty contract
Khi character realm 19+ chết:
1. load `next_tribulation_at_utc`
2. trừ `death.tribulation_penalty_seconds`
3. nếu kết quả <= `utcNow`, persist bằng `utcNow`
4. không lưu `remaining_seconds` làm source-of-truth canonical

## Trigger when reaches zero
- death penalty **không** tự khởi động tribulation battle inline trong transaction này
- chỉ persist timestamp về `utcNow`
- nếu repo chưa có tribulation runtime hook thực, Dev được phép tạo follow-up riêng cho trigger integration

## Important
- được defer **trigger integration**
- **không được defer** storage + mutation contract

---

# B. Skill-only buff clear — canonical decision

## Authority chosen
**Option B** — phải thêm source/origin tagging.

## Minimum required tagging
Áp cho toàn bộ state trong `CombatStatusCollection` có thể bị death clear:
- shield
- stun
- stat modifier

## Minimum enum boundary
- `Skill`
- `Talisman`
- `Formation`
- `External`

## Clear contract
Khi death resolve:
- chỉ clear entries có `source_type == Skill`
- giữ nguyên `Talisman`, `Formation`, `External`

## Reviewer expectation
Reviewer sẽ kiểm toàn bộ path add combat statuses có set source type đúng, không chỉ path clear.

---

# C. Pending permanent deletion — canonical decision

## Persisted source of truth
Thêm persisted state/flag:
- `pending_permanent_deletion`

## Preferred location
- ưu tiên trên bảng `characters`
- acceptable nếu Dev dùng lifecycle extension table, miễn là source-of-truth là persisted character-level state

## Gate points canonical
Phải gate tại cả 3 layer:
1. `GetCharacterList`
2. `GetCharacterData`
3. `EnterWorld`

## Character list behavior
- character pending-delete vẫn **hiện trong list**
- không ẩn khỏi player trước khi player xác nhận xóa

## Enter-world guard order
Trong `WorldEntryService.EnterAsync(...)`, gate pending-delete phải chạy **trước**:
- `PrepareSnapshotForWorldEntryAsync(...)`
- `RecoverSnapshotToHomeAsync(...)`
- `AttachPlayerSession(...)`

=> character pending-delete không bao giờ được vào recovery/home/world attach path.

## Confirm contract
Không reuse notification acknowledge.

Thêm packet mới:
- `ConfirmPermanentCharacterDeletionPacket`
- `ConfirmPermanentCharacterDeletionResultPacket`

## Confirm flow
1. player chọn character pending-delete
2. server trả trạng thái/code phù hợp + notice contract cho client hiện modal
3. user bấm OK
4. client gửi `ConfirmPermanentCharacterDeletionPacket { CharacterId }`
5. server hard-delete toàn bộ data character liên đới
6. trả result success
7. client refresh list và mở create-character flow nếu muốn

## Delete mode
- **hard delete** toàn bộ data character liên đới sau confirm

---

# Resume Scope For Dev

Dev tiếp tục `#37` với clarifications trên và cần cập nhật implementation để bao gồm:
1. migration/schema cho `next_tribulation_at_utc`
2. migration/schema cho `pending_permanent_deletion` source-of-truth
3. source tagging cho `CombatStatusCollection`
4. packet/result mới cho confirm permanent deletion
5. hard gate pending-delete tại list/data/enter-world paths
6. follow-up handoff riêng nếu tribulation trigger runtime chưa có hook thực

---

# Expected Dev Response

Dev response cần nêu rõ:
- file/migration nào thêm cho `next_tribulation_at_utc`
- file/migration nào thêm cho `pending_permanent_deletion`
- source tagging được wire ở các path add shield/stun/stat-modifier nào
- gate tại `GetCharacterList`, `GetCharacterData`, `EnterWorld` làm thế nào
- contract packet delete confirm đã thêm ở đâu
- còn điểm nào phải defer follow-up (nếu tribulation trigger runtime chưa có)
