# Game Design Luyện Đan

## 1. Mục tiêu UX

UI luyện đan được chia làm 2 cột:

- Cột trái là danh sách các đan phương đã học.
- Cột phải là khu vực thao tác luyện chế.

Người chơi kéo đan phương từ cột trái sang ô đan phương ở cột phải. Đan phương là template vĩnh viễn, không tiêu hao khi dùng để luyện chế.

## 2. Tooltip đan phương

Khi hover hoặc click vào đan phương, tooltip phải hiển thị:

- Tên đan phương
- Mô tả công dụng
- Danh sách nguyên liệu
- Thời gian luyện chế
- Tỉ lệ thành công

Tooltip nguyên liệu trong panel luyện đan cũng dùng cùng dữ liệu recipe này, nhưng phần danh sách nguyên liệu phải thể hiện tình trạng hiện tại kiểu:

- `Kim linh thảo 1/1`
- `Hỏa nhan thảo 0/2`

## 3. Flow thao tác trong panel

### 3.1. Ô đan phương

- Chỉ nhận các đan phương đã học.
- Kéo xong thì danh sách bên trái vẫn còn nguyên.
- Có thể kéo đan phương khác vào để thay thế khi chưa bắt đầu luyện chế.
- Có thể kéo đan phương từ ô này trả ngược về danh sách để xóa lựa chọn khi chưa bắt đầu luyện chế.
- Khi đã bắt đầu luyện chế thì ô này bị khóa, không cho thay đổi cho tới khi phiên luyện kết thúc hoặc bị hủy.

### 3.2. Ô nguyên liệu

- Phase UI hiện tại dùng `1 ô nguyên liệu tổng hợp`.
- Ô này nhận mọi nguyên liệu hợp lệ của recipe đang được đặt trong ô đan phương.
- Fill thể hiện tiến độ tổng của toàn recipe từ `0%` tới `100%`.
- Click ô nguyên liệu sẽ mở tooltip recipe để xem chi tiết danh sách nguyên liệu và tình trạng hiện tại.
- Tooltip là nơi thể hiện rõ phần nào đủ, phần nào thiếu.

### 3.3. Nút luyện chế

- Chỉ sáng khi recipe hợp lệ và server preview trả về `CanCraft = true`.
- Khi bấm, server consume nguyên liệu ngay lập tức.
- Sau khi server chấp nhận bắt đầu luyện chế:
  - nguyên liệu biến mất khỏi inventory
  - recipe và ingredient slots bị khóa
  - nhân vật vào trạng thái `Practicing`
  - không được di chuyển
  - không được luyện khí, tu luyện, luyện bùa, luyện khí cụ song song

### 3.4. Trong lúc luyện chế

- Có time bar đếm ngược theo giây.
- Có thể `Tạm dừng` và `Tiếp tục`.
- Có thể `Hủy bỏ` chỉ khi progress chưa chạm `80%`.
- Khi progress đạt từ `80%` trở lên thì không thể hủy.

Nếu người chơi đóng panel trong lúc đang luyện:

- session vẫn chạy hoặc vẫn paused ở server
- mở lại panel phải khôi phục đúng recipe đang luyện, nguyên liệu đã tiêu hao và thời gian còn lại

Nếu người chơi đóng panel khi chưa bấm luyện:

- draft hiện tại chỉ là state client-side
- có thể reset về trạng thái đầu khi mở lại panel

## 4. Quy tắc random kết quả

- Server không roll success/fail ở thời điểm bấm `Luyện chế`.
- Server chỉ roll ở thời điểm thời gian luyện hoàn tất.
- Tỉ lệ thành công cuối cùng dùng dữ liệu authoritative phía server ở thời điểm kết thúc.
- Kết quả hiện tại đang là:
  - thành công: nhận 1 pill kết quả
  - thất bại: không nhận pill

## 5. Popup kết quả

Khi server có kết quả luyện chế:

- nếu người chơi đang online và đang mở panel phù hợp thì nhận packet kết quả và hiện popup ngay
- nếu người chơi đang online nhưng chưa mở panel phù hợp thì packet vẫn được cache ở client state
- nếu người chơi offline, server sẽ push packet ở lần đăng nhập gần nhất

Popup kết quả:

- chỉ hiển thị đúng 1 lần
- phải có icon vật phẩm chính nếu thành công
- nếu thất bại thì hiện icon hỏng/fallback icon
- text ví dụ:
  - `Luyện chế đan dược thành công`
  - `Luyện chế đan dược thất bại`

Sau khi người chơi bấm `OK`:

- client gửi acknowledge packet
- server đánh dấu đã xem
- lần sau không hiện lại popup đó nữa

## 6. Rule gameplay nền

- Mọi hình thức tu luyện dài hạn đều dùng chung `generic long-running practice system`
- Luyện đan chỉ được bắt đầu tại `private home map`
- Đơn vị thời gian chuẩn là `giây`
- Luyện đan, tu luyện, luyện bùa, luyện khí cụ đều bị xem là cùng nhóm hành động `practice`

## 7. Trạng thái hệ thống hiện tại

### 7.1. DB và server đã có

- `pill_recipe_templates.craft_duration_seconds`
- `player_practice_sessions`
- generic `PracticeService`
- `AlchemyPracticeService`
- packet start / preview / load status / pause / resume / cancel / acknowledge result
- push `PracticeCompletedPacket`
- đồng bộ `CharacterCurrentState = Practicing`

### 7.2. Client runtime đã có

- `ClientAlchemyService`
- `ClientAlchemyState`
- load learned recipes
- load recipe detail
- preview craft
- start craft practice
- pause / resume / cancel
- pending result + acknowledge result

### 7.3. UI phase này đã có script

- recipe list drag/drop
- recipe slot drag/drop
- ingredient aggregate slot
- recipe tooltip
- inventory grid reuse để kéo nguyên liệu
- time bar / countdown / trạng thái practice
- popup kết quả một lần

## 8. Giới hạn hiện tại

Các phần sau chưa làm sâu ở phase này:

- `mutation_rate` mới ảnh hưởng rate preview, chưa tạo nhánh loot đột biến riêng
- `required_herb_maturity` vẫn chưa mở
- advanced pill effects ngoài `RecoverHp` và `RecoverMp` chưa execute đầy đủ
- draft nguyên liệu trước khi bấm luyện hiện là state client-side, chưa lưu server-side

## 9. Script/UI mapping hiện tại

### 9.1. Script chính

- `WorldAlchemyPanelController`
  - controller panel luyện đan
  - bind recipe list, recipe slot, ingredient slots, inventory grid, preview, time bar, buttons

- `AlchemyPracticeResultInboxController`
  - theo dõi pending result
  - mở popup đúng 1 lần khi một root UI đủ điều kiện đang visible

### 9.2. View phụ

- `AlchemyRecipeListView`
- `AlchemyRecipeListItemView`
- `AlchemyRecipeSlotView`
- `AlchemyIngredientSlotView`
  - ở phase hiện tại là ô tổng hợp duy nhất, không còn 1 slot riêng cho từng input
- `AlchemyRecipeTooltipView`
- `AlchemyPracticeResultPopupView`

### 9.3. View có sẵn được reuse

- `InventoryItemGridView`
- `InventoryItemSlotView`
- `InventoryItemTooltipView`
- `InventoryItemPresentationCatalog`

## 10. Cách controller hiểu nguyên liệu

- Với item non-stackable:
  - client phải giữ `playerItemId` cụ thể để gửi sang server
- Với item stackable:
  - server tự allocate theo `item_template_id` trong inventory
  - client chỉ cần coi slot đã được "arm" và hiển thị progress theo tổng số lượng đang có

## 11. Điều kiện khóa thao tác

Khi có `CurrentPracticeSession` loại `Alchemy` ở trạng thái:

- `Active`
- `Paused`

thì panel phải khóa:

- thay recipe
- thay ingredient
- bấm craft lần nữa

Chỉ còn các hành động:

- `Pause`
- `Resume`
- `Cancel` nếu `CanCancel = true`
