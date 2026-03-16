# Working Context

## Mục đích

File này dùng để giữ ngữ cảnh làm việc giữa nhiều session Codex.
Mỗi session mới nên đọc file này trước khi tiếp tục.
Cuối mỗi buổi có thể cập nhật thêm các quyết định mới, trạng thái hiện tại và việc tiếp theo.

## Trọng tâm hiện tại của dự án

- Tạm dừng tối ưu/scaling sâu thêm trong một thời gian.
- Ưu tiên làm Unity client để kiểm chứng flow thật với GameServer.
- Ưu tiên logic chạy đúng trước, đồ họa và UI đẹp tính sau.

## Quy tắc cộng tác

- Trước khi sửa `GameServer` hoặc `GameShared`, phải phân tích trước.
- Chỉ sửa `GameServer` hoặc `GameShared` khi user đồng ý rõ ràng.
- Có thể sửa `ClientUnity` hoặc `TestClient` để test/verify khi hợp lý.
- Commit theo nhóm việc rõ ràng, không gom lẫn nhiều mục đích khác nhau.
- Client chỉ được phụ thuộc `GameShared`, không phụ thuộc `GameServer`.
- Nếu cần đổi protocol dùng chung, ưu tiên sửa trong `GameShared`.

## Các quyết định kiến trúc đã chốt

- `GetCharacterData` không còn là packet vào world.
- `GetCharacterData` chỉ dùng để query snapshot nhân vật.
- `EnterWorldPacket` mới là packet dùng để select character và vào world.
- Login flow dùng như sau:
  - login thành công
  - lấy `GetCharacterList`
  - nếu list rỗng thì hiện create character
  - nếu list có character thì lấy character đầu tiên và gọi `EnterWorldPacket`
- Sau khi `EnterWorld` thành công, client mới load `World` scene.
- `MapTemplate` không đồng nghĩa với Unity scene.
- Unity scene nên giữ ít:
  - `Bootstrap`
  - `Login`
  - `World`
- `zone/khu/instance` là runtime state, không phải Unity scene.

## Trạng thái server

- Roadmap scaling đã đi qua phase 1, 2, 3.
- Phase 4 đã có nền tảng:
  - `MapTemplate`
  - `MapDefinition`
  - `MapInstance`
  - interest management theo map/vùng/khoảng cách
- Hệ thống map hiện hỗ trợ:
  - `Player Home` là map riêng
  - map public có chia khu/zone
  - zone rỗng có thể bị hủy khỏi memory
- DB local đã đủ migration map/zone:
  - `current_zone_index`
  - `map_templates`
  - `map_template_adjacent_maps`

## Trạng thái client

- Đã tạo Unity project trong `ClientUnity/PhamNhanOnline`.
- Đã có bộ khung `Assets/Game`.
- `GameShared` đã được làm Unity-friendly.
- Có script sync DLL dùng chung:
  - `scripts/sync-gameshared-to-unity.ps1`
- Scene/client flow hiện có:
  - `Bootstrap`
  - `Login`
  - `World`
- Login UI/controller hiện đã hỗ trợ:
  - login
  - nhận biết account không có character
  - create character
  - `Open World`

## Ghi chú kỹ thuật quan trọng

- Unity đang dùng DLL sync từ `GameShared`, không copy source packet sang client.
- Sau mỗi thay đổi trong `GameShared` mà client cần dùng, phải chạy:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

- VS Code đã được chỉnh để:
  - ẩn file `.meta`
  - `explorer.autoReveal = true`
- Cách start server local đã verify ổn để smoke test:

```powershell
Start-Process '.\GameServer\bin\Debug\net8.0\GameServer.exe' -WorkingDirectory '.\GameServer\bin\Debug\net8.0'
```

## Account / dữ liệu test đang dùng

- Account đã tạo thành công để test:
  - username: `admin123456`
  - password: `admin@admin`
  - email đăng ký đã dùng: `admin123456@test.com`
  - character: `Admin123456`
- Account mới để test nhánh create character:
  - username: `flowcreate0316a`
  - password: `Flow@12345`
  - email đăng ký đã dùng: `flowcreate0316a@test.com`
  - character: `FlowHero0316`

## Các lưu ý hiện tại

- Build solution và sync DLL đã pass.
- Đã verify được lại runtime bằng `TestClient`.
- Hai nhánh đã pass:
  - account đã có character: `login -> get list -> EnterWorld -> MapJoined`
  - account chưa có character: `register -> login -> get list -> create character -> EnterWorld -> MapJoined`
- `admin123456` hiện nhận `EnterWorld:CharacterLifespanExpired`, nên đây cũng là account tốt để test nhánh character bị giới hạn hành động.

## Cách bắt đầu session tiếp theo

Khi bắt đầu session mới:

1. Đọc file này.
2. Đọc thêm:
   - `docs/UNITY_CLIENT_SCENE_SETUP.md`
   - `docs/UNITY_GAMESHARED_WORKFLOW.md`
3. Xác nhận mục tiêu của buổi làm việc tiếp theo.
4. Nếu có đổi `GameShared`, nhớ sync lại DLL sang Unity.

## Các bước nhiều khả năng sẽ làm tiếp

- Test lại Unity login/create character/open world flow trong Editor.
- Dựng render world/player tối thiểu trong `World` scene.
- Quan sát packet `MapJoined` và observer packets trên client.
- Chỉ quay lại sửa server nếu client expose ra vấn đề thật sự.

## Thói quen cập nhật cuối buổi

Cuối mỗi buổi, nên bổ sung:

- việc đã xong
- quyết định mới vừa chốt
- bug/blocker mới
- việc ưu tiên cho buổi tiếp theo

## Tooling note
- If apply_patch fails with a Windows sandbox refresh error, switch to shell-based file editing immediately instead of retrying multiple times.

## Session update 2026-03-16

- Unity client world flow da chay duoc: login -> load scene `World` -> spawn map -> spawn local player -> camera follow.
- Da them `ClientMapCatalog`, `WorldMapPresenter`, `WorldLocalPlayerPresenter`, `WorldCameraFollowController`, `ClientMapView`.
- Moi map prefab nen co `ClientMapView` + `PlayableBounds` (`BoxCollider2D` trigger) de quy doi server coords -> Unity world coords va clamp camera.
- Server/client da chot he toa do logic map, khong dung art size de lam gameplay coords. `MapCatalog.cs` da duoc doi sang scale logic (`Player Home` = `1000 x 500`, `Starter Plains` = `1000 x 1000`). DB local cung da duoc sua tuong ung.
- Local movement/action da co trong client:
  - `LocalCharacterActionController`
  - `LocalCharacterActionConfig`
  - `PlayerView`
- Flow local action hien tai:
  - di trai/phai + quay huong dung
  - bay len / hover / roi
  - dang roi thi phai cham dat moi bay lai
  - hover dung yen tren khong, bay ngang roi dung lai thi hover timer reset lai tu dau
  - attack local co ban
- Animator locomotion dang phu hop voi animator controller cua prefab free:
  - `MoveSpeed` parameter duoc set tu code cho `Idle/Run`
  - `Jump/Fly/Fall` la optional states, co state thi play, khong co thi bo qua
- Da bo tri san hook cho logic sau nay:
  - `CanUseFlight()`
  - `ActivateFlightPresentation()`
  - `DeactivateFlightPresentation()`
  - `OnFlightPresentationActivated()`
  - `OnFallingPresentationActivated()`
- Local player khong nen bi authoritative server position keo nguoc lien tuc. `WorldLocalPlayerPresenter` hien chi snap khi force hoac lech qua nguong.
- `LocalCharacterActionController` chi nen dung cho local player. Remote players ve sau nen co presenter/controller rieng, nhe hon, khong doc input va khong chay full local movement logic nay.
- Da them smoothing cho bay/roi:
  - `Rigidbody2D.Interpolate`
  - `CollisionDetectionMode2D.Continuous`
  - `VerticalVelocityChangeRate` trong `LocalCharacterActionConfig`

## Session follow-up

- Viec hop ly nhat cho buoi sau:
  - tach input source khoi `LocalCharacterActionController` de de support mobile touch / virtual joystick
  - hoac lam remote player presenter rieng
  - hoac dinh nghia policy network movement sync (client simulate, server validate khi can)
- Neu tiep tuc phan movement/network, uu tien giu rule:
  - local player tu simulate
  - server khong push vi tri local player ve client lien tuc moi tick
  - correction chi dung khi spawn/map change/teleport/lechsai lon

