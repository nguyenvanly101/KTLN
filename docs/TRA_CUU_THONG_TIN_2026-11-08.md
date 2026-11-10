# Mục tiêu 1: giúp khách du lịch tìm kiếm thông tin

Phạm vi đã được duyệt ngày 08/11/2026. Không thay thế thiết kế 91 thao tác FullText cũ; bổ sung mô-đun tra cứu. Không triển khai quy trình chuyến đi v2 trong đợt này.

## Luồng sử dụng

Khám phá theo nhu cầu → lọc loại/khu vực/ngày/giá/thuộc tính → xem danh sách và bản đồ đồng bộ → đọc chi tiết có nguồn → tìm thông tin liên quan/quanh vị trí/dọc tuyến → so sánh/lưu/theo dõi. Tạo chuyến đi và gửi yêu cầu đặt là thao tác tùy chọn tiếp theo qua các luồng cũ.

Tìm câu hỏi dựa trên nội dung và hỏi đáp đã công bố; từ khóa được chuẩn hóa tiếng Việt và hiển thị cho khách kiểm tra. Không sử dụng mô hình sinh câu trả lời ngoài dữ liệu. Chưa có dữ liệu thì hiển thị rõ và cho gửi câu hỏi tại mục liên quan.

## Dữ liệu và hàm

Schema SQL `006_TraCuuThongTin.sql` bổ sung 12 bảng; thiết kế khai báo tại `tools/DefineDiscoverySchema.cjs`, manifest `docs/discovery-schema.json`, EF configuration explicit. Giữ nguyên 46 bảng có trước và các khóa, quyền, dữ liệu riêng tư của chuyến đi.

| Bảng | Mục đích |
|---|---|
| DiscoveryEntry | Điểm đến, khu vực, địa điểm, dịch vụ, cẩm nang, di chuyển, sự kiện, tiện ích; nguồn, ngày kiểm tra, thời hạn, tọa độ, giờ, khoảng giá |
| DiscoveryFeature | Thuộc tính Có/Không/Chưa biết; ghi chú; bộ lọc chỉ khớp Có |
| DiscoveryPrice | Giá theo hạng mục, đơn vị, điều kiện, khoản bao gồm/trả thêm và ngày hiệu lực; giữ dòng cũ khi thay đổi |
| DiscoveryQuestion | Câu hỏi, trả lời có nguồn, người trả lời, kiểm duyệt, phiên bản |
| DiscoveryExperience | Kinh nghiệm theo chủ đề/ngày trải nghiệm; pending trước khi public; không tự gắn xác minh |
| DiscoveryReport | Phản ánh riêng; Pending → Checking → Resolved/Rejected, có người tiếp nhận và phản hồi |
| DiscoveryFollow | Theo dõi tự nguyện; thông báo trong hệ thống khi nội dung đã công bố thay đổi |
| DiscoveryRevision | Lịch sử append-only của nội dung, thuộc tính và bảng giá |
| DiscoveryProposal | NCC đề nghị bổ sung dịch vụ của mình; admin kiểm tra payload và phiên bản trước khi áp dụng |
| DiscoveryCollection / Item | Danh sách riêng tư, ghi chú, bỏ mục/lưu trữ; đồng bộ từ thiết bị do khách chủ động |
| DiscoveryRoute | Tuyến riêng tư, 2–8 điểm theo thứ tự, phương tiện; tính lại thời gian khi mở |

`DiscoveryService.Search/Detail/Suggest/Compare` cung cấp truy vấn; `DiscoveryContent` quản lý công bố và đề nghị; `DiscoveryCommunity` xử lý hỏi đáp, kinh nghiệm, phản ánh, thư viện; `DiscoveryRouter` tính tuyến và `DiscoveryIndexWorker` đưa thông tin gốc đã công bố vào chỉ mục mỗi phút. Tên, mô tả, giá và giờ dịch vụ liên kết được đọc từ bản dịch vụ hiện hành, không lấy một bản giá sao chép làm giá mới.

Các thao tác ghi chạy transaction SERIALIZABLE; nội dung sửa/duyệt dùng rowversion; gửi câu hỏi/kinh nghiệm/phản ánh/đề nghị/tuyến có submission ID chống lặp. Không tự đổi dữ liệu đang công bố khi nhận phản ánh. Không dùng kinh nghiệm địa điểm để tạo đánh giá dịch vụ đã sử dụng; đánh giá dịch vụ tiếp tục qua yêu cầu đặt FullText.

## API và quyền

Public GET: `/Discovery`, `/Discovery/Entry/{id}`, `/Discovery/Compare?ids=...`, `/api/discovery/suggest?q=...`, `/api/discovery/map?...`. Root `/` dùng giao diện tra cứu mới. Các route `/thong-tin/...` cũ vẫn giữ hợp đồng.

Public POST `/api/discovery/route`: JSON points/mode/corridor/filter, có antiforgery và giới hạn tần suất. Trả tuyến thực, quãng đường, phút ước tính, các lựa chọn gần hình tuyến, thời gian đi thêm nếu tính được. Không trả metadata nội bộ hay dữ liệu riêng.

Signed: Library, Ask, Report, Follow, Collection, SaveRoute, ArchiveRoute. Traveller thêm Experience. Provider chỉ Edit/Propose và Answer câu hỏi thuộc dịch vụ của mình; câu trả lời NCC chờ admin kiểm tra. Admin Save/Answer/ModerateExperience/HandleReport/ReviewProposal. Mọi kiểm tra quyền đối tượng được lặp lại trong service, không phụ thuộc menu hay field ẩn.

## Bản đồ và giới hạn dữ liệu

Leaflet 1.9.4 lưu local; JS/CSS kiểm tra SHA-256 theo bản phát hành. Nền bản đồ OpenStreetMap có attribution, không tải trước hoặc tải offline. URL nền đổi được qua `Discovery:Map:TileUrl`; cấu hình CSP phải cho phép đúng origin tương ứng.

OSRM/FOSSGIS mặc định phục vụ thử nghiệm qua 3 endpoint cấu hình `Discovery:Routing:Car/Bike/Foot`. Một gate toàn ứng dụng giữ tối đa 1 request/s, cache 30 phút, timeout và thông báo lỗi rõ. Nguồn công cộng không cam kết SLA; khi triển khai nhiều người dùng cần nhà cung cấp hoặc máy chủ định tuyến phù hợp. Không gọi public Nominatim.

Tọa độ tuyến được gửi tới nhà cung cấp khi khách chủ động tính; không tự lấy vị trí thiết bị hay theo dõi nền. Ước tính không gồm ùn tắc trực tiếp, dừng/chờ; ô tô/đi bộ/xe đạp theo profile, không giả lập lịch xe buýt hay xe máy. Điểm thiếu tọa độ vẫn có trong danh sách. Bản đồ tối đa 300 vị trí; tìm kiếm tối đa 1.500 mục; khuyến khích thu hẹp điều kiện. Dọc tuyến kiểm tra tối đa 5 ứng viên gần hình tuyến, giữ thứ tự điểm dừng, tính thời gian đi thêm bằng tuyến ghé thật; không hứa tối ưu toàn cục hay phù hợp giờ mở cửa tại thời điểm đến.

Nội dung khởi đầu có nguồn xác định; dữ liệu gốc chưa có ngày kiểm tra, giá, tiện ích hoặc tọa độ được ghi Chưa có thông tin. Giá hiển thị không xác nhận còn chỗ. Lịch sự kiện, khả năng tiếp cận, giá theo hạng mục được quản trị/NCC nhập và kiểm tra; không tự tạo lịch, giá, đánh giá hay tiện ích giả.

## Kiểm thử và nâng cấp

Theo dõi tại `preview/discovery-work-notes.md`. Kết quả kiểm thử mới được lưu riêng trong `tests/discovery-*-results.json`; các báo cáo companion cũ không được tính là kiểm thử cho đợt này. Sao lưu source/SQL COPY_ONLY CHECKSUM/App_Data trước nâng cấp, kiểm tra schema và dấu vân tay dữ liệu cũ trên database riêng trước khi áp dụng bản chính.
