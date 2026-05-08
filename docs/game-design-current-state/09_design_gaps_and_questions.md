# 09. Design Gaps And Questions

## Gameplay design gaps

### 1. Quest loop chưa tồn tại thật

- Vấn đề: client có tab `Quest` nhưng chưa có packet/service/db flow.
- Vì sao quan trọng: thiếu mục tiêu dài hạn ngoài grind combat/cultivation.
- File/code liên quan: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`
- Đề xuất hướng xử lý sơ bộ: chốt quest domain model tối thiểu trước: quest template, progress state, reward claim, NPC/source mapping.

### 2. Guild/social loop chưa có

- Vấn đề: tab `Guild` mới placeholder.
- Vì sao quan trọng: game online thiếu social retention loop.
- File/code liên quan: `WorldMenuController.cs`
- Đề xuất hướng xử lý sơ bộ: decide whether guild is near-term MVP hay post-MVP.

### 3. Smithing / Talisman chưa phải gameplay thật

- Vấn đề: panel/portal có, nhưng flow chỉ placeholder.
- Vì sao quan trọng: người chơi thấy affordance nhưng không có payoff.
- File/code liên quan: `WorldCraftingPanelController.cs`, `LocalFixPortalPresenter.cs`, `GameServer/Runtime/PracticeSystemTypes.cs`
- Đề xuất hướng xử lý sơ bộ: hoặc ẩn khỏi UI phase hiện tại, hoặc chốt scope nhỏ giống alchemy.

### 4. Farming/herb/soil ở server đã sâu nhưng chưa public

- Vấn đề: data model và service có, nhưng thiếu packet + client.
- Vì sao quan trọng: đây có thể là progression/resource loop quan trọng cho alchemy.
- File/code liên quan: `GameServer/Services/HerbService.cs`, `database/initDatabase.sql`
- Đề xuất hướng xử lý sơ bộ: define MVP farming flow: view plots, insert soil, plant seed, harvest.

## Technical design gaps

### 5. DB bootstrap hiện chưa one-shot rõ ràng

- Vấn đề: cần ít nhất 2 SQL paths để có schema đầy đủ.
- Vì sao quan trọng: agent/dev mới rất dễ setup sai DB.
- File/code liên quan: `database/phamnhan_online.sql`, `database/initDatabase.sql`
- Đề xuất hướng xử lý sơ bộ: tạo `database/bootstrap_full.sql` hoặc README migration order chuẩn.

### 6. `breakthrough_conditions` chưa được runtime sử dụng

- Vấn đề: table/repo tồn tại nhưng breakthrough logic hiện không đọc.
- Vì sao quan trọng: design data có nguy cơ thành dead config.
- File/code liên quan: `GameServer/Repositories/BreakthroughConditionRepository.cs`, `GameServer/Runtime/CharacterCultivationService.cs`
- Đề xuất hướng xử lý sơ bộ: xác nhận bỏ hẳn hay integrate vào roll chance/requirement UI.

### 7. `game_configs` key seed mismatch

- Vấn đề: code có keys chưa seed DB.
- Vì sao quan trọng: config source-of-truth không đồng bộ với runtime expectations.
- File/code liên quan: `GameServer/Config/GameConfigKeys.cs`, `database/initDatabase.sql`
- Đề xuất hướng xử lý sơ bộ: thêm migration seed missing keys và document default fallback behavior.

## DB / config gaps

### 8. Balance/config documentation cho enemy/skill/item còn tản mát

- Vấn đề: nhiều gameplay numbers nằm trong DB nhưng chưa có one-page design explanation.
- Vì sao quan trọng: designer mới khó chỉnh balance mà không đọc code/runtime.
- File/code liên quan: `public.skills`, `public.skill_effects`, `public.enemy_templates`, `public.enemy_reward_rules`
- Đề xuất hướng xử lý sơ bộ: tạo admin-facing balance guide riêng sau bộ current-state docs.

### 9. Practice payload/result JSON chưa có contract doc tách riêng

- Vấn đề: `player_practice_sessions.request_payload_json` và `result_payload_json` khá quan trọng nhưng khó đọc chỉ từ DB row.
- Vì sao quan trọng: debug/cross-tooling/admin audit sẽ khó.
- File/code liên quan: `player_practice_sessions`, `AlchemyCraftActionService.cs`, `AlchemyPracticeService.cs`
- Đề xuất hướng xử lý sơ bộ: publish JSON payload schema examples.

## Security / anti-cheat gaps

### 10. Chưa có dedicated anti-cheat subsystem

- Vấn đề: hardening hiện phân tán ở middleware/gates/clamps, chưa có centralized suspicious-action strategy.
- Vì sao quan trọng: khi game mở rộng PvP/economy, abuse detection sẽ khó scale.
- File/code liên quan: `GameLoop.cs`, `WorldInteractionGate.cs`, `RateLimitMiddleware.cs`
- Đề xuất hướng xử lý sơ bộ: define anti-cheat telemetry + severity buckets trước khi thêm feature economy/social.

### 11. Movement authority chưa có collision/path rule sâu

- Vấn đề: server clamp tốc độ tốt, nhưng chưa thấy nav/collision/path obstruction authority.
- Vì sao quan trọng: map complexity tăng sẽ lộ exploit di xuyên chướng ngại nếu client presentation tự đi được.
- File/code liên quan: `GameServer/Runtime/GameLoop.cs`, `CharacterPositionSyncHandler.cs`
- Đề xuất hướng xử lý sơ bộ: chốt whether world is intentionally open plane hay cần obstacle authority later.

## UX / UI gaps

### 12. World menu đang trộn placeholder và panel thực

- Vấn đề: `Quest`, `Inventory`, `Equipment`, `Guild` tabs trong menu text cũ không phản ánh panel gameplay hiện hữu đầy đủ.
- Vì sao quan trọng: người mới rất khó hiểu tab nào là thật, tab nào chỉ để dành.
- File/code liên quan: `WorldMenuController.cs`, `WorldInventoryPanelController.cs`
- Đề xuất hướng xử lý sơ bộ: redesign navigation map cho current-state features.

### 13. Login flow tự chọn character đầu tiên

- Vấn đề: không có explicit selection UX nếu sau này account có nhiều character.
- Vì sao quan trọng: architectural assumption hiện đang cứng vào UX.
- File/code liên quan: `ClientLoginFlowService.cs`
- Đề xuất hướng xử lý sơ bộ: nếu vẫn 1 character/account thì document rõ; nếu không thì làm character select screen.

### 14. Hardcoded dev credentials trong Login scene

- Vấn đề: UI login đang autofill `admin123456` / `admin@admin`.
- Vì sao quan trọng: dễ gây nhầm đây là production-ready flow.
- File/code liên quan: `LoginScreenController.cs`
- Đề xuất hướng xử lý sơ bộ: gate bằng dev build flag hoặc bỏ khỏi main branch runtime scene.

## Documentation gaps

### 15. Chưa có một bộ “current state” thống nhất trước file này

- Vấn đề: docs hiện có nhiều audit/spec draft nhưng không có một pack để design agent mới đọc tuần tự.
- Vì sao quan trọng: context bị phân mảnh giữa docs cũ, audit, code.
- File/code liên quan: `docs/reference-and-specs/*`, `docs/reports-and-testing/*`, `ClientUnity/PhamNhanOnline/docs/game-design-client-overview.md`
- Đề xuất hướng xử lý sơ bộ: dùng chính folder `docs/game-design-current-state/` làm canonical onboarding pack.

## Additional questions cần bàn tiếp

- Long-term economy có currency chính thức hay chỉ item-based reward?
- Multi-character/account có còn là roadmap không?
- Có PvP không, hay game hoàn toàn PvE/co-op?
- Home cave có tùy biến/placement thật không, hay chỉ là private utility map?
- Reward direct grant vs ground drop dùng triết lý nào cho từng content category?
- Cultivation failure penalty có cần item-based mitigation?
- Alchemy mutation hiện muốn đi theo “rare jackpot result” hay “quality tier / bonus stats”?
- Farming materials có intended cadence với cultivation/alchemy như thế nào?

Source: synthesis from `GameServer` + `ClientUnity` current codebase
