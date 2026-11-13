# Cấu hình bản đồ và Goong (13/11/2026)

Mọi bản đồ Leaflet trên website (chọn điểm đến, chọn dịch vụ, Khám phá, chi tiết điểm đến) lấy nền qua hàm chung `htBaseMap` trong `wwwroot/js/map-base.js`. Lựa chọn nhà cung cấp, đường dẫn kiểu bản đồ, tên miền cho phép trong CSP, mức phóng nhỏ nhất và khung nhìn nằm tập trung ở `Services/MapSettings.cs` (đăng ký singleton, dùng trong `_Layout.cshtml` và `Program.cs`).

## Khóa cần có

| Khóa (kho bí mật) | Dùng để | Loại khóa Goong |
|---|---|---|
| `Discovery:Goong:MapKey` | Vẽ bản đồ (ảnh vector, biểu tượng, phông chữ) | **Maptiles Key** |
| `Discovery:Goong:RestKey` | Gợi ý địa điểm, đổi vị trí thành tên khu vực (chỉ ở máy chủ) | **API Key (REST)** |
| `Discovery:Map:TrackAsiaKey`, `Discovery:Map:VietMapKey` | Nhà cung cấp dự phòng | |

Hai khóa Goong **không đổi cho nhau được**: khóa REST không vẽ được bản đồ (ảnh trả 403), khóa Maptiles không gọi được REST. Đặt khóa vào kho bí mật của dự án (không ghi vào tệp cấu hình hay git; `UserSecretsId` = `HoTroDuLich.Web.TravelMaps`, chỉ nạp khi chạy môi trường Development):

```bash
dotnet user-secrets set "Discovery:Goong:MapKey" "KHOA_MAPTILES" --project src/HoTroDuLich.Web
dotnet user-secrets set "Discovery:Goong:RestKey" "KHOA_REST" --project src/HoTroDuLich.Web
```

Khi triển khai thật, đặt qua biến môi trường (`Discovery__Goong__MapKey`, `Discovery__Goong__RestKey`). Khóa bản đồ nằm trong mã trang (thẻ `meta`) để trình duyệt tải bản đồ, nên **bắt buộc giới hạn khóa Maptiles theo tên miền** trong trang quản lý Goong (thêm `localhost` khi thử). Khóa REST không bao giờ đưa xuống trình duyệt.

## Chọn nhà cung cấp bản đồ

`Discovery:Map:Provider` (tùy chọn): `Goong`, `TrackAsia`, `VietMap`, hoặc `OSM` (tắt bản đồ vector, dùng ảnh gạch `Discovery:Map:TileUrl`). Để trống hoặc `auto` thì lấy nhà cung cấp đầu tiên có khóa theo thứ tự **Goong, TrackAsia, VietMap**; không có khóa nào thì dùng ảnh gạch cấu hình sẵn (mặc định OpenStreetMap Đức).

Bản đồ vector vẽ bằng thư viện VietMap GL (nền MapLibre) và plugin Leaflet, đặt sẵn trong `wwwroot/lib/vietmap/` (bản CSP, không cần worker từ blob). Nếu trình duyệt không có WebGL, nhà cung cấp trả lỗi 401/403/423/429 hoặc 12 giây vẫn chưa vẽ được ô nào thì tự dùng lại ảnh gạch cấu hình sẵn. `Discovery:Map:Attribution` (tùy chọn) ghi đè dòng ghi nguồn bản đồ (HTML).

### Đặc điểm theo nhà cung cấp

- **Goong:** ô bản đồ chỉ có từ mức phóng 6 (Leaflet), nên mọi bản đồ đặt mức phóng nhỏ nhất là 6 (`MapSettings.MinZoom`, thẻ `ds-map-minzoom`). Đã kiểm tra bằng cách truy vấn các nhãn đang vẽ: vùng Hoàng Sa có "Quần đảo Hoàng Sa", "Đảo Phú Lâm", "Biển Đông"; vùng Trường Sa có các đảo như "đảo Sinh Tồn", "đảo Đá Chữ Thập", "Biển Đông"; không thấy nhãn tiếng Trung. Vì vậy với Goong, khung nhìn mở rộng ra đông (`MapSettings.Bounds` = vĩ độ 5–24, kinh độ 101,5–118,5) để xem được hai quần đảo.
- **TrackAsia:** có từ mức phóng 0; dữ liệu ô có nhãn "Hoàng Sa", "Trường Sa" và không có "Sansha", "Nansha", "Paracel", "Spratly" ở các ô đã kiểm tra.
- **VietMap:** khóa thử nghiệm (11/2026) trả lỗi 423 "Your request is limited" ở phần ảnh bản đồ dù kiểu bản đồ tải được; cần kiểm tra hạn mức/dịch vụ trong trang quản lý VietMap.
- **OpenStreetMap (ảnh gạch):** ở mức phóng 7 vẽ ranh giới hành chính và nhãn "Nansha" của nước khác quanh Trường Sa. Vì vậy với các nền không phải Goong, khung nhìn bị giới hạn trong đất liền và ven biển Việt Nam (kinh độ khoảng 101,5–110,5, vĩ độ 7,5–24).

## Chính sách bảo mật nội dung (CSP)

`Program.cs` thêm tên miền của nhà cung cấp đang dùng (`MapSettings.Origins`) vào `img-src` và `connect-src`: Goong `tiles.goong.io`; TrackAsia `maps.track-asia.com`, `tiles.track-asia.com`; VietMap `maps.vietmap.vn`.

## Goong REST: gợi ý địa điểm và đổi vị trí thành tên khu vực

`Services/GoongPlacesService.cs`, gọi từ máy chủ, có lưu đệm trong bộ nhớ và giới hạn tần suất như API Khám phá:

- `GET /api/lapkehoach/dia-diem?q=`: gợi ý địa điểm khắp Việt Nam (Goong Autocomplete, lưu đệm 30 phút). Hiện trong ô gõ điểm đến ("Địa điểm khác") và cuối danh sách Thêm dịch vụ (thêm như hoạt động tự nhập).
- `GET /api/lapkehoach/dia-chi?lat=&lng=`: đổi vị trí bấm trên bản đồ thành tên khu vực (Goong Geocode, lưu đệm 6 giờ). Chỉ trong khung vĩ độ 5–24, kinh độ 101,5–118,5, ngoài khung trả 422.
- `GET /api/lapkehoach/goong-goi-y?group=&destination=&q=`: địa điểm trực tiếp từ Goong cho nhóm dịch vụ (khách sạn, nhà hàng, khu du lịch, bến xe…) quanh điểm đến: gợi ý (Autocomplete) rồi lấy tọa độ (Place Detail), chỉ giữ địa điểm trong bán kính 30 km quanh điểm đến (xác định bằng Geocode) và xếp theo khoảng cách. Hiện trong hộp Thêm dịch vụ ở mục "Gợi ý từ bản đồ Goong" với ghim màu xanh lá; chọn thì thêm như hoạt động tự nhập (chưa xác nhận giờ và giá). **Không lưu vào cơ sở dữ liệu**, chỉ lưu đệm trong bộ nhớ (30 phút gợi ý, 24 giờ chi tiết và tâm điểm đến).
- Không có khóa REST hoặc Goong lỗi thì các API trả danh sách rỗng, trang vẫn dùng bình thường.
- Tên khu vực của Goong theo địa giới mới (ví dụ "Hội An, Đà Nẵng"), khác hệ cũ trong danh mục địa giới của website nên điểm đến lấy từ Goong không có mã khu vực.

## Vì sao không nhập hàng loạt dữ liệu Goong vào cơ sở dữ liệu

- Chưa tìm được điều khoản API của Goong về việc lưu trữ hoặc sao chép dữ liệu địa điểm (trang điều khoản tìm thấy là thỏa thuận cho ứng dụng điều hướng, không phải API). Khi chưa xác nhận, website chỉ dùng Goong theo kiểu tra cứu trực tiếp và lưu đệm ngắn.
- Dịch vụ (khách sạn, quán ăn, tour) trong website do nhà cung cấp tự đăng, có duyệt và đặt được; địa điểm lấy từ Goong không phải dịch vụ của nhà cung cấp nên không được tạo thành dịch vụ.
- Nếu Goong xác nhận bằng văn bản cho phép lưu trữ, có thể dựng quy trình nhập có kiểm duyệt dựa trên `GoongPlacesService.Nearby`.
