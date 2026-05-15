# Handoff Queue

File này là bảng nhìn nhanh để biết hiện có handoff nào đang sẵn sàng cho agent khác nhận.

Không dùng file này để thay thế nội dung chi tiết của handoff.
Chi tiết luôn nằm trong từng file ở `active/`.

`Priority` hiện đang được dùng như queue-id tăng dần cho từng handoff lifecycle node. Mỗi handoff mới phải dùng số tự nhiên kế tiếp đúng `+1` so với dòng mới nhất trước đó. Số này là định danh canonical để liên kết queue row với file handoff tương ứng. Tên file mới nên theo format `YYYYMMDD-<queue-id>-<short-task-name>.md`; nếu ngày và queue-id mâu thuẫn nhau thì queue-id là canonical.

## Quy tắc

- Chỉ đưa vào đây các handoff đã đủ rõ để làm thật.
- Nếu ý tưởng còn đang bàn và chưa chốt, giữ nó ở `docs/game-design-wp/`, không đưa vào queue.
- Nếu có nhiều handoff `Ready`, `dev` không tự đoán ưu tiên khi user chưa chốt thứ tự.
- Agent không tự poll hoặc auto-claim queue theo timer. Chỉ check và nhận handoff khi user dispatch thủ công.
- Khi user nói `check có handoff không`, agent chỉ xem các dòng `Ready` có `Owner` đúng vai trò của mình.
- Nếu có nhiều dòng `Ready` cùng owner, agent hỏi user chọn ưu tiên.
- Khi bắt đầu làm một handoff đã chọn, agent phải cập nhật dòng queue nguồn từ `Ready` sang `In Progress` ngay.
- Khi xong việc hoặc hủy việc, cập nhật trạng thái ở đây và trong file handoff gốc sang `Done` hoặc `Blocked`.
- Không được chỉ tạo handoff mới mà để handoff nguồn vẫn `Ready` hoặc `In Progress` nếu lượt xử lý của nó đã kết thúc.
- Khi tạo handoff mới, dùng queue-id kế tiếp đúng `+1` và ưu tiên đặt tên file theo format `YYYYMMDD-<queue-id>-<short-task-name>.md`.

## Status

- `Ready`
  - đã đủ rõ để giao cho agent khác làm
- `In Progress`
  - đã có agent nhận và đang làm; nếu agent lỗi/reset giữa chừng thì đây là dấu hiệu cần audit hoặc rollback phần làm dở
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
| 4 | Done | [inventory-bag-system-dev](active/20260515-inventory-bag-system-dev.md) | dev | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Dev implementation completed and handed to reviewer |
| 5 | Blocked | [inventory-bag-system-qa](active/20260515-inventory-bag-system-qa.md) | qa | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Tạm chặn: reviewer vừa fail follow-up bag diff do Required Fix `reviewer-bag-capacity-precheck-race`; chỉ QA lại sau khi dev sửa và reviewer pass lại. |
| 6 | Ready | [home-cave-defense](active/20260515-home-cave-defense-techdesign.md) | techdesign | `requirements/home-cave-defense.md` | — | TD state machine, blueprint persistence, contested map runtime, disconnect handling |
| 7 | Ready | [npc-system](active/20260515-npc-system-techdesign.md) | techdesign | `requirements/npc-system.md` | — | TD verify NPC runtime, timed lifecycle, shop semantics, entry actions |
| 8 | Done | [reviewer-bag-slot-count-and-bag-init-races](active/20260515-reviewer-bag-slot-count-and-bag-init-races.md) | dev | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Required reviewer fixes implemented in dev follow-up. |
| 9 | Done | [inventory-bag-system-reviewer](active/20260515-inventory-bag-system-reviewer.md) | reviewer | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Reviewer đã xử lý lượt review này và sinh follow-up Required Fix `reviewer-bag-capacity-precheck-race`. |
| 10 | Done | [reviewer-bag-capacity-precheck-race](active/20260515-reviewer-bag-capacity-precheck-race.md) | dev | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Required reviewer fix implemented: active capacity check moved into same inventory lock/transaction boundary as grant. |
| 11 | Done | [reviewer-bag-capacity-precheck-race-response](active/20260515-reviewer-bag-capacity-precheck-race-response.md) | reviewer | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Reviewer verified TOCTOU follow-up fix; handed off to QA with accepted residual risks. |
| 12 | Done | [inventory-bag-system-qa-followup](active/20260515-12-inventory-bag-system-qa-followup.md) | qa | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | QA completed follow-up and returned to Dev via report `active/20260515-13-inventory-bag-system-qa-followup-fail.md`. |
| 13 | Done | [inventory-bag-system-qa-followup-fail](active/20260515-13-inventory-bag-system-qa-followup-fail.md) | techdesign | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | TechDesign đã đánh giá: Loại A, spec bổ sung random output contract, handoff Dev tạo tại #14. |
| 14 | Done | [inventory-bag-herb-random-output-fix-dev](active/20260515-14-inventory-bag-herb-random-output-fix-dev.md) | dev | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Fix HarvestHerbAsync: roll all outputs trước, check capacity trên full proc set, reject entirely nếu không fit. Spec đã bổ sung section random output contract. |
| 15 | Done | [inventory-bag-herb-random-output-fix-reviewer](active/20260515-15-inventory-bag-herb-random-output-fix-reviewer.md) | reviewer | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Reviewer pass with risks: fix đúng spec, atomic, không còn TOCTOU. QA handoff tại #16. |
| 16 | Done | [inventory-bag-herb-random-output-fix-qa](active/20260515-16-inventory-bag-herb-random-output-fix-qa.md) | qa | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | QA completed; passed. Report: `active/20260515-17-inventory-bag-herb-random-output-fix-qa-report.md`. |
| 17 | Done | [inventory-bag-herb-random-output-fix-qa-report](active/20260515-17-inventory-bag-herb-random-output-fix-qa-report.md) | release | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | TechDesign đã đọc QA report → tạo handoff dev-client tại #18. |
| 18 | Ready | [herb-bag-client-dev](active/20260515-18-herb-bag-client-dev.md) | dev-client | `requirements/inventory-bag-system.md` | `tech-design/inventory-bag-system.md` | Unity client implement Inventory Bag + Herb Farming: packet flow, error handling, UI contract. |
