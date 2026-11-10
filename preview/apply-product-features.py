from pathlib import Path
root=Path('D:/KLTN/src/HoTroDuLich.Web')
def replace(path,old,new,count=-1):
 file=root/path;s=file.read_text(encoding='utf-8-sig')
 if new in s:return
 if old not in s:raise RuntimeError(f'Cannot find source in {path}: {old[:90]}')
 file.write_text(s.replace(old,new,count),encoding='utf-8')
replace('Services/NghiepVuService.cs','[("TenDichVu", "Name"),','[("DiaChiPhucVu", "Address"), ("DonViGia", "PriceUnit"), ("ChinhSachHuy", "CancellationPolicy"), ("AnhJson", "PhotoPaths"), ("TenDichVu", "Name"),')
replace('Services/Ui.cs','public static string ServiceImage(DichVuDuLich s) =>','public static string ServiceImage(DichVuDuLich s) => ServicePhotos(s.AnhJson).Length>0 ? "/service-photos/"+s.MaDichVu+"/0" :')
replace('Services/Components/QuanLyDichVuService.cs','Ensure(!SameService(previous, i),','await PreparePhotos(i,(string?)previous.GetType().GetProperty("AnhJson")!.GetValue(previous));\n                    Ensure(!SameService(previous, i),')
replace('Services/Components/QuanLyDichVuService.cs','            if (draft || s.PhienBanCongBo == null)\n', '            if (draft) await PreparePhotos(i,s.AnhJson);\n            if (draft || s.PhienBanCongBo == null)\n')
replace('Services/Components/QuanLyDichVuService.cs','    public async Task<DichVuDuLich> LayChiTiet(long id)', '''    private async Task PreparePhotos(ServiceInput input,string? existing)
    {
        input.PhotoPaths=input.RemovePhotos?null:existing;
        Require(input.Photos.Count<=5,"Chọn tối đa 5 ảnh.",400);
        Require(input.Photos.All(f=>new[]{".jpg",".jpeg",".png"}.Contains(Path.GetExtension(f.FileName).ToLowerInvariant())),"Ảnh dịch vụ chỉ nhận JPG/PNG, tối đa 5 MB mỗi ảnh.",400);
        if(input.Photos.Count>0)input.PhotoPaths=JsonSerializer.Serialize((await StoreFiles(input.Photos)).Select(x=>x.Path));
    }

    public async Task<DichVuDuLich> LayChiTiet(long id)''')
replace('Controllers/ProviderController.cs','            Name = d.TenDichVu,','            Address=d.DiaChiPhucVu,PriceUnit=d.DonViGia,CancellationPolicy=d.ChinhSachHuy,PhotoPaths=d.AnhJson,\n            Name = d.TenDichVu,')
replace('Controllers/ProviderController.cs','            input.Name = last.TenDichVu;','            input.Address=last.DiaChiPhucVu;input.PriceUnit=last.DonViGia;input.CancellationPolicy=last.ChinhSachHuy;input.PhotoPaths=last.AnhJson;\n            input.Name = last.TenDichVu;')
replace('Models/TripWorkspace.cs','    List<TripTask> Tasks);','    List<TripTask> Tasks, int PreparationCount=0, int PreparationDone=0, decimal ActualSpent=0);')
replace('Services/TripWorkspaceService.cs','        return new(plan, activities, bookings, names,', '''        var preparation=await db.MucChuanBi.Where(x=>x.MaKeHoach==id).ToListAsync();
        var spent=await db.KhoanChiChuyenDi.Where(x=>x.MaKeHoach==id).SumAsync(x=>x.SoTien);
        var overdue=preparation.Count(x=>!x.DaHoanThanh&&x.HanHoanThanh<TravelService.Today);
        if(overdue>0)tasks.Insert(0,new("Có việc chuẩn bị đã quá hạn",overdue+" việc cần bạn kiểm tra.","/Preparation?planId="+id,"Xem danh sách",true));
        if(plan.NganSachDuKien is decimal limit&&spent>limit)tasks.Insert(0,new("Thực chi đã vượt ngân sách",Labels.Money(spent-limit)+" vượt ngân sách đặt ra.","/Expenses?planId="+id,"Xem khoản chi",true));
        return new(plan, activities, bookings, names,''')
replace('Services/TripWorkspaceService.cs','requestable.ToHashSet(), incidents, tasks);','requestable.ToHashSet(), incidents, tasks, preparation.Count, preparation.Count(x=>x.DaHoanThanh),spent);')
replace('Services/TripWorkspaceService.cs','            else if (service != null)\n                result[activity.MaHoatDong] = new(service.KhuVucPhucVu + ", Việt Nam", false);','            else if (service != null)\n                result[activity.MaHoatDong] = !string.IsNullOrWhiteSpace(service.DiaChiPhucVu) ? new(service.DiaChiPhucVu+", "+service.KhuVucPhucVu+", Việt Nam",true) : new(service.KhuVucPhucVu + ", Việt Nam", false);')
replace('Views/Provider/Edit.cshtml','action="/Provider/Edit" method="post"','action="/Provider/Edit" method="post" enctype="multipart/form-data"')
replace('Views/Provider/Edit.cshtml','<div class="form-grid">','<partial name="/Views/Provider/_ServiceDetailsForm.cshtml" model="Model"/><div class="form-grid">',1)
replace('Views/Shared/_Layout.cshtml','"Plans","Bookings","Support"','"Plans","Bookings","Support","Preparation","Expenses","Saved"',1)
replace('Views/Shared/_Layout.cshtml','var tripArea=new[]{"Plans","Bookings","Support"}', 'var tripArea=new[]{"Plans","Bookings","Support","Preparation","Expenses"}')
replace('Views/Shared/_Layout.cshtml','"Plans"=>"Chuyến đi",','"Preparation"=>"Chuẩn bị","Expenses"=>"Chi tiêu","Saved"=>"Đã lưu","Plans"=>"Chuyến đi",')
replace('Views/Shared/_Layout.cshtml','<link rel="stylesheet" href="~/css/trip-workspace.css" asp-append-version="true"/>','<link rel="stylesheet" href="~/css/trip-workspace.css" asp-append-version="true"/>\n <link rel="stylesheet" href="~/css/product-features.css" asp-append-version="true"/>')
replace('Views/Shared/_Layout.cshtml','TÌM CẢM HỨNG LÊN ĐƯỜNG</span>','TÌM CẢM HỨNG LÊN ĐƯỜNG</span>@if(canPlan){<a href="/Saved">@Ui.Icon("compass")<span>Lựa chọn đã lưu<small>Những nơi và dịch vụ bạn đang cân nhắc</small></span></a>}',1)
replace('Views/Shared/_Layout.cshtml','  @if(traveler){\n   <details class="sidebar-group"','  @if(traveler){\n   <details class="sidebar-group" open="@(route=="Saved")"><summary>@Ui.Icon("compass")<span>Khám phá</span>@Ui.Icon("chevron")</summary><div class="sidebar-subnav"><a href="/">Tìm trải nghiệm</a><a class="@(route=="Saved"?"active":"")" href="/Saved">Lựa chọn đã lưu</a></div></details>\n   <details class="sidebar-group"',1)
replace('Views/Shared/_ServiceCard.cshtml','alt="Ảnh minh họa cho @DanhMuc.Ten(Model.LoaiDichVu)"','alt="@Ui.ServiceImageLabel(Model) · @Model.TenDichVu"')
replace('Views/Shared/_ServiceCard.cshtml','<strong>@Labels.Money(Model.GiaThamKhao)</strong>','<strong>@Labels.Money(Model.GiaThamKhao)</strong>@if(Model.DonViGia!=null){<small class="price-unit">/ @Model.DonViGia</small>}')
replace('Views/Shared/_ServiceCard.cshtml','</div></div>\n</article>','</div><partial name="_SaveChoice" model="@(new SavedInput{ServiceId=Model.MaDichVu,ReturnUrl=Context.Request.Path+Context.Request.QueryString})"/></div>\n</article>')
replace('Views/Explore/Detail.cshtml','alt="Ảnh minh họa cho @DanhMuc.Ten(Model.LoaiDichVu)"','alt="@Ui.ServiceImageLabel(Model) · @Model.TenDichVu"')
replace('Views/Explore/Detail.cshtml','<span>Ảnh minh họa</span></div>','<span>@Ui.ServiceImageLabel(Model)</span></div>\n  @if(Ui.ServicePhotos(Model.AnhJson).Length>1){<div class="service-photo-grid">@for(var photo=1;photo<Ui.ServicePhotos(Model.AnhJson).Length;photo++){<img src="/service-photos/@Model.MaDichVu/@photo" alt="Ảnh dịch vụ @(photo+1)" loading="lazy" width="400" height="300"/>}</div>}')
replace('Views/Explore/Detail.cshtml','<h2>@Labels.Money(Model.GiaThamKhao)</h2>','<h2>@Labels.Money(Model.GiaThamKhao)</h2><p class="price-unit">/ @(Model.DonViGia??"đơn vị giá chưa được công bố")</p>')
replace('Views/Explore/Detail.cshtml','<h3>Điều kiện phục vụ</h3>','<partial name="/Views/Shared/_ServicePublicDetails.cshtml" model="Model"/><h3>Điều kiện phục vụ</h3>')
replace('Views/Explore/Detail.cshtml','  @if(canPlan){','  <partial name="_SaveChoice" model="@(new SavedInput{ServiceId=Model.MaDichVu,ReturnUrl=Context.Request.Path+Context.Request.QueryString})"/>\n  @if(canPlan){',1)
replace('Views/Explore/Place.cshtml',' <div class="trip-place-actions">',' <partial name="_SaveChoice" model="@(new SavedInput{PlaceId=Model.MaDiaDiem,ReturnUrl=Context.Request.Path+Context.Request.QueryString})"/>\n <div class="trip-place-actions">')
replace('Views/Admin/Review.cshtml','<div class="comparison">','<partial name="/Views/Admin/_ServiceDetailsReview.cshtml" model="Model"/><div class="comparison">',1)
replace('Views/Plans/Detail.cshtml',' <section id="trip-overview"',' <partial name="/Views/Plans/_TripProductShortcuts.cshtml" model="Model"/>\n <section id="trip-overview"',1)
replace('Views/Plans/Detail.cshtml','<a href="#trip-budget">Ngân sách</a>','<a href="/Preparation?planId=@plan.MaKeHoach">Chuẩn bị</a><a href="/Expenses?planId=@plan.MaKeHoach">Chi tiêu</a>')
replace('Controllers/PlansController.cs','var a = (await S.Activities(id)).Single(x => x.MaHoatDong == activityId);','var a = (await S.Activities(id)).SingleOrDefault(x => x.MaHoatDong == activityId) ?? throw new BusinessException("Không tìm thấy hoạt động trong chuyến đi.",404);')
print('Applied product integration changes.')
