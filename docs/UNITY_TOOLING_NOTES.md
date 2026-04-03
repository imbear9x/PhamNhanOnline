# Tooling Notes

## Mục tiêu

Tài liệu này ghi lại các lưu ý tooling dễ gây hiểu lầm khi làm việc với repo này.

## 1. `Assembly-CSharp.csproj` là file generated

File [Assembly-CSharp.csproj](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj) được Unity generate.

Ý nghĩa thực tế:
- Source of truth vẫn là script trong `Assets/`.
- Khi thêm script Unity mới, Unity cần regenerate project để compile list được cập nhật đầy đủ.
- Nếu chỉ `dotnet build` ngay khi vừa thêm file `.cs` mới mà chưa regenerate project, build CLI có thể không nhìn thấy file mới.

## 2. Khi nào cần mở Unity hoặc regenerate project

Các case nên mở Unity trước khi verify build:
- vừa thêm script `.cs` mới trong `Assets/`
- vừa thêm folder/script `.meta`
- vừa đổi asmdef hoặc cấu trúc compile của Unity

## 3. Script tooling hiện có

- [sync-gameshared-to-unity.ps1](/F:/PhamNhanOnline/scripts/sync-gameshared-to-unity.ps1)
  - build `GameShared` cho Unity và copy DLL sang `Assets/Plugins`
- [solution.ps1](/F:/PhamNhanOnline/scripts/solution.ps1)
  - entry script đang có sẵn trong repo
- [verify-solution-build.ps1](/F:/PhamNhanOnline/scripts/verify-solution-build.ps1)
  - build nhanh cả `GameServer` và `Assembly-CSharp`

Ví dụ:

```powershell
powershell -File .\scripts\verify-solution-build.ps1
```

## 4. Khuyến nghị workflow

1. Sửa code server/client.
2. Nếu có đổi `GameShared`, chạy:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

3. Nếu có thêm script Unity mới, mở Unity để regenerate project.
4. Verify build:

```powershell
powershell -File .\scripts\verify-solution-build.ps1
```

## 5. Naming cleanup hiện tại

Pass cleanup này chỉ xử lý naming ở phạm vi an toàn:
- chuẩn hóa tên partial file theo trách nhiệm như `*.Actions.cs`, `*.ViewState.cs`, `*.Execution.cs`, `*.Visuals.cs`
- bổ sung doc và script tooling có tên mô tả đúng việc nó làm

Các rename path lớn như `CientTest` chưa đổi ở pass này vì ảnh hưởng rộng tới solution path, project reference và tooling hiện có. Nếu muốn đổi, nên làm thành một pass riêng.
