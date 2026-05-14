---
doc_type: game_design_shared_rules
status: draft
owner: gamedesign
created_at: 2026-05-13
updated_at: 2026-05-13
requires_code_verification: true
---

# Shared Gameplay Rules

This file is the canonical source for gameplay rules that appear in more than one feature.

Feature docs should not redefine these rules independently. They should reference the shared rule and only describe feature-specific application details.

When a user decision changes a shared rule:

1. Update this file first.
2. Audit every related live primary doc in `notes/`, `features/`, and `requirements/`.
3. Propagate the canonical decision to affected docs in the same pass.
4. If the canonical decision is unclear, ask the user before treating the update as complete.
5. Record unresolved cross-doc issues in `consistency-audit.md`.

## Rule Index

| Shared rule | Current status | Related docs |
|---|---|---|
| Death taxonomy | Canonical | `features/death-penalty.md`, `features/home-cave-defense.md`, `features/sect-system.md`, `features/player-interaction-group.md` |
| Looting window | Canonical | `features/home-cave-defense.md`, `features/sect-system.md`, `features/death-penalty.md` |
| Inventory full / reward overflow | Canonical | `features/main-progression-quest-chain.md`, `features/sect-system.md`, `features/inbox-mail-system.md` |
| Spirit Sense model | Canonical | `features/spirit-sense.md`, `features/spirit-beast.md`, `features/machine-system.md`, `features/home-cave-defense.md`, `features/crafting-talisman-formation.md` |
| Companion slot model | Canonical | `features/spirit-sense.md`, `features/spirit-beast.md`, `features/machine-system.md` |
| Escrow | Canonical | `features/sect-system.md`, future trading/task docs |
| Blueprint / bản vẽ | Canonical | `features/home-cave-defense.md`, `features/sect-system.md` |
| Ownership / drop rights | Canonical | `features/death-penalty.md`, `features/spirit-beast.md`, `features/home-cave-defense.md`, `features/sect-system.md` |
| Structure loot drop rate | Canonical | `features/home-cave-defense.md`, `features/sect-system.md` |
| Pet auto-loot | Canonical | `features/spirit-beast.md`, `features/death-penalty.md` |
| PvP state taxonomy | Canonical | `features/death-penalty.md`, `features/home-cave-defense.md`, `features/sect-system.md`, `features/player-interaction-group.md` |
| Offline time-based activities | Canonical | `features/cultivation-and-breakthrough.md`, `features/mineral-vein-system.md`, future crafting/alchemy docs |
| Cultivation penalty (realm drop + potential revert) | Canonical | `features/cultivation-and-breakthrough.md`, `features/tribulation-system.md`, `features/death-penalty.md` |
| Structure deployment cast time / setup lock | Canonical | `features/home-cave-defense.md`, `features/sect-system.md`, `features/mineral-vein-system.md` |
| Interaction range | Canonical (impl) | `features/npc-system.md`, all interactable entities |
| Guaranteed hit | Canonical | `features/speed-system.md`, skill data |
| Tu vi / tiềm năng chia sẻ (proximity sharing) | Canonical | `features/party-system.md`, `features/sect-system.md` |
| Pháp khí penalty (cấp tut) | Canonical | `features/magic-weapon-system.md` |

## Canonical Rules

### Death Taxonomy

Status: canonical.

Canonical rule:

- **Bất kể chết vì lý do gì** đều áp dụng penalty nền giống nhau:
  - có tỉ lệ rớt linh thạch và item trên người
  - mất buff khi chết
  - trừ **thọ nguyên** (cảnh giới dưới Hoá Thần) hoặc rút ngắn **thời gian đến Lôi Kiếp tiếp theo** (cảnh giới trên Hoá Thần)
- Hết thọ nguyên hoặc bị đẩy đến mốc Lôi Kiếp mà không vượt qua được theo rule feature liên quan có thể dẫn đến mất nhân vật.
- Một số **nguyên nhân chết đặc biệt** có thể áp dụng **penalty bổ sung** do game design data cấu hình.

Canonical categories vẫn hữu ích cho feature docs khi mô tả ngữ cảnh, nhưng **không còn dùng để quyết định có/không có penalty thọ nguyên / lôi kiếp**:

- `pve_death`
- `duel_death`
- `lawful_pvp_death`
- `raid_death`
- `pk_death`
- `permadeath_trigger`

Shared implication:

- Duel, PvP Zone, cave raid, sect war, mineral conflict, và PK đều cùng chịu penalty nền giống nhau.
- Sự khác biệt giữa các ngữ cảnh chết nằm ở **penalty bổ sung** hoặc **multiplier đặc thù** nếu feature đó định nghĩa.

### Looting Window

Status: canonical.

Canonical rule:

- Looting window là giai đoạn ngắn sau khi một cấu trúc được phá hủy hoàn toàn.
- Chỉ người **đã ở trong map tại thời điểm window bắt đầu** mới được tham gia loot.
- Từ bên ngoài, cổng / entry point không còn nhìn thấy hoặc không còn vào được.
- PvP trong looting window là **free-for-all** trừ khi feature nói khác rõ ràng.
- Người trong map **không được tự rời đi** cho đến khi window kết thúc.
- Nếu người chơi **offline** trong looting window, toàn bộ item họ đã nhặt trong window đó rơi ra ngay lập tức trong map.
- Nếu người chơi **chết** trong looting window, penalty chết bình thường vẫn áp dụng; feature có thể thêm multiplier hoặc effect bổ sung.
- Khi window kết thúc, tất cả người còn trong map bị teleport ra; người sống giữ loot họ đang nắm giữ hợp lệ.

Feature-specific application:

- `features/home-cave-defense.md`: cave destruction dùng cave loot pool + cave blueprint recovery path.
- `features/sect-system.md`: sect gate destruction dùng sect treasury / sect blueprint / sect cave-area rules.

### Inventory Full / Reward Overflow

Status: canonical.

Canonical rule:

- Khi người chơi nhận reward nhưng inventory không đủ chỗ, reward **được chuyển vào inbox / hòm thư chờ nhận**.
- Rule này áp dụng thống nhất cho progression reward, sect welfare, quest reward, và các reward tương tự trừ khi một feature cực kỳ đặc biệt được user chốt khác rõ ràng sau này.
- Feature docs không nên tự định nghĩa timeout 5 phút hoặc mất reward khi inventory đầy trừ khi có quyết định canonical mới.

### Spirit Sense Model

Status: canonical.

Canonical rule:

- Thần Thức là **slot / bandwidth stat**, không phải resource tiêu hao theo thời gian.
- Thần Thức **không tự hồi**, không tụt về 0 do duy trì skill, và không có khái niệm “Thần Thức hiện tại” kiểu mana/stamina.
- Một phần Thần Thức được reserved cho hoạt động cơ bản của player; phần còn dư là slot cho companion hoặc các hệ cần reserve.
- Thần Thức cũng quyết định ngưỡng nhìn thấy / tương tác giữa các thực thể.
- **Tàng hình tiêu hao mana**, không tiêu hao Thần Thức.
- Nếu cần hiệu ứng tạm thời làm tăng/giảm hiệu lực Thần Thức, feature phải mô tả đó là **modifier** lên stat/threshold, không phải resource drain/recovery loop.

Shared implication:

- Các docs về Linh Thú, Khôi Lỗi, Trận Pháp, Động Phủ phải dùng Thần Thức như capacity model.
- Mọi language về “hồi Thần Thức”, “tiêu hao Thần Thức theo thời gian”, “Thần Thức tụt về 0 khi tàng hình” cần bị loại bỏ hoặc đổi thành mana / modifier language.

### Companion Slot Model

Status: canonical.

Canonical rule:

- Linh Thú and Khôi Lỗi both reserve fixed Thần Thức slots while active.
- Reserved basic player operation consumes a fixed part of player Thần Thức.
- Remaining free slots determine how many companions can be active at once.
- Individual companions still have their own operating model:
  - Linh Thú: mana / rest / cooldown.
  - Khôi Lỗi: energy converted from linh thạch.
- **Không dùng map density limit cho companion.** Linh Thú và Khôi Lỗi không bị chặn bởi một giới hạn density map riêng.

Shared implication:

- Quyết định có triệu hồi thêm được hay không chỉ phụ thuộc vào slot Thần Thức còn dư và resource riêng của companion đó.
- Feature docs không nên giữ rule “map đã đủ density nên không triệu hồi thêm pet/khôi lỗi”.

### Escrow

Status: canonical.

Canonical rule:

- Escrow là **tài sản bị khóa ngay khi tạo một cam kết có thưởng / thanh toán tương lai**.
- Tài sản đã escrow không thể rút, không thể tiêu lại, không thể dùng cho cam kết khác.
- Mỗi escrow gắn với:
  - **source**: nơi tài sản bị khóa (bảo khố, inventory người tạo...).
  - **destination rule**: ai nhận khi resolve thành công.
  - **return rule**: phần chưa dùng quay về source container hợp lệ khi cancel / dissolve.

Resolve behavior:

- **Complete / success** → chuyển phần tương ứng cho destination.
- **Partial fill** → chỉ phần đã resolve rời escrow; phần còn lại tiếp tục bị khóa.
- **Task hết hạn / trả về pool nhưng chưa hủy** → escrow **giữ nguyên** gắn với task.
- **Hủy task chủ động** → phần escrow chưa dùng **trả về source container**.
- **Close / dissolve source system (giải tán tông môn, đóng shop...)** → phần escrow chưa dùng **trả về source container**, rồi xử lý theo flow dissolve của feature đó.

Shared implication:

- Phúc lợi tuần, thưởng nhiệm vụ, buy order shop tông môn đều dùng cùng 1 logic escrow.
- Feature docs không nên dùng nhiều từ khác nhau ("khóa", "trừ trước", "giữ hộ", "treo thưởng") với semantics mơ hồ — dùng chung từ **escrow** và tuân theo resolve behavior trên.

Known applications:

- Sect weekly welfare: escrow từ bảo khố đầu tuần.
- Sect voluntary task reward: escrow từ bảo khố khi tạo task.
- Member-posted task reward: escrow từ inventory người tạo khi tạo task.
- Sect shop buy orders: escrow linh thạch từ bảo khố khi tạo đơn thu mua.

### Blueprint / Bản Vẽ

Status: canonical.

Canonical rule:

- Blueprint là item **không giao dịch được**, lưu bố cục và toàn bộ nội dung của một cấu trúc triển khai được.
- Mỗi loại cấu trúc: tối đa **1 bản vẽ / người** tại mọi thời điểm.
- Nếu blueprint bị mất / mất do edge case: player có thể **mua lại tại NPC** miễn là chưa có bản vẽ cùng loại trong kho.
- **Thu dọn bình thường (pack-up)**: toàn bộ nội dung đính vào bản vẽ, không mất gì. Bản vẽ về kho chủ.
- **Bị phá hủy**: một phần nội dung rơi ra map theo **Structure Loot Drop Rate** (xem section riêng); phần còn lại đính vào bản vẽ → bản vẽ về tay chủ nhân.
- **Bản vẽ giữ nguyên cấu trúc + nội dung còn lại** sau khi bị phá → chủ có thể mở lại ở vị trí mới, hoạt động như cũ.

Feature-specific applications:

- **Cave blueprint**: thuộc về player cá nhân. Bố cục, tài sản, linh thú bảo vệ đều lưu trong bản vẽ.
- **Sect blueprint**: thuộc về môn chủ. Khi tông môn bị phá, bản vẽ về tay môn chủ — có thể mở lại ở vị trí mới; toàn bộ đệ tử có thể quay về, mọi hoạt động tiếp tục như cũ (chỉ mất phần tài sản đã rơi theo drop rate).

### Ownership / Drop Rights

Status: canonical.

Canonical rule:

- Khi player chết và đồ rơi xuống map, **đồ thuộc quyền ưu tiên của player chết** trong một khoảng thời gian cấu hình (`game_configs`).
- Trong thời gian ưu tiên: người khác vẫn **nhìn thấy đồ**, nhưng khi cố nhặt sẽ nhận thông báo "chưa thể nhặt, cần đợi X giây".
- Sau khi hết thời gian ưu tiên: đồ trở thành **public** — ai cũng nhặt được.
- Pet auto-loot của chủ nhân **tuân theo cùng rule ưu tiên này**: pet nhặt được đồ của chủ (kể cả trong priority window), và nhặt được đồ public của người khác sau khi hết priority. Xem thêm rule Pet Auto-Loot bên dưới.
- **Looting window (sau khi cấu trúc bị phá)**: rule ưu tiên death drop **vẫn áp dụng** bình thường — không bị override bởi looting window. Người chết trong looting window vẫn có priority window cho đồ của mình.
- **Structure destruction drops** (đồ rơi khi cổng động phủ / tông môn vỡ): đây không phải death drop — những item này rơi vào map và có thể được nhặt tự do bởi bất kỳ ai đang trong looting window (không có priority owner). Xem Structure Loot Drop Rate.

### Pet Auto-Loot

Status: canonical.

Canonical rule:

- Linh Thú có khả năng nhặt đồ **không tự động spam liên tục**. Mỗi chu kỳ cố định (ví dụ ~1 giây, cấu hình trong `game_configs`), pet tìm item có thể nhặt trong vùng bán kính của nó và gửi yêu cầu nhặt.
- **Tuân theo Ownership / Drop Rights**: pet chỉ nhặt đồ mà chủ nhân có quyền nhặt tại thời điểm đó (đồ của chính chủ, hoặc đồ public đã hết priority window).
- Pet **không thể nhặt đồ vẫn còn trong priority window của người khác**.
- **2 pet cùng nhắm 1 drop**: chỉ 1 pet nhặt được; item về tay chủ nhân bình thường.
- **Chủ chết** → pet dừng nhặt, đứng yên cho đến khi chủ hồi sinh.
- **Balo chủ đầy** → pet dừng nhặt cho đến khi có chỗ trống.
- Có thể bật / tắt trong cấu hình linh thú.
- Trong looting window: pet vẫn nhặt đồ theo đúng rule trên — đồ structure drop (public ngay) thì nhặt được; đồ death drop vẫn theo priority window.

### PvP State Taxonomy

Status: canonical.

Mục đích của taxonomy này là mô tả **ngữ cảnh chiến đấu** — để feature docs dùng chung khi cần mô tả điều kiện, trigger, hoặc penalty bổ sung. Taxonomy không quyết định có/không có baseline death penalty (baseline luôn áp dụng — xem Death Taxonomy).

Canonical states:

| State | Ý nghĩa đơn giản | Kích hoạt như thế nào |
|---|---|---|
| `neutral` | Không có chiến đấu, không có cam kết PvP | Mặc định |
| `duel` | 2 người đồng ý đánh nhau; chỉ 2 người này tấn công được nhau | Cả 2 bấm đồng ý |
| `pvp_zone` | Cả map cho phép đánh nhau tự do | Bước vào map có flag `pvp_mode: free_for_all` |
| `cave_raid` | Đang công / thủ động phủ của người khác | Bùa phá phủ được kích hoạt |
| `sect_war` | Đang công / thủ cổng tông môn | Bùa phá tông môn được kích hoạt |
| `mineral_conflict` | Đang tranh giành mỏ linh thạch | Tấn công trong khu vực mỏ tông môn |
| `pk` | Tấn công không được đồng ý ngoài các context được phép | Tấn công player ngoài các state trên |

Về penalty:

- **Tất cả ngữ cảnh chết** đều chịu baseline death penalty như nhau (xem Death Taxonomy).
- `pk` là nguyên nhân chết **đặc biệt** — áp dụng thêm **penalty bổ sung** (trừ thêm thọ nguyên / rút ngắn Lôi Kiếp) do game design data cấu hình.
- `duel`, `pvp_zone`, `cave_raid`, `sect_war`, `mineral_conflict`: chỉ baseline, không phải PK — trừ khi feature đó định nghĩa penalty riêng.

Clarifications:

- **PvP Zone**: tấn công nhau trong map pvp_zone **không phải PK**, dù không có đồng thuận riêng.
- **Duel bỏ chạy / thoát map**: duel tự kết thúc, không phạt bên nào.
- **Mineral conflict**: chết trong khu vực tranh mỏ → baseline penalty, không tính PK.
- **Không có friendly fire trong tông môn**: thành viên cùng tông vẫn tấn công nhau bình thường khi ở trong các context PvP cho phép — không có mechanic miễn sát thương đồng đội.

### Structure Loot Drop Rate

Status: canonical.

Canonical rule:

- Khi một cấu trúc (động phủ, tông môn) bị phá hủy hoàn toàn, một phần nội dung **rơi ngẫu nhiên ra map** trong looting window.
- Drop rate là một **khoảng ngẫu nhiên** cấu hình per-structure (ví dụ 20–30%); server roll 1 số trong khoảng đó để quyết định % thực tế cho từng loại tài sản.
- Áp dụng **độc lập** cho mỗi loại tài sản:
  - Linh thạch: roll rate riêng → rơi X% số linh thạch trong cấu trúc.
  - Item: roll rate riêng → rơi Y% tổng số item.

Cách đếm item khi roll:

- Mỗi stack (dù 1 hay nhiều cái) tính là **1 đơn vị** khi roll dice.
- Item đơn lẻ cũng tính là 1 đơn vị.
- Ví dụ: 3 loại item stack (mỗi loại 30 cái) + 10 item đơn lẻ = **13 đơn vị** tổng. Nếu roll 25% → rơi 3–4 đơn vị (server chọn ngẫu nhiên đơn vị nào rơi). Nếu 1 đơn vị là stack 30 thì toàn bộ stack đó rơi ra.
- Phần không rơi **đính vào bản vẽ** → về tay chủ nhân.

Structure drops là **public ngay** (không có priority owner) — bất kỳ người nào trong looting window đều nhặt được.

Feature-specific config:

- `cave.loot_drop_rate_lingstone_min/max` — % linh thạch động phủ rơi khi bị phá.
- `cave.loot_drop_rate_item_min/max` — % item động phủ rơi khi bị phá.
- `sect.loot_drop_rate_lingstone_min/max` — % linh thạch bảo khố (gồm cả hàng shop) rơi khi cổng vỡ.
- `sect.loot_drop_rate_item_min/max` — % item bảo khố (gồm cả hàng shop) rơi khi cổng vỡ.

### Offline Time-Based Activities

Status: canonical.

Canonical rule:

- Các hoạt động **tính thời gian thực** (tu luyện, khai thác mỏ, luyện chế nguyên liệu, pháp khí, đan dược, phù lục, trận pháp, khôi lỗi) **không yêu cầu player phải online**.
- Chỉ cần player đã **vào đúng địa điểm yêu cầu** và **kích hoạt hoạt động** — sau đó có thể offline; thời gian vẫn được tính.
- Khi đăng nhập lại: server settle kết quả dựa trên khoảng thời gian thực đã trôi qua; player nhận kết quả hoặc notification ngay khi vào game.
- Player vẫn ở trong **trạng thái hoạt động** (cultivating, mining, crafting...) khi đăng nhập lại — không bị reset về idle.

Shared implication:

- Feature docs không nên viết "phải online để tiến trình tiếp tục".
- Mọi hoạt động time-based đều dùng server-side settlement timestamp, không tin tưởng client timer.
- Điều kiện bắt đầu (đúng địa điểm, đủ điều kiện) là cứng; sau khi bắt đầu thì offline vẫn tính.

Known applications:

| Hoạt động | Địa điểm yêu cầu |
|---|---|
| Tu luyện | Private home instance |
| Khai thác mỏ | Bên trong mỏ (vein interior) |
| Luyện đan | Private home instance |
| Luyện pháp khí / phù lục / trận pháp | Private home instance (cần confirm) |
| Luyện chế nguyên liệu | Private home instance (cần confirm) |
| Khôi Lỗi | Không áp dụng time-based — hoạt động theo lệnh |

`requires_code_verification: true` — các dòng "cần confirm" sẽ được TechDesign xác nhận khi review.

### Cultivation Penalty (Realm Drop + Potential Revert)

Status: canonical.

Canonical rule:

Rule này áp dụng khi **tu vi bị trừ vì bất kỳ penalty nào** (đột phá thất bại, Lôi Kiếp thất bại, hoặc các penalty tương lai).

**Bước 1 — Trừ tu vi:**
- Tu vi bị trừ theo **% của cultivation hiện tại**, tỉ lệ do game design data cấu hình per-penalty.
- Nếu sau khi trừ, cultivation tụt **dưới ngưỡng tối thiểu của cảnh giới hiện tại** → player **tụt 1 cảnh giới**.

**Bước 2 — Tụt cảnh giới (nếu xảy ra):**
- Player bị đặt về cảnh giới ngay dưới.
- Cultivation ở cảnh giới mới = **cultivation tương ứng sau khi trừ** (không reset về 0, không reset về max).
- **Không có bình cảnh khi leo lại**: sau khi tụt, player có thể đột phá lại ngay khi đủ cultivation — không bị block bởi "đã từng tụt từ cảnh giới đó".

**Bước 3 — Revert tiềm năng (potential revert):**
- Khi tụt cảnh giới, **tiềm năng bị trừ tương ứng** theo lượng tiềm năng lẽ ra thuộc cảnh giới bị mất.
- Nếu player đã dùng tiềm năng để nâng chỉ số: **đảo ngược công thức** — chỉ số bị giảm để lấy lại tiềm năng.
- Thứ tự revert: **chỉ số được nâng nhiều nhất trước**, sau đó đến chỉ số thứ 2, v.v.
- Server lưu số lần dùng tiềm năng per stat (`upgrade_count`) → đảo ngược là deterministic.

Shared implication:

- `potential_reward_locked` (thiết kế cũ) không còn là cơ chế canonical cho penalty — thay bằng potential revert rule này.
- Feature docs mô tả penalty cultivation chỉ cần reference rule này, không tự define lại.
- Rule áp dụng đồng nhất cho: đột phá thất bại, Lôi Kiếp thất bại, và các penalty tương lai cùng loại.

Known applications:

- Đột phá cảnh giới thất bại: cultivation bị trừ %; nếu tụt cảnh giới thì áp dụng full rule này.
- Lôi Kiếp thất bại: tụt 1 cảnh giới → áp dụng full rule này.

### Structure Deployment Cast Time / Setup Lock

Status: canonical.

Canonical rule:

- Triển khai **động phủ**, **tông môn**, và **chiếm mỏ bằng bản vẽ khai thác** đều có **cast time mặc định 1 phút**.
- Trong thời gian cast:
  - Không thể bị tấn công.
  - Người khác không thể vào cấu trúc / khu vực đang được mở.
  - Nếu là động phủ: chưa thể vào động phủ trong lúc cast.
- Sau khi cast hoàn tất: cấu trúc vào trạng thái hoạt động bình thường và các rule công / thủ mới bắt đầu áp dụng.

Feature-specific notes:

| Hệ | Cast time mặc định |
|---|---|
| Động phủ | 1 phút |
| Tông môn | 1 phút |
| Mỏ linh thạch (dùng bản vẽ khai thác) | 1 phút |

Balance note:

- User chose to keep this unified at 1 minute for now. Future data design may still expose config, but current canonical design is shared and fixed at 1 minute.

### Interaction Range

Status: canonical (already implemented in repo).

Canonical rule:

- Mọi đối tượng tương tác (NPC, entity, cổng, v.v.) đều dùng **chung 1 rule interaction range** đã được implement trong repo.
- Player phải ở trong range xác định mới có thể tương tác với đối tượng.
- Feature docs không tự định nghĩa lại range rule; chỉ reference shared rule này.
- Giá trị range cụ thể và behavior khi ra ngoài range (đóng UI, hủy giao dịch, v.v.) do implementation trong repo quyết định — cần code verification nếu cần thay đổi.

Known applications:

- NPC interaction (xem `features/npc-system.md`)
- Tất cả interactable entity trong game

---

### Guaranteed Hit

Status: canonical.

Canonical rule:

- Một số skill có flag **guaranteed hit** — khi kích hoạt, skill **bypass hoàn toàn evasion calculation**.
- Guaranteed hit không phụ thuộc vào chênh lệch Speed giữa attacker và defender.
- Guaranteed hit là property của skill data (flag per skill trong DB), không phải stat của player.
- Evasion buff trên defender cũng bị vô hiệu hóa hoàn toàn bởi guaranteed hit.

Known applications:

- Speed / evasion system (xem `features/speed-system.md`)
- Skill data design

### Tu Vi / Tiềm Năng Chia Sẻ (Proximity Sharing)

Status: canonical.

Canonical rule:

- Khi player A đánh quái và nhận **tu vi + tiềm năng**, các thành viên **cùng tông môn** của A đang ở **cùng map** và trong **tầm proximity** cũng nhận được một lượng tương ứng.
- Chỉ áp dụng cho thành viên **cùng tông môn** — không áp dụng cho party member khác tông hoặc player ngẫu nhiên gần đó.
- Proximity range: config trong `game_configs`.
- Tỉ lệ chia sẻ tu vi/tiềm năng: data design xác định sau.
- Rule này active bất kể có party hay không — chỉ cần cùng tông môn, cùng map, trong proximity.

Known applications:

- Party system (xem `features/party-system.md`)
- Sect system — thành viên cùng tông môn trên cùng map

### Pháp Khí Penalty (Cấp Tụt)

Status: canonical.

Canonical rule:

- Khi penalty xảy ra trên pháp khí sinh trưởng: **exp bị trừ theo % exp hiện tại** của cấp đó — tỉ lệ config per penalty type.
- Nếu exp tụt xuống dưới 0 của cấp hiện tại → **tụt 1 cấp**, exp về mức tương ứng.
- Khi tụt cấp: **chỉ số và skill đã mở khóa ở cấp đó bị khóa lại** cho đến khi lên lại.
- Pháp khí ở cấp 0 bị penalty: giữ ở 0, không tụt thêm.
- Không có “bình cảnh” — lên lại được ngay khi đủ exp.
- Rule này độc lập với cultivation penalty rule của player — không dùng chung.
- Penalty type list (chết, v.v.) — data design xác định.

Known applications:

- Magic weapon system (xem `features/magic-weapon-system.md`)
