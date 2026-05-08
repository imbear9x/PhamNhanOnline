# Hệ Thống Speed — Design Notes

**Ngày tạo:** 2026-05-08  
**Trạng thái:** Đã chốt cơ bản

---

## Concept

Speed là chỉ số của **mọi thực thể** trong game (player, quái, boss...).
Ảnh hưởng đến 3 thứ:
1. **Tốc độ di chuyển** trên map
2. **Tốc độ bay** (dùng chung chỉ số di chuyển, không tách riêng)
3. **Tỉ lệ né tránh (Evasion)** khi bị tấn công

---

## Thực Thể và Speed

- Speed fixed theo **template** trong DB
- Quái/boss cũng là thực thể có speed và thần thức — không khác player về mặt rule
- Nếu quái/boss được buff speed nhất thời bởi skill → speed tăng tạm thời, evasion tính lại theo speed mới

---

## Tốc Độ Di Chuyển / Bay

- Speed map trực tiếp lên movement speed và fly speed
- Tốc độ bay = tốc độ di chuyển — dùng chung 1 chỉ số

---

## Evasion — Relative Speed System

Không có Accuracy/Hit rate là stat riêng. Chỉ có speed quyết định evasion.

### Rule

Dựa trên **chênh lệch speed giữa attacker và defender**:

- Nếu speed attacker **cao hơn defender vượt ngưỡng Y%** → **100% trúng**, defender không thể né
- Nếu speed attacker **thấp hơn defender dưới ngưỡng Z%** → bắt đầu có evasion
- Càng thấp hơn nhiều → evasion càng cao theo **curve (phi tuyến)**
- Curve mượt hơn linear: chênh lệch nhỏ thì evasion tăng chậm, chênh lệch lớn thì evasion tăng nhanh hơn
- Có **cap evasion tối đa** (không thể né 100%) — đặt trong `game_configs`

### Ví dụ minh họa (Y=20%, Z=20%, evasion cap=80%)

| Speed attacker | Speed defender | Kết quả |
|---|---|---|
| 120 | 100 | Attacker cao hơn 20% → 100% trúng |
| 100 | 100 | Bằng nhau → base hit rate |
| 80 | 100 | Thấp hơn 20% → bắt đầu có evasion (thấp) |
| 50 | 100 | Thấp hơn nhiều → evasion cao (gần cap 80%) |

### Áp dụng
- Áp dụng cho **cả tấn công đơn lẫn AoE** — dùng chung rule
- Server tính evasion roll mỗi khi có hit event

### Config
- Ngưỡng Y% → `game_configs`
- Ngưỡng Z% → `game_configs`
- Cap evasion tối đa → `game_configs`
- Shape của curve → xác định khi làm balance

---

## Còn cần thảo luận
- [ ] Shape curve cụ thể (quadratic, exponential...) — bàn khi làm balance
