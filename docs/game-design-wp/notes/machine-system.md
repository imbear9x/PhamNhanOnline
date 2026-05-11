# Machine System / Khôi Lỗi — Design Notes

**Ngày tạo:** 2026-05-11  
**Trạng thái:** Đã chốt concept core, còn runtime / balance / progression detail

---

## 1. Goal

Tạo một hệ **Khôi Lỗi / Machine System** là nhánh companion riêng, gần với **Linh Thú** ở vai trò chiến đấu/hỗ trợ, nhưng khác rõ về:

- **nguồn gốc**: không nở trứng, mà phải **luyện chế**
- **tài nguyên vận hành**: không dùng mana/thần thức như pet, mà chạy bằng **linh thạch nạp vào**
- **cảm giác fantasy**: thiên về cơ quan, cấu kiện, lõi máy, module, khung thể

Hệ này nên trở thành một nhánh build/collect/crafting riêng, bổ sung cho fantasy tu tiên thay vì đụng vai trò quá nhiều với Linh Thú.

---

## 2. Bản chất hệ thống

Khôi lỗi là một **thực thể đồng minh** của người chơi, tương tự pet ở lớp runtime/combat cơ bản:
- có entity thật trong game
- có stat
- có target
- có skill/behavior
- có thể được triệu hồi / mang theo / tham chiến
- có thể bị đánh, bị phá hủy hoặc ngừng hoạt động

Nhưng Khôi Lỗi khác Linh Thú ở bản chất cốt lõi:
- là **đồ máy / cơ quan luyện chế ra**, không phải sinh vật sống
- **không có thần thức**
- **không có cơ duyên**
- năng lượng hoạt động đến từ **linh thạch người chơi cấp cho nó**

---

## 3. Vai trò gameplay

Khôi lỗi nên đi theo 3 hướng chính:

1. **Combat companion**
   - đánh cùng người chơi
   - đỡ đòn / gây damage / hỗ trợ chiến thuật

2. **Utility machine**
   - nhặt đồ
   - gác nhà
   - hỗ trợ khai thác / vận chuyển / kích hoạt cơ quan (nếu sau này mở rộng)

3. **Craft-driven power fantasy**
   - cho cảm giác “người chơi tự chế tạo đồng minh cơ khí của mình”
   - khác fantasy thuần nuôi pet

---

## 4. Nguồn gốc và cách sở hữu

### Nguồn gốc chính
Khôi lỗi có được chủ yếu bằng **luyện chế**.

Flow tư duy:
- thu thập base material / component
- luyện thành các bộ phận trung gian
- ghép thành Khôi Lỗi hoàn chỉnh

Điều này khiến Machine System gắn rất chặt với **hệ luyện chế nhiều tầng**.

### Nguồn phụ có thể có sau này
- quest reward
- boss drop component / blueprint
- event reward
- NPC bán blueprint cấp thấp

Nhưng bản chất chính vẫn là **craft ra**, không phải bắt ngoài map hay ấp trứng.

---

## 5. Chỉ số của Khôi Lỗi

Khôi lỗi có các chỉ số chính:
- **HP**
- **Năng lượng**
- **ATK**
- **Speed**

### Không có
- **Thần Thức**
- **Cơ Duyên**

### Ghi chú về Năng lượng
Năng lượng của Khôi Lỗi **không giống mana của player/pet**.

Tư duy đúng nên là:
- Khôi Lỗi vận hành bằng **linh thạch nạp vào**
- linh thạch được chuyển thành **energy reserve** của nó
- khi chiến đấu / hoạt động / duy trì tồn tại, nó tiêu hao energy này

Tức là với Machine System, “mana” chỉ nên hiểu là một thanh **năng lượng vận hành**, chứ fantasy thật là **linh thạch làm nhiên liệu**.

---

## 6. Cơ chế năng lượng

## 6.1. Nguồn năng lượng
- Người chơi phải **nạp linh thạch** vào Khôi Lỗi
- Linh thạch trở thành **dung lượng năng lượng hiện tại** của khôi lỗi
- Không có linh thạch / hết năng lượng -> khôi lỗi không hoạt động được

## 6.2. Cách tiêu hao
Năng lượng bị tiêu hao bởi:
- được triệu hồi ra ngoài
- di chuyển / duy trì hoạt động
- dùng skill
- kích hoạt behavior đặc biệt

Có thể chia mức tiêu hao như sau:
- **Duy trì tồn tại**: hao nền theo thời gian
- **Combat/action**: hao thêm khi tấn công, cast skill, chạy behavior mạnh

## 6.3. Trạng thái khi hết năng lượng
Khi cạn năng lượng, nên ưu tiên hướng:
- Khôi Lỗi **ngừng hoạt động**
- không chết thật theo nghĩa sinh vật
- hoặc tự thu hồi về túi / kho khôi lỗi

Điều này hợp fantasy máy móc hơn là “chết” như pet.

---

## 7. Quan hệ với Linh Thú

Khôi Lỗi nên **gần giống Linh Thú ở runtime layer**, nhưng khác ở fantasy và resource model.

### Giống nhau
- đều là đồng minh có entity thật
- đều có thể theo người chơi và tham chiến
- đều có AI/behavior
- đều có thể gác nhà
- đều có thể bị vô hiệu hóa trong combat

### Khác nhau
| Trục | Linh Thú | Khôi Lỗi |
|---|---|---|
| Nguồn có được | Trứng / thu phục / quest | Luyện chế |
| Bản chất | Sinh vật sống | Máy / cơ quan |
| Tài nguyên vận hành | Mana pet + thần thức của chủ | Linh thạch nạp vào |
| Thần thức | Có | Không |
| Cơ duyên | Có thể có trong hệ sinh vật/player-facing | Không |
| Sinh sản | Có | Không |
| Fantasy tăng trưởng | Nuôi, tu luyện, tiến hóa | Chế tạo, lắp ráp, nâng cấp |

### Ý nghĩa design
Nếu Linh Thú là nhánh “nuôi dưỡng sinh linh”, thì Khôi Lỗi là nhánh “công nghệ luyện khí / cơ quan thuật”.

Cả hai nên cùng tồn tại mà không bị trùng fantasy.

---

## 8. Crafting của Khôi Lỗi

Khôi Lỗi nên dùng logic gần với **luyện chế Pháp Khí / Trận Pháp** hơn là logic pet.

### Cấu trúc recipe dự kiến
Một khôi lỗi hoàn chỉnh có thể được tạo từ nhiều bộ phận như:
- **Khung khôi lỗi**
- **Lõi khôi lỗi**
- **Mạch dẫn linh lực**
- **Bộ phận vũ trang / công cụ**
- **Ấn điều khiển / module điều khiển**

Các bộ phận này bản thân cũng nên là **item trung gian nhiều tầng**.

### Hướng data design
- blueprint xác định loại khôi lỗi sẽ tạo
- material/component quyết định tỉ lệ thành công và quality outcome
- một số stats có thể tăng theo chất lượng vật liệu giống logic trận pháp/pháp khí

Ví dụ:
- vật liệu tốt hơn -> HP cao hơn
- lõi tốt hơn -> energy capacity cao hơn
- module tốt hơn -> behavior/skill tốt hơn
- cấu kiện nhẹ hơn -> speed cao hơn

---

## 9. Cấu trúc build của Khôi Lỗi

Có 2 hướng thiết kế, cần chọn về sau:

### Hướng A — Khôi lỗi là mẫu hoàn chỉnh cố định
- Mỗi công thức tạo ra 1 loại khôi lỗi gần như hoàn chỉnh
- Bộ skill / behavior gần như cố định
- Dễ làm, dễ balance

### Hướng B — Khôi lỗi có module/bộ phận thay thế
- Khung, lõi, module, vũ trang là các phần có thể thay thế
- Cùng một khung có thể build ra hướng khác nhau
- Sâu hơn, fantasy mạnh hơn, nhưng complexity tăng mạnh

### Recommendation hiện tại
Nên bắt đầu bằng **Hướng A**:
- recipe ra một mẫu khôi lỗi tương đối cố định
- cho phép khác nhau chủ yếu ở quality và tier
- module hóa sâu để bàn sau

Lý do:
- tránh đụng quá sớm vào bài toán custom build phức tạp
- vẫn giữ được fantasy luyện chế khôi lỗi

---

## 10. Triệu hồi và mang theo

Khôi lỗi nên có flow gần pet:
- nằm trong túi / kho khôi lỗi của người chơi
- người chơi triệu hồi ra khi cần
- khi không có mục tiêu thì follow player

### Giới hạn số lượng
Tạm thời nên đi theo hướng giống pet để dễ giữ balance:
- tại một thời điểm chỉ triệu hồi tối đa **2 companion tổng cộng**
- companion ở đây có thể bao gồm cả Linh Thú và Khôi Lỗi

Ví dụ:
- 2 linh thú
- hoặc 1 linh thú + 1 khôi lỗi
- hoặc 2 khôi lỗi

### Lý do
Nếu tách trần riêng, player rất dễ spam quá nhiều entity và map sẽ loạn.

---

## 11. Combat / Behavior

Khôi lỗi có thể có:
- attack cơ bản
- skill riêng
- behavior/AI ưu tiên mục tiêu

### Hướng behavior
Khá giống pet:
1. đánh kẻ gần nhất đang tấn công chủ
2. đánh kẻ gần nhất đang tấn công chính nó
3. đánh mục tiêu mà chủ đang target

### Fantasy hành vi
Khôi lỗi nên thiên về:
- ổn định
- máy móc
- đúng chức năng
- ít “thông minh hữu cơ” hơn pet

Ví dụ:
- thiên về guard/auto routine hơn là hành vi ngẫu hứng

---

## 12. Bị phá hủy / ngừng hoạt động

Do không phải sinh vật sống, khôi lỗi không nên dùng ngôn ngữ “chết” giống pet.

Khi HP về 0 hoặc energy cạn:
- Khôi Lỗi **bị phá hủy tạm thời** hoặc **ngừng hoạt động**
- trở về kho / túi khôi lỗi
- cần thời gian sửa chữa / hồi phục / nạp lại trước khi dùng lại

### Giá phải trả khi bị hạ
Có thể đi theo hướng:
- cooldown sửa chữa
- tốn thêm linh thạch để kích hoạt lại
- tốn linh kiện sửa chữa ở tier cao

### Recommendation hiện tại
Phase đầu nên giữ đơn giản:
- bị hạ -> về túi
- có cooldown hồi phục/sửa chữa
- có thể cần nạp lại linh thạch trước khi gọi ra tiếp

---

## 13. Bảo vệ động phủ / utility

Khôi lỗi rất hợp fantasy **thủ nhà**.

Các vai trò hợp lý:
- khôi lỗi gác cổng
- khôi lỗi bắn xa / phòng thủ cố định
- khôi lỗi tuần tra cơ bản
- khôi lỗi hỗ trợ nhặt đồ / vận chuyển nhẹ (nếu mở rộng sau)

### Trong động phủ
- có thể được đặt làm **đơn vị phòng thủ** giống pet thủ nhà
- khi động phủ bị công, khôi lỗi xuất hiện như một phần của hệ phòng thủ
- nếu bị phá trong lúc thủ nhà -> trở về trạng thái hỏng/ngừng hoạt động của chủ

---

## 14. Ràng buộc design quan trọng

### 14.1. Không được đè vai trò của Linh Thú
Nếu Khôi Lỗi làm được mọi thứ pet làm nhưng còn rẻ hơn/bền hơn/dễ điều khiển hơn, pet sẽ mất chỗ đứng.

=> Cần tách fantasy và tách lợi thế:
- pet mạnh ở sinh tồn, tự nhiên, có thần thức, có tăng trưởng sinh học
- khôi lỗi mạnh ở ổn định, thủ nhà, cấu hình chế tạo, dùng nhiên liệu

### 14.2. Không biến linh thạch thành chi phí quá khó chịu
Nếu khôi lỗi ăn linh thạch quá gắt mỗi phút, player sẽ ngại dùng.

=> Nên để chi phí đủ có ý nghĩa nhưng không làm người chơi thấy “gọi ra là lỗ”.

### 14.3. Không để recipe quá nặng ở V1
Khôi lỗi mà bắt qua quá nhiều tầng ngay từ đầu sẽ mệt.

=> Nên có:
- mẫu khôi lỗi sơ cấp craft tương đối đơn giản
- mẫu cao cấp mới cần nhiều component tầng sâu

---

## 15. Kết luận tạm chốt

### Đã chốt
- Khôi Lỗi / Machine System là một nhánh companion riêng
- Nguồn chính để có khôi lỗi là **luyện chế**, không phải trứng/thu phục
- Khôi lỗi có stat như: **HP, Năng lượng, ATK, Speed**
- Khôi lỗi **không có Thần Thức và không có Cơ Duyên**
- Năng lượng của khôi lỗi đến từ **linh thạch người chơi nạp vào**
- Hệ luyện chế của khôi lỗi nên gần với **Pháp Khí / Trận Pháp**
- Runtime/combat layer có thể tái dùng nhiều tư duy từ Linh Thú

### Chưa chốt
- Có dùng blueprint bắt buộc hay không
- Có custom module sâu hay chỉ recipe cố định
- Rule sửa chữa chi tiết khi khôi lỗi bị hạ
- Có chia loại khôi lỗi theo role rõ ràng hay để bộ skill quyết định
- UI kho khôi lỗi / nạp năng lượng / bảo trì

---

## 16. Recommended next step

Khi quay lại hệ này, nên bàn theo thứ tự:
1. taxonomy loại khôi lỗi cơ bản
2. chuỗi recipe nhiều tầng để tạo 1 khôi lỗi mẫu
3. rule nạp linh thạch -> đổi ra energy như thế nào
4. quan hệ slot/giới hạn companion giữa Linh Thú và Khôi Lỗi
5. vai trò thủ nhà / utility cụ thể
