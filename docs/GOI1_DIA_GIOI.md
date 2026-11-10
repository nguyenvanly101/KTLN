# Gói 1 — Địa giới hành chính

Trạng thái: **dữ liệu đã dựng và kiểm thử trên database tạm (đã xóa). Chưa áp dụng vào database chính `HoTroDuLich`, chưa sửa mã C#.**

## 1. Nguồn và dữ liệu đã dựng

Nguồn: bảng chuyển đổi đơn vị hành chính mới–cũ của Tổng cục Thống kê (NSO, cổng `danhmuchanhchinh.nso.gov.vn`, tệp cập nhật 23/10/2025), lưu tại `data/dia-gioi/nguon/`. Công cụ dựng: `python tools/BuildDiaGioi.py` (chỉ dùng thư viện chuẩn, chạy lại được).

| Hệ | Nội dung | Số lượng |
|---|---|---|
| **Cũ** (trước 01/07/2025) | 63 tỉnh/thành → quận/huyện/thị xã → xã/phường/thị trấn | 63 / 696 / 10.035 |
| **Mới** (từ 01/07/2025) | 34 tỉnh/thành → xã/phường/đặc khu (không còn cấp huyện) | 34 / 3.321 |
| **Ánh xạ cũ → mới** | Mỗi xã cũ nhập toàn bộ/một phần vào xã mới nào | 10.571 cặp |

Kiểm tra: cây cha–con đúng cấp, mọi xã cũ đều có ánh xạ, mọi xã mới đều có nguồn cũ. Ví dụ: Đà Nẵng (cũ) có 8 quận/huyện; Đà Nẵng (mới, gồm Quảng Nam cũ) có 94 phường/xã/đặc khu.

Tệp sinh ra: `data/dia-gioi/dvhc_cu.csv`, `dvhc_moi.csv`, `chuyen_doi.csv`, `bao_cao_lam_sach.txt`, và `Database/008c_NapDiaGioi.sql`.

## 2. Lỗi trong nguồn đã xử lý (chi tiết ở `bao_cao_lam_sach.txt`)

| Lỗi | Cách xử lý |
|---|---|
| Khoảng trắng thừa, chữ hoa/thường lẫn, ký tự Unicode tổ hợp, thiếu dấu ("Xa Ea Kly", "Thi trấn…") | Chuẩn hóa NFC, nhận diện tiền tố không phân biệt hoa/thường/dấu |
| 1 xã mới có 2 cách viết ("Điềm Thụy"/"Điềm Thuỵ") | Chọn cách viết phổ biến nhất |
| Huyện Cồn Cỏ ghi nhầm tỉnh "Quảng Trị (44)" | Sửa thành Quảng Trị (45) |
| 2 huyện có 1 dòng ghi sai tỉnh (Thành phố Tân An, Thành phố Hà Tiên) | Chọn tỉnh theo đa số dòng |
| 3 cặp xã cũ bị gõ trùng mã (Cổ Bi/Đặng Xá; Hồng Quang/Mỹ Hưng; Công Đa/Phú Thịnh) | Đặng Xá tự xác định được (00556). **Mỹ Hưng và Phú Thịnh đang dùng mã tạm `01615X`, `02485X`, cần đối chiếu với danh mục gốc** |
| 2 xã cũ ở Phú Yên thiếu tiền tố loại (Xuân Quang 3, Xuân Phước) | Mặc định "Xã", ghi trong báo cáo |

## 3. Thiết kế dữ liệu (script `008`)

| Bảng | Vai trò |
|---|---|
| `DonViHanhChinh` | Cả hai hệ trong một bảng, phân biệt bằng `Regime` (`Cu`/`Moi`). `Level`: 1 tỉnh, 2 huyện (chỉ hệ Cũ), 3 xã. Mã nguồn duy nhất theo (`Regime`,`Code`) |
| `DonViChuyenDoi` | Ánh xạ xã cũ → xã mới (nhiều–nhiều) kèm ghi chú "Nhập toàn bộ/một phần" |
| `TenGoiKhacDonVi` | Tên gọi khác, viết tắt, tên nước ngoài ("Danang", "TP.HCM"). Chưa nạp dữ liệu |
| `KhuDuLich` + `KhuDuLichDonVi` | Lớp chồng: Bán đảo Sơn Trà, Phố cổ Hội An… Chưa nạp dữ liệu |
| `ChuyenDoiDiaGioiCho` | Hàng chờ cho khu vực cũ không tự gán được |
| Cột `DonViId` (NULL) | Thêm vào 7 bảng đang dùng khu vực dạng chuỗi; cột chuỗi cũ được giữ nguyên |

`Status` của đơn vị nghĩa là "hệ thống có dùng để chọn hay không", không phải trạng thái pháp lý (pháp lý nằm ở `EffectiveFrom/EffectiveTo`: hệ Cũ kết thúc 30/06/2025, hệ Mới bắt đầu 01/07/2025).

## 4. Chuyển dữ liệu khu vực cũ (`008b`, `usp_GanDonViChoKhuVucCu @Regime`)

`@Regime` chọn hệ dùng làm gốc (mặc định `Cu`, theo yêu cầu hiện tại). So khớp tên hoặc tên đầy đủ, không phân biệt hoa/thường/dấu:
- Khớp đúng một đơn vị → tự gán `DonViId`. Tên trùng nhiều cấp (Bắc Giang vừa là tỉnh vừa là thành phố) → lấy cấp cao nhất nếu cấp đó chỉ có một đơn vị.
- Khớp nhiều đơn vị ("Phường 1") → hàng chờ `Ambiguous`. Không khớp → hàng chờ `NoMatch`. "Việt Nam" bị bỏ qua.
- Chạy lại được; chỉ xử lý bản ghi còn `DonViId IS NULL`.

Kết quả thử: "Đà Nẵng", "da nang", "Thành phố Đà Nẵng" → tỉnh; "Quận Hải Châu", "Hải Châu" → quận; "Bắc Giang" → tỉnh; "Phường 1" → Ambiguous; "Xứ lạ" → NoMatch.

## 5. Quyết định thiết kế cần biết

- **Hệ nào làm gốc?** Bạn yêu cầu 63 tỉnh cũ nên mặc định `Cu`. Vì địa chỉ hợp lệ từ 01/07/2025 theo hệ Mới, hệ Mới vẫn được nạp để đổi gốc về sau qua bảng ánh xạ.
- Ánh xạ cũ → mới là **nhiều–nhiều** (một xã cũ có thể tách vào nhiều xã mới), nên không thể đổi `DonViId` hàng loạt bằng một phép gán đơn giản; cần quy tắc cho trường hợp "nhập một phần".
- Số quận/huyện nạp được là **696**. Nguồn chỉ liệt kê huyện có dòng chuyển đổi; 5 huyện đảo (Bạch Long Vĩ, Cồn Cỏ, Hoàng Sa, Lý Sơn, Côn Đảo) có huyện nhưng không có xã con. Tôi chưa đối chiếu được với tổng số huyện công bố chính thức, nên nên kiểm tra trước khi coi là đầy đủ.

## 6. Việc còn lại

1. Khởi tạo Git; sao lưu (`tools/SaoLuu.ps1`); **dừng web**.
2. Chạy `008` → `008c` → `008b` trên `HoTroDuLich` (chưa chạy) và xử lý hàng chờ.
3. Sửa mã C#: entity + cấu hình cho các bảng mới, thêm `DonViId` vào 7 entity; API `/api/dia-gioi/...`; biểu mẫu chọn tỉnh → huyện → xã; lọc tìm kiếm theo `DonViId` kèm đơn vị con; màn hình xử lý hàng chờ.
4. Cập nhật `Quyen_CSDL.sql` (quyền cho bảng mới), `docs/*-schema.json` và bộ kiểm thử có kiểm tra số bảng/cột.
5. Đối chiếu 2 mã tạm (`01615X`, `02485X`) và nạp `TenGoiKhacDonVi`, `KhuDuLich`.
6. Script chưa nối vào `SchemaDatabase`; cố ý để chạy tay. Chạy `008b` làm đổi `RowVersion` của dịch vụ/cơ sở nên phải dừng web.
