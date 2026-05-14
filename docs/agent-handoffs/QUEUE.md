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
