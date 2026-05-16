# Agent Handoffs

Thư mục này là điểm giao việc chung giữa các agent trong repo.

Shortcut:

- nếu user chỉ nói `đọc rule làm việc của chúng ta đi`, agent nên tự hiểu là cần đọc `AGENTS.md`, `WORKFLOW_RULES.md`, rồi mới dùng file này khi task có liên quan tới handoff

Mục tiêu:

- tránh việc user phải copy prompt tay từ agent này sang agent khác
- biến phần đã chốt thành artifact bền trong repo
- cho agent thực thi đọc đúng source of truth thay vì đoán từ chat

## Vòng đời đúng

Flow khuyến nghị cho feature gameplay:

1. Bàn bạc / làm rõ ý tưởng với `gamedesign`
2. Ghi và hoàn thiện dần trong:
   - `docs/game-design-wp/notes/`
   - `docs/game-design-wp/features/`
   - `docs/game-design-wp/requirements/`
3. Khi user chốt design, `gamedesign` tạo handoff cho `techdesign` trong `docs/agent-handoffs/active/`
4. `techdesign` đọc handoff, đọc design docs, inspect code, rồi tạo spec trong `docs/tech-design/`
5. Khi spec đủ để implement, `techdesign` tạo handoff cho `dev` trong `docs/agent-handoffs/active/`
6. `dev` đọc handoff + TechDesign spec + source GameDesign docs, implement và verify server/shared scope
7. Khi code xong, `dev` tạo handoff cho `reviewer`, không giao thẳng cho `qa`
8. `reviewer` review chất lượng code/struct/DB/performance/maintainability/testability
9. Nếu review fail hoặc có Required Fix, `reviewer` tạo handoff ngược lại cho `dev`
10. Nếu review đạt, `reviewer` tạo handoff cho `qa`
11. `qa` verify expected vs actual bằng evidence rồi báo Passed/Failed/Blocked/Needs clarification
12. Nếu QA fail, `qa` luôn tạo handoff cho `techdesign` — không tạo thẳng cho `dev`
13. `techdesign` nhận handoff QA fail, đánh giá spec, rồi quyết định: update spec nếu cần, sau đó tạo handoff `dev`
14. Nếu QA pass và feature có khả năng ảnh hưởng client/release contract, QA report/handoff kế tiếp phải giao cho `techdesign`
15. `techdesign` đọc QA report + spec + packet/code evidence, rồi tạo handoff `dev-client` nếu client cần implement hoặc cập nhật hành vi
16. `dev-client` implement phần Unity/C# text-editable, viết User Unity Implementation Guide, rồi tạo handoff cho `client-reviewer`
17. `client-reviewer` review code client + guide; nếu pass thì tạo handoff `Owner = user` để user/agent ngoài wire Unity Editor và test tay
18. Xong việc thì chuyển handoff sang `archive/` hoặc đánh dấu `Done`

Nói ngắn:

- `game-design-wp` là nơi suy nghĩ và hoàn thiện dần
- `agent-handoffs` là nơi giao việc đã chốt giữa agent
- `docs/tech-design/` là nơi TechDesign xuất tài liệu kỹ thuật cho Dev
- flow implementation chuẩn là `dev -> reviewer -> qa -> techdesign`
- nếu QA fail: `techdesign -> dev`
- nếu QA pass và cần client: `techdesign -> dev-client -> client-reviewer -> user`

## Cách dùng

Khi đang bàn bạc với một agent và đã chốt được việc cần làm:

1. Agent hiện tại tạo hoặc cập nhật một handoff doc trong `active/`
2. Cập nhật `QUEUE.md` nếu handoff đó đã sẵn sàng giao cho agent khác
3. Nếu có nhiều việc sẵn sàng cùng lúc, user quyết định ưu tiên trước khi agent nhận việc bắt đầu
4. User chỉ cần nói với agent tiếp theo:
   - `check QUEUE xem có handoff của m không, nếu có thì làm`
   - hoặc `làm theo docs/agent-handoffs/active/<ten-file>.md`
5. Agent tiếp theo đọc file đó trước, rồi mới đọc thêm code/doc liên quan

## Handoff Lifecycle Rule

Đây là rule chung cho mọi agent, không phân biệt `gamedesign`, `techdesign`, `dev`, `reviewer`, `qa`, hay `manager`.

Khi một agent nhận một handoff đang ở trạng thái `Ready` và hoàn tất lượt xử lý của chính handoff đó, agent **phải đóng vòng handoff nguồn trong queue**:

- đổi `Ready -> In Progress` ngay khi bắt đầu làm handoff
- đổi `In Progress -> Done` nếu đã xử lý xong lượt của handoff đó
- đổi `In Progress -> Blocked` nếu đã xử lý nhưng đang chờ dependency hoặc handoff khác

Không được bắt đầu xử lý mà bỏ nguyên handoff nguồn ở trạng thái `Ready`.
Không được chỉ tạo handoff tiếp theo mà bỏ nguyên handoff nguồn ở trạng thái `Ready` hoặc `In Progress`.
Nếu agent lỗi hoặc bị reset giữa chừng, queue còn `In Progress` để user biết đây là việc đang làm dở và có thể audit/rollback.

Nếu kết quả công việc sinh ra bước tiếp theo:

1. tạo handoff mới cho bước tiếp theo
2. thêm dòng mới vào `QUEUE.md`
3. ghi rõ quan hệ lifecycle giữa handoff mới và handoff cũ

Mục tiêu là để `QUEUE.md` luôn phản ánh **task hiện hành** thay vì danh sách task từng tồn tại.

## Manual Dispatch Rule

Agents must not auto-claim handoffs on a timer.

The user manually dispatches work by telling an agent to check its handoff. When dispatched:

- `techdesign` checks `QUEUE.md` for `Owner = techdesign`
- `dev` checks `QUEUE.md` for `Owner = dev`
- `reviewer` checks `QUEUE.md` for `Owner = reviewer`
- `qa` checks `QUEUE.md` for `Owner = qa`
- `techdesign` also checks `QUEUE.md` for post-QA release/client handoffs with `Owner = techdesign`
- `dev-client` checks `QUEUE.md` for `Owner = dev-client` when the user dispatches client work
- `client-reviewer` checks `QUEUE.md` for `Owner = client-reviewer`
- `user` rows are for the human user or an outside Unity agent, not for an OpenClaw agent to auto-claim
- if exactly one matching `Ready` handoff exists, the agent may start after restating the target
- if multiple matching `Ready` handoffs exist, the agent asks the user which one to do first
- if none exists, the agent reports that there is no ready handoff for its role

When an agent starts a selected `Ready` handoff, it must update that queue row to `In Progress` before doing implementation/review/QA/spec work.

## Implementation Review Gate

Dev does not normally hand work directly to QA.

After implementation, Dev must prepare a Reviewer handoff with:

- implementation summary
- files/modules touched
- build/test commands run and results
- DB/schema/seed changes, if any
- packet/broadcast/runtime changes, if any
- QA notes, test scope, and known gaps for later QA verification
- risks, blockers, or skipped checks

The handoff owner must be `reviewer`.

Reviewer then decides the next route:

- `Fail` or Required Fix -> create/update a handoff for `dev`
- `Pass` or `Pass with risks` -> create a QA handoff for `qa`
- Improvement Proposal -> ask user first; create Dev handoff only after approval

QA normally starts only from a Ready handoff with `Owner = qa` that was created after Reviewer passed the implementation, unless the user explicitly says to skip Reviewer.

## QA Fail Route

Khi QA fail, **QA luôn tạo handoff cho `techdesign`**, không tạo thẳng cho `dev`.

QA không cần phán xét defect thuộc loại nào. QA chỉ cần:

1. báo cáo rõ expected vs actual và evidence
2. tạo handoff với `Owner = techdesign`
3. đóng handoff nguồn của mình sang `Done`

`techdesign` nhận handoff từ QA fail và **phải đánh giá trước khi làm bất cứ việc gì**:

- đọc defect report từ QA
- đối chiếu với spec hiện tại trong `docs/tech-design/`
- nếu spec đã đủ cover case này: tạo handoff `dev` ngay, kèm pointer rõ vào spec và contract cần implement — TechDesign không implement code
- nếu spec còn gap hoặc cần design decision: update spec trước, rồi mới tạo handoff `dev` — TechDesign không implement code

`techdesign` **không tự implement code** trong flow này. Output của TechDesign luôn là spec (update nếu cần) + handoff `dev`.

`dev` nhận handoff từ TechDesign, implement theo spec đã được TechDesign confirm, rồi tiếp tục vòng `dev -> reviewer -> qa` như bình thường.

## QA Passed Client/Release Route

Khi QA pass một server/shared feature, QA không nên giao thẳng cho `dev-client`.

QA nên:

1. viết QA report với evidence rõ ràng
2. nếu feature có packet/UI/runtime behavior client cần biết, tạo hoặc cập nhật queue row `Owner = techdesign`, `Status = Ready`
3. đóng QA handoff nguồn sang `Done`

`techdesign` nhận QA-passed handoff và phải làm bước **client contract synthesis**:

- đọc QA report, reviewer verdict, Dev handoff, TechDesign spec, packet/model files, và MessageCode liên quan
- xác định server contract nào đã thật sự pass và contract nào đã bị supersede hoặc blocked
- nếu không có client impact, đóng handoff nguồn sang `Done` và ghi rõ `No client handoff needed`
- nếu có client impact, tạo handoff mới cho `dev-client`

Handoff `dev-client` phải đủ để client implement mà không cần đoán:

- source QA report(s), source TechDesign spec, source requirement/design docs
- packet names, packet IDs, direction C→S/S→C/broadcast, important fields
- UI state rules and refresh strategy
- success/failure behavior, especially what state must not be optimistically removed on failure
- error/message codes and user-facing handling
- accepted backend risks that client must tolerate
- out of scope
- manual E2E test checklist for client
- supersedes/response_to/source_handoff metadata when replacing older client handoffs

Nếu tạo handoff mới thay thế handoff cũ, TechDesign phải update `QUEUE.md` và handoff cũ khỏi `Ready` để `dev-client` không nhặt nhầm.

## Client Implementation Route

`dev-client` nhận handoff từ TechDesign và chỉ làm phần client code có thể sửa bằng text:

- packet DTO/handler
- client service/controller/view-model/state cache
- message/error mapping
- helper/mock/debug code nếu cần

`dev-client` không vận hành Unity Editor trên VPS và không claim đã setup prefab/scene/Inspector.

Sau khi implement, `dev-client` phải tạo:

- Dev Client report
- User Unity Implementation Guide: prefab cần tạo/sửa, component cần gắn, Inspector field cần assign, scene/UI hierarchy, button/event wiring, manual Play Mode test checklist
- handoff mới `Owner = client-reviewer`

`client-reviewer` review code client và guide:

- nếu fail: tạo handoff lại cho `dev-client`
- nếu pass: tạo handoff `Owner = user`

`Owner = user` nghĩa là việc chuyển sang user hoặc agent ngoài để mở Unity Editor, wire prefab/scene/Inspector và test tay.

Nếu user test Unity phát hiện cần đổi server/spec, user trao đổi với `dev-client`; `dev-client` phân loại vấn đề và tạo handoff `Owner = techdesign` nếu đó là server/spec/contract issue.

Khi đang bàn nhưng chưa muốn làm ngay:

1. Không cần tạo handoff
2. Tiếp tục cập nhật doc ở `docs/game-design-wp/`
3. Chỉ khi nào user nói kiểu `ổn rồi`, `chốt`, `giao techdesign làm`, hoặc `giao dev làm`, mới nâng lên handoff

## Cấu trúc

- `TEMPLATE.md`
  - khung chuẩn để tạo handoff mới
- `CLIENT_DEV_TEMPLATE.md`
  - khung chuẩn cho TechDesign tạo handoff Unity/client sau khi QA pass server/shared scope
- `CLIENT_REVIEW_TEMPLATE.md`
  - khung chuẩn cho Dev Client tạo handoff review client code/guide
- `USER_UNITY_HANDOFF_TEMPLATE.md`
  - khung chuẩn cho Client Reviewer giao việc Unity Editor/manual test cho user hoặc agent ngoài
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
- source agent
- target agent / owner
- expected output artifact
- agent / vai trò được khuyến nghị nhận việc

Không nên nhét toàn bộ quá trình bàn bạc vào handoff.

Những phần còn đang khám phá, tranh luận, hoặc chưa khóa quyết định thì giữ ở `docs/game-design-wp/`.

## Quy ước đặt tên

Format chuẩn mới:

- `YYYYMMDD-<queue-id>-<short-task-name>.md`

Trong đó:

- `YYYYMMDD` giúp đọc lịch sử dễ hơn bằng mắt người
- `queue-id` là số tự nhiên tăng dần theo `QUEUE.md`
- handoff tạo sau phải có `queue-id = queue-id trước + 1`
- `queue-id` là định danh canonical để tránh trùng tên và tránh mơ hồ giữa các vòng follow-up
- nếu ngày và queue-id mâu thuẫn nhau thì **queue-id là khóa lifecycle canonical**, còn ngày chỉ là metadata hỗ trợ đọc

Ví dụ:

- `20260515-11-reviewer-bag-capacity-precheck-race-response.md`
- `20260515-12-inventory-bag-system-qa-rerun.md`

Không tái dùng file cũ cho một vòng follow-up mới. Mỗi vòng lifecycle phải có file handoff riêng.

## Legacy filename migration rule

Repo hiện có nhiều handoff cũ theo format legacy kiểu:

- `YYYYMMDD-short-task-name.md`

Khi làm việc với handoff legacy:

1. **không bắt buộc rename hồi tố toàn bộ file cũ** chỉ để khớp format mới
2. nếu handoff legacy vẫn còn active, có thể giữ nguyên tên file cũ nhưng từ vòng follow-up kế tiếp nên chuyển sang format mới `YYYYMMDD-<queue-id>-<short-task-name>.md`
3. trong `QUEUE.md`, queue row mới phải dùng `queue-id` tăng dần đúng chuẩn kể cả khi handoff cha đang dùng tên legacy
4. nếu cần ghi rõ liên hệ giữa handoff legacy và handoff mới, dùng metadata `Source Handoff`, `Response To`, `Supersedes`
5. không tạo thêm file mới theo format legacy nữa

Mục tiêu là chuyển đổi dần, không đòi hỏi một lần migrate toàn bộ lịch sử cũ.

## Metadata tối thiểu khuyến nghị

Ngoài metadata cũ, handoff mới nên có thêm các field lifecycle sau khi phù hợp:

- `Queue ID`: số định danh canonical của handoff
- `Feature Key`: ví dụ `inventory-bag-system`
- `Handoff Type`: ví dụ `review`, `required-fix`, `response`, `qa`, `re-review`
- `Source Handoff`: handoff cha hoặc handoff khởi nguồn gần nhất
- `Response To`: handoff đang được phản hồi trực tiếp
- `Supersedes`: handoff cũ bị thay thế về mặt lifecycle, nếu có
- `Iteration`: vòng xử lý thứ mấy cho cùng feature/slice

Không phải field nào cũng bắt buộc cho mọi case, nhưng agent nên điền khi có để queue và lịch sử không mơ hồ.

## Queue row contract

Mỗi dòng trong `QUEUE.md` nên được hiểu là một lifecycle node riêng.

Khuyến nghị thêm hoặc duy trì được các thông tin sau trong queue row hoặc notes:

- `Queue ID`
- `Status`
- `Owner`
- `Response To` hoặc `Supersedes`
- `Blocked By` khi có dependency rõ ràng

Khi user nói `check handoff`, agent phải ưu tiên nhìn các dòng `Ready` còn hiệu lực thật, không nhặt lại các dòng cũ đã bị supersede hoặc đáng lẽ phải `Done`/`Blocked`.

## Nguyên tắc source of truth

- Chat dùng để khám phá và ra quyết định.
- Handoff doc dùng để giao việc.
- Nếu chat và handoff mâu thuẫn nhau, phải cập nhật handoff doc trước khi giao sang agent khác.
- `QUEUE.md` là nơi nhìn nhanh xem hiện có việc nào thật sự sẵn sàng cho agent nhận việc.

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
