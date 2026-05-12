# Prompt: Migrate Existing Game Design Docs

Use this prompt with the `gamedesign` agent when you want it to normalize all existing `docs/game-design-wp` docs.

```text
đọc rule làm việc của chúng ta.

Nhiệm vụ: migrate toàn bộ docs hiện có trong `docs/game-design-wp/` sang chuẩn lifecycle/template mới.

Phạm vi:
- Chỉ làm trong `docs/game-design-wp/` trừ khi cần update link trong `docs/DOCS_INDEX.md`.
- Không đụng code, server, client, DB, scripts, infra, hoặc `.openclaw/`.
- Đọc `docs/game-design-wp/DOC_LIFECYCLE.md`.
- Dùng template trong `docs/game-design-wp/templates/`.

Yêu cầu bắt buộc:
- Mỗi `system_id` chỉ được có 1 live primary doc tổng cộng trong `notes/`, `features/`, `requirements/`.
- Nếu một system đã đủ feature thì migrate note sang `features/<system_id>.md` và xóa note cũ.
- Nếu một system đã đủ requirement thì migrate feature sang `requirements/<system_id>.md` và xóa feature cũ.
- Không tạo file `v2`, `final`, `new`, `copy`, hoặc date-suffixed duplicate.
- Không giữ source doc cũ sau khi promote; trước khi xóa phải copy toàn bộ decision/question/risk còn giá trị vào doc mới.
- Các file exception được giữ: `conversation-log.md`, `deferred-features.md`, `design-backlog-triage.md`, folder `README.md`, `clarifications/*.md`, và agent identity/tool files.
- Nếu gặp conflict giữa docs, không tự chọn im lặng. Ghi vào `Known conflicts / drift` và nếu cần hỏi user.
- Nếu cần code verification, set `requires_code_verification: true` và ghi rõ câu hỏi cần verify.

Cách làm:
1. Liệt kê toàn bộ primary docs hiện có và nhóm theo system.
2. Quyết định maturity cao nhất phù hợp cho từng system: note, feature, hoặc requirement.
3. Rewrite/migrate từng system vào đúng template.
4. Xóa primary doc tầng thấp hơn sau khi migrate xong.
5. Update link nội bộ và README nếu cần.
6. Báo cáo theo format: Goal, Design summary, Key decisions, Open questions, Recommended next step.

Ưu tiên migrate một lượt toàn bộ vì số lượng docs hiện còn ít.
```
