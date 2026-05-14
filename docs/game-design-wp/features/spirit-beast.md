---
doc_type: game_design_feature
system_id: spirit-beast
status: draft
maturity: feature
owner: gamedesign
created_at: 2026-05-08
updated_at: 2026-05-12
promoted_from: notes/spirit-beast-system.md
related_docs:
  - features/home-cave-defense.md
  - features/spirit-sense.md
  - features/death-penalty.md
  - features/machine-system.md
requires_code_verification: false
---

# Hệ Thống Linh Thú — Feature Draft

## Goal

Tạo hệ **Linh Thú** là companion sinh vật sống cho người chơi, phục vụ cả combat lẫn utility. Linh thú tự hành theo AI, có thể tiến hóa qua tu luyện, sinh sản, và kế thừa phẩm chất từ bố mẹ. Khác biệt rõ ràng với Khôi Lỗi ở bản chất và cơ chế điều khiển.

## Design Summary

Linh Thú là thực thể thật trong game, gần với enemy đồng minh về mặt runtime. Người chơi có thể có được linh thú qua trứng, thu phục ngoài map, hoặc quest. Có thể triệu hồi linh thú miễn đủ **slot thần thức** — mỗi linh thú chiếm một lượng thần thức cố định, không hao theo thời gian. Linh thú tự hành theo AI ưu tiên mục tiêu, có thể tự che chắn cho chủ và nhặt đồ. Sinh sản tạo trứng với phẩm chất kế thừa từ bố mẹ. Pet chết giảm tu vi, hết thọ nguyên thì mất vĩnh viễn.

## Scope

### In Scope
- Nguồn sở hữu: trứng, thu phục, quest
- Triệu hồi, duy trì, giới hạn số lượng
- AI ưu tiên mục tiêu
- Cơ chế che chắn chủ nhân
- Auto loot
- Bảo vệ động phủ
- Sinh sản và ấp trứng
- Kế thừa phẩm chất
- Tăng trưởng / tu luyện
- Trạng thái bị hạ gục, cooldown, mất tu vi
- Thọ nguyên pet

### Out Of Scope
- % tu vi mất khi chết (cụ thể) — phase balance
- Rule chi tiết số trứng, tỉ lệ nở, item giảm thời gian ấp — phase balance
- UI flow chi tiết thu phục / ấp trứng / phòng linh thú
- Ally system cho pet trong PvP faction/party — defer đến Tông Môn/Party system

## Core Loop

1. Player nhận/mua trứng hoặc thu phục linh thú ngoài map.
2. Ấp trứng trong Phòng Linh Thú tại động phủ → pet nở.
3. Triệu hồi ra → pet follow và tham chiến theo AI.
4. Pet tăng trưởng qua chiến đấu / đan dược / tự tu luyện.
5. Pet đủ điều kiện → sinh sản với pet khác → trứng mới.
6. Pet chết trong combat → về túi, cooldown, mất % tu vi.
7. Pet hết thọ nguyên → biến mất vĩnh viễn.

## Player-Facing Rules

### Vai trò
- **Combat pet**: đánh, đỡ đòn.
- **Utility pet**: nhặt đồ, bảo vệ động phủ.
- Không phải mount là trọng tâm.

### Bản chất
- Là **thực thể thật** trong game, có entity, chỉ số, runtime state.
- Combat logic gần với **enemy đồng minh**: có target, skill, runtime state.
- **Không drop ra đất** khi bị hạ.
- Chủ từ bỏ pet → pet **biến mất**.

### Quyền sở hữu / Trade
- Chỉ **pet cấp thấp** (theo cảnh giới) mới được giao dịch / đổi chủ.
- **Pet cấp cao không được giao dịch**.

### Nguồn pet
1. **Nở từ trứng** — trứng mua từ NPC hoặc từ người chơi khác.
2. **Thu phục vô chủ ngoài map**:
   - Map thi thoảng xuất hiện linh thú vô chủ ngẫu nhiên.
   - Bấm Thu Phục → pet bị khóa theo thứ tự người tương tác trước.
   - Người đến sau thấy trạng thái "đang bận / đang tranh đấu".
   - Vào không gian chiến đấu riêng có đếm ngược → đánh thắng → thu phục.
   - Đánh thua → pet hồi full máu, không mất.
3. **Phần thưởng nhiệm vụ**.
4. **Boss drop trứng** — không drop pet trực tiếp.

### Triệu hồi và duy trì
- **Triệu hồi**: chiếm một lượng **slot thần thức cố định** của player — không hao theo thời gian.
- Không đủ slot thần thức → **không cho triệu hồi thêm**, phải thu hồi linh thú hiện tại trước.
- Không giới hạn cứng số lượng — giới hạn thực tế là **tổng slot thần thức còn dư** sau khi trừ phần reserved cho hoạt động cơ bản.
- Không có mục tiêu địch → follow player.

### AI ưu tiên mục tiêu
Khi có kẻ địch, ưu tiên theo thứ tự:
1. Kẻ địch gần nhất đang tấn công người chơi.
2. Kẻ địch gần nhất đang tấn công chính linh thú đó.
3. Kẻ địch mà người chơi đang target / tấn công.

Không có target phù hợp → follow player.

### Combat / Skill
- Pet có **skill riêng**, tự dùng theo AI/runtime rule.
- Pet có **mana riêng** — là nguồn năng lượng vận hành khi đang triệu hồi.
- Mana pet tiêu hao liên tục khi đang triệu hồi (duy trì tồn tại) và tiêu hao thêm khi dùng skill.
- Hết mana → **tự động về túi**, bắt đầu hồi phục.
- **Chỉ hồi mana khi trong túi** — không hồi khi đang triệu hồi.
- Tốc độ hồi phụ thuộc vào **phẩm cấp túi trữ vật** và **tỉ lệ hồi phục của pet**.
- Phải có **≥ X% mana tối đa** mới được triệu hồi lại (X → `game_configs`).
- Pet **không chia role cứng** — tính chất quyết định bởi bộ skill được config sẵn.
- Mỗi cảnh giới/phát triển tương ứng: bộ skill định sẵn, không thay đổi được.
- Pet được config bao nhiêu skill thì dùng bấy nhiêu.

**Thứ tự dùng skill:**
1. Skill đỡ đòn — nếu player bật cơ chế chủ động đỡ đòn.
2. Skill buff.
3. Skill attack.
4. Nếu 2 skill cùng loại → random.

### Che chắn cho chủ
- Là **behavior/action**, không phải skill riêng.
- Khi đang triệu hồi, còn sống, và player bị nhắm tới:
  - Pet **tele chắn trước mặt player**.
  - Pet **nhận thay** skill đó.
- Nếu lúc tele pet có skill type đỡ đòn → dùng skill đó luôn.
- Có thể **bật / tắt** trong cấu hình linh thú.
- **Cơ chế gánh sát thương**: nếu HP pet không đủ so với lượng sát thương phải chịu, **phần thừa vẫn tác động lên player**.

### Auto loot
- **Nhặt tất cả** (tạm thời).
- Pet **không spam nhặt liên tục** — mỗi chu kỳ ~1 giây (cấu hình `game_configs`) pet tìm item có thể nhặt trong bán kính và gửi yêu cầu nhặt 1 lần.
- Tuân theo **Ownership / Drop Rights** (xem `shared-rules.md`): chỉ nhặt đồ mà chủ nhân có quyền nhặt — đồ của chính chủ, hoặc đồ public đã hết priority window. Không nhặt đồ vẫn trong priority window của người khác.
- Balo player đầy → dừng nhặt.
- 2 pet cùng nhắm 1 drop: chỉ 1 pet nhặt được, item về tay chủ.
- Player đang trade / PK: pet vẫn nhặt bình thường.
- Player chết → pet **không nhặt**, đứng yên. Hồi sinh xong mới nhặt tiếp.
- Có thể bật / tắt trong cấu hình linh thú.

### Bảo vệ động phủ
- Đặt pet ở vị trí bảo vệ → pet ở lại động phủ.
- Pet đang thủ **không thể triệu hồi mang theo** — phải thu hồi về túi trước.
- Khi bị công: pet thủ nhà xuất hiện tại **map cổng động phủ**.
- Pet thủ nhà chết → về **túi linh thú của chủ**.
- Chủ offline, pet chết, kẻ công bỏ đi: pet hồi sinh để tiếp tục thủ.
- Chủ offline, pet chết, kẻ công vào cướp: pet về túi, hồi sinh trong túi.

### Thần thức / tàng hình / PvP
- Pet có thần thức, bị ảnh hưởng bởi rule thần thức.
- Pet **không có nút tàng hình chủ động**.
- Trong combat, pet có thể **tự tàng hình** theo behavior.
- Thần thức pet thường thấp → hầu hết trường hợp bị nhìn thấy.
- Pet vẫn có thể tấn công mục tiêu nếu **chủ nhìn thấy** mục tiêu đó.
- **Duel**: pet tham chiến bình thường.
- **PvP Zone**: pet tấn công player đối phương theo rule ưu tiên target.

### Trạng thái bị hạ gục
- Hết HP → **về túi linh thú**.
- Hết mana → **tự động về túi**, bắt đầu nghỉ ngơi hồi phục.
- Rơi vào trạng thái **ngủ / hồi phục**.
- Cần **cooldown** trước khi triệu hồi lại.
- **Nếu bị hạ gục trong combat**: giảm thêm **% tu vi của pet**.
- Áp dụng cho mọi nguyên nhân chết (PvE, PvP, thủ động phủ).

### Thọ nguyên của pet
- Pet có thọ nguyên theo cảnh giới, thường **gấp 5–10 lần player cùng cảnh giới**.
- Hết thọ nguyên (dù ở bất kỳ trạng thái nào: túi, triệu hồi, thủ nhà) → **biến mất**, chủ nhận thông báo.
- Lên đến **Hóa Thần** thì gần như bất tử.

### Sinh sản
- Cần **2 pet cùng loài**, không cần giới tính.
- **Không được cận huyết**: mỗi pet có 1 lineage field, cùng lineage id → không sinh sản được.
- Cho 2 pet vào **Phòng Sinh Sản** trong động phủ → sau thời gian sinh ra **trứng pet**.
- **Số trứng** random trong khoảng config.
- Trứng có thời gian ấp và tỉ lệ nở; có tỉ lệ hỏng/trượt.

### Kế thừa phẩm chất
- Pet con inherit từ **phẩm chất 2 bố mẹ** theo random range.
- Ví dụ: bố thượng cấp + mẹ trung cấp → con random trong khoảng trung → thượng.
- Tỉ lệ nghiêng về phần **giữa trở lên**.
- Model theo **giống loài + tu vi/cảnh giới**, không inherit ngẫu nhiên từ bố mẹ.

### Tăng trưởng
- Kết hợp 3 nguồn: **cùng chiến đấu**, **ăn đan dược**, **tự tu luyện** (trong túi hoặc động phủ).
- Pet có **phẩm chất** (thiên phú) quyết định tốc độ tăng trưởng.
- Khi tu luyện: tiềm năng **tự động phân bổ ngẫu nhiên theo trọng số config**.
- 2 pet cùng loại vẫn có thể khác nhau về tu vi và chỉ số.

### Điều kiện triệu hồi
- Pet có chỉ số **thần thức yêu cầu để điều khiển / triệu hồi**.
- Nếu thần thức player không đủ → **không thể triệu hồi pet**.
- Pet lên cảnh giới có thể **đổi model** và **mở skill mới**.

### UI / Không gian sử dụng
- **Túi linh thú**: mở được mọi lúc.
- **Phòng linh thú**: chỉ mở được trong map động phủ.
- **Phòng sinh sản**: chỉ mở được trong map động phủ.
- Các màn hình tách riêng, không gộp.

## System States

| State | Mô tả |
|---|---|
| Trong túi (bình thường) | Pet đang ở túi, không triệu hồi |
| Trong túi (ngủ hồi phục) | Vừa bị hạ gục hoặc hết mana, đang hồi phục |
| Đang triệu hồi | Đang theo player, chiếm slot thần thức chủ |
| Thủ động phủ | Ở tại cổng động phủ, không triệu hồi được |
| Đã biến mất | Hết thọ nguyên hoặc chủ từ bỏ |

## Edge Cases
- 2 pet cùng gần 1 drop: chỉ 1 pet nhặt, item về tay chủ bình thường.
- Pet đang thủ nhà bị chết, chủ offline, kẻ công bỏ đi: pet hồi sinh để tiếp tục thủ.
- Player thần thức không đủ sau khi pet lên cảnh giới: không triệu hồi được cho đến khi nâng thần thức.
- Pet hết thọ nguyên khi đang triệu hồi: biến mất ngay, player nhận thông báo.
- Cận huyết check: chỉ cần cùng lineage id → không cho sinh sản, bất kể cảnh giới.

## Data / Config Needs
- % tu vi pet mất khi chết → phase balance
- Pool thọ nguyên pet theo cảnh giới (hệ số 5–10x player) → DB config
- Trọng số phân bổ tiềm năng pet theo loài → DB config
- Số trứng random range → `game_configs`
- Thời gian ấp trứng → `game_configs`
- Tỉ lệ nở / hỏng trứng → `game_configs`
- Cooldown hồi phục sau khi bị hạ → `game_configs`
- Ngưỡng thần thức yêu cầu điều khiển theo cảnh giới pet → DB config
- Tỉ lệ nghiêng trong kế thừa phẩm chất → phase balance

## UI / UX Notes
- Túi linh thú: hiển thị trạng thái mỗi pet (active / ngủ hồi phục / thủ nhà / thọ nguyên còn lại).
- Phòng sinh sản: hiển thị lineage để kiểm tra cận huyết trước khi xác nhận.
- Phòng linh thú: hiển thị tiến độ ấp trứng.
- Cảnh báo khi thọ nguyên pet sắp hết.

## Related Systems
- **Động Phủ**: thủ nhà, phòng linh thú, phòng sinh sản — xem `features/home-cave-defense.md`
- **Thần Thức**: pet bị ảnh hưởng rule tàng hình — xem `features/spirit-sense.md`
- **Death Penalty**: pet chết giảm tu vi, không giống penalty thọ nguyên player — xem `features/death-penalty.md`
- **Khôi Lỗi**: companion khác bản chất, chia sẻ slot thần thức duy trì — xem `features/machine-system.md`

## Key Decisions
1. Pet là thực thể thật, không drop ra đất khi bị hạ.
2. Chỉ pet cấp thấp mới tradable.
3. Tối đa 2 linh thú triệu hồi đồng thời.
4. Không có giới hạn density map riêng cho linh thú; giới hạn nằm ở slot thần thức và resource của pet.
4. Triệu hồi chiếm **slot thần thức cố định** — không hao theo thời gian.
5. Pet tự hành theo AI, không do player chủ động điều khiển từng lệnh.
6. Che chắn là behavior, không phải skill riêng.
7. Phần sát thương thừa khi pet không đủ HP vẫn tác động lên player.
8. Thu phục: khóa theo thứ tự, vào không gian riêng, đánh thua không mất pet.
9. Không cận huyết: check lineage id.
10. Pet chết giảm tu vi, hết thọ nguyên thì biến mất vĩnh viễn.
11. Thọ nguyên pet gấp 5–10x player cùng cảnh giới.
12. Tiềm năng pet tự phân bổ theo trọng số, không do player điều khiển.

## Open Questions
- [ ] % tu vi pet mất khi chết — phase balance.
- [ ] Số trứng / tỉ lệ nở / item giảm thời gian ấp cụ thể — phase balance.
- [ ] UI flow chi tiết thu phục / ấp trứng / phòng linh thú.
- [ ] Ally rule cho pet trong PvP faction/party — defer đến Tông Môn/Party system.

## Known Conflicts / Drift
- Chưa có conflict nào ghi nhận.

## Requirement Readiness Checklist
- [x] Behavior is specific enough for `dev` to estimate.
- [x] Acceptance criteria can be written without guessing.
- [x] Major edge cases are covered.
- [x] Config/data needs are listed.
- [x] Out-of-scope items are explicit.
- [x] Ready to promote to `requirements/`.
