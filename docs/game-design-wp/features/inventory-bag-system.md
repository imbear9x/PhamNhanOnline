---
doc_type: game_design_feature
system_id: inventory-bag-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: null
related_docs:
  - features/npc-system.md
  - features/inbox-mail-system.md
  - shared-rules.md
requires_code_verification: false
---

# Hệ Thống Túi Trữ Vật — Feature Draft

## Goal

Mỗi player có đúng 1 túi trữ vật là container chứa item. Túi có cấp, số slot tăng theo cấp. Nâng cấp túi bằng cách mua túi cấp cao hơn từ NPC — đồ cũ giữ nguyên, chuyển vào túi mới. Không thể hạ cấp. Túi không phải item, không giao dịch được.

## Design Summary

Khi tạo nhân vật, player được trang bị túi cấp 1 (cấp thấp nhất). Túi là một thuộc tính gắn liền với nhân vật, không phải item trong inventory. Không thể drop, trade, hay mất túi. Nâng cấp túi bằng cách mua túi cấp cao hơn từ NPC bằng linh thạch — server thay túi hiện tại bằng túi mới, toàn bộ item trong túi cũ chuyển sang túi mới nguyên vẹn. Không thể trang bị túi cấp thấp hơn cấp hiện tại. Túi là per-character — mỗi nhân vật có túi riêng, không share account.

## Scope

### In Scope
- Túi trữ vật: container gắn với nhân vật, có cấp và số slot
- 4 cấp túi: số slot theo data config
- Khởi tạo: nhân vật mới có túi cấp 1
- Nâng cấp túi: mua từ NPC bằng linh thạch
- Khi nâng cấp: đồ cũ chuyển sang túi mới nguyên vẹn
- Block hạ cấp: cả UI và server
- Túi không phải item: không drop, không trade, không mail
- Hiển thị cấp túi và số slot trên UI
- Per-character: mỗi nhân vật có túi riêng

### Out Of Scope
- Số slot cụ thể per cấp — data design / balance
- Giá linh thạch per cấp — data design / balance
- Shared storage giữa các character cùng account — không có trong V1
- Túi theo loại item (ví dụ túi vũ khí riêng, túi nguyên liệu riêng) — không có

## Túi Trữ Vật

### Định nghĩa
- Túi là **container**, không phải item.
- Gắn liền với **nhân vật** (per-character), không phải account.
- Không thể bị mất, drop, trade, hay mail cho người khác.
- Hiển thị trên UI: **cấp túi** và **số slot hiện tại / tổng slot**.

### Cấp túi
- Có **4 cấp** — cấp 1 là thấp nhất, cấp 4 là cao nhất.
- Số slot per cấp: config per cấp trong data — tham khảo balance hiện tại: cấp 1 = 50 slot, mỗi cấp tiếp theo x2.
- **Không thể hạ cấp** — đây là invariant cứng.

### Khởi tạo
- Nhân vật mới tạo: được cấp túi cấp 1 tự động — không cần player làm gì.

## Nâng Cấp Túi

### Luồng nâng cấp
1. Player mua túi cấp cao hơn từ **NPC** bằng **linh thạch**.
2. NPC chỉ bán túi cấp cao hơn cấp hiện tại của player — không bán cùng cấp, không bán thấp hơn.
3. Server validate: túi muốn mua phải cao hơn cấp hiện tại.
4. Server thay túi cũ bằng túi mới.
5. Toàn bộ item trong túi cũ chuyển sang túi mới **nguyên vẹn** (giữ đúng vị trí nếu có thể, hoặc pack vào slot từ đầu nếu cần).
6. Số slot mới = số slot của cấp túi mới.

### Block hạ cấp
- UI: không hiển thị / disable option mua túi cấp thấp hơn hoặc cùng cấp.
- Server: nếu nhận được request mua túi cấp ≤ cấp hiện tại → reject, không xử lý.

### Không mất đồ khi nâng cấp
- Tất cả item giữ nguyên khi chuyển túi.
- Nâng cấp không bao giờ gây mất item.
- Số item trong túi cũ luôn ≤ số slot túi mới (vì túi mới luôn cao cấp hơn = nhiều slot hơn).

## Inventory Full và Overflow

- Túi đầy = không còn slot trống.
- Các action nhận item khi túi đầy đều theo **shared inbox overflow rule** (xem `shared-rules.md`):
  - drop từ quái
  - quest reward
  - mail attachment claim
  - event reward
- Các action **bị reject** khi túi đầy (không inbox fallback):
  - harvest linh thảo từ ô trồng
  - extract linh thảo trong túi
  - (các action khác theo rule riêng của từng hệ)

## Edge Cases

- Player mua túi khi túi đang đầy: cho phép — túi mới có nhiều slot hơn, item chuyển sang, không có vấn đề gì.
- Nâng cấp túi trong combat: tùy rule — phase này không có combat restriction đặc biệt, nên cho phép.
- Character bị xóa: túi và toàn bộ item trong túi bị xóa cùng.
- Không có túi "cấp 0" hay túi trống — nhân vật luôn có túi cấp ít nhất là 1.

## Data / Config Needs

- Bag grade config table:
  - grade (1–4)
  - slot count
  - upgrade cost (linh thạch)
  - display name
- NPC shop config: túi các cấp có trong shop NPC nào, giá bao nhiêu → DB

## UI / UX Notes

- Inventory UI hiển thị: cấp túi hiện tại + số slot (ví dụ "Túi cấp 2 — 100/100 ô").
- NPC shop: chỉ hiển thị túi cấp cao hơn cấp hiện tại của player.
- Khi túi đầy: hiển thị cảnh báo khi player thực hiện action liên quan.

## Related Systems

- **NPC System** (`features/npc-system.md`): NPC bán túi nâng cấp.
- **Inbox Mail System** (`features/inbox-mail-system.md`): overflow khi túi đầy.
- **Shared Rules** (`shared-rules.md`): inbox overflow rule.
- Tất cả các hệ tạo ra item reward đều phụ thuộc vào túi trữ vật của player.

## Key Decisions

1. Túi không phải item — không drop, không trade, không mail.
2. Per-character — không share giữa các nhân vật cùng account.
3. Nhân vật mới tạo luôn có túi cấp 1.
4. Nâng cấp chỉ từ NPC bằng linh thạch, chỉ lên cấp cao hơn.
5. Không thể hạ cấp — block cả UI và server.
6. Nâng cấp không bao giờ làm mất item.
7. 4 cấp túi — số slot per cấp là data config.
8. Túi đầy + nhận item passive → inbox overflow. Túi đầy + action chủ động → reject.

## Open Questions
- Không có câu hỏi mở. Tất cả đã được user chốt.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
