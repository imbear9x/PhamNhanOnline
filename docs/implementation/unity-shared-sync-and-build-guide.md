---
title: Unity shared sync and build guide
doc_type: implementation-note
status: verified
owner: dev
code_status: code-verified
last_verified: 2026-05-11
source_of_truth:
  - docs/workflow-and-operations/UNITY_TOOLING_NOTES.md
  - scripts/sync-gameshared-to-unity.ps1
  - scripts/verify-solution-build.ps1
related_docs:
  - docs/rules/client-state-sync-runtime.md
  - docs/systems/auth-character-world-phase1.md
tags:
  - unity
  - tooling
  - gameshared
  - build
---

# Summary

`GameShared` là nguồn chân lý contract dùng chung giữa server và Unity client. Khi thay đổi shared packet/model/serializer contract, phải sync output `netstandard2.1` sang Unity plugin và verify build theo workflow chuẩn.

# Canonical rules

## Shared source-of-truth rule

- server tham chiếu `GameShared` trực tiếp dưới dạng project
- Unity client không tham chiếu `GameServer`
- Unity dùng output `netstandard2.1` của `GameShared`
- không copy tay packet/model giữa server và client

## When sync is required

Phải sync `GameShared` sang Unity mỗi khi đổi:

- packet
- shared model
- `MessageCode`
- serializer/contract dùng chung

## Sync command

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

Theo script hiện tại, bước sync sẽ:

1. build `GameShared` cho `netstandard2.1`
2. copy `GameShared.dll`, `GameShared.pdb`, `GameShared.xml` vào `Assets/Plugins/GameShared`
3. copy `LiteNetLib.dll`, `LiteNetLib.xml` vào `Assets/Plugins/LiteNetLib`

## Generated project rule

`Assembly-CSharp.csproj` là file Unity generate.

Hệ quả thực tế:

- source-of-truth vẫn là script trong `Assets/`
- nếu vừa thêm file `.cs` mới mà Unity chưa regenerate project thì CLI build có thể không thấy file đó
- không nên coi một lỗi build CLI kiểu này là bằng chứng code gameplay sai ngay lập tức

## When Unity regenerate/open is required

Nên mở Unity hoặc để Unity regenerate project khi:

- vừa thêm script `.cs` mới trong `Assets/`
- vừa thêm folder/script `.meta`
- vừa đổi asmdef hoặc compile structure của Unity

## Verification command

```powershell
powershell -File .\scripts\verify-solution-build.ps1
```

Theo script hiện tại, verify build sẽ:

- build `GameServer/GameServer.csproj`
- build `ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj`

# Recommended workflow

1. sửa code server/client
2. nếu có đổi `GameShared`, chạy sync script
3. nếu có thêm script Unity mới, mở Unity để regenerate project
4. chạy verify build script

# Common pitfalls

- sync `GameShared` xong chưa chắc Unity compile lại ngay nếu Editor chưa refresh
- build `Assembly-CSharp.csproj` chỉ đáng tin khi Unity đã regenerate project
- nếu build CLI không thấy script Unity mới, hãy kiểm tra generated project trước khi kết luận code sai
