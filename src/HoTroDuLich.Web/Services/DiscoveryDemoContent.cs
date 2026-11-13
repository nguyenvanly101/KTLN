using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

/// <summary>
/// Du lieu mau cho diem den Da Nang (chi nap khi Demo:Enabled = true, khong nap o Production).
/// Moi muc ghi ro nguon la du lieu mau, de trong ngay kiem tra, khong co gia/gio/danh gia; toa do chi la moc tham chieu gan dung.
/// </summary>
public static class DiscoveryDemoContent
{
    const string Source = "Dữ liệu mẫu do hệ thống soạn từ kiến thức phổ biến, chưa kiểm chứng tại chỗ";
    const string Unknown = "Giá vé, giờ mở cửa và điều kiện tiếp cận chưa có thông tin xác nhận; kiểm tra với đơn vị quản lý trước khi đi.";

    /// <summary>
    /// Co so mau cho Da Nang (da duyet, thuoc nha cung cap mau dang hoat dong). Ten ghi "(mẫu)", khong co toa do, gio, so dien thoai hay gia:
    /// chi de minh hoa trang diem den va luong tim kiem; khong phai don vi that. Chi goi khi Demo:Enabled = true.
    /// </summary>
    public static async Task EnsureBranches(AppDbContext db, IDongHo clock)
    {
        var province = await db.Set<DonViHanhChinh>().AsNoTracking().SingleOrDefaultAsync(u => u.Regime == "Cu" && u.Code == "48");
        var provider = await db.NhaCungCapDichVu.AsNoTracking().Where(p => p.TrangThaiXacMinh == TrangThaiXacMinh.DaXacMinh
            && p.TrangThaiQuyenHoatDong == TrangThaiQuyenHoatDong.DangHoatDong
            && db.TaiKhoan.Any(a => a.MaTaiKhoan == p.MaTaiKhoan && a.TrangThai == TrangThaiTaiKhoan.HoatDong)).OrderBy(p => p.MaNhaCungCap).FirstOrDefaultAsync();
        if (province == null || provider == null) return;

        // (ten, loai, ma quan/huyen cu cua Da Nang)
        var plan = new (string Name, string Type, string District, string Summary)[]
        {
            ("Quán mì Quảng (mẫu)", "AnUong", "492", "Quán ăn mẫu để minh họa trang điểm đến."),
            ("Quán chay (mẫu)", "AnUong", "491", "Quán ăn chay mẫu để minh họa bộ lọc ăn uống."),
            ("Quán cà phê (mẫu)", "AnUong", "493", "Quán cà phê mẫu để minh họa tìm kiếm theo loại hình."),
            ("Khách sạn ven biển (mẫu)", "LuuTru", "493", "Cơ sở lưu trú mẫu để minh họa mục Lưu trú."),
            ("Homestay gần Ngũ Hành Sơn (mẫu)", "LuuTru", "494", "Cơ sở lưu trú mẫu để minh họa tìm kiếm theo khu vực."),
            ("Cho thuê xe máy (mẫu)", "PhuongTien", "490", "Cơ sở di chuyển mẫu để minh họa mục Di chuyển."),
        };
        const string Note = " Không phải đơn vị thật: chưa có giá, giờ mở cửa, liên hệ hay vị trí chính xác.";
        var now = clock.BayGioUtc;
        foreach (var item in plan)
        {
            if (await db.Set<CoSoDichVu>().AnyAsync(b => b.ProviderId == provider.MaNhaCungCap && b.Name == item.Name)) continue;
            var district = await db.Set<DonViHanhChinh>().AsNoTracking().SingleOrDefaultAsync(u => u.Regime == "Cu" && u.Code == item.District);
            if (district == null) continue;
            var ward = await db.Set<DonViHanhChinh>().AsNoTracking().Where(u => u.ParentId == district.Id && u.Level == 3).OrderBy(u => u.Id).FirstOrDefaultAsync();
            if (ward == null) continue;
            var region = $"{ward.FullName}, {district.FullName}, {province.FullName}";
            var branch = new CoSoDichVu
            {
                ProviderId = provider.MaNhaCungCap, Name = item.Name, Type = item.Type, Region = region, DonViId = ward.Id,
                Address = "Địa chỉ mẫu, " + region, Summary = item.Summary + Note, LocationSource = "Provider",
                Status = "Approved", PublishedVersion = 1, CreatedAt = now, UpdatedAt = now
            };
            db.Add(branch);
            await db.SaveChangesAsync();
            db.Add(new BanCapNhatCoSo
            {
                BranchId = branch.Id, Number = 1, Name = branch.Name, Type = branch.Type, Region = branch.Region, DonViId = branch.DonViId,
                Address = branch.Address, Summary = branch.Summary, LocationSource = "Provider", Status = "Approved",
                OwnershipEvidence = "Dữ liệu mẫu do hệ thống tạo", SubmittedAt = now, ReviewedAt = now
            });
            await db.SaveChangesAsync();
        }
    }

    public static async Task Initialize(AppDbContext db, IDongHo clock)
    {
        var codes = new[] { "48", "490", "491", "492", "493", "494", "497" };
        var units = await db.Set<DonViHanhChinh>().AsNoTracking().Where(u => u.Regime == "Cu" && codes.Contains(u.Code)).ToDictionaryAsync(u => u.Code, u => u.Id);
        if (!units.TryGetValue("48", out var province)) return; // chua nap dia gioi
        long? U(string code) => units.TryGetValue(code, out var id) ? id : province;

        var list = new List<(DiscoveryEntry Entry, bool UnderDestination)>();
        void Add(string slug, string kind, string title, string summary, string body, string tags, long? unit, decimal? lat = null, decimal? lng = null, string? note = null, bool child = true) =>
            list.Add((new DiscoveryEntry
            {
                Slug = "demo-danang-" + slug, Kind = kind, Title = title, Region = "Đà Nẵng", DonViId = unit, Summary = summary, Body = body + "\n\n" + Unknown,
                Latitude = lat, Longitude = lng, LocationNote = lat == null ? null : (note ?? "Mốc tham chiếu gần đúng, không phải lối vào cụ thể."),
                SourceName = Source, Tags = tags, Status = "Published", UpdatedAt = clock.BayGioUtc
            }, child));

        Add("destination", "Destination", "Đà Nẵng", "Thành phố biển ở miền Trung, gần Hội An và Huế; có biển, núi, di sản và nhiều món ăn địa phương.",
            "Đà Nẵng nằm ven biển miền Trung. Du khách thường kết hợp tắm biển, tham quan các điểm trên núi và bán đảo, ghé chợ truyền thống và thử món địa phương, rồi đi tiếp tới Hội An hoặc Huế.\n\nTrang này gom các mục đã công bố theo khu vực. Hãy chọn từng quận, huyện để thu hẹp.",
            "Điểm đến thành phố biển", province, 16.0544m, 108.2022m, "Mốc trung tâm thành phố, không phải một địa điểm cụ thể.", false);
        Add("ba-na-hills", "Place", "Bà Nà Hills", "Khu du lịch trên dãy núi Bà Nà ở phía tây thành phố, nổi tiếng với cáp treo và Cầu Vàng.",
            "Khu du lịch nằm trên núi ở huyện Hòa Vang, khí hậu trên núi thường mát hơn dưới đồng bằng. Du khách thường đi cáp treo lên khu vực tham quan.", "Tham quan núi cáp treo", U("497"), 15.9977m, 107.9887m);
        Add("ban-dao-son-tra", "Place", "Bán đảo Sơn Trà", "Bán đảo nhô ra biển ở phía đông bắc thành phố, có rừng tự nhiên và ngôi chùa nhìn ra vịnh.",
            "Bán đảo có rừng tự nhiên, đường ven biển và là nơi sinh sống của loài voọc chà vá chân nâu. Cần giữ yên tĩnh và không cho thú hoang ăn khi tham quan.", "Tham quan thiên nhiên biển rừng", U("493"), 16.1167m, 108.2833m);
        Add("ngu-hanh-son", "Place", "Ngũ Hành Sơn", "Cụm năm ngọn núi đá ở phía nam thành phố, có hang động, chùa và làng đá mỹ nghệ gần đó.",
            "Quần thể núi đá vôi và đá cẩm thạch với nhiều hang động và chùa. Đường đi có bậc thang; hãy chuẩn bị giày thoải mái và tránh giờ nắng gắt.", "Tham quan núi hang động chùa", U("494"), 16.0039m, 108.2631m);
        Add("bai-bien-my-khe", "Place", "Bãi biển Mỹ Khê", "Bãi biển dài ở phía đông thành phố, một trong những nơi tắm biển được du khách biết đến nhiều.",
            "Bãi biển nằm gần các khu lưu trú và quán ăn ven biển. Điều kiện sóng gió thay đổi theo mùa; theo dõi thông báo của đơn vị cứu hộ trước khi xuống nước.", "Tham quan biển tắm biển", U("493"), 16.0678m, 108.2467m);
        Add("cau-rong", "Place", "Cầu Rồng", "Cầu bắc qua sông Hàn có hình con rồng, nằm ở trung tâm thành phố.",
            "Cầu nằm giữa khu trung tâm và bờ đông sông Hàn. Có thông tin về trình diễn phun lửa và nước vào một số tối; lịch có thể thay đổi, hãy kiểm tra thông báo mới nhất.", "Tham quan cầu sông Hàn", province, 16.0611m, 108.2278m, "Cầu nằm giữa hai quận; mốc gắn với toàn thành phố.");
        Add("bao-tang-cham", "Place", "Bảo tàng Điêu khắc Chăm Đà Nẵng", "Bảo tàng lưu giữ bộ sưu tập điêu khắc của người Chăm tại khu trung tâm.",
            "Nơi tìm hiểu về nghệ thuật điêu khắc Chăm qua các hiện vật đá. Thích hợp cho chuyến tham quan ngắn trong thành phố.", "Tham quan bảo tàng văn hóa", U("492"), 16.0606m, 108.2236m);
        Add("deo-hai-van", "Place", "Đèo Hải Vân", "Đèo trên tuyến đường nối Đà Nẵng với Huế, có điểm ngắm vịnh khi trời quang.",
            "Đoạn đèo quanh co ở phía bắc thành phố. Tầm nhìn phụ thuộc thời tiết; nên đi vào ban ngày và theo dõi dự báo trước khi khởi hành.", "Tham quan đèo cảnh quan", U("490"), 16.2000m, 108.1333m);
        Add("cho-han", "Place", "Chợ Hàn", "Chợ truyền thống ở trung tâm thành phố, gần sông Hàn.",
            "Chợ bán thực phẩm, đồ khô và hàng lưu niệm. Giá cả thương lượng theo từng sạp; chưa có bảng giá được xác nhận.", "Tham quan chợ mua sắm", U("492"), 16.0678m, 108.2236m);
        Add("cho-con", "Place", "Chợ Cồn", "Chợ lớn ở khu trung tâm, nơi người địa phương mua thực phẩm và đồ dùng.",
            "Chợ phục vụ chủ yếu người dân địa phương với nhiều mặt hàng. Giờ đông nhất thường là buổi sáng; chưa có giờ hoạt động xác nhận.", "Tham quan chợ mua sắm", U("492"), 16.0679m, 108.2144m);
        Add("san-bay", "Transport", "Sân bay quốc tế Đà Nẵng", "Sân bay nằm gần khu trung tâm thành phố, thuộc quận Hải Châu.",
            "Sân bay nằm gần trung tâm nên thời gian di chuyển vào thành phố ngắn so với nhiều thành phố khác. Kiểm tra lịch bay và phương tiện đón từ hãng hoặc đơn vị vận tải.", "Di chuyển sân bay", U("492"), 16.0439m, 108.1994m);
        Add("ga-da-nang", "Transport", "Ga Đà Nẵng", "Ga đường sắt trên tuyến Bắc – Nam, thuộc quận Thanh Khê.",
            "Ga phục vụ các chuyến tàu trên tuyến Bắc – Nam. Kiểm tra lịch tàu và giá vé từ đơn vị đường sắt trước khi lên kế hoạch.", "Di chuyển tàu hỏa ga", U("491"), 16.0715m, 108.2095m);
        Add("am-thuc", "Guide", "Ẩm thực Đà Nẵng: các món nên làm quen", "Giới thiệu chung một số món địa phương thường được nhắc tới, chưa phải danh sách quán cụ thể.",
            "Một số món thường được nhắc tới khi nói về ẩm thực Đà Nẵng và vùng lân cận gồm mì Quảng, bún chả cá, bánh tráng cuốn thịt heo và bánh xèo.\n\nHồ sơ này không giới thiệu quán cụ thể vì chưa có nguồn xác nhận. Khi chọn quán, hãy kiểm tra giá, giờ mở cửa và đánh giá từ nguồn gần đây.", "Ăn uống ẩm thực món địa phương mì quảng bún chả cá bánh xèo", province);
        Add("noi-o", "Guide", "Chọn khu vực lưu trú ở Đà Nẵng", "Gợi ý chung về cách cân nhắc khu vực ở theo mục đích chuyến đi, chưa phải danh sách cơ sở lưu trú.",
            "Khu gần biển phù hợp khi ưu tiên tắm biển; khu trung tâm thuận tiện khi muốn đi bộ tới cầu, chợ và các quán; khu phía nam gần Ngũ Hành Sơn phù hợp khi muốn kết hợp tham quan núi.\n\nHồ sơ này không giới thiệu cơ sở lưu trú cụ thể. Hãy so sánh giá, vị trí và điều kiện hủy trước khi đặt.", "Lưu trú khu vực chọn nơi ở", province);

        var existing = (await db.Set<DiscoveryEntry>().Where(x => x.Slug.StartsWith("demo-danang-")).Select(x => x.Slug).ToListAsync()).ToHashSet();
        foreach (var (entry, _) in list.Where(x => !existing.Contains(x.Entry.Slug)))
        {
            entry.CheckedOn = null; // chua kiem tra nguon: giao dien hien "Chua co ngay kiem tra nguon"
            db.Add(entry);
            await db.SaveChangesAsync();
        }
        // Dong bo tu khoa (Tags) cho muc mau da co, tru muc quan tri vien da sua tay.
        foreach (var (entry, _) in list)
        {
            var row = await db.Set<DiscoveryEntry>().SingleOrDefaultAsync(x => x.Slug == entry.Slug);
            if (row != null && row.UpdatedBy == null && row.Tags != entry.Tags) row.Tags = entry.Tags;
        }
        await db.SaveChangesAsync();
        var parent = await db.Set<DiscoveryEntry>().SingleOrDefaultAsync(x => x.Slug == "demo-danang-destination");
        if (parent != null)
        {
            var childSlugs = list.Where(x => x.UnderDestination).Select(x => x.Entry.Slug).ToList();
            foreach (var e in await db.Set<DiscoveryEntry>().Where(x => childSlugs.Contains(x.Slug) && x.ParentId == null && x.UpdatedBy == null).ToListAsync()) e.ParentId = parent.Id;
            await db.SaveChangesAsync();
        }
    }
}
