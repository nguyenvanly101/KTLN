# Dữ liệu khu du lịch và địa điểm nổi tiếng (13/11/2026)

Đã nhập **102 địa điểm** trên cả nước vào CSDL chính `HoTroDuLich` (tổng 105 địa điểm và 105 mục Khám phá loại Địa điểm đã công bố).

## Cách nhập

Script sinh ra bởi `Database/data/gen-khu-du-lich.js`, chạy lại được, không tạo bản trùng (bỏ qua nếu đã có cùng tên và khu vực):

1. `Database/data/khu-du-lich-1-nguon.sql`: thêm vào bảng nguồn `DiaDiemDuLich` (tên, loại, khu vực, mô tả, liên kết tỉnh theo hệ địa giới cũ).
2. Chờ tiến trình đồng bộ chỉ mục Khám phá (chạy mỗi phút khi website đang chạy) tạo mục Khám phá tương ứng.
3. `Database/data/khu-du-lich-2-bosung.sql`: bổ sung tọa độ, từ khóa, nguồn và ngày kiểm tra cho các mục đó.

Chạy bằng `sqlcmd -I -b -f 65001 -i <tệp>`. Các script này **không** nằm trong danh sách script tự chạy khi khởi động nên không ảnh hưởng CSDL kiểm thử.

## Phạm vi và chất lượng

- Gồm các điểm nổi tiếng của Hà Nội, Quảng Ninh, Hải Phòng, Ninh Bình, Lào Cai, Hà Giang, Cao Bằng, Tây Bắc, Thanh Hóa, Nghệ An, Quảng Bình, Huế, Đà Nẵng, Quảng Nam, Quảng Ngãi, Bình Định, Khánh Hòa, Ninh Thuận, Bình Thuận, Lâm Đồng, Kon Tum, TP. Hồ Chí Minh, Bà Rịa – Vũng Tàu, Kiên Giang, Cần Thơ, Cà Mau, Đồng Nai, Tây Ninh. **Chưa phải mọi tỉnh hay mọi địa điểm.**
- **Tọa độ là gần đúng**, đã ghi trong `LocationNote`; chưa đối chiếu với nguồn bản đồ chính thức.
- Mô tả viết lại ngắn, **không có giá vé và giờ mở cửa** vì dễ sai theo thời gian. Nguồn ghi: "Tổng hợp từ thông tin du lịch công khai, biên soạn lại".
- Chưa có ảnh thật; mục chưa có ảnh dùng ảnh minh họa chung của website.
- Cách nên bổ sung: quản trị viên nhập hoặc duyệt thêm trong **Quản lý Khám phá** (`/Discovery/Manage`).

## Nhập địa điểm từ Goong Maps (13/11/2026)

Theo yêu cầu và sự cho phép của chủ website, đã dùng khóa **REST** của Goong để lấy thông tin địa điểm rồi nhập vào bảng `DiaDiemDuLich` (thông tin tham khảo, ghi nguồn "Goong Maps (thông tin tham khảo)" và ngày kiểm tra). Công cụ ở `tools/goong-import/`:

1. `node tools/goong-import/fetch.cjs <thư-mục-tạm>`: với từng điểm đến (63 tỉnh theo địa giới cũ + các khu vực đã có), tìm tâm (Geocode), tìm theo từ khóa (khu du lịch, điểm tham quan, bãi biển, thác nước, chùa, bảo tàng, di tích lịch sử; với khu vực còn có chợ, nhà hàng đặc sản, khách sạn) bằng Autocomplete rồi lấy tọa độ bằng Place Detail, chỉ giữ nơi trong 35 km quanh tâm. Kết quả ghi vào `cache.json` (để chạy tiếp được) và `staging.json`. Khóa đọc từ kho bí mật, không in ra.
2. `node tools/goong-import/build-sql.cjs <thư-mục-tạm> <thư-mục-ra>`: lọc chất lượng (tên phải chứa từ khóa đúng loại, bỏ tên chung chung, bãi xe, cửa hàng…, bỏ trùng tên và vị trí) và tạo hai tệp SQL.
3. Chạy `goong-1-nguon.sql`, đợi tiến trình đồng bộ chỉ mục Khám phá (1–2 phút), rồi chạy `goong-2-bosung.sql` (tọa độ, địa chỉ, từ khóa, nguồn). Cả hai chạy lại được, không tạo bản trùng.

Lượt đầu nhập **76 địa điểm** (Bến Tre, Bình Định, Bình Dương, Bình Phước, Bình Thuận, Cà Mau, Cần Thơ, Cao Bằng) sau lọc từ 155 kết quả. Tổng hiện có 181 địa điểm.

**Cập nhật 14/11/2026:** đợt hai nhập thêm 327 địa điểm (tổng 508), hết hạn mức tại điểm đến Hà Nội; còn khoảng 70 điểm đến chạy tiếp ngày mai.

**Hạn mức:** gói miễn phí của Goong chỉ cho **1.000 yêu cầu mỗi ngày** (tài khoản dùng chung với các gợi ý trực tiếp trên website). Lượt đầu đã dùng hết hạn mức trong ngày nên các điểm đến còn lại (khoảng 90) cần chạy tiếp vào những ngày sau: chạy lại `fetch.cjs` với cùng thư mục tạm (kết quả đã lấy được giữ trong cache), rồi `build-sql.cjs` và hai tệp SQL.

**Giới hạn chất lượng:** dữ liệu Goong chỉ có tên, địa chỉ và tọa độ; **không có giờ mở cửa, giá, mô tả hay ảnh**. Mô tả ghi rõ "thông tin tham khảo, chưa xác nhận giờ mở cửa và giá". Autocomplete khớp theo từ nên vẫn có thể lẫn vài kết quả không liên quan, cần quản trị viên rà lại trong Quản lý Khám phá. Địa chỉ theo địa giới mới của Goong (ví dụ "Vĩnh Long" cho một số nơi của Bến Tre).

**Google Maps:** chưa nhập vì cần khóa Google Places API của chủ website (chưa cung cấp).
