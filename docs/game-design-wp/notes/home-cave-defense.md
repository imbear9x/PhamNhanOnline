# Dong Phu / Cong Dong Phu / Cuop Boc — Design Notes

**Ngay tao:** 2026-05-08
**Trang thai:** Da chot co ban

---

## Thuat ngu

- **Than Thuc Quan**: nguong than thuc yeu cau de mot thuc the co the nhin thay, tuong tac, hoac tan cong mot doi tuong.

---

## 1. Vong doi Dong Phu

### Dong phu khoi dau
- Ngay khi tao account, ai cung co 1 **dong phu vinh vien** dang **private home**
- Dong phu nay **khong ai tan cong duoc** vi nam trong map private

### Chuyen sang dong phu that ngoai the gioi
- Den mot moc quest nhat dinh, nguoi choi nhan **Ban Ve Dong Phu**
- Ban ve dung de **mo dong phu** tai vi tri trong the gioi
- Trang thai quyen mo:
  - **Chua mo** -> ban ve nam trong kho do
  - **Da mo** -> ban ve bien mat
- Muon mo o noi khac -> phai **mua ban ve moi tu NPC**
- Ban ve **khong drop, khong giao dich duoc**

### Gioi han
- **1 nguoi = 1 dong phu active** tai mot thoi diem

### Thu don dong phu
- Nguoi choi co the chu dong **thu don** bat cu luc nao
- Thu don xong -> **ban ve dong phu hoi lai kho do**
- Phai thu don cai cu truoc moi duoc mo cai moi

### Sau khi mo dong phu that
- Dong phu private ban dau **bien mat vinh vien**

### Khi cong dong phu bi pha hoan toan
- Dong phu vao **phase sup do trong 1 phut**
- Chu nha bi **teleport ra ngoai ngay** khi cong vo
- Neu chu van gan map do co the **chay vao lai** trong 1 phut nay
- Tat ca moi nguoi trong map cong va dong phu van co the **danh nhau** trong thoi gian nay
- Sau 1 phut: tat ca bi **day ra map ngoai** noi dat dong phu
- Dong phu bien mat, o cell trong tro lai
- **Ban ve dong phu hoi lai kho do** cua chu nha
- Khi chet ma khong co dong phu nao de ve -> hoi sinh ngau nhien o map public

### Nang cap
- **Khong nang cap duoc**
- Suc manh phu thuoc **cap ban ve** dung de mo

---

## 2. Mo Dong Phu tren map

- Phai mo tai **o cell hop le** trong map duoc config cho phep
- O do phai **chua co dong phu nguoi khac**
- Khi mo:
  - chiem 1 o cell tren map
  - hien thi **ten dong phu**
  - nguoi di qua nhin thay neu vuot duoc **Than Thuc Quan**

### Cap dong phu / Than Thuc Quan
- Cap ban ve = cap dong phu
- Cap dong phu quyet dinh **Than Thuc Quan** cua dong phu
- Nguoi khong vuot duoc Than Thuc Quan -> **khong nhin thay, khong tuong tac, khong tan cong duoc**

---

## 3. Cau truc Dong Phu ben trong

Cac UI/chuc nang chi mo duoc khi o trong dong phu:
- **Mat That** — tu luyen
- **Dan That** — luyen dan
- **Luyen Khi That** — luyen phap bao, phu luc, tran phap
- **Linh Thu That** — quan ly / nuoi linh thu
- **Duc Linh That** — sinh san va ap trung linh thu
- **Cong ra Cua Dong Phu** — di ra khu map phong thu

### Luyen che / tu luyen trong dong phu
- Tat ca luyen che va tu luyen **bat buoc player phai ngoi tai phong**
- Luyen xong -> san pham **tu vao balo ngay**, khong co trang thai "chua claim"
- **Ngoai le:** ap trung va de trung trong **Duc Linh That** -> trung/san pham nam trong ruong phong den khi duoc lay

---

## 4. Khach / tham dong phu

### Dieu kien vao tham binh thuong
- Can **2 yeu to dong thoi**:
  - La **ban be** cua chu nha
  - Duoc chu nha **gui loi moi**
- Phai **dung canh dong phu** thi chu moi gui loi moi duoc
- Vao chi duoc **di trong map**, khong mo ruong hay lam gi duoc

### Nguoi tan cong thanh cong
- Sau khi pha cong vao duoc ben trong:
  - co the **mo ruong chua do** va cuop tai san
  - **Trung va san pham Duc Linh That** chua duoc lay -> co the bi cuop
  - Do **dang trong tui player** -> khong lay duoc truc tiep

---

## 5. Tai san / rule cuop boc

### Co the bi cuop
- Ruong chua do trong dong phu
- Trung va san pham Duc Linh That chua duoc lay

### Khong the bi cuop truc tiep
- **Do dang trong tui player** — chi co ti le rot neu player bi giet

### Rui ro cho nguoi di cuop
- Mang nhieu do thi **ti le rot cao hon khi bi giet**
- Mang it do thi an toan hon nhung yeu hon
- Day la tradeoff player tu quyet dinh
- Ti le cu the xac dinh o phase design data / balance

---

## 6. Cua Dong Phu / Phong thu

### Map Cua Dong Phu co the co
- **Tran phap phong thu**
- **Linh thu phong thu**
- **Cong dong phu** co HP rieng

### Cong dong phu hoi HP
- Tu hoi theo thoi gian sau khi bi danh nhung chua vo
- Khong can item/tai nguyen sua chua

### Cac loai tran phap phong thu
- **Tran phap tan cong**
- **Tran phap tang suc phong thu cho cong**
- **Tran phap tang Than Thuc Quan cua dong phu**

### Nhieu nguoi cong cung luc — Free-for-all
- Nhieu player co the **cung luc** vao map Cua Dong Phu
- **Khong co phe phai, khong co dong minh** trong map nay
- Moi nguoi tu quyet dinh uu tien: pha tran phap, linh thu, cong, hay danh nhau
- Cong vo -> ai vao truoc lay truoc
- Intentional design — tao drama, canh tranh giua cac ke cuop voi nhau

### Chu nha phong thu
- Neu chu online va dang trong dong phu -> nhan thong bao
- Chon OK -> chuyen thang toi map Cua Dong Phu de phong thu

### Chet khi phong thu
- Roi nhieu item / linh thach hon binh thuong
- **Khong the hoi sinh** cho toi khi nguoi tan cong roi khoi toan bo map dong phu + cua dong phu

---

## 7. Gia phai tra khi di cong dong phu

- **Ti le rot do khi chet**: gap 2-3 lan binh thuong
- **Penalty tho nguyen khi chet**: nang gap 2-3 lan PK thuong
- **Cooldown tan cong**: **per player** — sau moi lan tan cong phai cho 1-2 ngay truoc khi tan cong dong phu bat ky tiep theo
- Dong phu khong co cooldown rieng — cu du dieu kien la co the bi cong
- He so nhan cu the xac dinh o phase balance

---

## 8. Pet thu nha khi dong phu bi pha

- Pet con song -> ve tui linh thu cua chu
- Pet da chet -> ve tui linh thu cua chu va ngu hoi phuc

---

## 9. Lien quan voi he thong khac

- **Linh thu**: co the duoc de lai thu nha
- **Tran phap**: co the dat lam lop phong thu map cong
- **Death penalty**: chet khi thu phu / cong phu co penalty nang hon binh thuong
- **Than Thuc Quan**: quyet dinh viec nhin thay/tuong tac voi dong phu
- **Khai thac linh thach**: tinh nang rieng, khong dien ra trong dong phu
- **Practice sessions**: luyen che xong -> tu vao balo. Duc Linh That la exception — trung/san pham co the bi cuop
