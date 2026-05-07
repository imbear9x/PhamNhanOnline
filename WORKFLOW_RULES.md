# Workflow Rules

Đây là file điểm vào ngắn gọn cho workflow làm việc giữa user và các agent trong repo `PhamNhanOnline`.

Mục tiêu:

- user không phải nhớ nhiều file
- agent có một file chuẩn để đọc ở đầu phiên
- mọi agent vào đúng mode trước khi bắt đầu làm việc

## Câu Mở Đầu Chuẩn

Nếu user nói đại loại:

- `đọc rule làm việc của chúng ta đi`
- `đọc workflow làm việc rồi bắt đầu`
- `đọc rule repo rồi làm việc`

thì agent phải hiểu là cần đọc:

1. `AGENTS.md`
2. file này: `WORKFLOW_RULES.md`

và nếu task liên quan handoff / bàn bạc / giao việc thì đọc thêm:

3. `docs/agent-handoffs/README.md`

Nếu là `gamedesign`, đọc thêm:

4. `docs/game-design-wp/AGENTS.md`

## Quy Ước Chung

- đang bàn và hoàn thiện ý tưởng thì chưa tạo handoff sớm
- phần đang khám phá sống trong `docs/game-design-wp/`
- chỉ khi user nói đã chốt / sẵn sàng làm thật mới tạo handoff
- handoff là artifact giao việc, không phải bản chép lại toàn bộ chat
- nếu có nhiều handoff `Ready`, agent thực thi phải hỏi lại user thứ tự ưu tiên

## Theo Vai Trò

### GameDesign

- dùng `docs/game-design-wp/notes/`, `features/`, `requirements/` để ghi nhớ và hoàn thiện dần
- chưa chốt thì không đẩy sang execution handoff
- khi user nói chốt, tạo hoặc cập nhật handoff trong `docs/agent-handoffs/active/`
- cập nhật `docs/agent-handoffs/QUEUE.md`

### Dev

- ưu tiên đọc handoff doc nếu task đã được chốt
- không yêu cầu user nhắc lại toàn bộ cuộc bàn bạc trước đó
- nếu handoff chưa đủ chi tiết kỹ thuật, bổ sung lại vào doc trong lúc nhận việc
- nếu có nhiều handoff `Ready` và chưa rõ ưu tiên, hỏi user trước khi bắt đầu

### Manager Hoặc Agent Khác

- nếu đang giúp user làm rõ việc, có thể làm ở mode thảo luận
- khi task đủ rõ và user muốn giao tiếp cho agent khác, phải chuyển sang handoff doc thay vì tạo prompt tay cho user copy

## File Chính Cần Biết

- `AGENTS.md`
  - rule repo-wide
- `WORKFLOW_RULES.md`
  - điểm vào workflow đầu phiên
- `docs/agent-handoffs/README.md`
  - cách dùng handoff
- `docs/agent-handoffs/QUEUE.md`
  - danh sách việc đã sẵn sàng để làm

## Quy Tắc Thực Dụng

User không cần nhớ path chi tiết mỗi lần.

Chỉ cần nói:

```text
đọc rule làm việc của chúng ta đi. oke thì bắt đầu
```

Agent phải tự biết đọc đúng các file rule liên quan trước khi tiếp tục.
