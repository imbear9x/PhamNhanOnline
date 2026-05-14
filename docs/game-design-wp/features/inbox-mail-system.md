---
doc_type: game_design_feature
system_id: inbox-mail-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-14
updated_at: 2026-05-14
promoted_from: notes/inbox-mail-system.md
related_docs:
  - features/main-progression-quest-chain.md
  - features/sect-system.md
  - shared-rules.md
requires_code_verification: false
---

# Hệ Thống Inbox / Hòm Thư — Feature Draft

## Goal

Cung cấp một kênh system → player để nhận reward overflow, thông báo hệ thống, và các vật phẩm mà server cần gửi chủ động mà không thể đặt trực tiếp vào balo player.

## Design Summary

Inbox là hộp thư một chiều: chỉ server gửi, player nhận. Player không thể gửi item hay tin nhắn cho nhau qua inbox.

Inbox không có cơ chế hết hạn — item được giữ cho đến khi player chủ động nhận và xóa. Điều này đảm bảo player không mất reward do quên đăng nhập, và không cần hệ thống warning expire phức tạp.

Inbox phân tab theo loại nguồn. Mỗi tab giữ tối đa 50 tin chưa đọc và 50 tin đã đọc (trong vòng 1 ngày). Overflow xóa tin cũ nhất.

## Scope

### In Scope
- Nhận reward overflow khi balo đầy lúc claim
- Nhận reward hàng loạt từ admin (bồi thường server, quà sự kiện)
- Nhận welfare / task reward tông môn
- Nhận reward hệ thống khác (event, v.v.)
- Claim từng item hoặc claim all
- Phân tab theo loại nguồn
- Badge thông báo trên icon inbox
- Text đính kèm mỗi entry

### Out Of Scope
- Player gửi item / mail cho nhau — inbox là kênh system → player duy nhất
- Welfare / event reward tự chuyển inbox khi offline — nếu player không nhấn nhận thì mất; chỉ khi nhấn nhận mà balo đầy mới vào inbox
- Hệ thống expire / cảnh báo hết hạn

## Core Loop

1. Server tạo event hoặc player nhận reward.
2. Nếu balo còn chỗ: item vào thẳng balo.
3. Nếu balo đầy (hoặc server chủ động gửi): item đi vào inbox.
4. Player mở inbox, đọc entry, nhấn Nhận (hoặc Nhận Tất Cả).
5. Server kiểm tra balo trước khi claim — nếu không đủ chỗ: từ chối claim, hiện thông báo.
6. Claim thành công → item vào balo, entry chuyển sang trạng thái "đã đọc".
7. Player xóa entry thủ công khi muốn dọn inbox.

## Player-Facing Rules

### Nguồn item vào inbox

| Nguồn | Điều kiện vào inbox |
|---|---|
| Reward overflow | Balo đầy lúc claim |
| Admin gửi quà / bồi thường server | Gửi thẳng vào inbox bất kể balo |
| Event reward gửi hàng loạt | Gửi thẳng vào inbox bất kể balo |
| Sect welfare / task reward | Balo đầy lúc claim |

**Lưu ý quan trọng:** Welfare / event reward tự expire trước khi player nhấn nhận → mất luôn. Inbox chỉ nhận khi player **đã nhấn nhận** mà balo không đủ chỗ.

### Expire / Retention

- Item trong inbox **không hết hạn**.
- Entry chỉ bị xóa khi player chủ động xóa, hoặc khi bị đẩy khỏi inbox do overflow retention limit.
- Không có cơ chế warning hết hạn — không cần thiết vì không expire.

### Retention limit

| Trạng thái entry | Giới hạn | Overflow behavior |
|---|---|---|
| Chưa đọc | 50 tin / tab | Tin cũ nhất bị xóa khi vượt 50 |
| Đã đọc | 50 tin / tab | Tin cũ nhất bị xóa sau 1 ngày hoặc khi vượt 50 |

**Hệ quả:** Nếu inbox overflow unread, player mất entry cũ nhất. Đây là edge case — 50 unread entries là ngưỡng khá cao trong điều kiện bình thường.

### Claim behavior

- **Claim từng item**: server check balo trước, nếu không đủ chỗ → từ chối, hiện thông báo.
- **Claim All**: server check tổng slot cần thiết cho tất cả items đang chọn; nếu không đủ cho toàn bộ → từ chối toàn bộ, hiện thông báo. Không claim một phần.
- Balo đầy → player phải dọn balo trước, sau đó thử lại.

### Text đính kèm

- Mỗi entry inbox có **text mô tả nguồn** (ví dụ: "Thưởng nhiệm vụ: [Tên nhiệm vụ]", "Phúc lợi tuần Tông Môn", "Bồi thường bảo trì server").
- Text do server sinh tự động theo loại event; không có mail text tự do từ player.

## System States

- **Inbox rỗng**: không có entry, badge ẩn.
- **Có entry chưa đọc**: badge hiển thị trên icon inbox.
- **Tất cả đã đọc, chưa xóa**: badge có thể ẩn hoặc giảm cấp (tùy UX decision).
- **Inbox full (50 unread)**: entry mới vào → entry cũ nhất bị đẩy ra.

## Main Flows

### Flow 1 — Reward overflow vào inbox

1. Player nhấn Nhận reward (quest, welfare, v.v.).
2. Server check balo.
3. Balo đủ chỗ → item vào balo, done.
4. Balo đầy → item vào inbox với text mô tả nguồn.
5. Badge xuất hiện trên icon inbox.

### Flow 2 — Admin gửi hàng loạt

1. Admin tạo batch send (bồi thường, quà sự kiện).
2. Server push entry vào inbox từng player.
3. Badge xuất hiện trên icon inbox của player khi đăng nhập hoặc nhận push.

### Flow 3 — Claim từ inbox

1. Player mở inbox, chọn tab phù hợp.
2. Nhấn Nhận (1 entry) hoặc Nhận Tất Cả.
3. Server check balo:
   - Đủ chỗ → claim thành công, item vào balo, entry chuyển trạng thái đã đọc.
   - Không đủ → từ chối toàn bộ, hiện thông báo "Balo không đủ chỗ".
4. Player xóa entry thủ công sau khi đã nhận.

## Edge Cases

- **Balo đầy khi claim all một phần**: không cho phép claim một phần — từ chối toàn bộ, yêu cầu dọn balo.
- **Inbox overflow 50 unread**: entry cũ nhất bị xóa kể cả nếu còn item chưa nhận → item trong entry đó mất. Player cần định kỳ dọn inbox.
- **Player offline khi admin gửi hàng loạt**: entry vẫn vào inbox; player nhận khi đăng nhập lại.
- **Event reward expire trong lúc entry đang nằm inbox**: không xảy ra vì item đã vào inbox thì không expire.
- **Entry đã đọc quá 1 ngày**: bị xóa tự động khỏi tab đã đọc.

## Data / Config Needs

| Config | Giá trị mặc định | Ghi chú |
|---|---|---|
| `inbox.max_unread_per_tab` | 50 | Giới hạn entry chưa đọc mỗi tab |
| `inbox.max_read_per_tab` | 50 | Giới hạn entry đã đọc mỗi tab |
| `inbox.read_retention_days` | 1 | Số ngày giữ entry đã đọc |

## UI / UX Notes

- Badge thông báo chỉ hiển thị **trên icon inbox** — không xuất hiện trên màn hình chính hay shortcut.
- Inbox phân tab theo loại nguồn (ít nhất: Hệ Thống, Tông Môn, Sự Kiện — tab cụ thể xác định khi build UI).
- Mỗi entry hiển thị: icon loại item, số lượng, text mô tả nguồn, thời gian nhận.
- Nút Nhận và Nhận Tất Cả đều visible; Nhận Tất Cả chỉ active khi có ít nhất 1 entry chưa nhận.
- Khi từ chối claim do balo đầy: toast / popup ngắn gọn, không block toàn màn hình.

## Related Systems

- **Reward Overflow** (`shared-rules.md`): rule canonical về overflow vào inbox.
- **Quest** (`features/main-progression-quest-chain.md`): quest reward overflow dùng inbox.
- **Sect** (`features/sect-system.md`): welfare / task reward overflow dùng inbox.

## Key Decisions

1. Inbox là kênh **system → player một chiều**; player không gửi được cho nhau.
2. Item inbox **không hết hạn** — giữ cho đến khi player nhận hoặc bị đẩy do overflow.
3. Welfare / event expire **trước khi nhận** → mất luôn; inbox không bảo vệ item chưa được nhấn nhận.
4. Claim (single hoặc all) đều **check balo trước** — từ chối nếu không đủ chỗ; không claim một phần.
5. Retention: 50 unread + 50 read (1 ngày) per tab; cũ nhất bị đẩy khi overflow.
6. Badge chỉ trên **icon inbox**.
7. Inbox phân **tab theo loại nguồn**.

## Open Questions

- [x] Tab inbox: Hệ Thống / Tông Môn / Sự Kiện / Tin Nhắn Bạn Bè — thêm tab nếu cần khi build UI.
- [x] Badge: chấm đỏ — không hiển thị số đếm.
- [x] Claim All không giới hạn số entry/lần.

## Known Conflicts / Drift

- Không có conflict nào ghi nhận.

## Requirement Readiness Checklist

- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
