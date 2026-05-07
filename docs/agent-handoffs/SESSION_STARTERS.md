# Session Starters

File này chứa các câu mở đầu ngắn để user dùng ở đầu phiên làm việc với từng agent.

Mục tiêu:

- không phải nhắc lại workflow bằng tay mỗi lần
- đưa agent vào đúng mode ngay từ đầu
- buộc agent đọc đúng rule và dùng đúng artifact trong repo

## Cách dùng chung

Ở đầu phiên, user có thể nói ngắn:

- `đọc AGENTS.md và docs/agent-handoffs/README.md trước rồi làm việc`

Nếu muốn chặt hơn theo từng vai trò, dùng các câu bên dưới.

## Cho GameDesign

Prompt gợi ý:

```text
đọc AGENTS.md, docs/game-design-wp/AGENTS.md và docs/agent-handoffs/README.md trước.
phiên này mình đang bàn và hoàn thiện tính năng thôi.
cứ ghi dần vào docs/game-design-wp/notes/, features/, requirements/.
chưa tạo handoff cho dev cho đến khi mình nói chốt.
```

Ý nghĩa:

- agent vào mode bàn bạc / hoàn thiện dần
- ghi nhớ bằng doc trong `game-design-wp`
- không đẩy sang execution quá sớm

## Cho Dev

Prompt gợi ý khi đã có handoff:

```text
đọc AGENTS.md và docs/agent-handoffs/README.md trước.
làm theo handoff ở docs/agent-handoffs/active/<ten-file>.md.
nếu có nhiều handoff ready mà chưa rõ ưu tiên thì hỏi lại mình trước khi bắt đầu.
```

Ý nghĩa:

- `dev` nhận việc từ artifact thay vì từ chat cũ
- nếu queue có nhiều việc thì không tự đoán

## Cho Trường Hợp Chốt Từ GameDesign Sang Dev

Prompt gợi ý:

```text
tính năng này ổn rồi.
hãy cập nhật requirement nếu cần, tạo handoff trong docs/agent-handoffs/active/, cập nhật docs/agent-handoffs/QUEUE.md, rồi nói cho mình path handoff để giao dev.
```

## Quy tắc thực dụng

- Nếu đang khám phá ý tưởng: ưu tiên `docs/game-design-wp/`
- Nếu đã sẵn sàng làm thật: tạo `handoff`
- Nếu có nhiều việc sẵn sàng: nhìn `QUEUE.md` rồi user chốt ưu tiên

## Mức tối thiểu

Nếu user muốn nói cực ngắn, chỉ cần:

```text
đọc rule làm việc của repo và làm đúng workflow handoff hiện tại.
```

Nhưng câu ngắn này kém chặt hơn các câu role-specific ở trên.
