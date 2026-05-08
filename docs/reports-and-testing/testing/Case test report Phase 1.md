# Case Test Report Phase 1

Tài liệu này gom các case cần test sau khi hoàn thành các phần server Phase 1, Phase 1.2, Phase 1.5 và Phase 2.1-2.4 trong `Server Codebase Audit Report Phase 1.md`.

Mục tiêu là có một checklist ổn định để:

- User test thủ công sau mỗi đợt polish server/client.
- Session sau đọc lại biết cần test gì, không phải hỏi lại từ đầu.
- Sau này có thể chuyển thành test script hoặc AI-assisted test plan.

## 1. Thông Tin Test Run

Điền khi bắt đầu một lượt test:

| Field | Value |
|---|---|
| Test Run Date |  |
| Server Commit |  |
| Client Commit |  |
| Database Migration Version |  |
| Tester |  |
| Environment | Local / Dev / Staging |
| Notes |  |

Quy ước kết quả:

- `PASS`: đúng kỳ vọng.
- `FAIL`: sai kỳ vọng, cần ghi rõ log/video/cách tái hiện.
- `BLOCKED`: chưa test được vì thiếu data/tool/môi trường.
- `SKIP`: cố ý bỏ qua trong lượt test này.

## 2. Baseline Bình Thường

| ID | Case | Cách test | Kỳ vọng | Result | Notes |
|---|---|---|---|---|---|
| BL-01 | Login và vào map | Login một nhân vật bình thường, vào map chính | Vào map thành công, không exception server/client |  |  |
| BL-02 | Di chuyển bình thường | Click move nhiều điểm trong map | Player đi mượt, server không reject sai vị trí |  |  |
| BL-03 | Portal bình thường | Đứng gần portal và tương tác | Chuyển map gần như ngay, không bị delay lạ |  |  |
| BL-04 | Đánh quái bình thường | Tấn công một enemy gần player | Skill/action hiển thị đúng, enemy mất máu/chết đúng |  |  |
| BL-05 | Nhận progression reward | Kill enemy có reward cultivation/potential/exp | Stat/progression tăng đúng, không mất sau reconnect |  |  |
| BL-06 | Nhặt ground reward | Kill quái rơi đồ rồi nhặt | Item vào inventory đúng một lần |  |  |
| BL-07 | Equip/unequip item | Equip rồi unequip một item hợp lệ | Slot và inventory đúng trạng thái |  |  |
| BL-08 | Use item | Dùng một item consumable hoặc book hợp lệ | Item bị consume đúng, effect mở khóa/apply đúng |  |  |
| BL-09 | Craft thường | Craft một recipe đủ nguyên liệu | Consume input và grant output đúng rule |  |  |
| BL-10 | Alchemy lifecycle | Start, cancel, start lại, complete alchemy | Refund/grant đúng, không double item |  |  |

## 3. Movement / Interaction Ordering

| ID | Case | Cách test | Kỳ vọng | Result | Notes |
|---|---|---|---|---|---|
| MV-01 | Portal khi tốc độ thường | Đi tốc độ thường tới portal, bấm ngay khi client tới nơi | Vào map gần như ngay, không còn lệch khoảng 1 giây |  |  |
| MV-02 | Cheat speed tới portal | Tăng client speed x5 hoặc x10, chạy tới portal rồi bấm | Server chờ vị trí thật bắt kịp, sau đó mới chuyển map; không snap lung tung |  |  |
| MV-03 | Cheat speed portal rồi quay lại attack | Cheat speed tới portal, bấm portal, sau đó lập tức quay lại attack boss/quái map cũ | Request xử lý theo thứ tự. Nếu portal thành công trước thì attack target map cũ bị reject, không nhận damage/reward thật |  |  |
| MV-04 | Cheat speed qua quái đủ damage chết rồi attack | Còn ít máu, cheat speed chạy ngang enemy chủ động đủ damage để chết, sau đó attack enemy khác | Server settle damage/death trước, action attack sau bị hủy, không kill/reward enemy khác |  |  |
| MV-05 | Cheat speed tới reward rồi pickup | Cheat speed chạy tới ground reward và nhặt ngay | Server chờ/settle vị trí thật trước, không nhặt theo vị trí fake client |  |  |
| MV-06 | Spam interact khi đang chờ movement wait | Trong lúc server đang chờ player đi tới target, spam portal/pickup/attack | Không tạo nhiều action thắng song song; behavior nhất quán theo gate hiện tại |  |  |

## 4. Death / Cancel Action

| ID | Case | Cách test | Kỳ vọng | Result | Notes |
|---|---|---|---|---|---|
| DT-01 | Chết khi đang chờ portal | Còn ít máu, cheat speed tới portal, enemy đánh chết trước khi server position tới portal | Không chuyển map, movement target bị clear, client nhận state chết hợp lý |  |  |
| DT-02 | Chết khi đang chờ pickup | Còn ít máu, cheat speed tới reward, enemy đánh chết trước khi pickup hợp lệ | Không nhặt được item, reward không mất im lặng |  |  |
| DT-03 | Chết khi đang chờ attack | Còn ít máu, cheat speed tới enemy target, bị enemy khác đánh chết trước | Không gây damage sau khi death đáng lẽ xảy ra trước |  |  |
| DT-04 | Spam action sau khi chết | Sau khi death packet về, spam portal/pickup/attack/use item | Server reject sạch, không tạo reward/damage/teleport/item effect |  |  |
| DT-05 | Death presentation | Bị enemy/boss đánh chết bằng skill/basic attack mặc định | Client thấy action combat trước damage/death, không bị chết im lặng |  |  |

## 5. Inventory / Equipment Race

| ID | Case | Cách test | Kỳ vọng | Result | Notes |
|---|---|---|---|---|---|
| INV-01 | Double equip cùng item | Double click equip một item thật nhanh | Chỉ equip một lần, không duplicate, không lỗi slot |  |  |
| INV-02 | Spam equip hai item cùng slot | Có hai item cùng equipment slot, spam equip cả hai | Cuối cùng chỉ một item ở slot đó, item còn lại ở inventory/trạng thái hợp lệ |  |  |
| INV-03 | Use item quantity 1 nhiều lần | Item stack số lượng 1, double click use thật nhanh | Chỉ consume một lần, không âm quantity |  |  |
| INV-04 | Split/drop/use cùng stack | Stack nhiều item, thao tác split/drop/use thật nhanh | Tổng quantity bảo toàn, không âm, không duplicate |  |  |
| INV-05 | Drop rồi equip/use cùng item | Drop item rồi gần như cùng lúc equip/use item đó | Chỉ một action thắng, action còn lại bị reject |  |  |
| INV-06 | Remove/consume item trong transaction lồng | Trigger flow có gọi `ItemService` từ service khác, ví dụ equipment-granted skill hoặc alchemy input | Không lỗi transaction lồng, không deadlock, dữ liệu đúng |  |  |

## 6. Craft / Alchemy

| ID | Case | Cách test | Kỳ vọng | Result | Notes |
|---|---|---|---|---|---|
| CR-01 | Spam craft đủ nguyên liệu một lần | Có đúng đủ nguyên liệu cho một lần craft, spam craft nhiều lần | Chỉ craft được theo đúng số nguyên liệu thật, không âm item |  |  |
| CR-02 | Craft đồng thời drop nguyên liệu | Bấm craft rồi gần như đồng thời drop nguyên liệu | Nếu craft consume trước thì drop fail; nếu drop trước thì craft fail; không tạo output miễn phí |  |  |
| CR-03 | Craft đồng thời use nguyên liệu | Bấm craft rồi gần như đồng thời use nguyên liệu nếu item cũng có thể use | Chỉ một action consume item thắng |  |  |
| CR-04 | Craft fail case | Dùng recipe có tỉ lệ fail hoặc setup fail | Consume input đúng theo rule fail, không grant sai output |  |  |
| ALC-01 | Alchemy start spam | Spam start cùng một recipe/alchemy session | Không tạo nhiều active session sai, input không bị consume quá số lượng |  |  |
| ALC-02 | Alchemy cancel spam | Start alchemy rồi spam cancel | Refund đúng một lần |  |  |
| ALC-03 | Cancel và complete gần nhau | Alchemy gần complete, vừa cancel vừa complete | Chỉ một nhánh thắng: hoặc refund, hoặc nhận output; không vừa refund vừa nhận output |  |  |
| ALC-04 | Complete spam nhiều lần | Khi session complete được, spam complete/poll nhiều lần | Chỉ grant output một lần |  |  |

## 7. Ground Reward

| ID | Case | Cách test | Kỳ vọng | Result | Notes |
|---|---|---|---|---|---|
| GR-01 | Pickup bình thường | Kill enemy rơi reward, nhặt một lần | Reward vào inventory đúng một lần, reward despawn |  |  |
| GR-02 | Spam pickup một reward | Kill enemy rơi reward, spam pickup liên tục | Chỉ nhận reward một lần; request trùng bị reject/in-progress |  |  |
| GR-03 | Pickup khi chưa tới server position | Cheat speed tới reward rồi pickup ngay | Server chờ/settle trước; không nhặt theo vị trí client fake |  |  |
| GR-04 | Pickup rồi reconnect nhanh | Nhặt reward rồi disconnect/reconnect nhanh | Không double reward. Nếu đã grant DB thì reward biến mất; nếu grant lỗi thì không mất im lặng |  |  |
| GR-05 | Hai nhân vật cùng nhặt | Nếu có thể mở hai client, cho hai nhân vật nhặt cùng một reward | Chỉ một người claim được |  |  |
| GR-06 | DB grant lỗi giả lập nếu có thể | Tạo điều kiện lỗi DB khi pickup, ví dụ stop DB đúng lúc hoặc dùng data invalid | Reward không mất im lặng, server log lỗi đủ context |  |  |

## 8. Enemy Reward / Progression

| ID | Case | Cách test | Kỳ vọng | Result | Notes |
|---|---|---|---|---|---|
| ER-01 | Kill enemy nhận item direct reward | Kill enemy có direct reward item | Item được grant đúng, không duplicate |  |  |
| ER-02 | Kill enemy nhận cultivation/potential | Kill enemy có progression reward | Runtime stat tăng và được flush DB sớm |  |  |
| ER-03 | Kill nhiều enemy liên tục | Kill nhiều enemy liên tiếp trong thời gian ngắn | Reward/progression không mất, không double bất thường |  |  |
| ER-04 | Stop server sau reward nếu test được | Kill enemy nhận progression rồi stop server gần ngay sau đó | Sau restart/reconnect, progression không mất bất thường |  |  |
| ER-05 | Reward pickup sau enemy death | Enemy chết rơi ground reward, spam pickup | Không double item, runtime reward lifecycle đúng |  |  |

## 9. Log / Debug Cần Quan Sát

Trong mỗi lượt test, mở server log và client log. Ghi lại nếu thấy:

| ID | Điều cần soi | Kỳ vọng | Result | Notes |
|---|---|---|---|---|
| LOG-01 | Exception transaction | Không có exception transaction/deadlock bất thường |  |  |
| LOG-02 | Malformed/invalid action spam | Có thể có reject hợp lệ khi test cheat, nhưng không spam vô hạn hoặc crash receive loop |  |  |
| LOG-03 | Pickup grant lỗi | Nếu DB grant lỗi, phải có log context player/reward/map và reward không mất im lặng |  |  |
| LOG-04 | Dead player action | Dead player không portal/pickup/attack thành công |  |  |
| LOG-05 | Enemy attack presentation | Trước death/damage phải có combat action packet tương ứng nếu enemy có skill |  |  |
| LOG-06 | Queue/abuse issue | Nếu spam quá mạnh làm lag server, ghi lại để đưa vào Phase 4 Abuse Resistance |  |  |

## 10. Smoke Test Tối Thiểu

Nếu chỉ có ít thời gian, test 4 case này trước:

| Priority | Case ID | Lý do |
|---|---|---|
| 1 | MV-04 | Bắt lỗi quan trọng nhất: cheat speed, pending hazard, death/action ordering |
| 2 | GR-02 | Bắt lỗi double claim reward |
| 3 | INV-04 | Bắt lỗi race inventory/stack quantity |
| 4 | ALC-03 | Bắt lỗi transaction giữa cancel và complete |

## 11. Rule Cập Nhật Tài Liệu

Khi phát hiện bug trong lúc test:

1. Ghi `FAIL` vào case tương ứng.
2. Thêm mô tả ngắn ở `Notes`: bước tái hiện, kết quả thực tế, log/video nếu có.
3. Nếu bug là case mới chưa có trong file này, thêm một row mới vào đúng nhóm.
4. Sau khi fix xong và user test lại ổn, đổi `Result` thành `PASS` và ghi commit/fix note nếu có.

Không xóa case cũ chỉ vì đã pass; đây là regression checklist cho các lần polish sau.
