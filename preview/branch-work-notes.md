# Triển khai cơ sở dịch vụ và Google Maps — 08/11/2026

Người dùng đã duyệt sửa code: địa điểm là mốc cụ thể (điểm tham quan/cửa vào/cơ sở), nhà cung cấp có nhiều cơ sở/chi nhánh, mỗi cơ sở gắn Google Maps, dịch vụ áp dụng tại cơ sở được chọn. Không lấy schema cũ làm giới hạn thiết kế. Giữ dữ liệu hiện có khi nâng cấp; chưa tự gán địa chỉ/toạ độ cho dịch vụ cũ.

Người dùng CHƯA CÓ API key Google. Tiếp tục xây và kiểm thử; bổ sung hướng dẫn cấu hình máy. Không tạo dự án trả phí, không giả dữ liệu Google. Phải phân biệt kiểm thử mô phỏng API với gọi Google thật.

Sao lưu trước thay đổi: preview/workflow-backup.json, SQL HoTroDuLich_before_workflow_20261108_151914.bak + ZIP source/App_Data đã xác minh. Descriptor backup006 trước đó lưu preview/discovery-backup-before-branches.json.

Phạm vi: schema007 CoSoDichVu/BanCapNhatCoSo/DichVuCoSo/BanCapNhatDichVuCoSo/MocDiaDiem; duyệt phiên bản cơ sở, kiểm soát sở hữu và quyền NCC; gắn nhiều cơ sở vào phiên bản dịch vụ; màn hình khách tìm quanh mốc + Google Maps + cơ sở/chi nhánh + dịch vụ; mốc/cửa vào quản trị; Place IDs, Nearby, Routes; cấu hình hai khóa browser/server và CSP nonce; hướng dẫn và test API/SQL/quyền.

Google Places: giữ Place ID, không sao chép toàn bộ nội dung Google vào DB. Tên/địa chỉ hồ sơ do NCC cung cấp. Toạ độ Google có hạn sử dụng tối đa29ngày và dọn khi hết hạn; điểm do người dùng đặt ghim có nguồn riêng. Không dùng dữ liệu Google trên Leaflet/OSM. Dữ liệu live cần nguồn và attribution.

Tiến độ cuối: ĐÃ TRIỂN KHAI CODE VÀ DB007. Main HoTroDuLich hiện64bảng/699cột,58bảng trước nâng cấp giữ nguyên SHA/count trong cửa sổ migration; CHECKDB pass. Main chạy bằng start.ps1 ở http://localhost:5216, owned exec session18171. Stage5225 đã dừng session83285. KHÔNG chạy lại deploy007/restore one-shot vào DB đã tồn tại.

Tổng hợp tests/branches-completion-results.json:9báo cáo pass;47 mock/EF contract (có purge-test),1621 legacy contract/model (giữ91route/policy),83features,32edge/SQL/privacy,147HTTP/HTML/asset trên20trang,45main/files. Đây là số assertion, không phải số yêu cầu duy nhất. Upgrade/ populated upgrade và deployment có SHA/count của58bảng. Không có dữ liệu QA trong6bảng mới của main; tệp App_Data giữ nguyên. Build source bình thường0warning0error, start.ps1 chạy thành công.

Backup cuối trước deploy: preview/workflow-backup.json → HoTroDuLich_before_workflow_20261108_213502.bak + ZIP source/App_Data verified. Backup bắt đầu151914 được giữ ở preview/branches-preimplementation-backup.json (cần rollback code thì dùng ZIP source đầu này). Backup006 trước đó preview/discovery-backup-before-branches.json.

Người dùng CHƯA CÓ GOOGLE KEY: đã có code Maps JS/PlacesNew/Routes, cấu hình2key và hướng dẫn docs/CAU_HINH_GOOGLE_MAPS.md. Google live CHƯA BẬT/CHƯA XÁC MINH. Computer Use bị dừng do URL Chrome không xác định, KHÔNG thử bypass; KHÔNG có visual QA/screenshot. Cần báo rõ giới hạn này trong final. Chỉ đọc window/state, không gửi input, không có tab phải đóng.

Tài liệu chính: docs/CO_SO_GOOGLE_MAPS_007.md, docs/CAU_HINH_GOOGLE_MAPS.md, README, tests/README. Triển khai mô hìnhNCC→nhiềucơsở→dịchvụtừngcơsở; duyệt phiên bản, giới hạn sở hữu, publicmốc/cửa, Google PlaceIDcase-sensitive, nháp không chiếm quyền, map/list/nearby/route, compare, snapshot địa chỉ trong yêu cầu. Dịch vụ cũ chưa gắn không tự bịa địa chỉ; NCC có CTA cập nhật. Header public dùng phiên bản đã duyệt, không lấy thời gian sửa nháp làm thời gian cập nhật công khai.

Các ghi chú tiến độ bên dưới là lịch sử, KHÔNG dùng để chạy lại việc đã hoàn thành.

## Trạng thái tiếp tục

- 007 hiện có 6 bảng (thêm YeuCauDatCoSo để giữ tên/địa chỉ chi nhánh trong yêu cầu). 58 bảng cũ không sửa. Generator tools/DefineBranchSchema.cjs tạo SQL/EF/entities/manifest. Pair tọa độ có CHECK chặt, Google coords hết hạn29ngày và cleanup mỗi6h; cache client chỉ tọa độ15phút.
- Backup hiện tại vẫn là bản 151914; main chưa triển khai007. Main cũ session42304 thuộc agent từ006 có thể vẫn chạy5216; không kill process người dùng.
- DB thử mới nhất HoTroDuLich_Branches_20261108_Final: restore từ backup mới, 007 áp dụng2lần,64bảng, toàn bộ58 SHA/count không đổi + CHECKDB pass. tests/branches-upgrade-results.json. DB thử trước HoTroDuLich_Branches_20261108 giữ lại, không ghi đè. Không chạy lại upgrade one-shot nếu tên DB đã tồn tại.
- tools/VerifyBranches: mock Google Places/Autocomplete/Nearby/Routes và EF/SQL metadata. 41checks PASS trong tests/branches-contract-results.json; KHÔNG có key/gọi Google live. CLI đã tạo4 tài khoản thử trong DB Final; preview/branches-fixture.json chứa ID/email (mật khẩu chung BranchTest@2026 của fixtures). Không đổi tài khoản thật/main.
- Server thử F tại http://127.0.0.1:5225, owned exec session60476, preview/branches-build-f, DB Final, DataProtection riêng preview/branches-runtime/keys. Bản G đang build session40147, chứa sửa collation Latin1_General_100_CI_AI, trang policy/notice đối tác. Phải chờ buildG và restart đúng session60476 trước test tiếp.
- tests/branches-features.cjs chạy HTTP/SQL/roles bằng FULLTEXT_TEST_BASE=...5225 và BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final. Đã pass các bước branch create/approve/tenant isolation/private evidence, manualpin, exact entrance, parking scope. Lần mới nhất fail autocomplete do Vietnamese collation không gộp dấu nguyên âm; source đã đổi sang Latin1_General_100_CI_AI + normalize/replace đ. Cần sửa truy vấn gợi ý test dùng tên entrance đầy đủ duy nhất (các fixture lần cũ còn tồn tại, Take5 có thể loại lần mới) và chạy lại sau G.
- Trước đó đã sửa Range(typeof(decimal),"0.1"...) gây500 trong vi-VN ->Range(double). Sửa kiểm tra sở hữu cơ sở trước deriveRegion. ServiceInput.Region giờ nullable/derived từ cơ sở, không bắt khách nhập vùng cũ. Booking preview có BranchVersion; finalsubmit chụp tên/địa chỉ cơ sở vào bảng mới. Chưa kiểm tra xong phần service approval/booking của suite.
- UI mới Views/Places (Index/Branch/Landmark/Compare), Views/Branches (owner/edit/queue/review), admin mốc/cửa; JS google-maps.js/places.js, CSS places.css. Root Explore redirect /kham-pha, bareDiscovery cũng redirect; canonical /thong-tin routes dùng UI mới, các hướng dẫn/entry006 giữ. Canonical91 route signatures giữ. Chỉ link cơ sở đã duyệt thuộc NCC vào phiên bản dịch vụ, công bố link khi duyệt dịch vụ. PublicServices ẩn dịch vụ gắn cơ sở khi tất cả cơ sở ẩn/inactive; dịch vụ cũ chưa gắn giữ nguyên.
- GoogleKey config appsettings trống, csproj UserSecretsId. Program DI/CSP nonce cho Google, rate limit40lookup/30search/12routes; khóa server không vào public config. docs/CAU_HINH_GOOGLE_MAPS.md đã có hướng dẫn tạo2key/billing/3API/localhost/user-secrets. Cần sửa câu start.ps1 bị dư cú pháp ở bước4. Chưa viết docs kiến trúc007/README/testsREADME/deployfinal.
- Computer Use đã DỪNG CHO LƯỢT NÀY vì không xác định chắc URL Chrome. Chỉ list/getwindow/state; KHÔNG gửi input, KHÔNG có tab cần đóng. KHÔNG tiếp tục UI automation hay dùng cách khác vượt chặn. Chưa có xác minh thị giác; cần báo rõ giới hạn cuối. Có thể tiếp tục code/API/SQL độc lập.
- Việc còn lại: buildG, restart stage, suite feature+edge/SQL constraints, sửa lỗi còn lại, kiểm tra service source+booking privacy/branch snapshots, meaningful JS/HTML checks; cuối cùng buildrelease, backupfresh/snapshot main/apply007/hash58row unchanged, runmain5216 và smoke, stopstage, docs/report.
