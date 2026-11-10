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
public partial interface ILapKeHoachService
{
    Task<List<KeHoachChuyenDi>> DanhSachAsync();
    Task<KeHoachChuyenDi> XemKeHoachAsync(long id);
    Task<List<HoatDongLichTrinh>> LayHoatDong(long id);
    Task<long> CapNhatThongTinAsync(PlanInput i);
    Task ChapNhanAsync(CommandInput i);
    Task ThemHoatDongAsync(ActivityInput i);
    Task SapXepHoatDongAsync(CommandInput i);
    Task XoaHoatDongAsync(CommandInput i);
}
