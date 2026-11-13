# Hỗ trợ khi đang đi (13/11/2026)

Nằm ngay trên trang chi tiết chuyến đi (`/Plans/Detail/{id}`), không có trang riêng. Mã: `Views/Plans/_TripHelp.cshtml`, `wwwroot/js/trip-help.js`, kiểm tra `tests/trip-help.cjs`.

## Thành phần

- **Dải "Đang đi":** phía trên lịch trình. Hôm nay nằm trong khoảng ngày chuyến đi thì ghi "Bạn đang trong chuyến đi · Ngày N", ngược lại ghi "Khi đang trên đường". Nút **Gặp sự cố, cần giúp** mở khung hỗ trợ.
- **Khung hỗ trợ** (hộp thoại, không rời trang). Trên cùng luôn có 115, 113, 114, 112. Hai thẻ:
  - **Chuyện đang xảy ra** (19 tình huống, 4 nhóm): sức khỏe (ốm, chấn thương và tai nạn, ngộ độc, hết thuốc, bị chích cắn, nguy hiểm khi tắm biển), giấy tờ, tiền, đồ đạc (mất ví, giấy tờ, điện thoại, hành lý, hết tiền, vượt ngân sách), an toàn (lừa đảo, quấy rối, bị lạc, thiên tai), nhóm đi cùng (lạc, ốm hoặc về sớm, bất đồng).
  - **Tìm nơi giúp gần tôi** (22 loại: bệnh viện, nhà thuốc, công an, ATM, sửa xe, trạm xăng, bến xe, sân bay, ga tàu, chỗ ở khác, quán ăn...).
- **Nút "Có vấn đề"** ở từng hạng mục lịch trình. Lý do theo nhóm dịch vụ của hạng mục (chỗ ở, di chuyển, ăn uống, tham quan, trải nghiệm), nếu hạng mục không gắn dịch vụ thì đoán nhóm theo tên. Thêm 3 lý do chung (thời tiết xấu, mệt muốn đổi lịch, khác). Hạng mục gắn dịch vụ đã đặt có thêm: xem yêu cầu, đề nghị đổi hoặc hủy, nhắn nhà cung cấp.
- Mỗi tình huống hoặc lý do hiện: việc nên làm ngay (theo thứ tự), nút gọi, nơi nên tìm, rồi các nút làm tiếp (đổi lịch trong kế hoạch, báo nhóm chat chuyến đi, ghi chi phí phát sinh) và **Ghi lại vào chuyến đi**.

## Ghi sự cố

`POST /api/lapkehoach/su-co` (JSON, kèm mã chống giả mạo) ghi vào bảng `TinhHuongPhatSinh` như màn hình Hỗ trợ trong chuyến đi cũ (`/Support`), gắn hạng mục nếu có. Chỉ **chủ chuyến đi** ghi được. Loại sự cố mở rộng từ 4 lên 11: thêm Sự cố di chuyển, Chỗ ở có vấn đề, Sức khỏe tai nạn, Mất giấy tờ tiền đồ đạc, An toàn cá nhân, Nhóm đi cùng, Hết tiền vượt ngân sách. Danh sách sự cố hiện tên loại (trước đây hiện mã).

## Tìm nơi giúp theo vị trí

- Có vị trí (khách cho phép): `GET /api/lapkehoach/gan-day?kind=&lat=&lng=` dùng Goong Autocomplete có `location` và `radius=10`, rồi lấy tọa độ bằng Place Detail, xếp theo khoảng cách, tối đa 10 km, lưu đệm 15 phút. Chỉ nhận tọa độ trong khung Việt Nam và loại dịch vụ hợp lệ.
- Chưa có vị trí: tìm quanh điểm đến của chuyến đi qua `goong-goi-y`.
- Luôn có nút mở Google Maps tìm đúng từ khóa (không tốn hạn mức Goong).

## Giới hạn

- Nội dung các bước là hướng dẫn chung do người soạn viết, **cần đối chiếu** (số điện thoại, thủ tục, quy trình y tế và pháp lý) với cơ quan có thẩm quyền trước khi dùng thật. Khung ghi rõ "Hướng dẫn chung, không thay thế ý kiến của cơ quan chức năng hoặc bác sĩ".
- Website không biết nơi nào đang mở cửa hay xe có trễ thật không (không có dữ liệu thời gian thực).
- Danh sách gợi ý từ Goong dùng chung hạn mức 1.000 yêu cầu mỗi ngày; hết hạn mức thì khung báo dùng nút bản đồ.
- Thành viên chuyến đi chung (không phải chủ) chưa ghi được sự cố.
- Chưa có thông báo tự động cho nhà cung cấp hoặc nhóm; khung chỉ dẫn tới trang nhắn tin.
