---
doc_type: game_design_requirement
system_id: sect-task-welfare
status: ready
maturity: requirement
owner: gamedesign
created_at: 2026-05-15
updated_at: 2026-05-15
promoted_from: features/sect-system.md
related_docs:
  - features/sect-system.md
  - requirements/sect-core.md
  - requirements/inbox-mail-system.md
  - shared-rules.md
requires_code_verification: true
handoff_ready: true
---

# Tông Môn — Nhiệm Vụ & Phúc Lợi — Requirement Spec

## Goal

Implement vòng lặp tuần tâm hồn của tông môn: môn chủ tạo pool nhiệm vụ bắt buộc + config phúc lợi → đệ tử nhận instance, thực hiện, output tự về bảo khố → claim phúc lợi khi đủ quota.

**Prerequisite:** `requirements/sect-core.md` phải implement trước.

## Source Design Summary

Canonical design: `features/sect-system.md` — sections G, H.
Shared rules: `shared-rules.md` — Escrow rule.

## Target Design Summary

3 loại nhiệm vụ: bắt buộc (quota tuần, output về bảo khố), tự nguyện (thưởng riêng, escrow từ bảo khố), cá nhân (thưởng riêng, escrow từ inventory người tạo). Phúc lợi tuần escrow đầu tuần — nhận khi đủ quota bắt buộc. Tất cả output nhiệm vụ tự về bảo khố khi done, không cần báo cáo.

## Current Runtime / Evidence Snapshot

- **Not confirmed**: task / welfare system đã có server-side chưa.
- **Not confirmed**: escrow mechanism đã implement chưa.
- **Confirmed**: Escrow rule canonical — `shared-rules.md`.

## Scope

### Must Implement

**Nhiệm vụ bắt buộc (Weekly Mandatory):**
- Môn chủ / người có quyền tạo pool instance từ template gamedata
- Pool là flat list; ai nhận trước lấy trước; reset cuối tuần
- Pool phải đủ cho tất cả đệ tử snapshot đầu tuần; môn chủ có thể thêm instance giữa tuần
- Quota 2 dạng: free (bất kỳ N) và typed (N loại A + M loại B)
- Đệ tử nhận instance → thực hiện → output tự về bảo khố, instance tự done → player nhận thông báo
- Đệ tử hủy tối đa 1 lần/ngày → instance về lại pool
- Không kick đệ tử đang giữ NV bắt buộc chưa xong (enforce từ sect-core)
- Nếu có NV bắt buộc → bắt buộc phải có phúc lợi tuần

**Nhiệm vụ tự nguyện:**
- Môn chủ / người có quyền tạo + set thưởng + set deadline
- Thưởng escrow từ bảo khố ngay khi tạo
- 1 người nhận 1 instance; ai muốn nhận thì nhận
- Chưa ai nhận: treo đến hết deadline hoặc sang tuần tiếp (config môn chủ)
- Đã nhận, hết deadline chưa xong: instance về pool kèm thưởng nguyên vẹn; escrow giữ nguyên
- Hủy task chủ động: escrow hoàn về bảo khố

**Nhiệm vụ cá nhân:**
- Bất kỳ thành viên tạo + set thưởng từ inventory cá nhân + set deadline
- Thưởng escrow từ inventory người tạo ngay khi tạo
- Chỉ nội bộ tông môn; 1 người nhận 1 instance
- Hết deadline chưa xong: instance về pool kèm thưởng nguyên vẹn; escrow giữ nguyên
- Hủy task chủ động (người tạo hoặc người có quyền): escrow hoàn về inventory người tạo ban đầu

**Phúc lợi tuần:**
- Môn chủ config phúc lợi (linh thạch hoặc vật phẩm) cố định / người / tuần
- Escrow đầu tuần: snapshot số đệ tử → lock tổng từ bảo khố; không điều chỉnh giữa tuần
- Điều kiện nhận: hoàn thành đủ quota NV bắt buộc
- Đệ tử chủ động claim (không tự vào túi)
- Bị kick sau khi đủ quota: phúc lợi trả tự động trước khi kick có hiệu lực
- Balo đầy khi claim: vào inbox theo shared overflow rule

### Must Not Implement
- NV lặp lại / daily quest
- NV có điều kiện thời gian realtime (chỉ trong X giờ)
- Partial quota (claim phúc lợi khi chưa đủ hoàn toàn)
- Balance cụ thể (thưởng, quota số lượng)

## Terminology

- `template nhiệm vụ`: loại NV do gamedata define. Môn chủ chọn từ danh sách.
- `instance nhiệm vụ`: 1 NV cụ thể được tạo từ template.
- `pool`: flat list instance nhiệm vụ bắt buộc của tuần.
- `quota free`: hoàn thành bất kỳ N instance.
- `quota typed`: hoàn thành đúng X loại A + Y loại B.
- `escrow`: tài sản bị lock tự động khi tạo NV / phúc lợi — xem `shared-rules.md`.

## Functional Requirements

### Nhiệm vụ bắt buộc
- `REQ-001`: Pool nhiệm vụ bắt buộc là flat list instance — không phân cấp, ai nhận trước lấy trước.
- `REQ-002`: Pool reset cuối tuần — tất cả instance chưa nhận bị xóa, không carry over.
- `REQ-003`: Pool snapshot đầu tuần — phải đủ instance cho số đệ tử tại thời điểm đó. Môn chủ / người có quyền có thể thêm instance giữa tuần nếu pool thiếu.
- `REQ-004`: Quota hỗ trợ 2 dạng: `free` (hoàn thành bất kỳ N) và `typed` (hoàn thành đúng N_A loại A + N_B loại B...). Server validate typed quota — chỉ count đúng loại.
- `REQ-005`: Khi đệ tử hoàn thành instance: output tự động về bảo khố, instance mark done, đệ tử nhận thông báo. Không cần về map báo cáo.
- `REQ-006`: Đệ tử hủy instance: tối đa 1 lần/ngày; instance về lại pool ngay lập tức.
- `REQ-007`: Nếu pool có NV bắt buộc trong tuần: bắt buộc phải có phúc lợi tuần config. Server block publish pool nếu không có phúc lợi.
- `REQ-008`: Đệ tử gia nhập giữa tuần không được nhận instance từ pool tuần hiện tại.

### Nhiệm vụ tự nguyện
- `REQ-009`: Môn chủ / người có quyền tạo NV tự nguyện: chọn template, set thưởng (từ bảo khố), set deadline.
- `REQ-010`: Thưởng escrow từ bảo khố ngay khi tạo NV — bảo khố trừ ngay, tài sản lock.
- `REQ-011`: Một instance chỉ 1 người nhận. Ai muốn nhận thì nhận tự do.
- `REQ-012`: Chưa ai nhận và hết deadline: instance tự expire. Môn chủ chọn: xóa hoặc sang tuần tiếp (config per NV). Escrow giữ nguyên cho đến khi hủy chủ động.
- `REQ-013`: Đã nhận, hết deadline chưa xong: instance về pool kèm thưởng nguyên vẹn. Escrow giữ nguyên.
- `REQ-014`: Hủy NV tự nguyện chủ động (người có quyền): escrow hoàn về bảo khố.

### Nhiệm vụ cá nhân
- `REQ-015`: Bất kỳ thành viên tạo NV cá nhân: chọn template, set thưởng từ inventory cá nhân, set deadline.
- `REQ-016`: Thưởng escrow từ inventory người tạo ngay khi tạo — item trừ khỏi inventory và lock.
- `REQ-017`: Chỉ thành viên tông môn nhận NV cá nhân. 1 người nhận 1 instance.
- `REQ-018`: Hết deadline chưa xong: instance về pool kèm thưởng nguyên vẹn. Escrow giữ nguyên.
- `REQ-019`: Hủy NV cá nhân chủ động (người tạo hoặc người có quyền): escrow hoàn về inventory người tạo ban đầu — không về bảo khố.

### Phúc lợi tuần
- `REQ-020`: Môn chủ config phúc lợi (loại + số lượng) cố định per đệ tử per tuần.
- `REQ-021`: Escrow đầu tuần: server snapshot số đệ tử (không gồm đệ tử gia nhập giữa tuần) → lock tổng phúc lợi từ bảo khố. Không điều chỉnh giữa tuần dù số đệ tử thay đổi.
- `REQ-022`: Điều kiện nhận: hoàn thành đủ quota NV bắt buộc trong tuần.
- `REQ-023`: Claim phúc lợi: đệ tử chủ động claim — không tự vào túi. Balo đầy → inbox.
- `REQ-024`: Đệ tử bị kick sau khi đủ quota: server tự động resolve phúc lợi về tay đệ tử trước khi kick có hiệu lực.
- `REQ-025`: Đệ tử gia nhập giữa tuần không nhận phúc lợi tuần đó.

## Acceptance Criteria

- `AC-001`: Given pool NV bắt buộc có 5 instance, 5 đệ tử nhận hết, when thêm đệ tử thứ 6 muốn nhận, then pool rỗng — không có instance để nhận.
- `AC-002`: Given đệ tử nhận 1 instance và hoàn thành, when done, then output tự về bảo khố và instance mark done mà không cần báo cáo thủ công.
- `AC-003`: Given đệ tử đã hủy 1 instance hôm nay, when cố hủy thêm 1 instance khác hôm nay, then bị block — hết quota hủy ngày.
- `AC-004`: Given quota typed yêu cầu 2 loại A + 1 loại B, player hoàn thành 3 loại A và 0 loại B, when quota check, then chưa đủ quota.
- `AC-005`: Given NV tự nguyện có thưởng 100 LS, when tạo NV, then 100 LS bị escrow khỏi bảo khố ngay lập tức.
- `AC-006`: Given NV tự nguyện đã nhận, hết deadline chưa xong, when deadline passes, then instance về pool kèm thưởng nguyên vẹn; escrow giữ nguyên.
- `AC-007`: Given NV tự nguyện bị hủy chủ động, when hủy confirmed, then escrow hoàn về bảo khố.
- `AC-008`: Given NV cá nhân bị hủy chủ động, when hủy confirmed, then escrow hoàn về inventory người tạo — không vào bảo khố.
- `AC-009`: Given đệ tử đủ quota NV bắt buộc, balo đầy khi claim phúc lợi, when claim, then phúc lợi vào inbox.
- `AC-010`: Given đệ tử đủ quota và bị kick, when kick executes, then phúc lợi tự động resolve về tay đệ tử trước khi kick có hiệu lực.
- `AC-011`: Given pool NV bắt buộc được publish mà chưa config phúc lợi, when publish, then server block và báo lỗi.

## Runtime Flow

### Đầu tuần (weekly reset)
1. Server job chạy vào thời điểm reset (config).
2. Pool NV bắt buộc cũ bị xóa.
3. Môn chủ tạo pool mới từ template.
4. Snapshot số đệ tử eligible (không gồm gia nhập giữa tuần).
5. Escrow phúc lợi tuần từ bảo khố theo snapshot.
6. Đệ tử vào map nội bộ, nhận instance từ pool.

### Hoàn thành NV
1. Đệ tử thực hiện objective (khai thác, tinh luyện...).
2. Server detect objective complete.
3. Output → bảo khố tự động.
4. Instance mark done.
5. Đệ tử nhận thông báo.
6. Server update quota progress của đệ tử.

### Claim phúc lợi
1. Đệ tử đủ quota → nút Claim phúc lợi active.
2. Đệ tử nhấn Claim.
3. Server check balo: đủ chỗ → vào balo; đầy → inbox.
4. Escrow resolve cho đệ tử đó.

## Rules And Invariants

- Output nhiệm vụ tự về bảo khố — không cần thao tác thủ công.
- Escrow lock là bất khả xâm phạm cho đến khi resolve đúng flow.
- Hủy task chủ động là cách duy nhất hoàn escrow về source.
- Hết deadline không giải phóng escrow — chỉ giải phóng khi hủy chủ động.
- Đệ tử gia nhập giữa tuần không nhận NV bắt buộc tuần hiện tại, không nhận phúc lợi tuần đó.
- Pool NV bắt buộc bắt buộc phải đi kèm phúc lợi — không có phúc lợi thì không publish pool.

## Data / Config Requirements

| Config key | Notes |
|---|---|
| `sect.quest_pool_reset_day` | Ngày reset pool (đầu tuần) |
| `sect.welfare_escrow_day` | Ngày escrow phúc lợi (đầu tuần) |
| `sect.member_quest_cancel_per_day` | Giới hạn hủy NV bắt buộc/ngày (1) |

- Task template schema: template_id, type, output_type, output_amount, description.
- Task instance schema: instance_id, template_id, sect_id, week, assigned_to, status, created_at, deadline.
- Quota config per week: sect_id, quota_type (free/typed), quota_value.
- Welfare config per week: sect_id, welfare_item, welfare_amount_per_member.
- Welfare escrow record: sect_id, week, snapshot_count, total_escrowed, resolved_count.

## Telemetry / Logs / Debug Needs

- Log instance created/assigned/done/cancelled: sect_id, instance_id, player_id, action, timestamp.
- Log welfare escrow: sect_id, week, snapshot_count, amount.
- Log welfare claim: sect_id, player_id, result (balo/inbox).
- Log quota progress per player per week.

## Related Systems

- `requirements/sect-core.md` — prerequisite.
- `requirements/inbox-mail-system.md` — phúc lợi balo đầy → inbox.
- `shared-rules.md` — Escrow rule.

## Blocking Questions

- **None** — design đã chốt. TechDesign verify: typed quota validation server-side có cần block nhận sai loại không (feature doc note: requires_code_verification).

## Known Conflicts / Drift

- Không có conflict. Escrow rule canonical đã ở shared-rules.md.

## Readiness Level

- Ready for TechDesign: **yes**
- Ready for Dev handoff: **pending** — cần sect-core implement xong
- Ready for QA: **no**

## Handoff Checklist

- [x] No blocking design questions.
- [x] Acceptance criteria testable.
- [x] Config/data outlined.
- [x] Prerequisite explicit.
- [x] `handoff_ready: true`
