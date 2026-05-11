---
title: Server transaction boundary
doc_type: system
status: reviewed
owner: devops
code_status: doc-derived
last_verified: 2026-05-11
source_of_truth:
  - docs/workflow-and-operations/server-transaction-rules.md
related_docs:
  - docs/workflow-and-operations/WORKING_CONTEXT.md
related_code:
  - GameServer/Services
  - GameServer/Network/Handlers
tags:
  - second-brain
  - rules
  - server
  - transaction
---

# Summary

## Purpose

Giữ canonical rule ngắn cho transaction ownership phía server.

## Scope

- transaction owner
- ambient transaction compatibility
- packet-after-commit rule

## Non-goals

- chưa là audit code-complete của toàn bộ server write flows

# Architecture / Flow

## Inputs

- flow business có ghi DB

## Outputs

- write path rõ transaction owner
- notifier/push sau commit

## Runtime flow

1. Tầng orchestration cấp cao mở transaction khi cần.
2. Service con chạy trong ambient transaction nếu đã có.
3. Standalone service cũ có thể tự mở transaction, nhưng phải chịu được ambient transaction.
4. Packet/notifier chỉ nên đi sau commit.

# Rules / Invariants

- một flow business atomic chỉ nên có một transaction owner
- không nested transaction bừa bãi trên cùng DB context
- side effect ghi DB phải dễ trace ở method/layer

# Data / Contracts

## Config

Không áp dụng.

## DB

Rule này điều phối cách ghi DB, không mô tả schema cụ thể.

## Network / messages

Packet result/change không được gửi trước commit path chính.

# Operational Notes

## Failure modes

- nested transaction
- partial write giữa chừng
- push packet trước commit

## Monitoring / logs

Khi audit flow write DB, cần truy transaction owner và thời điểm push packet.

# Verification

## Code checked

Chưa verify trực tiếp một write flow cụ thể trong lượt này.

## Docs checked

- `docs/workflow-and-operations/server-transaction-rules.md`

## Gaps / drift

- trạng thái hiện tại là canonicalized rule từ legacy doc; chưa có conflict report vì chưa chạy audit code đủ sâu
