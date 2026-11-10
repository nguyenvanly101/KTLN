# Đề xuất thiết kế lại tìm kiếm dịch vụ theo nhu cầu

Trạng thái: ĐỀ XUẤT ĐỂ RÀ SOÁT, chưa thay đổi schema/code/database đang chạy. Bản 006 trước đó đã triển khai nhưng người dùng đánh giá chức năng chưa hợp lý và yêu cầu xem lại cả bảng thông tin dịch vụ.

## Vấn đề từ mã hiện tại

DichVuDuLich và BanCapNhatDichVu dùng LoaiDichVu/KhuVucPhucVu dạng chữ, một GiaThamKhao/DonViGia, ThoiGianPhucVu và DieuKienPhucVu dạng mô tả. Extension Discovery bổ sung lớp thông tin chung, chưa thay thế cấu trúc dịch vụ gốc. Thiếu phương án phục vụ (hạng phòng/gói thuê/vé), sức chứa, địa điểm cung cấp cụ thể, lịch và giá theo điều kiện. Không thể suy ra các thông tin này bằng tìm từ khóa trong mô tả.

## Quy trình đề xuất

Chọn nhu cầu dịch vụ → nhập bối cảnh cần thiết → đặt điều kiện bắt buộc/sở thích → xem các phương án phù hợp đồng bộ với map → xem cấu thành giá và điều kiện → so sánh hoặc tìm phương án thay thế → lưu lựa chọn/gửi yêu cầu khi cần.

1. Tìm ở/ăn/di chuyển/trải nghiệm có biểu mẫu và bộ lọc riêng. Trường chung: khu vực/mốc vị trí/tuyến, ngày giờ, quy mô nhóm; chỉ hỏi khi có tác dụng với loại dịch vụ đó.
2. Tìm ở cấp phương án: một cơ sở có thể có nhiều hạng phòng/gói dịch vụ. Không hiển thị giá rẻ nhất của phương án không phục vụ đủ nhóm khách.
3. Tìm quanh một mốc hoặc dọc tuyến: khoảng cách tới điểm cung cấp/đón thực tế; dịch vụ giao tận nơi thể hiện vùng phục vụ. Không dùng trụ sở nhà cung cấp làm vị trí tất cả dịch vụ.
4. Lọc động: thuộc tính thay đổi theo loại, có số kết quả và đơn vị rõ. Có/Không/Chưa biết; không suy đoán thuộc tính từ mô tả. Trong một bộ lọc có thể chọn nhiều giá trị thay thế; giữa các điều kiện bắt buộc dùng AND.
5. Tính và so sánh chi phí theo cùng bối cảnh: ngày/đêm, người, phòng/xe/vé, khoản bắt buộc. Giá ước tính, phí chưa biết và đặt cọc hoàn lại thể hiện riêng; quán ăn chỉ có khoảng chi tiêu không được biến thành tổng hóa đơn chính xác.
6. Kết quả giải thích điều kiện đã khớp và dữ liệu cần xác nhận. Không tự nới điều kiện bắt buộc. Nếu rỗng, đề nghị thay từng điều kiện kèm số lựa chọn mở thêm, người dùng chủ động chọn.
7. Tìm phương án tương tự: giữ những điều kiện khách chọn, thay mức giá/khu vực/phương án khác; không tự bỏ sức chứa, khả năng tiếp cận hoặc yêu cầu giấy tờ.

## Màn hình khách sử dụng

Một mục Tìm dịch vụ, với bốn lựa chọn Ở / Ăn uống / Di chuyển / Trải nghiệm. Có thể tìm khi chỉ biết loại hoặc khu vực; không bắt nhập ngày, số người, ngân sách trước khi được xem thông tin. Mỗi thông tin bổ sung giúp thu hẹp kết quả hoặc tính giá rõ hơn, trường nào cần để tính giá phải được giải thích tại chỗ.

- Ô tìm có gợi ý phân biệt tên khu vực, địa điểm làm mốc, tên cơ sở và loại dịch vụ; hỗ trợ từ đồng nghĩa và tiếng Việt không dấu. Khách chọn gợi ý để tránh lẫn các địa danh trùng tên. Không tự biến một suy đoán từ câu nhập thành điều kiện bắt buộc.
- Vị trí có ba cách: chọn khu vực, tìm quanh một mốc/vị trí hiện tại, hoặc tìm trên tuyến. Chỉ hỏi quyền vị trí khi khách chọn Gần tôi; luôn có cách nhập/chọn mốc thủ công.
- Phía trên có tóm tắt nhu cầu bằng các nhãn sửa được: loại, vị trí, thời gian, quy mô nhóm và ngân sách. Chuyển loại giữ bối cảnh còn phù hợp; các bộ lọc riêng của loại cũ được bỏ rõ ràng.
- Danh sách và bản đồ cùng một truy vấn. Chọn dòng đánh dấu đúng điểm cung cấp; chọn điểm mở dòng tương ứng. Di chuyển map chỉ tìm lại khi chọn Tìm trong vùng này, giữ các điều kiện khác. Điện thoại chuyển Danh sách/Bản đồ và giữ vị trí đang xem.
- Bản đồ có phương thức di chuyển khi dùng thời gian đường đi. Không gắn nhãn khoảng cách đường bộ/thời gian tuyến cho một phép đo đường thẳng. Thiếu tọa độ vẫn có thể xem trong danh sách với nhãn chưa có vị trí, nhưng không thể coi là đáp ứng giới hạn khoảng cách bắt buộc.
- Mỗi dòng kết quả hiển thị: cơ sở/điểm cung cấp → phương án đáp ứng nhóm → giá kèm cách tính → khoảng cách/thời gian → điều kiện đã khớp và điều cần xác nhận → Xem / Trên bản đồ / So sánh. Số cơ sở và số phương án phải phân biệt, tránh đếm một khách sạn có nhiều hạng phòng thành nhiều cơ sở.
- Xem thông tin và so sánh không cần đăng nhập. Lưu đường dẫn tìm kiếm để chia sẻ và mở lại đúng bộ lọc; quay từ trang chi tiết giữ kết quả, vị trí cuộn và trạng thái bản đồ. Chỉ yêu cầu đăng nhập khi thực hiện hành động gắn tài khoản.
- Từ cơ sở/điểm tham quan đang xem có thể chọn Ăn uống quanh đây, Chỗ ở gần đây hoặc Thuê xe quanh đây. Dùng đúng điểm đang xem làm mốc và giữ bối cảnh còn phù hợp, không bắt khách nhập lại địa danh. Đây là thao tác tìm thông tin liên quan, không tự đưa dịch vụ vào lịch trình.

## Bảng thông tin và thẻ kết quả theo từng loại

Giao diện khách có chế độ thẻ gọn và bảng so sánh tối đa ba lựa chọn. Không dùng cùng một bộ cột cho mọi loại, không bắt đọc mô tả dài để tìm các thuộc tính có thể lọc.

| Loại | Những cột/thông tin chính của kết quả |
|---|---|
| Lưu trú | Cơ sở + hạng phòng/căn, số phòng cần/giường/sức chứa, tổng theo ngày và nhóm nếu đủ dữ liệu, tiện ích bắt buộc, vị trí và tình trạng cần xác nhận |
| Ăn uống | Đúng chi nhánh, loại món/kiểu phục vụ, khoảng chi tiêu mỗi người hoặc giá món, giờ phục vụ, phục vụ nhóm/tiện ích, khoảng cách. Phân biệt quán thuần chay với quán có lựa chọn chay |
| Thuê xe/di chuyển | Xe/gói, số chỗ, tự lái/có tài xế, giá theo đúng giờ/ngày/chuyến và thời lượng, giao nhận, đặt cọc/phụ phí và điều kiện công bố |
| Hoạt động/vé | Gói/hạng vé, thời điểm bắt đầu và thời lượng, nhóm/độ tuổi áp dụng, khoản bao gồm, tổng theo số vé nếu có giá phù hợp và điểm tập trung |

Giá trị Chưa biết không được trình bày là Không có hoặc đã đáp ứng. Kết quả đáp ứng điều kiện bắt buộc phải có dữ liệu chứng minh; những cơ sở thiếu dữ liệu quan trọng chỉ xuất hiện trong mục riêng Các lựa chọn cần xác nhận nếu khách muốn xem. Nếu thiếu giá áp dụng cho ngày/nhóm, ghi giá tham khảo với đơn vị và không xếp là đã nằm trong tổng ngân sách.

## Thông tin chuyên biệt

| Nhóm | Thông tin cần để phục vụ tìm kiếm |
|---|---|
| Lưu trú | Loại cơ sở, hạng phòng/căn, số giường, sức chứa người lớn/trẻ em, số phòng, tiện ích cơ sở và tiện ích hạng phòng, nhận/trả phòng, giá theo đêm/ngày áp dụng |
| Ăn uống | Loại món, kiểu phục vụ, món chay do quán công bố, thực đơn/khoảng chi tiêu có đơn vị, sức chứa nhóm, khung phục vụ, bãi xe và tiện ích của đúng chi nhánh |
| Thuê xe/di chuyển | Loại xe, số chỗ, tự lái/có người lái, đơn vị giờ/ngày/chuyến, nơi giao nhận/vùng giao, yêu cầu giấy tờ do NCC công bố, đặt cọc và phụ phí |
| Hoạt động/vé | Hạng vé/gói, ngày/khung bắt đầu, thời lượng, sức chứa, điều kiện tuổi/chiều cao khi được công bố, điểm tập trung, ngôn ngữ và khoản bao gồm |

## Cấu trúc dữ liệu dự kiến

Thiết kế chi tiết tiếp theo cần đồng bộ schema, API tìm kiếm/đăng dịch vụ/xét duyệt, biểu mẫu NCC, màn hình khách và snapshot đặt dịch vụ. Tên bảng dưới đây là tên đề xuất, chưa phải migration đã áp dụng.

| Nhóm dữ liệu | Trách nhiệm và quan hệ |
|---|---|
| DanhMucDichVu | Phân cấp nhóm/loại bằng ID, tên hiển thị và bí danh; thay lọc bằng chuỗi tùy ý |
| DichVuDuLich | Giữ MaDichVu hiện có, NCC, danh mục, tên, mô tả chung, trạng thái và phiên bản công bố |
| DiaDiemCungCap + DichVuDiaDiem | Địa chỉ, khu vực chuẩn hóa, tọa độ/lối vào/điểm đón, kiểu vị trí; một dịch vụ có thể có nhiều điểm cung cấp |
| PhuongAnDichVu | Hạng phòng/gói thuê/hạng vé, số lượng/sức chứa; liên kết đúng điểm cung cấp; các bảng chuyên biệt lưu dữ liệu số/enum theo từng nhóm |
| TienIch + liên kết theo phạm vi | Danh mục thuộc tính có kiểu; áp dụng cho cơ sở, điểm cung cấp hoặc phương án; Có/Không/Chưa biết, nguồn/ngày kiểm tra |
| BangGiaPhuongAn + KhoanPhi | Tiền tệ, đơn vị tính, ngày/khung áp dụng, điều kiện số lượng/đối tượng, khoản bắt buộc/tùy chọn/đặt cọc hoàn lại; không lấy một mức giá chung để báo mọi nhu cầu |
| LichPhucVu + NgoaiLe | Lịch tuần/nhiều khung/ngày nghỉ/tạm dừng/phiên hoạt động; độc lập với tồn phòng/chỗ. Chỉ công bố còn chỗ khi có dữ liệu đủ tin cậy và cơ chế kiểm soát tương ứng |
| PhienBanCongBo | Bản cập nhật đồng bộ cho dữ liệu gốc và các bảng con; admin duyệt đúng bản, không trộn giá mới với phương án cũ |

Phần Discovery có thể giữ cẩm nang/hỏi đáp/kinh nghiệm và tham chiếu dịch vụ; hồ sơ dịch vụ là nguồn chính cho vị trí, phương án, tiện ích, giá và lịch. Không có hai biểu mẫu chỉnh cùng một giá/giờ hoạt động độc lập.

## Quy tắc cần chốt khi lập trình

- Điều kiện bắt buộc gồm sức chứa, thời gian sử dụng, vùng phục vụ và các yêu cầu khách xác định là cần thiết. Sở thích chỉ dùng xếp hạng; không dùng số điểm AI không có căn cứ.
- Kết quả cho biết giá đang tính cho phương án nào, nhóm bao nhiêu người và khoảng thời gian nào. Giữ đơn vị gốc khi chưa đủ dữ liệu để quy đổi.
- Giờ mở cửa, tiếp nhận yêu cầu và còn phòng/chỗ là ba thông tin khác nhau. Mặc định giữ quy trình NCC xác nhận hiện có.
- Một tiện ích tại chi nhánh/phương án không mặc nhiên áp dụng cho cả NCC hay toàn bộ dịch vụ.
- Cập nhật từ một luồng: NCC chọn loại → nhập dữ liệu chuyên biệt → chọn điểm cung cấp → khai giá/lịch/tiện ích → xem bản khách sẽ thấy → gửi duyệt.
- Giữ ID và các liên kết đặt dịch vụ cũ. Các bản đã xác nhận giữ giá/điều kiện lịch sử; không tính lại theo bảng giá mới.
- Di chuyển dữ liệu cũ: ánh xạ trường có căn cứ, giữ thông tin chưa đủ là chưa biết; biểu mẫu hỗ trợ NCC bổ sung theo đúng loại. Không tự suy đoán sức chứa, lịch trống hoặc tọa độ từ mô tả.

## Ví dụ kiểm tra tính đúng của luồng

Khách cần chỗ ở cho 4 người lớn/2 đêm, ngân sách tổng 2,2 triệu, cần chỗ đỗ ô tô và tới phố cổ tối đa 15 phút. Hệ thống phải xét sức chứa của phương án phòng/căn, tính số phòng/đêm và các khoản đã biết, đối chiếu bãi xe của đúng cơ sở, tính thời gian đường đi từ điểm cung cấp. Nếu chỉ có tiếp nhận yêu cầu, hiển thị Cần NCC xác nhận thay vì tự nhận còn phòng.

Ví dụ số học minh họa: 2 phòng × 2 đêm × 500.000 = 2.000.000; phí bắt buộc 100.000 → chi phí đã biết 2.100.000. Đặt cọc hoàn lại 500.000 được trình bày riêng cùng điều kiện và thời điểm thu. Ví dụ không phải giá dịch vụ thực tế.

## Tham khảo cách tổ chức nghiệp vụ

- Airbnb: bộ lọc lưu trú theo ngày/khách, loại chỗ ở, giường/phòng và tiện ích: https://www.airbnb.com/help/article/479
- Booking.com Demand: tìm cơ sở theo ngày, khách/phòng và trả phương án/giá phù hợp: https://developers.booking.com/demand/docs/accommodations/search-for-available-properties
- Booking.com Demand: phân biệt thành phần giá và chi phí: https://developers.booking.com/demand/docs/accommodations/prices-accommodations

Các nguồn dùng để đối chiếu cách tổ chức; đề xuất này không đồng nghĩa đã tích hợp API, dữ liệu hay cơ chế đặt của các nền tảng đó.

## Tiêu chí nghiệm thu chức năng

1. Một khách sạn có phòng đôi rẻ và căn bốn người phải trả phương án đủ cho bốn khách; không dùng giá một phòng đôi để quảng cáo tổng chi phí nhóm.
2. Lọc quán thuần chay không nhận quán chỉ có một món chay; tiện ích của chi nhánh khác không được tính cho chi nhánh đang tìm.
3. Tìm điểm thuê xe trên tuyến phải xét điểm giao nhận và mức đi vòng theo phương thức đã chọn; không dùng địa chỉ trụ sở NCC thay thế.
4. Giới hạn tổng ngân sách phải xét cùng thời gian/nhóm và khoản bắt buộc. Khoản hoàn lại trình bày riêng. Thiếu giá/ngày hoặc phụ phí chưa xác định phải được báo rõ.
5. Không có kết quả thì hệ thống nêu điều kiện đang giới hạn và gợi ý thay từng điều kiện với số kết quả thực tế; chỉ áp dụng khi khách chọn.
6. Giá, giờ, phương án và tiện ích sau duyệt phải thống nhất ở trang chi tiết, kết quả, bản đồ và biểu mẫu NCC. Yêu cầu đặt đã xác nhận giữ thông tin lịch sử.
7. Chuyển danh sách/bản đồ, vào chi tiết rồi quay lại hoặc mở đường dẫn chia sẻ phải khôi phục đúng nhu cầu tìm kiếm. Chỉ đọc thông tin và so sánh không bị buộc đăng nhập.
