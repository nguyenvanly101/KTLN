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
public partial interface IXuLyTinhHuongService
{
    Task<List<TinhHuongPhatSinh>> DanhSachAsync(long plan);
    Task GhiNhanTinhHuongAsync(IncidentInput i);
    Task GhiKetQuaAsync(CommandInput i);
    Task<List<PhuongAnXuLy>> XemTinhHuongAsync(long id);
}
