# Tìm bạn đồng hành gộp về Bảng tin (17/11/2026)

## Vì sao đổi

Trước đây website có ba luồng tìm bạn đồng hành chồng nhau, bên cạnh kết bạn, chat và mời vào kế hoạch:

- Trang Đồng hành (`/Companions`, `/dong-hanh/...`): đăng nhu cầu, gửi đề nghị kết nối, lập thỏa thuận nhiều bước.
- Nhóm Cùng đi (`/Together`): nhóm công khai có chat, lịch, bình chọn, việc cần làm, chia chi phí riêng, trùng với nhóm chat chuyến đi, lịch trình, Chuẩn bị và Chi tiêu.
- Bài "Tìm bạn đồng hành" trên Bảng tin.

Cơ sở dữ liệu thật chỉ có 1 nhu cầu, 1 đề nghị, 1 thỏa thuận và 1 nhóm. Ngoài ra nút Nhắn tin trên bài tìm bạn ở Bảng tin bị từ chối khi hai người chưa là bạn bè.

## Luồng còn lại

1. **Đăng bài** loại "Tìm bạn đồng hành" trên Bảng tin (điểm đến, ngày đi, ngày về).
2. **Tìm bài** ở tab tìm bạn của Bảng tin, mở thẳng bằng `/bang-tin?tab=dong-hanh`, lọc theo điểm đến hoặc nội dung.
3. **Nhắn tin** cho người đăng bài. Người đang có bài tìm bạn công khai còn hạn thì nhận được tin từ người chưa là bạn bè.
4. **Mời vào chuyến đi** ngay trong cuộc trò chuyện: nút "Mời vào chuyến đi" liệt kê các chuyến sắp tới của mình, gửi lời mời và tự nhắn một dòng báo cho người kia. Mời được bạn bè hoặc người đã có cuộc trò chuyện riêng.
5. **Chấp nhận** trong mục Chuyến đi. Sau đó hai người cùng sửa lịch trình và nhóm chat của chuyến tự tạo.

## Đã ẩn

- Mọi địa chỉ của trang Đồng hành và nhóm Cùng đi chuyển hướng về `/bang-tin?tab=dong-hanh` (quản trị viên về `/Admin`), kể cả các thao tác ghi.
- Đã gỡ lối vào ở trang chủ Khám phá, mục Chuyến đi, lối tắt trong chuyến đi, menu quản trị và chân trang.
- Bảng dữ liệu và mã cũ vẫn giữ nguyên. Mở lại bằng cấu hình `Features:LegacyCompanion=true` (`Services/LegacyCompanionFilter.cs`).

## Đối chiếu ca sử dụng

| Mã | Trước | Nay |
|---|---|---|
| SUC03.01 Tìm nhu cầu | Trang Đồng hành | Tab tìm bạn của Bảng tin |
| SUC03.02 Đăng nhu cầu | Biểu mẫu nhu cầu | Bài "Tìm bạn đồng hành" |
| SUC04.01 Kết nối | Đề nghị kết nối | Nhắn tin, kết bạn |
| SUC04.02 Thỏa thuận | Thỏa thuận nhiều bước | Mời và chấp nhận tham gia kế hoạch |

## Giới hạn

- Khách chưa đăng nhập không còn xem được danh sách tìm bạn; Bảng tin yêu cầu đăng nhập.
- Thẻ kết nối và thỏa thuận cũ đã nằm trong các cuộc trò chuyện vẫn hiển thị, nhưng không tạo mới được từ giao diện.
- Lời mời vào chuyến đi được báo bằng một tin nhắn chữ thường, chưa có nút Chấp nhận ngay trong chat.

Kiểm tra: `tests/companion-merge.cjs` (31 mục).
