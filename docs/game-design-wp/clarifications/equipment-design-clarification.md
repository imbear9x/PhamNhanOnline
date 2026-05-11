# Equipment Design Clarification

## Intended player-facing behavior

- Người chơi có một số lượng **slot trang bị cố định** để mặc đồ lên nhân vật.
- Mỗi slot chứa đúng một trang bị; khi đổi sang trang bị mới, đồ cũ tự tháo ra.
- Trang bị trên người cộng thêm chỉ số vào final stats của nhân vật.
- Một số trang bị đặc biệt còn mở thêm skill khi được mặc vào.
- Khi tháo trang bị, stat modifier bị xóa và skill được grant từ trang bị đó cũng bị thu hồi.
- Thay đổi trang bị nên phản ánh ngay lập tức lên chỉ số nhân vật và danh sách skill sở hữu.

## Intended terminology

- **Equipment Slot**: ô trang bị trên nhân vật
- **Equipped Item**: trang bị đang được mặc vào một slot
- **Equipment Stat Modifier**: lượng stat bonus được cộng thêm từ trang bị vào final stats
- **Equipment Bonus Row**: dữ liệu bonus bổ sung được persist riêng cho một trang bị cụ thể của player (ví dụ: enchant/nâng cấp)
- **Equipment-Granted Skill**: skill được mở từ việc mặc trang bị, thu hồi khi tháo ra
- **Slot Count**: số slot trang bị tối đa, config-driven

## Intended rules

- Số slot trang bị là config-driven, không hard-code.
- Chỉ item có định nghĩa là `Equipment` mới được mặc vào slot trang bị.
- Mỗi slot chứa tối đa 1 trang bị; thay vào là tự động tháo cái cũ.
- Sau bất kỳ thay đổi trang bị nào, 3 việc phải xảy ra theo thứ tự:
  1. sync lại skill được grant từ trang bị
  2. recompute final stats
  3. trả về inventory mới nhất cho client
- Nếu trang bị grant skill nhưng player chưa đủ điều kiện cảnh giới, skill không được cho vào loadout.
- Stat bonus từ trang bị đến từ 2 nguồn:
  - base modifier từ item definition
  - persisted bonus row riêng của player (nâng cấp/enchant)
- Khi unequip, skill grant từ trang bị đó bị xóa khỏi owned skills và loadout.

## Acceptable current behavior

- Slot count config-driven với default 4 là chấp nhận được ở thời điểm hiện tại.
- Slot agnostic (không phân loại slot theo loại trang bị) là behavior hiện tại có thể chấp nhận tạm thời nếu game design chưa yêu cầu slot typing.
- Khi mặc vào slot đang có đồ, tự unequip đồ cũ rồi equip đồ mới là chấp nhận được.
- 3 bước downstream (skill sync, stat recompute, inventory fetch) sau equip/unequip là đúng.
- Stat aggregation bỏ qua hàng lỗi/thiếu thay vì fail toàn bộ recompute là behavior an toàn.
- Equipment-granted skill sync xóa stale rows khi equipment thay đổi là đúng hướng.

## Mismatch vs current code

- **Không có slot typing**: runtime hiện không có rule nào map loại trang bị (vũ khí, giáp, phụ kiện…) sang slot cụ thể. Mọi item equipment đều có thể vào bất kỳ slot nào. Nếu game design có ý định phân slot theo loại trang bị, đây là gap lớn cần design decision rõ ràng.
- **`ValidateEquipAsync` tạo side effect**: phương thức validate tự tạo `PlayerEquipmentEntity` row nếu chưa có, trong khi tên gợi ý đây chỉ là validation. Behavior này không phải mismatch gameplay, nhưng canonical docs cần ghi rõ để tránh hiểu nhầm.
- **Unequip conflict resolution là silent replacement**: không có warning hay confirmation path khi equip đè lên slot đang có đồ. Về UX, đây có thể cần design decision xem có muốn báo hiệu hay confirm gì không.
- **Stat drift im lặng khi persistence inconsistent**: nếu một equipment row có itemId bị missing, stat bonus bị bỏ qua thay vì reported. Player không có cách biết rằng stat mình đang thấy có thể thấp hơn thực tế.
- **Equipment stat effect sống trong `EquipmentActionService`**: downstream recompute chỉ xảy ra qua action service. Nếu bypass action service bằng cách khác, stat và skill có thể lệch.

## Unresolved design questions

- Có muốn **slot typing** không? Tức là slot 1 chỉ cho phép vũ khí, slot 2 chỉ cho giáp... hay tất cả slot đều generic?
- Có bao nhiêu loại trang bị mà game sẽ hỗ trợ trong V1?
- **Equipment Bonus Row** (phần stat nâng thêm lưu riêng) tương ứng với cơ chế gameplay gì? Nâng cấp? Enchant? Khắc chữ? Hay cái gì khác?
- Khi unequip, slot có trở về trạng thái "trống" rõ ràng cho người chơi thấy không, hay chỉ phản ánh qua inventory?
- Trang bị grant skill với điều kiện cảnh giới — điều kiện này được config ở đâu và được apply bởi ai?
- Có cần rule giới hạn một nhân vật mặc được tối đa bao nhiêu trang bị cùng grant cùng một skill không?
- Nếu tháo trang bị khi skill đó đang ở trong loadout active, cần UI/warning gì cho người chơi?

## Canonicalization recommendation

- Canonicalize equipment thành 2 doc:
  1. **equipment equip/unequip runtime flow**: slot rule, equip/unequip logic, downstream sync order
  2. **equipment-to-stats-and-skills link**: cách stat modifier và skill grant hoạt động, 2 nguồn stat bonus
- Đánh dấu **slot typing** là `needs design decision` — nếu intent là generic slot, ghi rõ explicit trong canonical doc để không bị hiểu nhầm là oversight.
- Ghi rõ rằng `Equipment Bonus Row` là dữ liệu per-player, không phải item definition — canonical doc cần nói rõ nghĩa gameplay của nó là gì một khi có quyết định.
- Ghi rõ silent replacement khi equip đè slot là **current behavior**, không nhất thiết là **final UX intent**.
- Nối sang `player-stats-design-clarification.md` và `skill-design-clarification.md` vì equipment là điểm giao giữa stat domain và skill domain.
