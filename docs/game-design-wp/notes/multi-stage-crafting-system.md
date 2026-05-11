# Hệ Thống Luyện Chế Nhiều Tầng — Design Notes

**Ngày tạo:** 2026-05-11  
**Trạng thái:** Đã chốt concept core, còn data structure / balance / UX detail

---

## 1. Goal

Biến hệ luyện chế từ dạng **công thức phẳng dùng raw material trực tiếp** thành một hệ **nhiều tầng sản xuất**, nơi:

- nguyên liệu cấp thấp nhất chủ yếu là **base material**
- base material thường **không dùng trực tiếp được**
- người chơi phải luyện chế qua một hoặc nhiều tầng trung gian
- thành phẩm ở mỗi tầng có thể:
  - là **item sử dụng được ngay**
  - hoặc là **nguyên liệu cho tầng cao hơn**
- tất cả phụ thuộc vào **game data design**, không hard-code theo loại item

Mục tiêu là tạo progression rõ hơn cho economy, crafting loop, nhu cầu thị trường, và giá trị của item trung gian.

---

## 2. Vấn đề của mô hình cũ

Mô hình cũ đang ngầm đi theo hướng:

- recipe cần một danh sách material item
- mỗi material có số lượng cố định
- bấm luyện chế -> ra thành phẩm cuối

Vấn đề:

1. **Quá phẳng**
   - không có cảm giác chuỗi sản xuất

2. **Item trung gian không có vai trò**
   - kinh tế khó hình thành các tầng chuyên môn hóa

3. **Khó tạo chiều sâu nghề nghiệp**
   - người chơi ít có động lực farm/craft một nhánh riêng để bán cho người khác

4. **Game data bị nghèo**
   - item chỉ chia thành “raw material” và “thành phẩm”, thiếu lớp bán thành phẩm

---

## 3. Concept cốt lõi

Hệ luyện chế mới đi theo mô hình:

**Base Material -> Processed Component -> Advanced Component -> Final Product**

Tuy nhiên đây chỉ là **mẫu tham chiếu**, không phải rule cứng.

Rule thật nên là:
- một item có thể được gắn vai trò qua data là:
  - **raw/base material**
  - **processed item / component**
  - **final usable item**
  - hoặc **đa vai trò**
- cùng một item có thể:
  - dùng trực tiếp
  - đồng thời vẫn là nguyên liệu cho recipe khác

Ví dụ tư duy:
- Quặng thô -> Luyện thành Kim Phôi
- Kim Phôi -> dùng để rèn Pháp Khí cấp thấp
- Kim Phôi -> cũng có thể tiếp tục luyện thành Tinh Kim cao cấp
- Tinh Kim -> dùng cho Pháp Khí / Khôi Lỗi / Trận Nhãn / cấu kiện đặc biệt

---

## 4. Rule tổng quát của item trong hệ craft

### 4.1. Không phân loại cứng theo “có dùng được hay không” ở code

Không nên hard-code rằng:
- raw material thì luôn không dùng được
- component thì luôn không dùng được
- thành phẩm thì luôn dùng được

Thay vào đó, nên để **game data quyết định**:
- item này có thể dùng được không
- item này có thể là input cho recipe nào
- item này thuộc tier chế tác nào
- item này có bị consume khi dùng không

### 4.2. Base material mặc định nên không dùng trực tiếp

Về mặt design, mặc định nên ưu tiên:
- base material cấp thấp nhất **không dùng trực tiếp được**
- nó cần qua luyện chế để thành dạng usable hơn hoặc refined hơn

Lý do:
- tạo nhu cầu cho các công đoạn sơ chế
- khiến việc sở hữu thành phẩm có giá trị hơn
- giúp economy có nhiều tầng hàng hóa hơn

Nhưng đây vẫn là **default design direction**, không phải cấm tuyệt đối. Nếu có item đặc biệt, game data vẫn có thể cho phép ngoại lệ.

---

## 5. Cấu trúc tầng luyện chế

## Tầng 0 — Base Material

Là nguyên liệu khai thác / loot / thu thập trực tiếp từ thế giới.

Ví dụ:
- quặng thô
- gỗ linh
- da thú
- xương yêu thú
- thảo dược thô
- mảnh linh tinh / mảnh cấu kiện

Đặc điểm:
- chủ yếu là input cho recipe khác
- thường không có giá trị sử dụng trực tiếp
- có thể stack lớn
- là nền của economy farm

## Tầng 1 — Processed Component

Là vật phẩm đã qua một bước xử lý cơ bản.

Ví dụ:
- kim phôi
- linh dịch tinh luyện
- da thuộc
- mộc phôi
- lõi cấu kiện sơ cấp

Đặc điểm:
- có thể là nguyên liệu chính cho nhiều nhánh recipe
- một số item tầng này đã có thể dùng được
- đây là lớp rất quan trọng để tạo thị trường bán thành phẩm

## Tầng 2 — Advanced Component

Là linh kiện / vật liệu tinh luyện cao hơn, thường dùng cho đồ tốt hoặc hệ đặc thù.

Ví dụ:
- tinh kim
- trận hạch
- phù phôi cao cấp
- lõi khôi lỗi
- mạch dẫn linh lực

Đặc điểm:
- thường yêu cầu recipe phức tạp hơn
- có thể phụ thuộc nhiều hệ khác nhau
- tạo độ giao thoa giữa các ngành craft

## Tầng 3 — Final Product

Là thành phẩm player dùng trực tiếp hoặc triển khai trực tiếp.

Ví dụ:
- đan dược
- pháp khí
- phù lục
- trận pháp
- khôi lỗi hoàn chỉnh
- module nâng cấp khôi lỗi

Đặc điểm:
- có thể dùng ngay
- nhưng một số “final product” cấp thấp vẫn có thể là nguyên liệu cho đồ cao hơn

=> Quan trọng: **“Final ở recipe này” không có nghĩa là “không thể làm input cho recipe khác”.**

---

## 6. Nguyên tắc data design

### 6.1. Recipe graph thay vì recipe phẳng

Về mặt design, toàn bộ crafting nên được hiểu như một **recipe graph**:
- item A tạo từ B + C
- item A lại là input của D
- item D lại là input của E

Tức là cấu trúc nên là **đồ thị phụ thuộc recipe**, không phải từng recipe rời rạc không liên quan.

### 6.2. Vai trò item là data-driven

Mỗi item nên được data hóa để biết:
- có thể dùng trực tiếp không
- có thể làm material không
- thuộc nhóm craft nào
- thuộc tier nào
- có thể là input cho bao nhiêu recipe

### 6.3. Một item có thể phục vụ nhiều ngành

Đây là chỗ rất quan trọng.

Ví dụ:
- **Tinh Kim** có thể dùng cho:
  - pháp khí
  - trận nhãn
  - khôi lỗi
- **Linh Dịch Tinh Luyện** có thể dùng cho:
  - đan dược cao cấp
  - lõi năng lượng khôi lỗi
  - vật liệu kích hoạt trận pháp

Nếu làm được vậy, economy sẽ sống hơn rất nhiều vì cùng một node trong chuỗi sản xuất có nhiều đầu ra tiêu thụ.

---

## 7. Quan hệ với các hệ đã có

### Đan dược
- Có thể vẫn giữ flow hiện tại ở bản đơn giản
- Nhưng về lâu dài cũng nên cho phép một phần nguyên liệu đan dược là item trung gian thay vì chỉ raw herb

### Pháp khí
- Là hệ rất hợp để dùng nhiều tầng vật liệu
- Ví dụ: quặng thô -> kim phôi -> tinh kim -> pháp khí

### Phù lục
- Có thể có lớp vật liệu trung gian như:
  - giấy phù sơ chế
  - mực linh
  - hồn ấn

### Trận pháp
- Có thể có lớp component như:
  - trận cơ
  - trận hạch
  - mạch dẫn linh lực

### Khôi lỗi / Machine System
- Là hệ gần như bắt buộc nên dùng mô hình nhiều tầng
- Vì bản chất khôi lỗi hợp với cấu kiện, lõi, module, khung máy...

---

## 8. Trải nghiệm người chơi mong muốn

Hệ này nên tạo cảm giác:

1. **Tiến trình chế tác có lớp lang**
   - không phải nhặt vài món raw rồi bấm ra đồ mạnh ngay

2. **Có nghề phụ / công đoạn phụ để kiếm sống**
   - người chơi có thể chuyên bán component trung gian

3. **Item trung gian có giá trị thị trường thật**
   - không chỉ là rác chờ ghép tiếp

4. **Một nguyên liệu tốt có thể mở nhiều hướng chế tác**
   - tăng cảm giác quyết định và chiến lược

---

## 9. Rule nên giữ đơn giản ở phase đầu

Để tránh hệ quá nặng ngay từ đầu, nên giữ một số rule đơn giản:

- chưa cần mastery riêng cho từng công đoạn
- chưa cần durability cho component
- chưa cần quality random ở mọi tầng
- trước mắt chỉ cần:
  - recipe nhiều tầng
  - item trung gian làm input cho recipe khác
  - một số recipe có thể cho ra item usable ngay

Tức là chiều sâu đến từ **chuỗi sản xuất**, chưa cần dồn hết vào simulator crafting.

---

## 10. Rủi ro design

### Rủi ro 1 — Quá rườm rà
Nếu mọi thứ đều bắt qua 4-5 tầng, người chơi sẽ mệt.

=> Nên có rule thực dụng:
- đồ phổ thông: ít tầng hơn
- đồ hiếm / đồ mạnh / hệ đặc thù: nhiều tầng hơn

### Rủi ro 2 — Inventory overload
Nhiều component sẽ làm balo rác và khó đọc.

=> Sau này cần bàn thêm:
- stack limit
- nhóm tab item
- search/filter
- có cần kho nguyên liệu riêng không

### Rủi ro 3 — Kinh tế bị nghẽn ở node bắt buộc
Nếu một component trung gian quá quan trọng mà nguồn quá hiếm, toàn bộ chuỗi sản xuất bị bóp cổ chai.

=> Phase data balance phải rất cẩn thận ở các node dùng chung.

---

## 11. Kết luận tạm chốt

### Đã chốt
- Hệ luyện chế nên chuyển sang **mô hình nhiều tầng**
- Base material cấp thấp nhất **thường không dùng trực tiếp**
- Item tạo ra ở mỗi tầng có thể:
  - sử dụng được
  - hoặc làm nguyên liệu cho tầng cao hơn
  - hoặc vừa dùng được vừa làm nguyên liệu
- Tất cả nên là **data-driven**, không hard-code cứng theo loại item
- Crafting nên được tư duy như một **recipe graph** liên kết nhiều ngành với nhau

### Chưa chốt
- Tên gọi cụ thể của từng tier trong game
- Mức độ nhiều tầng tối đa cho từng ngành craft
- Có cần kho nguyên liệu riêng / UI riêng không
- Cách migrate từ hệ recipe hiện tại sang hệ mới trong data

---

## 12. Recommended next step

Khi quay lại hệ này, nên bàn tiếp theo thứ tự:
1. taxonomy item/recipe cho hệ craft nhiều tầng
2. ví dụ chuỗi sản xuất cụ thể cho 2-3 ngành
3. UI recipe / hiển thị dependency chain
4. cách Khôi Lỗi dùng component nhiều tầng ra sao
