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
check docs/agent-handoffs/QUEUE.md xem có handoff Ready nào owner là dev không.
nếu có đúng 1 cái thì đọc handoff đó, đọc TechDesign spec và source GameDesign docs được dẫn trong handoff, rồi implement theo rule.
nếu có nhiều cái thì hỏi mình chọn cái nào trước.
nếu không có thì báo không có handoff Ready cho dev.
```

Ý nghĩa:

- `dev` nhận việc từ artifact thay vì từ chat cũ
- nếu queue có nhiều việc thì không tự đoán

## Cho TechDesign

Prompt gợi ý khi GameDesign đã tạo handoff:

```text
đọc AGENTS.md, docs/agent-workflows/techdesign-workflow.md và docs/agent-handoffs/README.md trước.
check docs/agent-handoffs/QUEUE.md xem có handoff Ready nào owner là techdesign không.
nếu có đúng 1 cái thì đọc handoff đó, đọc source GameDesign docs, inspect code liên quan, tạo TechDesign spec trong docs/tech-design/, rồi tạo handoff Ready cho dev.
nếu có nhiều cái thì hỏi mình chọn cái nào trước.
nếu không có thì báo không có handoff Ready cho techdesign.
```

## Cho Trường Hợp Chốt Từ GameDesign Sang TechDesign

Prompt gợi ý:

```text
tính năng này ổn rồi.
hãy cập nhật requirement nếu cần, tạo handoff cho techdesign trong docs/agent-handoffs/active/, cập nhật docs/agent-handoffs/QUEUE.md, rồi nói cho mình path handoff để giao techdesign.
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
