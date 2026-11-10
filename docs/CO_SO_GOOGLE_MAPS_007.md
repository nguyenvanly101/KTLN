# Cơ sở dịch vụ, mốc cụ thể và Google Maps — 007

Thiết kế được người dùng chấp thuận ngày 08/11/2026: nhà cung cấp có nhiều cơ sở/chi nhánh, dịch vụ áp dụng tại cơ sở được chọn, từng cơ sở có vị trí Google Maps. Điểm tham quan và cửa vào là mốc cụ thể để tìm quanh. Không tự suy đoán địa chỉ, tọa độ hay quyền quản lý từ dữ liệu cũ.

## Các luồng đã lập trình

- Khách: chọn mốc cụ thể bằng gợi ý, vị trí hiện tại hoặc ghim → chọn loại cần tìm → lọc phạm vi/tiện ích → xem danh sách và Google Maps → xem đúng cơ sở → xem dịch vụ tại cơ sở → so sánh hoặc xem đường đến.
- Nhà cung cấp: tạo riêng từng cơ sở → khai địa chỉ/vị trí, tiện ích và căn cứ quản lý → gửi duyệt → chọn cơ sở đã duyệt khi đăng dịch vụ. Chỉ chọn cơ sở thuộc mình. Một dịch vụ có thể áp dụng tại nhiều cơ sở khi giá và điều kiện giống nhau; nếu khác, đăng dịch vụ riêng.
- Quản trị: đối chiếu hồ sơ cơ sở và quyền quản lý → duyệt/từ chối/yêu cầu bổ sung. Bản đang công bố được giữ trong khi chờ duyệt cập nhật. Khi duyệt dịch vụ, kiểm tra lại cơ sở áp dụng rồi công bố các liên kết cùng phiên bản dịch vụ.
- Yêu cầu dịch vụ: khách chọn cơ sở trong biểu mẫu → kiểm tra lại, kèm phiên bản cơ sở → gửi yêu cầu. Tên và địa chỉ cơ sở được chụp vào yêu cầu, giữ lịch sử sau khi cơ sở cập nhật. Gửi lại cùng SubmissionId không tạo yêu cầu/cơ sở trùng.
- Mốc: quản trị tạo điểm cụ thể; mỗi cửa vào có ghim riêng và có thể thuộc một địa điểm chính. Tìm quanh cửa sử dụng tọa độ của cửa đó. Ẩn địa điểm chính sẽ ẩn các cửa liên kết khỏi tra cứu công khai.

## Dữ liệu mới

007 bổ sung sáu bảng; không sửa dữ liệu/cột của 58 bảng trước đó:

| Bảng | Trách nhiệm |
|---|---|
| CoSoDichVu | Một cơ sở/chi nhánh thuộc một NCC, thông tin đang công bố, vị trí, tiện ích, phiên bản |
| BanCapNhatCoSo | Nội dung từng lần gửi, căn cứ quản lý riêng tư, quyết định và quản trị viên duyệt |
| DichVuCoSo | Cơ sở áp dụng của dịch vụ đang công bố |
| BanCapNhatDichVuCoSo | Cơ sở trong đúng phiên bản dịch vụ chờ duyệt/lịch sử |
| MocDiaDiem | Điểm cụ thể hoặc cửa vào, quan hệ với địa điểm chính, vị trí, nội dung có nguồn/ngày kiểm tra |
| YeuCauDatCoSo | Cơ sở và địa chỉ được ghi nhận khi gửi yêu cầu |

`tools/DefineBranchSchema.cjs` tạo SQL, entities, EF configurations và `docs/branch-schema.json`. Các FK không xóa dây chuyền. Có khóa đồng thời rowversion, chỉ một phiên bản cơ sở chờ duyệt, CHECK tọa độ đầy đủ/giới hạn và trạng thái, khóa duy nhất liên kết dịch vụ/cơ sở. Google Place ID dùng collation nhị phân để phân biệt hoa/thường. Liên kết duy nhất được giữ cho cơ sở đã duyệt; một bản nháp chưa xác minh không thể chiếm địa điểm để chặn hồ sơ của NCC khác.

## Địa chỉ, vị trí và nguồn

- Tên/địa chỉ hồ sơ do NCC hoặc quản trị viên cung cấp. Không sao chép nguyên danh bạ, đánh giá hay hình ảnh Google vào SQL.
- Google Place ID liên kết tới địa điểm Google, không chứng minh quyền quản lý cơ sở.
- Chọn địa điểm Google thì server xác minh ID và lấy vị trí. Tọa độ Google lưu tạm có hạn29ngày; worker dọn khi hết hạn, cache tọa độ trong bộ nhớ15phút. Thông tin tên/địa chỉ Google được đọc trực tiếp, không dùng như bản sao danh bạ trong DB.
- Đặt ghim/nhập tọa độ thủ công được ghi nguồn do người cung cấp xác nhận. Vị trí Google và vị trí tự cung cấp được phân biệt.
- Vị trí hết hạn không được dùng để khẳng định khoảng cách; có thể lấy lại từ Google khi cấu hình cho phép. Các cơ sở thiếu vị trí dùng được không được xem là đáp ứng phạm vi bắt buộc.
- Tiện ích Có/Không/Chưa công bố thuộc đúng cơ sở. Quán có món chay khác với quán thuần chay. Không dùng tiện ích của chi nhánh khác hoặc của NCC để suy ra tiện ích của cơ sở.
- Tên địa điểm tìm không dấu, kể cả Đ/đ. Gợi ý mốc Google loại các kết quả chỉ là thành phố/tỉnh/quốc gia. Không dùng tâm địa điểm chính thay cho cửa đã chọn.

## Google Maps và giới hạn được thể hiện

- Maps JavaScript API: bản đồ trong website, mốc và cơ sở đồng bộ với danh sách, ghim vị trí, đường đi.
- Places API (New): gợi ý tên, xác minh địa điểm, gợi ý địa điểm Google quanh mốc. Thông tin Google được ghi nguồn; các khoản giá/dịch vụ của NCC được trình bày riêng.
- Routes API: đường và thời gian ước tính cho đi bộ/ô tô/xe đạp. Khoảng cách thẳng khi lọc bán kính được ghi riêng với khoảng cách đường đi.
- Địa điểm Google chưa liên kết cơ sở trên website chỉ có thông tin Google/liên kết, không có nút đặt một dịch vụ không tồn tại.
- Gợi ý Google tối đa20địa điểm/lần theo API; không cam kết mọi cơ sở trong một vùng đều được trả về. Tiện ích bắt buộc chỉ nhận cơ sở có dữ liệu đã công bố tương ứng.
- Tra cứu trên website xét tối đa500cơ sở sau lọc tên/loại/tiện ích và có thông báo khi cần thu hẹp. Vị trí Google hết hạn chỉ làm mới có giới hạn trong lần tìm, tránh quét danh bạ hàng loạt.
- Chưa có key: vẫn quản lý cơ sở bằng tọa độ cung cấp và xem dữ liệu đã công bố; bản đồ/gợi ý/đường Google không được giả lập thành dữ liệu thực.

Hướng dẫn bật ba API, tạo hai khóa và cấu hình máy: [CAU_HINH_GOOGLE_MAPS.md](CAU_HINH_GOOGLE_MAPS.md).

## Các đường dẫn chính

| Đường dẫn | Quyền / chức năng |
|---|---|
| /kham-pha | Công khai, tìm quanh mốc, loại/tiện ích/phạm vi, danh sách/bản đồ |
| /co-so/{id} | Công khai, cơ sở được duyệt và dịch vụ tại cơ sở |
| /dia-diem/{id} | Công khai, mốc hoặc cửa vào đã công bố |
| /kham-pha/so-sanh?ids=… | Công khai, tối đa3cơ sở khác nhau |
| /nha-cung-cap/co-so | NCC, cơ sở thuộc tài khoản |
| /nha-cung-cap/co-so/them và /{id} | NCC, tạo/cập nhật cơ sở |
| /nha-cung-cap/co-so/luu | POST, NCC đang hoạt động, CSRF, kiểm tra quyền và phiên bản |
| /nha-cung-cap/co-so/hien-thi | POST, NCC sở hữu, ẩn/hiện cơ sở đã duyệt |
| /quan-tri/co-so và /{revisionId} | Admin, danh sách và kiểm tra hồ sơ |
| /quan-tri/co-so/duyet | POST, Admin, quyết định phiên bản |
| /quan-tri/moc-dia-diem | Admin, mốc và cửa vào |
| /api/maps/config | Cấu hình trình duyệt, không trả khóa máy chủ |
| /api/maps/suggest và /place | Gợi ý/chi tiết Google, giới hạn tần suất, không cache HTTP |
| /api/maps/route | POST JSON, CSRF, phương thức/đầu-cuối hợp lệ, giới hạn tần suất |

`BranchService` xử lý quyền, hồ sơ, phiên bản, liên kết và vị trí. `PlaceSearchService` xác định mốc, lọc và ghép kết quả. `GoogleMapsClient` gọi REST Google với field mask, timeout, lỗi đã làm sạch. `GoogleLocationCleanup` dọn tọa độ Google hết hạn. Các đường dẫn FullText cũ giữ nguyên; trang tìm kiếm chính chuyển sang luồng mốc/cơ sở. Nội dung cẩm nang/hỏi đáp006 vẫn có đường dẫn riêng.

## Tương thích và vận hành

Dịch vụ cũ không được tự gán cơ sở hoặc địa chỉ Google. NCC thấy thông báo dịch vụ chưa gắn cơ sở và cập nhật qua luồng xét duyệt. Các yêu cầu cũ giữ nguyên; yêu cầu mới đối với dịch vụ đã gắn cơ sở phải chỉ rõ cơ sở và kiểm tra lại trước khi gửi.

Sao lưu SQL COPY_ONLY/CHECKSUM và ZIP source/App_Data trước khi nâng cấp. Script007 có transaction và khóa nâng cấp, chạy lặp an toàn. Ca nghiệp vụ ghi dữ liệu chỉ chạy ở DB thử `HoTroDuLich_Branches_20261108_Final`, không tạo cơ sở/tài khoản QA trong DB chính.

## Kiểm thử và giới hạn xác minh

- `branches-features-results.json`:83assertion cho hồ sơ nhiều chi nhánh, hai NCC, quyền sở hữu, nội dung riêng tư, duyệt, mốc cửa vào, tiện ích, dịch vụ và snapshot yêu cầu.
- `branches-edge-results.json`:32assertion cho biên tọa độ, tìm không dấu, mốc không tự lặp, CSRF, khóa/ràng buộc SQL, URL, vòng lặp địa điểm, privacy và snapshot.
- `branches-contract-results.json`: kiểm thử payload Google mô phỏng và mapping EF/SQL. Không phải bằng chứng gọi Google trực tiếp.
- `branches-upgrade-results.json` và `branches-populated-upgrade-results.json`: diễn tập nâng cấp/lặp script và SHA/count của58 bảng trước nâng cấp.
- Người dùng chưa có API key nên chưa kiểm thử dịch vụ Google trực tiếp. Computer Use bị công cụ dừng vì không xác định chắc URL Chrome; không có xác minh giao diện bằng ảnh trong đợt này. Đã kiểm tra build, HTML/HTTP, JS syntax, nghiệp vụ và dữ liệu; không gọi đó là kiểm thử trực quan.

## Kết quả triển khai chính

Database HoTroDuLich đã áp dụng007:64bảng/699cột, CHECKDB đạt. tests/branches-deploy-results.json giữ toàn bộ58cặp SHA/count trước và sau migration. Backup SQL trước triển khai: HoTroDuLich_before_workflow_20261108_213502.bak (COPY_ONLY/CHECKSUM/VERIFYONLY), descriptor preview/workflow-backup.json, ZIP source và App_Data đã kiểm hash. Bản chính khởi động bằng start.ps1 tại http://localhost:5216. Không đưa tài khoản/cơ sở/mốc kiểm thử vào DB chính;45 kiểm tra chỉ đọc/tệp riêng tư sau khởi động đạt.
