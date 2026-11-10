const fs=require('fs'),path=require('path');const root='D:/KLTN/src/HoTroDuLich.Web';
const walk=p=>fs.readdirSync(p,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(p+'/'+e.name):[p+'/'+e.name]);
const edits=[
 ['Một hệ thống đáng tin cậy.','Tổng quan quản trị'],['Trải nghiệm tốt, bắt đầu từ bạn.','Quản lý dịch vụ'],
 ['Chuyến đi, theo cách của bạn.','Chuyến đi của tôi'],['Từ một ý tưởng nhỏ đến lịch trình sẵn sàng lên đường.','Quản lý lịch trình, ngân sách và các hoạt động trong chuyến đi.'],
 ['CHÀO MỪNG BẠN TRỞ LẠI','CHÀO MỪNG TRỞ LẠI'],['Tiếp tục hành trình','Đăng nhập'],['Chuyến đi tiếp theo<br/>đang chờ bạn.','Lên kế hoạch cho<br/>những trải nghiệm mới.'],
 ['Một tài khoản. Nhiều hành trình.','Tạo tài khoản'],['Một chút về bạn','Thông tin cá nhân'],['Bạn muốn đi đâu, cùng ai?','Đăng tin tìm bạn đồng hành'],
 ['Chung điểm đến. Cùng nhịp bước.','Tìm bạn đồng hành'],['Lời hẹn cho một chuyến đi','Bài đăng của tôi'],['Những người bạn sắp gặp','Kết nối của tôi'],
 ['Thống nhất trước khi lên đường','Thỏa thuận đồng hành'],['Bắt đầu bằng một lời chào','Gửi lời mời kết nối'],['Một thay đổi trong kế hoạch?','Yêu cầu thay đổi hoặc hủy dịch vụ'],
 ['Gửi một yêu cầu cho chuyến đi','Yêu cầu đặt dịch vụ'],['Thêm một điều muốn làm','Thêm hoạt động'],['Thêm vào chuyến đi nào?','Chọn chuyến đi'],
 ['Khi hành trình cần một lối khác','Hỗ trợ trong chuyến đi'],['Những điều mới nhất','Thông báo'],['Bạn đã xem hết mọi thứ','Bạn chưa có thông báo mới'],
 ['Đối chiếu trước khi công bố','Xét duyệt thông tin dịch vụ'],['Mỗi quyết định đều có dấu vết','Nhật ký hoạt động'],['200 sự kiện gần nhất. Lịch sử được giữ lại khi trạng thái thay đổi.','Xem lịch sử thao tác và các quyết định trên hệ thống.'],
 ['Nhu cầu của tôi','Bài đăng của tôi'],['nhu cầu đồng hành bạn đã công bố','bài đăng tìm bạn đồng hành của bạn'],['Kết nối & thỏa thuận','Kết nối của tôi'],
 ['Công bố nhu cầu đồng hành','Đăng tin tìm bạn đồng hành'],['Đăng nhu cầu đồng hành','Đăng tin tìm bạn'],['Đăng nhu cầu đầu tiên','Đăng tin đầu tiên'],['Đăng nhu cầu','Đăng tin'],
 ['Bạn chưa đăng nhu cầu nào','Bạn chưa có bài đăng nào'],['Tìm một nhu cầu phù hợp và bắt đầu bằng lời chào.','Xem các bài đăng và gửi lời mời cho người có kế hoạch phù hợp.'],
 ['Tìm những người có cùng kế hoạch để chia sẻ trải nghiệm.','Tìm người có cùng điểm đến, thời gian và sở thích du lịch.'],
 ['Mở rộng khoảng ngày hoặc đăng nhu cầu để người khác tìm thấy bạn.','Thử thay đổi bộ lọc hoặc đăng tin để những người có cùng kế hoạch tìm thấy bạn.'],
 ['Để những người chung điểm đến có thể tìm thấy bạn.','Đăng thông tin chuyến đi để tìm người đồng hành phù hợp.'],['Chia sẻ kế hoạch và chọn phạm vi công bố phù hợp.','Giới thiệu chuyến đi và chọn ai có thể xem bài đăng của bạn.'],
 ['Tiêu đề / nội dung nhu cầu','Giới thiệu chuyến đi'],['Mong muốn về người đồng hành','Bạn muốn tìm người đồng hành như thế nào?'],
 ['Lưu và công bố','Đăng bài'],['Dừng công bố','Ẩn bài đăng'],['Phạm vi chia sẻ','Ai có thể xem bài đăng?'],
 ['Kết quả chỉ gồm nhu cầu còn công bố trong phạm vi bạn được phép xem. Kết nối chưa phải thỏa thuận đi cùng.','Gửi lời mời để trao đổi thêm. Hai bên cần thống nhất kế hoạch trước khi đồng ý đi cùng.'],
 ['Kết nối đã được chấp nhận. Hai bên cần xác nhận cùng một phiên bản thỏa thuận để đồng ý đi cùng.','Bạn đã kết nối thành công. Hãy trao đổi và xác nhận thỏa thuận trước khi đi cùng.'],
 ['Đề nghị cần được người nhận chấp nhận. Sau đó hai bên lập và xác nhận thỏa thuận riêng.','Người nhận sẽ xem và phản hồi lời mời của bạn. Hai bên có thể thống nhất lịch trình sau khi kết nối.'],
 ['Lời giới thiệu và mong muốn đi cùng','Lời nhắn của bạn'],['Gửi đề nghị kết nối','Gửi lời mời kết nối'],['Phản hồi đề nghị và thống nhất chuyến đi cùng nhau.','Quản lý lời mời đã gửi, đã nhận và thỏa thuận đi cùng.'],
 ['Mỗi phiên bản cần được bên còn lại xác nhận.','Trao đổi thời gian, ngân sách và phạm vi đi cùng. Thay đổi chỉ có hiệu lực khi bên còn lại xác nhận.'],
 ['Chi phí đã lên lịch','Tổng chi phí dự kiến'],['Đang lên ý tưởng','Bản nháp'],['Sửa thông tin','Chỉnh sửa'],
 ['Kế hoạch đã chấp nhận vẫn là lịch trình dự kiến. Đặt dịch vụ được thực hiện riêng và cần nhà cung cấp xác nhận.','Lịch trình giúp bạn sắp xếp chuyến đi. Để sử dụng dịch vụ, bạn cần gửi yêu cầu và chờ nhà cung cấp xác nhận.'],
 ['Một nơi muốn đến và một khoảng thời gian dành cho chính mình.','Nhập điểm đến, thời gian và ngân sách dự kiến. Bạn có thể bổ sung các hoạt động sau.'],
 ['Hoạt động phải nằm trong khoảng thời gian của chuyến đi.','Chọn ngày, giờ và dự trù chi phí cho hoạt động.'],['Chi phí là dự kiến. Thêm hoạt động hoặc chấp nhận kế hoạch chưa xác nhận đặt dịch vụ.','Các hoạt động trong lịch trình chưa bao gồm xác nhận đặt dịch vụ.'],
 ['Lịch trình còn đang để ngỏ','Chuyến đi chưa có hoạt động'],['Thêm hoạt động tự nhập hoặc chọn dịch vụ, địa điểm từ trang khám phá.','Tự thêm hoạt động hoặc chọn địa điểm và dịch vụ từ trang khám phá.'],
 ['cảnh báo không chặn lưu.','bạn vẫn có thể lưu lịch trình.'],['Hãy kiểm tra lịch trình; bạn vẫn có thể chấp nhận kế hoạch.','Hãy kiểm tra lại để sắp xếp lịch trình phù hợp.'],['Xem dịch vụ và đặt riêng','Xem và đặt dịch vụ'],
 ['Trước khi bạn lên đường','Thông tin dịch vụ'],['Giá cuối cùng và khả năng phục vụ cần được xác nhận.','Nhà cung cấp sẽ xác nhận giá và khả năng phục vụ khi phản hồi yêu cầu.'],
 ['Gửi yêu cầu đặt dịch vụ','Yêu cầu đặt dịch vụ'],['Thêm vào lịch trình dự kiến','Thêm vào lịch trình'],['Thêm vào kế hoạch chưa tạo yêu cầu đặt dịch vụ. Hệ thống không thu tiền trực tuyến.','Thêm vào lịch trình để lên kế hoạch. Khi muốn sử dụng dịch vụ, hãy gửi yêu cầu đặt riêng.'],
 ['Bản cập nhật được duyệt riêng; nội dung đã công bố giữ nguyên trong lúc chờ.','Gửi thông tin mới để quản trị viên xét duyệt. Nội dung hiện tại vẫn hiển thị trong thời gian chờ.'],
 ['Gửi Admin xét duyệt','Gửi xét duyệt'],['Gửi Admin xem xét','Gửi giải trình'],['Admin','quản trị viên'],
 ['Thao tác chưa có hiệu lực','Chưa thể hoàn tất yêu cầu'],['Không thể hoàn tất thao tác','Không thể hoàn tất yêu cầu'],
 ['Bạn có thể quay lại để sửa thông tin hoặc tải lại dữ liệu mới nhất.','Vui lòng quay lại kiểm tra thông tin hoặc tải lại trang để cập nhật dữ liệu.'],
 ['Ghi nhận sự việc, cân nhắc phương án và theo dõi kết quả thực tế.','Ghi lại sự cố, phương án xử lý và kết quả trong chuyến đi của bạn.'],
 ['Hệ thống ghi nhận và hỗ trợ bạn tự xử lý; không tự liên hệ, điều phối hay bảo đảm phương án đã được thực hiện.','Bạn có thể lưu thông tin sự cố và theo dõi cách xử lý tại đây. Với việc cần hỗ trợ trực tiếp, hãy liên hệ nhà cung cấp hoặc đơn vị phù hợp.'],
 ['Chuyến đi đang yên ổn','Chưa có sự cố nào được ghi nhận'],['Ghi nhận tình huống','Báo cáo sự cố'],['Loại tình huống','Loại sự cố'],
 ['Những phương án đã chọn','Lịch sử phương án xử lý'],['Chọn phương án chưa đồng nghĩa sự việc đã được giải quyết.','Sau khi thực hiện phương án, hãy cập nhật kết quả để theo dõi tiến độ xử lý.'],
 ['Ngừng sử dụng tài khoản','Ngừng sử dụng tài khoản'],['Bạn cần hoàn tất các nghĩa vụ liên quan trước khi ngừng.','Vui lòng xử lý các dịch vụ và thỏa thuận còn đang thực hiện trước khi ngừng tài khoản.'],
 ['Xác thực lại và kiểm tra nghĩa vụ trước khi ngừng.','Xác nhận thông tin và kiểm tra các dịch vụ, thỏa thuận chưa hoàn tất.'],
 ['Một khởi đầu mới','Thông tin'],['Theo dõi yêu cầu, điều chỉnh và kết quả phục vụ.','Theo dõi trạng thái đặt dịch vụ, gửi yêu cầu thay đổi và xem kết quả phục vụ.'],
 ['Đề nghị cần nhà cung cấp chấp nhận trước khi có hiệu lực.','Nhà cung cấp sẽ xem xét và phản hồi yêu cầu thay đổi của bạn.'],
 ['Thỏa thuận hiện tại giữ nguyên cho đến khi đề nghị được chấp nhận. Hệ thống không tự tính hoàn tiền hoặc tự hủy dịch vụ.','Dịch vụ đã xác nhận vẫn giữ nguyên cho đến khi nhà cung cấp đồng ý thay đổi hoặc hủy. Vui lòng trao đổi trực tiếp về các chi phí phát sinh và hoàn tiền, nếu có.'],
 ['Chọn kế hoạch','Chọn chuyến đi'],['Kế hoạch chuyến đi','Chuyến đi của tôi'],['Tạo kế hoạch mới','Tạo chuyến đi'],['Tạo kế hoạch đầu tiên','Tạo chuyến đi đầu tiên'],['Tạo kế hoạch','Tạo chuyến đi'],['Sửa kế hoạch','Chỉnh sửa chuyến đi'],
 ['Tên kế hoạch','Tên chuyến đi'],['Lưu kế hoạch','Lưu chuyến đi'],['Kế hoạch của tôi','Chuyến đi của tôi'],['Chia sẻ hồ sơ','Ai có thể xem hồ sơ?'],
 ['Không có kết nối nào','Chưa có kết nối nào'],['Hàng chờ đã được xử lý','Không có dịch vụ chờ duyệt'],['Chỉnh sửa & lịch sử','Chỉnh sửa'],['Gửi dịch vụ mới','Thêm dịch vụ'],
 ['Không có quyền.','Bạn không có quyền thực hiện yêu cầu này.']
];
// Only replace visible copy. Never change endpoint names, bindings, state values, or business identifiers.
for(const file of walk(root+'/Views').filter(f=>f.endsWith('.cshtml')&&!f.includes('/Shared/_Layout.cshtml'))){let s=fs.readFileSync(file,'utf8');for(const [from,to]of edits){if(from==='Admin')continue;s=s.split(from).join(to);}s=s.replace(/>Admin /g,'>Quản trị viên ').replace(/ Admin /g,' quản trị viên ');fs.writeFileSync(file,s);}
// Avoid repeating a generic section label on every operation screen.
for(const file of walk(root+'/Views').filter(f=>f.endsWith('.cshtml'))){let s=fs.readFileSync(file,'utf8');s=s.replace('<span class="eyebrow">Hành trình của bạn</span>','');fs.writeFileSync(file,s);}
// Preserve the meaning of accepted connections while using an appropriate label for a finalized itinerary.
for(const folder of ['Plans'])for(const file of walk(root+'/Views/'+folder)){let s=fs.readFileSync(file,'utf8').replace(/Labels.Status\((p|Model)\.TrangThai\)/g,'Labels.PlanStatus($1.TrangThai)').replace('value="DaChapNhan">Đã chấp nhận','value="DaChapNhan">Xác nhận lịch trình');fs.writeFileSync(file,s);}
// User-facing metadata for validation messages.
const inputFile=root+'/Models/Inputs.cs';let inputs=fs.readFileSync(inputFile,'utf8');
const names={Email:'Email',Password:'Mật khẩu',ConfirmPassword:'Xác nhận mật khẩu',Name:'Tên',Phone:'Số điện thoại',Organization:'Tên đơn vị',Address:'Địa chỉ',Destination:'Điểm đến',Start:'Ngày bắt đầu',End:'Ngày kết thúc',Budget:'Ngân sách',Content:'Nội dung',Cost:'Chi phí',Description:'Mô tả',Price:'Giá',Reason:'Lý do',People:'Số người',Quantity:'Số lượng',Bio:'Giới thiệu',City:'Nơi sinh sống',Interests:'Sở thích',Wishes:'Mong muốn đồng hành',Scope:'Phạm vi đi cùng',Terms:'Điều kiện phục vụ',Hours:'Thời gian phục vụ',PauseReason:'Lý do tạm ngừng',Type:'Loại hình',Region:'Khu vực',Style:'Hình thức du lịch',Location:'Vị trí',Text:'Nội dung',Evidence:'Căn cứ'};
inputs=inputs.replace(/public (string\??|decimal\?|int|DateOnly\??) (\w+) \{get;set;\}/g,(match,type,name)=>names[name]?'[Display(Name="'+names[name]+'")] '+match:match);fs.writeFileSync(inputFile,inputs);
// Add semantic anchors used by the admin sidebar.
const adminFile=root+'/Views/Admin/Index.cshtml';let admin=fs.readFileSync(adminFile,'utf8').replace('<h2 class="mb-4">Nội dung dịch vụ cần xét duyệt</h2>','<h2 class="mb-4" id="service-reviews">Dịch vụ chờ xét duyệt</h2>').replace('<h2>Hồ sơ xác minh</h2>','<h2 id="verification">Hồ sơ xác minh</h2>').replace('<h2>Quyền hoạt động nhà cung cấp</h2>','<h2 id="provider-rights">Quyền hoạt động nhà cung cấp</h2>');fs.writeFileSync(adminFile,admin);
// Destination photos are illustrative; service-specific imagery uses a category image instead.
const detailFile=root+'/Views/Explore/Detail.cshtml';let detail=fs.readFileSync(detailFile,'utf8');detail=detail.replace(/<div class="detail-art @\(Model.KhuVucPhucVu[\s\S]*?<\/div>/,'<div class="detail-gallery"><img src="@Ui.ServiceImage(Model)" alt="Ảnh minh họa cho @Model.LoaiDichVu" class="detail-cover"/><span>Ảnh minh họa</span></div>');fs.writeFileSync(detailFile,detail);
