---
doc_type: game_design_feature
system_id: sect-system
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-13
updated_at: 2026-05-13
promoted_from:
  - notes/sect-system.md
  - notes/sect-quest-system.md
  - notes/sect-shop-system.md
  - notes/sect-pvp-system.md
  - notes/sect-headquarters-system.md
related_docs:
  - notes/mineral-vein-system.md
  - features/home-cave-defense.md
  - features/spirit-beast.md
  - features/crafting-talisman-formation.md
  - features/death-penalty.md
  - notes/player-interaction-group.md
requires_code_verification: true
---

# Tông Môn — Feature Draft

## Goal

Tông Môn là hệ thống xã hội cốt lõi của game — vận hành như một "công ty nhỏ" có tài chính, phân công vai trò, nhiệm vụ nội bộ, kinh tế, và cạnh tranh bên ngoài. Mục tiêu tạo ra **hoạt động xã hội chính và nguồn niềm vui chính** cho người chơi dài hạn.

**Player value:**
- Người chơi cảm nhận được sự thuộc về — không chỉ là thành viên mà là người có vai trò thực
- Môn chủ có trải nghiệm quản lý thật sự: tài chính, nhân sự, nhiệm vụ, kinh doanh
- Đệ tử có nhiều con đường đóng góp: khai thác, nhiệm vụ, giao dịch, chiến đấu
- Tông môn mạnh = trạng thái xã hội cao và bảo vệ tập thể thật sự

---

## Design Summary

Tông môn được thành lập bằng bản vẽ tông môn + nộp linh thạch vào bảo khố. Người thành lập trở thành môn chủ duy nhất — có toàn quyền quản lý, phân quyền thủ công cho đệ tử, và là người duy nhất có thể giải tán hoặc chuyển giao.

Vòng lặp tuần là trung tâm: môn chủ chuẩn bị pool nhiệm vụ bắt buộc + phúc lợi đầu tuần → đệ tử nhận nhiệm vụ, khai thác, luyện chế → output về bảo khố tự động → nhận phúc lợi khi đủ quota.

Bảo khố là "tài khoản ngân hàng" của tông môn — escrow phúc lợi, escrow thưởng nhiệm vụ, và nguồn vốn cho shop. Shop tông môn mở 24/7 cho cả thành viên lẫn người ngoài mua/bán bằng linh thạch.

Tông môn có thể chiếm mỏ linh thạch và cổng tông môn trên map public — đây là tài sản chiến lược thật sự. Đệ tử đặt động phủ trong khu vực tông môn được bảo vệ tập thể. Mất cổng tông môn = tông môn giải tán ngay lập tức.

---

## Scope

### In Scope
- Thành lập, vận hành, giải tán tông môn
- Bảo khố và escrow
- Hệ thống quyền hạn và vai trò
- Nhiệm vụ tông môn (bắt buộc, tự nguyện, cá nhân)
- Phúc lợi hàng tuần
- Shop tông môn (bán ra + thu mua)
- Khai thác mỏ linh thạch dưới danh nghĩa tông môn
- Cổng tông môn và PvP tông môn
- Động phủ trong khu vực tông môn
- Bản vẽ tông môn, looting window
- Chat tông môn (2 kênh)
- Gia nhập, rời tông môn

### Out Of Scope
- Balance cụ thể (%, HP, tỉ lệ rơi item, giá bán)
- Cơ chế đấu giá shop
- UI chi tiết từng màn hình
- Data model / backend
- Party system
- Liên minh tông môn in-game (không tồn tại)

---

## Key Terms

- `Môn chủ`: người thành lập hoặc kế thừa tông môn. Có toàn quyền quản lý.
- `Phó tông chủ`: danh hiệu + ưu tiên số 1 trong chuỗi kế thừa tự động. Không có quyền mặc định.
- `Trưởng lão`: danh hiệu thuần túy, không có quyền mặc định.
- `Đệ tử`: tên gọi chung cho tất cả thành viên bình thường.
- `Bảo khố`: kho tài sản chung — chứa linh thạch và vật phẩm.
- `Ngưỡng tồn tại`: 10 linh thạch tối thiểu trong bảo khố.
- `Bản vẽ tông môn`: item gắn với 1 người — lưu mô hình tông môn + toàn bộ nội dung sau khi thu dọn. Không giao dịch được. Tối đa 1 / người.
- `Thu dọn`: đóng gói toàn bộ nội dung vào bản vẽ để di chuyển — không mất gì.
- `Looting window`: 1 phút sau khi cổng tông môn vỡ — PvP tự do trong map, item rơi ngẫu nhiên.
- `Whitelist thoải mái`: danh sách thành viên được khai thác mỏ tự do, không qua nhiệm vụ.
- `Template nhiệm vụ`: loại nhiệm vụ do gamedata define. Môn chủ chọn từ danh sách và set tham số.
- `Instance nhiệm vụ`: 1 nhiệm vụ cụ thể được tạo từ template. Pool là flat list instance.
- `Escrow`: tài sản bị khóa tự động khi tạo nhiệm vụ hoặc đơn thu mua — không ai dùng được cho đến khi resolve. Task hết hạn / trả về pool thì escrow giữ nguyên; chỉ khi **hủy task** mới hoàn phần chưa dùng về source. Xem canonical rule tại `shared-rules.md`.

---

## Core Loop

### Vòng lặp tuần (đệ tử)
1. Đầu tuần: xem pool nhiệm vụ bắt buộc + phúc lợi tuần đã công bố.
2. Nhận instance từ pool → thực hiện (khai thác, tinh luyện, luyện chế...).
3. Output tự về bảo khố, instance tự done — không cần về báo cáo.
4. Khi đủ quota → claim phúc lợi tuần.

### Vòng lặp tuần (môn chủ)
1. Đầu tuần: tạo pool nhiệm vụ bắt buộc, config phúc lợi → escrow tự khóa từ bảo khố.
2. Quản lý shop: đưa hàng ra bán, đặt đơn thu mua.
3. Quản lý khai thác: giao chỉ tiêu, quản lý whitelist thoải mái.
4. Quản lý thành viên: phê duyệt đơn gia nhập, phân quyền.

### Vòng lặp PvP
1. Chiếm mỏ linh thạch dưới danh nghĩa tông môn.
2. Bảo vệ mỏ và cổng tông môn khỏi tấn công.
3. Hoặc tấn công tông môn đối thủ để cướp tài sản bảo khố.

---

## Player-Facing Rules

### A. Thành lập & Giải tán

- **Thành lập:** mua bản vẽ tông môn tại NPC → chọn vị trí trên map public → nộp ≥ 10 linh thạch vào bảo khố → tông môn tồn tại.
- **Ngưỡng tồn tại:** bảo khố < 10 linh thạch → thông báo toàn tông môn + đếm ngược 24h. Nếu không bổ sung → tự động giải tán, tài sản trả về môn chủ.
- **Giải tán chủ động:** chỉ môn chủ. Tài sản bảo khố trả về môn chủ.
- **Giải tán do không còn ai kế thừa** (offline > 100 ngày): tài sản mất hết.
- **Không có cấp bậc tông môn.** Tối đa 200 thành viên.

### B. Vai Trò & Quyền Hạn

Quyền không gắn với danh hiệu — môn chủ phân thủ công cho từng người.

| Quyền | Mô tả | Phân được? |
|---|---|---|
| Giải tán tông môn | Chủ động giải tán | ❌ chỉ môn chủ |
| Quản lý thành viên | Phê duyệt gia nhập, sa thải (trừ người cùng có quyền này) | ✅ |
| Quản lý bảo khố & shop | Đưa hàng ra shop, thu mua, rút/nạp bảo khố | ✅ |
| Đặt nhiệm vụ tông môn | Tạo nhiệm vụ bắt buộc và tự nguyện | ✅ |
| Quản lý chat nhóm | Add thành viên vào kênh private | ✅ |
| Quản lý khai thác mỏ | Giao chỉ tiêu, quản lý whitelist thoải mái, **dùng danh nghĩa tông môn** để chiếm mỏ hoặc phát động chiến dịch công | ✅ |

> Người có quyền Quản lý thành viên **không** sa thải được nhau. Chỉ môn chủ sa thải được họ.
> Không được kick đệ tử đang giữ instance nhiệm vụ bắt buộc chưa hoàn thành.

### C. Kế Thừa Môn Chủ

**Chuyển giao tự nguyện:** môn chủ chọn người → chuyển ngay lập tức, không cần accept. Môn chủ cũ thành đệ tử thường. Nếu người được chuyển không muốn → ấn giải tán.

**Kế thừa tự động (offline > 100 ngày):**
1. Phó tông chủ (nếu có)
2. Người có quyền quản lý bảo khố gia nhập sớm nhất
3. Thành viên gia nhập sớm nhất
4. Không còn ai → tự động giải tán, tài sản mất hết

### D. Gia Nhập & Rời

- **Gia nhập:** gửi đơn qua cổng tông môn → người có quyền Quản lý thành viên phê duyệt. Không có phí gia nhập.
- **Gia nhập giữa tuần:** không tham gia nhiệm vụ bắt buộc, không nhận phúc lợi tuần đó. Tính từ tuần tiếp theo.
- **Rời:** tự rời bất kỳ lúc nào. Cooldown 1 ngày trước khi gia nhập tông môn khác.

### E. Bảo Khố

- Tất cả thành viên xem được nội dung bảo khố.
- Rút linh thạch: môn chủ / người có quyền, miễn bảo khố còn ≥ 10 linh thạch sau khi rút.
- Escrow phúc lợi + thưởng nhiệm vụ bị khóa tự động — không ai dùng được.

### F. Chat

- **Kênh All:** tất cả thành viên thấy và chat được.
- **Kênh Private:** chỉ vào được khi được add thủ công.

### G. Nhiệm Vụ Tông Môn

#### G1. Nhiệm Vụ Bắt Buộc (Weekly Mandatory)

- Môn chủ / người có quyền tạo pool từ template gamedata. Mỗi instance = 1 nhiệm vụ cụ thể. Pool là flat list.
- **Quota 2 dạng:**
  - *Free quota:* hoàn thành bất kỳ N instance nào.
  - *Typed quota:* hoàn thành đúng X loại A + Y loại B.
- Pool phải đủ cho tất cả đệ tử đã snapshot đầu tuần. Môn chủ có thể thêm instance giữa tuần.
- Pool reset cuối tuần.
- Đệ tử nhận instance → thực hiện → output tự về bảo khố, instance tự done → người chơi nhận thông báo.
- Đệ tử được hủy tối đa **1 lần/ngày** — instance về lại pool.
- Nếu có nhiệm vụ bắt buộc → **bắt buộc phải có phúc lợi tuần**.

#### G2. Nhiệm Vụ Tự Nguyện

- Môn chủ / người có quyền tạo + set thưởng (bắt buộc) + set deadline.
- Thưởng escrow từ bảo khố ngay khi tạo.
- Ai muốn nhận thì nhận — 1 người / instance.
- Chưa ai nhận: treo hoặc chuyển tuần tiếp tuỳ config môn chủ. Escrow giữ nguyên.
- Đã nhận, hết deadline chưa xong: instance về pool kèm thưởng nguyên vẹn. Escrow giữ nguyên.
- **Hủy task chủ động (người có quyền):** escrow hoàn về bảo khố.

#### G3. Nhiệm Vụ Cá Nhân (Member-Posted)

- Bất kỳ thành viên tạo + set thưởng từ inventory cá nhân + set deadline.
- Thưởng escrow từ inventory người tạo ngay khi tạo.
- Chỉ nội bộ tông môn.
- Hết deadline chưa xong: instance về pool kèm thưởng nguyên vẹn. Escrow giữ nguyên.
- **Hủy task chủ động (người tạo hoặc người có quyền):** escrow hoàn về inventory người tạo ban đầu.

#### So sánh 3 dạng

| | Bắt buộc | Tự nguyện | Cá nhân |
|---|---|---|---|
| Người tạo | Môn chủ / quyền | Môn chủ / quyền | Bất kỳ thành viên |
| Bắt buộc nhận? | ✅ (quota) | ❌ | ❌ |
| Thưởng | Phúc lợi tuần | Có thưởng riêng | Có thưởng riêng |
| Escrow | ✅ đầu tuần từ bảo khố | ✅ từ bảo khố khi tạo | ✅ từ inventory người tạo |
| Hết deadline | Reset cuối tuần | Về pool kèm thưởng | Về pool kèm thưởng |

> **Rule chung output:** bất kỳ nhiệm vụ nào có output (linh thạch khai thác, vật phẩm tinh luyện...) → output tự về bảo khố khi done. Người chơi nhận thông báo, không cần về map báo cáo. Chỉ về map để nhận NV mới.

### H. Phúc Lợi Hàng Tuần

- Môn chủ config số lượng phúc lợi cố định / người / tuần (linh thạch hoặc tài nguyên khác).
- **Escrow đầu tuần:** snapshot số đệ tử → khóa tổng phúc lợi từ bảo khố. Không điều chỉnh giữa tuần.
- **Điều kiện nhận:** hoàn thành đủ quota nhiệm vụ bắt buộc trong tuần.
- **Claim:** đệ tử chủ động claim (không tự vào túi).
- **Bị kick sau khi đủ quota:** phúc lợi trả tự động trước khi kick có hiệu lực.
- **Kho đồ đầy khi claim:** phúc lợi / reward được chuyển vào **inbox / hòm thư chờ nhận** theo shared reward-overflow rule.

### I. Cổng Tông Môn & Map Nội Bộ

- Mỗi tông môn có 1 cổng tông môn trên map public. Chiếm diện tích lớn hơn động phủ.
- Nhấn vào cổng → menu: Xem thông tin / Cửa hàng / Xin gia nhập / Vào tông môn (thành viên).
- **Map nội bộ:** nơi thành viên xem nhiệm vụ, bảo khố, hoạt động nội bộ.

### J. Shop Tông Môn

- Mở 24/7. Tất cả mọi người (thành viên + người ngoài) đều mua và bán được.
- Tiền tệ duy nhất: **linh thạch**.
- **Giao diện bán ra:** 30 slot cố định. Mỗi loại item = 1 slot. Stackable follow item system. Tiền bán vào thẳng bảo khố. Vật phẩm trừ bảo khố ngay khi đưa ra.
- **Giao diện thu mua:** 30 slot cố định, độc lập với bán ra. Escrow linh thạch ngay khi tạo đơn. Vật phẩm thu mua vào thẳng bảo khố. Điều chỉnh realtime.
- **Realtime notify:** người đang mở giao diện nhận thông báo khi có thay đổi, load lại.
- **Lịch sử giao dịch:** lưu 1 ngày gần nhất, tự xóa sau đó.
- **Khi giải tán:** shop đóng ngay, hàng + escrow về bảo khố.

### K. Khai Thác Mỏ Linh Thạch

> Chi tiết cơ chế chiếm mỏ, công phá, bảo vệ → xem `notes/mineral-vein-system.md`.

**Quyền dùng danh nghĩa tông môn:** chỉ người có quyền Quản lý khai thác mỏ.

**Truy cập mỏ tông môn — 2 tier:**

| Tier | Điều kiện | Giới hạn | Linh thạch về đâu |
|---|---|---|---|
| Khai thác nhiệm vụ | Đang giữ NV khai thác active | Đến hết quota NV | Bảo khố tự động, NV tự done |
| Whitelist thoải mái | Được add vào whitelist | Không giới hạn | Túi cá nhân |

- Hai tier độc lập. Vừa có NV vừa trong whitelist: xong NV tiếp tục khai thác vào túi, không bị out.
- Hết quota NV, không trong whitelist → bị out khỏi mỏ.

### L. PvP Tông Môn

**Chỉ 2 hình thức:**

1. **Tranh mỏ linh thạch** — xem `notes/mineral-vein-system.md`.
2. **Tấn công cổng tông môn** — dùng bùa phá tông môn (item tiêu hao).

**Cổng tông môn vỡ = tông môn giải tán ngay lập tức:**
- Bảo khố: % rơi vào người dùng bùa phá (người khởi động). Phần còn lại về môn chủ.
- Phe thủ tele ra map random ngay lập tức.
- Bản vẽ tông môn: item rơi ngẫu nhiên trong map theo tỉ lệ → phần còn lại đính vào bản vẽ → về tay môn chủ.
- Bản vẽ động phủ đệ tử: trả về tay từng đệ tử nguyên vẹn.
- Looting window 1 phút bắt đầu.
- Lập lại: mua bản vẽ tông môn mới tại NPC → chọn vị trí → lập từ đầu.

**Không có cơ chế liên minh in-game.** Ngoài map combat: tương tác như người lạ bình thường.

### M. Bản Vẽ Tông Môn & Looting Window

**Bản vẽ:**
- Tối đa 1 / người. Không giao dịch.
- Lưu toàn bộ nội dung + bố cục sau khi thu dọn.
- Thu dọn bình thường (chỉ môn chủ): toàn bộ đính vào bản vẽ, không mất gì.
- Bị công phá: item rơi ngẫu nhiên trong map theo tỉ lệ, phần còn lại đính vào bản vẽ về tay môn chủ.

**Looting window 1 phút:**
- Chỉ người đang trong map (phe công) được loot.
- PvP tự do — ai cũng tấn công được nhau.
- Không được rời map trong 1 phút.
- Offline trong map → toàn bộ đồ đã nhặt rơi ra ngay.
- Chết → rule chết bình thường (tỉ lệ rớt đồ đang mang). Respawn về động phủ cá nhân nếu có, không có → map random.
- Sau 1 phút: tất cả tele ra map random. Người sống giữ đồ đã nhặt.

### N. Động Phủ Trong Khu Vực Tông Môn

- Động phủ đặt trong khu vực tông môn **không thể bị công phá trực tiếp**.
- Phải phá cổng tông môn trước → mới công phá được động phủ bên trong.
- **Slot giới hạn, ai đặt trước được trước.** Hết slot → chờ người khác rời.
- **1 đệ tử chỉ đặt động phủ ở 1 nơi tại 1 thời điểm** (trong tông môn hoặc tự mở ngoài, không đồng thời).
- Đệ tử tự đặt / tự rút, không cần phê duyệt.
- Bị kick: bản vẽ động phủ trả về tay đệ tử nguyên vẹn — item không mất.
- Tông môn giải tán → động phủ trong khu vực biến mất. Item / linh thú có tỉ lệ rơi mất, còn lại về túi người chơi.

---

## System States

| State | Mô tả |
|---|---|
| `active` | Tông môn đang vận hành bình thường |
| `warning` | Bảo khố < ngưỡng 10 linh thạch, đếm ngược 24h |
| `under_attack` | Cổng tông môn đang bị tấn công |
| `looting_window` | Cổng vỡ, 1 phút looting, tông môn đã giải tán |
| `dissolved` | Tông môn đã giải tán |

---

## Main Flows

### Flow 1 — Thành lập tông môn
1. Mua bản vẽ tông môn tại NPC.
2. Chọn vị trí hợp lệ trên map public.
3. Nộp ≥ 10 linh thạch vào bảo khố.
4. Tông môn tồn tại. Người thành lập = môn chủ.

### Flow 2 — Vòng lặp tuần
1. Đầu tuần: môn chủ tạo pool nhiệm vụ + config phúc lợi → hệ thống escrow tự động.
2. Đệ tử vào map nội bộ, nhận instance từ pool.
3. Thực hiện nhiệm vụ (khai thác, tinh luyện...) — output tự về bảo khố.
4. Khi đủ quota → claim phúc lợi tuần.

### Flow 3 — Giao dịch shop
1. Người mua / bán vào cổng tông môn → chọn Cửa hàng.
2. Mua: chọn item → trả linh thạch → nhận item. Tiền vào bảo khố.
3. Bán vào đơn thu mua: chọn đơn → nộp vật phẩm → nhận linh thạch từ escrow. Vật phẩm vào bảo khố.

### Flow 4 — Cổng tông môn bị phá
1. Kẻ tấn công dùng bùa phá tông môn → chiến dịch bắt đầu.
2. Phe thủ phòng ngự (trận pháp, linh thú, chiến đấu trực tiếp).
3. Cổng HP về 0 → vỡ.
4. Phe thủ tele ra ngay. Item rơi ngẫu nhiên trong map.
5. Looting window 1 phút: PvP tự do, không rời được.
6. Sau 1 phút: tất cả tele ra. Bản vẽ về tay môn chủ.
7. Tông môn giải tán hoàn toàn.

### Flow 5 — Giải tán / Tái lập
1. Môn chủ giải tán chủ động hoặc hệ thống tự động giải tán (bảo khố cạn / không còn ai kế thừa).
2. Tài sản xử lý theo rule tương ứng.
3. Muốn lập lại: mua bản vẽ tông môn mới → chọn vị trí mới → lập từ đầu.

---

## Edge Cases

- Đệ tử bị kick khi đang nhận NV bắt buộc chưa xong → không được kick (server block).
- Đệ tử bị kick sau khi đủ quota → phúc lợi trả tự động trước khi kick có hiệu lực.
- Bảo khố cạn giữa tuần (sau khi escrow) → chỉ escrow phần đã lock; phần còn lại không bị escrow (bảo khố không đủ để khóa tiếp).
- Pool nhiệm vụ bắt buộc hết giữa tuần (do hủy nhiều) → môn chủ phải thêm instance thủ công.
- Đơn thu mua partially filled khi môn chủ hủy → phần chưa mua escrow trả về bảo khố, phần đã mua giữ nguyên.
- Chủ mỏ tông môn offline → người có quyền Quản lý khai thác mỏ quản lý bình thường.
- Tông môn giải tán khi có đơn thu mua đang mở → escrow về bảo khố → về môn chủ.
- Offline trong looting window → toàn bộ đồ đã nhặt rơi ra ngay.
- Chết trong looting window không có động phủ → tele map random.

---

## Data / Config Needs

- `sect.min_treasury` — ngưỡng tồn tại linh thạch (mặc định 10)
- `sect.max_members` — tối đa thành viên (200)
- `sect.dissolution_countdown_hours` — đếm ngược khi bảo khố cạn (24h)
- `sect.offline_succession_days` — ngưỡng offline kế thừa (100 ngày)
- `sect.cooldown_rejoin_hours` — cooldown sau khi rời (1 ngày)
- `sect.shop_sell_slots` — slot bán ra (30)
- `sect.shop_buy_slots` — slot thu mua (30)
- `sect.shop_transaction_log_days` — lịch sử giao dịch (1 ngày)
- `sect.quest_pool_reset_day` — ngày reset pool (đầu tuần)
- `sect.welfare_escrow_day` — ngày escrow phúc lợi (đầu tuần)
- `sect.member_quest_cancel_per_day` — giới hạn hủy NV bắt buộc (1 lần/ngày)
- `sect.looting_window_seconds` — thời gian looting window (60 giây)
- `sect.cave_slot_limit` — số slot động phủ trong khu vực tông môn (data design)
- `sect.treasury_attack_drop_pct` — % bảo khố rơi vào người phá cổng (data design balance)
- `sect.item_drop_pct_on_destruction` — % item rơi khi bị công phá (data design balance)

---

## UI / UX Notes

- Cổng tông môn map public: menu dropdown khi nhấn vào.
- Map nội bộ: tab nhiệm vụ / bảo khố / thành viên / shop.
- Shop: 2 tab riêng biệt (Bán ra / Thu mua). Realtime notify khi có thay đổi.
- Phúc lợi: nút claim rõ ràng, hiển thị trạng thái quota tuần hiện tại.
- Bảo khố cạn: cảnh báo nổi bật cho toàn bộ thành viên.
- Pool nhiệm vụ: hiển thị số instance còn lại theo loại.

---

## Related Systems

- `notes/mineral-vein-system.md` — chiếm mỏ, khai thác, tranh giành
- `features/home-cave-defense.md` — cơ chế cổng, trận pháp, bản vẽ động phủ, looting window
- `features/spirit-beast.md` — linh thú thủ cổng tông môn
- `features/crafting-talisman-formation.md` — trận pháp bảo vệ cổng
- `features/death-penalty.md` — rule chết trong looting window
- `notes/player-interaction-group.md` — PvP state, không có friendly fire off tông môn

---

## Key Decisions

1. Tông môn yêu cầu bảo khố tối thiểu 10 linh thạch để thành lập và tồn tại.
2. Không có cấp bậc tông môn. Tối đa 200 thành viên.
3. Quyền phân thủ công, không gắn với danh hiệu.
4. Phó tông chủ / Trưởng lão chỉ là danh hiệu, không có quyền mặc định.
5. Chuyển giao môn chủ: ngay lập tức, không cần accept, môn chủ cũ thành đệ tử thường.
6. Kế thừa tự động: phó tông chủ → người có quyền bảo khố (gia nhập sớm nhất) → thành viên gia nhập sớm nhất → giải tán (tài sản mất).
7. Gia nhập cần phê duyệt. Không có phí gia nhập. Rời tự do, cooldown 1 ngày.
8. Gia nhập giữa tuần: không tham gia bắt buộc, không nhận phúc lợi tuần đó.
9. Pool bắt buộc: flat list instance, ai nhận trước lấy trước, reset cuối tuần.
10. Quota 2 dạng: free (bất kỳ N) và typed (N loại A + M loại B).
11. Output nhiệm vụ tự về bảo khố khi done — không cần báo cáo.
12. Instance hết deadline: về pool kèm thưởng nguyên vẹn.
13. Escrow phúc lợi snapshot đầu tuần, không điều chỉnh giữa tuần.
14. Shop: 30 slot bán / 30 slot thu mua, cố định. Tiền tệ duy nhất là linh thạch. Không phí giao dịch.
15. Lịch sử giao dịch lưu 1 ngày, tự xóa.
16. Dùng danh nghĩa tông môn (chiếm mỏ, công mỏ, công tông môn): chỉ người có quyền Quản lý khai thác mỏ.
17. Mỏ tông môn: 2 tier truy cập (NV khai thác → bảo khố; whitelist thoải mái → túi cá nhân).
18. Cổng vỡ = giải tán ngay. % bảo khố về người khởi động, còn lại về môn chủ.
19. Bản vẽ tông môn: 1 / người, không giao dịch, lưu nội dung + bố cục. Thu dọn bình thường không mất gì.
20. Looting window 1 phút: PvP tự do, không rời, offline rơi đồ, sau 1 phút tele ra.
21. Bị kick: bản vẽ động phủ về tay đệ tử nguyên vẹn.
22. Tông môn giải tán: động phủ trong khu vực biến mất, item tỉ lệ rơi mất.
23. Không có cơ chế liên minh in-game.

---

## Open Questions

1. Số slot động phủ trong khu vực tông môn → data design.
2. % bảo khố rơi vào người phá cổng → data design balance.
3. Tỉ lệ item rơi khi bị công phá → data design balance.
4. HP cổng tông môn → data design balance.
5. Bùa phá tông môn mua / drop ở đâu → data design.
6. Overlap khu vực nhiều tông môn cùng map → liên quan map design, defer.
7. Thống kê khai thác mỏ (hôm nay / 7 ngày) → data design.
8. Bản vẽ động phủ lần đầu nhận ở đâu (mua NPC / quest / drop) → data design.
9. Giá bản vẽ tông môn → data design.
10. **requires_code_verification:** Typed quota validation — server có block đệ tử nhận sai loại không?
11. **requires_code_verification:** Notify đệ tử khi pool sắp hết — push notification hay tự vào xem?

---

## Known Conflicts / Drift

- Không có conflict nào còn tồn tại sau audit session 2026-05-13.

---

## Requirement Readiness Checklist

- [ ] Behavior is specific enough for `dev` to estimate.
- [ ] Acceptance criteria can be written without guessing.
- [ ] Major edge cases are covered.
- [ ] Config/data needs are listed.
- [ ] Out-of-scope items are explicit.
- [ ] Ready to promote to `requirements/`.
