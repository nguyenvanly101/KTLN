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
public partial interface IDatDichVuService
{
    IQueryable<YeuCauDatDichVu> TruyVanYeuCau();
    Task<List<YeuCauDatDichVu>> DanhSachCuaToiAsync();
    Task<YeuCauDatDichVu> ChiTietAsync(long id);
    void KiemTraDuLieu(BookingInput i);
    Task<DichVuDuLich> MoDatDichVuAsync(long id);
    Task<List<string>> KiemTraTruocGuiAsync(BookingInput i);
    Task<long> GuiYeuCauAsync(BookingInput i);
    Task ChapNhanAsync(CommandInput i);
    Task<List<string>> LayHangMucAsync(long id);
    Task<Dictionary<long, string>> LayTenDichVuAsync();
    Task<string> LayTenDichVuCuaYeuCauAsync(long id);
}
