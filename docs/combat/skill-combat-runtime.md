---
title: Skill combat runtime
doc_type: system
status: verified
owner: devops
code_status: partially-verified
last_verified: 2026-05-11
source_of_truth:
  - docs/reference-and-specs/SKILL_SYSTEM_COMBAT_FLOW.md
  - GameServer/Network/Handlers/AttackEnemyHandler.cs
  - GameServer/Runtime/SkillExecutionService.cs
related_docs:
  - docs/workflow-and-operations/server-transaction-rules.md
related_code:
  - GameServer/World/MapInstance.cs
  - GameServer/Runtime/GameLoop.cs
  - GameServer/World/SkillExecutionRuntimeTypes.cs
tags:
  - second-brain
  - combat
  - skills
---

# Summary

**Graph links:** [[runtime-knowledge-map]] · [[server-validation-and-runtime-rules]] · [[server-runtime-architecture]] · [[game-configs-phase1]]

## Purpose

Tài liệu hóa canonical runtime flow cho combat skill data-driven phía server.

## Scope

- validate yêu cầu dùng skill
- resolve skill đang equip
- enqueue pending execution
- resolve cast release / impact theo scheduler
- apply effect lên player hoặc enemy
- phát packet đồng bộ kết quả xuống client

## Non-goals

- không bao quát toàn bộ UI/presentation client
- không mô tả mọi effect edge case chưa được verify trong code

# Architecture / Flow

## Inputs

- `AttackEnemyPacket`
- skill definition từ combat definition catalog
- target runtime snapshot từ world instance

## Outputs

- `AttackEnemyResultPacket`
- runtime effect application
- impact event / sync packet cho client

## Runtime flow

1. `AttackEnemyHandler` kiểm tra player/session/world instance.
2. Handler dùng `SkillService.ResolveEquippedSkillForCombatAsync(...)` để resolve skill đang equip.
3. Handler validate cooldown, target type, target compatibility, range, interaction gate.
4. Nếu hợp lệ, handler enqueue skill execution vào instance runtime.
5. Khi tới cast release / impact, `SkillExecutionService` resolve effect theo `trigger_timing`.
6. Effect được áp lên player hoặc enemy theo `target_scope` và `effect_type`.

# Rules / Invariants

- server là nơi validate cuối cùng cho combat skill
- target area/map-wide hiện chưa được support trong flow này
- trạng thái combat runtime không phải dữ liệu persist lâu dài
- target `Self` đi theo đường apply-to-caster

# Data / Contracts

## Config

Flow phụ thuộc combat definitions được load từ dữ liệu skill/effect runtime.

## DB

Legacy design doc nói dữ liệu đi từ `skills` và `skill_effects`.
Chưa verify trực tiếp lớp truy cập DB trong lượt này, nên giữ claim này theo source legacy.

## Network / messages

- input: `AttackEnemyPacket`
- result/failure: `AttackEnemyResultPacket`
- downstream sync: xem legacy combat flow doc và runtime event broadcaster

# Operational Notes

## Failure modes

- player chưa vào world
- target invalid / out of range
- skill cooldown
- target type chưa support

## Monitoring / logs

Nên kiểm tra thêm broadcast/runtime event path nếu cần audit sâu hơn phần packet cuối luồng.

# Verification

## Code checked

- `GameServer/Network/Handlers/AttackEnemyHandler.cs`
- `GameServer/Runtime/SkillExecutionService.cs`

## Docs checked

- `docs/reference-and-specs/SKILL_SYSTEM_COMBAT_FLOW.md`

## Gaps / drift

- legacy doc nói rõ hơn về một số packet broadcast cuối luồng; phần broadcaster chưa verify trực tiếp ở lượt này
