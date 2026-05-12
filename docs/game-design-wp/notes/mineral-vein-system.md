---
doc_type: game_design_note
system_id: mineral-vein-system
status: draft
maturity: note
owner: gamedesign
created_at: 2026-05-12
updated_at: 2026-05-12
promoted_from: null
related_docs:
  - features/home-cave-defense.md
  - features/spirit-beast.md
  - features/crafting-talisman-formation.md
requires_code_verification: false
---

# Hệ Thống Mỏ Linh Thạch — Design Note

## Purpose

Thiết kế hệ thống mỏ linh thạch tranh đoạt — nguồn tài nguyên quan trọng trên thế giới, ai cũng có thể chiếm nhưng phải bảo vệ. Tạo động lực PvP tài nguyên, liên kết cá nhân và tông môn, và kiểm soát nguồn cung linh thạch trong game.

## Core Fantasy / Player Value

- Tranh đoạt tài nguyên thật sự — mỏ có giá trị, có người muốn cướp.
- Cảm giác làm chủ lãnh thổ tạm thời.
- Liên kết tông môn thông qua chiến dịch công / thủ mỏ tập thể.
- Kinh tế linh thạch có nguồn gốc rõ ràng, có thể kiểm soát.

## Key Terms

- `map`: template địa hình — không phải instance thật sự.
- `zone`: instance thật sự của player. 1 map có ~30–40 zone. Địa chỉ thật của player là `zone of map`.
- `mineral vein` / `mỏ`: nguồn tài nguyên linh thạch xuất hiện trong zone.
- `vein gate` / `cổng mỏ`: instance riêng liên kết với mỏ, là lớp bảo vệ giữa thế giới ngoài và khu vực khai thác bên trong.
- `vein interior` / `bên trong mỏ`: khu vực safe, chỉ chủ mỏ và danh sách được phép mới vào được, là nơi khai thác thực sự.
- `bản vẽ khai thác`: item tiêu hao 1 lần, dùng để chiếm mỏ vô chủ.
- `bùa phá mỏ`: item tiêu hao, dùng để phát động tấn công mỏ — có thể chọn danh nghĩa cá nhân hoặc tông môn.
- `mining alliance` / `liên minh khai thác`: chủ mỏ + danh sách người được mời khai thác.

## Draft Rules

### Spawn mỏ

- Mỏ chỉ xuất hiện trên **các map được chỉ định trước** (không phải toàn bộ map trong game).
- Trong các map đó, mỏ **random vào zone** cụ thể trong map.
- Trong zone đó, **vị trí xuất hiện cũng random**.
- Số lượng mỏ trên 1 map: **random, tối đa 3 mỏ** cùng lúc.
- Player phải **tự đi tìm** mỏ — không có thông báo toàn server khi mỏ spawn.
- Mỏ có **trữ lượng giới hạn** — khai thác hết thì biến mất.
- Mỏ cạn → admin dùng server tool để sinh mỏ mới dựa theo tốc độ khai thác thực tế.

### Chiếm mỏ

- Mỏ vô chủ: ai có **bản vẽ khai thác** thì dùng để chiếm.
- Bản vẽ khai thác là **item tiêu hao 1 lần** — dùng xong mất.
- Sau khi dùng bản vẽ: mỏ có chủ, **cổng mỏ** được tạo ra.

**Xác định chủ mỏ:**
- Nếu người dùng bản vẽ **không thuộc tông môn** hoặc **chọn danh nghĩa cá nhân**: chủ mỏ là cá nhân đó.
- Nếu người dùng bản vẽ **thuộc tông môn** và **chọn danh nghĩa tông môn**: chủ mỏ là tông môn, tông môn chủ là người quản lý.

### Bảo vệ mỏ

- Cơ chế bảo vệ tương tự động phủ nhưng ở quy mô rộng hơn.
- **Cổng mỏ** là instance riêng liên kết với mỏ — lớp bảo vệ giữa thế giới ngoài và khu vực khai thác.
- Chủ mỏ và liên minh khai thác có thể đặt trong cổng mỏ:
  - Tối đa **1 trận pháp che mắt**
  - Tối đa **1 trận pháp phòng ngự**
  - Tối đa **1 trận pháp tấn công**
  - Mỗi thành viên liên minh được đặt **1 linh thú thủ mỏ**
- Chủ mỏ (hoặc tông môn chủ nếu mỏ thuộc tông môn) quản lý danh sách liên minh khai thác.

### Liên minh khai thác

- Số người khai thác cùng lúc tối đa: **10 người** (config trong DB).
- Chủ mỏ có quyền **mời** và **kick** thành viên.
- Nếu chủ mỏ offline: mỏ vẫn hoạt động bình thường.
  - Nếu chủ đang trong trạng thái khai thác khi offline: vẫn khai thác bình thường.
  - Danh sách liên minh vẫn được áp dụng.

### Công phá mỏ

- Người muốn công phá cần dùng **bùa phá mỏ** (item tiêu hao) để phát động tấn công.
- Khi dùng bùa, chọn danh nghĩa:
  - **Cá nhân**: chỉ mình người đó tấn công.
  - **Tông môn** (nếu đã thuộc tông môn): toàn bộ tông môn có quyền tham gia chiến dịch công mỏ.
- Cơ chế công phá cổng mỏ **tương tự công phá động phủ** — tấn công cổng mỏ đến khi vỡ.
- Cổng mỏ có **HP**, khi HP về 0 → mỏ trở thành vô chủ.

**Khi cổng mỏ vỡ:**
- Người / tông môn thực hiện **last hit cổng mỏ** được **ưu tiên 1 phút** dùng bản vẽ khai thác để chiếm.
- Sau 1 phút → mỏ free, ai tới trước dùng bản vẽ trước thì chiếm được.
- Tất cả người đang bên trong mỏ (chủ mỏ + liên minh) bị **tele ra map ngẫu nhiên xung quanh** — tương tự cơ chế mất động phủ.
- **Trận pháp trong cổng mỏ bị phá hủy** hoàn toàn, dù còn duration hay không.
- **Linh thú thủ mỏ**: nếu còn sống → tự về túi nghỉ; nếu đã chết → về túi nghỉ và bị phạt (theo rule linh thú).

### Khai thác

- Khai thác là **passive** — player đứng bên trong mỏ, linh thạch tự ra.
- Phải ở **bên trong mỏ** (vein interior) để khai thác.
- Khi khai thác: **bị khóa toàn bộ thao tác khác**, không thể di chuyển.
- Thoát khỏi mỏ → dừng khai thác ngay.
- Linh thạch vào **thẳng balo**.
- Không giới hạn lượng khai thác per phiên — cứ đứng đó là ra, miễn mỏ còn trữ lượng.
- Tốc độ khai thác **khác nhau** giữa player — dựa theo chỉ số **MP** của player.
- Bên trong mỏ là **safe zone** — không thể bị tấn công khi đang khai thác.
- Chỉ chủ mỏ / danh sách liên minh mới vào được bên trong mỏ.

### Kết nối với Tông Môn

- Tông môn có thể chiếm mỏ dưới danh nghĩa tông môn.
- Tông môn có thể phát động chiến dịch công mỏ tập thể.
- Chủ tông môn là người quản lý mỏ khi mỏ thuộc tông môn.
- Chi tiết tông môn system → xem backlog tông môn.

## System States

| State | Mô tả |
|---|---|
| `unclaimed` | Mỏ vô chủ, ai cũng có thể dùng bản vẽ chiếm |
| `owned` | Có chủ (cá nhân hoặc tông môn), cổng mỏ đang hoạt động |
| `priority_window` | 1 phút sau khi cổng mỏ vỡ — phe công phá được ưu tiên chiếm |
| `depleted` | Mỏ cạn trữ lượng, biến mất |

## Main Flows

### Flow 1 — Chiếm mỏ vô chủ
1. Player phát hiện mỏ vô chủ trong zone.
2. Dùng **bản vẽ khai thác** (tiêu hao).
3. Chọn danh nghĩa: cá nhân hoặc tông môn.
4. Mỏ có chủ, cổng mỏ được tạo.
5. Chủ mỏ mời thành viên liên minh, đặt trận pháp, linh thú bảo vệ.

### Flow 2 — Khai thác
1. Chủ mỏ / thành viên liên minh vào bên trong mỏ qua cổng mỏ.
2. Bắt đầu khai thác — bị khóa thao tác, không di chuyển.
3. Linh thạch tự ra vào balo theo tốc độ MP.
4. Thoát mỏ → dừng khai thác.

### Flow 3 — Công phá mỏ
1. Attacker dùng **bùa phá mỏ**, chọn danh nghĩa cá nhân / tông môn.
2. Chiến dịch công mỏ bắt đầu — tấn công cổng mỏ.
3. Defender dùng trận pháp, linh thú, và chiến đấu trực tiếp để bảo vệ.
4. Cổng mỏ HP về 0 → vỡ.
5. Người / tông môn last hit được **ưu tiên 1 phút** dùng bản vẽ chiếm.
6. Sau 1 phút → free, ai tới trước chiếm trước.

### Flow 4 — Mỏ cạn
1. Trữ lượng về 0 → mỏ biến mất.
2. Cổng mỏ đóng, tất cả người bên trong tele ra.
3. Admin theo dõi tốc độ khai thác → dùng server tool sinh mỏ mới.

## Edge Cases

- Player đang khai thác khi mỏ cạn: bị tele ra, linh thạch đã khai thác giữ nguyên trong balo.
- Chủ mỏ offline khi bị công phá: mỏ vẫn bị công phá bình thường, không cần chủ online để bảo vệ.
- Race condition khi nhiều người cùng dùng bản vẽ sau priority window: server xử lý theo thứ tự request.
- Liên minh đầy (10 người) khi chủ muốn mời thêm: phải kick người cũ trước.
- Tông môn công mỏ nhưng không ai dùng bản vẽ trong 1 phút ưu tiên: mỏ về trạng thái free bình thường.

## Data / Config Needs

- Map whitelist cho phép spawn mỏ → DB map config
- Số mỏ tối đa per map (tạm thời 3) → `game_configs`
- Trữ lượng per mỏ → config per spawn (server tool)
- Tốc độ khai thác base + công thức theo MP → DB balance config
- Số thành viên liên minh tối đa (tạm thời 10) → `game_configs`
- Thời gian priority window sau khi cổng mỏ vỡ (1 phút) → `game_configs`
- HP cổng mỏ → DB balance config
- Giới hạn trận pháp trong cổng mỏ (1 che mắt, 1 phòng ngự, 1 tấn công) → `game_configs`
- Server tool sinh mỏ mới → ops tooling

## Related Systems

- **Động Phủ**: cổng mỏ và cơ chế bảo vệ kế thừa từ động phủ — xem `features/home-cave-defense.md`
- **Linh Thú**: linh thú thủ mỏ theo rule linh thú thủ động phủ — xem `features/spirit-beast.md`
- **Trận Pháp**: trận pháp bảo vệ cổng mỏ — xem `features/crafting-talisman-formation.md`
- **Tông Môn**: mỏ có thể thuộc tông môn, tông môn công / thủ mỏ tập thể — xem backlog

## Design Decisions

### Locked
- Mỏ chỉ spawn trên map được chỉ định, random vào zone, random vị trí trong zone.
- Tối đa 3 mỏ per map.
- Player tự tìm mỏ, không có thông báo toàn server.
- Mỏ có trữ lượng giới hạn, cạn thì biến mất, admin sinh mỏ mới bằng tool.
- Bản vẽ khai thác tiêu hao 1 lần.
- Chủ mỏ là cá nhân hoặc tông môn tùy chọn khi chiếm.
- Cổng mỏ là instance riêng liên kết với mỏ.
- Bên trong mỏ là safe zone, chỉ liên minh mới vào được.
- Khai thác passive, bị khóa thao tác, tốc độ theo MP.
- Linh thạch vào thẳng balo, không giới hạn per phiên.
- Last hit cổng mỏ được ưu tiên 1 phút để chiếm.
- Cổng mỏ vỡ: người bên trong bị tele ra, trận pháp mất, linh thú về túi.

### Tentative
- Tốc độ khai thác theo MP — cần confirm công thức cụ thể khi làm balance.
- Số thành viên liên minh tối đa (tạm 10) — balance.
- HP cổng mỏ — balance.

## Open Questions

- [ ] Chủ mỏ tông môn offline — tông môn chủ quản lý danh sách, hay có role phụ trong tông môn? → chờ tông môn system
- [ ] Mỏ thuộc tông môn: chủ tông môn quản lý danh sách khai thác, số lượng khai thác hàng ngày, thống kê hôm nay / hôm qua / 7 ngày gần đây → chờ tông môn system
- [ ] Linh thạch khai thác được có chia tự động cho tông môn không, hay 100% vào balo cá nhân? → chờ tông môn system
- [ ] Bùa phá mỏ drop / mua ở đâu?
- [ ] Bản vẽ khai thác mỏ mua ở NPC nào, giá bao nhiêu — data design.

## Risks / Watchouts

- Tốc độ khai thác theo MP tạo lợi thế lớn cho player cảnh giới cao — cần cân nhắc khi balance.
- Nếu admin chậm sinh mỏ mới, linh thạch khan hiếm có thể làm tắc nghẽn toàn bộ economy.
- Race condition khi nhiều người cùng chiếm sau priority window cần server enforce chặt.
- Mỏ tông môn + chiến dịch tập thể cần làm rõ thêm khi có tông môn system.

## Promotion Checklist

- [ ] Core gameplay goal is clear.
- [ ] Player-facing loop is understandable.
- [ ] Key terms are defined.
- [ ] Major alternatives are resolved or listed as open questions.
- [ ] Ready to promote to `features/`.
