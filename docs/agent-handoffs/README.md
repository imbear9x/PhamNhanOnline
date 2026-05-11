# Agent Handoffs

Thư mục này là điểm giao việc chung giữa các agent trong repo.

Shortcut:

- nếu user chỉ nói `đọc rule làm việc của chúng ta đi`, agent nên tự hiểu là cần đọc `AGENTS.md`, `WORKFLOW_RULES.md`, rồi mới dùng file này khi task có liên quan tới handoff

Mục tiêu:

- tránh việc user phải copy prompt tay từ agent này sang agent khác
- biến phần đã chốt thành artifact bền trong repo
- cho agent thực thi đọc đúng source of truth thay vì đoán từ chat

## Vòng đời đúng

Flow khuyến nghị:

1. Bàn bạc / làm rõ ý tưởng với `gamedesign`
2. Ghi và hoàn thiện dần trong:
   - `docs/game-design-wp/notes/`
   - `docs/game-design-wp/features/`
   - `docs/game-design-wp/requirements/`
3. Chỉ khi user thấy đã đủ rõ để làm thật, mới tạo handoff trong `docs/agent-handoffs/active/`
4. `dev` nhận việc từ handoff đó
5. Xong việc thì chuyển handoff sang `archive/` hoặc đánh dấu `Done`

Nói ngắn:

- `game-design-wp` là nơi suy nghĩ và hoàn thiện dần
- `agent-handoffs` là nơi giao việc đã chốt

## Cách dùng

Khi đang bàn bạc với một agent và đã chốt được việc cần làm:

1. Agent hiện tại tạo hoặc cập nhật một handoff doc trong `active/`
2. Cập nhật `QUEUE.md` nếu handoff đó đã sẵn sàng giao cho agent khác
3. Nếu có nhiều việc sẵn sàng cùng lúc, user quyết định ưu tiên trước khi `dev` bắt đầu
4. User chỉ cần nói với agent tiếp theo:
   - `làm theo docs/agent-handoffs/active/<ten-file>.md`
5. Agent tiếp theo đọc file đó trước, rồi mới đọc thêm code/doc liên quan

Khi đang bàn nhưng chưa muốn làm ngay:

1. Không cần tạo handoff
2. Tiếp tục cập nhật doc ở `docs/game-design-wp/`
3. Chỉ khi nào user nói kiểu `ổn rồi`, `chốt`, `giao dev làm`, mới nâng lên handoff

## Cấu trúc

- `TEMPLATE.md`
  - khung chuẩn để tạo handoff mới
- `QUEUE.md`
  - hàng đợi ngắn liệt kê handoff nào đang `Ready`, `In Progress`, `Blocked`
- `SESSION_STARTERS.md`
  - các câu mở đầu ngắn để user đưa agent vào đúng mode ngay từ đầu phiên
- `active/`
  - các handoff còn hiệu lực
- `archive/`
  - handoff đã xong, bị thay thế, hoặc không còn dùng

## Quy tắc viết

Handoff phải ngắn, cụ thể, và có thể thực thi được.

Phải có tối thiểu:

- mục tiêu
- bối cảnh cần giữ
- quyết định đã chốt
- phạm vi làm
- phạm vi không làm
- acceptance criteria
- câu hỏi mở hoặc blocker
- agent / vai trò được khuyến nghị nhận việc

Không nên nhét toàn bộ quá trình bàn bạc vào handoff.

Những phần còn đang khám phá, tranh luận, hoặc chưa khóa quyết định thì giữ ở `docs/game-design-wp/`.

## Quy ước đặt tên

Format gợi ý:

- `YYYYMMDD-short-task-name.md`

Ví dụ:

- `20260507-world-target-selection-cleanup.md`
- `20260507-openclaw-startup-ready-notify.md`

## Nguyên tắc source of truth

- Chat dùng để khám phá và ra quyết định.
- Handoff doc dùng để giao việc.
- Nếu chat và handoff mâu thuẫn nhau, phải cập nhật handoff doc trước khi giao sang agent khác.
- `QUEUE.md` là nơi nhìn nhanh xem hiện có việc nào thật sự sẵn sàng cho `dev`.

## Truth-resolution handoffs

Khi Knowledge Manager, GameDesign, Dev, hoặc Manager phát hiện một tri thức không thể tự chốt vì thiếu authority/evidence, không được để nó nằm im trong `partial`, `pending`, hoặc `needs-review`.

Tạo handoff trong `active/` và cập nhật `QUEUE.md` khi cần người/agent khác xác minh.

Handoff dạng này phải ghi rõ:

- domain hoặc checklist row đang bị chặn
- câu hỏi cần xác minh
- file/code/doc cần đọc
- expected output để Knowledge Manager cập nhật tri thức
- suggested owner: `dev`, `gamedesign`, hoặc `manager`
- nơi báo kết quả về: Manager trước, rồi Knowledge Manager re-check canonical docs/checklist

Nếu không biết route cho ai, Manager phải báo blocker cho user thay vì để artifact kẹt vô thời hạn.
