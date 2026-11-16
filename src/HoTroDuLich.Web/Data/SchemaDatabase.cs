using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HoTroDuLich.Web.Data;

// SQL, not EF's create script, owns the schema (FullText 4.8.9).
public static class SchemaDatabase
{
    public static async Task Initialize(AppDbContext db, string root)
    {
        var settings = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        var database = settings.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) throw new InvalidOperationException("Chuỗi kết nối phải có tên cơ sở dữ liệu.");
        settings.InitialCatalog = "master";
        await using (var master = new SqlConnection(settings.ConnectionString))
        {
            await master.OpenAsync();
            await using var exists = new SqlCommand("SELECT DB_ID(@name)", master);
            exists.Parameters.AddWithValue("@name", database);
            if (await exists.ExecuteScalarAsync() is DBNull)
            {
                var quoted = "[" + database.Replace("]", "]]") + "]";
                await using var create = new SqlCommand($"CREATE DATABASE {quoted} COLLATE Vietnamese_100_CI_AS_SC;", master);
                await create.ExecuteNonQueryAsync();
                await using var configure = new SqlCommand($"ALTER DATABASE {quoted} SET READ_COMMITTED_SNAPSHOT ON; ALTER DATABASE {quoted} SET ALLOW_SNAPSHOT_ISOLATION OFF; ALTER DATABASE {quoted} SET AUTO_CLOSE OFF; ALTER DATABASE {quoted} SET PAGE_VERIFY CHECKSUM; ALTER DATABASE {quoted} SET RECOVERY FULL;", master);
                await configure.ExecuteNonQueryAsync();
            }
        }
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var count = db.Database.GetDbConnection().CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0";
            if (Convert.ToInt32(await count.ExecuteScalarAsync()) == 0)
                await ExecuteScript(db, Path.Combine(root, "Database", "schema.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "002_LienKetYeuCauChuyenDi.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "003_ChuanBi_ChiTieu_LuaChon.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "004_HanhTrinh_ChiaSe_DanhGia_LichPhucVu.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "005_KhongGianDongHanh.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "006_TraCuuThongTin.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "007_CoSoDichVu_GoogleMaps.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "008_DiaGioiHanhChinh.sql"));
            await using (var units = db.Database.GetDbConnection().CreateCommand())
            {
                units.CommandText = "SELECT COUNT(*) FROM dbo.DonViHanhChinh";
                if (Convert.ToInt32(await units.ExecuteScalarAsync()) == 0)
                    await ExecuteScript(db, Path.Combine(root, "Database", "008c_NapDiaGioi.sql"));
            }
            await ExecuteScript(db, Path.Combine(root, "Database", "008b_GanDonViChoKhuVucCu.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "009_CoSoVaoTraCuu.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "010_CoSoThongTinChoKhach.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "011_TinNhanCoSo.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "012_DatDichVuTrongChat.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "013_TinNhanGiuaMoiNguoi.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "014_KetNoiTrongChat.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "015_MangXaHoi.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "016_TinNhanNangCao.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "017_DatCocNhacLich.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "018_ThoaThuanBaoCao.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "019_ThuocTinhCoSo.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "020_LoaiBaiViet.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "021_TheBaiVietBaoCao.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "022_ChuyenDiChung.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "023_BangTinNangCao.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "024_AnhBiaHoSo.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "025_LoaiDichVu.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "026_LichTrinhMau.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "027_ChiaTienNhom.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "028_HoiDapBaoSai.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "029_NhomChatChuyenDi.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "030_GhiChuNgay.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "031_LoaiDichVuMoRong.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "032_DiemTuyenChuyenDi.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "033_KhoiHanh.sql"));
            await ExecuteScript(db, Path.Combine(root, "Database", "034_NhanDiaDiem.sql"));
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    public static async Task ExecuteScript(AppDbContext db, string path)
    {
        var source = await File.ReadAllTextAsync(path);
        foreach (var batch in Regex.Split(source, @"^\s*GO\s*(?:--[^\r\n]*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync();
        }
    }
}
