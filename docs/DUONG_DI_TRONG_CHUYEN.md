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

## Ô điểm xuất phát dễ dùng hơn và không phụ thuộc Goong (17/11/2026)

Nguyên nhân lỗi: ô điểm xuất phát chỉ lấy gợi ý từ Goong. Khi khóa Goong hết hạn mức trong ngày (Goong trả mã 429), website nhận danh sách rỗng và báo "Chưa tìm thấy" với mọi từ khóa, nên không đặt được điểm xuất phát. Ngoài ra gõ tên mà không chọn dòng gợi ý thì khi lưu điểm xuất phát bị bỏ mất mà không báo.

Đã sửa:

- **Gợi ý từ hai nguồn.** Địa điểm và dịch vụ đã có tọa độ trên website hiện trước (không tốn lượt Goong), sau đó là địa chỉ từ Goong. Không còn ghép tên điểm đến vào từ khóa, vì nơi xuất phát thường là nhà ở tỉnh khác.
- **Báo đúng tình trạng.** Khi Goong không trả lời, ô gợi ý ghi rõ dịch vụ tìm địa chỉ đang quá tải và chỉ sang hai cách còn lại, thay vì báo "Chưa tìm thấy".
- **Chọn trên bản đồ.** Nút mới cho cả điểm xuất phát và điểm kết thúc: bấm vào bản đồ để đặt điểm, bấm chỗ khác để đổi, rồi **Dùng điểm này**. Cách này không cần Goong; nếu tra được tên thì điền tên, không thì ghi "Điểm đã chọn trên bản đồ".
- **Vị trí của tôi** tự điền địa chỉ thật khi tra được, và báo rõ khi trình duyệt chặn định vị.
- **Dòng trạng thái** dưới ô: "Đã có vị trí" kèm nút Xóa, hoặc "Chưa có vị trí" khi mới gõ tên.
- **Chặn lưu thiếu vị trí.** Gõ tên mà chưa có vị trí thì bấm Lưu sẽ báo và đưa con trỏ về ô đó. Ô để trống vẫn lưu được.
- **Phím Enter** chọn dòng gợi ý đầu tiên.
- **Đặt ngay trước khi khởi hành.** Khung "Sẵn sàng khởi hành?" ở trang Hôm nay có nút **Xuất phát từ vị trí hiện tại của tôi** (`POST /api/lapkehoach/{id}/xuat-phat`, chỉ chủ chuyến, giữ nguyên điểm kết thúc) và lối sang **Chọn nơi khác**.

Giới hạn: khi Goong hết hạn mức, đường đi vẫn tính được nhưng là ước tính theo đường chim bay nhân hệ số, và gợi ý chỉ gồm nơi đã có trên website. Công cụ nhập dữ liệu Goong dùng chung khóa và hạn mức với website, nên chạy nhập hàng loạt sẽ làm tính năng này tạm yếu đi trong ngày đó.

Kiểm tra: `tests/trip-route.cjs` (36 mục).

## Tìm dịch vụ dọc tuyến (trang Khám phá) dùng Goong

Từ 18/11/2026, tìm dịch vụ dọc tuyến không còn gọi OSRM/OpenStreetMap. `DiscoveryRouter` gọi Goong Direction một lần cho cả tuyến nhiều điểm (điểm đến cách nhau dấu `;`), giữ đúng thứ tự. Đi bộ dùng hình tuyến xe máy, thời gian tính theo 4,5 km/giờ. Khi Goong không trả được, tuyến là đường nối thẳng các điểm, ghi rõ là ước lượng, vẫn tìm chỗ ghé được.

Cách dùng mới: ô "Đi từ" (A) và "Đến" (B) gõ tên để lấy gợi ý Goong (`/api/discovery/diem`, `/api/discovery/diem-chi-tiet`), hoặc bấm 📍 rồi bấm lên bản đồ (tên lấy từ `/api/discovery/ten-vi-tri`). "+ Thêm điểm ghé" chèn điểm giữa, phương tiện chọn bằng nút (Ô tô, Xe máy, Đi bộ), khoảng cách và thời gian đi thêm nằm trong "Tùy chọn tìm chỗ ghé". Kiểm thử: `tests/route-goong.cjs`.
