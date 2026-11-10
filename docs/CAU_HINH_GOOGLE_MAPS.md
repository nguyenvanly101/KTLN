# Bản đồ của Hành Trình

Mặc định khi chưa đặt khóa Google Maps trình duyệt, trang **Khám phá** hiển thị OpenStreetMap. Photon tìm địa chỉ và gợi ý địa điểm ở Việt Nam trong tối đa 5 km quanh mốc. OSRM tính tuyến đi bộ, xe đạp và ô tô. Các cơ sở trên Hành Trình vẫn dùng phạm vi bạn chọn. Nhà cung cấp có thể tìm địa chỉ trên bản đồ hoặc đặt ghim, rồi xác nhận vị trí của từng chi nhánh.

Chạy `.\start.ps1` và mở [trang Khám phá](http://localhost:5216/kham-pha) để thử. FOSSGIS cung cấp ảnh nền mặc định và có thể đổi nguồn qua `Discovery:Map:TileUrl`. Các dịch vụ công cộng phù hợp với phát triển và trình diễn, có giới hạn tải và độ phủ. [Nền bản đồ FOSSGIS](https://openstreetmap.de/nutzen/dienste-osm-de/), [Photon](https://github.com/komoot/photon), [OSRM](https://routing.openstreetmap.de/about.html).

Các bước dưới đây dành cho trường hợp muốn bật Google Maps JavaScript, Places API (New) và Routes API.

## 1. Tạo dự án và bật API

1. Mở [Google Cloud Console](https://console.cloud.google.com/), đăng nhập tài khoản của bạn và tạo/chọn một project dành cho website.
2. Liên kết tài khoản Billing cho project nếu dùng đầy đủ Places và Routes. Đây là dịch vụ tính phí theo mức sử dụng; xem mức giá và đặt hạn mức/quota phù hợp trước khi chạy công khai. Hướng dẫn này không tự tạo tài khoản Billing hoặc thực hiện thanh toán.
3. Vào **APIs & Services → Library**, bật đúng ba API: **Maps JavaScript API**, **Places API (New)** và **Routes API**.

Maps Demo Key chỉ hỗ trợ thử một số tính năng Maps JavaScript; không thay hai khóa của tích hợp Places/Routes đầy đủ này.

## 2. Tạo hai API key riêng

Trong **APIs & Services → Credentials → Create credentials → API key**:

### Khóa trình duyệt

- Đặt tên dễ nhận biết, ví dụ `HanhTrinh-Browser`.
- **Application restrictions: Websites / HTTP referrers**.
- Khi chạy cổng mặc định, thêm `http://localhost:5216/*` và `http://127.0.0.1:5216/*`. Nếu đổi cổng, thêm đúng địa chỉ đang chạy. Website công khai cần thêm tên miền HTTPS của bạn.
- **API restrictions: Restrict key → Maps JavaScript API**.
- Đây là khóa dùng để tải bản đồ trong trình duyệt; xuất hiện ở phía trình duyệt là đúng cách tích hợp. Không dùng khóa máy chủ ở đây.

### Khóa máy chủ

- Tạo khóa khác, ví dụ `HanhTrinh-Server`.
- **API restrictions: Restrict key → Places API (New), Routes API**.
- Trên máy chủ triển khai, giới hạn **IP addresses** theo IP đi ra ngoài thực tế của máy chủ. Khóa này gọi REST từ .NET nên không dùng giới hạn HTTP referrer của trình duyệt.
- Khi phát triển bằng IP mạng thay đổi, cấu hình giới hạn IP theo môi trường của bạn và đặt quota thấp để kiểm soát sử dụng.
- Khóa máy chủ chỉ nằm trong cấu hình máy chủ, không nhập vào biểu mẫu cơ sở hay JavaScript.

## 3. Đặt khóa trên máy

Mở PowerShell tại `D:\KLTN`. Thay các phần trong dấu `<...>` bằng khóa của bạn:

```powershell
dotnet user-secrets set "GoogleMaps:BrowserApiKey" "<KHOA_TRINH_DUYET>" --project .\src\HoTroDuLich.Web
dotnet user-secrets set "GoogleMaps:ServerApiKey" "<KHOA_MAY_CHU>" --project .\src\HoTroDuLich.Web
```

Không ghi khóa thật vào `appsettings.json` hoặc gửi kèm source. User secrets là cấu hình phát triển cục bộ; trên môi trường triển khai dùng biến môi trường/secret store:

```text
GoogleMaps__BrowserApiKey
GoogleMaps__ServerApiKey
GoogleMaps__MapId
```

`MapId` mặc định `DEMO_MAP_ID` dùng cho phát triển với Advanced Markers. Khi triển khai chính thức, tạo Map ID loại JavaScript trong Google Maps Platform và cấu hình:

```powershell
dotnet user-secrets set "GoogleMaps:MapId" "<MAP_ID_CUA_BAN>" --project .\src\HoTroDuLich.Web
```

## 4. Khởi động lại và thử

1. Dừng website đang chạy bằng `Ctrl+C` trong terminal đó.
2. Tại `D:\KLTN`, chạy `.\start.ps1`.
3. Mở [màn hình tìm kiếm](http://localhost:5216/kham-pha).
4. Nhập tên một địa điểm cụ thể vào **Bạn muốn tìm gần đâu?**, chọn đúng gợi ý có địa chỉ, chọn **Quán ăn & cà phê** hoặc **Khách sạn & chỗ ở**, rồi bấm **Tìm quanh mốc**.
5. Chọn **Trên bản đồ** để đối chiếu điểm; chọn **Xem đường đến** để thử Routes theo cách di chuyển đã chọn.

## 5. Đăng cơ sở và dịch vụ

1. Đăng nhập tài khoản nhà cung cấp đã xác minh/có quyền hoạt động.
2. Vào **Cơ sở & chi nhánh → Thêm cơ sở**.
3. Điền tên chi nhánh và địa chỉ do bạn cung cấp. Tìm cơ sở trên bản đồ đang dùng hoặc đặt ghim đúng điểm phục vụ. Mỗi quán hoặc chi nhánh có một hồ sơ riêng.
4. Điền tiện ích của đúng cơ sở, căn cứ được quản lý cơ sở, xác nhận vị trí và gửi duyệt.
5. Quản trị viên vào **Xét duyệt cơ sở** để đối chiếu vị trí/quyền quản lý, rồi chấp thuận.
6. Nhà cung cấp vào **Dịch vụ của tôi**, chọn cơ sở áp dụng khi đăng dịch vụ. Giá hoặc điều kiện khác nhau giữa chi nhánh thì đăng dịch vụ riêng.
7. Duyệt dịch vụ; khách sẽ thấy dịch vụ trong đúng cơ sở. Yêu cầu dịch vụ ghi nhận tên/địa chỉ cơ sở lúc gửi để giữ lịch sử.

Quản trị viên tạo điểm tham quan và cửa vào tại `/quan-tri/moc-dia-diem`. Mỗi cửa có ghim riêng và có thể liên kết với địa điểm chính. Không tự lấy tâm một khu tham quan để thay cho tất cả cửa vào.

## Lỗi thường gặp

| Hiện tượng | Kiểm tra |
|---|---|
| Bản đồ chưa sẵn sàng | `GoogleMaps:BrowserApiKey`, API Maps JavaScript, Billing, khóa đúng project |
| Bản đồ có lỗi referrer | HTTP referrers có đúng giao thức, tên máy và cổng đang mở không |
| Có bản đồ nhưng gợi ý/đường đi không hoạt động | `GoogleMaps:ServerApiKey`, Places API (New), Routes API, Billing và giới hạn IP của khóa máy chủ |
| Google báo API chưa được bật | Bật đúng API trong project chứa khóa; chờ cấu hình được áp dụng rồi thử lại |
| Tìm hoặc xem đường quá nhanh trả 429 | Chờ hết cửa sổ giới hạn rồi thử; không tải lại liên tục |
| Cơ sở không xuất hiện cho khách | Đã duyệt cơ sở, cơ sở đang hiện, nhà cung cấp còn quyền hoạt động; dịch vụ cần được duyệt và gắn đúng cơ sở |
| Có Google Maps nhưng chưa có dịch vụ trên website | Địa điểm Google và nội dung dịch vụ của nhà cung cấp là hai nguồn khác nhau; cần đăng và duyệt dịch vụ tại cơ sở |

Khóa trình duyệt bật nền Google Maps và chức năng đặt ghim. Gợi ý địa điểm và tính đường trên Google Maps cần thêm khóa máy chủ. Hồ sơ đã liên kết vị trí Google có thể đối chiếu qua liên kết cũ hoặc xác nhận lại bằng ghim mới trong OpenStreetMap.

Nguồn chính thức: [Maps JavaScript](https://developers.google.com/maps/documentation/javascript/get-api-key), [Places API (New)](https://developers.google.com/maps/documentation/places/web-service/get-api-key), [Routes API](https://developers.google.com/maps/documentation/routes/get-api-key), [Place IDs](https://developers.google.com/maps/documentation/places/web-service/place-id).
