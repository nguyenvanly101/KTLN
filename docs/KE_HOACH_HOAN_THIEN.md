# Kế hoạch hoàn thiện "Hành Trình"

Mục tiêu: website **giúp khách du lịch tìm thông tin**, và **mọi liên hệ giữa các bên đi qua một hộp thư chung** (kiểu Messenger). Người dùng chỉ cần nhớ ba việc: *Tìm – Nhắn – Đi*. Nhà cung cấp chỉ cần nhớ hai việc: *Đăng cơ sở – Trả lời tin nhắn*.

Nguyên tắc cho mọi giai đoạn:

1. **Một việc – một đường.** Không có hai cách làm cùng một việc (ví dụ đặt dịch vụ chỉ qua chat).
2. **Thêm tính năng phải bớt một thứ khác** trong menu hoặc trang (đã làm với điều hướng).
3. **Không xóa dữ liệu, không xóa trang cũ** cho đến khi đường mới được kiểm thử và dùng thử.
4. **Mỗi giai đoạn có kiểm thử tự động và tiêu chí "xong" đo được.**
5. Cấu trúc dữ liệu luôn do SQL sở hữu (`Database/0xx_*.sql`, chạy lặp an toàn), EF chỉ ánh xạ.

---

## Giai đoạn 0 · Chốt bản hiện tại

| Việc | Chi tiết |
|---|---|
| Hồi quy | Chạy `run-all` (geography, ds-features/edge, search-merge, br-features/edge, provider-flow, chat-flow, chat-booking, chat-people, navigation, smoke, companion-pages). |
| Triển khai | Dừng bản 5216, build, chạy lại; kiểm tra 013 đã áp (cột `PeerId`, `BlockedById`). |
| Dọn test | Thêm bước **tạo lại database test sạch** trước khi chạy (hiện dữ liệu cũ tích lũy làm lệch kết quả tìm kiếm gần). |
| Thử tay | Kịch bản 10 phút: khách nhắn cơ sở → đặt dịch vụ → cơ sở đề xuất → khách đồng ý; khách A nhắn khách B từ lời rủ; chặn/bỏ chặn. |

**Xong khi:** mọi bộ đạt, 5216 chạy bản mới, kịch bản thử tay không lỗi.
**Công sức:** 0,5 ngày.

---

## Giai đoạn 1 · Chat là trung tâm thật sự

### 1.1 Lời rủ đi cùng ngay trong chat
- **Hiện tại:** khách điền form "Gửi lời mời kết nối" → chờ trạng thái → thỏa thuận ở trang riêng (`DeNghiKetNoiDongHanh`, `ThoaThuanDongHanh`).
- **Mới:** từ lời rủ, khách bấm "Nhắn tin"; trong chat có nút **"Gửi lời rủ đi cùng"** tạo thẻ (giống thẻ đặt dịch vụ). Người nhận bấm **Đồng ý / Từ chối** trên thẻ.
- **Dữ liệu:** thêm `TinNhanChat.ConnectionId` (hoặc dùng chung cột tham chiếu thẻ: đổi `BookingRequestId` thành `CardKind` + `CardRefId`, giữ tương thích).
- **Mã:** `ChatService.SendConnection`, `ChatService.ConnectionAct` gọi lại `IKetNoiDongHanhService` hiện có (không viết lại luật kết nối: kiểm tra trùng, hạn, riêng tư); thêm loại thẻ trong `chat.js` (`renderCards` theo `kind`).
- **Riêng tư:** sau khi hai bên đồng ý, mới chia sẻ ngân sách và nội dung "khi kết nối" (đúng mô hình hiện có); chat không tự lộ email/điện thoại.
- **Kiểm thử:** `tests/chat-companion.cjs` — gửi thẻ, đồng ý, từ chối, rút, gửi trùng, người thứ ba không thao tác được, quy tắc "chỉ nhắn với người có lời rủ công khai".

### 1.2 Gộp chat nhóm đồng hành
- **Hiện tại:** `Views/Together/_Chat.cshtml` là chat riêng của nhóm.
- **Mới:** nhóm = một cuộc trò chuyện loại `Group` trong hộp thư chung (danh sách thành viên, tin nhắn chung, đã xem theo từng thành viên).
- **Dữ liệu:** `ThanhVienChat(ConversationId, AccountId, ReadId)`; `CuocTroChuyen.GroupId` (liên kết nhóm sẵn có). Di chuyển tin nhắn nhóm cũ bằng script một lần, giữ nguyên bản gốc cho đến khi đối chiếu xong.
- **Rủi ro:** mốc "đã xem" từng người làm phức tạp truy vấn chưa đọc → thêm chỉ mục và đếm bằng bảng `ThanhVienChat`, không đếm quét toàn bộ tin.
- **Kiểm thử:** `tests/chat-group.cjs` — thêm/rời thành viên, người rời không đọc được tin mới, chưa đọc theo từng người.

### 1.3 Thông báo khi vắng mặt
- Giai đoạn đầu: chuông + số chưa đọc (đã có) + **tiêu đề tab nhấp nháy khi có tin**.
- Giai đoạn sau (cần bạn chọn): email nhắc nếu tin chưa đọc sau 10 phút, tối đa 1 email/giờ/cuộc trò chuyện, có link bỏ nhắc. Làm bằng `HostedService` quét `LastMessageAt` với mốc đã gửi (`NotifiedAt`) để không gửi lặp.
- Quy tắc: không đưa nội dung tin vào email (chỉ "Bạn có tin nhắn mới từ …").

### 1.4 Gỡ đường đặt cũ
- Sau khi 1.1 và chat đặt dịch vụ chạy ổn một thời gian: ẩn liên kết "Hoặc gửi yêu cầu theo mẫu"; trang `/Bookings` chỉ còn **danh sách theo dõi** (đã xác nhận, kết quả phục vụ, đánh giá).
- Giữ route cũ chạy để tương thích test và liên kết cũ.

### 1.5 Hoàn thiện trải nghiệm chat
| Hạng mục | Làm gì |
|---|---|
| Phân trang lịch sử | Cuộn lên để tải thêm (tham số `truoc` đã có ở API, thêm vào `chat.js`). |
| Tìm trong hộp thư | Tìm theo tên + nội dung (đã có phía nhà cung cấp; mở cho mọi người dùng). |
| Ảnh | Xem to (lightbox), nén phía trình duyệt trước khi tải. |
| Phản hồi nhanh | Phím Enter gửi, Shift+Enter xuống dòng (đã có); giữ bản nháp mỗi cuộc trò chuyện trong `localStorage`. |
| Trợ năng | Vùng đọc `aria-live` cho tin mới, điều hướng bàn phím trong danh sách. |
| Điện thoại | Kiểm tra bố cục một cột (CSS đã có), thử trên màn hình 375px. |

**Xong giai đoạn 1 khi:** từ tìm kiếm đến thỏa thuận (đặt dịch vụ hoặc đi cùng), người dùng không rời hộp thư; chỉ còn một khung chat cho mọi loại.
**Công sức:** 4–5 ngày (1.1: 1,5; 1.2: 2; 1.3: 0,5; 1.4–1.5: 1).

---

## Giai đoạn 2 · Thông tin cơ sở đúng theo loại

### 2.1 Khung mẫu
- **Cấu hình mẫu** (`Services/BranchTemplates.cs`): mỗi loại là danh sách trường: `{key, nhãn, kiểu: chon-mot|chon-nhieu|so|chuoi|bang-gia, bắt buộc, gợi ý, đơn vị, hiển thị cho khách, dùng để lọc}`.
- **Lưu trữ:** `CoSoDichVu.AttributesJson` và `BanCapNhatCoSo.AttributesJson` (nvarchar(max), có `ISJSON` check); kiểm tra theo mẫu ở `BranchService` (loại chưa biết bị từ chối, giá trị ngoài danh sách bị từ chối, giới hạn độ dài/số mục).
- **Chỉ mục lọc:** các trường `dùng để lọc` sao chép vào `DiscoveryEntry.TagsJson` (đã có cơ chế tag) khi đồng bộ chỉ mục.
- **Giao diện:** biểu mẫu sinh theo mẫu (`Views/Branches/_TypeFields.cshtml`), đổi loại thì đổi nhóm trường; trang cơ sở hiển thị bằng `_BranchFacts` mở rộng.

### 2.2 Ba mẫu đầu
| Loại | Trường riêng chính | Lọc tìm kiếm |
|---|---|---|
| **Ăn uống** | kiểu quán, món đặc trưng, **thực đơn có giá** (bảng giá), khoảng giá/người, chỗ ngồi (trong nhà/ngoài trời/phòng riêng), sức chứa, chay/halal, mang đi/giao, thanh toán, đỗ xe, giờ cao điểm | chay, ngoài trời, có phòng riêng, dưới X đồng |
| **Lưu trú** | hạng (khách sạn/homestay/resort/hostel), số sao, **danh sách loại phòng** (giá, sức chứa, giường, diện tích, ảnh), giờ nhận/trả, bữa sáng, đưa đón, hủy phòng | số khách, có bể bơi, bữa sáng, khoảng giá |
| **Tham quan** | loại điểm, **giá vé theo đối tượng** (người lớn/trẻ em/cao tuổi), thời gian tham quan gợi ý, giờ/mùa đẹp, lưu ý (trang phục, đi bộ nhiều), phù hợp trẻ em/người già, lối đi xe lăn | miễn phí, phù hợp trẻ em, ngắn dưới 2 giờ |

Loại còn lại (Tour, Di chuyển, Mua sắm, Tiện ích) dùng mẫu chung trước, bổ sung sau.

### 2.3 Liên kết với đặt chỗ
- Chỉ **lưu trú** (loại phòng), **tour/hoạt động** (gói/chuyến) và **di chuyển** có mục "Nhận đặt chỗ".
- Dịch vụ đặt hiện có (`DichVuDuLich`) trở thành "loại phòng / gói / chuyến" của cơ sở; **công tắc "Nhận đặt chỗ"** chỉ hiện khi loại cơ sở hỗ trợ và đủ điều kiện (đã công bố, có liên hệ, có ít nhất một thứ để đặt).
- Ăn uống và tham quan chỉ có đăng tin + chat; "giữ bàn" là tùy chọn của quán, không bắt buộc.

### 2.4 Di chuyển dữ liệu
- Cơ sở hiện có giữ nguyên; trường mới để trống (hiển thị "Chưa công bố"); `Completeness` tính thêm điểm cho phần riêng của loại.
- Script `014_ThuocTinhCoSo.sql`: thêm cột, kiểm tra JSON, không đổi dữ liệu cũ.

**Kiểm thử:** `tests/branch-templates.cjs` — mỗi mẫu nhận/từ chối đúng; JSON sai bị chặn; lọc tìm kiếm theo thuộc tính; đổi loại không làm mất dữ liệu chung; bản chờ duyệt giữ thuộc tính.
**Xong khi:** quán ăn không còn thấy "phòng", khách sạn khai được loại phòng, lọc "có chỗ đỗ xe / dưới 100k" chạy.
**Công sức:** 4 ngày.

---

## Giai đoạn 3 · Nhà cung cấp dễ dùng

### 3.1 Đăng nhanh
- Bước 1 (một màn hình): **tên, loại, khu vực, ghim bản đồ** → lưu thành bản nháp ngay.
- Sau đó là các **thẻ riêng** (Giới thiệu, Giờ & giá, Ảnh, Thông tin theo loại, Liên hệ), mỗi thẻ lưu độc lập (`PATCH`), có thanh hoàn thiện gợi ý "+15% nếu thêm ảnh".
- Gợi ý nhập sẵn: tìm tên cơ sở trong OpenStreetMap (`OpenMapPlaces` đã có) → điền tên, tọa độ, giờ; có nút "Dùng gợi ý này".

### 3.2 Duyệt theo mức độ
| Nhóm | Trường | Quy tắc |
|---|---|---|
| **Cần duyệt** | tên, địa chỉ, vị trí, ảnh, giới thiệu, loại | giữ nguyên quy trình bản chờ duyệt hiện tại |
| **Có hiệu lực ngay** | giờ mở cửa, giá tham khảo, điện thoại, "tạm đóng hôm nay", tiện ích | ghi nhật ký; quản trị xem được lịch sử; cơ sở bị báo sai nhiều thì quay lại chế độ duyệt |

### 3.3 Xác minh theo từng cơ sở
- Bỏ điều kiện "phải xác minh nhà cung cấp mới tạo được cơ sở"; **tạo nháp tự do, công bố thì cần xác minh**.
- Cách xác minh: quản trị gọi/nhắn đến số công khai của cơ sở, hoặc nhà cung cấp tải ảnh biển hiệu; kết quả lưu trên `CoSoDichVu.VerifiedAt` và hiện nhãn "Đã xác minh" cho khách.

### 3.4 Cơ sở nhiều người quản lý
- Bảng `QuanLyCoSo(BranchId, AccountId, Role: Owner|Manager|Staff)`; chủ mời nhân viên bằng email; nhân viên trả lời tin nhắn nhưng không sửa thông tin hoặc đặt cọc.
- Hộp thư chung đã gắn với cơ sở nên chỉ cần mở rộng điều kiện truy cập `Open()` (`p.MaTaiKhoan==UserId` → thành viên của cơ sở).

**Kiểm thử:** `tests/provider-quickpost.cjs`, mở rộng `provider-flow.cjs`.
**Xong khi:** nhà cung cấp mới đăng được cơ sở đầu tiên dưới 2 phút; sửa giờ/giá không phải chờ.
**Công sức:** 4 ngày.

---

## Giai đoạn 4 · Độ tin cậy của thông tin

1. **Báo sai thông tin** trên trang cơ sở (đóng cửa, sai giá, sai vị trí, khác): bảng `BaoSaiThongTin`, gửi vào hộp thư của cơ sở như một thẻ; cơ sở bấm "Đã sửa" hoặc "Thông tin đúng". Quá hạn 7 ngày không phản hồi thì hạ thứ hạng và hiện "Chưa được xác nhận gần đây".
2. **Đánh giá:** chỉ người đã có đặt chỗ được xác nhận hoặc đã nhắn tin với cơ sở mới đánh giá (chống đánh giá ảo); cơ sở được trả lời công khai; quản trị xử lý báo cáo (đã có `Reviews/Moderation`).
3. **Nơi công cộng không có chủ** (bãi biển, chợ, phố đi bộ): biên tập viên đăng (`MocDiaDiem` đã có); khách đề xuất thêm/sửa (có duyệt); nhập sẵn từ OpenStreetMap có ghi nguồn.
4. **Độ mới của dữ liệu:** nhắc nhà cung cấp xác nhận giờ và giá mỗi 3 tháng; nhãn "Cập nhật tháng này"; cơ sở quá hạn bị hạ thứ hạng.

**Công sức:** 5 ngày.

---

## Giai đoạn 5 · Dọn dẹp và nền tảng

| Hạng mục | Làm gì |
|---|---|
| **README và tài liệu** | Viết lại README theo kiến trúc hiện tại; bổ sung các JSON hợp đồng đang thiếu để chạy lại 3 bộ kiểm thử cũ. |
| **Kiểm thử sạch** | Script tạo database test mới từ `schema.sql` + migration trước mỗi lần chạy; bỏ phụ thuộc dữ liệu tích lũy. |
| **Gộp trang tìm kiếm** | `/kham-pha` thành tab Bản đồ của trang điểm đến; bỏ trùng lặp giữa hai trang tìm kiếm. |
| **Hiệu năng** | Giữ truy vấn hẹp (bài học memory-grant của LocalDB); thêm chỉ mục cho chưa đọc; đo thời gian phản hồi API chat dưới tải 50 người dùng. |
| **Bảo mật** | Rà soát: giới hạn tốc độ chat theo tài khoản (hiện theo IP + 20 tin/phút), kích thước ảnh, quét loại tệp, CSP cho SSE; kiểm tra IDOR mọi API `/chat/*`. |
| **Triển khai** | Chuyển nền tảng chạy từ `Development` sang `Production` với `Quyen_CSDL.sql`; sao lưu `tools/SaoLuu.ps1` trước mỗi migration. |
| **Khả năng mở rộng chat** | SSE hiện chạy trong một tiến trình; ghi rõ cách chuyển sang Redis backplane/SignalR nếu chạy nhiều máy. |

**Công sức:** 3 ngày.

---

## Lộ trình và phụ thuộc

```
GĐ0 ─► GĐ1.1 ─► GĐ1.2 ─► GĐ1.3–1.5
            └──► GĐ2 ──► GĐ3 ──► GĐ4
                                 └─► GĐ5 (song song, làm dần)
```

| Mốc | Nội dung | Công sức | Người dùng thấy gì |
|---|---|---|---|
| M1 | GĐ0 + GĐ1.1 | 2 ngày | Rủ đi cùng và đặt dịch vụ đều trong chat |
| M2 | GĐ1 còn lại | 3 ngày | Một hộp thư duy nhất, kể cả nhóm |
| M3 | GĐ2 | 4 ngày | Thông tin cơ sở đúng theo từng loại, lọc chi tiết |
| M4 | GĐ3 | 4 ngày | Nhà cung cấp đăng nhanh, sửa giờ giá tức thì |
| M5 | GĐ4 | 5 ngày | Báo sai, đánh giá, nơi công cộng |
| M6 | GĐ5 | 3 ngày | Gọn, sạch, sẵn sàng chạy thật |

Tổng: khoảng **21 ngày làm việc**. Sau mỗi mốc có một bản chạy được trên 5216.

## Quyết định cần bạn chốt

1. **Email nhắc tin chưa đọc** (GĐ1.3): chọn dịch vụ gửi email (SMTP của bạn, hay dịch vụ ngoài) hoặc để sau. Đề xuất: **để sau**, dùng chuông và số chưa đọc.
2. **Chat nhóm** (GĐ1.2): gộp vào hộp thư chung. Đề xuất: **gộp**.
3. **Ai được đánh giá** (GĐ4): chỉ người đã đặt/nhắn tin. Đề xuất: **giữ quy tắc này**.
4. **Nhân viên nhiều cấp** (GĐ3.4): cần hay để sau? Đề xuất: **làm sau M4** nếu có nhà cung cấp thật yêu cầu.

## Rủi ro chính

| Rủi ro | Cách giảm |
|---|---|
| Làm nhiều làm trang phức tạp lại | Mỗi giai đoạn kèm "bớt gì"; kiểm tra số mục menu và số bước trước khi đóng giai đoạn |
| Gộp chat nhóm làm mất tin cũ | Di chuyển bằng script giữ bản gốc, đối chiếu số lượng trước khi ẩn khung cũ |
| Mẫu theo loại làm biểu mẫu dài | Chỉ hiện trường bắt buộc trước, còn lại là thẻ "Nên bổ sung" |
| Chat bị lạm dụng (spam, quấy rối) | Chặn, báo cáo, giới hạn tốc độ, chỉ nhắn trước với người có lời rủ công khai |
| LocalDB thiếu bộ nhớ | Truy vấn hẹp, kiểm tra thời gian từng API chính, thử trên SQL Server thật trước khi chạy thật |

---

# PHẦN BỔ SUNG (v2)

Phần này thêm các mảng kế hoạch trước còn thiếu. Thứ tự đề xuất: **B1 → B2 → B3** ngay sau Giai đoạn 1; các mảng còn lại làm theo nhu cầu.

## B1 · Kết nối giữa người với người (kiểu "kết bạn" của Facebook)

**Vấn đề:** hiện có chat nhưng chưa có khái niệm "người quen". Muốn nhắn lại phải tìm lại trang lời rủ hoặc trang cơ sở.

**Mô hình:** *Kết nối* là quan hệ hai chiều giữa hai tài khoản (khách–khách). Cơ sở thì khách **theo dõi** (một chiều, không cần chấp nhận). Quan hệ này là nền để: gợi ý, xem ai đang hoạt động, mời vào nhóm, chia sẻ chuyến đi.

| Thành phần | Chi tiết |
|---|---|
| Dữ liệu | `KetNoi(AccountA, AccountB, Status: Pending/Accepted/Declined/Removed, RequestedBy, CreatedAt, RespondedAt)`, duy nhất theo cặp (A<B). `TheoDoiCoSo(AccountId, BranchId)` cho theo dõi cơ sở. |
| Hành động | Thêm kết nối · Chấp nhận · Từ chối · Hủy lời mời · Xóa kết nối · Chặn (dùng lại cơ chế chặn của chat). |
| Nơi xuất hiện nút | Trang lời rủ, hồ sơ công khai, kết quả tìm bạn, thẻ trong chat ("Thêm kết nối"), trang cơ sở ("Theo dõi"). |
| Danh bạ | Tab **"Mọi người"** trong hộp thư: danh sách kết nối, chấm xanh đang hoạt động, bấm tên để nhắn tin. |
| Gợi ý | "Người bạn có thể muốn kết nối": cùng điểm đến và ngày gần nhau, cùng nhóm, từng nhắn tin với nhau. Không gợi ý người đã chặn hoặc đã từ chối. |
| Quyền riêng tư | Cài đặt "Ai được nhắn tin cho tôi": mọi người có lời rủ công khai / chỉ người đã kết nối. Email và số điện thoại chỉ lộ cho người đã kết nối nếu chủ tài khoản bật. |
| Thông báo | Chuông + thẻ "Lời mời kết nối" trong hộp thư. |
| Kiểm thử | `tests/connections.cjs`: gửi/chấp nhận/từ chối/hủy/xóa, trùng cặp, không tự kết nối, chặn, quyền riêng tư, gợi ý không lộ dữ liệu. |

**Quan hệ với kết nối đi cùng đã có:** lời rủ đi cùng (`DeNghiKetNoiDongHanh`) vẫn là thỏa thuận cho **một chuyến cụ thể**; kết nối (B1) là quan hệ **lâu dài**. Khi hai người đồng ý đi cùng, hệ thống gợi ý "Thêm kết nối" một lần.
**Công sức:** 3 ngày.

## B2 · Hồ sơ công khai (khách và cơ sở)

| Hạng mục | Chi tiết |
|---|---|
| Hồ sơ khách | Ảnh, tên hiển thị, giới thiệu, sở thích, nơi sống, chuyến đã đi (tùy chọn công khai), lời rủ đang mở. Tận dụng `HoSoNguoiDung` và cài đặt `ThongTinChiaSe` sẵn có. |
| Hồ sơ cơ sở | Chính là trang `/co-so/{id}` (đã có), thêm: nút Theo dõi, số người theo dõi, bài đăng ngắn (khuyến mãi, thông báo nghỉ lễ), đánh giá. |
| Liên kết | Mọi tên người/cơ sở trong chat, lời rủ, đánh giá đều bấm vào được hồ sơ. |
| Riêng tư | Mỗi trường hồ sơ có mức: Công khai / Người đã kết nối / Chỉ mình tôi. |

**Công sức:** 2 ngày.

## B3 · Khung chat nổi ở mọi trang

- Bấm "Nhắn tin" ở bất kỳ đâu mở **cửa sổ chat nhỏ ở góc màn hình**, không chuyển trang; có thể thu nhỏ thành bong bóng, giữ tối đa 3 cửa sổ.
- Nút bong bóng cố định ở góc (số chưa đọc), bấm mở danh sách rút gọn + ô tìm người.
- Điện thoại: bong bóng mở toàn màn hình.
- Kỹ thuật: tách `chat.js` thành mô-đun dùng chung giữa trang `/tin-nhan` và khung nổi; khung nổi nạp lười; giữ trạng thái mở/đóng trong `sessionStorage`; vẫn một kết nối SSE cho cả trang (`chat-badge.js`).
- Tránh trùng: đang ở trang `/tin-nhan` thì không hiện khung nổi.

**Công sức:** 2,5 ngày.

## B4 · Thao tác với tin nhắn

| Chức năng | Chi tiết |
|---|---|
| Trả lời một tin | Trích dẫn tin gốc (`ReplyToId`), bấm nhảy tới tin gốc. |
| Thu hồi / xóa | Thu hồi cho cả hai trong 10 phút (hiện "Tin đã được thu hồi", xóa nội dung và ảnh khỏi đĩa); xóa chỉ phía mình. Không thu hồi tin đã là thẻ đặt/kết nối. |
| Biểu cảm | Thả emoji dưới tin (`PhanUngTin(MessageId, AccountId, Emoji)`), bộ 6 emoji cố định. |
| Chỉnh sửa | Sửa trong 10 phút, hiện nhãn "đã sửa". |
| Gửi vị trí | Chia sẻ ghim bản đồ một lần (không chia sẻ liên tục); bấm mở bản đồ. |
| Gửi tệp | PDF/ảnh ≤ 10 MB (vé, voucher), kiểm tra loại tệp. |
| Chia sẻ trong chat | Gửi **thẻ cơ sở**, **thẻ lịch trình** (xem trước + nút mở), **thẻ chuyến đi** cho người khác. |

**Công sức:** 3 ngày.

## B5 · Chuyến đi chung trong chat

- Từ lịch trình, bấm "Mời bạn" gửi thẻ chuyến đi vào chat; người nhận xem lịch trình theo quyền (chỉ xem / cùng chỉnh).
- Khi hai người thống nhất đi cùng, tạo **nhóm chuyến đi** (cuộc trò chuyện nhóm kèm lịch trình, chi tiêu chung, danh sách việc — tận dụng `Together` sẵn có).
- Thẻ chi tiêu chung và nhắc việc xuất hiện trong chat nhóm; mốc "Hôm nay" (`Journey/Today`) gửi nhắc vào chat nhóm.

**Công sức:** 3 ngày (sau Giai đoạn 1.2).

## B6 · Thanh toán và đặt cọc

**Nguyên tắc:** giai đoạn đầu **không xử lý tiền** trên website; chỉ ghi nhận thỏa thuận để tránh rủi ro pháp lý.

| Mức | Chi tiết |
|---|---|
| Mức 1 (làm trước) | Thẻ đặt chỗ có trường "Đặt cọc: có/không, số tiền, hình thức (chuyển khoản trực tiếp/tại quầy)", hai bên cùng thấy; cơ sở bấm "Đã nhận cọc". Không lưu số tài khoản trên hệ thống. |
| Mức 2 (sau) | Mã QR chuyển khoản theo số tài khoản cơ sở tự khai; hiển thị chính sách hủy và hoàn cọc rõ ràng. |
| Mức 3 (nếu cần) | Cổng thanh toán (VNPay/MoMo): cần hợp đồng, kiểm duyệt, đối soát; chỉ làm khi có nhu cầu thật. |

**Công sức:** Mức 1: 1 ngày.

## B7 · Thông báo và nhắc việc

| Kênh | Chi tiết |
|---|---|
| Trong trang | Chuông + số chưa đọc (đã có), âm báo nhẹ tùy chọn, tiêu đề tab nhấp nháy. |
| Thông báo trình duyệt (Web Push) | Xin quyền một lần, gửi khi có tin chưa đọc sau 2 phút; tắt được trong cài đặt. |
| Email | Tóm tắt tin chưa đọc sau 10 phút, tối đa 1 email/giờ/cuộc trò chuyện, không kèm nội dung tin. |
| Cài đặt | Trang "Thông báo của tôi": bật/tắt theo loại (tin nhắn, kết nối, đặt chỗ, nhắc chuyến đi), giờ yên lặng. |

**Công sức:** 2 ngày (Web Push) + 1 ngày (email) khi bạn chốt dịch vụ gửi.

## B8 · Tìm kiếm và khám phá nâng cao

- **Một ô tìm cho tất cả:** điểm đến, cơ sở, người (chỉ người có lời rủ công khai), cuộc trò chuyện.
- **Bản đồ:** bấm ghim có nút "Nhắn tin" và "Lưu"; lọc "đang mở cửa".
- **Lưu và so sánh:** danh sách yêu thích theo chuyến đi; so sánh 2–3 cơ sở cạnh nhau (giá, giờ, tiện ích).
- **Gợi ý theo bối cảnh:** "Gần đây", "Mở cửa giờ này", "Phù hợp trẻ em", dựa trên thuộc tính của Giai đoạn 2.
- **Chia sẻ:** nút chia sẻ danh sách/điểm đến qua đường dẫn công khai (có xem trước, SEO cơ bản).

**Công sức:** 3 ngày.

## B9 · An toàn trong chuyến đi

- **Chia sẻ vị trí tạm thời** với bạn đồng hành trong nhóm (bật theo giờ, tự tắt).
- **Nút "Cần giúp"** trong chat nhóm: gửi vị trí + tin khẩn cho cả nhóm; liên kết số khẩn cấp địa phương (113/114/115).
- **Báo cáo** người dùng/cơ sở/tin nhắn có vấn đề (lý do + bằng chứng là chính tin nhắn), hàng chờ cho quản trị (đã có kiểu `Moderation`).
- **Xác minh danh tính nhẹ** cho người đăng lời rủ (số điện thoại xác minh, huy hiệu).

**Công sức:** 3 ngày.

## B10 · Quản trị và vận hành

| Hạng mục | Chi tiết |
|---|---|
| Bảng điều khiển | Số người dùng, cơ sở chờ duyệt, báo cáo mở, số tin/ngày, thời gian phản hồi trung bình của cơ sở. |
| Kiểm duyệt chat | Chỉ xem khi có báo cáo, có nhật ký ai xem gì. |
| Cấu hình | Giới hạn tốc độ, độ dài tin, kích thước tệp, thời hạn thu hồi chỉnh trong `appsettings`. |
| Nhật ký | Mở rộng `NhatKy` cho kết nối, chặn, thu hồi tin, thay đổi cài đặt cơ sở. |
| Dữ liệu | Chính sách giữ tin nhắn (ví dụ 12 tháng) và xóa tài khoản (ẩn danh hóa tin nhắn thay vì xóa). |

**Công sức:** 3 ngày.

## B11 · Nhà cung cấp: công cụ bán hàng nhẹ

- **Bài đăng ngắn** của cơ sở (khuyến mãi, món mới, giờ lễ), hiện trên trang cơ sở và gửi cho người theo dõi.
- **Báo cáo hoạt động:** lượt xem, lượt nhắn, lượt chỉ đường, tỷ lệ chuyển thành đặt chỗ, thời gian phản hồi.
- **Gợi ý tin mẫu:** khi khách gõ từ khóa ("giờ mở cửa", "đỗ xe") gợi ý tin mẫu cho nhân viên.
- **Chatbot trả lời nhanh** (tùy chọn, về sau): trả lời câu hỏi cơ bản từ thông tin đã khai, luôn có nút "Gặp nhân viên".

**Công sức:** 3 ngày (không gồm chatbot).

## B12 · Đa ngôn ngữ và trợ năng

- Giao diện Việt/Anh, mô tả cơ sở song ngữ (nhà cung cấp nhập hoặc dịch gợi ý có xem lại).
- Dịch tin nhắn một chạm (tùy chọn, chỉ gửi nội dung ra dịch vụ dịch khi người dùng đồng ý).
- Trợ năng: đọc màn hình cho chat (`aria-live`), điều hướng bàn phím, độ tương phản, cỡ chữ.
- Ngày giờ theo múi giờ người xem (hiện lưu UTC).

**Công sức:** 3 ngày.

## B13 · Nền tảng kỹ thuật cho quy mô thật

| Hạng mục | Làm gì |
|---|---|
| Thời gian thực nhiều máy chủ | Thay `ChatBroker` trong bộ nhớ bằng Redis pub/sub hoặc SignalR backplane; giữ giao diện `IChatBroker`. |
| Lưu tệp | Chuyển ảnh/tệp sang kho đối tượng (S3/Azure Blob) có quét virus và CDN. |
| Tìm kiếm | Chỉ mục toàn văn có xếp hạng (SQL Full-Text đã có; cân nhắc OpenSearch khi dữ liệu lớn). |
| Hiệu năng | Phân trang mọi danh sách, đếm chưa đọc bằng bảng tổng hợp; kiểm thử tải 200 người dùng đồng thời. |
| CI | Chạy toàn bộ bộ kiểm thử mỗi lần đẩy mã, database test tạo mới từ `schema.sql` + migration. |
| Triển khai | Môi trường staging/production tách biệt, sao lưu tự động, kiểm tra sức khỏe `/healthz`, log tập trung. |

**Công sức:** 4 ngày.

---

## Lộ trình sau bổ sung

| Mốc | Nội dung | Công sức |
|---|---|---|
| M1 | GĐ0 + GĐ1.1 | 2 ngày |
| M2 | GĐ1 còn lại | 3 ngày |
| **M2b** | **B1 Kết nối + B2 Hồ sơ + B3 Khung chat nổi** | **7,5 ngày** |
| **M2c** | **B4 Thao tác tin nhắn + B6 Mức 1 Đặt cọc** | **4 ngày** |
| M3 | GĐ2 Thông tin theo loại | 4 ngày |
| M4 | GĐ3 Nhà cung cấp dễ dùng | 4 ngày |
| **M4b** | **B5 Chuyến đi chung + B7 Thông báo** | **6 ngày** |
| M5 | GĐ4 Độ tin cậy + **B9 An toàn** | 8 ngày |
| **M5b** | **B8 Tìm kiếm nâng cao + B10 Quản trị + B11 Công cụ nhà cung cấp** | **9 ngày** |
| M6 | GĐ5 Dọn dẹp + **B12 Đa ngôn ngữ + B13 Nền tảng** | 10 ngày |

Tổng sau bổ sung: khoảng **58 ngày làm việc**. Mọi mốc vẫn kết thúc bằng một bản chạy được; mảng nào chưa cần có thể cắt mà không làm hỏng mảng khác. Phụ thuộc chính: B3 và B4 dùng chung mô-đun chat; B5 cần GĐ1.2; B7 cần dịch vụ gửi email/Web Push.

## Nguyên tắc "bớt một thứ" cho phần bổ sung

| Khi thêm | Bớt |
|---|---|
| Danh bạ kết nối (B1) | Trang "Kết nối của tôi" riêng trong menu Đồng hành |
| Khung chat nổi (B3) | Chuyển trang khi bấm "Nhắn tin" |
| Hồ sơ công khai (B2) | Thông tin hồ sơ lặp lại trên trang chi tiết lời rủ |
| Thẻ chuyến đi trong chat (B5) | Form chia sẻ chuyến đi riêng |
| Thông báo trình duyệt (B7) | Trang "Thông báo" như nơi duy nhất xem tin |
| Bảng điều khiển (B10) | Nhiều trang thống kê rời |

## Quyết định cần bạn chốt thêm

1. **Hướng "kết nối"**: B1 (danh bạ kết bạn) + B2 (hồ sơ) như mô tả trên, hay ý khác? Đây là điểm quan trọng nhất.
2. **Thanh toán** (B6): dừng ở Mức 1 (ghi nhận cọc, không xử lý tiền)?
3. **Chat nổi** (B3): đặt trước hay sau B1? Đề xuất làm B1 và B3 cùng mốc M2b.
4. **Dịch vụ gửi email / Web Push** (B7): dùng dịch vụ nào, hay để sau?
5. **Chatbot** (B11): có cần ở giai đoạn này không? Đề xuất không.
