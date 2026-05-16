---
handoff_id: 20260515-21
queue_id: 45
title: Inbox / Mail System — TechDesign Spec
type: requirement-to-techdesign
status: Ready
owner: techdesign
source_design_doc: requirements/inbox-mail-system.md
feature_doc: features/inbox-mail-system.md
created_at: 2026-05-15
created_by: gamedesign
iteration: 1
response_to: null
supersedes: null
---

# Handoff: Inbox / Mail System — TechDesign

## Summary

Inbox là kênh server → player một chiều để nhận reward overflow và admin grant. Item không hết hạn. Claim là atomic all-or-nothing. Phân tab, retention limit 50 unread + 50 read (1 ngày) per tab.

Requirement doc đầy đủ tại: `requirements/inbox-mail-system.md`

---

## TechDesign cần làm

### 1. Verify runtime existence
- Inbox system đã có trong server chưa? DB schema, service layer, packet handler?
- Nếu có: map behavior hiện tại với requirement — ghi drift vào tech-design doc.
- Nếu chưa: thiết kế từ đầu theo requirement.

### 2. Tech spec các điểm cần xác định

| Điểm | Câu hỏi cần chốt |
|---|---|
| Storage | Inbox entries lưu DB hay cache? Schema entry gồm những field gì? |
| Tab model | Tab được lưu thế nào — enum, config table, hay hardcode? |
| Retention cleanup | Job định kỳ hay trigger on-push? Frequency? |
| Claim atomicity | Transaction scope khi claim — bao nhiêu items có thể trong 1 entry? |
| Admin grant | API endpoint hay in-game tool? Batch size limit? |
| Push khi offline | Entry ghi DB ngay; client nhận khi login — cần push queue riêng không? |
| Badge sync | Badge state sync với client thế nào — server push hay client poll? |

### 3. Packet / protocol spec
- Packet danh sách inbox entries per tab.
- Packet claim (single + all).
- Packet delete entry.
- Packet badge update / unread count notification.
- Error codes: `INVENTORY_FULL`, `ENTRY_NOT_FOUND`, `ALREADY_CLAIMED`.

### 4. Output
- `tech-design/inbox-mail-system.md` — schema, service design, packet spec, retention job design.

---

## Key design rules (không thay đổi)

- Inbox là **server → player một chiều** — không có player-to-player path.
- Item trong inbox **không expire** — chỉ mất do retention overflow hoặc player xóa thủ công.
- Claim là **all-or-nothing** — không partial grant.
- **Ground loot, herb action, NPC buy** không bao giờ trigger inbox push.
- Retention: 50 unread + 50 read (1 ngày) per tab; oldest bị xóa khi overflow (items lost).

---

## Acceptance Gate (TechDesign verify trước khi Dev handoff)

- [ ] Runtime existence confirmed (có hay chưa có).
- [ ] DB schema drafted.
- [ ] Retention cleanup approach decided.
- [ ] Claim atomicity approach decided.
- [ ] Packet list drafted.
- [ ] `tech-design/inbox-mail-system.md` created.
