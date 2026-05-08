# Server Transaction Rules

## Muc tieu

Giu transaction boundary ro rang, tranh nested transaction, tranh moi service tu y mo transaction va ghi DB theo cach kho trace.

## Rule chot

1. Transaction owner nam o tang orchestration cap cao.
   - thuong la packet handler
   - hoac application/orchestration service neu flow do khong di truc tiep tu handler

2. Service cap duoi khong duoc mac dinh assume minh la transaction owner.
   - service con nen hoat dong duoc trong ambient transaction da co san
   - neu can tu mo transaction de standalone, phai support ca case da co transaction ben ngoai

3. Moi flow business atomic chi nen co 1 transaction owner.
   - equip / unequip
   - use item
   - breakthrough / allocate potential
   - craft / herb / alchemy

4. Packet/notifier khong duoc gui truoc khi write path chinh da commit xong.
   - state DB va runtime phai on dinh truoc
   - sau do moi push packet result hoac packet changed

5. Method nao co side effect ghi DB phai lo ro.
   - ten method va layer phai de trace
   - tranh kieu method trong nhu helper nhung thuc te lai update DB va mo transaction

## Rule thuc dung hien tai

Hien tai repo chua duoc refactor het theo model transaction-owner duy nhat. Vi vay tam thoi ap dung them rule compatibility:

- service nao dang co kha nang standalone va tu mo transaction
  - van duoc phep giu behavior do
  - nhung bat buoc phai chiu duoc ambient transaction da ton tai

Vi du:

- `CharacterService.UpdateCharacterRuntimeSnapshotAsync(...)`
  - neu da co `_db.Transaction` thi khong mo transaction long nua
  - neu chua co transaction thi van tu mo transaction de giu backward compatibility

## Khong lam

- Khong de handler mo transaction ngoai roi service con mo transaction long tren cung `GameDb`
- Khong de moi feature tu nghi ra transaction rule rieng
- Khong gui packet changed truoc commit roi moi hy vong DB thanh cong sau
- Khong refactor toa bo repo trong 1 lan neu khong co ly do rat manh

## Cach refactor dung huong

Refactor theo tung cum feature, khong dap ca repo mot luc.

Thu tu uu tien goi y:

1. equipment + final stats + skill sync
2. cultivation + breakthrough + potential
3. item use
4. craft / herb / alchemy

Moi cum feature nen di ve mot shape nhu sau:

- transaction owner
  - mo transaction
  - goi cac buoc con
  - commit
- service con
  - tinh logic
  - update qua repository
  - khong assume minh luon la owner transaction
- notifier
  - push packet sau commit

## Checklist review nhanh

Khi sua flow server co ghi DB, tu hoi 5 cau nay:

1. Transaction owner cua flow nay la ai?
2. Co chuyen service con mo transaction long khong?
3. Co write nao co the partial neu giua duong loi khong?
4. Packet/result co dang gui truoc commit khong?
5. Flow nay da theo dung rule chung hay dang them ngoai le moi?
