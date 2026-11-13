# Lập kế hoạch chuyến đi một trang (13/11/2026)

Thay luồng lập kế hoạch nhiều bước bằng **một trang tạo/sửa** và **một trang xem chỉ đọc**. Đây là mô tả hành vi, API, dữ liệu và quy tắc để bảo trì.

## Các trang

| Đường dẫn | Vai trò | Nội dung |
|---|---|---|
| `/Plans` | Khách | Danh sách chuyến đi. Mỗi thẻ có **Xem chi tiết**, **Sửa**, **Xóa** (chuyến đi chung chỉ có Xem chi tiết và Sửa). |
| `/Plans/Detail/{id}` | Khách / thành viên | Chỉ đọc. Hàng nút trên cùng: Sửa, Xác nhận lịch trình, Chia sẻ, Mời bạn, Chia tiền, Nhóm chat. Bên dưới là một lớp cuộn gồm lịch trình từng ngày (kèm ghi chú ngày) và danh sách thành viên (trưởng nhóm + thành viên). Dịch vụ đã đặt và hỗ trợ nằm trong mục thu gọn. |
| `/lapkehoach` | Khách | Tạo mới. |
| `/lapkehoach/{id}` | Chủ chuyến đi + thành viên đã tham gia | Sửa. Thành viên sửa được lịch trình; tên, điểm đến, ngày và ngân sách chỉ trưởng nhóm đổi. |
| `/lap-ke-hoach`, `/ke-hoach/{id}/lap` | | Chuyển vĩnh viễn (301) sang hai trang trên. |

## Trang tạo/sửa

- **Thông tin chung:** tên (ô đầu tiên, bắt buộc), điểm đến (nhiều điểm, tối đa 5, ở mọi cấp: tỉnh, quận/huyện, phường/xã, khu du lịch; gõ để gợi ý, hộp "Chọn chi tiết", hoặc chọn trên bản đồ), ngày đi, số ngày (1–30), ngân sách (nút chọn nhanh hoặc nhập số).
- **Lịch theo ngày:** các ngày xếp nối nhau. Mỗi ngày có **+ Thêm dịch vụ**, **+ Thêm hoạt động** (tự nhập), **+ Ghi chú**, và mục "Khác" (chép sang ngày khác, xóa hết ngày). Mỗi hạng mục có Sửa, Xóa (có hoàn tác).
- **Thêm dịch vụ:** dùng chính bộ máy tìm kiếm của Khám phá (từ khóa/nhu cầu, 9 nhóm, giá từ–đến, sắp xếp, chỉ trong vùng bản đồ, chỉ mục đã lưu, phân trang). Mục **đã lưu** xếp trước. Chỉ chọn được **dịch vụ thật** (có `ServiceId`) hoặc **địa điểm** (có `PlaceId`); mục điểm đến, khu vực và "thông tin tham khảo" không gắn dịch vụ bị ẩn. Lưu trú tính giá theo số đêm còn lại. Mỗi thẻ có nút **Chi tiết** (giới thiệu, giờ, bảng giá, đã gồm/chưa gồm, nguồn).
- **Thanh dưới:** tạm tính so với ngân sách, cảnh báo cập nhật ngay (vượt ngân sách, ngày trống, trùng giờ, đêm chưa có chỗ ở). Form trống không báo cảnh báo.
- **Lưu:** một lần cho cả thông tin và lịch trình; chưa bấm Lưu thì không ghi gì. Rời trang khi có thay đổi sẽ hỏi lại.

## API (JSON, cần mã chống giả mạo trong tiêu đề `RequestVerificationToken`)

| Phương thức, đường dẫn | Chức năng |
|---|---|
| `POST /api/lapkehoach` | Lưu mới hoặc cập nhật (`id` = 0 là tạo). Đồng bộ hạng mục (thêm/sửa/xóa) và ghi chú ngày. |
| `GET /api/lapkehoach/goi-y` | Tìm dịch vụ/địa điểm (bộ lọc `DiscoveryFilter`, `daLuu=true` chỉ lấy mục đã lưu). |
| `GET /api/lapkehoach/chi-tiet/{entryId}` | Thông tin đầy đủ một mục Khám phá. |
| `GET /api/lapkehoach/diem-den?q=` | Gợi ý điểm đến (đơn vị hành chính + khu du lịch/địa điểm). |
| `GET /api/lapkehoach/ban-do` | Các ghim điểm đến trên bản đồ. |
| `POST /Plans/Delete` | Xóa chuyến đi (form). |

Bốn API `GET` có giới hạn tần suất chính sách `discovery` (90 yêu cầu/phút/IP).

## Xóa chuyến đi

Chỉ chủ chuyến đi. Bị từ chối (409) nếu đã có yêu cầu đặt dịch vụ, nhóm đồng hành gắn với chuyến, hoặc tình huống phát sinh. Khi xóa, các dữ liệu đi kèm cũng bị xóa: hạng mục, ghi chú ngày, mục chuẩn bị, chi phí, chia tiền nhóm, liên kết chia sẻ, thành viên mời và liên kết nhóm chat (cuộc trò chuyện vẫn giữ).

## Nhóm chat tự động

Khi chuyến đi có từ 2 người (chủ chuyến + thành viên đã chấp nhận), hệ thống tạo nhóm chat tên "Chuyến đi: {tên}", trưởng nhóm là Admin của nhóm. Thành viên mới vào thì được thêm; rời chuyến đi thì rời nhóm; đổi tên chuyến thì đổi tên nhóm. Mã nằm ở `Services/ChatTripGroup.cs` (`SyncTripGroup`), được gọi khi chấp nhận lời mời, rời chuyến và lưu kế hoạch.

## Dữ liệu và script

- `Database/029_NhomChatChuyenDi.sql`: bảng `NhomChatChuyenDi` (liên kết chuyến đi ↔ cuộc trò chuyện).
- `Database/030_GhiChuNgay.sql`: bảng `GhiChuNgayChuyenDi` (ghi chú từng ngày).
- `Database/Quyen_CSDL.sql`: cấp quyền xóa cho `KeHoachChuyenDi`, `ChiaSeChuyenDi`, `NhomChatChuyenDi`, `GhiChuNgayChuyenDi` (quyền xóa cho tài khoản ứng dụng hạn chế **chưa kiểm thử** với tài khoản đó).

## Mã nguồn chính

`Services/PlanBoardService.cs`, `Controllers/PlanBoardController.cs`, `Views/PlanBoard/Editor.cshtml`, `wwwroot/js/plan-editor.js`, `Views/Plans/Detail.cshtml`. Bộ kiểm thử: `tests/flow-v10.cjs`.

## Giới hạn đã biết

- Ghim trên bản đồ chỉ có khi dịch vụ có tọa độ (từ cơ sở nhà cung cấp hoặc mục Khám phá). Nhà cung cấp chưa bị buộc ghim vị trí cơ sở.
- Hạng mục tự nhập không gắn dịch vụ nên không đặt được.
- Gợi ý điểm đến có thể trùng tên ở nhiều tỉnh; mỗi dòng ghi rõ cấp và đường dẫn hành chính để phân biệt.
