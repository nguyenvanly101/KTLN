# Mục tiêu 1 — tìm kiếm thông tin du lịch — 08/11/2026

BẢN 006 ĐÃ TRIỂN KHAI NHƯNG NGƯỜI DÙNG CHƯA CHẤP THUẬN THIẾT KẾ CHỨC NĂNG. Yêu cầu mới: tự nghiên cứu lại chức năng tìm kiếm thuận tiện và thay đổi cả bảng thông tin dịch vụ. Đang đề xuất thiết kế theo nhu cầu và phương án dịch vụ tại docs/DE_XUAT_TIM_KIEM_DICH_VU_THEO_NHU_CAU.md; chưa sửa code/schema cho đề xuất mới. Không tiếp tục thêm chức năng tra cứu chung hoặc triển khai chuyến đi v2 theo các đề xuất cũ.

Kết quả cuối (08/11/2026):
- Main HoTroDuLich đã áp dụng006: 58 bảng/616 cột. tests/discovery-deploy-results.json chứng minh tất cả số dòng/SHA-256 của46 bảng trước nâng cấp không đổi. Main schema747checks PASS.
- Main đang chạy bằng .\start.ps1 tại http://localhost:5216, owned exec session42304. Test server5224 đã dừng (session82623). Các session build cũ đã hoàn tất; không chạy lại rehearsal/deploy one-shot vào database đã tồn tại.
- Build cuối preview/discovery-release và sourcebin normal build PASS. VerifyDiscovery1621 PASS giữ91 thao tác,46 bảng gốc, EF và143 cột mới. Source mới nhất đã được build và chạy trên main.
- Final suite: core52, contextual29, edge/privacy28, route thật8 PASS. HTML289 trên14 trang PASS. SQL schema test747/upgrade747/main747 PASS; permissions61, rehearsal51, main smoke33 PASS. Tổng hợp tests/discovery-completion-results.json. Đây là số assertion, không phải số yêu cầu hay tình huống duy nhất.
- Browser visual: CHƯA THỰC HIỆN vì Computer Use dừng do không xác định chắc URL Chrome. Không dùng công cụ khác vượt chặn. Chỉ list/selection, không gửi input, không có tab agent cần đóng. Phải báo rõ giới hạn này khi trả lời.
- Backup cuối preview/workflow-backup.json trỏ SQL HoTroDuLich_before_workflow_20261108_141751.bak (COPY_ONLY/CHECKSUM/VERIFYONLY), ZIPsource/App_Data đã kiểm SHA. Backup đầu lưu riêng preview/discovery-initial-backup.json.
- LocalDB bị ngắt giữa đợt khi có lượt mới; đã khởi động lại đúng instance, alias (localdb)\MSSQLLocalDB hiện hoạt động. Helper tools/ResolveLocalDb.ps1 tích hợp start.ps1, fallback pipe từ log chỉ dùng khi sqlcmd xác minh, không ghi pipe vào appsettings/registry. docs/TU_XU_LY_LOI_KHOI_DONG.md đã khôi phục hướng dẫn. Không xóa/tạo lại instance hay sửa OS.
- README đã thêm mục006 và hướng dẫn; docs/TRA_CUU_THONG_TIN_2026-11-08.md ghi schema/API/hàm/quyền/giới hạnnguồn. Không chép fixture QA vào main.

Các dòng tiến độ bên dưới là lịch sử triển khai, không phải việc còn thiếu. Nếu tiếp tục, đọc trạng thái cuối này trước.

Phạm vi: tìm kiếm tiếng Việt/câu hỏi có nguồn, điểm đến/khu vực, di chuyển, giá và điều kiện, sự kiện theo ngày, khả năng tiếp cận, tiện ích thiết yếu, kinh nghiệm theo chủ đề, hỏi đáp có kiểm duyệt, theo dõi cập nhật; map đồng bộ danh sách, quanh vị trí/dọc tuyến/xem trước điểm dừng. Giữ 91 hợp đồng FullText cũ và dữ liệu extension 005.

Thiết kế: SQL additive 006; danh mục thông tin chung (điểm đến/khu vực/địa điểm/dịch vụ/cẩm nang/di chuyển/sự kiện/tiện ích), thuộc tính có trạng thái có/không/chưa biết, liên kết nguồn/ngày kiểm tra, giá có đơn vị; nội dung mới có version/lịch sử. Tránh sửa bảng/row cũ. Quyền admin công bố; provider gửi đề nghị cho dịch vụ của mình; traveller hỏi/đánh giá/báo sai/follow; public đọc đã duyệt. Không tự công khai private trip/chat/documents.

Map: Leaflet 1.9.4 local vendor + OSM raster (attribution, no prefetch/offline), OSRM configurable, 1 request/s toàn ứng dụng + cache và timeout; không dùng Nominatim public. Tìm tên/toạ độ trong dữ liệu riêng hoặc chọn ghim. Khoảng cách thẳng và đường đi được ghi riêng, không gọi đường thẳng là tuyến thực. Thời gian tuyến không bao gồm dừng/ùn tắc.

UI: tiếp tục Open Atlas. Skill query design-system travel discovery map search trả Aurora/kids fonts không phù hợp: không áp dụng/persist. Query UX map search result navigation xác nhận no-results suggestions, autocomplete, sticky không che nội dung.

Tiến độ 08/11:
- Sao lưu thành công preview/workflow-backup.json; SQL COPY_ONLY/CHECKSUM/VERIFYONLY; source và App_Data ZIP kiểm hash. Snapshot 46 bảng cũ preview/discovery-main-before.json.
- LocalDB alias không dùng được trong tool sandbox; pipe từ log hoạt động: np:\\.\pipe\LOCALDB#4A1EA738\tsql\query. Không sửa/xóa instance hay OS. Truyền pipe bằng DISCOVERY_SQL_SERVER và ConnectionStrings__HoTroDuLich trong từng command, không ghi cố định vào config người dùng.
- Đã tạo 006: 12 bảng/143 cột, entities/mapping/manifest và module Discovery. Đã tạo database riêng HoTroDuLich_Discovery_20261108 (58 bảng) + khởi tạo dữ liệu.
- Build A/B/C/D thành công; đang chuẩn bị E để kiểm tra cuối. Source mới hơn B gồm trả lời theo ý định giá/giờ/địa chỉ/tiện ích; ghi chú; follow mục ẩn; ngày sự kiện; route JSON [FromBody]; source API cũ dùng UI mới.
- Test server B đang chạy cổng 127.0.0.1:5224, owned exec session 41281, binary preview/discovery-build-b; cần dừng đúng session rồi chạy build mới (đừng stop user processes).
- tests/discovery-features-results.json: PASS 52; tests/discovery-edge-results.json: PASS 26. Đây là B, cần chạy lại sau bản cuối. Edge test ẩn entry fixture của core; core rerun tạo fixture mới. Admin test DB password đã đổi thành HanhTrinhMoi@2026 qua helper test, main password chưa động đến.
- Real route test hiện fail vì body DTO có property IFormFile kế thừa làm inference bỏ JSON. Đã sửa action route [FromBody]; cần kiểm tra thật OSRM lại.
- Các lỗi đã sửa: partial _Card cần path tuyệt đối từ Explore controller; tọa độ decimal form phải invariant qua value provider chỉ cho Discovery/Explore; không ảnh hưởng controller FullText khác.
- Computer-use/Sky đã bị công cụ DỪNG cho lượt này vì không xác định được URL Chrome đủ chắc chắn. KHÔNG tiếp tục thao tác app trong lượt này, KHÔNG dùng công cụ khác để vượt rào đó. Chưa có kiểm thử thị giác/screenshot. Chỉ list/selection, chưa gửi bất kỳ input Chrome nào; không có tab agent cần đóng. Node_repl đã import @oai/sky, nhưng dừng dùng UI.

Việc còn lại: kiểm thử bản cuối (câu hỏi tự nhiên/sự kiện/giờ/phiên bản/NCC/riêng tư/tuyến thật), kiểm tra schema/EF/91 API FullText từ backup contract (có trong ZIP before-companion), permissions và diễn tập upgrade DB riêng từ SQL backup, backup mới và apply006 lên main giữ nguyên46 bảng, chạy main và cập nhật README/docs/kết quả. Không được báo hoàn thành trước các bước này. Không thực thi lại script companion restore cũ.
