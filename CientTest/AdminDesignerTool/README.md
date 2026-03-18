# Admin Designer Tool

Tool admin MVP de game design config truc tiep cac bang template trong PostgreSQL.

## Muc tieu

- Them, sua, xoa cac resource template ma khong can vao SQL tay.
- Gom cac bang quan trong theo nhom de game design de tim.
- De mo rong dan khi server co them schema moi nhu boss, drop table, quest, NPC.

## Cac nhom hien co

- `Cong Phap`
- `Item & Equipment`
- `Che Tao`
- `Balance`
- `World`
- `Mo Rong Sau Nay`

## Cach chay

```powershell
dotnet run --project CientTest/AdminDesignerTool/AdminDesignerTool.csproj
```

Tool se tu tim `GameServer/Config/dbConfig.json` tu thu muc hien tai di nguoc len.

## Ghi chu

- Day la generic table editor, nen FK / enum van dang hien duoi dang so.
- Trong UI da co `Description` va `HelpText` cho tung resource de game design biet thu tu config.
- Co san cac thao tac:
  - `Tai Lai`
  - `Them Dong`
  - `Nhan Ban Dong`
  - `Xoa Dong`
  - `Luu Thay Doi`
  - `Loc nhanh`

## Gioi han phase nay

- Chua co form chuyen biet cho tung loai resource.
- Chua co dropdown FK / enum lookup.
- Chua co schema boss/drop that, nen moi dat san diem mo rong trong navigation.
- Chua co auth/permission; hien la tool local noi thang vao DB.
