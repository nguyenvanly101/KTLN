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
public partial interface IThoaThuanDongHanhService
{
    IQueryable<ThoaThuanDongHanh> TruyVanThoaThuan();
    Task<List<ThoaThuanDongHanh>> LayLichSuAsync(long connection);
    Task<AgreementInput> MoTaoThoaThuanAsync(long connection);
    Task GuiDeNghiThoaThuanAsync(AgreementInput i);
    Task XacNhanAsync(CommandInput i);
}
