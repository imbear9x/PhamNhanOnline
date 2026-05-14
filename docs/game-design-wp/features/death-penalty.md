---
doc_type: game_design_feature
system_id: death-penalty
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-08
updated_at: 2026-05-12
promoted_from: notes/death-penalty.md
related_docs:
  - features/home-cave-defense.md
  - features/spirit-beast.md
  - features/player-interaction-group.md
requires_code_verification: false
---

# Death Penalty — Feature Draft

## Goal

Tạo hệ thống trừng phạt khi chết có ý nghĩa nhưng không phá hủy tiến trình của người chơi. Chết vì bất kỳ lý do gì đều chịu penalty nền giống nhau: rớt đồ và bị trừ thọ nguyên / thời gian đến Lôi Kiếp tiếp theo. Hết thọ nguyên = chết vĩnh viễn.

## Design Summary

Penalty áp dụng đồng nhất bất kể nguyên nhân chết (PvE, Duel, PvP Zone, lawful PvP, PK). Không mất tu vi, không mất tiềm năng. Penalty nền gồm: có tỉ lệ rớt linh thạch và item, đồng thời bị trừ thọ nguyên hoặc rút ngắn thời gian đến Lôi Kiếp tiếp theo. Một số nguyên nhân chết đặc biệt có thể áp dụng penalty bổ sung do game design data cấu hình. Hết thọ nguyên = nhân vật chết vĩnh viễn, phải tạo nhân vật mới.

## Scope

### In Scope
- Drop linh thạch và item khi chết
- Quyền nhặt đồ rơi
- Mất buff khi chết
- Lựa chọn hồi sinh
- Penalty thọ nguyên khi chết (cảnh giới dưới Hoá Thần)
- Penalty Lôi Kiếp khi chết (cảnh giới trên Hoá Thần)
- Hết thọ nguyên — chết vĩnh viễn
- Tăng thọ nguyên bằng đan dược

### Out Of Scope
- Penalty khi thất bại Lôi Kiếp — defer, chưa thiết kế
- Chi tiết recipe / nguồn đan dược tăng thọ nguyên — defer đến phase economy
- Balance cụ thể (tỉ lệ rớt, lượng rút thọ nguyên, giá đan dược)

## Core Loop

1. Player chết.
2. Server tính penalty: có tỉ lệ rớt linh thạch + có tỉ lệ rớt item.
3. Trừ vào thọ nguyên / đếm ngược đến Lôi Kiếp tiếp theo theo rule cảnh giới.
4. Đồ rơi xuống map — chủ có thời gian ưu tiên nhặt lại, sau đó mới mở cho người khác.
5. Player chọn hồi sinh (về Động Phủ hoặc Checkpoint nếu có).
6. Nếu thọ nguyên về 0: nhân vật bị xóa, phải tạo mới.

## Player-Facing Rules

### Nguyên tắc chung
- Penalty áp dụng **bất kể chết vì lý do gì** (PvE, Duel, PvP Zone).
- **Không mất tu vi, không mất tiềm năng**.
- Chỉ rớt đồ trên người và luôn ảnh hưởng thọ nguyên/lôi kiếp theo rule chung khi chết.

### Drop khi chết

**Linh Thạch:**
- Có tỉ lệ rớt riêng.
- Nếu trúng: rơi **X% số linh thạch đang cầm** (X là random).
- Ngưỡng: **tối thiểu 1 viên, tối đa 10 viên** (tạm thời, config-driven).
- Thông số trong `game_configs`.

**Item khác:**
- Có tỉ lệ rớt riêng (ví dụ tạm thời 5%).
- Nếu trúng: **random 1 item** trong số những đồ có flag droppable.
- Item non-tradable / non-droppable → **không rơi**.
- Tỉ lệ trong `game_configs`.

### Quyền nhặt đồ rơi
- Đồ rơi thuộc quyền **player chết** trong một khoảng thời gian (cấu hình `game_configs`).
- Trong thời gian đó: người khác **nhìn thấy đồ nhưng không nhặt được** — nhấn nhặt nhận thông báo "chưa thể nhặt, cần đợi X giây".
- Sau thời gian ưu tiên: đồ trở thành **public**, ai cũng nhặt được.
- Pet auto-loot của chủ nhân vẫn nhặt được đồ của chủ trong priority window.
- Trong looting window sau cấu trúc vỡ: rule này **vẫn áp dụng** cho death drop; structure drops là public ngay (xem `shared-rules.md` Ownership / Drop Rights).

### Buff khi chết
- **Mất buff từ skill** (buff chiến đấu, thiết giáp...).
- **Giữ buff từ bùa chú / trận pháp** — thứ bên ngoài nhân vật, không phải trạng thái nội tại.

### Hồi sinh
Khi chết, player chọn:
- **Về Động Phủ (home)** — luôn luôn có.
- **Về Checkpoint** — chỉ nếu map hiện tại có checkpoint và cho phép (thường chỉ trong dungeon/phó bản).

### Thọ Nguyên (realm 1–18: Luyện Khí → Nguyên Anh Hậu Kỳ)

**Cơ chế đột phá:**
- Mỗi cảnh giới có pool thọ nguyên riêng (config theo cảnh giới).
- Khi đột phá → cộng thêm **phần chênh lệch** giữa pool mới và pool cũ.
- Ví dụ: pool hiện tại 1 tháng, còn 1 ngày. Pool mới 2 tháng. Chênh lệch 1 tháng. Kết quả: còn 1 tháng 1 ngày.

**Penalty thọ nguyên khi chết:**
- Rút ngắn thọ nguyên trực tiếp (ví dụ -2 phút mỗi lần chết).
- Lượng rút ngắn trong `game_configs`.

**Tăng thọ nguyên:**
- Một số đan dược hiếm có thể tăng/hồi phục thọ nguyên.
- Ví dụ: còn 1 ngày → uống đan → còn 1 tuần.
- Item loại này có thể hiếm, gắn với nạp tiền hoặc drop đặc biệt.
- Chi tiết recipe/nguồn gốc xác định khi làm economy.

**Hết thọ nguyên:**
- Nhân vật **chết vĩnh viễn**, phải tạo nhân vật mới (tên mới).

### Lôi Kiếp (realm 19–31: Hóa Thần Kỳ trở lên)
- Không có thọ nguyên, thay vào đó có **đếm ngược đến Lôi Kiếp**.
- Khi chết: rút ngắn đếm ngược đến Lôi Kiếp (ví dụ -2 phút).
- Lượng rút ngắn trong `game_configs`.
- **Chết do không vượt qua Lôi Kiếp (thất bại Lôi Kiếp)**: tụt 1 cảnh giới → áp dụng **Cultivation Penalty Rule** (xem `shared-rules.md`). Đây không phải death penalty thông thường — là penalty riêng của Lôi Kiếp.

## System States

### Thọ Nguyên
- **Còn thọ nguyên**: đếm ngược bình thường.
- **Cảnh báo thọ nguyên thấp**: threshold cần xác định, hiển thị warning.
- **Hết thọ nguyên**: nhân vật bị xóa.

### Lôi Kiếp
- **Đang đếm ngược**: bình thường.
- **Lôi Kiếp đến**: kích hoạt sự kiện Lôi Kiếp (detail defer).

## Edge Cases
- Chết tại dungeon có checkpoint: có thể chọn về checkpoint thay vì về nhà.
- Chết do bị công động phủ khi thủ nhà: penalty nặng hơn, không hồi sinh ngay — xem `features/home-cave-defense.md`.
- Chết trong looting window (sau khi cổng động phủ / tông môn vỡ): respawn về Động Phủ cá nhân nếu có; nếu không có Động Phủ → tele map random. Rule penalty bình thường vẫn áp dụng.
- Duel, PvP Zone, cave raid, sect war, mineral conflict, và PK đều chịu penalty nền giống nhau; khác biệt chỉ đến từ penalty bổ sung nếu feature đó định nghĩa.
- Thọ nguyên về 0 trong lúc đang trong dungeon: nhân vật xóa ngay, không chờ thoát dungeon.

## Data / Config Needs
- Tỉ lệ rớt linh thạch khi chết → `game_configs`
- X% linh thạch rớt (random range) → `game_configs`
- Ngưỡng min/max linh thạch rớt (tạm 1–10 viên) → `game_configs`
- Tỉ lệ rớt item khi chết (tạm 5%) → `game_configs`
- Thời gian bảo lưu quyền nhặt đồ rơi → `game_configs`
- Lượng rút ngắn thọ nguyên / Lôi Kiếp per lần chết do PK → `game_configs`
- Pool thọ nguyên theo từng cảnh giới → DB config theo cảnh giới

## UI / UX Notes
- Hiển thị thanh thọ nguyên đếm ngược rõ ràng trong UI.
- Cảnh báo khi thọ nguyên còn ít (threshold cần xác định).
- Khi chết: popup chọn hồi sinh (Về nhà / Checkpoint nếu có).
- Hiển thị đồ rơi và thời gian còn quyền nhặt.

## Related Systems
- **Động Phủ**: penalty chết khi công/thủ phủ nặng hơn — xem `features/home-cave-defense.md`
- **Linh Thú**: pet chết giảm tu vi, không áp dụng thọ nguyên giống player — xem `features/spirit-beast.md`
- **PvP State**: định nghĩa thế nào là "chết do PK" — xem `features/player-interaction-group.md`

## Key Decisions
1. Penalty giống nhau bất kể nguyên nhân chết.
2. Không mất tu vi, không mất tiềm năng.
3. Rớt linh thạch: tỉ lệ riêng + % random + ngưỡng min/max.
4. Rớt item: tỉ lệ riêng + random 1 item droppable.
5. Đồ rơi: chủ có quyền ưu tiên nhặt một thời gian trước khi mở public.
6. Mất buff skill, giữ buff bùa chú/trận pháp.
7. Chết do PK: trừ thọ nguyên / rút ngắn Lôi Kiếp.
8. Cơ chế đột phá cảnh giới: cộng phần chênh lệch pool, không reset.
9. Hết thọ nguyên = chết vĩnh viễn, tạo nhân vật mới.

## Open Questions
- [x] Penalty Lôi Kiếp thất bại — tụt 1 cảnh giới. Xem `features/tribulation-system.md`.
- [ ] Chi tiết đan dược tăng thọ nguyên — defer đến phase economy/item.
- [ ] "Chết do PK" định nghĩa chính xác thế nào khi liên quan đến các PvP state? — cần đồng bộ với `features/player-interaction-group.md`.
- [ ] Threshold cảnh báo thọ nguyên thấp là bao nhiêu?

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
