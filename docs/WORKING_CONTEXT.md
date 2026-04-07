# Bối Cảnh Làm Việc Và Quy Ước Nhanh

## Mục đích

File này chỉ giữ lại các quyết định và rule dễ quên giữa các session.
Những gì có thể tự suy ra từ code hoặc đọc ở doc chuyên đề thì không lặp lại ở đây.

## Quy ước cộng tác

- Khi code phải tính trước performance cho cả client và server. Không chốt theo kiểu chỉ cần chạy được.
- Trước khi sửa `GameServer` hoặc `GameShared`, phải phân tích phạm vi ảnh hưởng trước. Chỉ sửa khi thật sự cần đụng vào phía đó.
- Client chỉ được phụ thuộc `GameShared`, không phụ thuộc `GameServer`.
- Với UI gameplay trong Unity, ưu tiên viết controller/logic trước để user tự dựng hierarchy, prefab, scene trong Editor và kéo ref qua Inspector.
- Không tự sinh cả UI hierarchy bằng runtime code nếu user chưa yêu cầu rõ kiểu đó.

## Quy ước khi chỉnh DB

- Nếu thay đổi schema bảng:
  - cập nhật entity liên quan
  - cập nhật repository liên quan
  - cập nhật DTO/model liên quan nếu có
- Luôn cập nhật lại file schema gốc:
  - `database/phamnhan_online.sql`
- Nếu thay đổi schema hoặc dữ liệu cần migrate một lần:
  - tạo thêm file SQL chạy được trực tiếp trong `database/`
  - đặt tên theo format `migrate_YYYYMMDD_HHmm_mo_ta_ngan.sql`
- Nếu chỉ chỉnh dữ liệu gameplay/config, ưu tiên ghi rõ trong doc hoặc migration thay vì để knowledge rơi mất sau session.

## Quy ước DI server

- Khi thêm service hoặc runtime mới ở server, phải kiểm tra vòng DI trước khi chốt code.
- Đã từng có lỗi startup kẹt ngay tại `provider.GetRequiredService<NetworkServer>()` do vòng:
  - `NetworkServer -> CharacterCombatDeathRecoveryService -> WorldInterestService -> INetworkSender -> NetworkServer`
- Rule bắt buộc:
  - không inject trực tiếp `WorldInterestService`, `CharacterRuntimeNotifier` hoặc service nào phụ thuộc `INetworkSender/NetworkServer`
  - vào các service mà chính `NetworkServer` cần để khởi tạo
  - nếu thật sự cần, resolve lười trong method runtime bằng `IServiceScopeFactory`

## Quy ước client Unity

- Phải phân biệt rõ:
  - ref lõi / bắt buộc như `WorldSceneController`, `WorldMapPresenter`, `WorldTargetActionController`, `WorldLocalPlayerPresenter`, panel view bắt buộc
  - ref phụ / optional như text phụ, badge, status text, root hiển thị bổ sung
- Rule bắt buộc:
  - ref lõi nếu thiếu thì không được im lặng `return` hoặc tự chữa cháy theo kiểu `không có thì thôi`
  - phải `ClientLog.Error` sớm để lộ lỗi setup scene/prefab
  - chỉ các ref phụ dạng hiển thị thêm mới được phép `có thì hiện, không có thì thôi`
- Nếu một component world phải auto-add ở `WorldRoot` để giữ backward compatibility, vẫn phải log lỗi rõ là scene đang thiếu component bắt buộc.

## Quy ước world scene readiness

- Core world scene đi theo `WorldSceneReadinessService`.
- Base class để giảm lặp là:
  - [WorldSceneBehaviour.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneBehaviour.cs)
- Doc chi tiết:
  - [world-scene-readiness.md](/F:/PhamNhanOnline/docs/world-scene-readiness.md)

## Quy ước presentation replication

- Không làm generic sync kiểu gắn một script Unity để sync mọi component raw.
- Hướng đúng là:
  - server authoritative cho gameplay
  - semantic packet/state cho gameplay quan trọng
  - client có một lớp presentation replication để chuẩn hóa event/state presentation dùng chung
- Nền hiện tại nằm ở:
  - [ClientPresentationReplicationService.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationService.cs)
- Lớp này mới là foundation client-side, chưa phải full protocol replication mới.

## Quy ước GameShared và build

- Nếu đổi contract dùng chung trong `GameShared`, phải chạy:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

- `ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj` là file generated của Unity. Nó hữu ích cho `dotnet build` local nhưng không phải source of truth cho compile list dài hạn.
- Khi thêm script `.cs` mới ở Unity:
  1. mở Unity hoặc regenerate project để Unity cập nhật compile list
  2. nếu cần verify bằng CLI, có thể dùng script build/verify trong `scripts/`
- Script verify nhanh hiện có:
  - [verify-solution-build.ps1](/F:/PhamNhanOnline/scripts/verify-solution-build.ps1)
- Doc thao tác chi tiết:
  - [UNITY_TOOLING_NOTES.md](/F:/PhamNhanOnline/docs/UNITY_TOOLING_NOTES.md)

## Quy ước tài liệu

- Khi viết doc trong repo, dùng tiếng Việt có dấu.
- Nếu tài liệu là roadmap hoặc draft tương lai, phải ghi rõ đó không phải trạng thái đã implement.
- Nếu gộp/xóa doc cũ, ưu tiên giữ lại phần giải thích chi tiết còn đúng thay vì viết lại ngắn gọn một cách mất ngữ cảnh.

## Quyết định kiến trúc đã chốt

- Unity scene nên giữ ít:
  - `Bootstrap`
  - `Login`
  - `World`
- `MapTemplate` không đồng nghĩa với Unity scene.
- `zone / khu / instance` là runtime state, không phải Unity scene.
- Hệ chuyển map đi theo `portal -> target spawn point`, không đi theo logic map-to-map cứng.
- Server là nơi validate cuối cùng việc dùng portal.
- Map root hiện tại không nên pooling; cứ `Destroy/Instantiate` khi đổi map.

## Khi bắt đầu session mới

1. Đọc file này.
2. Nếu làm phần world scene, đọc thêm:
   - [world-scene-readiness.md](/F:/PhamNhanOnline/docs/world-scene-readiness.md)
3. Nếu có đổi `GameShared`, nhớ sync DLL sang Unity.
4. Nếu làm feature presentation mới, kiểm tra trước xem có thể đi qua `presentation replication` hay chưa thay vì mở thêm một đường sync ad-hoc.
