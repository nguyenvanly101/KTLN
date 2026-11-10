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
public partial interface IXetDuyetDichVuService
{
    Task<List<BanCapNhatDichVu>> LayHangDoiAsync();
    Task DuyetAsync(CommandInput i);
    Task<(BanCapNhatDichVu Pending, DichVuDuLich Current, NhaCungCapDichVu Provider)> XemNoiDungAsync(long id);
    Task<FilteredPage<AdminReviewRow>> MoDanhSachChoDuyetAsync(string? q, string status, int page);
}
