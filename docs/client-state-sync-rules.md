# Client State Sync Rules

## Muc tieu

Giu rule dong bo state client don gian, de trace bug, khong de moi panel tu fetch theo cach rieng.

## Rule chot

1. `EnterWorld` luon bootstrap snapshot cho cac subsystem panel can.
   - `character` lay tu `EnterWorldResult`
   - `inventory`, `martial arts`, `skills` load 1 lan ngay sau `EnterWorld`

2. Sau bootstrap, local state chi doi theo 2 nguon:
   - packet push tu server
   - action result tra du data de update local state

3. Neu action result khong du data de client update chac chan:
   - reload lai dung subsystem do trong service xu ly action
   - khong day fallback reload xuong panel/controller

4. Panel/controller khong tu fetch data.
   - panel chi nghe state va render
   - khong polling de tu retry load missing data

5. Reconnect thanh cong thi chay lai bootstrap snapshot giong `EnterWorld`
   - hien tai reconnect di qua `EnterWorldAsync`, nen ap dung cung rule

## Ownership

- `ClientCharacterService`
  - nhan `EnterWorldResult`
  - apply `character` state
  - kick bootstrap load cho `inventory`, `martial arts`, `skills`

- Tung feature service
  - so huu local state cua subsystem
  - nghe packet/result packet
  - tu quyet dinh co can reload subsystem cua minh hay khong

- Panel/controller
  - khong duoc la owner cua fetch/reload
  - chi doc state, nghe event, va render UI

## Khong lam

- Khong them fallback polling trong panel de auto-reload missing data
- Khong de moi panel tu nghi ra mot rule sync rieng
- Khong them co che resync phuc tap hon tru khi da co bug drift that va lap lai
