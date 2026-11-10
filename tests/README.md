# Kiểm thử cơ sở và Google Maps — 007

Bản hiện hành dùng64bảng, giữ nguyên58bảng trước007 và91đường dẫn FullText. Các mục005/006 bên dưới là lịch sử.

Ca ghi chạy riêng ở `HoTroDuLich_Branches_20261108_Final`, cổng5225. `tests/branches-features.cjs` kiểm tra hồ sơ nhiều cơ sở, quyền sở hữu, xét duyệt, tìm quanh cửa và địa chỉ trong yêu cầu. `tests/branches-edge.cjs` kiểm tra biên/SQL/quyền riêng tư. Đặt `FULLTEXT_TEST_BASE=http://127.0.0.1:5225` và `BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final` khi chạy. Fixture tài khoản nằm trong `preview/branches-fixture.json`; chỉ tạo bằng `VerifyBranches --fixtures` khi `BRANCH_TEST_CONNECTION` trỏ DB thử có prefix cho phép. Không chạy fixture hoặc ca ghi vào DB chính.

`tools/VerifyBranches` kiểm tra hợp đồng Google bằng HttpMessageHandler mô phỏng, mapping và collation. Chạy DLL từ thư mục build với cwd D:/KLTN. Không có key nên báo cáo `liveGoogle=false`; chưa xác nhận Maps/Places/Routes live. Computer Use bị dừng do không xác định URL nên không có bằng chứng kiểm thử trực quan.

`branches-upgrade.cjs` là diễn tập **một lần cho DB mới** từ backup được xác minh; từ chối ghi đè DB/file dữ liệu đã có. Script SQL007 chạy lặp an toàn. Kết quả row count/SHA của58 bảng và CHECKDB nằm trong báo cáo upgrade.

[Hướng dẫn vận hành007](../docs/CO_SO_GOOGLE_MAPS_007.md) · [Cấu hình Google](../docs/CAU_HINH_GOOGLE_MAPS.md).

# Kiểm thử phiên bản FullText

## Hồ sơ companion hiện hành — 005, hoàn thiện 06/11/2026

[Thiết kế, bảng kết quả và vận hành](../docs/KHONG_GIAN_DONG_HANH_2026-11-05.md). Tổng hiện hành 46 bảng/473 cột; giữ 91 action FullText và thêm 35 endpoint đồng hành. Các hồ sơ 004 trở về trước dưới đây là mốc lịch sử, không dùng kỳ vọng 31 bảng để kiểm tra bản mới.

Các ca ghi chỉ dùng `HoTroDuLich_Companion_20261105`, cổng 5223; khóa và tệp tải lên tách dưới preview. Không trỏ ca nghiệp vụ vào database chính. Dừng server thử trước khi ghi đè DLL.

```powershell
dotnet build .\src\HoTroDuLich.Web --no-restore -o .\preview\companion-build
dotnet run --project .\tools\VerifyFullText --no-restore -- D:/KLTN --companion-extension
```

Chạy server trong terminal riêng:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__HoTroDuLich='Server=(localdb)\MSSQLLocalDB;Database=HoTroDuLich_Companion_20261105;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
$env:DataProtection__ThuMucKhoa='D:\KLTN\preview\companion-test-keys'
$env:UploadFiles__ThuMuc='D:\KLTN\preview\companion-test-uploads'
dotnet .\preview\companion-build\HoTroDuLich.Web.dll --contentRoot D:\KLTN\src\HoTroDuLich.Web --urls http://localhost:5223
```

```powershell
node .\tests\companion-features.cjs
# Chờ cửa sổ giới hạn đăng nhập mới trước mỗi bộ tiếp theo.
node .\tests\companion-edge-cases.cjs
node .\tests\companion-pages.cjs
node .\tests\companion-database-permissions.cjs
$env:TRAVEL_COMPANION_EXTENSION='1'
$env:TRAVEL_SCHEMA_DB='HoTroDuLich_Companion_20261105'
$env:TRAVEL_SCHEMA_RESULT='tests/companion-schema-results.json'
node .\tests\fulltext-schema.cjs
node .\tests\journey-features.cjs
node .\tests\product-features.cjs
$env:FULLTEXT_TEST_BASE='http://localhost:5223'
$env:TRAVEL_WORKFLOW_DB='HoTroDuLich_Companion_20261105'
$env:TRAVEL_WORKFLOW_RESULT='tests/companion-trip-regression-results.json'
node .\tests\trip-workflow.cjs
$env:UI_TEST_BASE='http://localhost:5223'
$env:UI_ADMIN_PASSWORD='HanhTrinh@2026'
$env:UI_TEST_RESULT='tests/companion-navigation-results.json'
node .\tests\navigation-redesign.cjs
$env:FULLTEXT_TEST_RESULT='tests/companion-smoke-results.json'
node .\tests\fulltext-smoke.cjs
```

Giữ 10 lần đăng nhập/phút/IP, không chạy đồng thời các bộ có đăng nhập. Ca chính/biên tạo tài khoản fixture và có giới hạn nghiệp vụ; không dùng cùng database vô hạn mà không theo dõi dữ liệu đã tạo. `companion-pages` kiểm tra HTTP/HTML; `companion-browser-results.json` ghi kiểm tra tương tác/bố cục bằng công cụ trình duyệt, không thể tái tạo bằng ca HTML.

Đã đạt: 158 luồng, 166 biên, 104 quyền SQL, 150 journey, 90 product, 82 trip, 102 điều hướng, 41 smoke, 222 HTML, 324 trình duyệt, 2555 hợp đồng, 2422 SQL thử và 2422 SQL chính. Thử nâng cấp cuối/áp dụng chính 169 assertion; sao lưu App_Data 22; khởi động thật hai chế độ 14. Tệp JSON và ảnh nằm trong tests/preview.

`companion-rehearsal.cjs` và `companion-upgrade.cjs` là bằng chứng của đợt nâng cấp, không phải script reset. Chúng từ chối phục hồi khi tên đích đã tồn tại và không phục hồi đè bản chính. Không chạy lại `--apply` khi main đã có 005. Script SQL 005 tự chạy lại được; trên database hiện có, quy trình nâng cấp phải tạo backup và đối chiếu riêng trước.

## Mốc hồ sơ journey — 004, ngày 05/11/2026

[Thiết kế và bàn giao](../docs/HANH_TRINH_MO_RONG_2026-11-05.md). Hồ sơ journey bao gồm nền + 002 + 003 + 004: 31 bảng/335 cột, 91 action chuẩn giữ nguyên và 19 điểm vào bổ sung của đợt 004. Các mục product/trip/nền phía dưới là mốc lịch sử; dùng đúng hồ sơ để so lược đồ hiện hành.

Ca ghi chỉ chạy trên **HoTroDuLich_Journey_20261105**, cổng **5222**, tách khóa/tệp. Chuẩn bị database bằng phục hồi vào tên và MDF/LDF riêng, không phục hồi đè bản chính. Server phải dừng trước khi ghi đè DLL:

```powershell
dotnet build .\src\HoTroDuLich.Web --no-restore -o .\preview\journey-build /p:UseAppHost=false
dotnet build .\tools\VerifyFullText --no-restore /p:UseAppHost=false
dotnet .\tools\VerifyFullText\bin\Debug\net10.0\VerifyFullText.dll D:/KLTN --journey-extension
```

Server thử trong terminal riêng:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__HoTroDuLich='Server=(localdb)\MSSQLLocalDB;Database=HoTroDuLich_Journey_20261105;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
$env:DataProtection__ThuMucKhoa='D:\KLTN\preview\journey-test-keys'
$env:UploadFiles__ThuMuc='D:\KLTN\preview\journey-test-uploads'
dotnet .\preview\journey-build\HoTroDuLich.Web.dll --contentRoot D:\KLTN\src\HoTroDuLich.Web --urls http://localhost:5222
```

Chạy từng bộ ở terminal kiểm thử, chờ cửa sổ giới hạn xác thực mới giữa các bộ đăng nhập nhiều:

```powershell
node .\tests\journey-features.cjs
node .\tests\journey-database-permissions.cjs
$env:FULLTEXT_TEST_BASE='http://localhost:5222'
$env:TRAVEL_WORKFLOW_DB='HoTroDuLich_Journey_20261105'
$env:TRAVEL_WORKFLOW_RESULT='tests/journey-trip-regression-results.json'
node .\tests\trip-workflow.cjs
$env:UI_TEST_BASE='http://localhost:5222'
$env:UI_ADMIN_PASSWORD='HanhTrinh@2026'
$env:UI_TEST_RESULT='tests/journey-navigation-results.json'
node .\tests\navigation-redesign.cjs
$env:FULLTEXT_TEST_RESULT='tests/journey-smoke-results.json'
node .\tests\fulltext-smoke.cjs
$env:TRAVEL_JOURNEY_EXTENSION='1'
node .\tests\product-features.cjs
$env:TRAVEL_SCHEMA_DB='HoTroDuLich_Journey_20261105'
$env:TRAVEL_SCHEMA_RESULT='tests/journey-schema-results.json'
node .\tests\fulltext-schema.cjs
```

Giữ giới hạn **10 lần xác thực/phút/IP**. Mật khẩu ở đây là của bản sao Journey đã dùng trong đợt này; dùng thông tin đúng của bản sao khi phục hồi nguồn khác. Không dùng test để đổi lại mật khẩu hay dữ liệu tài khoản chính. Xóa biến môi trường thử hoặc mở terminal mới trước khi chạy bản chính.

Kết quả: 150 chức năng mới, 1986 hợp đồng, 1778 SQL trên từng bản thử/chính, 29 quyền SQL, 82 luồng chuyến đi, 102 điều hướng, 41 trang và 90 hồi quy phần 003. UI: 174 assertion tại 75 tổ hợp/15 màn hình qua trình duyệt thật. Diễn tập 58, nâng cấp chính 67, App_Data 22 và khởi động/bàn giao 18. Giới hạn và bằng chứng từng bộ được ghi trong báo cáo liên kết trên. `journey-live-data-observation.json` chỉ là quan sát dữ liệu lúc phiên sử dụng tiếp theo đã mở, không phải bộ hồi quy hay bằng chứng thay đổi do migration.

`tools/UpgradeJourney.cjs --apply` là công cụ triển khai một lần trên main 28/302: cần dừng web/sao lưu App_Data trước, tạo SQL backup VERIFYONLY rồi áp dụng và so dữ liệu. Nó từ chối bản 31/335 đã nâng cấp; không phải lệnh chạy kiểm thử hằng ngày.

## Mốc phần mở rộng sản phẩm — 003, ngày 05/11/2026

[Thiết kế, kết quả và sao lưu](../docs/MO_RONG_SAN_PHAM_2026-11-05.md). Mốc này dùng hồ sơ **product**, bao gồm liên kết chuyến đi 002 và phần mở rộng 003. Các hồ sơ này có số lượng lược đồ thấp hơn journey hiện hành và được giữ để truy vết.

Các ca ghi mới chỉ dùng `HoTroDuLich_Product_20261105`, cổng **5221**. Không cấu hình server này trỏ vào database chính. Khóa và ảnh phải tách riêng:

```powershell
dotnet build .\src\HoTroDuLich.Web --no-restore -o .\preview\product-build /p:UseAppHost=false
dotnet build .\tools\VerifyFullText --no-restore /p:UseAppHost=false
dotnet .\tools\VerifyFullText\bin\Debug\net10.0\VerifyFullText.dll D:/KLTN --product-extension
```

Khởi động server thử trong terminal riêng:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__HoTroDuLich='Server=(localdb)\MSSQLLocalDB;Database=HoTroDuLich_Product_20261105;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
$env:DataProtection__ThuMucKhoa='D:\KLTN\preview\product-test-keys'
$env:UploadFiles__ThuMuc='D:\KLTN\preview\product-test-uploads'
dotnet .\preview\product-build\HoTroDuLich.Web.dll --contentRoot D:\KLTN\src\HoTroDuLich.Web --urls http://localhost:5221
```

Ở terminal chạy kiểm tra:

```powershell
node .\tests\product-features.cjs
$env:FULLTEXT_TEST_BASE='http://localhost:5221'
$env:TRAVEL_WORKFLOW_DB='HoTroDuLich_Product_20261105'
node .\tests\trip-workflow.cjs
$env:TRAVEL_PRODUCT_EXTENSION='1'
$env:TRAVEL_SCHEMA_DB='HoTroDuLich_Product_20261105'
$env:TRAVEL_SCHEMA_RESULT='tests/product-schema-results.json'
node .\tests\fulltext-schema.cjs
node .\tests\product-database-permissions.cjs
```

Các lệnh trên là từng bộ độc lập. Chờ sang cửa sổ giới hạn đăng nhập mới giữa các bộ có nhiều lần đăng nhập; giữ nguyên 10 lần/phút/IP. `product-features.cjs` dùng 7 lượt xác thực, luồng chuyến đi 5, smoke 4–5 và điều hướng 4. Không ghi đè DLL khi server thử đang chạy. Xóa các biến môi trường thử nghiệm hoặc mở terminal mới trước khi chạy bản chính.

Kết quả: 90 chức năng, 82 luồng chuyến đi, 102 điều hướng, 41 trang theo vai trò, 108 trình duyệt, 1807 hợp đồng, 1629 SQL trên mỗi database thử/chính. Thêm 22 kiểm tra quyền SQL, 3 kiểm tra nhắc hạn qua hai lần khởi động lại, 80 kiểm tra phục hồi/nâng cấp, 63 đối chiếu nâng cấp chính và 14 khởi động hai chế độ. Kết quả và giới hạn được mô tả theo từng bộ trong báo cáo thiết kế.

`preparation-reminder.cjs` chụp trước/sau khi khởi động lại server thử bằng ba pha `before`, `first-restart`, `second-restart`; dùng fixture từ bộ chức năng và xác nhận thông báo chỉ có một. `product-upgrade-rehearsal.cjs` là bằng chứng thử phục hồi của đợt này: đích cố định riêng và từ chối ghi đè database đã tồn tại. Các công cụ ghi dữ liệu/cấp quyền thử chỉ được chạy ở môi trường thử đã nêu. Kiểm tra schema trên bản chính chỉ đọc metadata và tạo bảng tạm trong phiên kết nối.

## Mốc lịch sử liên kết chuyến đi — 05/11/2026

Thiết kế bổ sung và kết quả: [LUONG_CHUYEN_DI_2026-11-05.md](../docs/LUONG_CHUYEN_DI_2026-11-05.md). Bộ `trip-workflow.cjs` chạy tại `http://localhost:5219` và dùng database riêng `HoTroDuLich_TripFlow_20261104`. Nó tạo tài khoản/chuyến đi/yêu cầu thử; chỉ chạy khi đã cấu hình đúng server thử nghiệm dưới đây.

```powershell
dotnet build .\src\HoTroDuLich.Web --no-restore -o .\preview\trip-build /p:UseAppHost=false
dotnet build .\tools\VerifyFullText --no-restore /p:UseAppHost=false
dotnet .\tools\VerifyFullText\bin\Debug\net10.0\VerifyFullText.dll D:/KLTN --trip-extension
```

Khởi động server thử nghiệm ở terminal riêng:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__HoTroDuLich='Server=(localdb)\MSSQLLocalDB;Database=HoTroDuLich_TripFlow_20261104;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
$env:DataProtection__ThuMucKhoa='D:\KLTN\preview\trip-test-keys'
dotnet .\preview\trip-build\HoTroDuLich.Web.dll --contentRoot D:\KLTN\src\HoTroDuLich.Web --urls http://localhost:5219
```

```powershell
node .\tests\trip-workflow.cjs
$env:TRAVEL_TRIP_EXTENSION='1'
$env:TRAVEL_SCHEMA_DB='HoTroDuLich_TripFlow_20261104'
$env:TRAVEL_SCHEMA_RESULT='tests/trip-schema-results.json'
node .\tests\fulltext-schema.cjs
```

Kết quả ở mốc trip: 82 kiểm tra luồng, 1700 kiểm tra hợp đồng, 1479 kiểm tra lược đồ, 102 kiểm tra điều hướng, 41 kiểm tra trang theo vai trò và 13 kiểm tra trình duyệt bổ sung. Giữ giới hạn đăng nhập 10 lần/phút khi chạy nhiều bộ. Dừng server thử nghiệm trước khi ghi đè DLL đang chạy.

Phần dưới lưu quy trình/kết quả của bản nền FullText ngày 04/11. Bộ luồng cũ có một số kỳ vọng bố cục lịch trình trước khi tạo trung tâm chuyến đi; dùng các bộ và hồ sơ mở rộng phía trên để đánh giá thay đổi hiện hành.

Chạy từ `D:\KLTN`. Các ca HTTP mặc định chỉ kết nối `http://localhost:5219`, database `HoTroDuLich_FullText_20261104`. Không đổi chúng sang database chính. Bộ nghiệp vụ tạo dữ liệu mới và thay đổi trạng thái của các tài khoản thử nghiệm.

## Kết quả ngày 04/11/2026

| Bộ kiểm tra | Số kiểm tra đạt | Phạm vi |
| --- | ---: | --- |
| `fulltext-contract-results.json` | 1697 | C# entity/enum, EF, 43 Controller, 91 route/verb/policy, ViewModel và tên phương thức Service |
| `fulltext-main-schema-results.json` | 1472 | Lược đồ vật lý database chính: kiểu/null/default, PK/FK, CHECK, khóa/INCLUDE/lọc/thứ tự của index, 4 view, cấu hình SQL |
| `fulltext-workflow-results.json` | 169 | HTTP xuyên suốt 18 SUC, kiểm tra dữ liệu sau ghi, phiên bản, giao dịch, quyền riêng tư, gửi lặp và thao tác đồng thời |
| `fulltext-access-results.json` | 364 | 91 action × khách vãng lai/khách du lịch/NCC/Admin; quyền route, CSRF; chấp nhận 429 đúng ở route xác thực |
| `fulltext-smoke-results.json` | 41 | Trang chính theo từng vai trò và các trường hợp sai vai trò |
| `fulltext-ui-flow-results.json` | 35 | Nút theo vai trò, biểu mẫu đăng nhập/nhu cầu/kế hoạch/thông báo và giữ tiêu chí tìm kiếm |
| `fulltext-security-results.json` | 23 | Nhật ký độc lập, che định danh, quyền/CSRF, HTTP headers, 10 lần đăng nhập/phút/IP, đăng xuất |

Số lượng kiểm tra là số assertion của từng bộ, không phải số yêu cầu thiết kế độc lập. Ma trận truy cập cố ý không gửi CSRF ở POST nên không thay thế kiểm thử nghiệp vụ thành công. Kiểm tra cấu trúc không chứng minh mọi tổ hợp dữ liệu thực tế đều đúng.

## Chuẩn bị và chạy

Biên dịch bản dùng cho kiểm chứng hợp đồng:

```powershell
dotnet build .\src\HoTroDuLich.Web --no-restore -o .\preview\final-build /p:UseAppHost=false
dotnet build .\tools\VerifyFullText --no-restore /p:UseAppHost=false
dotnet .\tools\VerifyFullText\bin\Debug\net10.0\VerifyFullText.dll D:/KLTN
```

Khởi động server thử nghiệm ở terminal riêng:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__HoTroDuLich='Server=(localdb)\MSSQLLocalDB;Database=HoTroDuLich_FullText_20261104;Trusted_Connection=True;TrustServerCertificate=True'
dotnet .\preview\final-build\HoTroDuLich.Web.dll --contentRoot D:\KLTN\src\HoTroDuLich.Web --urls http://localhost:5219
```

```powershell
node .\tests\fulltext-workflows.cjs
node .\tests\fulltext-ui-flows.cjs
node .\tests\fulltext-smoke.cjs
node .\tests\fulltext-access.cjs
```

Chờ cửa sổ giới hạn đăng nhập mới giữa các bộ. Giới hạn này được giữ trong môi trường thử nghiệm. `fulltext-security.cjs` cần server vừa khởi động vì xác nhận chính xác lần thứ 11. Admin mới ở database thử nghiệm phải đổi mật khẩu lần đầu; helper của bộ smoke/workflow xử lý bước này. Các bộ access/security/UI dùng mật khẩu Admin thử nghiệm sau bước đổi là `HanhTrinhMoi@2026`.

Kiểm tra database chính chỉ đọc metadata và tạo bảng `#temp` rỗng để SQL Server chuẩn hóa biểu thức CHECK:

```powershell
$env:TRAVEL_SCHEMA_DB='HoTroDuLich'
$env:TRAVEL_SCHEMA_RESULT='tests/fulltext-main-schema-results.json'
node .\tests\fulltext-schema.cjs
```

`fulltext-constraint-probe.sql` được sinh bởi bộ kiểm tra trên; bảng tạm tự mất khi kết thúc kết nối. 41 CHECK liên thuộc tính và 18 unique index trong `docs/fulltext-constraints.json` được trích độc lập từ Bảng 4.161–4.162. Các định nghĩa miền còn lại được so sánh với script SQL chuẩn đã rà soát.

## Các bộ cũ

`http-workflows.cjs`, `actor-access.cjs`, `function-pages.cjs` và các kết quả trước đợt FullText giữ lại để truy vết. Chúng có kỳ vọng cũ về quyền tra cứu, hồ sơ Admin và nghiệp vụ. Không dùng các bộ cũ làm tiêu chuẩn chấp nhận phiên bản hiện hành.

Bộ hiện hành dùng kiểm thử HTTP với SQL Server thật, một chương trình kiểm chứng C# và truy vấn metadata SQL. Dự án chưa chuyển các bộ này sang xUnit/WebApplicationFactory như công cụ minh họa trong tài liệu.
