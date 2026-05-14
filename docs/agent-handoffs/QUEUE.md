# Handoff Queue

File này là bảng nhìn nhanh để biết hiện có handoff nào đang sẵn sàng cho agent khác nhận.

Không dùng file này để thay thế nội dung chi tiết của handoff.
Chi tiết luôn nằm trong từng file ở `active/`.

## Quy tắc

- Chỉ đưa vào đây các handoff đã đủ rõ để làm thật.
- Nếu ý tưởng còn đang bàn và chưa chốt, giữ nó ở `docs/game-design-wp/`, không đưa vào queue.
- Nếu có nhiều handoff `Ready`, `dev` không tự đoán ưu tiên khi user chưa chốt thứ tự.
- Agent không tự poll hoặc auto-claim queue theo timer. Chỉ check và nhận handoff khi user dispatch thủ công.
- Khi user nói `check có handoff không`, agent chỉ xem các dòng `Ready` có `Owner` đúng vai trò của mình.
- Nếu có nhiều dòng `Ready` cùng owner, agent hỏi user chọn ưu tiên.
- Khi xong việc hoặc hủy việc, cập nhật trạng thái ở đây và trong file handoff gốc.

## Status

- `Ready`
  - đã đủ rõ để giao cho agent khác làm
- `In Progress`
  - đã có agent nhận và đang làm
- `Blocked`
  - đang bị chặn bởi quyết định, dependency, hoặc xác nhận từ user
- `Done`
  - đã xong, chờ archive hoặc đã archive

## Queue

| Priority | Status | Handoff | Owner | Source Design Doc | TechDesign Spec | Notes |
|---|---|---|---|---|---|---|
| 1 | Done | [herb-farming-system-techdesign](active/20260514-herb-farming-system-techdesign.md) | techdesign | `requirements/herb-farming-system.md` | `tech-design/herb-farming-system.md` | TD spec complete |
| 2 | Ready | [herb-farming-system-dev](active/20260514-herb-farming-system-dev.md) | dev | `requirements/herb-farming-system.md` | `tech-design/herb-farming-system.md` | All slices in spec. Inventory cap + herb drop wiring out of scope. |
| 3 | Done | [inventory-bag-system-techdesign](active/20260514-inventory-bag-system-techdesign.md) | techdesign | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | TD spec complete |
| 4 | Ready | [inventory-bag-system-dev](active/20260515-inventory-bag-system-dev.md) | dev | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Bag schema + capacity + upgrade action ready for implementation |
| 5 | Ready | [home-cave-defense](active/20260515-home-cave-defense-techdesign.md) | techdesign | `requirements/home-cave-defense.md` | — | TD state machine, blueprint persistence, contested map runtime, disconnect handling |
