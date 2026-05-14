---
doc_type: game_design_feature
system_id: spirit-sense
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-08
updated_at: 2026-05-12
promoted_from: notes/spirit-sense-system.md
related_docs:
  - features/home-cave-defense.md
  - features/crafting-talisman-formation.md
requires_code_verification: false
---

# Hệ Thống Thần Thức — Feature Draft

## Goal

Tạo một chỉ số **Thần Thức** cho mọi thực thể sống trong game (người chơi, quái, boss, trận nhãn) quyết định khả năng **nhìn thấy, tương tác, và tấn công** giữa các thực thể. Hệ thống này là nền tảng cho cơ chế tàng hình, PvP ẩn nấp, và phân tầng phòng thủ động phủ.

## Design Summary

Thần Thức hoạt động theo mô hình **bandwidth / slot** — không phải resource tiêu hao theo thời gian. Thần Thức biểu diễn **dung lượng xử lý đồng thời** của thực thể. Một phần cố định được **reserved bắt buộc** cho hoạt động cơ bản của player; phần còn dư player có thể dùng để triệu hồi Linh Thú hoặc Khôi Lỗi, mỗi thứ chiếm một lượng slot cố định. Thần Thức cũng quyết định ngưỡng nhìn thấy giữa các thực thể. Tàng hình là skill, tốn **mana** để duy trì, không tốn Thần Thức.

## Scope

### In Scope
- Thần Thức là stat của mọi thực thể: player, quái, boss, trận nhãn
- Rule nhìn thấy dựa trên ngưỡng X%
- Tàng hình chủ động — tốn mana để duy trì
- Lộ diện khi bị tấn công
- Áp dụng cho trận pháp, phù lục ẩn

### Out Of Scope
- Mastery hay kỹ năng đặc thù theo nghề
- Balance cụ thể (slot reserved, ngưỡng nhìn thấy, mức tiêu hao mana khi tàng hình)
- Chi tiết UI/UX hiển thị Thần Thức

## Core Loop

1. Player tăng Thần Thức qua tu luyện / trang bị / skill.
2. Khi muốn tàng hình: bật thủ công → tiêu hao mana liên tục.
3. Đối thủ có Thần Thức < X% Thần Thức của mình → không nhìn thấy khi đang ẩn.
4. Bị tấn công → lộ diện X giây → tất cả nhìn thấy.
5. Hết X giây + còn đủ mana / vẫn duy trì skill hợp lệ → tự tàng hình lại.

## Player-Facing Rules

### Chỉ số Thần Thức
- Hoạt động theo mô hình **slot / bandwidth**, không tiêu hao theo thời gian.
- Có **tối đa** (tăng qua tu luyện, phân bổ tiềm năng, trang bị, skill buff).
- Một phần cố định được **reserved bắt buộc** cho hoạt động cơ bản của player (di chuyển, dùng skill, chiến đấu). Phần reserved này **cố định mọi cảnh giới**, không scale.
- Phần còn dư là **slot tự do** — player dùng để triệu hồi Linh Thú hoặc Khôi Lỗi.
- Mỗi Linh Thú / Khôi Lỗi chiếm một lượng slot cố định (tạm coi bằng nhau, xác định khi balance).
- Không có giới hạn density map riêng cho companion; slot Thần Thức là giới hạn summon chính ở layer shared rule.
- Quái/boss có Thần Thức fixed theo template trong DB, không scale theo map.

### Rule nhìn thấy (ngưỡng X%)
- Config toàn server: ngưỡng X% (ví dụ 40%).
- Nếu Thần Thức của A **< X% Thần Thức của B** → A không nhìn thấy B khi B đang ẩn.
- B luôn có Thần Thức > X% của A → B luôn nhìn thấy A.
- Rule **một chiều**: kẻ yếu hơn mù trước kẻ mạnh hơn.

**Ví dụ (X = 40%):**
- A Thần Thức 100, B Thần Thức 249: 100 > 40% × 249 (≈99.6) → A vẫn thấy B.
- A Thần Thức 100, B Thần Thức 251: 100 < 40% × 251 (≈100.4) → A không thấy B khi B ẩn.

### Tàng hình chủ động
- Phải **chủ động bật** — không tự động.
- Tàng hình là **skill** → tốn **mana** để duy trì, không tốn Thần Thức.
- Hết mana → tàng hình tắt, lộ diện.
- Tốc độ tiêu hao mana khi tàng hình → config trong `game_configs`.

**Ý nghĩa gameplay:**
- Thần Thức cao = ngưỡng nhìn thấy cao hơn, ẩn sâu hơn.
- Mana quyết định ẩn được bao lâu — tạo tradeoff rõ ràng với việc dùng skill combat.

### Lộ diện khi bị tấn công
- Đang tàng hình bị tấn công → **lộ diện X giây** (config).
- Trong thời gian lộ diện: tất cả thực thể đều nhìn thấy, tương tác, tấn công được.
- Hết X giây: nếu còn đủ mana / vẫn duy trì được skill → tự tàng hình lại.
- Bị tấn công tiếp khi đang lộ diện → **reset timer**.

**Ví dụ:**
- C (Thần Thức 1000) thấy cả A và B. C tấn công B → B lộ diện → A (vốn không thấy B) giờ thấy và có thể đánh. Sau X giây B lại ẩn trong mắt A nếu còn Thần Thức.

### Phá tàng hình
- **Không có skill/item phá tàng hình trực tiếp**.
- Cách duy nhất: dùng skill nâng Thần Thức nhất thời để vượt ngưỡng X% của đối phương → nhìn thấy.
- Counter hợp lý: đầu tư Thần Thức cao → ẩn lâu, ẩn sâu; đối thủ muốn counter phải đầu tư skill/item nâng Thần Thức.

### Áp dụng cho Trận Pháp
- Trận Nhãn có chỉ số Thần Thức bảo vệ.
- Thực thể có Thần Thức < X% Thần Thức Trận Nhãn → không nhìn thấy, không tấn công được.
- Dùng chung rule X% toàn server.

### Áp dụng cho Phù Lục / Ẩn Trận
- Linh Ẩn Phù và Ẩn Trận cung cấp buff Thần Thức tạm thời hoặc nâng ngưỡng ẩn.
- Dùng chung rule X%.

### Áp dụng cho Động Phủ
- Cấp bản vẽ động phủ quyết định Thần Thức Quan của động phủ.
- Người không vượt ngưỡng này: không nhìn thấy, không tương tác, không tấn công được.

## System States
- **Hiện diện bình thường**: tất cả thực thể đủ ngưỡng đều nhìn thấy.
- **Đang tàng hình**: tiêu hao mana liên tục, ẩn với thực thể dưới ngưỡng.
- **Lộ diện cưỡng bức**: bị tấn công, mọi thực thể đều thấy, đếm ngược X giây.
- **Tự động lộ diện**: hết mana khi đang tàng hình.

## Edge Cases
- Bị tấn công liên tục khi đang lộ diện: timer lộ diện reset liên tục, không thể ẩn lại.
- Kẻ thứ 3 reveal mục tiêu bằng cách tấn công vào: tạo gameplay phối hợp hoặc phá bẫy.
- Thần Thức quái fixed theo template: map cao → quái Thần Thức cao → player ẩn không hiệu quả nếu chênh lệch lớn.

## Data / Config Needs
- Ngưỡng X% toàn server (`game_configs`)
- Tốc độ tiêu hao mana khi tàng hình (`game_configs`)
- Thời gian lộ diện cưỡng bức X giây (`game_configs`)
- Thần Thức template của quái/boss theo từng map (DB)

## UI / UX Notes
- Thần Thức hiển thị như thanh stat thứ 3 (sau HP/Mana).
- Trạng thái tàng hình: cần hiệu ứng rõ ràng cho chính người dùng (ví dụ nhân vật mờ).
- Trạng thái lộ diện cưỡng bức: timer đếm ngược hiển thị.

## Related Systems
- **Động Phủ**: Thần Thức Quan quyết định phát hiện động phủ — xem `features/home-cave-defense.md`
- **Trận Pháp / Phù Lục**: Trận Nhãn và Ẩn Trận dùng chung rule X% — xem `features/crafting-talisman-formation.md`
- **Linh Thú**: pet có Thần Thức, ảnh hưởng bởi rule tàng hình — xem `features/spirit-beast.md`

## Key Decisions
1. Thần Thức là stat của **mọi thực thể**, không chỉ player.
2. Rule nhìn thấy dựa trên ngưỡng X% — config toàn server, không hard-code.
3. Tàng hình phải chủ động bật, không tự động.
4. Tàng hình là skill, tốn **mana** để duy trì — không tốn Thần Thức.
5. Không có skill/item phá tàng hình trực tiếp — chỉ có cách nâng Thần Thức của bản thân.
6. Bị tấn công → lộ diện cưỡng bức X giây, timer reset nếu bị đánh tiếp.
7. Thần Thức quái/boss fixed theo template DB, không scale map.

## Open Questions
- [ ] Ngưỡng X% cụ thể sẽ xác định khi làm balance.
- [ ] Tốc độ tiêu hao và hồi Thần Thức cụ thể — phase balance.
- [ ] Thời gian lộ diện cưỡng bức X giây — phase balance.
- [x] Người không đủ Thần Thức Quan nhìn động phủ: thấy **hiệu ứng mờ** như có thứ gì ở đó, nhưng không tương tác được.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
