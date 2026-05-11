# Skill Design Clarification

## Intended player-facing behavior

- Người chơi sở hữu một danh sách skill. Một số skill đến từ quá trình tu luyện/unlock, một số đến từ trang bị đang mặc.
- Người chơi chọn ra một số skill đặt vào **loadout** — đây là bộ skill họ có thể dùng trong combat.
- Trong combat, người chơi chọn skill từ loadout để tấn công kẻ địch.
- Loadout có số slot có giới hạn cố định (config-driven).
- Khi thay trang bị, skill grant từ trang bị đó bị cộng/trừ tự động. Người chơi nên biết ngay khi skill bị thu hồi để tránh dùng nhầm slot rỗng.
- Starter skill được grant tự động khi tạo nhân vật.

## Intended terminology

- **Owned Skill**: skill người chơi đang sở hữu, dù có trong loadout hay không
- **Permanent Skill**: skill sở hữu vĩnh viễn, không liên quan đến trang bị
- **Equipment-Granted Skill**: skill có được từ trang bị đang mặc, mất khi tháo trang bị
- **Skill Loadout**: bộ slot skill người chơi chọn để dùng trong combat
- **Loadout Slot**: một ô trong loadout, gắn với một skill cụ thể
- **Loadout Slot Count**: số slot tối đa trong loadout, config-driven
- **Starter Skill**: skill mặc định được grant khi tạo nhân vật mới
- **Skill Source**: nguồn gốc skill (permanent/equipment-granted)

## Intended rules

- Owned skill phải có nguồn gốc rõ ràng: permanent hoặc equipment-granted.
- Chỉ skill đang sở hữu và đủ điều kiện mới được đặt vào loadout slot.
- Equipment-granted skill không thể vào loadout nếu:
  - trang bị nguồn không còn được equip
  - skill definition yêu cầu cảnh giới tối thiểu chưa đạt
- Khi tháo trang bị, skill grant từ trang bị đó phải được xóa khỏi owned skills và loadout.
- Loadout slot chứa tối đa 1 skill mỗi slot; mỗi skill không được xuất hiện quá 1 lần trong loadout.
- Xóa loadout slot (gán `PlayerSkillId = 0`) là hợp lệ và không phải lỗi.
- Combat chỉ execute skill nếu skill đó có definition hợp lệ trong catalog; không có fallback tự động.
- Starter skill grant chỉ xảy ra một lần khi nhân vật mới được tạo; không re-grant nếu đã sở hữu.

## Acceptable current behavior

- Loadout slot count config-driven với default 5 là chấp nhận được.
- Full snapshot return sau mọi mutation loadout (thay vì delta) là hợp lệ.
- Skill canonicalization ẩn duplicate rows với client là chấp nhận được **với điều kiện canonical docs giải thích rõ tại sao có duplicate và cách resolve**.
- Equipment sync tự động khi equip/unequip là đúng hướng.
- Xóa loadout row khi skill nguồn bị thu hồi là đúng hướng.
- Skill browsing không cần world-entry state là có thể chấp nhận nếu đây là intentional design.

## Mismatch vs current code

- **Duplicate skill rows tồn tại trong persistence**: server đang ẩn duplicate với client thông qua canonicalization logic, nhưng data thật vẫn có nhiều rows cho cùng một skill. Đây là debt persistence có thể dẫn đến hành vi khó đoán nếu canonicalization logic thay đổi sau này.
- **Equipment-granted skill không phân biệt rõ trong model**: equipment-granted skill và permanent skill đều là `PlayerSkillEntity`, chỉ phân biệt qua `SourcePlayerItemId`. Downstream code phải nhớ check field này; nếu bỏ qua có thể nhầm equipment skill là permanent và không thu hồi đúng cách.
- **Skill fetch không yêu cầu world-entry** trong khi equip/inventory handlers thì có. Nếu đây là intentional (skill là account-level, không phải world-session-level) thì cần canonical doc confirm explicitly.
- **Invalid skill definition tại combat time không có fallback**: nếu skill trong loadout nhưng definition bị thiếu/lỗi, combat fail. Không có behavior graceful degradation. Về UX, người chơi có thể không hiểu tại sao attack thất bại.
- **Loadout slot chứa skill không còn khả dụng**: skill có thể remain trong loadout nhưng thực tế không dùng được nếu requirement không thỏa (ví dụ equipment bị tháo, cảnh giới không đủ). Behavior hiện tại là block assignment, nhưng chưa rõ stale slot có bị clean up không.

## Unresolved design questions

- Ngoài equipment-granted và permanent, có nguồn skill nào khác trong V1 không? Ví dụ: quest reward, NPC learn, boss drop skill book?
- Người chơi có thể nâng cấp cấp độ skill không? Nếu có thì cơ chế là gì?
- Skill loadout có phải là một "set" duy nhất hay người chơi có nhiều preset loadout để swap?
- Khi skill trong loadout bị thu hồi (do tháo trang bị), slot đó trở thành trống và người chơi cần tự fill lại, hay có cơ chế gì khác?
- Có cần hiển thị rõ cho người chơi biết skill nào là permanent và skill nào là equipment-granted trong UI skill list không?
- Equipment-granted skill với realm requirement — requirement này được thiết kế để làm gì? Phòng exploit hay có ý nghĩa progression thật?
- Starter skill có thể thay đổi theo config khi game scale không? Hay đây luôn là một skill cố định?
- Combat skill execution có thể mở rộng sang các loại action khác ngoài attack không? Ví dụ: buff, heal, AoE khu vực?

## Canonicalization recommendation

- Canonicalize skill domain thành 2 doc:
  1. **skill ownership và loadout runtime**: owned skill model, nguồn gốc skill, loadout slot management
  2. **skill execution runtime**: cách skill được resolve và execute trong combat
- Ghi rõ trong canonical doc rằng **equipment-granted skill và permanent skill** cùng kiểu entity nhưng có lifecycle khác nhau. Phân biệt bằng `SourcePlayerItemId`.
- Đánh dấu duplicate persistence rows là **known technical debt**, không canonicalize duplicate state, chỉ canonicalize phần canonical/representative view mà client thấy.
- Ghi rõ về world-entry requirement: nếu skill fetch là intentionally không cần world-entry, canonical doc phải note rõ đây là account-level behavior.
- Nối sang `equipment-design-clarification.md` vì skill domain phụ thuộc trực tiếp vào equip/unequip flow.
- Nối sang `player-stats-design-clarification.md` vì martial art/pháp môn ảnh hưởng cả skill execution lẫn stat bonus.
- Đánh dấu **combat fallback khi skill definition missing** là `needs design decision` — cần chốt xem có graceful degrade hay vẫn là hard fail.
