---
doc_type: game_design_feature
system_id: tribulation-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-13
updated_at: 2026-05-13
promoted_from: features/tribulation-system.md
requires_code_verification: false
related_docs:
  - features/death-penalty.md
  - features/cultivation-and-breakthrough.md
---

# Hệ Thống Lôi Kiếp — Feature Draft

## Purpose

Thiết kế sự kiện Lôi Kiếp — thử thách vượt cảnh giới đặc biệt áp dụng cho player ở cảnh giới cao (từ Hoá Thần trở lên). Lôi Kiếp thay thế cơ chế thọ nguyên ở cảnh giới thấp bằng một áp lực khác: thử thách định kỳ bắt buộc, thất bại thì tụt cảnh giới.

## Core Fantasy / Player Value

- Cảm giác hành trình tu tiên có rủi ro thật — cảnh giới cao không an toàn.
- Lôi Kiếp là sự kiện đặc biệt, tạo điểm nhấn trong vòng đời nhân vật.
- Thử thách cá nhân hoàn toàn — không có hỗ trợ từ bên ngoài.

## Khi Nào Lôi Kiếp Xảy Ra

- Cảnh giới từ **Hóa Thần Sơ Kỳ (realm 19) trở lên**: không còn thọ nguyên, thay vào đó có **đếm ngược đến Lôi Kiếp**.
- Áp dụng cho 13 cảnh giới: Hóa Thần (19–21), Luyện Hư (22–24), Hợp Thể (25–27), Độ Kiếp (28), Chân Tiên (29–31).
- Mỗi lần player **chết** ở cảnh giới này: đếm ngược bị rút ngắn (xem `features/death-penalty.md`).
- Khi đếm ngược về 0:
  - Nếu player **đang online**: Lôi Kiếp kích hoạt ngay lập tức.
  - Nếu player **đang offline**: countdown bị **hold** ở 0, đợi player online rồi mới xử lý. Khi online lại: dừng tu luyện/hoạt động đang chạy và tele vào map Lôi Kiếp.
- Sau khi vượt Lôi Kiếp thành công hoặc thất bại: **đếm ngược reset về ban đầu** theo config cảnh giới.

## Cơ Chế Lôi Kiếp

### Map Lôi Kiếp (Private)
- Khi Lôi Kiếp kích hoạt:
  - Nếu player đang ở **map thường** (không phải PvP map): tele vào **map Lôi Kiếp private** ngay lập tức.
  - Nếu player đang ở **map PvP**: khi rời map PvP sang map thường → tele vào map Lôi Kiếp ngay lập tức.
- Map Lôi Kiếp là **instance riêng, chỉ có 1 mình player** — không ai vào được, không bị tấn công.

### Thử Thách
- Lôi Kiếp là **màn PvP với boss sấm sét** được thiết kế riêng.
- Điều kiện vượt qua (1 trong 2):
  - **Đánh bại boss** trong thời gian quy định.
  - **Sống sót** đến hết thời gian quy định (không chết).
- Điều kiện thất bại:
  - **Chết** trong map Lôi Kiếp.

### Hoãn Lôi Kiếp
- Có thể **gia tăng thời gian đếm ngược Lôi Kiếp** bằng item phù hợp.
- Về bản chất, đây là cơ chế tương tự **tăng thọ nguyên** ở cảnh giới thấp: không hủy sự kiện, chỉ đẩy mốc thời gian lùi ra sau.
- Thiết kế item / lượng thời gian tăng thêm → phase data design / economy.

### Vượt Qua Thành Công
- Player thoát map Lôi Kiếp, quay lại world bình thường.
- Đếm ngược reset về ban đầu theo cảnh giới.
- Không có penalty — tiếp tục bình thường.

### Thất Bại
- Player **tụt 1 cảnh giới** — áp dụng **Cultivation Penalty Rule** (xem `shared-rules.md`): cultivation bị trừ %, potential revert, chỉ số giảm theo.
- **Không có bình cảnh** khi leo lại sau khi tụt.
- Đếm ngược Lôi Kiếp reset về ban đầu theo config cảnh giới mới (sau khi tụt).
- Linh thú, Khôi Lỗi, Tông Môn **không hỗ trợ** được — thử thách cá nhân hoàn toàn.
- Chỉ dùng **skill cá nhân** của nhân vật.

## Giới Hạn Trong Map Lôi Kiếp

| Hành động | Được phép |
|---|---|
| Dùng skill cá nhân | ✅ |
| Dùng item trong balo | ✅ Bình thường như PvP |
| Triệu hồi Linh Thú | ❌ |
| Triệu hồi Khôi Lỗi | ❌ |
| Nhận hỗ trợ từ Tông Môn | ❌ |
| Bị tấn công bởi player khác | ❌ (map private) |

## States

| State | Mô tả |
|---|---|
| `tribulation_pending` | Đếm ngược đang chạy, chưa về 0 |
| `tribulation_active` | Player đang trong map Lôi Kiếp |
| `tribulation_passed` | Vừa vượt qua, đếm ngược reset |
| `tribulation_failed` | Vừa thất bại, tụt cảnh giới |

## Data / Config Needs

- `tribulation_countdown_per_realm` — thời gian đếm ngược ban đầu per cảnh giới (Hoá Thần trở lên)
- `tribulation_boss_id_per_realm` — boss sấm sét tương ứng per cảnh giới
- `tribulation_duration` — thời gian giới hạn mỗi trận Lôi Kiếp
- `tribulation_death_countdown_reduction` — lượng rút ngắn đếm ngược per lần chết (có thể trùng config death penalty)
- Map Lôi Kiếp private instance per cảnh giới

## Open Questions

- [x] Item trong balo dùng được bình thường như PvP.
- [x] Cultivation sau khi tụt: tính theo % cultivation bị trừ của Cultivation Penalty Rule — không reset về 0 hay max.
- [x] Nếu online khi countdown về 0: dừng tu luyện/hoạt động và tele ngay. Nếu offline: hold ở 0, đợi player online rồi mới dừng hoạt động và tele.
- [x] Có thể hoãn bằng cách tăng thêm thời gian đếm ngược bằng item phù hợp.
- [x] Map Lôi Kiếp dùng **cùng 1 template**; khác nhau ở boss và sức mạnh theo cảnh giới.
- [x] Thất bại Lôi Kiếp **không** rớt đồ hay mất linh thạch; penalty duy nhất là Cultivation Penalty Rule (tụt cảnh giới / potential revert nếu có).

## Related Systems

- **Death Penalty**: rule rút ngắn đếm ngược Lôi Kiếp khi chết → `features/death-penalty.md`
- **Tu Luyện & Đột Phá**: cảnh giới, thọ nguyên, cultivation flow → `features/cultivation-and-breakthrough.md`
- **Combat / Skill**: Lôi Kiếp là combat với boss — dùng chung skill pipeline


## Requirement Readiness Checklist

- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
