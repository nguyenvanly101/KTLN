# Hành Trình — Open Atlas

Áp dụng cho bản thiết kế lại ngày 04/11/2026. Hướng thiết kế: một bản đồ du lịch hiện đại, giàu hình ảnh, thao tác rõ ràng; kết hợp trang khám phá có cảm hứng với không gian quản lý chuyến đi có cấu trúc.

## Cơ sở từ ui-ux-pro-max

Đã chạy `search.py "travel booking immersive" --design-system`: chọn cấu trúc tiến dần từ khám phá đến hành động, màu xanh cho sản phẩm du lịch, lưới rõ ràng, chuyển trạng thái nhẹ. Áp dụng các hướng dẫn `navigation hierarchy disclosure`: phân tầng điều hướng, breadcrumb cho màn hình sâu, không để thanh cố định che nội dung. Truy vấn thử phong cách `travel discovery bold editorial` trả về Brutalism dành cho portfolio; không sử dụng gợi ý này cho sản phẩm du lịch.

Phần tùy chỉnh theo sản phẩm: xanh chủ đạo, xanh đậm cho các khối nhấn, vàng xanh cho dấu hành trình; ảnh dạng bưu thiếp và những đường nối gợi tuyến đi. Không có carousel tự chạy, số liệu hay đánh giá giả.

## Token dùng chung

| Vai trò | Giá trị |
|---|---|
| Chữ chính | `#15213e` |
| Chữ phụ | `#5c667b` |
| Hành động chính | `#2447e8` |
| Hover hành động | `#1934b4` |
| Nền không gian cá nhân | `#f5f7fc` |
| Bề mặt | `#ffffff` |
| Nền trạng thái chọn | `#edf1ff` |
| Khối nhấn đậm | `#142447` |
| Điểm nhấn trên nền đậm | `#def69b`, chữ `#142447` |
| Viền ô nhập | `#8390a6` |

Chữ: Segoe UI / Arial có hỗ trợ tiếng Việt, không cần tải font ngoài. Tiêu đề trang 32–34px, tiêu đề phần 24–34px, nội dung 14–16px; chữ phụ 12–13px. Hero là ngoại lệ hiển thị lớn, thích ứng theo màn hình. Dùng icon SVG nét thống nhất từ bộ hiện có.

## Cấu trúc điều hướng

- Khám phá: điểm đến; lưu trú; ăn uống; di chuyển; hoạt động.
- Chuyến đi: lịch trình; đặt dịch vụ; hỗ trợ chuyến đi.
- Đặt dịch vụ có các tab yêu cầu đặt; thay đổi/hủy; kết quả phục vụ; đánh giá của khách. Chi tiết từng yêu cầu giữ các tab theo đúng đối tượng.
- Đồng hành: tìm người; bài đăng cá nhân; kết nối. Thỏa thuận nằm trong từng kết nối.
- Nhà cung cấp: nhóm dịch vụ & đặt chỗ (dịch vụ, yêu cầu đặt); hồ sơ đối tác; tài khoản.
- Quản trị: tổng quan; duyệt & xác minh; quyền nhà cung cấp; nhật ký; tài khoản.

Thanh trên ở trang công khai có ba menu nhóm. Trong không gian quản lý, thanh bên thể hiện nhóm chức năng theo vai trò; thanh trên dẫn đến hai khu vực công khai. Mục đang dùng được mở sẵn và đánh dấu. Không nhân bản từng bước xử lý thành nhiều mục ngang hàng.

## Tương tác và responsive

Menu nhóm dùng `details/summary`, mở bằng nhấp hoặc bàn phím; một menu trên cùng mở tại một thời điểm, đóng bằng Escape hoặc nhấp ngoài. Menu điện thoại có nền phủ, giữ focus trong điều hướng, đưa focus về nút mở khi đóng và khóa nội dung phía sau. Breakpoint của JavaScript và CSS cùng là 1000px.

Điều khiển chính cao tối thiểu 44px; ô nhập văn bản trên điện thoại dùng 16px. Không giấu chức năng qua hover. Có focus ring, skip link, nhãn hiển thị, trạng thái trống kèm thao tác tiếp theo, và hỗ trợ `prefers-reduced-motion`.

Trang chuyến đi lọc tên/điểm đến không dấu và trạng thái trên những bản ghi đã hiển thị; không có JavaScript thì vẫn xem được tất cả. Kết quả lọc được thông báo bằng live region. Tìm kiếm điểm đến và đồng hành vẫn gửi qua API/route nghiệp vụ đang có. Bộ lọc phụ đã dùng tự mở khi xem kết quả.

## Phạm vi

Chỉ thay đổi view, CSS, JavaScript giao diện và tài liệu. Giữ hợp đồng database, API, service, quyền truy cập và các bước nghiệp vụ FullText. Tất cả hình du lịch là ảnh minh họa, tái sử dụng tài nguyên có sẵn và ghi công tại `wwwroot/images/CREDITS.md`.

Phạm vi đoạn trên mô tả đợt làm giao diện ngày 04/11. Bản cập nhật luồng ngày 05/11 thêm trung tâm chuyến đi và quan hệ tùy chọn với yêu cầu đặt theo đề xuất đã được người dùng chấp thuận; chi tiết ở `docs/LUONG_CHUYEN_DI_2026-11-05.md`. Tiếp tục dùng các token trên, bổ sung `trip-workspace.css`. Trong một quy trình, giữ lựa chọn và điền lại thông tin đã có: kết quả `redundant entry forms --domain ux` của ui-ux-pro-max khớp mục Forms / Redundant Entry. Tổng quan ưu tiên việc cần xử lý theo dữ liệu; thao tác đặt dịch vụ và kết nối vẫn là tùy chọn.

## Mở rộng sản phẩm 05/11/2026

Tiếp tục dùng Open Atlas cho lưu/so sánh, checklist, chi tiêu và nội dung dịch vụ. `product-features.css` chỉ bổ sung bố cục/thành phần, tái dùng token hiện có. So sánh ở desktop đặt các lựa chọn cạnh nhau, điện thoại xếp dọc. Chuẩn bị/thực chi dùng lưới danh sách + biểu mẫu, chuyển một cột ở màn hình nhỏ và có liên kết nhanh tới biểu mẫu. Truy vấn ui-ux-pro-max `error summary validation --domain ux` được áp dụng: giữ dữ liệu đã nhập, tổng hợp lỗi có focus và liên kết tới ô cần sửa, thông báo tiếng Việt. Thông tin cơ bản của dịch vụ đứng trước nhóm thông tin bổ sung. 108 kiểm tra trình duyệt cùng ảnh ở `preview/product-*.png`; hợp đồng mới được ghi riêng tại `docs/MO_RONG_SAN_PHAM_2026-11-05.md`.

## Hành trình, chia sẻ, đánh giá và lịch phục vụ — 004

Áp dụng truy vấn ui-ux-pro-max về mobile itinerary/progressive disclosure: đưa điểm dừng tiếp theo và thao tác trong ngày lên trước; các việc chuẩn bị/chi tiêu/chia sẻ giữ trong ngữ cảnh chuyến đi. `journey-features.css` tiếp tục dùng token Open Atlas. Trạng thái có chữ; khối xanh đậm dùng tiêu đề trắng, trạng thái hoàn thành dùng chữ xanh đậm trên nền xanh nhạt. Giữ thứ tự người dùng đã xếp, không tự đổi thứ tự hoạt động theo giờ khi xem Hôm nay.

Chia sẻ là thao tác có lựa chọn và xác nhận, nêu rõ snapshot gồm nội dung hoạt động; ngân sách/dịch vụ mặc định bỏ chọn. URL có copy và fallback; token của người dùng không ghi vào nhật ký nghiệp vụ hoặc báo cáo bàn giao. Fixture của test chỉ trỏ tới database thử. In A4 có bố cục riêng. Lịch ngày có chú giải và ba trạng thái bằng chữ; số lượng ghi rõ tham khảo. Form dài có lối tắt trên điện thoại, lưu giữ ngày đang sửa. Biểu mẫu kiểm duyệt liên kết lỗi tới đúng dòng và đưa lại nội dung đó vào màn hình khi lỗi. Các trang mới dùng một h1 và giữ điều khiển có nhãn.

Bằng chứng: 174 assertion trình duyệt, 75 tổ hợp/15 màn hình, ảnh `preview/journey-*.png`. API, dữ liệu và phạm vi nghiệp vụ được ghi ở `docs/HANH_TRINH_MO_RONG_2026-11-05.md`; các đoạn phạm vi của đợt cũ phía trên là tư liệu lịch sử.

## Không gian đồng hành — 005

Giữ Open Atlas: xanh biển cho thao tác, navy cho hero/số dư, lime cho điểm nhấn; ảnh bưu thiếp ở khám phá và thẻ lời rủ. Chỉ thêm mục con trong Đồng hành và Duyệt & xác minh; sáu tác vụ của nhóm dùng tab cùng ngữ cảnh, không thêm menu chính. Nhãn trạng thái luôn có chữ, điểm hẹn riêng chỉ xuất hiện cho thành viên đã tham gia.

Áp dụng hướng dẫn ui-ux-pro-max về progressive disclosure và Focusable Error Summary: chia đều không hiện các ô tiền riêng; chia tùy chọn chỉ bắt buộc phần của người được chọn. Lỗi đưa focus vào summary, liên kết đến ô nhập và có mô tả cạnh ô; giữ nội dung khi máy chủ từ chối. Thông báo số tin mới không tự thay nội dung đang soạn hoặc nhảy vị trí đọc. Thông tin chia sẻ và các giới hạn tài chính ghi bằng ngôn ngữ người dùng; không giả lập thanh toán trực tuyến.

Kiểm chứng: 23 thao tác trình duyệt, 75 tổ hợp của 15 màn hình ở 375/768/1024/1280px và xoay ngang 812×375; tổng 324 assertion, không có lỗi console. Đã xem ảnh desktop và mobile, viewport trả mặc định. `companion-features.css`/`.js` và `Views/Together` chứa triển khai; `docs/KHONG_GIAN_DONG_HANH_2026-11-05.md` ghi thiết kế/API/hàm/data/giới hạn. Ảnh `preview/companion-*.png` là fixture riêng, không đưa vào database chính.
