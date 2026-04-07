# Quy Ước Sync GameShared Và Tooling Unity

## Mục tiêu

Tài liệu này gom lại các lưu ý thao tác dễ quên khi làm việc với `GameShared`, build CLI, và Unity project.

## 1. `GameShared` là nguồn chân lý dùng chung

- Server tham chiếu `GameShared` trực tiếp dưới dạng project.
- Client Unity không được tham chiếu `GameServer`.
- Unity lấy output `netstandard2.1` của `GameShared`.
- Không copy tay source packet/model giữa server và client.

Thiết lập hiện tại:

- `GameShared` target:
  - `net8.0`
  - `netstandard2.1`
- `PacketSerializer` không còn phụ thuộc `dynamic`.
- Unity đang dùng `LiteNetLib` từ thư mục plugin.

## 2. Khi nào phải sync `GameShared` sang Unity

Phải chạy sync mỗi khi đổi:

- packet
- shared model
- `MessageCode`
- serializer/contract dùng chung

Lệnh:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

Script hiện tại sẽ:

1. build `GameShared` cho `netstandard2.1`
2. copy vào `ClientUnity/PhamNhanOnline/Assets/Plugins/GameShared`:
   - `GameShared.dll`
   - `GameShared.pdb`
   - `GameShared.xml`
3. copy vào `ClientUnity/PhamNhanOnline/Assets/Plugins/LiteNetLib`:
   - `LiteNetLib.dll`
   - `LiteNetLib.xml`

## 3. `Assembly-CSharp.csproj` là file generated

File [Assembly-CSharp.csproj](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj) được Unity generate.

Ý nghĩa thực tế:

- Source of truth vẫn là script trong `Assets/`.
- Khi thêm script Unity mới, Unity cần regenerate project để compile list được cập nhật đầy đủ.
- Nếu chỉ `dotnet build` ngay khi vừa thêm file `.cs` mới mà chưa regenerate project, build CLI có thể không nhìn thấy file mới.

## 4. Khi nào cần mở Unity hoặc regenerate project

Các case nên mở Unity trước khi verify build:

- vừa thêm script `.cs` mới trong `Assets/`
- vừa thêm folder/script `.meta`
- vừa đổi asmdef hoặc cấu trúc compile của Unity

## 5. Script tooling hiện có

- [sync-gameshared-to-unity.ps1](/F:/PhamNhanOnline/scripts/sync-gameshared-to-unity.ps1)
  - build `GameShared` cho Unity và copy plugin cần thiết
- [solution.ps1](/F:/PhamNhanOnline/scripts/solution.ps1)
  - entry script đang có sẵn trong repo
- [verify-solution-build.ps1](/F:/PhamNhanOnline/scripts/verify-solution-build.ps1)
  - build nhanh cả `GameServer` và `Assembly-CSharp`

Ví dụ:

```powershell
powershell -File .\scripts\verify-solution-build.ps1
```

## 6. Workflow khuyến nghị

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

## 7. Những chỗ dễ hiểu nhầm

- Sync `GameShared` xong chưa chắc Unity compile lại ngay nếu Editor chưa refresh.
- `dotnet build ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj` chỉ đáng tin khi Unity đã regenerate project.
- Nếu build CLI không thấy script Unity mới, đừng vội kết luận code sai. Hãy kiểm tra xem project generated đã được cập nhật chưa.
