using System.Security.Claims;
using System.Data;
using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Security.Cryptography;

namespace HoTroDuLich.Web.Services;
public partial class LuuTepService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider) : NghiepVuService(context, accessor, environment, configuration, provider), ILuuTepService
{
    private string ThuMucTep
    {
        get
        {
            var root=Path.GetFullPath(config["UploadFiles:ThuMuc"]??Path.Combine("App_Data","uploads"),env.ContentRootPath);
            var publicRoot=Path.GetFullPath(env.WebRootPath??Path.Combine(env.ContentRootPath,"wwwroot"));
            if(root.Equals(publicRoot,StringComparison.OrdinalIgnoreCase)||root.StartsWith(publicRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Thư mục tải lên phải nằm ngoài wwwroot.");
            return root;
        }
    }
    public string DuongDanAnToan(string file)
    {
        var root = ThuMucTep + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar)));
        Require(path.StartsWith(root, StringComparison.OrdinalIgnoreCase), "Đường dẫn không hợp lệ.", 404);
        return path;
    }

    public async Task<(string Path, string Name, string ContentType)> TaiTepAsync(string kind, long id)
    {
        string file, name, type;
        if (kind == "verification")
        {
            var f = await Find<TaiLieuXacMinh>(id);
            Require(Role==AdminRole||(Role==ProviderRole&&await db.HoSoXacMinhNhaCungCap.AnyAsync(h=>h.MaHoSo==f.MaHoSo&&db.NhaCungCapDichVu.Any(p=>p.MaNhaCungCap==h.MaNhaCungCap&&p.MaTaiKhoan==UserId))),"Không tìm thấy tài liệu.",404);
            file = f.DuongDan;
            name = f.TenTepGoc;
            type = (f.LoaiTep).ToString();
        }
        else
        {
            Require(kind == "explanation", "Tệp không hợp lệ.", 404);
            var f = await Find<TaiLieuGiaiTrinh>(id);
            var g = await Find<GiaiTrinhNhaCungCap>(f.MaGiaiTrinh);
            var d = await Find<QuyetDinhQuyenHoatDong>(g.MaQuyetDinh);
            Require(Role==AdminRole||(Role==ProviderRole&&await db.NhaCungCapDichVu.AnyAsync(p=>p.MaNhaCungCap==d.MaNhaCungCap&&p.MaTaiKhoan==UserId)),"Không tìm thấy tài liệu.",404);
            file = f.DuongDan;
            name = f.TenTepGoc;
            type = (f.LoaiTep).ToString();
        }

        return (DuongDanAnToan(file), name, type == "pdf" ? "application/pdf" : type == "png" ? "image/png" : "image/jpeg");
    }
}
