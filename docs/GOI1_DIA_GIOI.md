# Gói 1 — Địa giới hành chính

Trạng thái: **dữ liệu đã dựng và kiểm thử trên database tạm (đã xóa). Chưa áp dụng vào database chính `HoTroDuLich`, chưa sửa mã C#.**

## 1. Nguồn và dữ liệu đã dựng

Nguồn: bảng chuyển đổi đơn vị hành chính mới–cũ của Tổng cục Thống kê (NSO, cổng `danhmuchanhchinh.nso.gov.vn`, tệp cập nhật 23/10/2025), lưu tại `data/dia-gioi/nguon/`. Công cụ dựng: `python tools/BuildDiaGioi.py` (chỉ dùng thư viện chuẩn, chạy lại được).

| Hệ | Nội dung | Số lượng |
|---|---|---|
| **Cũ** (trước 01/07/2025) | 63 tỉnh/thành → quận/huyện/thị xã → xã/phường/thị trấn | 63 / 696 / 10.035 |
| **Mới** (từ 01/07/2025) | 34 tỉnh/thành → xã/phường/đặc khu (không còn cấp huyện) | 34 / 3.321 |
| **Ánh xạ cũ → mới** | Mỗi xã cũ nhập toàn bộ/một phần vào xã mới nào | 10.571 cặp |

Kiểm tra: cây cha–con đúng cấp, mọi xã cũ đều có ánh xạ, mọi xã mới đều có nguồn cũ. Ví dụ: Đà Nẵng (cũ) có 8 quận/huyện; Đà Nẵng (mới, gồm Quảng Nam cũ) có 94 phường/xã/đặc khu.

Tệp sinh ra: `data/dia-gioi/dvhc_cu.csv`, `dvhc_moi.csv`, `chuyen_doi.csv`, `bao_cao_lam_sach.txt`, và `Database/008c_NapDiaGioi.sql`.

## 2. Lỗi trong nguồn đã xử lý (chi tiết ở `bao_cao_lam_sach.txt`)

| Lỗi | Cách xử lý |
|---|---|
| Khoảng trắng thừa, chữ hoa/thường lẫn, ký tự Unicode tổ hợp, thiếu dấu ("Xa Ea Kly", "Thi trấn…") | Chuẩn hóa NFC, nhận diện tiền tố không phân biệt hoa/thường/dấu |
| 1 xã mới có 2 cách viết ("Điềm Thụy"/"Điềm Thuỵ") | Chọn cách viết phổ biến nhất |
| Huyện Cồn Cỏ ghi nhầm tỉnh "Quảng Trị (44)" | Sửa thành Quảng Trị (45) |
| 2 huyện có 1 dòng ghi sai tỉnh (Thành phố Tân An, Thành phố Hà Tiên) | Chọn tỉnh theo đa số dòng |
| 3 cặp xã cũ bị gõ trùng mã (Cổ Bi/Đặng Xá; Hồng Quang/Mỹ Hưng; Công Đa/Phú Thịnh) | Đặng Xá tự xác định được (00556). **Mỹ Hưng và Phú Thịnh đang dùng mã tạm `01615X`, `02485X`, cần đối chiếu với danh mục gốc** |
| 2 xã cũ ở Phú Yên thiếu tiền tố loại (Xuân Quang 3, Xuân Phước) | Mặc định "Xã", ghi trong báo cáo |

## 3. Thiết kế dữ liệu (script `008`)

| Bảng | Vai trò |
|---|---|
| `DonViHanhChinh` | Cả hai hệ trong một bảng, phân biệt bằng `Regime` (`Cu`/`Moi`). `Level`: 1 tỉnh, 2 huyện (chỉ hệ Cũ), 3 xã. Mã nguồn duy nhất theo (`Regime`,`Code`) |
| `DonViChuyenDoi` | Ánh xạ xã cũ → xã mới (nhiều–nhiều) kèm ghi chú "Nhập toàn bộ/một phần" |
| `TenGoiKhacDonVi` | Tên gọi khác, viết tắt, tên nước ngoài ("Danang", "TP.HCM"). Chưa nạp dữ liệu |
| `KhuDuLich` + `KhuDuLichDonVi` | Lớp chồng: Bán đảo Sơn Trà, Phố cổ Hội An… Chưa nạp dữ liệu |
| `ChuyenDoiDiaGioiCho` | Hàng chờ cho khu vực cũ không tự gán được |
| Cột `DonViId` (NULL) | Thêm vào 7 bảng đang dùng khu vực dạng chuỗi; cột chuỗi cũ được giữ nguyên |

`Status` của đơn vị nghĩa là "hệ thống có dùng để chọn hay không", không phải trạng thái pháp lý (pháp lý nằm ở `EffectiveFrom/EffectiveTo`: hệ Cũ kết thúc 30/06/2025, hệ Mới bắt đầu 01/07/2025).

## 4. Chuyển dữ liệu khu vực cũ (`008b`, `usp_GanDonViChoKhuVucCu @Regime`)

`@Regime` chọn hệ dùng làm gốc (mặc định `Cu`, theo yêu cầu hiện tại). So khớp tên hoặc tên đầy đủ, không phân biệt hoa/thường/dấu:
- Khớp đúng một đơn vị → tự gán `DonViId`. Tên trùng nhiều cấp (Bắc Giang vừa là tỉnh vừa là thành phố) → lấy cấp cao nhất nếu cấp đó chỉ có một đơn vị.
- Khớp nhiều đơn vị ("Phường 1") → hàng chờ `Ambiguous`. Không khớp → hàng chờ `NoMatch`. "Việt Nam" bị bỏ qua.
- Chạy lại được; chỉ xử lý bản ghi còn `DonViId IS NULL`.

Kết quả thử: "Đà Nẵng", "da nang", "Thành phố Đà Nẵng" → tỉnh; "Quận Hải Châu", "Hải Châu" → quận; "Bắc Giang" → tỉnh; "Phường 1" → Ambiguous; "Xứ lạ" → NoMatch.

## 5. Quyết định thiết kế cần biết

- **Hệ nào làm gốc?** Bạn yêu cầu 63 tỉnh cũ nên mặc định `Cu`. Vì địa chỉ hợp lệ từ 01/07/2025 theo hệ Mới, hệ Mới vẫn được nạp để đổi gốc về sau qua bảng ánh xạ.
- Ánh xạ cũ → mới là **nhiều–nhiều** (một xã cũ có thể tách vào nhiều xã mới), nên không thể đổi `DonViId` hàng loạt bằng một phép gán đơn giản; cần quy tắc cho trường hợp "nhập một phần".
- Số quận/huyện nạp được là **696**. Nguồn chỉ liệt kê huyện có dòng chuyển đổi; 5 huyện đảo (Bạch Long Vĩ, Cồn Cỏ, Hoàng Sa, Lý Sơn, Côn Đảo) có huyện nhưng không có xã con. Tôi chưa đối chiếu được với tổng số huyện công bố chính thức, nên nên kiểm tra trước khi coi là đầy đủ.

## 6. Phần mã C# đã làm (đã chạy thử)

- **Entity/EF:** `Domain/DiaGioiEntities.cs`, `Data/Configurations/DiaGioiConfiguration.cs`; thêm `DonViId` vào 7 entity.
- **Dịch vụ:** `Services/DiaGioiService.cs` (tỉnh, con, đường dẫn, tìm theo tên/tên gọi khác, cây con, kiểm tra đơn vị hợp lệ, cha chung). Hệ gốc đặt bằng cấu hình `DiaGioi:HeGoc` (`Cu` mặc định, hoặc `Moi`).
- **API công khai:** `GET /api/dia-gioi/tinh`, `/{id}/con`, `/{id}/duong-dan`, `/tra-cuu?q=`.
- **Bộ chọn tỉnh → huyện → xã:** `Views/Shared/_DiaGioiPicker.cshtml` + `wwwroot/js/dia-gioi.js`. Dùng ở biểu mẫu cơ sở/mốc địa điểm (bắt buộc đến xã/phường; huyện đảo không có xã thì dừng ở huyện), soạn thông tin tra cứu của quản trị, và hai trang tìm kiếm.
- **Lưu dữ liệu:** `Region` (chuỗi) được **sinh từ đơn vị đã chọn** ("Phường Phước Ninh, Quận Hải Châu, Thành phố Đà Nẵng"). Gửi duyệt cơ sở mà không chọn khu vực bị từ chối (422). Dịch vụ lấy `DonViId` là đơn vị cha chung của các cơ sở áp dụng.
- **Tìm kiếm:** tham số `Area` lọc theo đơn vị và mọi đơn vị con, ở `/Discovery` và `/kham-pha`. Chỉ mục tra cứu tự đồng bộ `DonViId` từ địa điểm/dịch vụ.
- **Khởi động:** `SchemaDatabase` chạy 008, nạp 008c khi bảng rỗng, tạo thủ tục 008b; ở Development hệ thống gán khu vực cũ sau khi seed.

Kết quả chạy thử trên website thật: API trả 63 tỉnh; tìm "hải châu" ra đúng quận kèm đường dẫn; `/Discovery` 75 mục, lọc Hội An còn 27, Đà Nẵng 1; lưu nháp cơ sở với xã đã chọn thành công và sinh đúng `Region`; gửi duyệt không chọn khu vực và chọn chỉ cấp tỉnh đều bị từ chối. Dòng cơ sở thử đã xóa.

## 6b. Trang điểm đến và dữ liệu mẫu

- `/diem-den` và `/diem-den/{id}`: gom thông tin đã công bố theo cây địa giới (khu du lịch, ăn uống, lưu trú, hoạt động, di chuyển, khu vực con, ghi chú thay đổi hành chính 2025). Mã: `Services/DiscoveryDestination.cs`, `Views/Discovery/Destination*.cshtml`.
- Dữ liệu mẫu Đà Nẵng: `Services/DiscoveryDemoContent.cs`, 14 mục (điểm đến, 9 địa điểm, sân bay, ga, hai hồ sơ hướng dẫn ăn uống/lưu trú). **Chỉ nạp khi `Demo:Enabled = true`** (không nạp ở Production). Mỗi mục ghi nguồn là dữ liệu mẫu, **không có ngày kiểm tra, giá, giờ hay đánh giá**; tọa độ là mốc tham chiếu gần đúng cần kiểm tra. Không có tên quán hay khách sạn cụ thể vì chưa có nguồn xác nhận.

## 6c. Chỉ mục tra cứu chung (cơ sở cùng danh sách với thông tin)

- `Database/009_CoSoVaoTraCuu.sql`: thêm `DiscoveryEntry.BranchId`, chỉ mục duy nhất và ràng buộc liên kết mới (`CK_DS_Link2`). Chạy lặp an toàn; tự chạy khi khởi động.
- `DiscoveryIndexWorker` mỗi phút đồng bộ cơ sở đã công bố vào chỉ mục (`Kind=Service`, `BranchId`, khu vực, vị trí, giờ, liên hệ, tiện ích đỗ xe/lối vào). Cơ sở thôi công bố thì tự biến khỏi kết quả (bộ lọc `Public()`).
- Kết quả tra cuu, trang điểm đến, gợi ý khi gõ và bản đồ dùng chung một danh sách; thẻ cơ sở dẫn tới `/co-so/{id}`. `/Discovery/Entry/{id}` của cơ sở tự chuyển sang trang cơ sở.
- Câu hỏi có tên nơi ("quán ăn Đà Nẵng") được nhận ra và lọc theo cây địa giới (gồm mọi khu vực con). Khớp từ: từ ≤ 3 ký tự phải nguyên từ, từ dài khớp đầu từ.
- Dữ liệu mẫu: `DiscoveryDemoContent.EnsureBranches` thêm 6 cơ sở mẫu ở Đà Nẵng (tên ghi "(mẫu)", không có tọa độ, giờ, liên hệ, giá), chỉ khi `Demo:Enabled = true`.

## 6d. Giao diện duyệt theo nhu cầu (bước 1 và 2 của đề xuất)

- **Trang chủ `/Discovery`** (`Views/Discovery/Home.cshtml`): một ô tìm có gợi ý khi gõ, 6 ô nhu cầu (Ăn gì, Ở đâu, Chơi ở đâu, Đi lại, Xem bản đồ, Lên lịch trình) và thẻ điểm đến. Không còn 3 ô chọn tỉnh/huyện/xã. Khi có điều kiện tìm kiếm trong URL thì hiện trang kết quả `Search.cshtml`, nơi bộ chọn khu vực nằm trong mục "Bộ lọc". `/Discovery?Sort=relevance` mở thẳng trang kết quả/bộ lọc đầy đủ.
- **Chọn việc trước, nơi sau**: `/diem-den?viec=an-uong|luu-tru|tham-quan|di-chuyen|ban-do` liệt kê điểm đến và mở thẳng tab tương ứng.
- **Trung tâm điểm đến `/diem-den/{id}?tab=&q=&area=`** (`Views/Discovery/Destination.cshtml`, `DiscoveryService.Destination`): các tab Tổng quan, Tham quan, Ăn uống, Lưu trú, Di chuyển, Bản đồ kèm số lượng. Tab danh sách có bố cục danh sách + bản đồ (`wwwroot/js/destination.js`), chip khu vực con (kèm số mục) và chip lọc nhanh (Cà phê, Chay, Khách sạn…). Tab Bản đồ gom mọi nơi có tọa độ, tô màu theo nhóm. Mục chưa có tọa độ được ghi rõ là không hiện ghim.
- **Ảnh**: chỉ dùng ảnh thật khi hệ thống có ảnh đúng nơi (`Ui.DestinationPhoto`); còn lại dùng khối màu theo loại thay vì ảnh của nơi khác.
- **Hiệu năng gợi ý**: gợi ý khi gõ chỉ đọc các cột hẹp, không sắp xếp trên nhiều dòng trong SQL (`DiaGioiService.Search` xếp hạng trong bộ nhớ), vì máy ít RAM làm các truy vấn xin nhiều bộ nhớ phải chờ ~28 giây.

## 6e. Luồng nhà cung cấp: đăng thông tin cho khách

Mục tiêu là giúp khách tìm thông tin, nên nhà cung cấp đăng **cơ sở** (một địa điểm), không phải gói dịch vụ để đặt.

- **Trang chủ nhà cung cấp** `/nha-cung-cap/co-so` ("Cơ sở của tôi"): 4 bước (xác minh, thêm cơ sở, hoàn thiện, gửi duyệt), thẻ cơ sở có ảnh, thanh độ hoàn thiện và danh sách còn thiếu. "Dịch vụ có thể đặt" chỉ là mục tùy chọn.
- **Biểu mẫu 4 phần** (`Views/Branches/Edit.cshtml`, `wwwroot/js/provider-branch.js`): nơi nào (khu vực đến cấp xã + ghim bản đồ), khách cần biết (giới thiệu, ngày/giờ mở cửa, giá tham khảo từ–đến–đơn vị, liên hệ, tiện ích), ảnh thực tế (tối đa 5 JPG/PNG), gửi duyệt (vai trò + ghi chú). Độ hoàn thiện tính trực tiếp, cùng trọng số với `BranchService.Completeness`.
- **Dữ liệu mới** (`Database/010_CoSoThongTinChoKhach.sql`): `PriceFrom/PriceTo/PriceUnit`, `OpenDays/OpenTime/CloseTime`, `PhotosJson` trên `CoSoDichVu` và `BanCapNhatCoSo`; `DiscoveryEntry.ImagePath`. Ràng buộc `CK_*_Info` bảo đảm giá và giờ hợp lệ. Ngày mở cửa gửi qua `OpenDayList` (nhiều checkbox).
- **Ảnh**: `/co-so/{id}/anh/{n}` công khai khi cơ sở đã duyệt; bản chờ duyệt (`?revision=`) chỉ chủ cơ sở và quản trị xem.
- **Hiển thị cho khách**: trang `/co-so/{id}` và trang duyệt dùng `_BranchFacts` (ảnh, giờ, giá); chỉ mục tìm kiếm sao chép giá, giờ, ảnh nên thẻ kết quả hiện ảnh thật và bộ lọc "đang mở cửa" hoạt động với cơ sở.
- Kiểm thử: `tests/provider-flow.cjs` (37 kiểm tra).

## 6f. Tin nhắn khách ↔ cơ sở (giống hộp thư của Trang trên Facebook)

Khách không phải chờ "trạng thái": họ nhắn thẳng cho cơ sở và nhận trả lời ngay trên website. Đặt chỗ sau này sẽ là một thẻ yêu cầu nằm trong cuộc trò chuyện.

- **Dữ liệu** (`Database/011_TinNhanCoSo.sql`): `CuocTroChuyen` (mỗi cặp khách–cơ sở một cuộc, có mốc đã xem hai phía, trạng thái Open/Done, nhãn, ghi chú nội bộ), `TinNhanChat` (Traveler/Branch/System, Text/Image/Auto), `TinMauChat`, `CaiDatChatCoSo` (nhận tin, thường trả lời trong, lời chào, tin tự động).
- **Thời gian thực**: Server-Sent Events `GET /chat/luong` (`ChatBroker`, không cần thư viện ngoài); `wwwroot/js/chat-badge.js` giữ một kết nối cho mỗi tab, cập nhật số chưa đọc ở thanh bên và chuyển sự kiện cho trang tin nhắn (`chat.js`). Có làm mới định kỳ 20 giây dự phòng.
- **Trang**: khách `/tin-nhan`, `/tin-nhan/{id}`, `/tin-nhan/co-so/{branchId}` (cuộc trò chuyện chỉ được tạo khi gửi tin đầu tiên); cơ sở `/nha-cung-cap/tin-nhan` (lọc theo cơ sở, chưa đọc/đang mở/đã xử lý, tìm kiếm) và `/nha-cung-cap/tin-nhan/cai-dat` (cài đặt + tin mẫu). Nút "Nhắn tin cho cơ sở" và "Thường trả lời trong…" nằm trên trang `/co-so/{id}`.
- **Tính năng**: đã gửi/đã xem, đang soạn tin, ảnh JPG/PNG ≤ 5 MB, tin mẫu, trả lời tự động (tin đầu hoặc sau 6 giờ im lặng; có thể chỉ khi cơ sở đóng cửa), tạm ngưng nhận tin, thời gian trả lời thực tế 30 ngày gần nhất.
- **An toàn**: chỉ hai bên của cuộc trò chuyện đọc được (người khác nhận 404); tối đa 20 tin/phút/người gửi và giới hạn theo IP; không quá 2 liên kết trong tin của khách; không lộ email hay số điện thoại của khách; mọi POST kiểm tra mã chống giả mạo.
- Chưa có: thông báo email khi cơ sở offline, nhiều nhân viên cùng một hộp thư với phân quyền, thẻ yêu cầu đặt chỗ trong chat.
- Kiểm thử: `tests/chat-flow.cjs` (66 kiểm tra).

### Đặt dịch vụ qua tin nhắn (thay cho màn hình theo dõi trạng thái)

Khách không còn đợi "trạng thái". Muốn đặt dịch vụ, khách bấm **"Nhắn tin để đặt"** (trang dịch vụ hoặc trang cơ sở) và hai bên tự thỏa thuận ngay trong cuộc trò chuyện:

1. Khách mở form "Đặt dịch vụ" trong khung chat (chọn dịch vụ của cơ sở, ngày, số người, ghi chú) và gửi. Một **thẻ yêu cầu** hiện trong chat; bản ghi `YeuCauDatDichVu` vẫn được tạo bằng quy trình sẵn có (kiểm tra lịch phục vụ, chống gửi trùng, nhật ký, thông báo).
2. Hai bên nhắn tin tự do (hỏi giá, giảm giá, đổi giờ…).
3. Cơ sở bấm trên thẻ: **Xác nhận / Đề xuất khác** (ngày, số người, chi phí, điều kiện) **/ Từ chối** (có lý do). Khách: **Đồng ý / Không đồng ý** đề xuất, hoặc **Rút**. Mỗi hành động dùng đúng `ChapNhanAsync` hiện có và ghi một dòng sự kiện vào chat; thẻ cập nhật tức thì ở cả hai phía.
4. Yêu cầu đã thống nhất vẫn nằm ở mục "Đặt dịch vụ" của khách và "Yêu cầu" của nhà cung cấp để theo dõi kết quả phục vụ.

- Dữ liệu: `Database/012_DatDichVuTrongChat.sql` (`TinNhanChat.BookingRequestId`, loại tin `Booking` và `Event`). API: `GET /chat/co-so/{id}/dich-vu`, `POST /chat/dat`, `GET /chat/{id}/the`, `POST /chat/{id}/dat/hanh-dong`; lối vào `/tin-nhan/dich-vu/{serviceId}`.
- Dịch vụ phải gắn với cơ sở đang trò chuyện; cơ sở không gắn dịch vụ nào thì chỉ có hỏi đáp.
- Kiểm thử: `tests/chat-booking.cjs` (35 kiểm tra).

### Một hộp thư chung kiểu Messenger

Tin nhắn không còn chỉ là "khách ↔ cơ sở". Mọi vai trò dùng **một hộp thư `/tin-nhan`**, mọi liên hệ trong website đều đi qua đây:

- **Khách ↔ cơ sở** (hỏi đáp, đặt dịch vụ bằng thẻ yêu cầu): nút "Nhắn tin" trên trang cơ sở và trang dịch vụ.
- **Khách ↔ khách** (tìm bạn đồng hành): nút "Nhắn tin" trên lời rủ đi cùng (`/tin-nhan/nguoi/{accountId}`). Chỉ nhắn trước được với người đang có lời rủ công khai; tin nhắn không lộ email hay số điện thoại.
- Nhà cung cấp thấy mọi cuộc trò chuyện của các cơ sở mình trong cùng hộp thư (có bộ lọc theo cơ sở, trạng thái, nhãn, ghi chú nội bộ, tin mẫu); khách thấy cuộc trò chuyện với cơ sở lẫn với người cùng đi.
- **Chặn / bỏ chặn** ở mọi cuộc trò chuyện: người bị chặn không gửi thêm được, vẫn xem lại lịch sử.
- Dữ liệu: `Database/013_TinNhanGiuaMoiNguoi.sql` (`CuocTroChuyen.BranchId` cho phép rỗng, thêm `PeerId`, `BlockedById`; ràng buộc: hoặc có cơ sở hoặc có người nhận). Kiểm thử: `tests/chat-people.cjs` (37 kiểm tra).

### Mạng xã hội kiểu Facebook (bạn bè, bài viết, bình luận, chat nhóm)

Dành cho **khách du lịch**; nhà cung cấp và quản trị không dùng.

- **Kết bạn** (`/ban-be`, `FriendService`): gửi / chấp nhận / từ chối / hủy lời mời, hủy kết bạn, gợi ý (ưu tiên nhiều bạn chung), tìm theo tên. Gửi lời mời cho người đã mời mình thì tự thành bạn. Bị từ chối thì chờ 30 ngày mới gửi lại (người kia vẫn chủ động mời được). Có chuông thông báo.
- **Hồ sơ** (`/nguoi/{id}`): tên, ảnh, giới thiệu, nơi sống, sở thích, số bạn và bạn chung; tôn trọng mức chia sẻ của người dùng (công khai / chỉ bạn bè / riêng tư). Ảnh đại diện lấy qua `/mxh/anh/{id}` có kiểm tra quyền xem.
- **Bài viết** (`/bang-tin`, `/bai-viet/{id}`, `PostService`): đăng chữ + tối đa 4 ảnh, quyền xem **Công khai / Bạn bè / Chỉ mình tôi**, sửa (đổi nội dung, quyền xem, gỡ ảnh), xóa mềm, phân trang theo mốc. Bảng tin có hai tab: Bạn bè (mình + bạn) và Cộng đồng (bài công khai). Tối đa 10 bài/giờ.
- **Bình luận** (1 cấp trả lời), **Thích** (bài viết và bình luận; API hỗ trợ 6 cảm xúc): thêm, sửa, xóa (tác giả bình luận hoặc tác giả bài viết xóa được), bình luận bị xóa nhưng còn trả lời thì giữ chỗ trống; có thông báo cho tác giả.
- **Chat 1-1** nay cũng mở cho **bạn bè** (ngoài người có lời rủ công khai).
- **Chat nhóm** (`ChatGroups.cs`, bảng `ThanhVienChat`): tạo nhóm từ bạn bè (tối đa 50 người), tin nhắn nhóm thời gian thực có tên người gửi, đã xem theo từng người, "đang soạn" kèm tên, thêm người (chỉ thêm bạn của mình), quản trị xóa thành viên, rời nhóm (quản trị rời thì người vào sớm nhất thành quản trị), đổi tên, dòng sự kiện trong nhóm.
- Dữ liệu: `Database/015_MangXaHoi.sql` (`KetBan` duy nhất theo cặp, `BaiViet`, `BinhLuan`, `CamXuc`, `ThanhVienChat`, `CuocTroChuyen.IsGroup/Title`).
- Kiểm thử: `tests/social.cjs` (85 kiểm tra), `tests/chat-group.cjs` (44).

### Ứng dụng tin nhắn cho hai mục tiêu: tìm bạn đồng hành và đặt dịch vụ

**Tìm bạn đồng hành**
- Kết quả tìm bạn có nút **"Nhắn tin & rủ đi cùng"** trên từng thẻ (mở chat với form "Rủ đi cùng" đã chọn sẵn chuyến).
- Mục **"Phù hợp với chuyến của bạn"** (`/api/mxh/phu-hop`, `companion-match.js`): người cùng điểm đến và giao ngày với lời rủ bạn đang đăng, bỏ qua người đã mời.
- Trong chat: thẻ lời rủ → người kia **Đồng ý** → hai người **tự động thành bạn bè** → nút **"Lập nhóm chat chuyến đi"** trên thẻ tạo nhóm chat hai người (có thể thêm bạn sau); dòng "Lời rủ đang mở" hiện ngay dưới tên người kia.

**Đặt dịch vụ**
- Form đặt trong chat **kiểm tra trước khi gửi** (`/chat/dat/kiem-tra`): báo lỗi ngày quá khứ/lịch phục vụ/tạm ngừng, cảnh báo, và **tạm tính** theo giá tham khảo (theo người, theo đêm/ngày khi có ngày kết thúc, theo số lượng). Chi phí dự kiến của yêu cầu do máy chủ tính, không tin giá từ trình duyệt.
- **Ngữ cảnh cơ sở** ghim đầu cuộc trò chuyện (ảnh, tên, địa chỉ, giờ, giá, liên kết).
- Hộp thư: nhãn **"n yêu cầu đặt chờ xác nhận"** (nhà cung cấp) / "cần bạn phản hồi" (khách) và bộ lọc **Chờ xác nhận**.
- Thẻ đặt có liên kết **Chi tiết đặt chỗ & thêm vào chuyến đi**; trang chi tiết đặt chỗ có nút **Mở trò chuyện** (`/tin-nhan/dat-cho/{requestId}`).
- Kiểm thử: `tests/chat-apps.cjs` (36 kiểm tra).

### Chat đủ dùng (mốc M1 và M2)

- **Khung chat nổi** (`chat-dock.js`): bấm "Nhắn tin" ở bất kỳ trang nào mở cửa sổ nhỏ ở góc màn hình (iframe cùng nguồn, trang `/tin-nhan` ở chế độ nhúng `_EmbedLayout`), nhớ trạng thái khi chuyển trang; trang cha giữ một kết nối thời gian thực và chuyển sự kiện cho iframe. `X-Frame-Options: SAMEORIGIN`, CSP `frame-ancestors 'self'`.
- **Tin nhắn mới** (`/chat/tim`): tìm bạn bè / người có lời rủ công khai / người đã nhắn, và cơ sở, để bắt đầu chat.
- **Thao tác với tin** (`ChatMessages.cs`, `016_TinNhanNangCao.sql`): trả lời một tin (trích dẫn, nhảy tới tin gốc), sửa (15 phút), thu hồi (10 phút, xóa nội dung và tệp), biểu cảm 6 loại, vị trí (một lần, tọa độ phân tích theo invariant culture), tệp PDF ≤ 5 MB (kiểm tra nội dung thật), emoji.
- Kiểm thử: `tests/chat-dock.cjs` (18), `tests/chat-messages.cjs` (35).

### Đặt dịch vụ hoàn chỉnh trong chat (mốc M3)

- **Đặt cọc (chỉ ghi nhận, không xử lý tiền):** cơ sở "Đề nghị đặt cọc" (số tiền, hình thức Chuyển khoản trực tiếp / Tiền mặt tại quầy, ghi chú) cho yêu cầu đang chờ hoặc đã xác nhận; có thể đổi khi chưa nhận tiền; bấm "Đã nhận cọc" để chốt. Hai bên cùng thấy trên thẻ; mọi bước ghi thành dòng sự kiện. Website không lưu số tài khoản.
- **Nhiều phương án:** khi đề xuất khác, cơ sở nhập tối đa 3 phương án (nội dung + chi phí); khách bấm "Chọn phương án N" (gửi một tin xác nhận lựa chọn rồi đồng ý).
- **Thêm vào chuyến đi** ngay từ thẻ đã xác nhận (`/chat/ke-hoach`, `/chat/{id}/dat/chuyen-di`).
- **Nhắc lịch và mời đánh giá tự động** (`ChatReminderWorker`, bảng `NhacLichChat`, cấu hình `Chat:ReminderIntervalSeconds`, mặc định 900 giây): tin hệ thống vào cuộc trò chuyện đặt chỗ vào ngày trước khi dùng dịch vụ, và sau khi dùng xong kèm nút "Viết đánh giá"; mỗi loại chỉ gửi một lần cho mỗi yêu cầu, có chuông thông báo.
- **Công cụ cho nhà cung cấp:** gợi ý tin mẫu theo từ khóa trong tin khách vừa nhắn (chip ✨), thống kê 30 ngày (số yêu cầu, tỷ lệ chốt, thời gian phản hồi) ở trang cài đặt tin nhắn.
- Dữ liệu: `Database/017_DatCocNhacLich.sql`. Kiểm thử: `tests/booking-m3.cjs` (28 kiểm tra).

### Tìm bạn đồng hành hoàn chỉnh trong chat (mốc M4)

- **Thẻ thỏa thuận đi cùng** (`ChatAgreements.cs`, `018_ThoaThuanBaoCao.sql`): từ thẻ lời rủ đã được đồng ý, một bên bấm "Lập thỏa thuận đi cùng" (điểm đến, ngày đi/về, ngân sách, phạm vi đi cùng); bên kia **Xác nhận** hoặc **Chưa phù hợp** (bắt buộc có lý do); bên đề xuất không tự xác nhận được; sau khi xác nhận ai cũng có thể **Kết thúc thỏa thuận**. Dùng đúng quy trình `ThoaThuanDongHanh` (phiên bản, người xác nhận), mỗi bước có dòng sự kiện trong chat.
- **Gợi ý có lý do** ("Phù hợp với chuyến của bạn"): trùng bao nhiêu ngày, cùng phong cách đi, bạn chung, ngân sách gần nhau; nút **Không quan tâm** (bảng `BoQuaGoiY`) ẩn gợi ý đó.
- **An toàn:** tối đa **10 lời rủ trong 24 giờ**; **báo cáo** từng tin hoặc cả cuộc trò chuyện (Spam, Quấy rối, Lừa đảo, Nội dung xấu, Khác; tối đa 10 báo cáo/ngày, không báo cáo trùng khi đang chờ); quản trị xem hàng chờ ở `/quan-tri/bao-cao-chat` chỉ thấy tin bị báo cáo và vài tin ngay trước đó.
- Chưa làm: gộp chat nhóm của module "Nhóm đồng hành" (`Together`) vào hộp thư chung (module đó có chat, báo cáo và phân việc riêng); nhóm chat lập từ thẻ lời rủ vẫn là nhóm chat độc lập.
- Kiểm thử: `tests/chat-m4.cjs` (34 kiểm tra).

### Chat bền vững (mốc M5)

- **Tải thêm lịch sử** khi cuộn lên đầu cuộc trò chuyện (60 tin mỗi lần, `GET /chat/{id}/tin?truoc=`), giữ nguyên vị trí cuộn; **bản nháp** lưu theo từng cuộc trò chuyện (localStorage); **xem ảnh to**; nút **"Bật thông báo"**.
- **Thông báo khi vắng mặt** (`chat-badge.js`): khi số chưa đọc tăng lúc tab đang ẩn, tiêu đề tab nhấp nháy "(n) Tin nhắn mới" và hiện thông báo trình duyệt nếu đã cấp quyền (không kèm nội dung tin). Email nhắc để sau.
- **Hiệu năng:** danh sách hộp thư đếm chưa đọc bằng một truy vấn gộp trong SQL (không còn tải toàn bộ tin của 100 cuộc trò chuyện).
- **Chính sách giữ tin nhắn:** cấu hình `Chat:RetentionMonths` (mặc định 0 = giữ mãi). Khi bật, `ChatReminderService.PurgeOldAsync` thu hồi tin chữ/ảnh/vị trí/tệp cũ hơn mốc, xóa tệp khỏi đĩa; thẻ đặt chỗ và thỏa thuận được giữ.
- **Báo cáo** tin nhắn / cuộc trò chuyện đã có từ M4; chặn dành cho cuộc trò chuyện 1-1 (nhóm dùng "Rời nhóm").
- Kiểm thử: `tests/chat-m5.cjs` (14 kiểm tra).

### Thông tin riêng theo loại cơ sở

Mỗi loại cơ sở có biểu mẫu riêng (`BranchTemplates.cs`, `Database/019_ThuocTinhCoSo.sql`, cột `AttributesJson` có kiểm tra `ISJSON`), khách thấy ngay trên trang cơ sở:

| Loại | Trường riêng |
|---|---|
| Ăn uống | kiểu quán, chỗ ngồi, hình thức phục vụ, chế độ ăn (chay, halal…), sức chứa, thanh toán, món đặc trưng, **thực đơn có giá** |
| Lưu trú | hạng, số sao, giờ nhận/trả phòng, tiện nghi, **loại phòng có giá**, chính sách hủy |
| Tham quan / hoạt động | loại điểm, **giá vé theo đối tượng**, thời gian tham quan, phù hợp với, lưu ý, lối đi xe lăn |
| Di chuyển | loại dịch vụ, khu vực, **bảng giá**, thủ tục |
| Khác | ghi chú |

Biểu mẫu hiện nhóm trường của loại đang chọn (đổi loại thì đổi nhóm); máy chủ chỉ nhận trường của đúng loại (khóa lạ và trường loại khác bị bỏ), kiểm tra lựa chọn nằm trong danh sách, số trong khoảng, giờ `HH:mm`, danh sách tối đa N dòng dạng `Tên | giá`. Bản chờ duyệt giữ nguyên thông tin riêng; trang duyệt của quản trị cũng hiện. Các lựa chọn đánh dấu `Tag` (ví dụ "Sân vườn", "Có món chay", "Bể bơi") được thêm vào từ khóa của chỉ mục tìm kiếm. Kiểm thử: `tests/branch-templates.cjs` (19 kiểm tra). Thêm loại hoặc trường mới chỉ cần sửa `BranchTemplates.All`, không đổi cơ sở dữ liệu.

### Nhà cung cấp dễ dùng: đăng nhanh và cập nhật nhanh

- **Đăng nhanh:** cơ sở mới chỉ hiện phần vị trí (tên, loại, khu vực, địa chỉ, ghim) và nút "Lưu và tiếp tục"; lưu thành bản nháp rồi chuyển sang trang hoàn thiện có đủ 4 phần.
- **Cập nhật nhanh (duyệt theo mức độ):** với cơ sở đã được duyệt, đổi giờ mở cửa, giá, điện thoại, tiện ích, thông tin riêng theo loại (`BranchService.LightFields`) **có hiệu lực ngay**, không tạo bản chờ duyệt, ghi nhật ký `CapNhatNhanhCoSo`. Đổi tên, loại, khu vực, địa chỉ, giới thiệu, vị trí, ảnh, website (`HeavyFields`) vẫn phải duyệt; sửa lẫn cả hai nhóm thì cả hai chờ duyệt.
- Kiểm thử: `tests/provider-easy.cjs` (17 kiểm tra).

### Cơ sở – Dịch vụ – Tin tách riêng

- **Cơ sở** (thiết lập một lần, admin duyệt): đăng ký nhà cung cấp là đăng ký cơ sở đầu tiên (đăng nhập lần đầu dẫn thẳng tới form cơ sở); mở rộng thì "Đăng ký thêm cơ sở" và chờ duyệt.
- **Dịch vụ** nằm trong cơ sở: form chỉ cần chọn cơ sở rồi tên, loại, giá, mô tả; phần còn lại gập trong "Thông tin thêm". Mỗi cơ sở liệt kê dịch vụ và có nút "+ Đăng dịch vụ". Dịch vụ hiển thị ở trang cơ sở (Khám phá du lịch).
- **Sửa dịch vụ đã công bố**: giá, giờ phục vụ, điều kiện, chính sách hủy, tạm ngừng có hiệu lực ngay; tên, loại, mô tả, ảnh, cơ sở áp dụng vẫn qua duyệt.
- **Tin** (bảng tin, `BaiViet.Kind`): `Share` (chia sẻ), `Companion` (tìm bạn đồng hành: điểm đến, ngày đi/về, nút nhắn tin), `Branch` (tin giới thiệu của nhà cung cấp tại `/nha-cung-cap/co-so/{id}/tin`, không cần duyệt, hiện ở bảng tin cộng đồng và trang cơ sở). Migration 020.
- Kiểm thử: `tests/service-easy.cjs`, `tests/post-kinds.cjs`.

## 7. Việc còn lại

1. Khởi tạo Git; sao lưu (`tools/SaoLuu.ps1`); **dừng web**.
2. Chạy `008` → `008c` → `008b` trên `HoTroDuLich` (chưa chạy) và xử lý hàng chờ.
3. Sửa mã C#: entity + cấu hình cho các bảng mới, thêm `DonViId` vào 7 entity; API `/api/dia-gioi/...`; biểu mẫu chọn tỉnh → huyện → xã; lọc tìm kiếm theo `DonViId` kèm đơn vị con; màn hình xử lý hàng chờ.
4. Cập nhật `Quyen_CSDL.sql` (quyền cho bảng mới), `docs/*-schema.json` và bộ kiểm thử có kiểm tra số bảng/cột.
5. Đối chiếu 2 mã tạm (`01615X`, `02485X`) và nạp `TenGoiKhacDonVi`, `KhuDuLich`.
6. Script chưa nối vào `SchemaDatabase`; cố ý để chạy tay. Chạy `008b` làm đổi `RowVersion` của dịch vụ/cơ sở nên phải dừng web.
