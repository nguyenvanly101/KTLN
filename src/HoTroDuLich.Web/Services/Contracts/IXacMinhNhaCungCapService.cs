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
public partial interface IXacMinhNhaCungCapService
{
    Task<List<HoSoXacMinhNhaCungCap>> HoSoCuaToiAsync();
    Task NopHoSoMoiAsync(CommandInput i);
    Task NhanBoSungAsync(CommandInput i);
    Task<List<HoSoXacMinhNhaCungCap>> LayHangDoiAsync();
    Task XacMinhAsync(CommandInput i);
    Task<(HoSoXacMinhNhaCungCap Profile, NhaCungCapDichVu Provider, List<TaiLieuXacMinh> Files, List<QuyetDinhXacMinh> Decisions)> XemHoSoAsync(long id);
    Task<FilteredPage<AdminVerificationRow>> MoDanhSachAsync(string? q, string status, int page);
}
