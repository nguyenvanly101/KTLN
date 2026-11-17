# Ảnh cho Khám phá: Wikimedia và ảnh nhà cung cấp

## Nguồn ảnh

| Loại mục | Nguồn | Ghi công |
| --- | --- | --- |
| Cơ sở, dịch vụ | Ảnh nhà cung cấp tự tải (form cơ sở, form dịch vụ) | "Ảnh do cơ sở cung cấp" |
| Tỉnh/thành | Bài Wikipedia tiếng Việt trùng tên, ảnh trên Wikimedia Commons | Tác giả · giấy phép · Wikimedia Commons |
| Địa điểm, khu vực | OpenTripMap (khớp tên trong bán kính 400 m), dự phòng bằng bài Wikipedia trùng tên cách ≤ 3 km | Tác giả · giấy phép · Wikimedia Commons |
| Chưa có ảnh | Nền màu theo loại (tham quan, ăn uống, lưu trú, di chuyển) | — |

- Goong Place Detail không trả ảnh (chỉ tên, địa chỉ, tọa độ, loại), nên không dùng làm nguồn ảnh.
- Ảnh nhà cung cấp luôn được ưu tiên. Bước lập chỉ mục (`DiscoveryIndexWorker`, chạy mỗi phút) gán ảnh đầu tiên của cơ sở/dịch vụ vào mục tra cứu và gỡ ảnh khi cơ sở không còn công khai.
- Chỉ nhận ảnh có giấy phép CC0, Public domain, CC BY, CC BY-SA. Ảnh lưu riêng trên vi.wikipedia (thường là ảnh không tự do) bị bỏ.

## Dữ liệu

- Script `Database/035_AnhMinhHoa.sql` thêm hai cột vào `DiscoveryEntry`:
  - `ImageCredit` (tác giả, giấy phép);
  - `ImageSourceUrl` (trang tệp trên Commons).
- Script cũng tạo bảng `AnhDonVi` (ảnh tỉnh/thành, khóa `DonViId`).
- Ảnh Wikimedia được tải về `wwwroot/anh-tra-cuu/` và không đưa vào git. Khách xem trang không phải gọi ra ngoài, và không phải mở CSP cho máy chủ ảnh lạ.

## Lấy ảnh Wikimedia

1. Tìm và tải ảnh. Bước này chỉ đọc cơ sở dữ liệu, ảnh vào thư mục tạm:

   ```
   node tools/wikimedia-photos/fetch.cjs <thu-muc-tam> [--limit=N]
   ```

   Khóa OpenTripMap đọc từ kho bí mật của dự án (`Discovery:OpenTripMap:Key`). Nếu không có khóa, công cụ chỉ dùng Wikipedia theo tên.

2. Mở `<thu-muc-tam>/xem-truoc.html` và ghi lại mã các ảnh khớp sai (T = tỉnh/thành, M = mục tra cứu).

3. Sinh SQL và chép ảnh:

   ```
   node tools/wikimedia-photos/build-sql.cjs <thu-muc-tam> --loai=T12,M345 --chep
   ```

4. Chạy `<thu-muc-tam>/wiki-anh.sql` trên cơ sở dữ liệu. SQL chỉ gán ảnh cho mục chưa có ảnh, nên không đè ảnh nhà cung cấp.

Công cụ có cache, nên chạy lại không gọi mạng lặp. Mỗi giây gọi khoảng một lần tới Wikimedia.

## Gỡ ảnh khớp sai

Quản trị viên mở trang sửa mục tra cứu. Khối "Ảnh minh họa" hiện ảnh và ghi công; đánh dấu "Gỡ ảnh này khi lưu" rồi lưu.

## Kiểm thử

Chạy `tests/photo-sources.cjs` trên máy chủ thử. Bộ này kiểm:

- ảnh cơ sở vào mục tra cứu;
- gỡ ảnh khi cơ sở không còn công khai;
- trang chi tiết hiện ghi công;
- quản trị gỡ được ảnh;
- ảnh tỉnh từ `AnhDonVi`;
- nhắc nhà cung cấp thêm ảnh.
