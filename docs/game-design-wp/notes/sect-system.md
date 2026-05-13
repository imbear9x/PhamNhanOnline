---
doc_type: game_design_note
system_id: sect-system
status: draft
maturity: note
owner: gamedesign
created_at: 2026-05-13
updated_at: 2026-05-13
promoted_from: null
related_docs:
  - features/home-cave-defense.md
  - notes/mineral-vein-system.md
  - notes/player-interaction-group.md
requires_code_verification: false
---

# Tông Môn — Design Note

## Purpose

Tông Môn là hệ thống xã hội cốt lõi của game. Mục tiêu là trở thành **hoạt động xã hội chính, nguồn niềm vui chính** — vận hành có tổ chức như một "công ty nhỏ": có tài chính, phân công vai trò, nhiệm vụ nội bộ, hoạt động kinh tế, và cạnh tranh bên ngoài.

---

## Core Fantasy / Player Value

- Người chơi cảm nhận được sự thuộc về — không chỉ là thành viên mà là người có vai trò thực trong tổ chức
- Môn chủ có trải nghiệm quản lý thật sự: tài chính, nhân sự, nhiệm vụ, kinh doanh
- Đệ tử có nhiều con đường đóng góp: khai thác, nhiệm vụ, giao dịch, chiến đấu
- Tông môn mạnh = trạng thái xã hội cao trong game world

---

## Key Terms

- `Môn chủ`: người thành lập hoặc kế thừa tông môn. Có toàn quyền quản lý.
- `Phó tông chủ`: danh hiệu + ưu tiên số 1 trong chuỗi kế thừa tự động.
- `Trưởng lão`: danh hiệu thuần túy, **không có thực quyền mặc định**.
- `Đệ tử`: tên gọi chung cho tất cả thành viên bình thường (không phải môn chủ).
- `Bảo khố`: kho tài sản chung của tông môn — chứa linh thạch và các vật phẩm khác.
- `Ngưỡng tồn tại`: lượng linh thạch tối thiểu trong bảo khố để tông môn tồn tại (mặc định: 10 linh thạch).

---

## Draft Rules

### A. Thành lập tông môn

- Yêu cầu: nộp vào bảo khố tối thiểu **10 linh thạch** (ngưỡng tồn tại).
- Người nộp trở thành **môn chủ**.

### B. Bảo khố

- Chứa: linh thạch + tài liệu + đan dược + phù lục + trận pháp (và các vật phẩm khác tùy mở rộng).
- Tất cả thành viên **được xem** nội dung bảo khố.
- **Nạp/rút linh thạch**: môn chủ (hoặc ai được phân quyền) có thể nạp thêm hoặc rút, miễn bảo khố còn ≥ 10 linh thạch sau khi rút.
- **Escrow nhiệm vụ:** Khi tạo nhiệm vụ có thưởng, phần thưởng (linh thạch hoặc item) bị **khóa ngay lập tức** khỏi bảo khố (hoặc khỏi inventory người tạo với nhiệm vụ cá nhân) — không thể dùng cho việc khác cho đến khi nhiệm vụ hoàn thành hoặc bị hủy.

### C. Giải tán tông môn

- **Chỉ môn chủ** có quyền chủ động giải tán.
- Tự động giải tán nếu bảo khố dưới ngưỡng tối thiểu **và** đếm ngược 24h hết mà không được bổ sung.
- Khi giải tán (chủ động hoặc tự động do bảo khố cạn): toàn bộ tài sản trong bảo khố **trả về môn chủ**.
- Khi giải tán do **môn chủ offline > 100 ngày mà không có ai kế thừa**: toàn bộ tài sản **mất hết**.

### D. Cảnh báo & đếm ngược giải tán (bảo khố cạn)

- Khi bảo khố giảm xuống dưới ngưỡng 10 linh thạch: hệ thống **gửi thông báo** cho tông môn — "Tông môn không đủ tài nguyên duy trì".
- Bắt đầu **đếm ngược 24 giờ**.
- Nếu trong 24h có người nạp đủ để bảo khố ≥ 10: đếm ngược hủy, tông môn tồn tại bình thường.
- Nếu hết 24h mà vẫn thiếu: tông môn tự động giải tán, tài sản trả về môn chủ.

---

## Hệ thống Vai Trò

- **Môn chủ:** toàn quyền, duy nhất 1 người.
- **Phó tông chủ:** danh hiệu + ưu tiên kế thừa số 1. *(cần xác nhận: có quyền riêng hay chỉ là danh hiệu + ưu tiên kế thừa?)*
- **Trưởng lão:** danh hiệu thuần túy, không có quyền mặc định.
- **Đệ tử:** thành viên bình thường.

### Chuỗi kế thừa tự động (môn chủ offline > 100 ngày)

1. Chuyển cho **phó tông chủ** (nếu có).
2. Nếu không → chuyển cho **người có quyền quản lý bảo khố gia nhập sớm nhất** (nếu có).
3. Nếu không → chuyển cho **thành viên gia nhập sớm nhất** (ngoại trừ môn chủ cũ).
4. Nếu không còn ai → **tự động giải tán, toàn bộ tài sản mất hết**.

---

## Hệ thống Quyền Hạn

### Mặc định: môn chủ luôn có toàn bộ quyền dưới đây

| Quyền | Mô tả | Phân được cho người khác? |
|---|---|---|
| **Giải tán tông môn** | Chủ động giải tán | ❌ chỉ môn chủ |
| **Quản lý thành viên** | Phê duyệt gia nhập, sa thải đệ tử (trừ người cùng có quyền này). Môn chủ có thể sa thải bất kỳ ai. | ✅ |
| **Quản lý bảo khố & shop** | Chuyển vật phẩm từ bảo khố ra shop tông môn để bán; dùng linh thạch từ bảo khố để thu mua. *(chi tiết phần kinh doanh sẽ bàn riêng)* | ✅ (thường chỉ môn chủ giữ) |
| **Đặt nhiệm vụ tông môn** | Tạo nhiệm vụ bắt buộc và tự nguyện từ template có sẵn | ✅ |
| **Quản lý chat nhóm** | Add thành viên vào nhóm chat private | ✅ |
| **Quản lý khai thác mỏ** | Giao chỉ tiêu khai thác linh thạch cho thành viên từ các mỏ tông môn đã chiếm *(xem mục F)* | ✅ |

> **Nguyên tắc phân quyền:** Quyền không tự động theo danh hiệu. Môn chủ phân quyền thủ công cho từng người.

> ✅ **Chốt:** Người có quyền "Quản lý thành viên" **không** sa thải được nhau. Chỉ môn chủ mới sa thải được họ.

> ⚠️ **Ràng buộc kick:** Không được phép kick đệ tử khi họ đang có nhiệm vụ bắt buộc đang nhận (chưa hoàn thành). Cần server enforce.

---

## E. Hệ thống Chat

- **Kênh 1 (All):** Tất cả thành viên tông môn đều thấy và chat được.
- **Kênh 2 (Private):** Chỉ vào được nếu môn chủ (hoặc người được phân quyền) add vào.

---

## F. Quản lý Khai Thác Mỏ Linh Thạch

> *(liên kết với `notes/mineral-vein-system.md` khi mở note đó)*

- Khi tông môn chiếm được mỏ linh thạch, mỏ đó thuộc quyền khai thác của tông môn.
- Người có quyền **quản lý khai thác mỏ** có thể:
  - Giao **chỉ tiêu hàng tuần** cho từng thành viên (ví dụ: thành viên A phải khai thác ≥ X linh thạch/tuần từ bất kỳ mỏ nào của tông môn).
  - Theo dõi tiến độ hoàn thành của từng người.
  - Nếu không đạt: tùy tình huống có thể cảnh cáo, phạt, hoặc sa thải.
- *(Chi tiết cơ chế chiếm mỏ, bảo vệ mỏ, tranh giành mỏ → sẽ bàn trong Khai Thác Linh Thạch)*

---

## I. Cổng Tông Môn & Shop

### I1. Cổng Tông Môn (ngoài map public)

- Mỗi tông môn có **1 cổng tông môn** nằm tại map public.
- Chiếm diện tích lớn hơn động phủ — số liệu cụ thể do data design xác nhận sau.
- Nhấn vào cổng → mở menu các tùy chọn:

| Tùy chọn | Điều kiện hiện |
|---|---|
| **Xem thông tin** (tên, số thành viên, thông tin môn chủ, phúc lợi tuần, số NV bắt buộc, v.v.) | Tất cả |  
| **Cửa hàng** (mua & bán) | Tất cả |
| **Xin gia nhập** | Người ngoài tông môn |
| **Vào tông môn** (map nội bộ) | Chỉ thành viên |
| **Tấn công tông môn** | *Backlog — bàn sau* |

- **Map nội bộ tông môn:** nơi thành viên xem nhiệm vụ, bảo khố, các hoạt động nội bộ.

### I2. Cửa Hàng Tông Môn

**Ai mua được:**
- **Tất cả** — thành viên lẫn người ngoài, miễn đủ tiền.

**Nguồn hàng (bán ra):**
- Môn chủ / người có quyền **chọn vật phẩm từ bảo khố** đưa ra shop.
- Định giá và số lượng bán do họ set.
- Tiền bán vào **thẳng bảo khố**.

**Thu mua (mua vào):**
- Môn chủ / người có quyền đặt **đơn thu mua**: loại vật phẩm, số lượng, giá mua.
- Ai đem đến bán được → tiền từ bảo khố trả người bán, vật phẩm vào **thẳng bảo khố** (không qua shop).

**Giờ hoạt động:** 24/7.

**Khi tông môn giải tán:**
- Shop đóng ngay lập tức.
- Toàn bộ hàng trong shop chuyển về bảo khố.
- Bảo khố trả về môn chủ theo quy tắc giải tán.

> ⚠️ **Mở rộng chưa bàn:** giới hạn số slot shop, phí giao dịch (nếu có), cơ chế đấu giá, lịch sử giao dịch.

---

## H. Phúc Lợi Hàng Tuần

### Cấu hình

- Môn chủ config **số lượng phúc lợi cố định** cho mỗi tuần.
- Phúc lợi có thể là **linh thạch hoặc tài nguyên khác** từ bảo khố (tuỳ tình hình kinh tế tông môn).
- **Bắt buộc đồng đều:** tất cả thành viên đủ điều kiện nhận cùng một mức phúc lợi, không phân biệt.
- **Ràng buộc:** Nếu có nhiệm vụ bắt buộc thì **bắt buộc phải có phúc lợi**. Không có nhiệm vụ bắt buộc thì phúc lợi là tùy chọn.

### Escrow đầu tuần

- **Đầu mỗi tuần:** tổng phúc lợi cần thiết (số lượng × số đệ tử) được **khóa tự động từ bảo khố** — không ai đụng vào được trong suốt tuần đó.
- Mục đích: ngăn môn chủ rút bảo khố trước khi trả phúc lợi.
- *(Cần xác nhận: nếu số đệ tử thay đổi giữa tuần (thêm/bịt), escrow có điều chỉnh không? — requires\_code\_verification)*

### Điều kiện nhận

- Hoàn thành đủ **số nhiệm vụ tối thiểu** trong tuần.
- Đệ tử gia nhập giữa tuần: nếu làm đủ quota vẫn nhận bình thường. Nếu không đủ: không nhận được, người quản lý thấy trạng thái này và tự xử lý.

### Cơ chế claim

- **Đệ tử chủ động claim** (không tự động vào túi).
- **Trường hợp bị kick sau khi đủ quota:** phúc lợi được trả tự động trước khi kick có hiệu lực.
- **Kho đồ đầy:** nếu inventory chật, hiện thông báo yêu cầu dọn đồ trong **5 phút**.
  - Dọn xong trong 5p: phúc lợi lần lượt vào túi.
  - Hết 5p không dọn: **mất phúc lợi**.
  - *Rule này áp dụng chung cho tất cả trường hợp nhận quà: nhiệm vụ, NPC, v.v.*

---

## G. Hệ thống Nhiệm Vụ Tông Môn

> *(Sẽ tách thành note riêng. Phần này là capture sơ bộ để không mất thông tin.)*

### G1. Nhiệm vụ Bắt buộc (Weekly Mandatory)

- Môn chủ / người có quyền **tạo danh sách nhiệm vụ hàng tuần** từ template có sẵn (do gamedata design định nghĩa).
- **Config số nhiệm vụ tối thiểu** phải hoàn thành trong 1 tuần.
- Người xin gia nhập tông môn **thấy trước** bảo khố, phúc lợi hàng tuần, và số nhiệm vụ bắt buộc tối thiểu.
- **Template nhiệm vụ ví dụ:** khai thác X linh thạch, tinh luyện X nguyên liệu, luyện chế X lá phù / pháp bảo / trận pháp, canh gác / bảo vệ mỏ linh thạch trong X thời gian.
- Đệ tử nhận nhiệm vụ → cung cấp đầu vào nếu cần → tự động nhận đầu ra nếu có → **tự động done** khi hoàn thành điều kiện.
- Hoàn thành nhiệm vụ bắt buộc → nhận **phúc lợi hàng tuần** (trước khi bị kick hoặc trước đầu tuần tiếp theo nếu không bị kick).
- **Ràng buộc kick:** Không được kick đệ tử khi họ đang nhận nhiệm vụ bắt buộc (chưa hoàn thành).

### G2. Nhiệm vụ Tự Nguyện (Voluntary / "Tăng ca")

- Môn chủ / người có quyền tạo thêm nhiệm vụ ngoài quota bắt buộc, cũng từ template có sẵn.
- Treo trên **bảng nhiệm vụ tự nguyện** — ai muốn nhận thì nhận.
- Hoàn thành → **nhận thưởng ngay lập tức**.
- Phần thưởng bị **escrow** từ bảo khố khi tạo nhiệm vụ.
- Có thể hủy khi **chưa có ai nhận**.
- Nếu đã có người nhận: người nhận phải chủ động hủy trước → mới hủy được. Nếu không → phải đợi hết thời hạn hoàn thành mà người nhận chưa xong → mới hủy được.

### G3. Nhiệm Vụ Cá Nhân (Member-Posted)

- **Bất kỳ thành viên nào** trong tông môn đều có thể tạo nhiệm vụ + đặt phần thưởng riêng.
- Phần thưởng bị **lock ngay khi tạo** — khóa khỏi inventory người tạo, không ai (kể cả người tạo) đụng được vào trong thời gian nhiệm vụ còn hiệu lực.
- Có **thời hạn hoàn thành** cố định theo loại nhiệm vụ.
- Hủy: được hủy khi **chưa có ai nhận**. Nếu đã có người nhận → người nhận phải chủ động hủy trước → mới có thể hủy. Nếu không → đợi hết thời hạn mà người nhận chưa xong → mới hủy được.

### So sánh nhanh 3 dạng

| | Bắt buộc | Tự nguyện | Cá nhân |
|---|---|---|---|
| Người tạo | Môn chủ / quyền | Môn chủ / quyền | Bất kỳ thành viên |
| Bắt buộc nhận? | ✅ (có quota tối thiểu) | ❌ | ❌ |
| Nhận thưởng khi nào | Cuối tuần (phúc lợi) | Ngay khi xong | Ngay khi xong |
| Escrow thưởng? | ✅ | ✅ | ✅ |
| Tự động done? | ✅ | ✅ | ✅ |
| Hủy được không? | ✅ (tối đa 1 lần/ngày, NV về lại pool) | ✅ (nếu chưa có người nhận) | ✅ (nếu chưa có người nhận) |

---

## Cấu trúc các Sub-System cần bàn riêng

1. **Nhiệm vụ tông môn** *(đã capture sơ bộ ở mục G, cần tách note riêng)*
2. **Phúc lợi hàng tuần** — đã note ở mục H bên dưới
3. **Shop tông môn & kinh doanh bảo khố** — mua/bán vật phẩm với đệ tử / bên ngoài
4. **Khai thác mỏ linh thạch** — chiếm mỏ, phân công, chỉ tiêu, tranh giành
5. **Hoạt động bên ngoài tông môn** — PvP tông môn, chiếm mỏ, liên minh, v.v.
6. **Kế thừa môn chủ** — chi tiết UI chuyển giao tự nguyện

---

## Design Decisions

### Locked

- Tông môn yêu cầu bảo khố tối thiểu 10 linh thạch để thành lập và tồn tại.
- Bảo khố < ngưỡng → thông báo + đếm ngược 24h → giải tán nếu không bổ sung.
- Khi giải tán (chủ động hoặc tự động do bảo khố cạn): toàn bộ tài sản trả về môn chủ.
- Khi giải tán do không còn ai kế thừa (offline > 100 ngày): tài sản mất hết.
- Trưởng lão và phó tông chủ chỉ là danh hiệu, không có quyền mặc định. Quyền do môn chủ set thủ công.
- Môn chủ có toàn quyền, kể cả sa thải bất kỳ ai.
- Môn chủ có thể chuyển giao chức vụ tự nguyện.
- Chuỗi kế thừa tự động (offline > 100 ngày): phó tông chủ → người có quyền bảo khố (gia nhập sớm nhất) → thành viên gia nhập sớm nhất → giải tán (tài sản mất).
- Tối đa 200 thành viên.
- Không có phí duy trì định kỳ; chi phí vận hành = thưởng nhiệm vụ (có thể = 0).
- Nhiệm vụ bắt buộc không có thưởng riêng; thưởng là phúc lợi tuần (số cố định do môn chủ config, chỉ nhận khi đủ quota).
- Nhiệm vụ tự nguyện bắt buộc phải có phần thưởng.
- Phần thưởng nhiệm vụ (tự nguyện + cá nhân) bị escrow ngay khi tạo.
- Tự động done khi đủ điều kiện (tất cả 3 dạng).
- Pool bắt buộc reset cuối tuần. Tổng pool phải > quota/người × số đệ tử; môn chủ phải thêm nhiệm vụ khi có thêm đệ tử.
- Đệ tử được hủy nhiệm vụ bắt buộc tối đa 1 lần/ngày; NV về lại pool.
- Môn chủ có thể tạo từng hoặc hàng loạt nhiệm vụ (cả 2 loại bắt buộc và tự nguyện).
- Không được kick đệ tử khi họ đang nhận nhiệm vụ bắt buộc chưa hoàn thành.
- Hai kênh chat: all-member và private (add thủ công).
- Quyền được phân thủ công, không gắn với danh hiệu.
- 3 dạng nhiệm vụ: bắt buộc (môn chủ tạo, quota tuần, không có thưởng) / tự nguyện (môn chủ tạo, tự nhận, có thưởng) / cá nhân (thành viên tự tạo, có thưởng).

### Tentative

- Ngưỡng tồn tại 10 linh thạch — con số cần balance sau.
- ~~Người có quyền "Quản lý thành viên" có sa thải được nhau không?~~ → **Chốt: không. Chỉ môn chủ.**

---

## Related Systems

- `notes/mineral-vein-system.md` — chiếm mỏ, khai thác, tranh giành
- `notes/player-interaction-group.md` — PvP state, trade, chat
- `features/home-cave-defense.md` — có thể tương tác với hoạt động bên ngoài tông môn
- Quest system (chưa mở note)

---

## Open Questions

1. ~~Khi tông môn giải tán, tài sản xử lý như thế nào?~~ → **Chốt: trả về môn chủ** (trừ trường hợp không còn ai kế thừa → mất hết).
2. ~~Môn chủ có thể chuyển giao không?~~ → **Chốt: có.** Chuỗi kế thừa tự động đã định nghĩa. UI chuyển giao tự nguyện chưa bàn.
3. ~~Giới hạn thành viên?~~ → **Chốt: 200 người.**
4. ~~Phí duy trì định kỳ?~~ → **Chốt: không có.**
5. ~~Escrow thưởng nhiệm vụ?~~ → **Chốt: escrow ngay khi tạo.**
6. ~~Bước 2 kế thừa (nhiều người cùng quyền bảo khố)?~~ → **Chốt: chọn người gia nhập sớm nhất.**
7. ~~Phó tông chủ có quyền riêng gì không?~~ → **Chốt: chỉ là danh hiệu + ưu tiên kế thừa. Quyền do môn chủ set thủ công.**
8. ~~Người có quyền "Quản lý thành viên" có sa thải được nhau không?~~ → **Chốt: không. Chỉ môn chủ.**
9. ~~Phúc lợi hàng tuần tính như thế nào?~~ → **Chốt: số cố định do môn chủ config. Chỉ nhận khi đủ quota nhiệm vụ.**
10. ~~Nhiệm vụ bắt buộc có thể hủy không?~~ → **Chốt: được, tối đa 1 lần/ngày. NV về lại pool.**
11. ~~Auto-expire nhiệm vụ không ai nhận?~~ → **Chốt: pool bắt buộc reset cuối tuần. Pool tự nguyện / cá nhân chưa bàn.**
12. ~~Nhiệm vụ cá nhân — chỉ nội bộ hay ngoài tông môn?~~ → **Chốt: chỉ nội bộ.**

---

## Risks / Watchouts

- **Bảo khố bị rút cạn:** Môn chủ rút sát ngưỡng rồi bỏ đi → tông môn sụp trong 24h. Cần cân nhắc rate-limit rút hoặc cơ chế cảnh báo sớm hơn.
- **Quyền phân tán khó kiểm soát:** Nhiều người có quyền bảo khố mà không có audit log → dễ mất kiểm soát tài sản.
- **Nhiệm vụ chỉ tiêu cứng nhắc:** Chỉ tiêu khai thác tuần cần điều chỉnh được dễ dàng, không thì gây friction.
- **Escrow lock tài sản lâu:** Nếu nhiệm vụ không ai nhận và người tạo quên hủy → tài sản bị khóa vô thời hạn. Cần cơ chế auto-expire hoặc nhắc nhở.

---

## Promotion Checklist

- [ ] Core gameplay goal là rõ.
- [ ] Player-facing loop (thành lập → vận hành → phát triển → giải tán) đã đủ hình dạng.
- [ ] Tất cả sub-system lớn đã có note riêng hoặc được defer có lý do.
- [ ] Open questions về phúc lợi, quyền phó tông chủ đã được trả lời.
- [ ] Sẵn sàng nâng lên `features/`.
