# Tự kiểm tra khi website không khởi động

Mở PowerShell tại `D:\KLTN`, chạy `./start.ps1`; muốn theo dõi thay đổi dùng `./start.ps1 -Dev`. Dừng bản đang chạy bằng Ctrl+C trước khi khởi động bản khác.

## Sai thư mục hoặc lệnh

Thư mục gốc không có csproj. Dùng `dotnet run --project ./src/HoTroDuLich.Web --launch-profile http`. `dotnet run dev` không bật chế độ phát triển; dùng script `-Dev` hoặc `dotnet watch --project ./src/HoTroDuLich.Web run --launch-profile http`.

## LocalDB chưa chạy

```powershell
SqlLocalDB info MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
SqlLocalDB info MSSQLLocalDB
```

Trạng thái thường cần là Running. Nếu lệnh start báo lỗi, đọc log của instance hiện có tại `%USERPROFILE%\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\error.log`.

`start.ps1` dùng helper `tools/ResolveLocalDb.ps1`: khởi động đúng instance, không xóa/tạo lại. Có trường hợp SQL đã ghi “ready” trong log nhưng thông tin alias chưa cập nhật. Khi đó script chỉ dùng pipe cục bộ trong log sau khi `sqlcmd` thực sự kết nối được bằng xác thực Windows. Pipe chỉ áp dụng cho lần chạy hiện tại, không lưu vào appsettings hay registry. Nếu không xác minh được, script dừng và giữ nguyên dữ liệu.

Không chép một pipe LOCALDB#... cũ vào appsettings: nó thay đổi khi SQL khởi động lại. Nếu thiếu SqlLocalDB/sqlcmd, kiểm tra các thành phần SQL Server Express LocalDB và công cụ dòng lệnh đã cài.

## File hoặc cổng đang được dùng

Thông báo MSB3021/MSB3027 hoặc “address already in use” thường xảy ra khi còn bản web chạy. Quay về terminal của bản đó và nhấn Ctrl+C. Kiểm tra cổng bằng `Get-NetTCPConnection -LocalPort 5216 -State Listen`, xem PID bằng `Get-Process -Id <PID>`, xác nhận đúng tiến trình dự án trước khi dừng. Không dừng tất cả dotnet hoặc SQL Server trên máy.

## Application Control chặn DLL

Build thành công chưa có nghĩa Windows cho phép chạy assembly. Lỗi 0x800711C7 cần kiểm tra chính sách Application Control của máy hoặc liên hệ người quản lý chính sách. Không tự tắt bảo vệ Windows, sửa registry hay tải công cụ để vượt chính sách.

## Khi đã chạy được

Terminal có `Now listening on: http://localhost:5216`; mở địa chỉ đó. Nếu xuất hiện lỗi SQL trong lúc dùng, kiểm tra lại trạng thái instance và log trước khi thao tác với database. Sao lưu database và App_Data trước mọi thay đổi dữ liệu; không xóa instance để chữa lỗi kết nối.

## Bước gán địa giới không còn quá giờ (17/11/2026)

Thủ tục `usp_GanDonViChoKhuVucCu` chạy mỗi lần khởi động. Trước đây nó luôn dựng bảng ứng viên cho toàn bộ khoảng 14.000 đơn vị hành chính rồi sắp xếp, gom nhóm, kể cả khi không còn bản ghi nào cần gán. Khi máy thiếu bộ nhớ, bước này quá 30 giây và bị bỏ qua.

Đã sửa trong `Database/008b_GanDonViChoKhuVucCu.sql`:

- **Thoát sớm** khi không còn bản ghi nào cần xử lý (chưa có đơn vị, khác "Việt Nam", chưa nằm trong hàng chờ). Đây là trường hợp thường gặp; chạy khoảng 0,1 đến 0,2 giây.
- **Chỉ xét các tên khu vực đang chờ gán** thay vì toàn bộ đơn vị. Khi có một bản ghi cần gán, thủ tục chạy khoảng 0,6 giây trên cơ sở dữ liệu thử; cách cũ chạy hơn 120 giây vẫn chưa xong.

Quy tắc gán không đổi: khớp đúng một đơn vị thì tự gán, không khớp hoặc khớp nhiều thì vào hàng chờ.

Trên cơ sở dữ liệu thật, hai lần bị bỏ qua không làm thiếu dữ liệu: chỉ có 4 mục Khám phá chưa có đơn vị, đều là bài hướng dẫn chung ghi khu vực "Việt Nam" mà thủ tục cố ý bỏ qua.
