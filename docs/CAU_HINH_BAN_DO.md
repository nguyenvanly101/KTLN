# Cấu hình bản đồ (13/11/2026)

Mọi bản đồ Leaflet trên website (chọn điểm đến, chọn dịch vụ, Khám phá, chi tiết điểm đến) lấy nền qua một hàm chung `htBaseMap` trong `wwwroot/js/map-base.js`.

## Thứ tự chọn nền bản đồ

1. **TrackAsia** (nếu có `Discovery:Map:TrackAsiaKey`): bản đồ vector `https://maps.track-asia.com/styles/v2/streets.json`.
2. **Goong** (nếu có `Discovery:Map:GoongKey`, khóa **Maptiles**): `https://tiles.goong.io/assets/goong_map_web.json`.
3. **VietMap** (nếu có `Discovery:Map:VietMapKey`, kiểu `Discovery:Map:VietMapStyle`, mặc định `tm`).
4. **Ảnh bản đồ cấu hình sẵn** `Discovery:Map:TileUrl` (mặc định OpenStreetMap Đức).

Bản đồ vector vẽ bằng thư viện VietMap GL (nền MapLibre) và plugin Leaflet, đã đặt sẵn trong `wwwroot/lib/vietmap/` (bản CSP, không cần worker từ blob). Nếu trình duyệt không có WebGL, nhà cung cấp trả lỗi 401/403/423/429 hoặc 12 giây vẫn chưa vẽ được ô nào thì tự dùng lại nền ở mục 4.

`Discovery:Map:Attribution` (tùy chọn) ghi đè dòng ghi nguồn bản đồ (HTML).

## Đặt khóa

Khóa lưu trong kho bí mật của dự án, **không** ghi vào tệp cấu hình hay git (`UserSecretsId` = `HoTroDuLich.Web.TravelMaps`; chỉ nạp khi chạy môi trường Development):

```bash
dotnet user-secrets set "Discovery:Map:TrackAsiaKey" "KHOA" --project src/HoTroDuLich.Web
```

Khi triển khai thật, đặt qua biến môi trường `Discovery__Map__TrackAsiaKey`. Khóa bản đồ được đưa vào trang (thẻ `meta`) để trình duyệt gọi ảnh bản đồ, nên **bắt buộc giới hạn khóa theo tên miền** trong trang quản lý của nhà cung cấp (thêm `localhost` khi thử).

## Chính sách bảo mật nội dung (CSP)

`Program.cs` tự thêm tên miền của nhà cung cấp vào `img-src` và `connect-src` khi khóa tương ứng được cấu hình (TrackAsia: `maps.track-asia.com`, `tiles.track-asia.com`; Goong: `tiles.goong.io`, `*.goong.io`; VietMap: `maps.vietmap.vn`).

## Khung nhìn giới hạn

Các bản đồ bị giới hạn trong phần đất liền và ven biển Việt Nam (kinh độ khoảng 101,5–110,5, vĩ độ 7,5–24) để người dùng không kéo ra vùng quần đảo ngoài khơi, nơi dữ liệu OpenStreetMap vẽ ranh giới hành chính và nhãn của nước khác (ví dụ "Nansha" ở mức phóng 7). Đây chỉ giảm rủi ro khi dùng nền OpenStreetMap. Dữ liệu ô vector TrackAsia có nhãn "Hoàng Sa", "Trường Sa" và không có nhãn "Sansha", "Nansha", "Paracel", "Spratly" ở các ô đã kiểm tra; chưa kiểm tra bằng mắt hình vẽ ở mọi mức phóng.

## Ghi chú vận hành

- VietMap: khóa thử nghiệm (11/2026) trả lỗi 423 "Your request is limited" ở phần ảnh bản đồ dù kiểu bản đồ tải được; cần kiểm tra hạn mức/dịch vụ trong trang quản lý VietMap.
- Goong: trang tạo khóa lỗi (`script.js` trả về HTML) vào ngày 13/11/2026.
- Không có hai nhà cung cấp cùng lúc: nhà cung cấp đứng trước trong danh sách thắng.
