// Sinh script nhap khu du lich / dia diem noi tieng cua Viet Nam vao bang DiaDiemDuLich (nguon) va bo sung toa do, mo ta cho muc Kham pha.
// Chay: node Database/data/gen-khu-du-lich.js  -> tao khu-du-lich-1-nguon.sql va khu-du-lich-2-bosung.sql (chay bang sqlcmd -I, theo thu tu 1 -> doi may chu tao muc Kham pha -> 2).
// Toa do la gan dung (lam tron 3 so le), mo ta viet lai ngan gon, khong ghi gia hay gio mo cua.
const fs = require('fs'), path = require('path');
const T = { N: 'Thiên nhiên', V: 'Văn hóa', B: 'Biển đảo', L: 'Lịch sử', S: 'Tâm linh', G: 'Vui chơi giải trí' };
// [ten, khu vuc hien thi, tinh (he cu), loai, vi do, kinh do, mo ta, tu khoa]
const D = [
 // ---- Ha Noi
 ['Hồ Hoàn Kiếm', 'Hà Nội', 'Hà Nội', 'V', 21.029, 105.852, 'Hồ nhỏ giữa trung tâm với đền Ngọc Sơn và Tháp Rùa, nơi người Hà Nội dạo bộ buổi sáng và cuối tuần.', 'hồ dạo bộ trung tâm'],
 ['Văn Miếu – Quốc Tử Giám', 'Hà Nội', 'Hà Nội', 'L', 21.028, 105.836, 'Quần thể di tích gắn với truyền thống khoa cử và giáo dục, có khuôn viên nhiều cây xanh và các bia tiến sĩ.', 'di tích lịch sử giáo dục'],
 ['Hoàng thành Thăng Long', 'Hà Nội', 'Hà Nội', 'L', 21.036, 105.840, 'Khu di tích trung tâm của kinh thành xưa, nay còn các dấu tích kiến trúc và hiện vật được trưng bày.', 'di sản lịch sử'],
 ['Quảng trường Ba Đình và Lăng Chủ tịch Hồ Chí Minh', 'Hà Nội', 'Hà Nội', 'L', 21.037, 105.835, 'Khu quảng trường lớn cùng các công trình lịch sử, thường kết hợp tham quan Phủ Chủ tịch và Chùa Một Cột.', 'lịch sử quảng trường'],
 ['Phố cổ Hà Nội', 'Hà Nội', 'Hà Nội', 'V', 21.034, 105.850, 'Khu phố hẹp với các tên phố theo nghề truyền thống, nhiều quán ăn và cửa hàng nhỏ.', 'phố cổ ẩm thực mua sắm'],
 ['Hồ Tây', 'Hà Nội', 'Hà Nội', 'N', 21.058, 105.823, 'Hồ nước lớn nhất nội thành, có đường dạo quanh hồ và nhiều quán cà phê ngắm hoàng hôn.', 'hồ dạo bộ hoàng hôn'],
 ['Chùa Trấn Quốc', 'Hà Nội', 'Hà Nội', 'S', 21.049, 105.837, 'Ngôi chùa cổ nằm trên một bán đảo nhỏ bên Hồ Tây, có tháp nhiều tầng đặc trưng.', 'chùa cổ tâm linh'],
 ['Nhà tù Hỏa Lò', 'Hà Nội', 'Hà Nội', 'L', 21.025, 105.847, 'Di tích lịch sử trưng bày về giai đoạn đấu tranh của các thế hệ người Việt.', 'di tích lịch sử bảo tàng'],
 ['Làng gốm Bát Tràng', 'Hà Nội', 'Hà Nội', 'V', 20.974, 105.911, 'Làng nghề gốm lâu đời ven sông Hồng, có thể xem làm gốm và tự tay nặn thử.', 'làng nghề gốm trải nghiệm'],
 ['Chùa Hương', 'Hà Nội', 'Hà Nội', 'S', 20.620, 105.743, 'Quần thể chùa và hang động trong núi, đi bằng thuyền theo suối Yến, đông khách vào mùa lễ hội đầu năm.', 'chùa suối lễ hội'],
 // ---- Quang Ninh
 ['Vịnh Hạ Long', 'Hạ Long', 'Quảng Ninh', 'N', 20.910, 107.184, 'Vịnh có hàng nghìn đảo đá vôi, di sản thiên nhiên thế giới; du khách thường đi tàu tham quan hang động và ngủ đêm trên vịnh.', 'vịnh di sản thuyền hang động'],
 ['Bãi Cháy', 'Hạ Long', 'Quảng Ninh', 'B', 20.958, 107.050, 'Khu bãi tắm và khu vui chơi ven vịnh, thuận tiện cho nghỉ ngắn ngày.', 'bãi biển nghỉ dưỡng'],
 ['Yên Tử', 'Uông Bí', 'Quảng Ninh', 'S', 21.163, 106.715, 'Dãy núi gắn với Thiền phái Trúc Lâm, có đường hành hương và cáp treo lên các chùa trên cao.', 'núi chùa hành hương'],
 ['Đảo Cô Tô', 'Cô Tô', 'Quảng Ninh', 'B', 20.989, 107.763, 'Quần đảo xa bờ với bãi cát dài và nước trong, phù hợp nghỉ dưỡng yên tĩnh.', 'đảo biển nghỉ dưỡng'],
 // ---- Hai Phong
 ['Đảo Cát Bà', 'Cát Bà', 'Hải Phòng', 'B', 20.725, 107.049, 'Hòn đảo lớn nhất vịnh Hạ Long – Cát Bà, có rừng quốc gia, bãi tắm và điểm đi thuyền ngắm vịnh.', 'đảo biển rừng quốc gia'],
 ['Biển Đồ Sơn', 'Đồ Sơn', 'Hải Phòng', 'B', 20.700, 106.783, 'Khu nghỉ biển gần trung tâm thành phố, thuận tiện đi về trong ngày.', 'biển nghỉ dưỡng'],
 // ---- Ninh Binh
 ['Khu du lịch sinh thái Tràng An', 'Ninh Bình', 'Ninh Bình', 'N', 20.251, 105.900, 'Quần thể núi đá vôi, hang động và sông nước, du khách đi thuyền qua nhiều hang xuyên núi.', 'di sản thuyền hang động'],
 ['Tam Cốc – Bích Động', 'Ninh Bình', 'Ninh Bình', 'N', 20.215, 105.936, 'Đi thuyền dọc sông Ngô Đồng giữa ruộng lúa và núi đá, ghé chùa Bích Động trên sườn núi.', 'thuyền ruộng lúa chùa'],
 ['Cố đô Hoa Lư', 'Ninh Bình', 'Ninh Bình', 'L', 20.288, 105.906, 'Di tích kinh đô thế kỷ X với các đền thờ vua nhà Đinh và nhà Tiền Lê.', 'di tích cố đô đền'],
 ['Chùa Bái Đính', 'Ninh Bình', 'Ninh Bình', 'S', 20.276, 105.863, 'Quần thể chùa quy mô lớn với nhiều tượng và hành lang dài trên triền núi.', 'chùa tâm linh quy mô lớn'],
 ['Vườn quốc gia Cúc Phương', 'Ninh Bình', 'Ninh Bình', 'N', 20.320, 105.610, 'Khu rừng nguyên sinh với đường mòn đi bộ, trung tâm cứu hộ linh trưởng và cây cổ thụ.', 'rừng quốc gia đi bộ'],
 // ---- Lao Cai
 ['Thị trấn Sa Pa', 'Sa Pa', 'Lào Cai', 'N', 22.336, 103.844, 'Thị trấn vùng cao khí hậu mát, điểm xuất phát đi bản làng và ruộng bậc thang.', 'vùng cao ruộng bậc thang'],
 ['Đỉnh Fansipan', 'Sa Pa', 'Lào Cai', 'N', 22.303, 103.775, 'Đỉnh núi cao nhất Việt Nam, có tuyến cáp treo và đường leo núi dành cho người đi trekking.', 'núi cáp treo trekking'],
 ['Bản Cát Cát', 'Sa Pa', 'Lào Cai', 'V', 22.326, 103.833, 'Bản người Mông gần thị trấn, có đường đi bộ xuống thung lũng, thác nước và nghề dệt thổ cẩm.', 'bản làng thổ cẩm đi bộ'],
 ['Thung lũng Mường Hoa', 'Sa Pa', 'Lào Cai', 'N', 22.296, 103.884, 'Thung lũng có ruộng bậc thang và bãi đá cổ khắc hình, phù hợp trekking nhẹ.', 'ruộng bậc thang trekking'],
 // ---- Ha Giang, Cao Bang, others north
 ['Cao nguyên đá Đồng Văn', 'Đồng Văn', 'Hà Giang', 'N', 23.277, 105.361, 'Công viên địa chất với những dãy núi đá tai mèo, phố cổ Đồng Văn và các bản làng vùng cao.', 'cao nguyên đá địa chất'],
 ['Cột cờ Lũng Cú', 'Đồng Văn', 'Hà Giang', 'L', 23.364, 105.316, 'Cột cờ nơi địa đầu Tổ quốc, nằm trên đỉnh núi nhìn xuống vùng biên giới.', 'cột cờ biên giới'],
 ['Đèo Mã Pí Lèng', 'Mèo Vạc', 'Hà Giang', 'N', 23.233, 105.411, 'Cung đèo uốn lượn trên vách đá, nhìn xuống sông Nho Quế rất sâu.', 'đèo cảnh quan phượt'],
 ['Thác Bản Giốc', 'Trùng Khánh', 'Cao Bằng', 'N', 22.856, 106.723, 'Thác nước lớn nằm trên biên giới, đẹp nhất vào mùa nước lớn.', 'thác biên giới'],
 ['Hồ Ba Bể', 'Ba Bể', 'Bắc Kạn', 'N', 22.417, 105.617, 'Hồ nước ngọt lớn trong vườn quốc gia, đi thuyền qua hang Puông và thác Đầu Đẳng.', 'hồ vườn quốc gia thuyền'],
 ['Mù Cang Chải', 'Mù Cang Chải', 'Yên Bái', 'N', 21.850, 104.083, 'Vùng ruộng bậc thang nổi tiếng, đẹp nhất vào mùa lúa chín và mùa nước đổ.', 'ruộng bậc thang vùng cao'],
 ['Mộc Châu', 'Mộc Châu', 'Sơn La', 'N', 20.833, 104.633, 'Cao nguyên có đồi chè, đồng cỏ, vườn hoa và khí hậu mát quanh năm.', 'cao nguyên đồi chè'],
 ['Mai Châu', 'Mai Châu', 'Hòa Bình', 'N', 20.660, 105.050, 'Thung lũng có nhà sàn người Thái, thích hợp nghỉ homestay và đạp xe giữa ruộng lúa.', 'thung lũng homestay nhà sàn'],
 ['Di tích chiến trường Điện Biên Phủ', 'Điện Biên Phủ', 'Điện Biên', 'L', 21.394, 103.015, 'Cụm di tích gồm đồi A1, hầm Đại tá De Castries và bảo tàng chiến thắng.', 'di tích lịch sử chiến thắng'],
 ['Tam Đảo', 'Tam Đảo', 'Vĩnh Phúc', 'N', 21.453, 105.643, 'Thị trấn trên núi mát mẻ, gần Hà Nội, phù hợp nghỉ cuối tuần.', 'núi nghỉ dưỡng mát mẻ'],
 ['Đền Hùng', 'Việt Trì', 'Phú Thọ', 'S', 21.339, 105.311, 'Quần thể đền thờ các vua Hùng trên núi Nghĩa Lĩnh, nơi diễn ra giỗ Tổ hằng năm.', 'đền lễ hội lịch sử'],
 ['Biển Sầm Sơn', 'Sầm Sơn', 'Thanh Hóa', 'B', 19.733, 105.900, 'Bãi biển lâu đời ở miền Bắc, nhiều khách sạn và quán hải sản.', 'biển hải sản nghỉ dưỡng'],
 ['Thành nhà Hồ', 'Vĩnh Lộc', 'Thanh Hóa', 'L', 20.076, 105.604, 'Tòa thành đá cuối thế kỷ XIV, di sản văn hóa thế giới với cổng thành xây bằng đá lớn.', 'di sản thành cổ'],
 ['Khu bảo tồn Pù Luông', 'Pù Luông', 'Thanh Hóa', 'N', 20.417, 105.233, 'Vùng núi có ruộng bậc thang, suối và bản làng, phù hợp trekking và nghỉ giữa thiên nhiên.', 'rừng ruộng bậc thang trekking'],
 ['Khu di tích Kim Liên', 'Nam Đàn', 'Nghệ An', 'L', 18.671, 105.681, 'Quê nội và quê ngoại của Chủ tịch Hồ Chí Minh, có nhà lưu niệm và khu vườn.', 'di tích lịch sử'],
 ['Biển Cửa Lò', 'Cửa Lò', 'Nghệ An', 'B', 18.817, 105.717, 'Bãi tắm dài với nhiều khách sạn và nhà hàng hải sản, đông vào mùa hè.', 'biển hải sản nghỉ dưỡng'],
 // ---- Quang Binh / Hue
 ['Động Phong Nha', 'Phong Nha', 'Quảng Bình', 'N', 17.593, 106.285, 'Hang động nổi tiếng trong vườn quốc gia Phong Nha – Kẻ Bàng, tham quan bằng thuyền đi trên sông ngầm.', 'hang động di sản thuyền'],
 ['Bãi biển Nhật Lệ', 'Đồng Hới', 'Quảng Bình', 'B', 17.483, 106.617, 'Bãi biển ngay thành phố, phù hợp nghỉ ngắn và ăn hải sản địa phương.', 'biển hải sản'],
 ['Thành cổ Quảng Trị', 'Đông Hà', 'Quảng Trị', 'L', 16.753, 107.196, 'Di tích lịch sử, nơi diễn ra nhiều trận đánh năm 1972, có đài tưởng niệm và khu bảo tàng.', 'di tích lịch sử'],
 ['Đại Nội Huế', 'Huế', 'Thừa Thiên Huế', 'L', 16.470, 107.579, 'Kinh thành triều Nguyễn với cung điện, cổng thành và các khu vườn, di sản văn hóa thế giới.', 'di sản cố đô cung đình'],
 ['Chùa Thiên Mụ', 'Huế', 'Thừa Thiên Huế', 'S', 16.454, 107.544, 'Ngôi chùa cổ bên bờ sông Hương với tháp bảy tầng nổi tiếng.', 'chùa sông Hương'],
 ['Lăng Khải Định', 'Huế', 'Thừa Thiên Huế', 'L', 16.401, 107.590, 'Lăng vua có kiến trúc kết hợp phong cách Đông – Tây, nhiều chi tiết trang trí công phu.', 'lăng tẩm kiến trúc'],
 ['Lăng Tự Đức', 'Huế', 'Thừa Thiên Huế', 'L', 16.438, 107.565, 'Quần thể lăng trong khuôn viên có hồ nước và rừng thông, từng là nơi vua nghỉ ngơi.', 'lăng tẩm hồ'],
 ['Sông Hương', 'Huế', 'Thừa Thiên Huế', 'N', 16.467, 107.583, 'Dòng sông chảy qua thành phố, du khách thường đi thuyền rồng nghe ca Huế buổi tối.', 'sông thuyền ca Huế'],
 ['Biển Lăng Cô', 'Phú Lộc', 'Thừa Thiên Huế', 'B', 16.250, 108.083, 'Vịnh biển đẹp dưới chân đèo Hải Vân với bãi cát dài và đầm Lập An gần đó.', 'biển vịnh đèo'],
 ['Vườn quốc gia Bạch Mã', 'Phú Lộc', 'Thừa Thiên Huế', 'N', 16.199, 107.856, 'Khu rừng quốc gia trên núi cao với thác nước, đường mòn đi bộ và khí hậu mát.', 'rừng quốc gia thác trekking'],
 // ---- Da Nang
 ['Bà Nà Hills và Cầu Vàng', 'Đà Nẵng', 'Đà Nẵng', 'G', 15.995, 107.996, 'Khu du lịch trên núi với cáp treo, cầu Vàng, làng Pháp và các khu vui chơi.', 'núi cáp treo vui chơi'],
 ['Bán đảo Sơn Trà', 'Đà Nẵng', 'Đà Nẵng', 'N', 16.117, 108.283, 'Khu bảo tồn thiên nhiên trên bán đảo có rừng, bãi biển nhỏ và các điểm ngắm toàn cảnh thành phố.', 'bán đảo rừng ngắm cảnh'],
 ['Ngũ Hành Sơn', 'Đà Nẵng', 'Đà Nẵng', 'S', 16.003, 108.263, 'Cụm núi đá vôi có hang động và chùa chiền, kèm làng nghề đá mỹ nghệ ở chân núi.', 'núi đá hang động chùa'],
 ['Cầu Rồng', 'Đà Nẵng', 'Đà Nẵng', 'G', 16.061, 108.228, 'Cây cầu hình rồng bắc qua sông Hàn, phun lửa và nước vào tối cuối tuần theo lịch.', 'cầu biểu tượng buổi tối'],
 ['Biển Mỹ Khê', 'Đà Nẵng', 'Đà Nẵng', 'B', 16.050, 108.250, 'Bãi biển dài ngay trong thành phố, nước trong, nhiều quán ăn và khách sạn.', 'biển tắm biển'],
 ['Bảo tàng Điêu khắc Chăm', 'Đà Nẵng', 'Đà Nẵng', 'V', 16.060, 108.222, 'Bảo tàng lưu giữ bộ sưu tập hiện vật điêu khắc đá và đất nung của người Chăm.', 'bảo tàng văn hóa Chăm'],
 ['Chùa Linh Ứng Sơn Trà', 'Đà Nẵng', 'Đà Nẵng', 'S', 16.100, 108.278, 'Ngôi chùa trên bán đảo Sơn Trà với tượng Quan Âm cao nhìn ra biển.', 'chùa tượng Quan Âm'],
 // ---- Quang Nam
 ['Thánh địa Mỹ Sơn', 'Duy Xuyên', 'Quảng Nam', 'L', 15.764, 108.124, 'Quần thể đền tháp Chăm trong thung lũng, di sản văn hóa thế giới.', 'di sản đền tháp Chăm'],
 ['Cù Lao Chàm', 'Hội An', 'Quảng Nam', 'B', 15.950, 108.517, 'Cụm đảo ngoài khơi Hội An, khu dự trữ sinh quyển, nổi tiếng với lặn ngắm san hô.', 'đảo biển lặn san hô'],
 ['Rừng dừa Bảy Mẫu', 'Hội An', 'Quảng Nam', 'N', 15.876, 108.361, 'Rừng dừa nước ven sông, du khách đi thuyền thúng và xem các màn trình diễn trên thuyền.', 'rừng dừa thuyền thúng'],
 ['Làng gốm Thanh Hà', 'Hội An', 'Quảng Nam', 'V', 15.873, 108.305, 'Làng nghề gốm cổ gần phố cổ, có công viên gốm và lớp nặn gốm cho du khách.', 'làng nghề gốm trải nghiệm'],
 ['Làng rau Trà Quế', 'Hội An', 'Quảng Nam', 'V', 15.904, 108.331, 'Làng trồng rau, du khách có thể đạp xe, tham gia làm vườn và học nấu món địa phương.', 'làng rau trải nghiệm ẩm thực'],
 ['Biển An Bàng', 'Hội An', 'Quảng Nam', 'B', 15.903, 108.373, 'Bãi biển gần Hội An, yên tĩnh hơn các bãi lớn, nhiều quán ăn ven biển.', 'biển quán ăn'],
 ['Chùa Cầu Hội An', 'Hội An', 'Quảng Nam', 'V', 15.877, 108.326, 'Cây cầu có mái che là biểu tượng của phố cổ, xây từ thế kỷ XVII.', 'cầu biểu tượng phố cổ'],
 // ---- Quang Ngai / Binh Dinh / Phu Yen
 ['Đảo Lý Sơn', 'Lý Sơn', 'Quảng Ngãi', 'B', 15.383, 109.117, 'Đảo núi lửa ngoài khơi với cánh đồng tỏi, bờ biển đá và nhiều điểm ngắm cảnh.', 'đảo biển núi lửa'],
 ['Biển Quy Nhơn', 'Quy Nhơn', 'Bình Định', 'B', 13.766, 109.240, 'Bãi biển cong dọc thành phố, thuận tiện đi dạo và ăn hải sản.', 'biển hải sản'],
 // ---- Khanh Hoa
 ['Vịnh Nha Trang', 'Nha Trang', 'Khánh Hòa', 'B', 12.200, 109.233, 'Vịnh có nhiều hòn đảo, du khách thường đi tàu tham quan, lặn biển và tắm biển.', 'vịnh đảo lặn biển'],
 ['Tháp Bà Ponagar', 'Nha Trang', 'Khánh Hòa', 'L', 12.265, 109.196, 'Cụm tháp Chăm trên đồi bên sông Cái, vẫn là nơi cúng bái của người dân địa phương.', 'tháp Chăm di tích'],
 ['Hòn Chồng', 'Nha Trang', 'Khánh Hòa', 'N', 12.270, 109.205, 'Bãi đá lớn ven biển với những khối đá chồng lên nhau, điểm ngắm bình minh quen thuộc.', 'đá biển ngắm cảnh'],
 ['Viện Hải dương học Nha Trang', 'Nha Trang', 'Khánh Hòa', 'V', 12.206, 109.219, 'Viện nghiên cứu có thủy cung và bảo tàng sinh vật biển, phù hợp đi cùng trẻ em.', 'thủy cung bảo tàng'],
 // ---- Ninh Thuan / Binh Thuan
 ['Tháp Chàm Po Klong Garai', 'Phan Rang – Tháp Chàm', 'Ninh Thuận', 'L', 11.598, 108.953, 'Cụm tháp Chăm cổ trên đồi, nơi diễn ra lễ hội Katê của người Chăm.', 'tháp Chăm lễ hội'],
 ['Mũi Né', 'Phan Thiết', 'Bình Thuận', 'B', 10.933, 108.283, 'Làng chài và khu nghỉ biển nổi tiếng với lướt ván diều, đồi cát và hải sản.', 'biển lướt ván hải sản'],
 ['Đồi cát bay Mũi Né', 'Phan Thiết', 'Bình Thuận', 'N', 10.953, 108.307, 'Những đồi cát lớn đổi hình theo gió, hay trượt cát và ngắm bình minh.', 'đồi cát trượt cát bình minh'],
 // ---- Lam Dong (Da Lat)
 ['Hồ Xuân Hương', 'Đà Lạt', 'Lâm Đồng', 'N', 11.944, 108.442, 'Hồ nằm giữa trung tâm Đà Lạt, có đường dạo quanh hồ và đạp vịt.', 'hồ trung tâm dạo bộ'],
 ['Chợ Đà Lạt', 'Đà Lạt', 'Lâm Đồng', 'V', 11.944, 108.438, 'Chợ trung tâm bán đặc sản, nông sản và đồ lưu niệm, nhiều quán ăn vặt quanh chợ.', 'chợ đặc sản ẩm thực'],
 ['Biệt thự Hằng Nga (Crazy House)', 'Đà Lạt', 'Lâm Đồng', 'V', 11.935, 108.430, 'Công trình kiến trúc độc đáo với nhiều ngõ ngách và hình khối lạ, vừa là khách sạn vừa là điểm tham quan.', 'kiến trúc độc đáo'],
 ['Núi Langbiang', 'Đà Lạt', 'Lâm Đồng', 'N', 12.046, 108.440, 'Khu núi cao gần Đà Lạt, có đường lên đỉnh và nhiều trò chơi ngoài trời.', 'núi ngắm cảnh trekking'],
 ['Thác Datanla', 'Đà Lạt', 'Lâm Đồng', 'N', 11.909, 108.449, 'Thác nước trong rừng thông, có máng trượt và đường đi bộ xuống thác.', 'thác trượt máng'],
 ['Thiền viện Trúc Lâm Đà Lạt', 'Đà Lạt', 'Lâm Đồng', 'S', 11.901, 108.438, 'Thiền viện trên đồi gần hồ Tuyền Lâm, có vườn hoa và không gian yên tĩnh.', 'thiền viện hồ yên tĩnh'],
 ['Ga Đà Lạt', 'Đà Lạt', 'Lâm Đồng', 'V', 11.946, 108.456, 'Ga xe lửa cổ với kiến trúc đặc trưng, có chuyến tàu du lịch ngắn đến Trại Mát.', 'ga cổ tàu hỏa'],
 ['Hồ Tuyền Lâm', 'Đà Lạt', 'Lâm Đồng', 'N', 11.904, 108.427, 'Hồ lớn ngoài trung tâm, quanh hồ có cáp treo, khu cắm trại và chèo thuyền kayak.', 'hồ cáp treo cắm trại'],
 // ---- Tay Nguyen others
 ['Nhà thờ gỗ Kon Tum', 'Kon Tum', 'Kon Tum', 'V', 14.352, 108.006, 'Nhà thờ xây bằng gỗ theo kiến trúc nhà rông, mang nét văn hóa Tây Nguyên.', 'nhà thờ kiến trúc gỗ'],
 // ---- HCM
 ['Dinh Độc Lập', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'L', 10.777, 106.696, 'Công trình gắn với thời điểm kết thúc chiến tranh, nay là di tích với phòng họp, hầm và tầng thượng.', 'di tích lịch sử'],
 ['Nhà thờ Đức Bà', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'V', 10.780, 106.699, 'Nhà thờ gạch đỏ do người Pháp xây, đối diện Bưu điện Trung tâm.', 'nhà thờ kiến trúc Pháp'],
 ['Bưu điện Trung tâm Sài Gòn', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'V', 10.780, 106.700, 'Tòa nhà kiến trúc Pháp vẫn hoạt động, bên trong có mái vòm lớn và các bản đồ cổ.', 'kiến trúc Pháp'],
 ['Chợ Bến Thành', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'V', 10.772, 106.698, 'Chợ lớn nhất trung tâm, bán đồ lưu niệm, vải, đặc sản và có khu ăn uống bên trong.', 'chợ mua sắm ẩm thực'],
 ['Phố đi bộ Nguyễn Huệ', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'G', 10.774, 106.704, 'Đại lộ đi bộ ở trung tâm, nơi tổ chức nhiều sự kiện và dạo chơi buổi tối.', 'phố đi bộ sự kiện buổi tối'],
 ['Địa đạo Củ Chi', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'L', 11.143, 106.464, 'Hệ thống địa đạo trong thời kháng chiến, du khách có thể chui thử một đoạn hầm và xem hiện vật.', 'di tích địa đạo lịch sử'],
 ['Bảo tàng Chứng tích Chiến tranh', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'L', 10.779, 106.692, 'Bảo tàng trưng bày hình ảnh và hiện vật về chiến tranh Việt Nam.', 'bảo tàng lịch sử'],
 ['Chùa Bà Thiên Hậu', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'S', 10.753, 106.660, 'Ngôi chùa của người Hoa ở Chợ Lớn, nhiều vòng hương treo và hoa văn gốm.', 'chùa người Hoa Chợ Lớn'],
 ['Thảo Cầm Viên Sài Gòn', 'TP. Hồ Chí Minh', 'Hồ Chí Minh', 'G', 10.788, 106.705, 'Vườn thú và vườn thực vật lâu đời ở trung tâm, phù hợp đi cùng gia đình.', 'vườn thú gia đình'],
 // ---- Mekong & south
 ['Côn Đảo', 'Côn Đảo', 'Bà Rịa - Vũng Tàu', 'B', 8.693, 106.609, 'Quần đảo có di tích nhà tù và bãi biển hoang sơ, nhiều điểm lặn và rừng quốc gia.', 'đảo biển di tích'],
 ['Bãi Sau Vũng Tàu', 'Vũng Tàu', 'Bà Rịa - Vũng Tàu', 'B', 10.340, 107.090, 'Bãi biển dài gần Sài Gòn, thuận tiện đi về trong ngày hoặc cuối tuần.', 'biển cuối tuần'],
 ['Đảo Phú Quốc', 'Phú Quốc', 'Kiên Giang', 'B', 10.290, 103.984, 'Đảo lớn nhất Việt Nam, có bãi biển, rừng quốc gia, chợ đêm và nhiều khu nghỉ dưỡng.', 'đảo biển nghỉ dưỡng'],
 ['Bãi Sao', 'Phú Quốc', 'Kiên Giang', 'B', 10.052, 104.038, 'Bãi biển cát trắng mịn ở phía nam đảo, nước nông và trong.', 'biển cát trắng'],
 ['Chợ đêm Phú Quốc (Dinh Cậu)', 'Phú Quốc', 'Kiên Giang', 'V', 10.215, 103.967, 'Khu chợ đêm gần Dinh Cậu bán hải sản, đặc sản và đồ lưu niệm.', 'chợ đêm hải sản'],
 ['Bến Ninh Kiều', 'Cần Thơ', 'Cần Thơ', 'V', 10.033, 105.788, 'Bến sông trung tâm Cần Thơ, nơi ngắm cầu Cần Thơ và đi thuyền du lịch.', 'bến sông thuyền'],
 ['Chợ nổi Cái Răng', 'Cần Thơ', 'Cần Thơ', 'V', 9.998, 105.746, 'Chợ nổi buôn bán nông sản trên sông, họp sớm vào buổi sáng.', 'chợ nổi sông nước'],
 ['Mũi Cà Mau', 'Cà Mau', 'Cà Mau', 'N', 8.608, 104.717, 'Điểm cực nam đất liền với rừng ngập mặn và cột mốc tọa độ.', 'cực nam rừng ngập mặn'],
 ['Vườn quốc gia Cát Tiên', 'Tân Phú', 'Đồng Nai', 'N', 11.417, 107.367, 'Khu rừng quốc gia có khu bảo tồn thú, sông và đường mòn đi bộ.', 'rừng quốc gia sinh thái'],
 ['Núi Bà Đen', 'Tây Ninh', 'Tây Ninh', 'S', 11.382, 106.168, 'Ngọn núi cao nhất Nam Bộ, có cáp treo lên chùa Bà và quần thể chùa trên núi.', 'núi chùa cáp treo'],
];
const sq = s => "N'" + String(s).replace(/'/g, "''") + "'";
const rows = D.map(r => ({ ten: r[0], vung: r[1], tinh: r[2], loai: T[r[3]], lat: r[4], lng: r[5], mota: r[6], tags: r[7] }));
const today = new Date().toISOString().slice(0, 10);

let a = '-- Nhap khu du lich / dia diem noi tieng (nguon: bang DiaDiemDuLich). Chay lap an toan: bo qua dia diem da co cung ten va khu vuc.\r\nSET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON;\r\nBEGIN TRANSACTION;\r\n';
a += 'CREATE TABLE #kdl(Ten nvarchar(200) COLLATE DATABASE_DEFAULT,Vung nvarchar(200) COLLATE DATABASE_DEFAULT,Tinh nvarchar(100) COLLATE DATABASE_DEFAULT,Loai nvarchar(50) COLLATE DATABASE_DEFAULT,MoTa nvarchar(1400) COLLATE DATABASE_DEFAULT);\r\nINSERT #kdl VALUES\r\n';
a += rows.map(r => `(${sq(r.ten)},${sq(r.vung)},${sq(r.tinh)},${sq(r.loai)},${sq(r.mota)})`).join(',\r\n') + ';\r\n';
a += `INSERT dbo.DiaDiemDuLich(TenDiaDiem,LoaiDiaDiem,KhuVuc,DonViId,DiaChi,MoTa,DangHienThi)
SELECT k.Ten,k.Loai,k.Vung,
 (SELECT TOP(1) u.Id FROM dbo.DonViHanhChinh u WHERE u.Regime=N'Cu' AND u.Level=1 AND u.Status=N'Active' AND (u.Name=k.Tinh OR u.Name=REPLACE(k.Tinh,N'Hồ Chí Minh',N'Thành phố Hồ Chí Minh') OR u.Name LIKE N'%'+k.Tinh+N'%') ORDER BY CASE WHEN u.Name=k.Tinh THEN 0 ELSE 1 END),
 NULL,k.MoTa,1
FROM #kdl k WHERE NOT EXISTS(SELECT 1 FROM dbo.DiaDiemDuLich d WHERE d.TenDiaDiem=k.Ten AND d.KhuVuc=k.Vung);
DROP TABLE #kdl;
COMMIT TRANSACTION;
`;
let b = '-- Bo sung muc Kham pha cho cac dia diem vua nhap: toa do gan dung, tu khoa, nguon, ngay kiem tra. Chay sau khi may chu da tao muc Kham pha (khoang 1-2 phut).\r\nSET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON;\r\nBEGIN TRANSACTION;\r\n';
b += 'CREATE TABLE #bs(Ten nvarchar(200) COLLATE DATABASE_DEFAULT,Vung nvarchar(200) COLLATE DATABASE_DEFAULT,Lat decimal(9,6),Lng decimal(9,6),Tags nvarchar(1000) COLLATE DATABASE_DEFAULT);\r\nINSERT #bs VALUES\r\n';
b += rows.map(r => `(${sq(r.ten)},${sq(r.vung)},${r.lat},${r.lng},${sq('Tham quan ' + r.tags)})`).join(',\r\n') + ';\r\n';
b += `UPDATE e SET e.Latitude=s.Lat,e.Longitude=s.Lng,e.Tags=s.Tags,
 e.LocationNote=N'Tọa độ gần đúng, nên kiểm tra lại trên bản đồ trước khi đến.',
 e.SourceName=N'Tổng hợp từ thông tin du lịch công khai, biên soạn lại',e.CheckedOn='${today}',e.UpdatedAt=SYSUTCDATETIME()
FROM dbo.DiscoveryEntry e JOIN dbo.DiaDiemDuLich d ON d.MaDiaDiem=e.PlaceId JOIN #bs s ON s.Ten=d.TenDiaDiem AND s.Vung=d.KhuVuc
WHERE e.Kind=N'Place' AND e.Latitude IS NULL;
DROP TABLE #bs;
COMMIT TRANSACTION;
`;
fs.writeFileSync(path.join(__dirname, 'khu-du-lich-1-nguon.sql'), '﻿' + a);
fs.writeFileSync(path.join(__dirname, 'khu-du-lich-2-bosung.sql'), '﻿' + b);
console.log(rows.length + ' dia diem');
