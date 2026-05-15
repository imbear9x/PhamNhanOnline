---
title: TechDesign Validation — Herb Farming Spec Authority Gaps During Dev #2
doc_type: handoff
status: Done
owner: techdesign
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: techdesign-validation
queue_id: 19
feature_key: herb-farming-system
handoff_type: authority-validation
source_handoff: docs/agent-handoffs/active/20260514-herb-farming-system-dev.md
response_to: docs/agent-handoffs/active/20260514-herb-farming-system-dev.md
iteration: 2
---

# Goal

Dev đã bắt đầu làm handoff #2 theo đúng luồng, nhưng gặp các điểm authority gap trong TechDesign/spec hiện tại cần TechDesign chốt lại trước khi tiếp tục implement. Đây không phải từ chối làm; đây là chặn kỹ thuật để tránh code sai authority.

# Current Dev State

- Handoff nguồn `#2 herb-farming-system-dev` đã được claim và audit.
- Dev đã đọc handoff + TechDesign + code hiện tại.
- Dev **chưa sửa code runtime** trong lượt này.
- Cần TechDesign xác nhận canonical data/runtime contract trước khi Dev tiếp tục.

# What Dev Confirmed In Repo

## Có thể implement nếu authority rõ
- `AlchemyService` vẫn còn guard `required_herb_maturity` → có thể remove khi authority data/runtime khớp.
- `PlayerHerbEntity` chưa có `ExpireAt` → có thể thêm nếu schema canonical rõ.
- `PlayerHerbRepository` chưa có query expired inventory herbs → có thể thêm.
- Chưa có packet/handler herb riêng → có thể thêm nếu packet contract được giữ nguyên.
- Chưa có background expiry sweep → có thể thêm nếu config/schema authority rõ.

## Repo hiện tại có baseline herb đang chạy
- `HerbService` đã có:
  - `InsertSoilAsync`
  - `PlantSeedAsync`
  - `PlantExistingHerbAsync`
  - `MoveHerbToInventoryAsync`
  - `HarvestHerbAsync`
- `AlchemyDefinitionCatalog` đã load herb templates / growth stages / harvest outputs từ DB.
- `HerbTemplateEntity` hiện có:
  - `id`
  - `code`
  - `name`
  - `seed_item_template_id`
  - `replant_item_template_id`
  - `description`
- `HerbGrowthStageConfigEntity` hiện có `stage_name` và `required_growth_seconds`.

---

# TechDesign Verdict

## Gap 1 — `survival_seconds_without_soil` + `inventory_expiry_seconds` thiếu trong schema/entity

**Verdict: Chỉ add `expire_at`. Bỏ `survival_seconds_without_soil` khỏi scope #2.**

- `player_herbs.expire_at` (timestamp NULL): **phải add** trong slice #2. Dùng để mark thời điểm herb inventory hết hạn.
- `survival_seconds_without_soil` trên `herb_templates`: **không add trong slice #2**. Survival countdown khi soil hết hạn là future design (xem Gap 2).
- `inventory_expiry_seconds` trên `herb_templates`: **không add dưới dạng DB column**. Thay vào đó, giá trị expiry được hardcode/config tạm trong `HerbService.HarvestAsync` khi set `expire_at`. Dev có thể dùng config key `herb.inventory_expiry_seconds` (default 604800 = 7 ngày) cho tạm. Khi nào cần per-herb expiry mới add column.

**Canonical migration cần làm:**
```sql
-- database/migrations/YYYYMMDD_add_player_herbs_expire_at.sql
ALTER TABLE player_herbs ADD COLUMN IF NOT EXISTS expire_at timestamp without time zone NULL;
```

**Entity cần add:**
```csharp
[Column("expire_at")] public DateTime? ExpireAt { get; set; }
```

---

## Gap 2 — Survival countdown khi soil hết hạn

**Verdict: Không implement trong slice #2. Out of scope.**

- Slice #2 chỉ cần: `expire_at` cho inventory herb + background sweep xóa expired inventory herb.
- Survival countdown cho planted herb khi soil hết hạn là future design, không có field cần thêm lúc này.
- Dev không cần làm gì cho gap này. Tiếp tục implement theo scope handoff #2.

---

## Gap 3 — Two-step lifecycle: `MoveHerbToInventoryAsync` vs `HarvestHerbAsync`

**Verdict: Rename + replace theo 2-step canonical.**

| Method hiện tại | Action | Method canonical |
|---|---|---|
| `MoveHerbToInventoryAsync` | Rename → bổ sung set `expire_at` | `HarvestAsync` |
| `HarvestHerbAsync` (grant items trực tiếp) | Xóa | Thay bằng `ExtractHerbAsync` |

**Canonical mapping:**
- `HarvestHerbPacket (204)` → `HerbService.HarvestAsync` → move herb to inventory, set `expire_at`, không grant item
- `ExtractHerbPacket (205)` → `HerbService.ExtractHerbAsync` → validate InInventory + not expired, roll outputs, grant items, delete herb entity

**`HarvestAsync` implementation contract:**
```
1. Load herb, verify owned, state = Planting
2. MaterializeHerbProgress
3. Validate stage >= Mature
4. Không cần check inventory capacity (không grant item)
5. Set herb.State = InInventory, herb.CurrentPlotId = null, herb.PlantedAt = null
6. Set herb.ExpireAt = DateTime.UtcNow + config["herb.inventory_expiry_seconds"]
7. Clear plot.CurrentPlayerHerbId = null
8. Save herb + plot trong 1 transaction
9. Return PlayerHerbId + ExpireAt
```

**`ExtractHerbAsync` implementation contract:**
```
1. Load herb, verify owned, state = InInventory
2. Validate not expired (ExpireAt == null || ExpireAt > now)
3. Resolve outputs từ HarvestOutputs config theo stage
4. Roll proc outputs (random chance per output)
5. Check bag capacity trên full proc set (dùng BagService.CheckCapacityForAsync)
6. Nếu không fit → throw GameException(InventoryFull) — hoặc GardenInventoryFull nếu muốn phân biệt
7. Grant items
8. Delete herb entity
9. Return granted items
```

---

## Gap 4 — Enum/state numeric values

**Verdict: Giữ nguyên code, fix spec.**

| Enum | Canonical values | Action |
|---|---|---|
| `PlayerHerbState` | `InInventory = 1, Planting = 2` | **Giữ nguyên code** — spec có lỗi đánh máy `Planting = 0`, không migrate DB |
| `HerbGrowthStage` | `Seedling=1, Mature=2, ThousandYear=3, Young=4` | Rename `Perfect → ThousandYear` trong C# enum, DB value 3 giữ nguyên; thêm `Young = 4` |
| `HerbHarvestOutputType` | `Material=1, Seed=2` | **Giữ tên `Seed`**, không rename thành `Replant` — tránh breaking change |
| `HerbMaturityRequirement` | `None=0, Mature=1, Perfect=2` | **Không rename `Perfect`** trong slice #2 — đây là alchemy concept riêng, không liên quan herb growth stage |

**Dev cần làm:**
```csharp
// AlchemySystemTypes.cs
public enum HerbGrowthStage
{
    Seedling     = 1,
    Mature       = 2,
    ThousandYear = 3,  // renamed from Perfect; DB value unchanged
    Young        = 4,  // new
}
```

Stage progression order (theo `required_growth_seconds`): `Seedling → Young → Mature → ThousandYear`.
Enum integer value không quyết định thứ tự; thứ tự do `required_growth_seconds` trong config quyết định.

---

## Gap 5 — Packet IDs + MessageCode range

**Verdict: Giữ nguyên tất cả ranges đã spec.**

| Range | Decision |
|---|---|
| Herb packet IDs: 200–215 | ✅ Giữ — không conflict với bag (220–223), ground reward (51–56) |
| MessageCode herb range: 6000–6011 | ✅ Giữ — không conflict với bag (3059–3061) |

**Canonical packet list:**

| Packet | ID | Dir |
|---|---|---|
| `GetGardenPlotsPacket` | 200 | C→S |
| `InsertSoilPacket` | 201 | C→S |
| `PlantHerbSeedPacket` | 202 | C→S |
| `PlantExistingHerbPacket` | 203 | C→S |
| `HarvestHerbPacket` | 204 | C→S |
| `ExtractHerbPacket` | 205 | C→S |
| `GetGardenPlotsResultPacket` | 210 | S→C |
| `InsertSoilResultPacket` | 211 | S→C |
| `PlantHerbSeedResultPacket` | 212 | S→C |
| `PlantExistingHerbResultPacket` | 213 | S→C |
| `HarvestHerbResultPacket` | 214 | S→C |
| `ExtractHerbResultPacket` | 215 | S→C |

---

# Spec Fix Required

Spec `docs/tech-design/herb-farming-system.md` có 1 lỗi cần fix:

- `PlayerHerbState.Planting` spec ghi `0` — **sai**. Canonical là `2` (theo code hiện tại). Đã fix trong spec.

---

# Next Step

**#2 unblock — Dev tiếp tục implement theo verdict trên.**

Dev không cần hỏi thêm authority gap nào trong scope đã chốt. Nếu gặp gap mới, tạo handoff mới cho TechDesign.
