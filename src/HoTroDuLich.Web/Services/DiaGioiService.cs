using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace HoTroDuLich.Web.Services;

public sealed record DonViItem(long Id, string Name, string FullName, string Kind, int Level, bool HasChildren);
public sealed record DonViHit(long Id, string Name, string FullName, int Level, string Path);

/// <summary>Tra cuu cay dia gioi hanh chinh (he goc cau hinh bang DiaGioi:HeGoc, mac dinh "Cu" = 63 tinh/huyen/xa).</summary>
public sealed class DiaGioiService(AppDbContext db, IMemoryCache cache, IConfiguration config)
{
    public string Regime => config["DiaGioi:HeGoc"] == "Moi" ? "Moi" : "Cu";
    IQueryable<DonViHanhChinh> Units => db.Set<DonViHanhChinh>().AsNoTracking();

    public async Task<List<DonViItem>> Provinces()
    {
        var regime = Regime;
        return (await cache.GetOrCreateAsync("DiaGioi.Tinh." + regime, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await Units.Where(x => x.Regime == regime && x.Status == "Active" && x.Level == 1).OrderBy(x => x.Name)
                .Select(x => new DonViItem(x.Id, x.Name, x.FullName, x.Kind, x.Level, true)).ToListAsync();
        }))!;
    }

    public async Task<List<DonViItem>> Children(long parentId) =>
        await Units.Where(x => x.ParentId == parentId && x.Status == "Active").OrderBy(x => x.Name)
            .Select(x => new DonViItem(x.Id, x.Name, x.FullName, x.Kind, x.Level, Units.Any(c => c.ParentId == x.Id))).ToListAsync();

    public async Task<DonViHanhChinh?> Get(long id) => await Units.SingleOrDefaultAsync(x => x.Id == id);

    /// <summary>Chuoi tu tinh xuong don vi chon (goc truoc).</summary>
    public async Task<List<DonViHanhChinh>> Path(long id)
    {
        var chain = new List<DonViHanhChinh>();
        long? cursor = id;
        while (cursor != null && chain.Count < 4)
        {
            var unit = await Get(cursor.Value);
            if (unit == null) break;
            chain.Insert(0, unit);
            cursor = unit.ParentId;
        }
        return chain;
    }

    /// <summary>"Phuong Hai Chau I, Quan Hai Chau, Thanh pho Da Nang" (tu nho den lon).</summary>
    public async Task<string> PathText(long id) => string.Join(", ", (await Path(id)).AsEnumerable().Reverse().Select(x => x.FullName));

    /// <summary>Nhan ngan (toi da 100 ky tu) cho cot khu vuc dang chuoi con dung o tra cuu cu.</summary>
    public async Task<string> RegionLabel(long id)
    {
        var path = await Path(id);
        if (path.Count == 0) return "";
        var last = path[^1];
        var label = last.Level == 3 && path.Count > 1 ? last.Name + ", " + path[^2].Name : last.Name;
        return label.Length <= 100 ? label : label[..100];
    }

    /// <summary>Truy van con: chinh don vi, cac con va chau (toi da 3 cap). Dung trong Where(x => sub.Contains(x.DonViId.Value)).</summary>
    public IQueryable<long> SubtreeQuery(long id) =>
        db.Set<DonViHanhChinh>().Where(u => u.Id == id || u.ParentId == id || db.Set<DonViHanhChinh>().Any(p => p.Id == u.ParentId && p.ParentId == id)).Select(u => u.Id);

    /// <summary>Don vi hop le de gan cho co so/dia diem: thuoc he goc, dang dung, la xa/phuong hoac huyen khong co xa con (huyen dao).</summary>
    public async Task<DonViHanhChinh> RequireLeaf(long id)
    {
        var unit = await Get(id) ?? throw new BusinessException("Không tìm thấy đơn vị hành chính đã chọn.", 422, "DIA_GIOI", "DonViId");
        if (unit.Regime != Regime || unit.Status != "Active")
            throw new BusinessException("Đơn vị hành chính đã chọn không còn được sử dụng.", 422, "DIA_GIOI", "DonViId");
        var isLeaf = unit.Level == 3 || (unit.Level == 2 && !await Units.AnyAsync(x => x.ParentId == unit.Id));
        if (!isLeaf)
            throw new BusinessException("Chọn đến cấp xã/phường (hoặc thị trấn) của khu vực.", 422, "DIA_GIOI", "DonViId");
        return unit;
    }

    public async Task<DonViHanhChinh> RequireAny(long id)
    {
        var unit = await Get(id) ?? throw new BusinessException("Không tìm thấy đơn vị hành chính đã chọn.", 422, "DIA_GIOI", "DonViId");
        if (unit.Regime != Regime || unit.Status != "Active")
            throw new BusinessException("Đơn vị hành chính đã chọn không còn được sử dụng.", 422, "DIA_GIOI", "DonViId");
        return unit;
    }

    /// <summary>Don vi cha chung gan nhat cua nhieu don vi (null neu khong co chung tinh/khong co du lieu).</summary>
    public async Task<long?> CommonAncestor(IEnumerable<long> unitIds)
    {
        List<long>? common = null;
        foreach (var id in unitIds.Distinct())
        {
            var chain = (await Path(id)).Select(x => x.Id).ToList();
            if (chain.Count == 0) return null;
            common = common == null ? chain : common.Zip(chain, (a, b) => (a, b)).TakeWhile(p => p.a == p.b).Select(p => p.a).ToList();
        }
        return common is { Count: > 0 } ? common[^1] : null;
    }

    /// <summary>Tim theo ten, ten day du hoac ten goi khac; khong phan biet hoa/thuong/dau.</summary>
    public async Task<List<DonViHit>> Search(string? q, int take = 20)
    {
        var term = DiscoveryText.Fold(q?.Trim());
        if (term.Length < 2 || term.Length > 100) return [];
        var regime = Regime;
        var aliasUnits = db.Set<TenGoiKhacDonVi>().Where(a => a.NameFolded.Contains(term)).Select(a => a.UnitId);
        // Khong ORDER BY trong SQL (sap xep tren nhieu dong doi cap phat bo nho lon, may it RAM se cho rat lau):
        // lay Id + ten bo dau (toi da 500 dong khop), xep hang trong bo nho, roi doc chi tiet cho ket qua dau.
        var matches = await Units.Where(x => x.Regime == regime && x.Status == "Active" && (x.NameFolded.Contains(term) || aliasUnits.Contains(x.Id)))
            .Select(x => new { x.Id, x.NameFolded, x.Level }).Take(500).ToListAsync();
        var topIds = matches.OrderBy(x => x.NameFolded == term ? 0 : x.NameFolded.StartsWith(term, StringComparison.OrdinalIgnoreCase) ? 1 : 2).ThenBy(x => x.Level).ThenBy(x => x.NameFolded, StringComparer.Ordinal)
            .Take(take).Select(x => x.Id).ToList();
        var rank = topIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var found = (await Units.Where(x => topIds.Contains(x.Id)).Select(x => new { x.Id, x.Name, x.FullName, x.Level, x.ParentId, x.NameFolded }).ToListAsync()).OrderBy(x => rank[x.Id]).ToList();
        var parentIds = found.Where(x => x.ParentId != null).Select(x => x.ParentId!.Value).Distinct().ToList();
        var parents = await Units.Where(x => parentIds.Contains(x.Id)).Select(x => new { x.Id, x.FullName, x.ParentId }).ToDictionaryAsync(x => x.Id);
        var grandIds = parents.Values.Where(x => x.ParentId != null).Select(x => x.ParentId!.Value).Distinct().ToList();
        var grands = await Units.Where(x => grandIds.Contains(x.Id)).Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id);
        string Text(string fullName, long? parentId)
        {
            var parts = new List<string> { fullName };
            if (parentId != null && parents.TryGetValue(parentId.Value, out var p))
            {
                parts.Add(p.FullName);
                if (p.ParentId != null && grands.TryGetValue(p.ParentId.Value, out var g)) parts.Add(g.FullName);
            }
            return string.Join(", ", parts);
        }
        return found.Select(x => new DonViHit(x.Id, x.Name, x.FullName, x.Level, Text(x.FullName, x.ParentId))).ToList();
    }
}

/// <summary>Tham so cho Views/Shared/_DiaGioiPicker.cshtml. LeafOnly: bat buoc chon den xa/phuong; false: chon o cap bat ky (bo loc).</summary>
public sealed record DiaGioiPickerModel(string Name, long? Value, bool LeafOnly = true, string? Hint = null);

public static class DiaGioiBootstrap
{
    /// <summary>Gan DonViId cho du lieu con khu vuc dang chuoi (thu tuc tu 008b); loi khong lam hong khoi dong.</summary>
    public static async Task Assign(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var regime = services.GetRequiredService<DiaGioiService>().Regime;
        try { await db.Database.ExecuteSqlRawAsync("EXEC dbo.usp_GanDonViChoKhuVucCu @Regime={0}", regime); }
        catch (Exception e) { services.GetRequiredService<ILoggerFactory>().CreateLogger("DiaGioi").LogWarning("Chưa gán địa giới cho khu vực cũ: {Type}", e.GetType().Name); }
    }
}
