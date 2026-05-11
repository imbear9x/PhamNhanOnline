# Semi-Automatic Knowledge Manager Workflow

## Purpose

Workflow này dùng để đảm bảo mọi thay đổi quan trọng trong dự án đều được ghi lại bằng Change Note và được Knowledge Manager xử lý để giữ docs/memory không stale.

## Flow

1. Agent hoàn thành task.
2. Nếu task ảnh hưởng system/design/code/config/db/docs/test, agent tạo Change Note trong:
   - `/docs/change-notes/inbox/`
3. User hoặc Manager gọi:
   - `Knowledge Manager, kiểm tra change notes mới và cập nhật docs liên quan.`
4. Knowledge Manager đọc inbox.
5. Knowledge Manager cập nhật docs hoặc tạo conflict report.
6. Knowledge Manager chuyển Change Note sang processed hoặc needs-review.
7. Knowledge Manager báo cáo kết quả.

## Current Mode

Semi-automatic:
- chưa có event hook
- chưa tự chạy nền
- User/Manager gọi thủ công

## Future Mode

Event-driven automatic:
- khi có Change Note mới
- hoặc khi task done
- hệ thống tự gọi Knowledge Manager
