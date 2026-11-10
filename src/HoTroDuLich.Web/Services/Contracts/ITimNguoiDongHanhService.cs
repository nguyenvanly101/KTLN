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
public partial interface ITimNguoiDongHanhService
{
    Task<List<NhuCauDongHanh>> TimNhuCauAsync(string? q, DateOnly? start, DateOnly? end, decimal? budget, string? style, int page = 1);
    Task<NhuCauDongHanh> XemChiTietAsync(long id);
}
