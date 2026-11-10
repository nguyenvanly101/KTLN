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
