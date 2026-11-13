(function () {
  var dlg = document.getElementById('th-dialog'); if (!dlg) return;
  var body = document.getElementById('th-body'), title = document.getElementById('th-title');
  var planId = dlg.dataset.plan, dest = dlg.dataset.dest || '', groupId = dlg.dataset.group || '';
  var esc = function (s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); };
  var token = function () { var i = document.querySelector('input[name="__RequestVerificationToken"]'); return i ? i.value : ''; };

  // Nhu cau tim noi giup: [ma loai dich vu, nhan, so goi nhanh, tu khoa tim tren ban do]
  var NEEDS = [
    { g: 'Sức khỏe', items: [['BenhVien', 'Bệnh viện', '115', 'bệnh viện'], ['NhaThuoc', 'Nhà thuốc', '', 'nhà thuốc'], ['PhongKham', 'Phòng khám', '', 'phòng khám']] },
    { g: 'An toàn', items: [['CongAn', 'Công an', '113', 'công an'], ['CuuHo', 'Cứu hộ', '114', 'cứu hộ']] },
    { g: 'Tiền và đồ đạc', items: [['Atm', 'ATM', '', 'ATM'], ['DoiTien', 'Đổi tiền', '', 'đổi tiền'], ['SimWifi', 'SIM, Wi-Fi', '', 'cửa hàng sim'], ['GuiHanhLy', 'Gửi hành lý', '', 'gửi hành lý'], ['BuuDien', 'Bưu điện', '', 'bưu điện']] },
    { g: 'Đi lại', items: [['SuaXe', 'Sửa xe', '', 'sửa xe'], ['TramXang', 'Trạm xăng', '', 'trạm xăng'], ['BenXe', 'Bến xe', '', 'bến xe'], ['ThueXe', 'Thuê xe', '', 'thuê xe máy'], ['SanBay', 'Sân bay', '', 'sân bay'], ['GaTau', 'Ga tàu', '', 'ga tàu']] },
    { g: 'Nghỉ và ăn', items: [['LuuTru', 'Chỗ ở khác', '', 'khách sạn'], ['AnUong', 'Quán ăn', '', 'nhà hàng'], ['CaPhe', 'Cà phê, chỗ trú', '', 'quán cà phê'], ['SieuThi', 'Siêu thị', '', 'siêu thị'], ['NhaVeSinh', 'Nhà vệ sinh', '', 'nhà vệ sinh công cộng']] }
  ];
  var findNeed = function (code) { var r = null; NEEDS.forEach(function (g) { g.items.forEach(function (i) { if (i[0] === code) r = i; }); }); return r; };

  // Tinh huong: ma -> [nhan, loai su co, cac buoc, noi nen tim, so goi]
  var BANK = 'Gọi tổng đài ngân hàng để khóa thẻ ngay (số ghi mặt sau thẻ hoặc trên ứng dụng ngân hàng).';
  var SIT = {
    om: ['Ốm, sốt, đau bụng, say nắng', 'SucKhoeCaNhan', ['Nghỉ ở nơi mát, uống đủ nước.', 'Dùng thuốc thông thường nếu bạn đã biết rõ mình dùng được.', 'Đi khám nếu không đỡ sau vài giờ hoặc có sốt cao, nôn nhiều, khó thở.', 'Báo người đi cùng, cho họ biết bạn đang ở đâu.'], ['NhaThuoc', 'PhongKham', 'BenhVien'], '115'],
    chanthuong: ['Chấn thương, gãy, bong gân, tai nạn', 'SucKhoeCaNhan', ['Giữ bình tĩnh, không di chuyển người bị thương nếu nghi gãy xương hoặc chấn thương đầu, cổ, lưng.', 'Gọi cấp cứu 115 nếu nặng.', 'Nếu là tai nạn giao thông: gọi thêm 113, chụp ảnh hiện trường, ghi biển số, báo chủ xe thuê hoặc bảo hiểm.', 'Báo người thân hoặc người đi cùng.'], ['BenhVien', 'CongAn'], '115'],
    ngodoc: ['Ngộ độc thực phẩm, dị ứng', 'SucKhoeCaNhan', ['Ngừng ăn thứ nghi ngờ, uống nước.', 'Đến cơ sở y tế gần nhất, gọi 115 nếu khó thở, sưng môi, mặt, ngất, nôn nhiều.', 'Nhớ lại món đã ăn và giờ ăn để nói với bác sĩ.'], ['BenhVien', 'NhaThuoc'], '115'],
    hetthuoc: ['Hết hoặc quên thuốc đang dùng', 'SucKhoeCaNhan', ['Mang theo vỏ thuốc hoặc đơn thuốc đến nhà thuốc.', 'Nếu là thuốc kê đơn, đến phòng khám gần nhất để được kê lại.'], ['NhaThuoc', 'PhongKham'], ''],
    chich: ['Bị thú cắn, côn trùng chích, rắn cắn', 'SucKhoeCaNhan', ['Rửa vết thương bằng nước sạch và xà phòng, giữ yên chi bị thương.', 'Đến cơ sở y tế ngay, không tự đắp thuốc lạ.', 'Gọi 115 nếu khó thở, sưng nhanh hoặc rắn độc.'], ['BenhVien', 'PhongKham'], '115'],
    duoinuoc: ['Gặp nguy hiểm khi tắm biển, lặn, leo núi', 'SucKhoeCaNhan', ['Gọi cứu hộ hoặc 114/115 ngay, báo rõ vị trí.', 'Không xuống cứu khi không có dụng cụ và kỹ năng.', 'Người được cứu lên: giữ ấm, đưa đến cơ sở y tế.'], ['CuuHo', 'BenhVien'], '114'],
    matvi: ['Mất ví, tiền, thẻ ngân hàng', 'MatGiayToTaiSan', [BANK, 'Báo công an nơi gần nhất để lấy giấy xác nhận mất.', 'Nhờ người thân chuyển tiền qua ứng dụng ngân hàng hoặc đến điểm chuyển tiền.', 'Báo người đi cùng; chia lại các khoản chi trước mắt.'], ['CongAn', 'Atm'], '113'],
    matgiayto: ['Mất CCCD, hộ chiếu, giấy tờ', 'MatGiayToTaiSan', ['Báo công an nơi gần nhất để lấy giấy xác nhận.', 'Chụp lại bản sao giấy tờ (nếu còn ảnh trong điện thoại) gửi cho bản thân và người thân.', 'Hộ chiếu mất ở nước ngoài: liên hệ đại sứ quán hoặc lãnh sự quán của Việt Nam gần nhất.', 'Làm lại giấy tờ tại cơ quan có thẩm quyền; hỏi sớm vì thủ tục có thể mất thời gian.'], ['CongAn'], '113'],
    matdt: ['Mất hoặc hỏng điện thoại', 'MatGiayToTaiSan', ['Dùng điện thoại của người đi cùng để gọi và báo người thân.', 'Khóa hoặc định vị máy qua tài khoản Google hoặc Apple từ thiết bị khác.', 'Gọi nhà mạng để khóa SIM, mua SIM mới nếu cần.', 'Khóa ứng dụng ngân hàng nếu máy mất.'], ['SimWifi', 'CongAn'], ''],
    matdo: ['Mất hành lý, đồ đạc', 'MatGiayToTaiSan', ['Mất ở sân bay, bến xe hoặc ga: làm biên bản ngay tại quầy, giữ thẻ hành lý.', 'Mất ở khách sạn hoặc quán: báo quản lý, nhờ xem camera.', 'Báo công an nếu bị lấy cắp.', 'Mua tạm đồ thiết yếu và giữ hóa đơn để đề nghị bồi thường.'], ['CongAn', 'SieuThi'], '113'],
    hettien: ['Hết tiền mặt, ATM không nhận thẻ', 'HetTienNganSach', ['Thử ATM khác hoặc rút ở quầy ngân hàng.', 'Thanh toán chuyển khoản hoặc quét QR nếu nơi đó nhận.', 'Nhờ người đi cùng ứng trước, ghi vào chia tiền nhóm.'], ['Atm', 'DoiTien'], ''],
    vuot: ['Chi tiêu vượt ngân sách', 'HetTienNganSach', ['Xem lại các hạng mục còn lại trong lịch trình.', 'Bỏ hoặc thay hạng mục đắt bằng lựa chọn rẻ hơn, miễn phí.', 'Ăn ở chợ, quán bình dân; đi bộ hoặc xe buýt thay taxi.'], ['AnUong'], ''],
    lua: ['Bị lừa đảo, móc túi, chặt chém', 'AnToanCaNhan', ['Giữ bình tĩnh, tránh tranh cãi nơi vắng người.', 'Chụp ảnh, ghi biển số, biên lai nếu có.', 'Báo công an gần nhất; gọi đường dây nóng du lịch 1800 1560 để phản ánh.', 'Nếu bị mất thẻ hoặc điện thoại, xử lý thêm mục “Mất ví” hoặc “Mất điện thoại”.'], ['CongAn'], '113'],
    quayroi: ['Bị quấy rối, đe dọa, bị theo dõi', 'AnToanCaNhan', ['Đến nơi đông người, có ánh sáng, vào cửa hàng hoặc khách sạn.', 'Gọi 113 và báo người thân, người đi cùng.', 'Chia sẻ vị trí hiện tại cho người tin cậy.', 'Ghi lại thời gian, đặc điểm người liên quan.'], ['CongAn'], '113'],
    lac: ['Bị lạc, hết pin, trời tối', 'AnToanCaNhan', ['Dừng lại ở nơi an toàn, đừng đi sâu vào nơi hẻo lánh.', 'Dùng bản đồ hoặc hỏi người dân để quay lại điểm quen thuộc.', 'Hết pin: vào cửa hàng, quán cà phê xin sạc nhờ.', 'Gọi 112 nếu ở nơi nguy hiểm; chia sẻ vị trí cho người đi cùng.'], ['CaPhe', 'LuuTru'], '112'],
    thientai: ['Bão, lũ, sạt lở, động đất', 'AnToanCaNhan', ['Theo dõi thông báo của chính quyền và cơ quan khí tượng.', 'Di chuyển đến nơi an toàn, tránh sông suối, đường ngập, sườn dốc.', 'Báo cho nhà cung cấp và người thân; hoãn hoặc dời các hạng mục ngoài trời.', 'Gọi 112 khi cần cứu hộ.'], ['LuuTru', 'CuuHo'], '112'],
    lacnhom: ['Người đi cùng bị lạc', 'NhomDiCung', ['Gọi hoặc nhắn người đó; hỏi họ nhìn thấy gì xung quanh.', 'Hẹn một điểm dễ nhận biết (cổng, quán, khách sạn) và cùng đến.', 'Nếu là trẻ em hoặc người cao tuổi và chưa liên lạc được, báo nhân viên tại chỗ và công an.', 'Báo cho cả nhóm trong nhóm chat chuyến đi.'], ['CongAn'], '113'],
    omnhom: ['Người đi cùng ốm hoặc phải về sớm', 'NhomDiCung', ['Đưa người đó đi khám nếu cần (xem mục “Ốm”).', 'Báo cả nhóm; quyết định ai ở cùng, ai đi tiếp.', 'Cập nhật thành viên và chia lại chi phí của nhóm.'], ['BenhVien', 'NhaThuoc'], ''],
    batdong: ['Bất đồng về lịch trình hoặc chia tiền', 'NhomDiCung', ['Thống nhất nhanh trong nhóm chat chuyến đi.', 'Có thể tách nhóm: mỗi người đi theo lịch của mình rồi hẹn gặp lại.', 'Ghi rõ khoản chung, khoản riêng trong chia tiền nhóm để tính lại sau.'], [], '']
  };
  var SITGROUPS = [
    ['Sức khỏe', ['om', 'chanthuong', 'ngodoc', 'hetthuoc', 'chich', 'duoinuoc']],
    ['Giấy tờ, tiền, đồ đạc', ['matvi', 'matgiayto', 'matdt', 'matdo', 'hettien', 'vuot']],
    ['An toàn', ['lua', 'quayroi', 'lac', 'thientai']],
    ['Nhóm đi cùng', ['lacnhom', 'omnhom', 'batdong']]
  ];

  // Ly do co van de o mot hang muc, theo nhom dich vu: [nhan, loai su co, cac buoc, noi nen tim, so goi]
  var ITEM = {
    Stay: [
      ['Đến nơi không có phòng, bị hủy hoặc đặt trùng', 'LuuTruCoVanDe', ['Đưa xác nhận đặt phòng cho lễ tân, yêu cầu phòng tương đương hoặc hoàn tiền.', 'Nếu không giải quyết được, tìm chỗ ở khác trước và giữ lại hóa đơn để đề nghị hoàn lại.', 'Báo nhà cung cấp trên website.'], ['LuuTru', 'NhaNghi'], ''],
      ['Phòng sai mô tả, bẩn, ồn, hỏng thiết bị', 'LuuTruCoVanDe', ['Chụp ảnh ngay, báo lễ tân và yêu cầu đổi phòng hoặc sửa.', 'Nếu không được, hỏi giảm giá hoặc hoàn phần chênh lệch.', 'Ghi lại sự cố để đánh giá sau chuyến.'], ['LuuTru'], ''],
      ['Bị tính thêm phí không có trong thỏa thuận', 'DichVuCoVanDe', ['Hỏi hóa đơn chi tiết và đối chiếu với xác nhận đặt.', 'Không trả phần chưa thỏa thuận trước khi được giải thích.', 'Chụp ảnh hóa đơn; phản ánh trên website sau đó.'], [], ''],
      ['Mất hoặc quên đồ trong phòng', 'MatGiayToTaiSan', ['Báo quản lý ngay, nhờ kiểm tra camera nếu cần.', 'Quên đồ sau khi trả phòng: gọi khách sạn ngay, nhờ gửi về hoặc cất giữ.', 'Mất cắp: báo công an và lấy biên bản.'], ['CongAn'], '113'],
      ['Nhận hoặc trả phòng không kịp giờ', 'LuuTruCoVanDe', ['Gọi khách sạn báo giờ đến, hỏi phí nhận sớm hoặc trả muộn.', 'Gửi hành lý nếu chưa nhận được phòng.'], ['GuiHanhLy'], '']
    ],
    Move: [
      ['Xe hoặc chuyến không đến', 'DiChuyenGapSuCo', ['Gọi hoặc nhắn nhà cung cấp ngay.', 'Chờ tối đa khoảng 15 phút rồi tự tìm phương tiện khác; giữ biên nhận chi phí để đề nghị hoàn lại.', 'Báo người đang đợi bạn và dời các hạng mục sau cho kịp.'], ['BenXe', 'ThueXe'], ''],
      ['Chuyến bay, tàu, xe bị trễ hoặc hủy', 'DiChuyenGapSuCo', ['Hỏi hãng hoặc nhà xe về đổi chuyến hoặc hoàn tiền; giữ vé và tin nhắn xác nhận.', 'Báo nơi ở, xe đón và người đi cùng giờ mới.', 'Dời hoặc bỏ các hạng mục không kịp.'], ['SanBay', 'GaTau', 'BenXe'], ''],
      ['Lỡ chuyến', 'DiChuyenGapSuCo', ['Đến quầy của hãng hoặc nhà xe ngay, hỏi chuyến kế tiếp.', 'Tìm phương tiện thay thế nếu cần đến kịp hạng mục sau.'], ['BenXe', 'GaTau', 'SanBay'], ''],
      ['Xe hỏng, hết xăng, thủng lốp', 'DiChuyenGapSuCo', ['Dừng ở nơi an toàn, bật đèn khẩn cấp.', 'Tìm tiệm sửa hoặc trạm xăng gần nhất; xe thuê thì gọi chủ xe.', 'Báo người đi cùng nếu sẽ trễ.'], ['SuaXe', 'TramXang'], ''],
      ['Bị chặt chém, tài xế gian lận', 'AnToanCaNhan', ['Chụp biển số, đồng hồ, ghi giờ và lộ trình.', 'Trả theo giá hợp lý nếu đã thỏa thuận, tránh tranh cãi nơi vắng.', 'Gọi 113 nếu bị đe dọa; phản ánh qua 1800 1560.'], ['CongAn'], '113'],
      ['Tai nạn giao thông', 'SucKhoeCaNhan', ['Đảm bảo an toàn, gọi 115 nếu có người bị thương và 113 để lập biên bản.', 'Chụp ảnh hiện trường, ghi biển số.', 'Báo chủ xe thuê hoặc bảo hiểm.'], ['BenhVien', 'CongAn'], '115']
    ],
    Eat: [
      ['Quán đóng cửa hoặc hết món', 'DichVuCoVanDe', ['Chọn quán gần đó để thay.', 'Nếu đã đặt bàn, gọi quán xác nhận hoặc báo hủy.'], ['AnUong', 'CaPhe'], ''],
      ['Chờ quá lâu hoặc quá đông', 'DichVuCoVanDe', ['Hỏi thời gian chờ ước tính.', 'Nếu không kịp lịch, đổi sang quán khác gần đó.'], ['AnUong'], ''],
      ['Bị tính giá cao hơn niêm yết', 'DichVuCoVanDe', ['Hỏi hóa đơn chi tiết, đối chiếu thực đơn.', 'Chụp ảnh thực đơn và hóa đơn; trả theo giá niêm yết.', 'Phản ánh qua 1800 1560 nếu không giải quyết được.'], [], ''],
      ['Ngộ độc, dị ứng sau khi ăn', 'SucKhoeCaNhan', ['Ngừng ăn, uống nước, đến cơ sở y tế gần nhất.', 'Gọi 115 nếu khó thở, sưng môi hoặc mặt, ngất.'], ['BenhVien', 'NhaThuoc'], '115'],
      ['Cần quán hợp chế độ ăn (chay, kiêng)', 'Khac', ['Tìm quán chay hoặc quán có món phù hợp quanh bạn.'], ['AnUong'], '']
    ],
    Place: [
      ['Điểm đóng cửa, bảo trì hoặc hết vé', 'DiemDenKhongPhucVu', ['Hỏi người tại chỗ hoặc gọi số liên hệ xem mở lại khi nào.', 'Chọn điểm tương tự gần đó để thay.', 'Nếu đã mua vé, giữ vé để hỏi hoàn tiền.'], [], ''],
      ['Quá đông, phải chờ lâu', 'DichVuCoVanDe', ['Đổi giờ (sáng sớm hoặc chiều muộn) hoặc đổi điểm khác.', 'Dời hạng mục sau nếu bị trễ.'], [], ''],
      ['Bị ép mua hàng hoặc bị đưa vào quán chỉ định', 'AnToanCaNhan', ['Từ chối lịch sự, không đưa thêm tiền.', 'Ghi lại tên nơi và thời gian, phản ánh qua 1800 1560.'], ['CongAn'], ''],
      ['Cáp treo, tàu, hoạt động ngoài trời ngừng chạy', 'DichVuCoVanDe', ['Hỏi quầy bán vé khi nào chạy lại hoặc hoàn tiền.', 'Chuyển sang hoạt động trong nhà hoặc dời sang ngày khác.'], ['CaPhe'], '']
    ],
    Experience: [
      ['Tour bị hủy hoặc sai chương trình', 'DichVuCoVanDe', ['Giữ xác nhận đặt và chương trình đã hứa.', 'Yêu cầu nhà cung cấp hoàn tiền hoặc tour thay thế.', 'Chụp ảnh, ghi lại điểm khác với cam kết.'], [], ''],
      ['Hướng dẫn viên hoặc xe đón không đến', 'DichVuCoVanDe', ['Gọi nhà cung cấp, hỏi người thay thế.', 'Nếu quá 15 phút chưa có phương án, tự di chuyển và giữ biên nhận chi phí.'], ['ThueXe'], ''],
      ['Bị ép mua hàng, thu thêm tiền', 'AnToanCaNhan', ['Từ chối lịch sự, ghi lại.', 'Phản ánh qua 1800 1560 và trên website.'], ['CongAn'], '']
    ],
    Other: [
      ['Dịch vụ không như đã thỏa thuận', 'DichVuCoVanDe', ['Giữ lại xác nhận, ảnh chụp, tin nhắn.', 'Trao đổi với nhà cung cấp trên website.', 'Nếu không giải quyết được, ghi lại sự cố và phản ánh sau chuyến.'], [], '']
    ]
  };
  var COMMON = [
    ['Thời tiết xấu', 'ThoiTiet', ['Theo dõi thông báo của chính quyền địa phương.', 'Đổi sang hoạt động trong nhà hoặc dời sang ngày khác.', 'Báo nhà cung cấp nếu hoạt động ngoài trời đã đặt.'], ['CaPhe', 'SieuThi'], ''],
    ['Tôi mệt, muốn đổi lịch', 'Khac', ['Nghỉ ngơi trước, quyết định sau.', 'Bỏ hoặc dời hạng mục không cần thiết sang ngày khác.'], [], ''],
    ['Vấn đề khác với hạng mục này', 'DichVuCoVanDe', ['Ghi lại chuyện đang xảy ra để theo dõi.', 'Liên hệ nhà cung cấp hoặc người đi cùng nếu cần.'], [], '']
  ];
  var inferGroup = function (name) {
    var n = (name || '').toLowerCase();
    if (/(khách sạn|homestay|resort|nhà nghỉ|hostel|villa|nhận phòng|trả phòng|nghỉ đêm)/.test(n)) return 'Stay';
    if (/(ăn|quán|nhà hàng|cà phê|phở|bún|cơm|buffet|chợ đêm|đặc sản)/.test(n)) return 'Eat';
    if (/(xe|bay|tàu|taxi|đón|đưa|bến|sân bay|ga |phà|thuê)/.test(n)) return 'Move';
    if (/(tour|lặn|chèo|trải nghiệm|spa|massage|vui chơi)/.test(n)) return 'Experience';
    return 'Place';
  };

  var geo = null, history = [], current = null;
  var link = function (href, text, cls) { return '<a class="' + (cls || 'btn btn-outline') + '" href="' + href + '">' + text + '</a>'; };
  var mapsUrl = function (kw) { return 'https://www.google.com/maps/search/' + encodeURIComponent(kw + (geo ? '' : ' ' + dest)) + (geo ? '/@' + geo.lat + ',' + geo.lng + ',15z' : ''); };
  var km = function (a, b, c, d) { var t = Math.PI / 180, x = (c - a) * t, y = (d - b) * t, h = Math.sin(x / 2) * Math.sin(x / 2) + Math.cos(a * t) * Math.cos(c * t) * Math.sin(y / 2) * Math.sin(y / 2); return 12742 * Math.asin(Math.sqrt(h)); };
  var go = function (view) { if (current) history.push(current); render(view); };
  var render = function (view) { current = view; views[view.v](view); };
  var backBtn = function () { return history.length ? '<p class="th-sub"><button type="button" class="text-link" data-back>← Quay lại</button></p>' : ''; };

  var views = {
    home: function (v) {
      title.textContent = 'Cần giúp gì?';
      var tab = v.tab || 'sit';
      var h = '<div class="th-tabs"><button type="button" class="' + (tab === 'sit' ? 'on' : '') + '" data-tab="sit">Chuyện đang xảy ra</button><button type="button" class="' + (tab === 'need' ? 'on' : '') + '" data-tab="need">Tìm nơi giúp gần tôi</button></div>';
      if (tab === 'sit') {
        h += '<p class="help">Chọn đúng chuyện bạn đang gặp, website chỉ việc nên làm ngay và nơi giúp gần bạn.</p>' + SITGROUPS.map(function (g) {
          return '<h3 class="th-grp">' + g[0] + '</h3><div class="th-reasons">' + g[1].map(function (k) { return '<button type="button" class="th-need" data-sit="' + k + '">' + SIT[k][0] + '</button>'; }).join('') + '</div>';
        }).join('') + '<p class="help th-sub">Hạng mục trong lịch trình gặp vấn đề? Đóng khung này và bấm “Có vấn đề” ở ngay hạng mục đó.</p>';
      } else {
        h += '<p class="help">Chọn thứ bạn cần tìm. Website chỉ nơi gần bạn, kèm số gọi và chỉ đường.</p>' + NEEDS.map(function (g) {
          return '<h3 class="th-grp">' + g.g + '</h3><div class="th-grid">' + g.items.map(function (i) { return '<button type="button" class="th-need" data-need="' + i[0] + '">' + i[1] + '</button>'; }).join('') + '</div>';
        }).join('');
      }
      body.innerHTML = h;
    },
    item: function (v) {
      var ds = v.ds, grp = ds.group || inferGroup(ds.name), list = (ITEM[grp] || ITEM.Other).concat(COMMON);
      title.textContent = 'Có vấn đề với: ' + ds.name;
      body.innerHTML = '<p class="help">Chuyện gì đang xảy ra?</p><div class="th-reasons">' + list.map(function (r, i) { return '<button type="button" class="th-need" data-reason="' + i + '">' + esc(r[0]) + '</button>'; }).join('') + '</div>';
      v.list = list;
    },
    detail: function (v) {
      var r = v.r, ds = v.ds;
      title.textContent = r[0];
      var acts = [];
      if (r[4]) acts.push(link('tel:' + r[4], 'Gọi ' + r[4], 'btn btn-danger-soft'));
      r[3].forEach(function (c) { var n = findNeed(c); if (n) acts.push('<button type="button" class="btn btn-lime" data-need="' + c + '">Tìm “' + esc(n[1]) + '” gần đây</button>'); });
      var more = [];
      if (ds) {
        if (ds.booking) more.push(link('/Bookings/Detail/' + ds.booking, 'Xem yêu cầu đã đặt'), link('/Bookings/Changes/' + ds.booking, 'Đề nghị đổi hoặc hủy'));
        if (ds.service) more.push(link('/tin-nhan/dich-vu/' + ds.service, 'Nhắn nhà cung cấp'));
        more.push(link('/lapkehoach/' + planId, 'Đổi hoặc dời trong kế hoạch'));
      } else more.push(link('/lapkehoach/' + planId, 'Đổi lịch trong kế hoạch'));
      if (groupId) more.push(link('/tin-nhan/' + groupId, 'Báo nhóm đi cùng'));
      more.push(link('/Expenses?planId=' + planId, 'Ghi chi phí phát sinh'));
      body.innerHTML = '<h3 class="th-grp">Nên làm ngay</h3><ol class="th-steps">' + r[2].map(function (s) { return '<li>' + esc(s) + '</li>'; }).join('') + '</ol>' +
        (acts.length ? '<h3 class="th-grp">Gọi hoặc tìm nơi giúp</h3><div class="th-actions">' + acts.join('') + '</div>' : '') +
        '<h3 class="th-grp">Làm tiếp trong chuyến đi</h3><div class="th-actions">' + more.join('') + '<button type="button" class="btn btn-outline" data-record>Ghi lại vào chuyến đi</button></div><p class="help" id="th-rec" role="status"></p>' +
        '<p class="help th-note">Hướng dẫn chung, không thay thế ý kiến của cơ quan chức năng hoặc bác sĩ.</p>' + backBtn();
      v.type = r[1];
    },
    need: function (v) {
      var n = findNeed(v.code); if (!n) return;
      title.textContent = n[1] + ' gần bạn';
      body.innerHTML = '<div class="th-actions">' + (n[2] ? link('tel:' + n[2], 'Gọi ' + n[2], 'btn btn-danger-soft') : '') + '<a class="btn btn-lime" target="_blank" rel="noopener" href="' + esc(mapsUrl(n[3])) + '">Mở bản đồ tìm “' + esc(n[3]) + '”</a></div>' +
        '<p class="help">' + (geo ? 'Đang dùng vị trí hiện tại của bạn.' : 'Chưa có vị trí của bạn, đang tìm quanh ' + esc(dest) + '. <button type="button" class="text-link" data-geo="' + v.code + '">Dùng vị trí hiện tại</button>') + '</p><div id="th-list" class="th-list"><p class="help">Đang tìm…</p></div>' + backBtn();
      load(v.code);
    }
  };

  var load = function (code) {
    var q = '/api/lapkehoach/' + (geo ? 'gan-day?kind=' + code + '&lat=' + geo.lat + '&lng=' + geo.lng : 'goong-goi-y?kind=' + code + '&destination=' + encodeURIComponent(dest));
    fetch(q, { headers: { Accept: 'application/json' } }).then(function (r) { return r.ok ? r.json() : []; }).catch(function () { return []; }).then(function (rows) {
      var box = document.getElementById('th-list'); if (!box) return;
      if (!rows || !rows.length) { box.innerHTML = '<p class="help">Chưa có gợi ý sẵn. Dùng nút bản đồ ở trên để tìm trực tiếp.</p>'; return; }
      rows.forEach(function (r) { r.d = geo ? km(geo.lat, geo.lng, r.lat, r.lng) : null; });
      if (geo) rows.sort(function (a, b) { return a.d - b.d; });
      box.innerHTML = rows.slice(0, 6).map(function (r) {
        return '<article class="th-place"><div><strong>' + esc(r.name) + '</strong><small>' + esc(r.address) + (r.d != null ? ' · ' + r.d.toFixed(1) + ' km' : '') + '</small></div><a class="btn btn-outline" target="_blank" rel="noopener" href="https://www.google.com/maps/dir/?api=1&destination=' + r.lat + ',' + r.lng + '">Chỉ đường</a></article>';
      }).join('');
    });
  };

  var record = function (btn) {
    var v = current, ds = v.ds, msg = document.getElementById('th-rec');
    btn.disabled = true; msg.textContent = 'Đang ghi…';
    var payload = { planId: +planId, type: v.type, description: v.r[0] + (ds ? ' — hạng mục: ' + ds.name : '') + '. Cách xử lý ban đầu: ' + v.r[2][0], activityId: ds && ds.act ? +ds.act : null };
    fetch('/api/lapkehoach/su-co', { method: 'POST', headers: { 'Content-Type': 'application/json', RequestVerificationToken: token() }, body: JSON.stringify(payload) })
      .then(function (r) { return r.json().catch(function () { return {}; }).then(function (j) { return { ok: r.ok, j: j }; }); })
      .then(function (x) {
        if (x.ok) msg.innerHTML = 'Đã ghi vào chuyến đi. <a href="/Support?planId=' + planId + '">Xem và cập nhật cách xử lý</a>.';
        else { btn.disabled = false; msg.textContent = (x.j && (x.j.message || x.j.error)) || 'Chưa ghi được. Chỉ chủ chuyến đi ghi được sự cố.'; }
      }).catch(function () { btn.disabled = false; msg.textContent = 'Chưa ghi được, hãy thử lại.'; });
  };

  dlg.addEventListener('click', function (e) {
    if (e.target === dlg || e.target.closest('[data-help-close]')) { dlg.close(); return; }
    var b;
    if ((b = e.target.closest('[data-tab]'))) { history = []; render({ v: 'home', tab: b.dataset.tab }); return; }
    if ((b = e.target.closest('[data-sit]'))) { var s = SIT[b.dataset.sit]; go({ v: 'detail', r: s, ds: null }); return; }
    if ((b = e.target.closest('[data-need]'))) { go({ v: 'need', code: b.dataset.need }); return; }
    if ((b = e.target.closest('[data-reason]'))) { go({ v: 'detail', r: current.list[+b.dataset.reason], ds: current.ds }); return; }
    if ((b = e.target.closest('[data-record]'))) { record(b); return; }
    if ((b = e.target.closest('[data-back]'))) { var p = history.pop(); if (p) render(p); return; }
    if ((b = e.target.closest('[data-geo]'))) {
      var code = b.dataset.geo; b.textContent = 'Đang lấy vị trí…';
      if (!navigator.geolocation) { b.textContent = 'Trình duyệt không hỗ trợ vị trí'; return; }
      navigator.geolocation.getCurrentPosition(function (p) { geo = { lat: +p.coords.latitude.toFixed(5), lng: +p.coords.longitude.toFixed(5) }; render({ v: 'need', code: code }); }, function () { b.textContent = 'Không lấy được vị trí, hãy cho phép truy cập vị trí'; }, { enableHighAccuracy: true, timeout: 10000 });
    }
  });
  document.addEventListener('click', function (e) {
    var o = e.target.closest('[data-help-open]'); if (o) { history = []; render({ v: 'home', tab: 'sit' }); if (!dlg.open) dlg.showModal(); return; }
    var i = e.target.closest('.trip-issue-btn'); if (i) { history = []; render({ v: 'item', ds: { name: i.dataset.name, service: i.dataset.service || '', booking: i.dataset.booking || '', act: i.dataset.act, group: i.dataset.group || '' } }); if (!dlg.open) dlg.showModal(); }
  });
})();
