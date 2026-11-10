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
public partial interface IQuyenHoatDongService
{
    Task<List<QuyetDinhQuyenHoatDong>> LayQuyetDinhCuaToiAsync();
    Task NhanGiaiTrinhAsync(CommandInput i);
    Task<List<NhaCungCapDichVu>> LayDanhSachAsync();
    Task<RightsConsequences> XemHeQuaAsync(long id, string decision);
    Task TamDinhChiAsync(CommandInput i);
    Task XemXetGiaiTrinhAsync(CommandInput i);
    Task<(NhaCungCapDichVu Provider, List<QuyetDinhQuyenHoatDong> Decisions, List<GiaiTrinhNhaCungCap> Explanations, List<TaiLieuGiaiTrinh> Files)> XemChiTietAsync(long id);
    Task<FilteredPage<NhaCungCapDichVu>> MoDanhSachAsync(string? q, string status, int page);
}
