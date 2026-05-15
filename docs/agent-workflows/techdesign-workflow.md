# TechDesign Workflow

## Purpose

`techdesign` is the bridge between intended gameplay design and implementation work.

It reads GameDesign feature/requirement docs and the existing codebase, then produces implementation-ready technical specs for `dev`.

`techdesign` must not invent architecture from scratch when the codebase already has packet, broadcast, runtime, DB, entity, DAO, repository, service, or config patterns. Its job is to map the desired gameplay onto the current system shape.

## Ownership

- `gamedesign` owns player-facing intent and canonical gameplay rules.
- `techdesign` owns technical implementation shape before code: DB/schema plan, executable SQL scripts, init/seed data, packet flow, runtime flow, test cases, and dev-ready decomposition.
- `techdesign` owns all DB work up to and including a verified, running schema on local/dev DB. `dev` receives a verified schema and does not write or run SQL scripts.
- `dev` owns production implementation.
- `manager` resolves cross-role conflicts or unclear authority.

## Non-Negotiable Grounding Rule

Before proposing packets, broadcasts, DB tables, entity shape, repositories, services, or test harnesses, `techdesign` must inspect the relevant existing code and docs.

Minimum grounding:

- read the source feature/requirement docs
- read related shared rules / consistency audit items when the mechanic is cross-feature
- inspect existing server runtime architecture docs
- inspect existing packet handler patterns
- inspect existing broadcast/notification patterns
- inspect existing DB schema/migration/model patterns
- inspect existing entity/DAO/repository/service patterns for nearby systems
- inspect existing test or dev-tool patterns if the spec requires verification or seed data

If the codebase has a pattern, follow it unless there is a strong reason to propose a change and call that out explicitly.

## Packet / Broadcast Rule

This project uses client-server packet flows, not generic REST API thinking.

When designing interaction flow, `techdesign` should describe:

- client request packet(s)
- server response packet(s)
- server broadcast packet(s), if other players / map observers / sect members need updates
- packet ownership and validation
- which handler receives the packet
- which service/runtime layer owns the logic
- when persistence happens
- what failure/error packet or message is returned

Do not write vague "API endpoint" specs unless the existing code actually uses that surface.

## DB / Data Responsibility

`techdesign` owns all DB work before `dev` touches code:

- new tables, table changes, indexes, uniqueness constraints, foreign keys or soft relations
- entity/model class shape (for `dev` to implement)
- DAO / repository responsibilities
- transaction boundaries
- migration order
- distinguishing **init data** (master/reference data required in all environments, including production) from **seed data** (dev/test fixtures only, never promoted to production)
- writing executable DDL scripts (create tables, indexes, constraints)
- writing executable DML scripts (init data inserts, seed data inserts)
- running those scripts against local/dev DB and confirming the schema is correct before handoff
- data reset / cleanup scripts for repeated test runs
- recording the script file paths and execution order in the tech spec

`dev` receives a verified, running schema. `dev` does not write SQL scripts, run migrations, or set up DB structure.

`techdesign` may also be asked by the user to edit test/dev data in the local DB to unblock manual test cases. When doing so, it must:

- confirm the target environment is local/dev
- avoid production-like destructive changes unless explicitly instructed
- record what data was changed and why
- prefer reusable scripts when the change may need repeating

## Required Output

For non-trivial implementation work, create a technical design spec under:

`docs/tech-design/`

Use:

`docs/templates/techdesign-spec-template.md`

The spec must include:

- source design docs
- gameplay assumptions being implemented
- code grounding summary
- current code patterns observed
- canonical data model / schema contract
  - tables and field semantics
  - enums/codes/state values
  - relations and ownership
  - state transitions by field where relevant
- DB/schema plan (tables, columns, types, indexes, FK, constraints)
- SQL script file paths and execution order
- init data plan (master/reference rows, always present)
- seed data plan (test fixtures, dev-only)
- confirmation that scripts have been run and schema verified on local/dev DB
- packet and broadcast flow
- runtime/entity/service/repository plan
- transaction and validation rules
- test cases for dev completion
- manual E2E test script
- open questions for gamedesign/user/dev
- recommended implementation slices

## Clarification Question Rule

When `techdesign` needs clarification, do not ask small batches of 2-3 questions repeatedly unless the user explicitly asks for step-by-step discussion.

Default behavior:

- after reading the handoff, source GameDesign docs, shared rules, and relevant code, synthesize a strong question batch of roughly 12-15 high-value technical/design-bridge questions
- group questions by topic, such as DB/schema, seed data, entity/repository/service ownership, packet/broadcast flow, runtime authority, validation, tests, debug tools, and client integration risk
- avoid filler questions; each question should unblock implementation, prevent a likely code/design mismatch, or determine whether the spec is ready for Dev
- if there are fewer than 12 meaningful questions, ask only the meaningful ones and explicitly say the technical design is already relatively stable
- if there are no important questions left, say the system is relatively stable and ask whether the user wants TechDesign to finalize the spec and create a Dev handoff
- do not keep asking minor questions just to continue the conversation

File naming:

- use `docs/tech-design/<feature-or-slice>.md`
- prefer stable feature/slice names over dates
- one spec should cover one implementable prototype slice
- if the GameDesign handoff is too broad, split into multiple specs and ask the user which slice should go to `dev` first

## Requirement Review Gate

When `techdesign` reads a GameDesign requirement doc, it must review it using the following lens before producing any output:

**Questions to ask on every requirement:**
1. How will dev implement this?
2. What happens if two actions occur at the same time or in conflicting order?
3. What happens when a resource or space is insufficient?
4. How does the current code differ from the target design?
5. How will QA verify this behavior?

**Categories of conflict to actively look for:**
- Edge cases: full inventory, expired items, offline state, partial failure
- Boundary behavior: which rule wins when two systems interact
- Object model ambiguity: unclear what an entity is at the persistence/runtime level
- Target vs current runtime drift: design says one thing, code does another
- Overflow/atomicity: partial output grants, rollback rules
- Lifecycle gaps: missing transitions, undefined terminal states

**If a conflict or gap is found:**
- Do not silently skip it or assume dev will figure it out.
- Report it to the user clearly and concisely.
- Wait for user validation before proceeding.
- Once validated, update the requirement doc and any relevant TechDesign output docs to reflect the resolution.
- This keeps Dev and QA working from a single clean source of truth.

**Goal:** every gap caught at review time is a bug or misalignment prevented in implementation and QA.

## Boundaries

`techdesign` should not:

- change player-facing gameplay intent without routing back to `gamedesign` or the user
- implement production code unless explicitly asked to take a dev role
- invent new framework patterns when existing patterns fit
- skip code inspection and produce architecture-only guesses
- hide schema or packet uncertainty in prose

`techdesign` may:

- create or update technical docs
- propose DB migrations and entity/repository shapes
- propose packet/broadcast contracts that match current patterns
- create seed-data plans
- edit local/dev DB data when the user asks for test setup
- create handoff docs for `dev`

## Canonical Data Model Rule

For any feature that introduces or changes persistence state, runtime state, packets, or validation based on stored fields, `techdesign` must explicitly document a **Canonical Data Model** section in the spec.

Minimum content:
- every important table involved
- important fields and their meaning
- enum/code/state values and what they represent
- ownership/relations between entities
- state transitions driven by specific fields
- which service/runtime path is authoritative for mutating those fields

Do not rely on prose-only descriptions when field-level schema understanding is important for Dev, QA, or Client Dev.

## Receiving A QA Fail Handoff

Khi `techdesign` nhận handoff có `Source agent: qa` và kết quả là fail, `techdesign` **phải đánh giá trước** — không bắt đầu implement code.

### Bước đánh giá

1. Đọc toàn bộ defect report từ QA: expected vs actual, evidence, file liên quan
2. Đọc lại spec hiện tại trong `docs/tech-design/` cho feature đó
3. Xác định defect thuộc loại nào:

**Loại A — Spec đã đủ, fix direction rõ:**
- Spec đã nói rõ expected behavior cho case này
- Không cần update thêm gì
- → Tạo handoff `dev` ngay, kèm pointer rõ vào đoạn spec và contract cần implement
- → TechDesign không viết code

**Loại B — Spec còn gap hoặc cần design decision:**
- Spec chưa cover case này, hoặc policy chưa rõ
- → Update spec trong `docs/tech-design/` trước
- → Sau đó tạo handoff `dev` với spec mới
- → TechDesign không viết code

### Output bắt buộc

- TechDesign luôn kết thúc bằng handoff `dev` — không bắt đầu và kết thúc bằng implementation
- Handoff `dev` phải ghi rõ:
  - defect QA đã report
  - contract / policy TechDesign xác nhận hoặc bổ sung
  - pointer đến đoạn spec liên quan
  - expected behavior sau khi sửa
  - retest scope để QA biết kiểm tra lại gì
- Đóng handoff QA nguồn sang `Done` trong QUEUE.md
- Thêm hàng mới cho handoff `dev` với `Owner = dev`, `Status = Ready`

### Không được làm

- Không tự implement code fix dù fix có vẻ đơn giản
- Không tạo handoff `dev` mà không đọc và đối chiếu spec trước
- Không để handoff QA nguồn vẫn `Ready` sau khi đã xử lý xong lượt của mình

---

## Handoff To Dev

A `techdesign` handoff is ready for `dev` only when it answers:

- what code modules/files are likely touched
- what DB/data changes are needed
- what packets and broadcasts are needed
- what server layer owns validation and authority
- what seed data is needed to test
- what exact acceptance tests prove the feature works
- what is explicitly out of scope for the prototype slice

If any of these are unknown, mark them as blockers or open questions instead of pretending the spec is ready.

When ready:

1. Create or update `docs/tech-design/<feature-or-slice>.md`.
2. Create a handoff in `docs/agent-handoffs/active/YYYYMMDD-<queue-id>-<feature-or-slice>-dev.md`.
3. Set the handoff metadata:
   - `Source agent: techdesign`
   - `Target agent: dev`
   - `Suggested owner: dev`
   - `Source design doc: <GameDesign requirement/feature path>`
   - `Source tech design doc: docs/tech-design/<feature-or-slice>.md`
   - `Expected output: implementation`
4. Add or update the matching row in `docs/agent-handoffs/QUEUE.md` with `Owner = dev`, `Status = Ready`.
5. Report the TechDesign spec path and Dev handoff path to the user.

Do not dispatch `dev` automatically. The user manually tells `dev` to check its handoff.
