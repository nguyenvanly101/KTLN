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
public partial interface IKetNoiDongHanhService
{
    Task MoDeNghiAsync(long id);
    Task GuiDeNghiAsync(CommandInput i);
    IQueryable<DeNghiKetNoiDongHanh> TruyVanKetNoi();
    Task<List<DeNghiKetNoiDongHanh>> DanhSachAsync();
    Task<DeNghiKetNoiDongHanh> XemDeNghiAsync(long id);
    Task PhanHoiAsync(CommandInput i);
    Task<string> LayNoiDenAsync(long id);
    Task<long> LayChuNhuCauAsync(long id);
}
