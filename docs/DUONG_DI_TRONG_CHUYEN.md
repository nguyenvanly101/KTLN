# Điểm xuất phát, khoảng cách và đường đi (14/11/2026)

Mỗi ngày của chuyến đi có thể tính đường: điểm xuất phát → các điểm dừng có vị trí (theo giờ) → điểm kết thúc, kèm km và phút từng đoạn, vẽ trên bản đồ.

## Dữ liệu

- Bảng `DiemTuyenChuyenDi` (`Database/032_DiemTuyenChuyenDi.sql`, tự chạy khi khởi động): mỗi chuyến đi một dòng gồm tên và tọa độ điểm xuất phát, điểm kết thúc. Xóa theo chuyến đi (`ON DELETE CASCADE`). Tọa độ phải nằm trong khung Việt Nam (5–24 vĩ độ, 101,5–118,5 kinh độ).
- Điểm kết thúc để trống nghĩa là quay về điểm xuất phát.
- Chỉ **chủ chuyến đi** đổi được hai điểm này (giống tên, điểm đến, ngày, ngân sách).

## Chọn điểm trong lúc lập kế hoạch

Trang Sửa kế hoạch có ô **Điểm xuất phát** (gõ để chọn từ gợi ý Goong, hoặc bấm **Vị trí của tôi**) và ô **Điểm kết thúc** (mặc định "Kết thúc tại điểm xuất phát"). Gợi ý ưu tiên theo điểm đến đầu tiên của chuyến đi. Mã: `Views/PlanBoard/Editor.cshtml`, `wwwroot/js/plan-editor.js`.

## API

| Đường dẫn | Việc |
| --- | --- |
| `GET /api/lapkehoach/diem-goi-y?q=&near=` | Gợi ý địa chỉ, địa điểm (Goong Autocomplete) |
| `GET /api/lapkehoach/diem-chi-tiet?placeId=` | Tọa độ của một gợi ý đã chọn (Goong Place Detail), 422 nếu ngoài Việt Nam |
| `GET /api/lapkehoach/{id}/tuyen?day=&mode=` | Đường đi của một ngày; `mode` là `bike` (mặc định), `car`, `foot` |

`/tuyen` trả các điểm (`origin`, `item`, `end`), từng đoạn (km, phút, `estimated`), đường vẽ, tổng km và phút, số hạng mục chưa có vị trí. Người không có quyền xem chuyến đi nhận lỗi.

## Cách tính

- Mỗi đoạn gọi **Goong Direction** bằng khóa REST (`Discovery:Goong:RestKey`), `bike` là xe máy, `car` là ô tô. Kết quả lưu đệm 7 ngày theo tọa độ làm tròn 4 chữ số (khoảng 10 m) nên mở lại cùng một ngày không tốn thêm lượt gọi.
- **Đi bộ** luôn là ước tính theo đường chim bay (Goong không có chế độ đi bộ).
- Khi Goong hết hạn mức hoặc không trả lời, đoạn đó dùng **ước tính** (đường chim bay nhân 1,35, chia theo tốc độ trung bình) và được ghi rõ "ước tính", lưu đệm 10 phút.
- Một lần tính tối đa 14 lần gọi Goong; các đoạn còn lại ước tính.
- Chỉ tính khi khách bấm **Xem đường đi**, không tự tính khi mở trang.
- Hạng mục lấy vị trí từ mục Khám phá đã công bố của địa điểm (`MaDiaDiem`) hoặc dịch vụ (`MaDichVu`). Hạng mục không gắn địa điểm hay dịch vụ, hoặc chưa có tọa độ, bị bỏ qua và được báo số lượng.

## Nơi hiện

- **Trang chuyến đi** (`/Plans/Detail/{id}`): mỗi ngày có nút **Xem đường đi** và chọn phương tiện. Kết quả hiện tổng km, thời gian, bản đồ và km/phút giữa các hạng mục (`wwwroot/js/trip-route.js`).
- **Trang Hôm nay** (`/Journey/Today`): khung **Đường đi hôm nay** liệt kê A → các điểm dừng → B, đánh dấu điểm kế tiếp, mỗi đoạn có nút **Chỉ đường** mở Google Maps, và nút chỉ đường từ vị trí hiện tại đến điểm kế tiếp (`wwwroot/js/today-route.js`).

## Giới hạn

- Goong dùng chung hạn mức **1.000 yêu cầu mỗi ngày** với gợi ý địa điểm và nhập dữ liệu. Mỗi đoạn mới là một yêu cầu.
- Thời gian là ước tính theo giao thông thông thường, không phải thời gian thực.
- Chưa tự điền ô "thời gian di chuyển trước" của hạng mục từ kết quả này; ô đó vẫn do người dùng nhập.
- Khởi hành và kết thúc chỉ ghi lại giờ; chưa thay đổi trạng thái nháp hay đã xác nhận của kế hoạch và chưa gửi thông báo cho người đi cùng.

## Khởi hành, bỏ qua và kết thúc (15/11/2026)

Trên trang Hôm nay (`/Journey/Today`), chỉ **chủ chuyến đi** thao tác được.

- **Khởi hành:** khung "Sẵn sàng khởi hành?" hiện khi hôm nay nằm từ một ngày trước ngày đi đến hết ngày kết thúc. Khung nêu điểm xuất phát và điểm dừng đầu tiên. Bấm **Khởi hành** (`POST /Journey/Depart`) ghi giờ đi vào bảng `TienDoChuyenDi`; bấm lần hai không ghi đè.
- **Đang trên đường:** khung đổi thành "Đang trên đường" kèm giờ khởi hành và nút **Kết thúc chuyến** (có hỏi lại). Thẻ "Điểm dừng tiếp theo" có nút **Xong, đến điểm tiếp theo** và **Bỏ qua**.
- **Bỏ qua hạng mục:** cột mới `HoatDongLichTrinh.BoQuaLuc` (`Decision=Skip` của `POST /Journey/Complete`). Hạng mục bỏ qua không tính là hoàn thành, được đánh dấu "Đã bỏ qua", điểm tiếp theo chuyển sang hạng mục sau, tiến độ ghi "0 / 3 · 1 bỏ qua". Bấm **Mở lại** để hoàn tác; đánh dấu xong hoặc đổi giờ hạng mục sẽ xóa dấu bỏ qua.
- **Kết thúc:** `POST /Journey/Finish` ghi giờ kết thúc, chỉ khi đã khởi hành; sau đó trang hiện "Chuyến đi đã kết thúc" và không còn nút.
- Dữ liệu: `Database/033_KhoiHanh.sql` (tự chạy khi khởi động). Kiểm tra: `tests/trip-depart.cjs`.
