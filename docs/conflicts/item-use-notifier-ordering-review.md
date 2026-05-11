---
title: Item use notifier ordering review
doc_type: conflict-report
status: open
date: 2026-05-11
owner: devops
severity: low
doc_source:
  - docs/rules/server-transaction-boundary.md
code_source:
  - GameServer/Network/Handlers/UseItemHandler.cs
tags:
  - second-brain
  - conflict
  - inventory
---

# Conflict Summary

Không thấy bằng chứng transaction commit sai trong `UseItem` flow đã audit.
Tuy nhiên có một điểm cần review: handler gửi `UseItemResultPacket` trước, sau đó mới gọi `NotifyOwnedSkillsChanged(...)` nếu có skill snapshot đổi.

# Observed Documentation Claim

Rule transaction chung nói packet/notifier không nên đi trước commit path chính.

# Observed Code / Runtime Reality

`ItemUseService.UseAsync(...)` hoàn tất service flow bên trong transaction wrapper trước khi handler gửi packet.
Vì vậy chưa có bằng chứng packet đi trước commit.
Nhưng within post-commit outbound ordering, `UseItemResultPacket` đi trước `NotifyOwnedSkillsChanged(...)`.

# Impact

Hiện chưa chứng minh là bug.
Nhưng nếu client nào đó ngầm phụ thuộc skill-changed packet tới trước hoặc cùng lúc với result interpretation, có thể phát sinh assumption drift.

# Recommended Resolution

- xác nhận contract packet ordering mong muốn cho item use + skill sync
- nếu ordering hiện tại là chủ đích, ghi rõ vào canonical doc
- nếu không, sửa flow và thêm change note

# Resolution Status

- [ ] docs updated
- [ ] code updated
- [ ] owner assigned
