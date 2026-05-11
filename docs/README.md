# Second Brain Docs Hub

Mục tiêu của cây `docs/` là giữ tri thức dự án ở dạng AI-readable, có thể kiểm tra được, và có vòng đời rõ ràng.

## Nguyên tắc lõi

- Docs / ADR / Config Contract = intended design
- Code / DB / Config hiện tại = current implementation
- QA / logs / runtime = actual behavior
- Rule nền: **Docs-first, Code-verified**

## Trạng thái hiện tại

Repo này đã có nhiều docs legacy trước khi chuẩn second-brain được áp vào.

Vì vậy hiện có 2 lớp tài liệu cùng tồn tại:

1. **Legacy docs**
   - các nhóm như `game-design-current-state/`, `reference-and-specs/`, `workflow-and-operations/`, `reports-and-testing/`
   - vẫn được giữ nguyên để không mất dấu lịch sử
2. **Second-brain docs structure**
   - các thư mục chuẩn như `systems/`, `rules/`, `data-design/config-contracts/`, `decisions/`, `change-notes/`, `conflicts/`, `agent-workflows/`, `index/`
   - sẽ là đích đến canonical sau khi migration hoàn tất

## Cách đọc

- Bắt đầu từ `docs/index/project-map.md`
- Nếu cần biết legacy docs hiện nằm đâu và được đánh giá ra sao, xem:
  - `docs/index/legacy-knowledge-inventory.md`
  - `docs/index/legacy-doc-classification.md`
  - `docs/index/legacy-path-mapping.md`
- Nếu cần workflow cho agent, xem `docs/agent-workflows/`
- Nếu cần template chuẩn, xem `docs/templates/`

## Rule migration

- Không xóa docs cũ mất dấu
- Không tự coi legacy doc nào là đúng tuyệt đối nếu chưa audit
- Nếu docs và code mâu thuẫn, tạo conflict report trong `docs/conflicts/`
- Chỉ chuyển source-of-truth sang docs mới khi đã có owner và bằng chứng phù hợp
