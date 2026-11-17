# Đặt dịch vụ gọn

## Vì sao đổi

Trước khi đổi, luồng đặt dịch vụ gồm:

- 13 trang và 10 trạng thái;
- quy trình đề nghị đổi/hủy phải chờ cơ sở duyệt, cơ sở còn đề xuất lại;
- ghi kết quả cho từng hạng mục;
- phản ánh kết quả trong 30 ngày.

Dữ liệu thật mới có 4 yêu cầu, và chưa lần nào dùng tới đề nghị đổi/hủy hay kết quả hạng mục.

## Luồng mới

1. **Đặt.** Bấm "Đặt dịch vụ" trên trang dịch vụ để mở bảng nổi gồm ngày, số người, ghi chú. Giá tạm tính hiện ngay theo giá tham khảo. Gửi xong, khách vào thẳng cuộc trò chuyện với cơ sở; thẻ yêu cầu nằm trong khung chat.
   - Dịch vụ chưa gắn cơ sở vẫn dùng biểu mẫu cũ.
   - `/Bookings/Create` chuyển sang bảng nổi.
2. **Một trang cho mỗi yêu cầu** (`/Bookings/Detail/{id}`), gồm:
   - thanh tiến trình: Đã gửi → Cơ sở phản hồi → Đã xác nhận → Đã dùng;
   - điều kiện, phần cơ sở đã đổi (gạch cũ, in đậm mới);
   - các nút hiện đúng lúc.
3. **Danh sách chung** (`/Bookings`), lọc theo việc: Cần trả lời, Sắp tới, Đã dùng, Đã hủy.
   - Nhà cung cấp thấy yêu cầu của mọi cơ sở, có nút Xác nhận ngay trên thẻ.
   - `/nha-cung-cap/yeu-cau` chuyển về đây.

## Ai làm gì, khi nào

| Trạng thái người dùng thấy | Khách | Cơ sở |
| --- | --- | --- |
| Chờ phản hồi | Sửa, Rút | Xác nhận, Đề xuất khác, Từ chối (kèm lý do) |
| Cơ sở đề xuất | Đồng ý, Không đồng ý | Chờ khách |
| Đã xác nhận, trước ngày dùng | Đổi ngày/số người (quay lại Chờ phản hồi), Hủy kèm lý do | Hủy kèm lý do |
| Đã xác nhận, từ ngày dùng | Chờ cơ sở ghi nhận | Đã phục vụ / Khách không đến |
| Đã dùng | Đánh giá | — |

- Hủy có hiệu lực ngay, không cần bên kia duyệt; bên kia nhận thông báo, cuộc trò chuyện có dòng sự kiện.
- Yêu cầu quá ngày dùng mà chưa chốt sẽ tự hủy, lý do "Quá ngày sử dụng".
- Giữ nguyên: đặt cọc (ghi nhận, hai bên tự thanh toán), nhắc lịch, mời đánh giá, gắn vào chuyến đi.

## Trạng thái

Cơ sở dữ liệu giữ nguyên 10 giá trị. Giao diện gộp lại theo `Services/BookingUi.cs`:

| Hiển thị | Giá trị lưu |
| --- | --- |
| Chờ phản hồi | ChoPhanHoi (ChoKiemTra cũ) |
| Cơ sở đề xuất | DeXuatKhac |
| Đã xác nhận | DaXacNhan |
| Đã dùng | HoanThanh (ThucHienMotPhan cũ) |
| Khách không đến | KhongThucHien |
| Đã hủy | DaHuy, TuChoi, KhongTiepTuc |

## Trang cũ

Các trang sau chuyển về trang yêu cầu hoặc danh sách (`Services/LegacyBookingFilter.cs`):

- `/Bookings/Adjustments`, `/Bookings/Changes/{id}`, `/Bookings/Change/{id}`;
- `/Bookings/Results`, `/Bookings/Outcome/{id}`;
- `/yeu-cau/{id}/dieu-chinh/...`, `/yeu-cau/{id}/ket-qua/...`.

Bảng `DeNghiDieuChinhDichVu` và `KetQuaCungCapDichVu` vẫn giữ để đọc dữ liệu cũ. Đặt `Features:LegacyBooking=true` để mở lại các trang này (kiểm thử cũ).

## Báo cáo khóa luận

Cần viết lại các use case sau cho khớp với luồng mới:

- **Đổi/hủy dịch vụ:** hủy trực tiếp kèm lý do; đổi thì yêu cầu quay lại chờ xác nhận.
- **Ghi nhận kết quả phục vụ:** cơ sở bấm "Đã phục vụ" hoặc "Khách không đến".
- **Phản ánh kết quả:** thay bằng nhắn tin trong cuộc trò chuyện và đánh giá dịch vụ.

## Kiểm thử

- Bộ mới: `tests/booking-simple.cjs`, 27 kiểm tra.
- Các bộ cũ vẫn qua sau khi cập nhật: `chat-booking` (35), `booking-m3` (28), `booking-gaps` (24).
