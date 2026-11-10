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
