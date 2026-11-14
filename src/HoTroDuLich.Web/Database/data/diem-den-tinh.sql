-- Them trang Diem den cho cac tinh noi bat (noi dung tong quan, chua kiem chung). Chay lap an toan: bo qua muc da co cung Slug.
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-ha-noi')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-ha-noi','Destination',N'Hà Nội',N'Hà Nội',N'Thủ đô với phố cổ, hồ Hoàn Kiếm, các di tích nghìn năm và ẩm thực đường phố nổi tiếng.',N'Điểm nên ghé: khu Phố cổ, hồ Hoàn Kiếm, Văn Miếu – Quốc Tử Giám, Hoàng thành Thăng Long, Lăng Chủ tịch Hồ Chí Minh.

Thời điểm dễ chịu: mùa thu (khoảng tháng 9–11) và mùa xuân. Mùa hè nóng và có mưa lớn; mùa đông lạnh ẩm.

Đến nơi: sân bay quốc tế Nội Bài, ga Hà Nội và nhiều bến xe liên tỉnh.

Ăn uống: phở, bún chả, chả cá, bún đậu mắm tôm, cà phê trứng.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',21.0285,105.8542,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'hà nội thủ đô phố cổ văn hóa ẩm thực','Published',1,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-tp-ho-chi-minh')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-tp-ho-chi-minh','Destination',N'TP. Hồ Chí Minh',N'TP. Hồ Chí Minh',N'Thành phố lớn nhất cả nước, sôi động, nhiều bảo tàng, chợ và quán ăn mở khuya.',N'Điểm nên ghé: chợ Bến Thành, Nhà thờ Đức Bà, Bưu điện Thành phố, Dinh Độc Lập, Bảo tàng Chứng tích Chiến tranh; Địa đạo Củ Chi ở ngoại thành.

Khí hậu có mùa khô (khoảng tháng 12–4) và mùa mưa (khoảng tháng 5–11), mưa thường vào chiều tối.

Đến nơi: sân bay Tân Sơn Nhất, ga Sài Gòn và các bến xe liên tỉnh.

Ăn uống: hủ tiếu, cơm tấm, bánh mì, ốc, cà phê.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',10.7769,106.7009,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'sài gòn thành phố hồ chí minh chợ bảo tàng ẩm thực','Published',50,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-hue')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-hue','Destination',N'Huế',N'Huế',N'Cố đô với quần thể di tích triều Nguyễn bên dòng sông Hương và ẩm thực cung đình.',N'Điểm nên ghé: Đại Nội, chùa Thiên Mụ, các lăng vua (Tự Đức, Khải Định, Minh Mạng), sông Hương.

Thời điểm: mùa nắng khoảng tháng 3–8. Mùa mưa cuối năm (khoảng tháng 9–12) dễ có bão và lũ, nên theo dõi dự báo.

Đến nơi: sân bay Phú Bài, ga Huế, quốc lộ 1A.

Ăn uống: bún bò Huế, bánh bèo, bánh nậm, bánh lọc, cơm hến.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',16.4637,107.5909,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'huế cố đô di sản đại nội lăng tẩm ẩm thực','Published',31,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-khanh-hoa-nha-trang')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-khanh-hoa-nha-trang','Destination',N'Khánh Hòa (Nha Trang)',N'Khánh Hòa (Nha Trang)',N'Thành phố biển với vịnh đẹp, đảo, lặn ngắm san hô và nhiều tháp Chăm cổ.',N'Điểm nên ghé: bãi biển trung tâm, Tháp Bà Ponagar, các đảo trong vịnh Nha Trang, Hòn Chồng, tắm bùn khoáng.

Mùa biển đẹp thường từ đầu năm đến khoảng tháng 8; cuối năm mưa và gió mạnh hơn.

Đến nơi: sân bay Cam Ranh (cách trung tâm khoảng 30 km), ga Nha Trang.

Ăn uống: bún sứa, nem nướng Ninh Hòa, hải sản, bánh canh chả cá.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',12.2388,109.1967,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'nha trang khánh hòa biển đảo lặn','Published',37,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-kien-giang-phu-quoc')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-kien-giang-phu-quoc','Destination',N'Kiên Giang (Phú Quốc)',N'Kiên Giang (Phú Quốc)',N'Đảo ngọc với bãi biển đẹp, hải sản, nước mắm và nhiều khu nghỉ dưỡng.',N'Điểm nên ghé: Bãi Sao, Bãi Dài, Hòn Thơm, chợ đêm Dương Đông, vườn tiêu, cơ sở làm nước mắm; Hà Tiên và Rạch Giá trên đất liền.

Mùa khô khoảng tháng 11–4 biển đẹp nhất; mùa mưa khoảng tháng 5–10 có thể mưa dông và sóng lớn.

Đến nơi: sân bay Phú Quốc; tàu cao tốc từ Rạch Giá hoặc Hà Tiên (kiểm tra lịch tàu).

Ăn uống: hải sản, gỏi cá trích, bún quậy, nhum biển.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',10.2899,103.984,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'phú quốc kiên giang đảo biển hải sản','Published',58,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-quang-ninh-ha-long')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-quang-ninh-ha-long','Destination',N'Quảng Ninh (Hạ Long)',N'Quảng Ninh (Hạ Long)',N'Nơi có vịnh Hạ Long với hàng nghìn đảo đá vôi, cùng vịnh Bái Tử Long, Yên Tử và Cô Tô.',N'Điểm nên ghé: vịnh Hạ Long (du thuyền, chèo kayak, thăm hang động), vịnh Bái Tử Long, Yên Tử, đảo Cô Tô, Tuần Châu.

Thời điểm: mùa thu và xuân thường dễ chịu; mùa hè (khoảng tháng 6–9) có thể có bão, tàu có thể bị hoãn.

Đến nơi: cao tốc từ Hà Nội (khoảng 150 km), sân bay Vân Đồn, tàu cao tốc ra đảo.

Ăn uống: chả mực, sá sùng, ngán Vân Đồn, bánh gai.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',20.9101,107.1839,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'hạ long quảng ninh vịnh di sản biển đảo du thuyền','Published',14,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-lao-cai-sa-pa')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-lao-cai-sa-pa','Destination',N'Lào Cai (Sa Pa)',N'Lào Cai (Sa Pa)',N'Thị trấn vùng cao với ruộng bậc thang, núi Fansipan và văn hóa các dân tộc thiểu số.',N'Điểm nên ghé: núi Fansipan, bản Cát Cát, thung lũng Mường Hoa, thác Bạc, chợ phiên vùng cao.

Lúa chín đẹp khoảng tháng 9–10; mùa mưa khoảng tháng 5–9; mùa đông rất lạnh, có sương mù và đôi khi có băng giá.

Đến nơi: cao tốc Hà Nội – Lào Cai rồi lên Sa Pa, tàu hỏa đến ga Lào Cai.

Ăn uống: thắng cố, cá hồi, thịt lợn cắp nách, rau rừng. Nên mang áo ấm và giày chống trơn.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',22.3364,103.8438,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'sa pa lào cai núi ruộng bậc thang fansipan','Published',6,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-ninh-binh')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-ninh-binh','Destination',N'Ninh Bình',N'Ninh Bình',N'Vùng núi đá vôi và sông nước với Tràng An, Tam Cốc và cố đô Hoa Lư, rất gần Hà Nội.',N'Điểm nên ghé: quần thể danh thắng Tràng An (đi thuyền), Tam Cốc – Bích Động, Hang Múa, chùa Bái Đính, cố đô Hoa Lư.

Lúa chín và cảnh đẹp nhất khoảng tháng 5–6 và tháng 9–10; mùa đông lạnh, mùa hè nóng.

Đến nơi: cách Hà Nội khoảng 90–100 km, đi xe khách, tàu hỏa hoặc ô tô.

Ăn uống: cơm cháy, thịt dê núi, ốc núi.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',20.2506,105.9745,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'ninh bình tràng an tam cốc hang múa di sản','Published',25,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-quang-binh')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-quang-binh','Destination',N'Quảng Bình',N'Quảng Bình',N'Vương quốc hang động với vườn quốc gia Phong Nha – Kẻ Bàng và những bãi biển còn hoang sơ.',N'Điểm nên ghé: động Phong Nha, động Thiên Đường, suối Nước Moọc, biển Nhật Lệ (Đồng Hới); hang Sơn Đoòng chỉ đi được qua đơn vị được cấp phép và số lượng rất hạn chế.

Mùa khô khoảng tháng 3–8 thuận lợi cho hang động; mùa mưa lũ cuối năm có thể đóng cửa nhiều điểm.

Đến nơi: sân bay Đồng Hới, ga Đồng Hới, quốc lộ 1A.

Ăn uống: bánh bột lọc, cháo canh, hải sản.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',17.59,106.35,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'quảng bình phong nha kẻ bàng hang động thiên nhiên','Published',29,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-binh-dinh-quy-nhon')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-binh-dinh-quy-nhon','Destination',N'Bình Định (Quy Nhơn)',N'Bình Định (Quy Nhơn)',N'Thành phố biển yên bình với Kỳ Co, Eo Gió, tháp Chăm cổ và văn hóa võ cổ truyền.',N'Điểm nên ghé: Kỳ Co, Eo Gió, Ghềnh Ráng, Tháp Đôi, tháp Bánh Ít, Phương Mai.

Mùa biển đẹp khoảng tháng 3–8; cuối năm mưa và biển động nhiều hơn.

Đến nơi: sân bay Phù Cát (cách Quy Nhơn khoảng 30 km), ga Diêu Trì.

Ăn uống: bánh xèo tôm nhảy, bún chả cá, nem chợ Huyện, hải sản.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',13.783,109.2197,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'quy nhơn bình định biển kỳ co eo gió tháp chăm','Published',35,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-can-tho')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-can-tho','Destination',N'Cần Thơ',N'Cần Thơ',N'Trung tâm miền Tây sông nước với chợ nổi, vườn trái cây và nhà cổ.',N'Điểm nên ghé: chợ nổi Cái Răng (nên đi sáng sớm), bến Ninh Kiều, nhà cổ Bình Thủy, vườn trái cây, chùa Ông.

Mùa nước nổi khoảng tháng 8–11 có cảnh sông nước đặc trưng; mùa khô khoảng tháng 12–4 dễ đi lại hơn.

Đến nơi: sân bay Cần Thơ, cao tốc từ TP. Hồ Chí Minh.

Ăn uống: lẩu mắm, bánh xèo, bún riêu, trái cây theo mùa.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',10.0452,105.7469,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'cần thơ miền tây chợ nổi sông nước trái cây','Published',59,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-ba-ria-vung-tau-con-dao')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-ba-ria-vung-tau-con-dao','Destination',N'Bà Rịa – Vũng Tàu (Côn Đảo)',N'Bà Rịa – Vũng Tàu (Côn Đảo)',N'Vùng biển gần TP. Hồ Chí Minh, có Vũng Tàu sôi động và Côn Đảo yên tĩnh, giàu di tích lịch sử.',N'Điểm nên ghé: Bãi Sau, tượng Chúa Kitô Vua, Hải đăng Vũng Tàu, Long Hải; Côn Đảo có hệ thống nhà tù lịch sử, nghĩa trang Hàng Dương và nhiều bãi biển.

Vũng Tàu cách TP. Hồ Chí Minh khoảng 100 km, tiện đi trong ngày; Côn Đảo đi bằng máy bay hoặc tàu (kiểm tra lịch).

Mùa khô khoảng tháng 11–4 biển yên hơn; mùa mưa có sóng lớn.

Ăn uống: bánh khọt, hải sản, lẩu cá đuối.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',10.346,107.0843,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'vũng tàu côn đảo bà rịa biển lịch sử','Published',49,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-binh-thuan-mui-ne')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-binh-thuan-mui-ne','Destination',N'Bình Thuận (Mũi Né)',N'Bình Thuận (Mũi Né)',N'Vùng biển nắng gió với đồi cát, làng chài và các môn thể thao lướt ván diều.',N'Điểm nên ghé: đồi cát bay, đồi cát vàng, Suối Tiên, làng chài Mũi Né, Tháp Chàm Pôshanư, Phan Thiết.

Thời điểm gió mạnh thuận lợi cho lướt ván diều khoảng tháng 11–4; nên đi bình minh hoặc hoàng hôn trên đồi cát.

Đến nơi: cách TP. Hồ Chí Minh khoảng 200 km, cao tốc và ga Phan Thiết.

Ăn uống: hải sản, bánh canh chả cá, nước mắm Phan Thiết.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',10.9333,108.287,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'mũi né phan thiết bình thuận đồi cát biển','Published',39,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-ha-giang')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-ha-giang','Destination',N'Hà Giang',N'Hà Giang',N'Cao nguyên đá Đồng Văn với những cung đường đèo hùng vĩ và vùng biên giới phía Bắc.',N'Điểm nên ghé: cổng trời Quản Bạ, Dinh thự họ Vương, Lũng Cú, đèo Mã Pí Lèng, sông Nho Quế, Hoàng Su Phì.

Mùa hoa tam giác mạch khoảng tháng 10–11; mùa mưa dễ sạt lở, sương mù nhiều.

Đường đèo hiểm trở: cân nhắc kỹ khi tự lái xe máy, kiểm tra phanh và thời tiết.

Khu vực biên giới có thể cần giấy tờ tùy thân và đăng ký theo quy định hiện hành.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',22.8233,104.9836,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'hà giang đồng văn đèo mã pí lèng cao nguyên đá','Published',2,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-son-la-moc-chau')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-son-la-moc-chau','Destination',N'Sơn La (Mộc Châu)',N'Sơn La (Mộc Châu)',N'Cao nguyên mát mẻ với đồi chè, nông trại bò sữa và mùa hoa mận, hoa cải.',N'Điểm nên ghé: đồi chè trái tim, thác Dải Yếm, rừng thông Bản Áng, cầu kính Bạch Long.

Hoa mận, hoa đào, hoa cải nở khoảng tháng 1–3; mùa mưa khoảng tháng 5–9.

Đến nơi: cách Hà Nội khoảng 180 km, đi xe khách hoặc ô tô.

Ăn uống: bê chao, sữa bò tươi, dâu tây theo mùa.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',20.842,104.64,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'mộc châu sơn la đồi chè cao nguyên hoa mận','Published',9,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-hai-phong-cat-ba')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-hai-phong-cat-ba','Destination',N'Hải Phòng (Cát Bà)',N'Hải Phòng (Cát Bà)',N'Thành phố cảng với đảo Cát Bà, bãi biển Đồ Sơn và ẩm thực đường phố đặc sắc.',N'Điểm nên ghé: đảo Cát Bà, vịnh Lan Hạ, vườn quốc gia Cát Bà, bãi biển Đồ Sơn, Nhà hát Lớn.

Mùa hè đông khách, có thể có bão; mùa thu thường dễ chịu.

Đến nơi: cách Hà Nội khoảng 100–120 km, tàu từ bến Bính hoặc Gia Luận ra Cát Bà.

Ăn uống: bánh đa cua, nem cua bể, bánh mì cay.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',20.8449,106.6881,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'hải phòng cát bà đồ sơn vịnh lan hạ biển đảo','Published',20,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-nghe-an')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-nghe-an','Destination',N'Nghệ An',N'Nghệ An',N'Quê hương Chủ tịch Hồ Chí Minh, có biển Cửa Lò và rừng nguyên sinh Pù Mát.',N'Điểm nên ghé: khu di tích Kim Liên (làng Sen), biển Cửa Lò, vườn quốc gia Pù Mát, đảo Ngư.

Mùa biển khoảng tháng 5–8; cuối năm dễ có mưa bão.

Đến nơi: sân bay Vinh, ga Vinh, quốc lộ 1A.

Ăn uống: cháo lươn, bánh mướt, cam Vinh theo mùa.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',18.6796,105.6813,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'nghệ an vinh cửa lò làng sen biển lịch sử','Published',27,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-thanh-hoa')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-thanh-hoa','Destination',N'Thanh Hóa',N'Thanh Hóa',N'Vùng đất có biển Sầm Sơn, Thành Nhà Hồ và khu bảo tồn Pù Luông.',N'Điểm nên ghé: Thành Nhà Hồ, biển Sầm Sơn, Pù Luông (ruộng bậc thang, homestay), suối cá thần Cẩm Lương.

Biển đông khách vào mùa hè; ruộng bậc thang Pù Luông đẹp khoảng tháng 5–6 và 9–10.

Đến nơi: sân bay Thọ Xuân, ga Thanh Hóa.

Ăn uống: nem chua, chè lam, hải sản Sầm Sơn.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',19.8067,105.7852,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'thanh hóa sầm sơn pù luông thành nhà hồ biển','Published',26,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-cao-bang')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-cao-bang','Destination',N'Cao Bằng',N'Cao Bằng',N'Vùng biên giới với thác Bản Giốc, hang động và cảnh quan địa chất đặc sắc.',N'Điểm nên ghé: thác Bản Giốc, động Ngườm Ngao, hồ Thang Hen, Pác Bó.

Thác nhiều nước khoảng tháng 6–9; mùa thu có hạt dẻ Trùng Khánh.

Khu vực biên giới: mang giấy tờ tùy thân và tuân thủ quy định địa phương.

Ăn uống: bánh cuốn Cao Bằng, vịt quay 7 vị, miến dong.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',22.6657,106.2522,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'cao bằng bản giốc thác hang động biên giới','Published',3,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-ninh-thuan')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-ninh-thuan','Destination',N'Ninh Thuận',N'Ninh Thuận',N'Vùng đất nắng gió với tháp Chăm, vườn nho, đồi cát và bãi biển hoang sơ.',N'Điểm nên ghé: tháp Po Klong Garai, làng gốm Bàu Trúc, vịnh Vĩnh Hy, đồi cát Nam Cương, vườn nho.

Là một trong những nơi khô nóng nhất cả nước; mang mũ, nước và kem chống nắng.

Đến nơi: sân bay Cam Ranh hoặc ga Tháp Chàm.

Ăn uống: cừu nướng, bánh canh chả cá, nho tươi theo mùa.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',11.5645,108.9886,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'ninh thuận phan rang tháp chàm vĩnh hy biển','Published',38,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-phu-yen')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-phu-yen','Destination',N'Phú Yên',N'Phú Yên',N'Vùng biển miền Trung với Gành Đá Đĩa, Mũi Điện và những bãi biển hoang sơ.',N'Điểm nên ghé: Gành Đá Đĩa, Mũi Điện, Bãi Xép, nhà thờ Mằng Lăng, vịnh Xuân Đài.

Mùa biển đẹp khoảng tháng 3–8; cuối năm mưa nhiều.

Đến nơi: sân bay Tuy Hòa, ga Tuy Hòa.

Ăn uống: sò huyết Ô Loan, bánh canh hẹ, cá ngừ đại dương.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',13.0955,109.3209,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'phú yên tuy hòa gành đá đĩa mũi điện biển','Published',36,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-quang-tri')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-quang-tri','Destination',N'Quảng Trị',N'Quảng Trị',N'Vùng đất giàu dấu ấn lịch sử với địa đạo Vịnh Mốc, Thành cổ Quảng Trị và cầu Hiền Lương.',N'Điểm nên ghé: Thành cổ Quảng Trị, địa đạo Vịnh Mốc, cầu Hiền Lương – sông Bến Hải, nghĩa trang Trường Sơn, Khe Sanh.

Mùa nắng nóng khoảng tháng 5–8; mùa mưa cuối năm có thể có lũ.

Đến nơi: quốc lộ 1A, ga Đông Hà; gần sân bay Phú Bài (Huế) hoặc Đồng Hới.

Nên đi cùng người hướng dẫn hoặc đọc tư liệu trước để hiểu bối cảnh lịch sử.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',16.8163,107.1003,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'quảng trị thành cổ vịnh mốc lịch sử di tích','Published',30,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-dak-lak-buon-ma-thuot')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-dak-lak-buon-ma-thuot','Destination',N'Đắk Lắk (Buôn Ma Thuột)',N'Đắk Lắk (Buôn Ma Thuột)',N'Thủ phủ cà phê Tây Nguyên với buôn làng, thác nước và văn hóa cồng chiêng.',N'Điểm nên ghé: Bảo tàng Thế giới Cà phê, thác Dray Nur – Dray Sáp, Buôn Đôn, hồ Lắk, làng cà phê.

Mùa khô khoảng tháng 11–4; hoa cà phê nở khoảng tháng 2–3.

Đến nơi: sân bay Buôn Ma Thuột, các tuyến xe khách liên tỉnh.

Ăn uống: cà phê, gà nướng cơm lam, bún đỏ. Khi tham quan buôn làng, hỏi ý kiến trước khi chụp ảnh người dân.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',12.6667,108.0383,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'đắk lắk buôn ma thuột cà phê tây nguyên thác','Published',42,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.DiscoveryEntry WHERE Slug='destination-lang-son')
INSERT dbo.DiscoveryEntry(Slug,Kind,Title,Region,Summary,Body,Latitude,Longitude,LocationNote,SourceName,CheckedOn,Tags,Status,DonViId,UpdatedAt)
VALUES('destination-lang-son','Destination',N'Lạng Sơn',N'Lạng Sơn',N'Vùng núi biên giới phía Bắc với hang động, núi Mẫu Sơn và nhiều đặc sản.',N'Điểm nên ghé: động Tam Thanh, núi Vọng Phu, khu du lịch Mẫu Sơn, ải Chi Lăng, Đồng Đăng – Kỳ Lừa.

Mùa thu và đông mát, Mẫu Sơn có thể rất lạnh, đôi khi có băng giá.

Đến nơi: cách Hà Nội khoảng 150 km, cao tốc và ga Đồng Đăng.

Ăn uống: vịt quay, lợn quay, khâu nhục, phở chua.

Thông tin tổng hợp từ kiến thức phổ biến, chưa kiểm chứng tại chỗ. Giá vé, giờ mở cửa, thời tiết và tình trạng đường đi có thể thay đổi; hãy kiểm tra với đơn vị quản lý hoặc nguồn chính thức gần ngày đi.',21.8537,106.7615,N'Tọa độ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan.',N'Tổng hợp biên tập từ kiến thức phổ biến, chưa kiểm chứng tại chỗ','2026-11-14',N'lạng sơn mẫu sơn tam thanh biên giới núi','Published',13,SYSUTCDATETIME());
COMMIT TRANSACTION;
