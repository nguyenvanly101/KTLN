# Nối luồng chuyến đi, giao diện điện thoại và nhận địa điểm (16/11/2026)

## 1. Nối các phần của chuyến đi

- **Tự điền thời gian di chuyển.** Sau khi bấm **Xem đường đi** ở một ngày trên trang chuyến đi, có nút **Điền thời gian di chuyển vào lịch** (`POST /api/lapkehoach/{id}/tuyen/ap-dung?day=&mode=`). Website lấy số phút của từng đoạn và ghi vào ô "thời gian di chuyển trước" của hạng mục đến. Chỉ điền hạng mục **chưa nhập**; hạng mục đã nhập tay được giữ nguyên và được báo số lượng. Chủ chuyến đi và thành viên đã chấp nhận đều dùng được.
- **Báo nhóm khi khởi hành và kết thúc.** Khi chủ chuyến bấm Khởi hành hoặc Kết thúc chuyến, nếu chuyến đi có nhóm chat thì nhóm nhận một tin nhắn từ chủ chuyến. Gửi tin lỗi không làm hỏng thao tác chính.
- **Sau khi kết thúc.** Khung "Chuyến đi đã kết thúc" có lối vào Xem chi tiêu, Chia sẻ chuyến đi và (khi chuyến có yêu cầu dịch vụ) Đánh giá dịch vụ đã dùng.
- **Thành viên chuyến đi chung.** Thành viên đã chấp nhận nay đánh dấu **Xong**, **Bỏ qua**, **Mở lại** hạng mục và **ghi sự cố** được như chủ chuyến. Khởi hành và Kết thúc vẫn chỉ dành cho chủ chuyến. Người ngoài chuyến đi vẫn bị từ chối.

Kiểm tra: `tests/trip-flow.cjs`.

## 2. Giao diện điện thoại

- Trang Hôm nay ở 375 px: không tràn ngang, các nút cao 48 px, bản đồ đường đi vừa khung.
- Trang Khám phá ở điện thoại: danh sách kết quả cuộn trong khung cao tối đa 78% màn hình (trước đây trang dài hơn 11.000 px, nay khoảng 3.150 px). Bấm ghim hoặc chọn mốc quanh đây thì trang tự kéo danh sách vào tầm nhìn.

## 3. Nhà cung cấp nhận địa điểm có sẵn

- Trang chi tiết của một **địa điểm** trong Khám phá (không phải dịch vụ hay cơ sở đã có chủ) có khung **Bạn quản lý nơi này?**. Nhà cung cấp thấy nút **Nhận địa điểm này**; khách chưa đăng nhập được mời đăng ký nhà cung cấp; khách du lịch chỉ thấy lời giải thích.
- Nút mở `/nha-cung-cap/co-so/them?tu={mã mục Khám phá}`: biểu mẫu Thêm cơ sở được **điền sẵn** tên, địa chỉ, khu vực, tọa độ, loại gợi ý theo nhãn (lưu trú, ăn uống, hoạt động), và phần căn cứ ghi rõ "Nhận từ địa điểm đã có trong Khám phá (mục #…)" để quản trị viên đối chiếu.
- Nhà cung cấp vẫn phải xác nhận vị trí, bổ sung giờ mở cửa, giá, ảnh, căn cứ quản lý và **gửi xét duyệt như bình thường**. Mở biểu mẫu không thay đổi địa điểm gốc.

- **Tự ẩn địa điểm gốc.** Khi lưu cơ sở mới từ biểu mẫu nhận địa điểm, website ghi lại mục Khám phá gốc (bảng `NhanDiaDiemKhamPha`, `Database/034_NhanDiaDiem.sql`). Khi quản trị viên duyệt cơ sở đó và vị trí khớp trong 500 m, địa điểm gốc được ẩn (`DiaDiemDuLich.DangHienThi = 0`) để Khám phá không hiện trùng; việc ẩn được ghi vào nhật ký. Quản trị viên có thể cho hiện lại trong phần quản lý địa điểm.

Kiểm tra: `tests/claim-place.cjs`.

## Giới hạn

- Địa điểm gốc chỉ tự ẩn khi vị trí cơ sở được duyệt cách địa điểm gốc không quá 500 m. Nếu lệch xa hơn, cả hai cùng hiện và quản trị viên cần xem lại bằng tay.
- Tin báo nhóm chỉ gửi khi chuyến đi đã có nhóm chat (chuyến có thành viên).
- Giờ mở cửa, giá, ảnh của các địa điểm nhập từ Goong vẫn trống cho đến khi có nhà cung cấp nhận hoặc có khóa Google Places.
- Ở điện thoại, khung "Đường đi hôm nay" nằm dưới danh sách việc trong ngày, phải cuộn xuống mới thấy.
