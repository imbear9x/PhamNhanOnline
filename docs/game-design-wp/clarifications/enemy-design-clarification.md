# Enemy Design Clarification

## Intended player-facing behavior

- Enemy là thực thể sống trong world/instance, có spawn, di chuyển, chiến đấu, chết, và cho reward.
- Mỗi map/instance nên có nhóm enemy phù hợp với runtime scope của map đó.
- Enemy thường có 2 trạng thái cảm nhận chính từ phía người chơi:
  - **đi tuần/chờ** khi chưa giao chiến
  - **giao chiến** khi đã khóa mục tiêu hoặc bị đánh
- Enemy aggressive sẽ chủ động tấn công nếu người chơi vào phạm vi aggro; enemy passive sẽ không chủ động gây hấn nhưng vẫn phản công khi bị đánh.
- Khi enemy chết, người tham gia chiến đấu nhận reward dựa trên mức đóng góp; reward có thể đi thẳng vào người chơi hoặc rơi ra ground reward.
- Boss là enemy đặc biệt, có thể gắn với completion của một instance/objective.

## Intended terminology

- **Enemy Template**: định nghĩa tĩnh của một loại quái/boss
- **Spawn Group**: nhóm spawn điều khiển việc sinh enemy trên map/instance
- **Patrol**: trạng thái đi tuần/chờ ngoài combat
- **Combat**: trạng thái đang đánh mục tiêu
- **Aggressive Enemy**: quái tự tìm mục tiêu trong phạm vi aggro
- **Passive Enemy**: quái không chủ động đánh trước, nhưng phản ứng khi bị đánh
- **Contribution**: lượng đóng góp damage của từng player lên enemy
- **Ground Reward**: reward rơi trên map với quyền sở hữu/thời gian FFA
- **Boss Completion**: trạng thái hoàn thành instance khi điều kiện boss thỏa mãn

## Intended rules

- Enemy spawn phải phụ thuộc vào **runtime scope** của map/instance, không phải mọi map đều dùng chung một nhóm spawn.
- Timer spawn là rule hợp lý mặc định: map được lấp quái ban đầu rồi respawn dần theo cấu hình.
- Enemy phải có state machine tối thiểu: sống -> patrol/chờ -> combat -> chết -> despawn/respawn.
- Aggressive enemy nên tự vào combat khi người chơi lọt vào phạm vi phù hợp; passive enemy chỉ vào combat khi đã có tác động hoặc aggro override.
- Khi bị đánh, enemy phải ghi nhận contribution và last-hit để phục vụ reward logic.
- Khi out-of-combat đủ lâu:
  - quái thường có thể reset về trạng thái ban đầu
  - boss có thể dùng rule khác với quái thường
- Reward nên chia theo contribution damage thay vì winner-takes-all tuyệt đối.
- Boss completion chỉ nên tính khi condition gameplay thật sự được thỏa, không chỉ dựa vào animation/xác chết còn trên map.
- Enemy, boss, player đều là thực thể nên về mặt design hiện tại có thể chia sẻ một số rule nền như speed; thần thức cũng đã được note là áp cho quái/boss.

## Acceptable current behavior

- Enemy catalog nạp đầy đủ template, spawn group, reward rule, random table và instance config từ đầu là ổn.
- Spawn group có filter theo runtime scope/public-private-instance là đúng hướng.
- Timer spawn với initial fill + respawn dần là behavior chấp nhận được cho batch 1.
- Enemy runtime đã có các state/gameplay cốt lõi:
  - patrol
  - combat
  - death
  - despawn sau delay
- Aggressive vs passive đã có khác biệt cơ bản ở logic acquire target.
- Damage ghi contribution, last hit, death event, respawn scheduling là nền tảng tốt cho reward logic.
- Reward chia theo đóng góp damage, rồi mới roll direct grant/ground drop, là hành vi phù hợp với intent coop/PvE.
- Boss instance completion chỉ tính khi không còn boss sống và boss spawn group đã initial fill xong là acceptable tạm thời.

## Mismatch vs current code

- Enum có `Objective` và `Manual` spawn mode, nhưng runtime thấy rõ chủ yếu mới đi theo **timer spawn**. Tức là data model đang rộng hơn behavior gameplay đã xác nhận.
- Nếu enemy không có skill, runtime vẫn mở attack window nhưng không thấy fallback basic attack rõ ràng. Về gameplay, điều này có thể tạo quái “vào combat nhưng không thật sự đánh được”.
- Boss out-of-combat hiện quay về patrol nhưng giữ nguyên HP. Đây có thể là intent chống reset boss, nhưng code extract không đủ để khẳng định design đã chốt như vậy.
- Dead monster còn tồn tại 2 giây trước khi despawn. Đây là runtime-friendly behavior, nhưng canonical docs cần xác nhận nó có phải intent player-facing hay chỉ là technical linger window.
- Passive enemy khi bị đánh sẽ chuyển combat như expected, nhưng canonical docs nên nói rõ đây là **passive ≠ vô hại tuyệt đối** để tránh hiểu sai.
- Reward ownership/free-for-all dùng ground reward timer config chung, nhưng chưa có design clarification đầy đủ xem đây là rule intended cho mọi loại enemy loot hay chỉ default runtime.

## Unresolved design questions

- `Objective` spawn và `Manual` spawn sẽ dùng cho những nội dung nào trong gameplay thực tế?
- Boss khi mất aggro có nên:
  - hồi full máu
  - hồi một phần
  - giữ nguyên máu như hiện tại
- Enemy không có skill có nên có **basic attack fallback bắt buộc** không?
- Thời gian corpse/despawn 2 giây có phải player-facing rule mong muốn không, hay chỉ là runtime detail có thể thay đổi tự do?
- Contribution reward có cần ngưỡng tối thiểu để chống “tag 1 hit lấy ké” không?
- Ground reward ownership cho loot quái có nên luôn theo cùng rule với reward rơi từ hệ khác không?
- Boss/world boss có cần behavior đặc biệt khác hệ enemy thường trước khi canonicalize chung không?
- Thần thức của quái/boss đã được design note định nghĩa, nhưng runtime batch này chưa cho thấy rõ interaction đó trong enemy loop; canonical docs nên nối ở mức nào?

## Clarification status (audit-driven)

_Last updated against `docs/qa/legacy-domain-coverage-audit.md` — enemy runtime domain is `needs-review`._

**Conflict doc:** `docs/conflicts/enemy-runtime-scope-and-reset-open-questions.md`

### Open question 1 — Objective and Manual spawn modes

- Code evidence: `EnemySpawnMode.Objective` and `EnemySpawnMode.Manual` exist in `GameServer/Runtime/EnemySystemTypes.cs`. The verified runtime path in `MapInstance.Runtime.cs` only shows timer spawn being exercised. No trigger path for Objective or Manual spawn was found in the inspected files.
- Design question: What gameplay content or scenarios are Objective and Manual spawn modes intended for? Which of these are currently authored in production content?
- **Needed answer:** list of concrete use cases, or a decision that only `Timer` spawn is supported in current scope and the other modes are forward-declared capability.
- Acceptable interim stance: canonical docs describe timer spawn as the confirmed behavior; flag Objective/Manual as declared data capability pending confirmation.

### Open question 2 — Boss out-of-combat HP reset behavior

- Code evidence: when a boss leaves combat (out-of-combat restore delay elapsed), `MapInstance.Runtime.cs` calls `ReturnToPatrol(...)` and enqueues an HP-changed event but does **not** restore HP to full. Non-boss enemies restore to full HP and clear contribution/aggro state.
- Design question: Is boss-no-full-reset the intended design (bosses stay wounded between encounters to prevent trivial kiting resets), or is this a gap waiting to be filled?
- If intentional: what is the intended player-facing explanation for this behavior?
- If unintentional: what should the reset rule be (full HP, partial, time-gated)?
- **Needed answer:** explicit design decision before this can be canonicalized as a boss rule.

### Open question 3 — Enemy with no skill / basic attack fallback

- Code evidence: if an enemy has no configured skills, `MonsterEntity` still opens attack windows on the normal interval but logs once that no attack skill/basic attack is configured. No fallback basic attack implementation was found in the inspected files.
- Design question: Should a content validation rule prevent authoring enemies with no attack capability, or should the runtime implement a fallback basic attack?
- Secondary question: Is "enemy in combat but dealing no damage" acceptable as a passive/trap-type design choice, or always a content error?
- **Needed answer:** either a content authoring rule (enemies must have at least one attack skill configured) or a runtime fallback design decision.

### What is already stable (can be canonicalized now)

- Enemy catalog loading at startup (templates, skills, spawn groups, reward rules, random tables, instance configs).
- Spawn group filtering by runtime scope (Any/Public/Private/Instance) and zone index.
- Timer spawn: initial fill to MaxAlive, then weighted entry selection respawn, with random position within SpawnRadius.
- Patrol/Combat state machine basics; aggressive vs passive target acquisition.
- Out-of-combat restore for non-boss: full HP restore, contribution clear, aggro clear after delay.
- Damage-contribution tracking and contribution-weighted reward distribution.
- Reward flow: contribution-split cultivation/potential, then random table roll for direct grants and ground drops.
- Boss `KillBoss` instance completion: no alive boss + boss spawn group initial fill complete.
- Ground reward ownership and free-for-all timers per existing config keys.

## Canonicalization recommendation

- Canonicalize enemy runtime thành 3 mảng riêng:
  1. **spawn + runtime scope model**
  2. **AI/combat state loop**
  3. **reward distribution + ground reward ownership**
- Với batch 1, ghi rõ timer spawning là **confirmed current behavior**, còn `Objective`/`Manual` là **declared data capability chưa đủ gameplay confirmation**.
- Canonicalize passive/aggressive theo gameplay intent rất ngắn gọn, tránh overfit vào chi tiết implementation tick.
- Ghi chú boss reset HP behavior là **needs explicit design decision** trước khi xem là canonical.
- Ghi rõ reward split theo contribution là intent hiện tại đủ mạnh để canonicalize tạm thời.
- Nối sang canonical stat docs sau này rằng enemy/boss dùng chung một số foundation stat rule với thực thể khác (ví dụ speed; thần thức ở mức design note), nhưng không khẳng định các interaction chưa thấy rõ trong batch code này.
