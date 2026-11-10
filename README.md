# Hành Trình — Hệ thống hỗ trợ khách du lịch tự túc

ASP.NET Core MVC .NET 10, Razor, Bootstrap 5, Entity Framework Core 10 và SQL Server. Thiết kế nền tảng lấy từ bản **26271.3-FullText-XayDungHeThongHoTroKhachDuLichTuTuc (4).docx** người dùng cung cấp. Hợp đồng triển khai lấy từ Chương 4; các điểm mâu thuẫn trong tài liệu được ghi tại [báo cáo thực thi](docs/THUC_THI_FULLTEXT_2026-11-04.md).

Luồng mới đặt chuyến đi làm trung tâm: tổng quan việc cần xử lý, lịch trình, dịch vụ đã gắn, dự trù và bản đồ. Phần liên kết dữ liệu/API bổ sung sau thiết kế gốc được mô tả riêng tại [thiết kế luồng chuyến đi](docs/LUONG_CHUYEN_DI_2026-11-05.md). Các đợt mở rộng gồm [lưu/so sánh, chuẩn bị, thực chi và thông tin/ảnh dịch vụ](docs/MO_RONG_SAN_PHAM_2026-11-05.md), tiếp đến [hành trình trong ngày, thời lượng, chia sẻ/in, đánh giá và lịch phục vụ](docs/HANH_TRINH_MO_RONG_2026-11-05.md).

Phần **Không gian đồng hành — 005**, hoàn thiện ngày 06/11/2026, bổ sung phong cách/gợi ý, lời rủ hoạt động, nhóm và lời mời, trao đổi/thông báo, bình chọn/lịch chung, việc chuẩn bị, chia chi phí/đối chiếu hoàn trả, điểm hẹn và chặn/báo cáo. Mở **Đồng hành → Lời rủ đi cùng** hoặc [trang đồng hành](http://localhost:5216/Together). [Thiết kế, API, hàm và hướng dẫn dùng](docs/KHONG_GIAN_DONG_HANH_2026-11-05.md). Database chính đã nâng cấp 005: **46 bảng/473 cột**; 31 bảng cũ giữ nguyên dữ liệu qua cửa sổ nâng cấp. Dữ liệu kiểm thử nằm riêng.

## Địa điểm cụ thể, cơ sở và bản đồ — 007

Luồng tìm kiếm chính hiện là **chọn mốc cụ thể → tìm cơ sở quanh mốc → xem dịch vụ tại đúng cơ sở**. Nhà cung cấp quản lý nhiều quán/chi nhánh; mỗi cơ sở có địa chỉ/vị trí riêng và được xét duyệt. Dịch vụ chọn cơ sở áp dụng, yêu cầu dịch vụ giữ tên/địa chỉ chi nhánh lúc gửi. Mở [Địa điểm & dịch vụ](http://localhost:5216/kham-pha), NCC dùng **Cơ sở & chi nhánh**, admin dùng **Xét duyệt cơ sở** và **Địa điểm & cửa vào**.

Mặc định trang Khám phá dùng **OpenStreetMap**, tìm địa chỉ và địa điểm quanh mốc qua **Photon** và xem đường qua **OSRM**. Chạy `.\start.ps1` là có thể thử mà không cần API key. Các dịch vụ công cộng có giới hạn tải. Google Maps là cấu hình tùy chọn. [Hướng dẫn dùng bản đồ và bật Google Maps](docs/CAU_HINH_GOOGLE_MAPS.md).

Database chính đã nâng cấp 007 lên **64 bảng / 699 cột**. Toàn bộ số dòng và SHA-256 của 58 bảng trước nâng cấp được giữ trong cửa sổ migration. [Thiết kế, dữ liệu, API, hàm và phạm vi xác minh 007](docs/CO_SO_GOOGLE_MAPS_007.md).

## Tra cứu thông tin và bản đồ — 006 (08/11/2026)

Trang chủ mới tập trung giúp khách tìm hiểu trước khi lựa chọn: tìm không dấu/theo câu hỏi cơ bản, phân loại điểm đến/khu vực/địa điểm/dịch vụ/cẩm nang/di chuyển/sự kiện/tiện ích, lọc ngày–giá–điều kiện đã xác nhận, thông tin có nguồn và ngày kiểm tra, hỏi đáp/kinh nghiệm qua kiểm duyệt, phản ánh, theo dõi cập nhật, danh sách riêng tư và tuyến đã lưu. Bản đồ đồng bộ danh sách, tìm quanh vị trí hoặc dọc tuyến và xem trước điểm ghé qua.

Mở **Khám phá** tại trang chủ. Admin/NCC dùng **Thông tin tra cứu** trong menu không gian quản lý; chỉ admin công bố thông tin mới và duyệt đề nghị. Dữ liệu chưa biết được ghi rõ; không tự tạo giá, giờ, tiện ích hay đánh giá. Bản đồ dùng Leaflet/OpenStreetMap, tính tuyến OSRM có cấu hình thay nguồn, không cần API key để thử nghiệm. Nguồn công cộng có giới hạn phục vụ; cấu hình triển khai và phạm vi dữ liệu xem tại [thiết kế và hướng dẫn tra cứu](docs/TRA_CUU_THONG_TIN_2026-11-08.md).

Database chính đã nâng cấp 006: **58 bảng / 616 cột**. Đã đối chiếu toàn bộ số dòng và SHA-256 của 46 bảng trước nâng cấp; dữ liệu kiểm thử ở database riêng. Các con số ở đoạn 005 phía trên là trạng thái của đợt cũ.

## Mở website

Bản chính chạy tại **[http://localhost:5216](http://localhost:5216)**, database `HoTroDuLich`. Khi cần khởi động lại, mở PowerShell tại `D:\KLTN` và chạy:

```powershell
.\start.ps1
```

Hoặc chỉ rõ dự án khi đang ở thư mục `D:\KLTN`:

```powershell
dotnet run --project .\src\HoTroDuLich.Web --launch-profile http
```

Muốn tự cập nhật khi sửa mã, dùng:

```powershell
.\start.ps1 -Dev
```

Lệnh tương đương:

```powershell
dotnet watch --project .\src\HoTroDuLich.Web run --launch-profile http
```

Profile `http` đã cấu hình môi trường `Development`. Không cần thêm `dev` sau `dotnet run`; từ này chỉ được truyền vào ứng dụng như một tham số. Thư mục gốc `D:\KLTN` không chứa file `.csproj`, nên cần dùng `start.ps1`, chỉ rõ `--project`, hoặc chuyển vào `src\HoTroDuLich.Web` trước khi chạy.

Dừng tiến trình đang dùng cổng 5216 trước khi chạy thêm một bản. Nhấn Ctrl+C trong terminal chạy máy chủ để dừng. Máy cần .NET 10 SDK và SQL Server Express LocalDB.

Nếu không chạy được, làm theo [hướng dẫn tự xử lý lỗi khởi động](docs/TU_XU_LY_LOI_KHOI_DONG.md): nhận diện sai thư mục/lệnh, cổng bị chiếm, LocalDB và lỗi Application Control; có lệnh kiểm tra và dấu hiệu thành công cho từng trường hợp.

Ở Development, ứng dụng tạo lược đồ nền bằng `Database/schema.sql` khi database rỗng, sau đó chạy các phần mở rộng 002/003/004/005/006 có thể chạy lại; dữ liệu mẫu chỉ thêm nếu chưa có tài khoản. Script căn chỉnh nền 001 vẫn là bước nâng cấp riêng. Database chính trên máy đã áp dụng 001–006 với sao lưu và đối chiếu dữ liệu. `start.ps1` kiểm tra instance LocalDB hiện có trước khi chạy web; không tự xóa/tạo lại instance khi có lỗi.

## Tài khoản mẫu

Mật khẩu ban đầu: `HanhTrinh@2026`.

| Vai trò | Email |
| --- | --- |
| Khách du lịch | khach@hanhtrinh.vn |
| Khách thứ hai | linh@hanhtrinh.vn |
| Nhà cung cấp | nhacungcap@hanhtrinh.vn |
| Quản trị viên | admin@hanhtrinh.vn |

Quản trị viên được khởi tạo mới phải đổi mật khẩu ở lần đăng nhập đầu tiên. Dữ liệu mẫu, ảnh và giá phục vụ trình diễn. Mật khẩu Admin ở database kiểm thử 5219 đã được đổi riêng bởi bộ kiểm thử; database chính và database thử nghiệm tách biệt.

## Kiến trúc và hợp đồng FullText

Các số lượng 25 bảng/267 cột dưới đây mô tả thiết kế FullText nền. Bản hiện hành gồm phần mở rộng đã được chấp thuận: **46 bảng, 473 cột, 83 FK NO ACTION, 161 CHECK, 31 unique index, 50 query index và 4 view**. Các mục bổ sung được khai báo trong [product-extension-schema.json](docs/product-extension-schema.json), [journey-extension-schema.json](docs/journey-extension-schema.json) và [companion-extension-schema.json](docs/companion-extension-schema.json); 91 action chuẩn được giữ. Mã kiểm chứng hiện hành dùng hồ sơ `--companion-extension`.

- **43 Controller chuẩn, 91 action GET/POST** theo Bảng 4.86, cùng 43 lớp ViewModel đầu vào. Danh sách route, query và policy: [fulltext-contract.json](docs/fulltext-contract.json).
- **19 component nghiệp vụ** có interface Service và đăng ký Scoped, cùng **11 giao diện liên component**. Các interface nằm ở `Services/Contracts/`; hiện thực ở `Services/Components/`.
- `TravelService` là lớp chuyển tiếp cho các đường dẫn giao diện cũ. Xử lý nghiệp vụ đã tách sang component; những URL tương thích gọi cùng Service và cùng kiểm tra quyền.
- **25 lớp miền, 267 thuộc tính vô hướng, 38 quan hệ và 24 enum**, ánh xạ qua 25 `IEntityTypeConfiguration`. RowVersion kiểm soát cập nhật đồng thời.
- **SQL là nguồn chuẩn**: 25 bảng, 117 CHECK, 18 chỉ mục duy nhất, 34 chỉ mục tra cứu, 4 view và 49 DEFAULT có tên. Collation `Vietnamese_100_CI_AS_SC`, RCSI ON, snapshot isolation OFF, recovery FULL, PAGE_VERIFY CHECKSUM, AUTO_CLOSE OFF.
- `IGiaoDich` dùng Serializable, thử lại toàn bộ thao tác tối đa ba lần khi deadlock/timeout. Thông báo và nhật ký quyết định đi cùng giao dịch; tệp vừa lưu được xóa bù khi thất bại.

Các API dùng biểu mẫu MVC, antiforgery token và trường tiếng Việt theo FullText. Bộ tiếp nhận dữ liệu hỗ trợ các tên trường của giao diện cũ. POST thành công dùng Post–Redirect–Get; lỗi có mã nghiệp vụ trong danh mục 98 mã.

## Chức năng mở rộng mới

- **Đồng hành từ lời rủ đến cùng đi:** gợi ý theo phong cách, hoạt động ngắn, nhóm/lời mời, trao đổi/thông báo, bình chọn/lịch chung, phân công chuẩn bị, chi phí/đối chiếu hoàn trả, điểm hẹn và chặn/báo cáo.
- **Hôm nay đi đâu:** điểm dừng tiếp theo, tiến độ hoàn thành, lịch trong ngày, dịch vụ và hỗ trợ trong cùng màn hình.
- **Thời gian vừa sức:** thời lượng/di chuyển tùy chọn, cảnh báo chồng lấn cả qua nửa đêm, giữ thứ tự người dùng đã xếp.
- **Chia sẻ / in:** snapshot chỉ xem, chọn thông tin chia sẻ, có hạn và thu hồi; bản in riêng hoặc lưu PDF qua trình duyệt.
- **Đánh giá trải nghiệm:** từ yêu cầu đã có kết quả; khách viết, NCC phản hồi và Admin duyệt từng phần.
- **Lịch phục vụ:** NCC cập nhật tiếp nhận/tạm ngừng từng ngày; khách xem lịch, kiểm tra lại ngày tạm ngừng trước khi gửi yêu cầu.
- **Lưu và so sánh:** lưu địa điểm/dịch vụ vào bộ sưu tập riêng, so sánh 2–3 dịch vụ rồi xếp lựa chọn vào chuyến đi. Mục đã lưu nằm trong nhóm Khám phá.
- **Chuẩn bị chuyến đi:** checklist có nhóm, hạn, đánh dấu xong/mở lại; bộ gợi ý không trùng và lời nhắc trong ứng dụng khi web đang chạy.
- **Chi tiêu thực tế:** ghi/sửa/xóa khoản đã trả, tổng theo ngày/loại và ngân sách còn lại; tách riêng với dự trù và giá yêu cầu đặt.
- **Thông tin dịch vụ:** địa chỉ cụ thể, đơn vị giá, chính sách hủy và tối đa 5 ảnh thực tế. Nội dung/ảnh được công bố sau khi duyệt đúng phiên bản.

Mở từng chuyến đi để vào Hôm nay, Chuẩn bị, Chi tiêu hoặc Chia sẻ / in. Đánh giá nằm trong quản lý đặt dịch vụ; lịch phục vụ nằm trên từng thẻ dịch vụ NCC. Admin tìm kiểm duyệt đánh giá và báo cáo đồng hành trong Duyệt & xác minh. [Thiết kế hành trình 004](docs/HANH_TRINH_MO_RONG_2026-11-05.md), [luồng/API/database và kiểm thử đồng hành 005](docs/KHONG_GIAN_DONG_HANH_2026-11-05.md).

## Chức năng FullText đã triển khai

| SUC | Hành vi chính |
| --- | --- |
| 01.01 | Tra cứu công khai theo tên/mô tả, khu vực động, loại, giá; chi tiết địa điểm kèm dịch vụ liên quan |
| 02.01 | Kế hoạch riêng, hoạt động theo ngày/giờ/thứ tự, cảnh báo giờ và chi phí; sửa nội dung trả về nháp; xác nhận riêng |
| 03.01–03.02 | Tìm nhu cầu còn hiệu lực, lọc và phân trang; xem trước hai phạm vi người xem; công bố/sửa/dừng, che nội dung theo phạm vi chia sẻ |
| 04.01–04.02 | Kết nối có lời nhắn tùy chọn; thông tin liên hệ sau kết nối; thỏa thuận có phiên bản cơ sở, xác nhận đúng phiên bản, thay thế/rút/cùng ngừng và lịch sử |
| 05.01 | Tình huống gắn nhiều hoạt động, phương án và lịch sử, trạng thái xử lý cùng kết quả thực tế |
| 06.01–06.02 | Lưu nháp/gửi dịch vụ, bản cập nhật riêng, thời gian tạm ngừng; Admin duyệt đúng phiên bản, giữ bản công bố đến khi bản mới được duyệt |
| 07.01 | Xem trước yêu cầu và cảnh báo; gửi chống trùng bằng mã lượt; nhà cung cấp đề xuất ngày, số người, số lượng, chi phí và điều kiện để khách xác nhận |
| 08.01 | Sửa/rút yêu cầu đang chờ; thay đổi/hủy trước ngày phục vụ, một đề nghị đang chờ; giữ điều kiện cũ đến khi được chấp nhận |
| 09.01 | Kết quả từng hạng mục, trạng thái tổng hợp; phản ánh trong thời hạn, đối chiếu có nhật ký và bảo toàn kết quả đã chốt |
| 10.01–10.02 | Đăng ký khách/nhà cung cấp với hồ sơ và tệp; bổ sung/nộp lại; quyết định xác minh có lịch sử |
| 11.01 | Đăng nhập email/số điện thoại, phiên trượt 30 phút/tối đa 8 giờ, dấu phiên, đổi mật khẩu Admin, giới hạn 10 lần/phút/IP |
| 12.01 | Hồ sơ riêng cho cả ba vai trò, ảnh đại diện, phạm vi chia sẻ, liên hệ nhà cung cấp có RowVersion riêng |
| 13.01 | Xem hệ quả trước đình chỉ/khôi phục/thu hồi; xử lý đúng các dịch vụ/yêu cầu liên quan, giải trình và nhắc hạn quyết định hiện hành |
| 14.01 | Xác thực lại, liệt kê nghĩa vụ, mã xác nhận 5 phút gắn tài khoản/dấu phiên; kiểm tra lại khi ghi, đồng bộ ngừng và bảo toàn lịch sử |

Không có thanh toán trực tuyến hoặc đặt chỗ tự động bên ngoài phạm vi FullText. Phân quyền cụ thể: [MA_TRAN_QUYEN_SUC.md](docs/MA_TRAN_QUYEN_SUC.md). Điều hướng: [CAU_TRUC_TRANG.md](docs/CAU_TRUC_TRANG.md).

## Giao diện

Giao diện Open Atlas áp dụng ui-ux-pro-max: xanh biển, xanh đậm, nền sáng và điểm nhấn vàng xanh; ảnh du lịch lớn; phân cấp rõ thao tác chính, bộ lọc và trạng thái. Các khu vực cá nhân, đối tác và quản trị giữ bố cục nhất quán. Điện thoại dùng menu thu gọn, biểu mẫu một cột, danh sách đặt dịch vụ dạng thẻ, ô nhập 16 px và vùng chạm rộng.

Quy tắc trình bày: [GIAO_DIEN.md](docs/GIAO_DIEN.md). CSS nền nằm ở `wwwroot/css/experience.css`; các không gian mới dùng thêm `trip-workspace.css`, `product-features.css`, `journey-features.css` và `companion-features.css`. Ảnh và ghi công nằm trong `wwwroot/images/CREDITS.md`. Ảnh giao diện: [khám phá lời rủ](preview/companion-discovery-desktop.png), [nhóm trên máy tính](preview/companion-overview-desktop.png), [nhóm trên điện thoại](preview/companion-overview-mobile.png), [chi phí chung](preview/companion-expenses-desktop.png).

## Kiểm chứng

Các kết quả thực tế và giới hạn kiểm chứng được tổng hợp trong [báo cáo mới nhất](docs/KHONG_GIAN_DONG_HANH_2026-11-05.md) và [hướng dẫn kiểm thử](tests/README.md); kết quả tổng hợp ở `tests/companion-completion-results.json`. Đợt 005 dùng database riêng `HoTroDuLich_Companion_20261105`, cổng 5223, tách ảnh và khóa. Đạt 158 kiểm tra luồng đồng hành, 166 trường hợp biên, 104 quyền SQL, 222 HTML, 324 trình duyệt tại 75 tổ hợp màn hình/kích thước, 2555 hợp đồng C#/API và 2422 SQL trên mỗi bản thử/chính. Các bộ hồi quy 150 journey, 90 product, 82 trip, 102 điều hướng, 41 trang nền đều đạt. Số assertion không phải số yêu cầu thiết kế độc lập. Kết quả các hồ sơ 001–004 được giữ để truy vết.

Các bộ `http-workflows.cjs`, `actor-access.cjs`, `function-pages.cjs` và báo cáo audit ban đầu là tư liệu của phiên bản trước, có kỳ vọng đã được thay đổi bởi FullText; không dùng chúng để chứng nhận bản hiện hành.

## Database và vận hành

Các script nằm ở `src/HoTroDuLich.Web/Database/`:

| Tệp | Mục đích |
| --- | --- |
| `CSDL_HoTroDuLichTuTuc.sql` | Tạo database và thiết lập cấu hình cho môi trường mới |
| `schema.sql` | Lược đồ chuẩn, chạy trên database rỗng |
| `001_CanChinh_FullText.sql` | ALTER database 25 bảng cũ; dừng web và sao lưu trước khi chạy |
| `002_LienKetYeuCauChuyenDi.sql` | Liên kết tùy chọn giữa yêu cầu đặt và chuyến đi |
| `003_ChuanBi_ChiTieu_LuaChon.sql` | Lưu lựa chọn, chuẩn bị, thực chi và metadata/ảnh dịch vụ |
| `004_HanhTrinh_ChiaSe_DanhGia_LichPhucVu.sql` | Thời lượng/tiến độ hoạt động, snapshot chia sẻ, đánh giá và lịch phục vụ |
| `005_KhongGianDongHanh.sql` | Phong cách, nhóm, trao đổi, lịch/bình chọn, việc chuẩn bị, chi phí, điểm hẹn và báo cáo |
| `KiemTra_FullText.sql` | Kiểm tra điều kiện tối thiểu khi triển khai |
| `Quyen_CSDL.sql` | Vai trò ứng dụng tối thiểu quyền, giới hạn DELETE/UPDATE |
| `SaoLuu_PhucHoi.sql` | Các khối mẫu full backup, log backup, CHECKDB và phục hồi tách biệt |

Hướng dẫn và trạng thái triển khai: [VAN_HANH.md](docs/VAN_HANH.md). Bản hiện hành chạy Development trên localhost. Cấu hình Production tắt dữ liệu mẫu, yêu cầu chuỗi kết nối và thông tin Admin ban đầu từ môi trường, bật HTTPS/HSTS/cookie Secure và Windows Event Log. Có thể cấu hình `UploadFiles__ThuMuc` và `DataProtection__ThuMucKhoa`; mặc định cục bộ là `App_Data/uploads` và `App_Data/keys`.
#   K T L N  
 