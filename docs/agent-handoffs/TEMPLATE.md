# Handoff: <task title>

## Metadata

- Queue ID: <required natural number, must match the queue row and filename numeric segment>
- Status: Draft | Ready | In Progress | Blocked | Done
- Priority: P1 | P2 | P3 | Unset
- Source agent: gamedesign | techdesign | dev | reviewer | qa | manager | user
- Target agent: techdesign | dev | reviewer | qa | gamedesign | manager
- Suggested owner: techdesign | dev | reviewer | qa | gamedesign | manager
- Feature key: <stable feature or slice key>
- Handoff type: <requirement | tech-design | implementation | review | required-fix | response | re-review | qa | other>
- Source handoff: <optional path to parent or originating handoff>
- Response to: <optional path to the handoff this one directly answers>
- Supersedes: <optional path to older handoff replaced by this one>
- Iteration: <optional natural number for the same feature/slice lifecycle>
- Last updated: YYYY-MM-DD
- Source discussion: <optional short note>
- Source design doc: <optional path under docs/game-design-wp/>
- Source tech design doc: <optional path under docs/tech-design/>
- Expected output: <requirement update | tech-design spec | implementation | review | qa report | other>

## Goal

Mục tiêu cuối cùng của việc này là gì.

## Context To Keep

Những bối cảnh quan trọng mà agent tiếp theo phải giữ đúng.

## Confirmed Decisions

- Quyết định 1
- Quyết định 2

## Scope

- Việc phải làm 1
- Việc phải làm 2

## Out Of Scope

- Việc không làm 1
- Việc không làm 2

## Acceptance Criteria

- Điều kiện hoàn thành 1
- Điều kiện hoàn thành 2

## Relevant Files Or Docs

- `path/to/file`
- `path/to/doc`

## Open Questions / Blockers

- Câu hỏi hoặc blocker 1
- Câu hỏi hoặc blocker 2

## Recommended Next Step

Bước tiếp theo cụ thể cho agent nhận việc.

## Completion Output

Agent nhận việc phải tạo/cập nhật artifact nào, báo cáo gì, và handoff tiếp theo đi đâu nếu có.

> **Ghi chú cho QA handoff:** Nếu kết quả là `Failed`, next owner luôn là `techdesign`, không phải `dev`.
> QA không cần phân loại defect — chỉ cần báo cáo rõ expected vs actual và evidence, rồi giao `techdesign` đánh giá và quyết định hướng fix.
> `techdesign` sẽ tự quyết định có cần update spec không trước khi tạo handoff `dev`.

## Lifecycle Update Required On Completion

Khi hoàn tất lượt xử lý của handoff này, agent phải:

1. ngay khi bắt đầu làm, cập nhật trạng thái dòng queue của chính handoff này từ `Ready` sang `In Progress`
2. khi hoàn tất lượt xử lý, cập nhật trạng thái dòng queue của chính handoff này sang `Done` hoặc `Blocked`, không để sót `Ready`/`In Progress`
3. nếu sinh handoff mới, tạo file handoff mới theo format `YYYYMMDD-<Queue ID>-<short-task-name>.md` với `Queue ID` mới lớn hơn queue trước đúng 1 đơn vị
4. thêm dòng mới vào `QUEUE.md`
5. ghi rõ `Response to` / `Source handoff` / `Supersedes` khi phù hợp
