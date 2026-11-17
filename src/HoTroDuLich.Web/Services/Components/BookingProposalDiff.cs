using System.Text.Json;
using System.Text.RegularExpressions;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

/// <summary>Mot dieu kien nha cung cap da doi trong de xuat: gia tri khach gui va gia tri de xuat.</summary>
public sealed record BookingChange(string Label, string Old, string New);

public partial interface IDatDichVuService
{
    Task<List<BookingChange>> ThayDoiDeXuatAsync(long id);
}

public partial class DatDichVuService
{
    /// <summary>So sanh de xuat dang cho khach tra loi voi yeu cau truoc do (lay tu nhat ky "DeXuatDieuKien"). Rong neu khong con de xuat hoac khong doc duoc nhat ky.</summary>
    public async Task<List<BookingChange>> ThayDoiDeXuatAsync(long id)
    {
        var y = await ChiTietAsync(id);
        if (y.TrangThai != TrangThaiYeuCau.DeXuatKhac) return [];
        var rows = await db.NhatKyThaoTac.AsNoTracking().Where(x => x.LoaiSuKien == "DeXuatDieuKien" && x.DoiTuong == "YeuCauDatDichVu" && x.MaDoiTuong == id)
            .OrderByDescending(x => x.MaNhatKy).Take(12).Select(x => new { x.TrangThaiTruoc, x.LyDo }).ToListAsync();
        if (rows.Count == 0) return [];
        // Anh chup dai duoc chia thanh nhieu dong lien tiep, danh so "[i/n]" trong ly do.
        var parts = 1;
        var mark = Regex.Match(rows[0].LyDo ?? "", @"^\[(\d+)/(\d+)\]");
        if (mark.Success) parts = Math.Min(rows.Count, int.Parse(mark.Groups[2].Value));
        var json = string.Concat(rows.Take(parts).Reverse().Select(r => r.TrangThaiTruoc));
        try
        {
            using var doc = JsonDocument.Parse(json);
            var o = doc.RootElement;
            string? Text(string name) => o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            DateOnly? Date(string name) => DateOnly.TryParse(Text(name), out var d) ? d : null;
            int? Number(string name) => o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
            decimal? Money(string name) => o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;
            static string Day(DateOnly? d) => d?.ToString("dd/MM/yyyy") ?? "Chưa có";
            static string Plain(string? s) => string.IsNullOrWhiteSpace(s) ? "Chưa có" : s.Replace("\r", "").Replace("\n", ", ");
            var changes = new List<BookingChange>();
            void Add(string label, string before, string after) { if (before != after) changes.Add(new(label, before, after)); }
            Add("Ngày bắt đầu", Day(Date("NgayBatDau")), Day(y.NgayBatDau));
            Add("Ngày kết thúc", Day(Date("NgayKetThuc")), Day(y.NgayKetThuc));
            Add("Số người", (Number("SoNguoi") ?? y.SoNguoi) + " người", y.SoNguoi + " người");
            Add("Số lượng", (Number("SoLuong") ?? y.SoLuong) + " đơn vị", y.SoLuong + " đơn vị");
            Add("Chi phí dự kiến", Labels.Money(Money("ChiPhiDuKien")), Labels.Money(y.ChiPhiDuKien));
            Add("Thời gian đến / đi", Plain(Text("ThoiGianDenDi")), Plain(y.ThoiGianDenDi));
            Add("Hạng mục chọn thêm", Plain(Text("NoiDungLuaChon")), Plain(y.NoiDungLuaChon));
            return changes;
        }
        catch (Exception e) when (e is JsonException or FormatException or InvalidOperationException) { return []; }
    }
}
