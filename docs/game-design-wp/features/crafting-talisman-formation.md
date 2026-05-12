---
doc_type: game_design_feature
system_id: crafting-talisman-formation
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-08
updated_at: 2026-05-12
promoted_from: notes/crafting-talisman-formation.md
related_docs:
  - features/spirit-sense.md
  - features/home-cave-defense.md
  - features/multi-stage-crafting.md
requires_code_verification: false
---

# Luyện Chế Phù Lục & Trận Pháp — Feature Draft

## Goal

Tạo hai hệ luyện chế cho **Phù Lục** (bùa chú dùng 1 lần trên người) và **Trận Pháp** (AOE entity đặt trên map) theo mô hình kế thừa từ foundation đan dược hiện tại: material quality ảnh hưởng cả tỉ lệ thành công lẫn hiệu quả output.

## Design Summary

Cả Phù Lục và Trận Pháp đều dùng chung pattern từ hệ alchemy (`pill_recipe_inputs`): mỗi input có `success_rate_bonus`, `mutation_bonus_rate`, `is_optional`, và thêm chiều **`effect_quality_bonus`** ảnh hưởng trực tiếp lên stats của output. Hai hệ khác nhau ở hình thức sử dụng: Phù Lục là item dùng 1 lần lên bản thân, Trận Pháp là AOE entity có HP và thời gian hiệu lực trên map.

## Scope

### In Scope
- Phù Lục: craft, sử dụng, stack/không stack, material quality
- Trận Pháp: craft, đặt trên map, Trận Nhãn, target rule, các loại hiệu ứng
- Material Quality System (áp dụng cho cả hai)
- Giới hạn đặt trận trên map

### Out Of Scope
- Mastery theo nghề/người chơi
- Balance cụ thể (tỉ lệ thành công, duration, radius, damage cụ thể)
- UI/UX chi tiết màn hình craft
- Chi tiết recipe data cho từng loại phù / trận

## Core Loop

### Phù Lục
1. Thu thập nguyên liệu phù hợp công thức.
2. Luyện chế → thành công (tạo phù) hoặc thất bại (mất vật liệu).
3. Sử dụng phù → hiệu ứng lên bản thân trong thời gian duration.

### Trận Pháp
1. Thu thập nguyên liệu (bao gồm linh thạch bắt buộc).
2. Luyện chế → thành công hoặc thất bại.
3. Đặt trận tại vị trí hợp lệ trên map → trận pháp xuất hiện, có thời gian hiệu lực.
4. Trận tồn tại cho đến khi hết duration hoặc Trận Nhãn bị phá.

## Player-Facing Rules

### Material Quality System (chung cho cả hai)
- **Chiều 1 — Tỉ lệ thành công**: base rate + sum(`success_rate_bonus` từng vật liệu).
- **Chiều 2 — Hiệu quả**: sum(`effect_quality_bonus`) nhân vào tất cả stats của output.
- Cả hai chiều đều độc lập.

### Thành công / Thất bại (chung)
- Cả hai đều **mất vật liệu**.
- Cancel giữa chừng: refund theo threshold tiến độ (giống đan dược).

---

### Phù Lục

**Bản chất:**
- Item dùng 1 lần, hiệu ứng chỉ lên **người sử dụng**.
- Mỗi phù có bộ stats riêng: duration + các chỉ số hiệu ứng.

**Không stack — rule khi dùng đè:**
- Dùng đè cùng loại → refresh duration + lấy giá trị **tốt nhất từng stat**.
- Ví dụ: Phù A (ngưỡng 100, còn 2 phút) + Phù B (ngưỡng 120, 5 phút) → ngưỡng 120, duration 5 phút.
- Nếu Phù B ngưỡng 80: giữ ngưỡng 100, duration refresh 5 phút.

**Mastery:** Không có — chỉ vật liệu quyết định tỉ lệ.

---

### Trận Pháp

**Bản chất:**
- Item dùng 1 lần — đặt xuống là mất.
- Tạo **vùng hiệu ứng (AOE entity)** cố định tại vị trí đặt.
- Có thời gian hiệu lực — hết thì tan.
- Không di chuyển được sau khi đặt.
- Boss/enemy dùng chung system này, không có loại riêng.

**Trận Nhãn:**
- Mỗi trận có **Trận Nhãn** — điểm sống của trận.
- Trận Nhãn có **HP** (base + tăng theo vật liệu) và **chỉ số bảo vệ Thần Thức**.
- Thực thể Thần Thức **cao hơn** ngưỡng bảo vệ → nhìn thấy, trỏ được, tấn công được.
- Thực thể Thần Thức **thấp hơn** → không nhìn thấy, không tương tác được.
- Trận Nhãn **không tự hồi HP**.
- Trận Nhãn hết HP → trận tan. Hết duration → trận tan. Không drop gì khi tan.

**Stats của Trận Pháp:**

| Stat | Mô tả |
|---|---|
| Duration | Thời gian hiệu lực |
| Radius | Bán kính vùng hiệu ứng |
| Cường độ hiệu ứng | Damage / buff / ngưỡng Thần Thức che giấu |
| Trận Nhãn HP | Độ bền trước khi bị phá — base + bonus từ vật liệu |
| Trận Nhãn Thần Thức bảo vệ | Ngưỡng để nhìn thấy / phá trận |

Tất cả stats bị ảnh hưởng bởi `effect_quality_bonus` từ vật liệu.

**Các loại hiệu ứng trận:**
- **Damage**: gây sát thương liên tục cho thực thể trong vùng.
- **Buff**: tăng stat cho thực thể trong vùng.
- **Ẩn**: che giấu thực thể/vật thể trong vùng khỏi thực thể Thần Thức thấp hơn ngưỡng.
- **Debuff/kiểm soát**: suy yếu quái, ngăn quái chủ động tấn công.
- Một trận có thể kết hợp nhiều loại hiệu ứng.

**Target Rule (config theo từng công thức):**
- Chỉ tấn công kẻ địch trong vùng.
- Chỉ buff đồng đội trong vùng.
- Chỉ tác động lên loại thực thể cụ thể (player / enemy / boss / công trình như động phủ).
- Có thể kết hợp nhiều rule cùng lúc.

**Nguyên liệu đặc biệt:**
- **Linh thạch là nguyên liệu bắt buộc** trong mọi công thức trận pháp — đắt, khó spam.

**Giới hạn đặt trận:**
- Không giới hạn số trận player sở hữu.
- Giới hạn theo **ô trên map**: mỗi ô chỉ chứa 1 trận pháp.
- Ô đã có trận → không đặt thêm.
- Số ô config trong DB theo từng map/zone.

**Không stack — rule khi đặt đè:**
- 2 trận cùng loại cùng vùng → lấy giá trị tốt nhất từng stat, duration refresh.

## System States

### Phù Lục
- **Chưa dùng**: item trong túi.
- **Đang active**: hiệu ứng đang chạy trên player, đếm ngược duration.
- **Hết hiệu lực**: tự xóa.

### Trận Pháp
- **Chưa đặt**: item trong túi.
- **Đang hoạt động**: AOE entity trên map, đếm ngược duration, Trận Nhãn còn HP.
- **Tan**: hết duration hoặc Trận Nhãn về 0 HP.

## Edge Cases
- Đặt trận tại ô đã có trận khác: không cho đặt, phải đặt ô khác hoặc đợi trận cũ tan.
- Dùng phù đè cùng loại khi stat phù mới thấp hơn: giữ stat cũ, chỉ refresh duration.
- Trận pháp của enemy/boss: dùng chung system, chỉ khác template target rule.
- Player bị lộ diện vì trong vùng Ẩn trận bị phá Trận Nhãn: tự lộ diện ngay khi trận tan.

## Data / Config Needs
- `success_rate_bonus` per input → DB recipe
- `effect_quality_bonus` per input → DB recipe
- `is_optional` per input → DB recipe
- Số ô tối đa trận pháp per map/zone → DB map config
- Linh thạch là nguyên liệu bắt buộc → flag trong recipe data
- Threshold cancel và refund → `game_configs`

## UI / UX Notes
- Craft UI: hiển thị 2 chiều quality riêng biệt (tỉ lệ thành công / hiệu quả output).
- Phù đang active: hiển thị trong buff bar với timer countdown.
- Trận pháp trên map: hiển thị vùng AOE rõ ràng, indicator Trận Nhãn HP.

## Related Systems
- **Thần Thức**: Trận Nhãn và Ẩn Trận dùng chung rule X% — xem `features/spirit-sense.md`
- **Động Phủ**: Trận pháp dùng để phòng thủ cửa động phủ — xem `features/home-cave-defense.md`
- **Crafting nhiều tầng**: Phù Lục và Trận Pháp chỉ cần khai báo **component đầu vào trực tiếp**; chuỗi tạo ra component đó được quản lý ở hệ craft nguồn — xem `features/multi-stage-crafting.md`

## Key Decisions
1. Phù Lục và Trận Pháp kế thừa pattern material quality từ hệ alchemy.
2. `effect_quality_bonus` là chiều thứ 2, nhân vào tất cả stats của output.
3. Không stack — dùng đè thì lấy giá trị tốt nhất từng stat + refresh duration.
4. Trận Nhãn là điểm sống của trận, không tự hồi.
5. Linh thạch bắt buộc trong mọi công thức trận pháp.
6. Giới hạn trận pháp theo ô trên map, không theo số lượng sở hữu.
7. Không có Mastery — chỉ vật liệu quyết định kết quả.

## Open Questions
- [ ] Balance cụ thể tỉ lệ thành công base theo từng loại phù/trận — phase data design.
- [ ] Danh sách loại phù lục và trận pháp cụ thể — phase data design.
- [ ] Giới hạn số ô trận pháp per map/zone cụ thể — phase data design.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [ ] Behavior is specific enough for `dev` to estimate.
- [ ] Acceptance criteria can be written without guessing.
- [ ] Major edge cases are covered.
- [ ] Config/data needs are listed.
- [ ] Out-of-scope items are explicit.
- [ ] Ready to promote to `requirements/`.
