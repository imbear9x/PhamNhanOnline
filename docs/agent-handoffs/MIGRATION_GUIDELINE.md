# Handoff Filename + Lifecycle Migration Guideline

Tài liệu này mô tả cách chuyển dần từ format handoff legacy sang format mới mà không cần rename toàn bộ lịch sử cũ ngay lập tức.

## Mục tiêu

- dùng `queue-id` tăng dần làm khóa lifecycle canonical
- vẫn giữ `YYYYMMDD` trong tên file để con người đọc lịch sử dễ hơn
- tránh rename hàng loạt các file cũ đang được tham chiếu ở nhiều nơi
- đảm bảo agent không nhặt lại handoff cũ đã xử lý xong

## Format mới

File handoff mới nên dùng format:

- `YYYYMMDD-<queue-id>-<short-task-name>.md`

Ví dụ:

- `20260515-11-reviewer-bag-capacity-precheck-race-response.md`
- `20260515-12-inventory-bag-system-qa-rerun.md`

Quy ước:

- `queue-id` là số tự nhiên tăng dần theo `QUEUE.md`
- handoff mới phải có `queue-id = queue-id trước + 1`
- `queue-id` là khóa canonical để đối chiếu queue row, file handoff, và lifecycle follow-up
- `YYYYMMDD` chủ yếu giúp đọc lịch sử và truy dấu theo thời gian
- nếu `YYYYMMDD` và `queue-id` mâu thuẫn nhau thì `queue-id` thắng

## Tình trạng legacy hiện tại

Repo đang có nhiều file theo format cũ:

- `YYYYMMDD-short-task-name.md`

Ví dụ:

- `20260515-inventory-bag-system-reviewer.md`

Các file này vẫn hợp lệ về mặt lịch sử và tham chiếu cũ. Không cần rename hồi tố tất cả chỉ để đạt format mới.

## Rule chuyển đổi dần

### 1. Không tạo file mới theo format legacy nữa

Từ bây giờ, handoff mới nên theo format:

- `YYYYMMDD-<queue-id>-<short-task-name>.md`

### 2. Không bắt buộc rename file cũ ngay

Nếu một handoff cũ đang được tham chiếu trong docs, queue, hoặc chat history thì có thể giữ nguyên.

### 3. Follow-up của file legacy phải dùng format mới

Nếu file cũ là:

- `20260515-inventory-bag-system-reviewer.md`

thì file follow-up mới nên là kiểu:

- `20260515-11-reviewer-bag-capacity-precheck-race-response.md`

và dùng metadata để nối lifecycle:

- `Source Handoff`
- `Response To`
- `Supersedes`

### 4. Queue là canonical hơn filename cũ

Trong giai đoạn chuyển tiếp, có thể tồn tại đồng thời:

- file legacy không có queue-id trong tên
- file mới có queue-id trong tên

Khi có mâu thuẫn hoặc khó phân biệt, lấy `QUEUE.md` làm chuẩn lifecycle hiện hành.

### 5. Chỉ rename file cũ khi thật sự đáng giá

Chỉ cân nhắc rename file cũ nếu:

- số lượng tham chiếu ít
- chưa bị link rộng ở nhiều nơi
- rename giúp giảm mơ hồ thật sự

Nếu rename, phải cập nhật đồng bộ:

- `QUEUE.md`
- link trong handoff khác
- tài liệu liên quan có path cũ

## Queue update checklist

Mỗi khi nhận và xử lý một handoff `Ready`:

1. ngay khi bắt đầu làm, cập nhật chính dòng queue của handoff nguồn từ `Ready` sang `In Progress`
2. khi hoàn tất lượt xử lý, cập nhật chính dòng queue đó sang `Done` hoặc `Blocked`
3. nếu có bước tiếp theo, tạo handoff mới với `queue-id + 1`
4. thêm dòng queue mới
5. notes nên ghi rõ:
   - `Response to: ...`
   - `Supersedes: ...`
   - `Blocked by: ...` nếu có

## Metadata khuyến nghị trong handoff mới

- `Queue ID`
- `Feature key`
- `Handoff type`
- `Source handoff`
- `Response to`
- `Supersedes`
- `Iteration`

## Ví dụ chuyển tiếp thực tế

Legacy:

- `docs/agent-handoffs/active/20260515-inventory-bag-system-reviewer.md`

Follow-up mới:

- `docs/agent-handoffs/active/20260515-11-reviewer-bag-capacity-precheck-race-response.md`

Queue row mới:

- `Priority = 11`
- `Owner = reviewer`
- `Status = Ready`
- notes ghi rõ đây là review follow-up cho fix của dev

## Kết quả mong muốn

Sau giai đoạn chuyển tiếp:

- file mới đều theo format `YYYYMMDD-<queue-id>-...`
- queue phản ánh đúng task hiện hành
- các vòng dev/review/fix/qa không còn mơ hồ
- automation có thể dựa vào queue-id để phân biệt lifecycle node ổn định hơn
