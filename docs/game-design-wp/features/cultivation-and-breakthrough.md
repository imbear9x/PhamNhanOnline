---
doc_type: game_design_feature
system_id: cultivation-and-breakthrough
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-13
updated_at: 2026-05-13
promoted_from: notes/cultivation-and-breakthrough.md
requires_code_verification: true
related_docs:
  - features/death-penalty.md
  - features/tribulation-system.md
  - docs/progression/cultivation-breakthrough-and-potential-runtime.md
  - docs/implementation/extractions/cultivation-runtime-extraction.md
---

# Tu Luyện & Đột Phá Cảnh Giới — Feature Draft

## Purpose

Ghi lại thiết kế hệ tu luyện và đột phá cảnh giới của PhamNhanOnline. Phần này đã được triển khai một phần trong code — note này dùng code/DB đã có làm nền tảng, bổ sung intent thiết kế và các phần còn cần chốt.

## Cơ sở từ code / DB đã có

| Thành phần | Trạng thái trong code |
|---|---|
| Tu luyện passive (tích lũy cultivation) | ✅ Hoạt động — chỉ ở private home instance, cần active martial art |
| Đột phá cảnh giới | ✅ Hoạt động — roll chance, có failure penalty, record breakthrough_attempts |
| Thọ nguyên per cảnh giới (`lifespan` trong `realm_templates`) | ✅ Có trong DB — runtime enforce `is_expired` / `lifespan-restricted` state |
| Potential reward khi lên cảnh giới | ✅ Hoạt động — `unallocated_potential` tăng, `potential_reward_locked` clear khi success |
| `breakthrough_conditions` table | ⚠️ Tồn tại trong DB nhưng runtime chưa đọc — chưa dùng |
| Lôi Kiếp / Tribulation | ❌ Chưa triển khai — defer, xem `features/tribulation-system.md` |

## Thuật ngữ

- `cultivation`: điểm tu luyện tích lũy trong cảnh giới hiện tại.
- `cultivation cap`: ngưỡng đầy trong cảnh giới — đạt cap mới được đột phá.
- `realm` / `cảnh giới`: tầng tu luyện. Mỗi cảnh giới có pool cultivation riêng.
- `potential`: điểm dùng để nâng base stat, nhận được khi lên cảnh giới hoặc đánh quái.
- `thọ nguyên` (`lifespan`): thời gian tồn tại của nhân vật trong cảnh giới, bị trừ khi chết. Hết thọ nguyên = nhân vật chết vĩnh viễn.
- `breakthrough`: hành động đột phá cảnh giới khi cultivation đã đầy.
- `absorption_multiplier`: hệ số tốc độ tích lũy cultivation, phụ thuộc vào công pháp active.
- `breakthrough_base_rate`: tỉ lệ đột phá thành công cơ bản của cảnh giới.
- `is_bottleneck`: stage công pháp đặc biệt — khó đột phá hơn.

## Cơ chế Tu Luyện

### Tích lũy cultivation
- Player chọn **công pháp** (martial art) và set làm active.
- Cultivation chỉ tích lũy khi player đã **ở private home instance và kích hoạt cultivation**.
- **Không yêu cầu online**: sau khi kích hoạt, thời gian tích lũy vẫn tính khi player offline. Server settle khi đăng nhập lại — xem Offline Time-Based Activities trong `shared-rules.md`.
- Khi đăng nhập lại: player vẫn ở trạng thái cultivating, nhận kết quả settle ngay.
- Tốc độ tích lũy phụ thuộc `absorption_multiplier` của công pháp active.
- Cultivation tích lũy đến `max_cultivation` của cảnh giới thì đầy — dừng tích lũy thêm.
- Khi đánh quái: nhận thêm cultivation và potential ngoài combat.

### Đột phá cảnh giới
- Điều kiện bắt buộc: cultivation đã đạt `max_cultivation` của cảnh giới hiện tại.
- Đột phá là **roll chance** — không guaranteed thành công:
  - `base_breakthrough_rate` từ `realm_templates`.
  - Có thể có bonus từ `breakthrough_conditions` (bảng tồn tại nhưng runtime chưa dùng — cần verify).
- **Thành công**: lên cảnh giới mới, nhận potential reward, `potential_reward_locked` được clear.
- **Thất bại**: áp dụng failure penalty (chi tiết xem dưới), cultivation giảm theo `breakthrough_exp_penalty`.

### Failure penalty khi đột phá thất bại
- Khi thất bại: cultivation bị trừ theo **% của cultivation hiện tại** (tỉ lệ từ `breakthrough_exp_penalty` per martial art stage).
- Nếu sau khi trừ cultivation tụt dưới ngưỡng cảnh giới hiện tại → **tụt cảnh giới** và áp dụng **Cultivation Penalty Rule** (xem `shared-rules.md`).
- **Không có bình cảnh**: sau khi tụt cảnh giới vì đột phá thất bại, player có thể đột phá lại ngay khi đủ cultivation.
- Không giới hạn số lần thử — player có thể thử lại liên tục miễn đủ cultivation.
- Lưu lịch sử mỗi lần thử vào `breakthrough_attempts`.

`requires_code_verification: true` — cần verify thêm:
- `breakthrough_exp_penalty` được apply như thế nào (% hay flat)?
- `breakthrough_conditions` có được runtime đọc chưa?
- Có cap số lần thử per ngày / per giờ không?

## Thọ Nguyên

- Mỗi cảnh giới có `lifespan` riêng trong `realm_templates`.
- Mỗi lần player chết: thọ nguyên bị trừ (xem `features/death-penalty.md` cho rule chi tiết).
- Khi đột phá lên cảnh giới mới: pool thọ nguyên **cộng thêm** phần chênh lệch — không reset về 0.
- `is_expired = true`: nhân vật bị khóa một số hành động, cultivation bị block.
- Hết thọ nguyên = nhân vật chết vĩnh viễn — phải tạo nhân vật mới.

## Cảnh Giới (Realm Tiers)

Dựa trên DB, game có ít nhất các cảnh giới theo thứ tự. Tên cụ thể chưa được confirm từ code — cần bổ sung khi bàn:

Tổng cộng **31 cảnh giới**, chia 9 đại giai đoạn. Nguồn: `docs/game-design-wp/realm-list.md`.

| Đại giai đoạn | Số tầng | Realm # | Thọ nguyên | Lôi Kiếp |
|---|---|---|---|---|
| Luyện Khí Kỳ | 9 | 1–9 | 120–160 tuổi | ❌ |
| Trúc Cơ Kỳ | 3 | 10–12 | 180–220 tuổi | ❌ |
| Kết Đan Kỳ | 3 | 13–15 | 350–500 tuổi | ❌ |
| Nguyên Anh Kỳ | 3 | 16–18 | 1.200–2.000 tuổi | ❌ |
| **Hóa Thần Kỳ** | 3 | 19–21 | **Vô hạn** | ✅ Bắt đầu từ đây |
| Luyện Hư Kỳ | 3 | 22–24 | Vô hạn | ✅ |
| Hợp Thể Kỳ | 3 | 25–27 | Vô hạn | ✅ |
| Độ Kiếp Kỳ | 1 | 28 | Vô hạn | ✅ |
| Chân Tiên Kỳ | 3 | 29–31 | Vô hạn | ✅ |

**Ngưỡng chuyển thọ nguyên → Lôi Kiếp:** từ **Hóa Thần Sơ Kỳ (realm 19)** trở lên.

## Potential

- Nhận potential từ: đánh quái, lên cảnh giới.
- Phân phối potential vào base stats (`base_hp`, `base_mp`, `base_attack`, `base_sense`, `base_luck`, `base_stamina`...) qua `potential_stat_upgrade_tiers`.
- Tier cost tăng dần — nâng nhiều lần cùng 1 stat thì mỗi lần tốn potential hơn.
- **Khi tụt cảnh giới**: potential bị revert tương ứng, chỉ số bị giảm để trả lại potential — xem Cultivation Penalty Rule trong `shared-rules.md`.

> `potential_reward_locked` trong code là thiết kế cũ — **không còn là cơ chế canonical**. Cần TechDesign refactor sang potential revert rule. `requires_code_verification: true`

## Công Pháp (Martial Art) — Quan Hệ Với Tu Luyện

- Công pháp (martial art) và cảnh giới nhân vật là **2 hệ song song, độc lập**.
- Exp công pháp tăng khi player **dùng công pháp đó để tu luyện** và hấp thụ tu vi / tiềm năng.
- Stage công pháp (`martial_art_stages`) có `breakthrough_base_rate` và `is_bottleneck` riêng — đây là đột phá **stage công pháp**, không phải đột phá cảnh giới nhân vật.
- **Không thể đổi công pháp active khi đang tu luyện** — tu luyện khóa toàn bộ thao tác. Phải dừng tu luyện trước khi đổi công pháp. Server validate điều này.

## Open Questions

- [x] Tên và cấu trúc cảnh giới — xem bảng trên, nguồn `realm-list.md`.
- [x] `breakthrough_conditions` — schema/entity dự phòng trong DB, **chưa implement trong runtime**. Intent: mỗi realm có thể có điều kiện phụ (cần item / đan dược) để tăng success_bonus khi đột phá. Chưa phải mechanic đang chạy — không đưa vào requirement/handoff cho đến khi được mở rộng.
- [x] `breakthrough_exp_penalty`: % của cultivation hiện tại.
- [x] Không giới hạn số lần thử — miễn đủ cultivation.
- [x] Cultivation offline: không cần online — xem Offline Time-Based Activities trong `shared-rules.md`.
- [x] Lôi Kiếp: áp dụng từ Hóa Thần Sơ Kỳ (realm 19) trở lên — xem `features/tribulation-system.md`.
- [x] Practice session (generic) và cultivation là 2 hệ độc lập hoàn toàn. Practice session là khung generic cho hoạt động cần timer server (alchemy là use case thật hiện tại). Cultivation dùng service riêng, state riêng, chỉ chặn lẫn nhau với practice session chứ không dùng chung backend.

## Trạng Thái Tài Liệu

> **Lưu ý:** Hệ tu luyện và đột phá cảnh giới **đã được implement trong code**. Doc này là grounding note cho game design — **không tạo requirement hay handoff** cho phần core cultivation/breakthrough.
>
> Phần **duy nhất cần TechDesign action**: refactor `potential_reward_locked` sang Cultivation Penalty Rule (potential revert). Xem `shared-rules.md` — Cultivation Penalty.

## Related Systems

- **Death Penalty / Thọ Nguyên**: rule trừ thọ nguyên khi chết → `features/death-penalty.md`
- **Lôi Kiếp**: sự kiện cảnh giới cao — xem `features/tribulation-system.md`
- **Công Pháp (Martial Art)**: quyết định absorption rate và breakthrough rate per stage
- **Potential**: phân phối vào base stat sau khi lên cảnh giới


## Requirement Readiness Note

- [x] Core cultivation / breakthrough runtime đã implement; doc này đóng vai trò grounding feature, không phải ứng viên promote requirement riêng cho core loop.
- [x] TechDesign review: penalty refactor có thể là thay thế cục bộ (isolated) nếu `potential_reward_locked` hiện không bị state persistence / reconnect / retry / anti-abuse flows phụ thuộc.
- [x] Nếu code hiện tại có các flow persistence/state machine phụ thuộc locked-potential flags, thì refactor này cần scope rộng hơn và phải được TechDesign tách rõ riêng.
- [x] Vì vậy doc này được xem là đủ chín ở layer feature/grounding, nhưng không đi theo workflow promote requirements bình thường.
