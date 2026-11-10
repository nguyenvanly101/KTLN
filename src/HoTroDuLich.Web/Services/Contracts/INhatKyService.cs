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
public partial interface INhatKyService
{
    void Ghi(string kind, string target, long id, string? before, string? after, string? reason = null);
    Task GhiRiengAsync(string kind, string target, long id, string? before, string? after, string? reason = null);
    Task<List<NhatKyThaoTac>> LayDanhSachAsync();
    Task<AdminOverview> TongQuanAsync();
}
