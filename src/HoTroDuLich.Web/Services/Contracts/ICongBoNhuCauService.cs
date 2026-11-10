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
public partial interface ICongBoNhuCauService
{
    IQueryable<NhuCauDongHanh> NguoiXemThayDuoc();
    Task<List<NhuCauDongHanh>> DanhSachCuaToiAsync();
    void KiemTraDuLieu(NeedInput i);
    Task XemTruocAsync(NeedInput i);
    Task CongBoAsync(NeedInput i);
    Task DungCongBoAsync(CommandInput i);
    Task<NeedInput> MoBieuMauAsync(long id);
}
