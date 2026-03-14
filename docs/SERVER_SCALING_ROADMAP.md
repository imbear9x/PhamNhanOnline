# Server Scaling Roadmap

Roadmap nay duoc viet theo codebase hien tai cua `PhamNhanOnline`.
Muc tieu la gia co nen tang som de sau nay them movement, multiplayer, combat, quest ma khong vo he thong.

## Cach dung roadmap

- Lam theo thu tu tu tren xuong duoi.
- Khong nhay qua phase sau neu phase truoc chua on.
- Moi muc nen duoc merge va test xong roi moi qua muc tiep.
- Uu tien nhung thay doi giam rui ro kien truc truoc khi them tinh nang.

## Nguyen tac thiet ke

- Server la authoritative.
- Tach networking, simulation, persistence thanh cac luong trach nhiem ro rang.
- Chi gui du lieu cho nguoi can nhan.
- Khong de world update bi block boi DB.
- Packet realtime va packet nghiep vu khong dung chung mot kieu truyen.

## Phase 0 - Khoa nen kien truc

Muc tieu:
- Co tai lieu va ranh gioi ro rang truoc khi them tinh nang lon.

Viec can lam:
- Chot conventions cho packet:
  - packet request/result
  - packet event/broadcast
  - packet state sync
- Chot conventions cho runtime state:
  - du lieu nao la authoritative tren server
  - du lieu nao chi la hien thi tren client
- Chot conventions cho `Map`, `MapInstance`, `PlayerSession`, `CharacterRuntimeState`.
- Viet tai lieu flow co ban:
  - dang nhap
  - vao character
  - vao world
  - runtime save
  - state change

Xong phase khi:
- Nguoi moi vao doc docs la hieu server dang van hanh theo flow nao.

## Phase 1 - Tach workload nguy hiem khoi main loop

Muc tieu:
- Khong de packet receive va world update bi block boi thao tac cham.

Viec can lam:
- Refactor `NetworkServer` de khong block luong nhan packet boi handler cham.
- Giu packet cua cung mot player duoc xu ly theo thu tu, nhung khong khoa ca server.
- Tach persistence khoi `GameLoop`:
  - world loop chi danh dau viec can save
  - worker rieng lo batch save xuong DB
- Tach `RefreshTimeDerivedStateForOnlinePlayersAsync` khoi duong di de gay hitch neu sau nay no nang.
- Them cancellation, shutdown sequence, va flush an toan.

Xong phase khi:
- Mot thao tac DB cham khong lam ca world tick dung lai.
- Packet cua player A cham khong lam player B cam thay lag ro.

## Phase 2 - Chuan hoa tick va scheduler

Muc tieu:
- Co simulation tick on dinh va de do dac.

Viec can lam:
- Thay `Thread.Sleep(...)` bang loop co do elapsed time.
- Ghi nhan tick duration, tick overrun, queue depth.
- Tach:
  - simulation tick
  - network send tick
  - persistence tick
- Chot tan so tick mac dinh:
  - world tick
  - movement snapshot tick
  - save tick
- Them metrics/log co cau truc cho:
  - so player online
  - so map instance
  - packet in/out moi giay
  - thoi gian xu ly packet

Xong phase khi:
- Co the nhin log/metric de biet server dang cham o dau.

## Phase 3 - Packet transport strategy

Muc tieu:
- Dung dung delivery mode cho tung loai du lieu.

Viec can lam:
- Phan loai packet:
  - reliable ordered cho login, inventory, trade, quest, state transition quan trong
  - realtime channel cho movement/rotation/animation/state tan suat cao
- Them packet categories trong code thay vi moi thu deu gui cung mot kieu.
- Them correlation id cho action packet se can sau nay:
  - skill cast
  - attack
  - interaction
  - trade request
- Review lai rate limit:
  - bo rate limit chung theo connection
  - thay bang rate limit theo packet type / bucket

Xong phase khi:
- Co quy tac ro rang packet nao dung kenh nao.
- Movement sau nay khong bi thiet ke buoc phai di qua reliable ordered.

## Phase 4 - Interest management va spatial partition

Muc tieu:
- Khong broadcast toan map/toan server mot cach mu quang.

Viec can lam:
- Them khai niem "watchers" / "observers" cho entity.
- Chot cach xac dinh ai nhin thay ai:
  - cung map instance
  - trong tam nhin
  - trong cell / grid
- Bo sung spatial partition:
  - grid la lua chon don gian va hop ly cho giai doan dau
- Them cac packet:
  - entity spawned
  - entity despawned
  - entity moved
  - entity state changed
- Chi gui su kien cho nhung client co lien quan.

Xong phase khi:
- So packet gui ra tang theo khu vuc co nguoi, khong tang theo tong so player toan server.

## Phase 5 - Runtime state va dirty replication

Muc tieu:
- Khong gui lai ca state khi chi doi 1 phan nho.

Viec can lam:
- Chia state thanh nhom:
  - stats co ban
  - current state
  - movement state
  - combat state
  - appearance state
- Co dirty flags rieng cho network replication, khong chi cho DB persistence.
- Ho tro delta update thay vi gui full state moi lan.
- Gom packet nho thanh batch hop ly neu can.

Xong phase khi:
- Thay doi nho chi phat sinh update nho.

## Phase 6 - Persistence strategy

Muc tieu:
- Luu DB an toan ma khong lam cham gameplay.

Viec can lam:
- Chuyen sang save queue/background worker.
- Gom save theo batch.
- Chot chinh sach save:
  - periodic save
  - disconnect flush
  - critical event flush
- Phan tach du lieu:
  - du lieu can save ngay
  - du lieu co the save tre
- Xem xet snapshot + event log cho cac he thong lon ve sau.

Xong phase khi:
- Online player tang len ma world tick van on.

## Phase 7 - Multiplayer movement foundation

Muc tieu:
- Co nen de nhin thay va di chuyen cung nhau trong map.

Viec can lam:
- Chot model movement server-authoritative hoac hybrid co reconciliation.
- Them packet:
  - move input
  - move snapshot
  - teleport / correction
- Client interpolation cho player khac.
- Client prediction co kiem soat cho player local neu can.
- Rate limit rieng cho movement.

Xong phase khi:
- 2-10 player cung map di chuyen muot ma khong spam packet vo toi va.

## Phase 8 - Combat-ready infrastructure

Muc tieu:
- San san cho skill/combat ma khong phai sua nguoc kien truc.

Viec can lam:
- Them action id / command id cho combat packet.
- Tach:
  - request cast
  - result accept/reject
  - combat event broadcast
- Chuan hoa cooldown timing theo server authority.
- Chuan hoa target validation, range validation, state validation.
- Them combat event queue neu can.

Xong phase khi:
- Co the them skill dau tien ma khong phai doi lai network model.

## Phase 9 - Quest, trade, social

Muc tieu:
- Them he thong nghiep vu tren nen da on dinh.

Viec can lam:
- Quest packets theo reliable flow.
- Trade request/result/cancel/confirm.
- Friend/guild/chat theo packet type rieng.
- Logging, audit va anti-abuse cho nghiep vu quan trong.

Xong phase khi:
- Cac he thong nghiep vu khong anh huong xau toi realtime gameplay.

## Phase 10 - Observability va load test

Muc tieu:
- Biet server chiu duoc den dau truoc khi dua them tinh nang lon.

Viec can lam:
- Them counters va metrics co the quan sat duoc.
- Viet bot/client gia lap:
  - login
  - vao map
  - di chuyen
  - spam packet hop le
- Chay load test theo moc:
  - 10 player
  - 50 player
  - 100 player
  - nhieu instance
- Ghi ket qua va bottleneck cua tung moc.

Xong phase khi:
- Moi thay doi lon deu co cach do anh huong performance.

## Thu tu uu tien nen lam ngay

Neu chi chon nhung viec nen lam rat som, uu tien theo thu tu nay:

1. Viet docs/rule nen kien truc trong Phase 0.
2. Tach DB save khoi `GameLoop`.
3. Sua model xu ly packet de khong block toan server.
4. Chuan hoa delivery mode va packet categories.
5. Bo rate limit tho hien tai, thay bang rate limit theo packet type.
6. Them metrics/log de biet server dang cham o dau.
7. Thiet ke interest management truoc khi lam multiplayer movement that su.

## Thu tu lam viec de nho Codex

De lam lan luot, co the dua lenh theo dang:

- "Lam Phase 0, muc 1"
- "Lam tiep Phase 1, tach DB save khoi GameLoop"
- "Lam Phase 3, thiet ke packet categories"
- "Lam metrics co ban cho tick va packet"

## Ghi chu cuoi

Roadmap nay co chu y khong "toi uu qua som" mot cach mo ho.
No tap trung vao nhung diem ma neu bo qua, sau nay them movement/combat/quest se de gay vo he thong nhat.
