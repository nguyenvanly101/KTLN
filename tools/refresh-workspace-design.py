from pathlib import Path

root = Path(__file__).resolve().parents[1] / 'src/HoTroDuLich.Web'

def replace(path, old, new):
    target = root / path
    source = target.read_text(encoding='utf-8-sig')
    if new in source:
        return
    if old not in source:
        raise RuntimeError(f'Missing expected source in {path}: {old[:70]}')
    target.write_text(source.replace(old, new, 1), encoding='utf-8')

replace('Views/Bookings/Index.cshtml', '<div class="stats compact-stats">', '''<partial name="_BookingWorkspaceTabs"/>
<div class="workflow-ribbon" aria-label="Quy trình đặt dịch vụ"><span><b>01</b> Gửi yêu cầu</span>@Ui.Icon("arrow")<span><b>02</b> Nhà cung cấp phản hồi</span>@Ui.Icon("arrow")<span><b>03</b> Xác nhận & theo dõi</span></div>
<div class="stats compact-stats">''')

for name in ('Adjustments', 'Results'):
    replace(f'Views/Bookings/{name}.cshtml', '\n@if(Model.Count==0)', '\n<partial name="_BookingWorkspaceTabs"/>\n@if(Model.Count==0)')
    replace(f'Views/Bookings/{name}.cshtml', '<table class="table">', '<table class="table booking-table" role="table">')
    replace(f'Views/Bookings/{name}.cshtml', '<td>@y.NgayBatDau.ToString', '<td><span class="mobile-cell-label" aria-hidden="true">Ngày sử dụng</span>@y.NgayBatDau.ToString')
    replace(f'Views/Bookings/{name}.cshtml', '<td><span class="badge ', '<td><span class="mobile-cell-label" aria-hidden="true">Trạng thái</span><span class="badge ')

replace('Views/Provider/Index.cshtml', ''' <div class="page-head"><div><span class="eyebrow">KHÔNG GIAN ĐỐI TÁC</span><h1>Quản lý dịch vụ</h1><p>@p.TenDonVi · Theo dõi công bố và tiếp nhận dịch vụ tại một nơi.</p></div>@if(canSubmit){<a class="btn btn-dark" href="/Provider/Edit">@Ui.Icon("plus") Thêm dịch vụ</a>}</div>''', ''' <section class="workspace-hero provider-hero" aria-labelledby="provider-title"><div><span class="eyebrow">ĐỐI TÁC / DỊCH VỤ DU LỊCH</span><h1 id="provider-title">Trải nghiệm tốt.<br/>Bắt đầu từ dịch vụ của bạn.</h1><p>@p.TenDonVi · Quản lý nội dung công bố và theo dõi tiếp nhận dịch vụ.</p>@if(canSubmit){<a class="btn btn-dark" href="/Provider/Edit">@Ui.Icon("plus") Thêm dịch vụ</a>}</div><span class="workspace-emblem" aria-hidden="true">@Ui.Icon("compass")</span></section>
 <nav class="workspace-shortcuts" aria-label="Công việc của đối tác"><a href="/Bookings">@Ui.Icon("ticket") Yêu cầu của khách @Ui.Icon("arrow")</a><a href="/Provider/Verification">@Ui.Icon("shield") Hồ sơ xác minh @Ui.Icon("arrow")</a><a href="/Provider/Rights">@Ui.Icon("users") Quyền hoạt động @Ui.Icon("arrow")</a></nav>''')

replace('Views/Provider/Index.cshtml', '<span class="managed-service-icon">@Ui.Icon(Ui.CategoryIcon(s.LoaiDichVu))</span>', '<img class="managed-service-thumb" src="@Ui.ServiceImage(s)" alt="Ảnh minh họa @DanhMuc.Ten(s.LoaiDichVu)" loading="lazy" width="196" height="176"/>')

replace('Views/Admin/Index.cshtml', ''' <div class="page-head"><div><span class="eyebrow">QUẢN TRỊ HỆ THỐNG</span><h1>Tổng quan quản trị</h1><p>Theo dõi công việc đang chờ và chọn nghiệp vụ cần xử lý.</p></div></div>''', ''' <section class="workspace-hero admin-hero" aria-labelledby="admin-title"><div><span class="eyebrow">QUẢN TRỊ / TỔNG QUAN VẬN HÀNH</span><h1 id="admin-title">Tổng quan quản trị</h1><p>Mọi công việc tại một nơi.<br/>Theo dõi xét duyệt, xác minh và hoạt động của đối tác.</p></div><span class="workspace-emblem" aria-hidden="true">@Ui.Icon("shield")</span></section>
 <div class="admin-priority-label">@Ui.Icon("clock") Công việc & nhà cung cấp</div>''')

print('Updated booking workspaces, provider and administration.')
