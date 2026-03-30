# Mục Lục Tài Liệu Skill Presentation

## Nên đọc theo thứ tự này

### 1. Tài liệu chính cho hệ đang chạy

- `docs/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md`

Tài liệu này giải thích:

- hệ hiện tại đã implement gì
- flow runtime từ server tới Unity
- các class chính
- cách dev tiếp quản và mở rộng trong Phase 1-2

### 2. Tài liệu roadmap tương lai

- `docs/SKILL_PRESENTATION_PHASE3_ROADMAP.md`

Tài liệu này giải thích:

- khi nào cần Phase 3
- Phase 3 sẽ làm gì
- nên refactor theo hướng nào
- những lỗi kiến trúc cần tránh

## Nếu chỉ có 5 phút

Đọc nhanh:

1. `docs/Skill Docs/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md`
2. phần `Flow runtime hiện tại`
3. phần `Hướng dẫn thêm skill mới trong Phase 1-2`
4. `docs/Skill Docs/SKILL_PRESENTATION_PHASE3_ROADMAP.md`
5. phần `Kế hoạch triển khai Phase 3 gợi ý`

## File code quan trọng tương ứng

- `GameShared/Packets/Packets/WorldPackets.cs`
- `GameServer/Network/Handlers/AttackEnemyHandler.cs`
- `GameServer/World/WorldInterestService.cs`
- `GameServer/World/SkillExecutionRuntimeTypes.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Application/ClientCombatService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Application/CombatRuntimeNotices.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/ClientSkillPresentationService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/ClientSkillPresentationState.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/SkillWorldPresentationCatalog.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/CharacterSkillPresenter.cs`
