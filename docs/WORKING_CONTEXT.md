# Working Context

## Mục đích

File này chỉ giữ lại những quyết định và rule dễ quên giữa các session.
Những gì có thể tự suy ra từ code hoặc đọc ở doc chuyên đề thì không lặp lại ở đây.

## Rule cộng tác

- Khi code luôn phải nghĩ tới, tính trước performance cho game client và server. Không code lấy xong, chạy là được. Phải báo user các case sẽ gây hại có thể xảy ra và hướng tối ưu.
- Trước khi sửa `GameServer` hoặc `GameShared`, phải phân tích trước và chỉ sửa khi user thực sự muốn đụng vào phía đó.
- Client chỉ được phụ thuộc `GameShared`, không phụ thuộc `GameServer`.
- Khi thêm service hoặc runtime mới ở server, phải kiểm tra vòng DI trước khi chốt code.
- Đã từng có lỗi startup kẹt ngay tại `provider.GetRequiredService<NetworkServer>()` do vòng:
  - `NetworkServer -> CharacterCombatDeathRecoveryService -> WorldInterestService -> INetworkSender -> NetworkServer`
- Rule bắt buộc:
  - không inject trực tiếp `WorldInterestService`, `CharacterRuntimeNotifier` hoặc service nào phụ thuộc `INetworkSender/NetworkServer`
  - vào các service mà chính `NetworkServer` cần để khởi tạo
  - nếu thật sự cần, resolve lười trong method runtime bằng `IServiceScopeFactory`
- Nếu đổi contract dùng chung trong `GameShared`, phải chạy:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

- Với UI gameplay trong Unity, ưu tiên viết controller hoặc logic trước để user tự dựng hierarchy, prefab, scene trong Editor và kéo ref bằng Inspector.
- Không tự sinh cả UI hierarchy bằng runtime code nếu user chưa yêu cầu rõ kiểu đó.

## Rule trả lời và tài liệu

- Khi viết doc trong repo, dùng tiếng Việt có dấu.
- Khi dẫn file trong câu trả lời, phải dùng markdown link với absolute path có dấu `/` ở đầu để bấm vào mở bằng VS Code thay vì mở Chrome.
- Format chuẩn:

```md
[WorldMapPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldMapPresenter.cs#L1)
```

## Quyết định kiến trúc đã chốt

- Unity scene nên giữ ít:
  - `Bootstrap`
  - `Login`
  - `World`
- `MapTemplate` không đồng nghĩa với Unity scene.
- `zone / khu / instance` là runtime state, không phải Unity scene.
- Hệ chuyển map đi theo `portal -> target spawn point`, không đi theo logic map-to-map cứng.
- Server mới là nơi validate cuối cùng việc dùng portal.
- Map root hiện tại không nên pooling; cứ `Destroy/Instantiate` khi đổi map.

## World scene readiness

- Core world scene hiện đã đi theo `WorldSceneReadinessService`.
- Base class dùng để giảm lặp là:
  - [WorldSceneBehaviour.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneBehaviour.cs)
- Doc chi tiết cơ chế, dependency và rule implement:
  - [world-scene-readiness.md](/F:/PhamNhanOnline/docs/world-scene-readiness.md)

## Khi bắt đầu session mới

1. Đọc file này.
2. Nếu làm phần world scene, đọc thêm:
   - [world-scene-readiness.md](/F:/PhamNhanOnline/docs/world-scene-readiness.md)
3. Nếu có đổi `GameShared`, nhớ sync DLL sang Unity.
