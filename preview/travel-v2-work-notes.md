# Triển khai quy trình chuyến đi thống nhất — 2026-11-06

Trạng thái: TẠM GÁC PHẠM VI TOÀN HỆ THỐNG theo yêu cầu tập trung mục tiêu đầu tiên ngày 08/11/2026. Chưa cập nhật database chính. Công việc đang làm được theo dõi tại discovery-work-notes.md.

Yêu cầu đã duyệt: khám phá → chọn phương án → lập lịch có mốc cố định → mời/duyệt người đi toàn chuyến hoặc từng hoạt động → đặt dịch vụ → chuẩn bị → sử dụng trong chuyến đi → xử lý thay đổi/sự cố → đối soát chi phí → kết thúc và đánh giá. Quyền quản lý chuyến đi không thay thế quyền của người đặt dịch vụ. Một hoạt động chung chỉ có một bản lịch chính.

Nền hiện tại: extension 005, 46 bảng; dữ liệu cũ phải giữ nguyên. Các báo cáo companion cũ là kết quả bản trước, không phải bằng chứng kiểm thử bản này.

Nguyên tắc: schema SQL 006 bổ sung; backup trước khi sửa; kiểm thử database riêng; không tự công khai dữ liệu cũ; không tự hủy hợp đồng khi hủy chuyến; không ghi khoản hoàn trả thành chi phí; không tự xác nhận có nhân viên tiếp nhận sự cố.

Các bước tiếp theo:
1. Sao lưu source, SQL COPY_ONLY/CHECKSUM và App_Data; lưu dấu vân tay dữ liệu cũ.
2. Thiết kế schema, quyền, trạng thái, API và hàm; triển khai dịch vụ nghiệp vụ.
3. Thiết kế giao diện theo ui-ux-pro-max, gom chức năng theo chuyến đi và theo vai trò.
4. Kiểm thử thực tế liên chức năng, quyền riêng tư, đồng thời, nâng cấp và giao diện.
5. Sao lưu mới, diễn tập nâng cấp, cập nhật database chính và hướng dẫn sử dụng.

Chưa có thay đổi mã tại thời điểm ghi tệp này. Không chạy lại các script restore companion cũ.
