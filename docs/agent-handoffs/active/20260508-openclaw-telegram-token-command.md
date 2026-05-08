# Handoff: OpenClaw Telegram `/token` command

## Metadata

- Status: Ready
- Priority: P2
- Suggested owner: dev
- Last updated: 2026-05-08
- Source discussion: User wants to hand off `/token` Telegram command work to another agent.

## Goal

Thêm lệnh `/token` cho bot Telegram của OpenClaw để user xem nhanh token/context usage hiện tại ngay trong chat.

## Context To Keep

- User chưa rõ `/token` để làm gì; cần giữ hành vi đơn giản, thực dụng.
- Mục đích của `/token`: xem nhanh session hiện tại đang tốn bao nhiêu token/context, cache hit ra sao, có gần đầy context không.
- Repo rule: handoff doc là source of truth cho agent tiếp theo.
- OpenClaw hiện chạy ổn sau fix gateway/update trước đó.
- `devops` agent đã được tối ưu prompt surface trong `~/.openclaw/openclaw.json`:
  - `contextInjection = continuation-skip`
  - `localModelLean = true`
  - `thinkingDefault = low`
  - `maxTokens = 4000`
  - `devops` tools/skills đã bị thu gọn để giảm token

## Confirmed Decisions

- `/token` là lệnh tiện ích để quan sát usage, không phải tính năng gameplay/app chính.
- Output nên bằng tiếng Việt, ngắn, dễ đọc trên Telegram.
- Có thể dùng emoji nếu làm lệnh này.
- Không cần nhét lịch sử dài; chỉ cần số hiện tại và vài chỉ báo chính.

## Scope

- Tìm điểm hook phù hợp trong OpenClaw/Telegram command handling hiện tại.
- Thêm lệnh `/token` cho account Telegram liên quan.
- Format trả về ngắn gọn, ví dụ:
  - model hiện tại
  - input/output/cacheRead/cacheWrite nếu có
  - tổng token hoặc context used/limit
  - % context đã dùng
- Test lệnh hoạt động end-to-end.

## Out Of Scope

- Không cần làm dashboard usage đầy đủ.
- Không cần sửa 9Router trong task này.
- Không cần tối ưu token further trong task này, trừ khi cần để lệnh hoạt động.

## Acceptance Criteria

- Gửi `/token` cho bot Telegram nhận được phản hồi hợp lệ.
- Phản hồi là tiếng Việt, ngắn, có emoji nếu phù hợp.
- Ít nhất hiển thị được một trong hai nhóm số:
  - session token usage gần nhất
  - hoặc context usage hiện tại của session
- Không làm hỏng các lệnh Telegram đang có.

## Relevant Files Or Docs

- `docs/agent-handoffs/QUEUE.md`
- `docs/agent-handoffs/active/20260508-openclaw-telegram-token-command.md`
- `~/.openclaw/openclaw.json`
- OpenClaw local install/code:
  - `/home/vm-01/.openclaw/tools/node-v24.15.0/lib/node_modules/openclaw`

## Open Questions / Blockers

- `/token` nên là slash command native của Telegram route, hay chỉ là text command handled by agent/runtime.
- Nguồn số liệu tốt nhất là:
  - session live metadata
  - transcript usage gần nhất
  - hay `status`/gateway snapshot
- Nếu OpenClaw không có hook command gọn sẵn, có thể cần patch trực tiếp source local install.

## Recommended Next Step

Agent `dev` đọc file này trước, rồi:
1. tìm command routing/hook của Telegram trong local OpenClaw source
2. implement `/token`
3. test trực tiếp qua Telegram hoặc qua CLI/gateway path tương đương
4. báo lại format output cuối cùng cho user
