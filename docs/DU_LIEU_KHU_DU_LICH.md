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
