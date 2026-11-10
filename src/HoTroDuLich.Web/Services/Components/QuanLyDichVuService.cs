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
public partial class QuanLyDichVuService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider) : NghiepVuService(context, accessor, environment, configuration, provider), IQuanLyDichVuService
{
    public async Task<bool> KiemTraDieuKienDangAsync()
    {
        RoleRequired(ProviderRole);
        var p = await MyProvider();
        return await ProviderActive(p.MaNhaCungCap);
    }

    public async Task<List<DichVuDuLich>> DanhSachAsync()
    {
        var p = await MyProvider();
        return await db.DichVuDuLich.Where(x => x.MaNhaCungCap == p.MaNhaCungCap).OrderByDescending(x => x.ThoiDiemCapNhat).ThenByDescending(x => x.MaDichVu).ToListAsync();
    }

    public async Task<DichVuDuLich> XemDichVuAsync(long id)
    {
        var p = await MyProvider();
        return await db.DichVuDuLich.SingleOrDefaultAsync(x => x.MaDichVu == id && x.MaNhaCungCap == p.MaNhaCungCap) ?? throw new BusinessException(MaLoi.TatCa["0601-02"].Message, 404, "0601-02");
    }

    public async Task<List<BanCapNhatDichVu>> LayLichSuAsync(long id)
    {
        await XemDichVuAsync(id);
        return await db.BanCapNhatDichVu.Where(x => x.MaDichVu == id).OrderByDescending(x => x.SoPhienBan).ToListAsync();
    }

    public async Task GuiDangAsync(ServiceInput i)
    {
        RoleRequired(ProviderRole);
        i.Type = DanhMuc.ChuanHoa(i.Type);
        var serviceProvider=await MyProvider();
        await Resolve<BranchService>().ValidateServiceBranches(serviceProvider.MaNhaCungCap,i.BranchIds,i.Operation=="GuiDang");
        if(i.BranchIds.Count>0)
        {
            var providerRecord=await MyProvider();
            var locations=await Resolve<BranchService>().PublicBranches().Where(x=>i.BranchIds.Contains(x.Id)&&x.ProviderId==providerRecord.MaNhaCungCap).Select(x=>x.Region).Distinct().ToListAsync();
            i.Region=string.Join(" · ",locations);if(i.Region.Length>200)i.Region="Theo cơ sở áp dụng";
            i.Address=null;
        }
        i.Name = i.Name.Trim();
        i.Region = (i.Region??"").Trim();
        if(i.Operation=="LuuNhap"&&i.Region.Length==0)i.Region="Chưa chọn cơ sở";
        i.Address=string.IsNullOrWhiteSpace(i.Address)?null:i.Address.Trim();
        i.PriceUnit=string.IsNullOrWhiteSpace(i.PriceUnit)?null:i.PriceUnit.Trim();
        i.CancellationPolicy=string.IsNullOrWhiteSpace(i.CancellationPolicy)?null:i.CancellationPolicy.Trim();
        Validate(i, "0601-04");
        Ensure(DanhMuc.LoaiDichVu.Contains(i.Type), "0601-04", "Type");
        var noPause = i.PauseFrom == null && i.PauseTo == null && string.IsNullOrWhiteSpace(i.PauseReason);
        Ensure(noPause || (i.PauseFrom != null && !string.IsNullOrWhiteSpace(i.PauseReason) && (i.PauseTo == null || (i.PauseTo >= i.PauseFrom && i.PauseTo >= HomNay))), "0601-03");
        if (noPause)
            i.PauseReason = null;
        Require(new[] { "GuiDang", "LuuNhap" }.Contains(i.Operation), "Thao tác không hợp lệ.", 400);
        await Execute(async () =>
        {
            var p = await MyProvider();
            Ensure(await ProviderActive(p.MaNhaCungCap), "0601-01");
            var branches=Resolve<BranchService>();
            await branches.ValidateServiceBranches(p.MaNhaCungCap,i.BranchIds,i.Operation=="GuiDang");
            if (i.PlaceId != null)
                Ensure(await db.DiaDiemDuLich.AnyAsync(x => x.MaDiaDiem == i.PlaceId && x.DangHienThi), "0601-04", "PlaceId");
            DichVuDuLich s;
            bool draft;
            if (i.Id == 0)
            {
                s = new()
                {
                    MaNhaCungCap = p.MaNhaCungCap,
                    TrangThaiCongBo = HoTroDuLich.Web.Domain.TrangThaiCongBo.Nhap,
                    TrangThaiTiepNhan = HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan
                };
                db.Add(s);
                draft = true;
            }
            else
            {
                s = await XemDichVuAsync(i.Id);
                CheckVersion(s, i.Version, "0601-07");
                Ensure(!await db.BanCapNhatDichVu.AnyAsync(x => x.MaDichVu == i.Id && x.TrangThaiDuyet == HoTroDuLich.Web.Domain.TrangThaiDuyet.ChoDuyet), "0601-05");
                draft = s.TrangThaiCongBo == HoTroDuLich.Web.Domain.TrangThaiCongBo.Nhap;
                Ensure(draft || new HoTroDuLich.Web.Domain.TrangThaiCongBo[] { HoTroDuLich.Web.Domain.TrangThaiCongBo.DaDuyet, HoTroDuLich.Web.Domain.TrangThaiCongBo.TuChoi, HoTroDuLich.Web.Domain.TrangThaiCongBo.CanSua, HoTroDuLich.Web.Domain.TrangThaiCongBo.NgungCongBo }.Contains(s.TrangThaiCongBo), "0601-02");
                if (!draft)
                {
                    Require(i.Operation == "GuiDang", "Chỉ bản nháp được lưu nháp.", 400);
                    object previous = s;
                    if (s.PhienBanCongBo == null)
                        previous = (object? )await db.BanCapNhatDichVu.Where(x => x.MaDichVu == i.Id).OrderByDescending(x => x.SoPhienBan).FirstOrDefaultAsync() ?? s;
                    await PreparePhotos(i,(string?)previous.GetType().GetProperty("AnhJson")!.GetValue(previous));
                    var currentBranches=await branches.ServiceBranches(s.MaDichVu,previous is BanCapNhatDichVu oldRevision?oldRevision.MaBanCapNhat:null);
                    Ensure(!SameService(previous, i)||!currentBranches.Order().SequenceEqual(i.BranchIds.Order()), "0601-06");
                }
            }

            if (draft) await PreparePhotos(i,s.AnhJson);
            if (draft || s.PhienBanCongBo == null)
            {
                CopyServiceInput(s, i);
                s.TrangThaiCongBo = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiCongBo>(i.Operation == "LuuNhap" ? "Nhap" : "ChoDuyet");
            }

            s.ThoiDiemCapNhat = Clock.BayGioUtc;
            await db.SaveChangesAsync();
            if(draft||s.PhienBanCongBo==null)await branches.ReplaceServiceBranches(s.MaDichVu,i.BranchIds);
            if (i.Operation == "GuiDang")
            {
                var version = (await db.BanCapNhatDichVu.Where(x => x.MaDichVu == s.MaDichVu).MaxAsync(x => (int? )x.SoPhienBan) ?? 0) + 1;
                var revision = new BanCapNhatDichVu
                {
                    MaDichVu = s.MaDichVu,
                    SoPhienBan = version,
                    TrangThaiDuyet = HoTroDuLich.Web.Domain.TrangThaiDuyet.ChoDuyet
                };
                CopyServiceInput(revision, i);
                db.Add(revision);
                await branches.SnapshotServiceBranches(revision,i.BranchIds);
            }
        });
    }

    private async Task PreparePhotos(ServiceInput input,string? existing)
    {
        input.PhotoPaths=input.RemovePhotos?null:existing;
        Require(input.Photos.Count<=5,"Chọn tối đa 5 ảnh.",400);
        Require(input.Photos.All(f=>new[]{".jpg",".jpeg",".png"}.Contains(Path.GetExtension(f.FileName).ToLowerInvariant())),"Ảnh dịch vụ chỉ nhận JPG/PNG, tối đa 5 MB mỗi ảnh.",400);
        if(input.Photos.Count>0)input.PhotoPaths=JsonSerializer.Serialize((await StoreFiles(input.Photos)).Select(x=>x.Path));
    }

    public async Task<DichVuDuLich> LayChiTiet(long id) => await PublicServices().SingleOrDefaultAsync(x => x.MaDichVu == id) ?? throw new BusinessException("Dịch vụ không còn được công bố.", 404);
}

