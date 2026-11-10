using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace HoTroDuLich.Web.Data;
public static class Seed
{
    public static async Task Initialize(IServiceProvider services, string root)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var config = services.GetRequiredService<IConfiguration>();
        await SchemaDatabase.Initialize(db, root);
        if (!config.GetValue<bool>("Demo:Enabled") && !await db.TaiKhoan.AnyAsync(x => x.VaiTro == HoTroDuLich.Web.Domain.VaiTro.QUAN_TRI_VIEN))
        {
            var email = config["QuanTriBanDau:Email"]?.Trim().ToLowerInvariant();
            var password = config["QuanTriBanDau:MatKhau"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password) || !Services.TravelService.ValidPassword(password, email))
                throw new InvalidOperationException("Đặt QuanTriBanDau__Email và QuanTriBanDau__MatKhau hợp lệ để tạo Admin ban đầu.");
            await using var bootstrap = await db.Database.BeginTransactionAsync();
            var admin = new TaiKhoan
            {
                Email = email,
                VaiTro = HoTroDuLich.Web.Domain.VaiTro.QUAN_TRI_VIEN,
                TrangThai = HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong,
                PhaiDoiMatKhau = true
            };
            admin.MatKhauBam = new PasswordHasher<TaiKhoan>().HashPassword(admin, password);
            db.Add(admin);
            await db.SaveChangesAsync();
            db.Add(new HoSoNguoiDung { MaTaiKhoan = admin.MaTaiKhoan, TenHienThi = "Quản trị viên", ThongTinChiaSe = HoTroDuLich.Web.Domain.ThongTinChiaSe.RiengTu });
            await db.SaveChangesAsync();
            await bootstrap.CommitAsync();
        }

        if (await db.TaiKhoan.AnyAsync() || !config.GetValue<bool>("Demo:Enabled"))
            return;
        await using var tx = await db.Database.BeginTransactionAsync();
        var accounts = new List<TaiKhoan>();
        foreach (var(email, role, name)in new[]
        {
            ("khach@hanhtrinh.vn", "KHACH_DU_LICH", "Minh Anh"),
            ("nhacungcap@hanhtrinh.vn", "NHA_CUNG_CAP", "Mây Travel"),
            ("admin@hanhtrinh.vn", "QUAN_TRI_VIEN", "Quản trị viên"),
            ("linh@hanhtrinh.vn", "KHACH_DU_LICH", "Thảo Linh")
        }

        )
        {
            var a = new TaiKhoan
            {
                Email = email,
                VaiTro = System.Enum.Parse<HoTroDuLich.Web.Domain.VaiTro>(role),
                TrangThai = HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong,
                PhaiDoiMatKhau = role == "QUAN_TRI_VIEN"
            };
            a.MatKhauBam = new PasswordHasher<TaiKhoan>().HashPassword(a, "HanhTrinh@2026");
            db.Add(a);
            await db.SaveChangesAsync();
            db.Add(new HoSoNguoiDung { MaTaiKhoan = a.MaTaiKhoan, TenHienThi = name, ThongTinChiaSe = HoTroDuLich.Web.Domain.ThongTinChiaSe.CongKhai, NoiSinhSong = "TP. Hồ Chí Minh", SoThich = "Khám phá thiên nhiên, nhiếp ảnh" });
            accounts.Add(a);
        }

        var provider = new NhaCungCapDichVu
        {
            MaTaiKhoan = accounts[1].MaTaiKhoan,
            TenDonVi = "Mây Travel",
            LoaiHinh = "Dịch vụ du lịch",
            NguoiLienHe = "Nguyễn Hoài Nam",
            DiaChi = "12 Nguyễn Văn Cừ, Đà Lạt",
            SoDienThoai = "0901234567",
            EmailLienHe = accounts[1].Email,
            TrangThaiXacMinh = HoTroDuLich.Web.Domain.TrangThaiXacMinh.DaXacMinh,
            TrangThaiQuyenHoatDong = HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.DangHoatDong
        };
        db.Add(provider);
        await db.SaveChangesAsync();
        var profile = new HoSoXacMinhNhaCungCap
        {
            MaNhaCungCap = provider.MaNhaCungCap,
            NguoiDaiDien = provider.NguoiLienHe,
            NoiDungDangKy = "Đơn vị cung cấp dịch vụ lưu trú và trải nghiệm địa phương. Hồ sơ minh họa để kiểm thử hệ thống.",
            TrangThaiXuLy = HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.DaXacMinh
        };
        db.Add(profile);
        await db.SaveChangesAsync();
        db.Add(new QuyetDinhXacMinh { MaHoSo = profile.MaHoSo, LanXuLy = 1, KetQua = HoTroDuLich.Web.Domain.KetQuaXacMinh.DaXacMinh, KetQuaXacMinh = "Dữ liệu minh họa", MaAdminDuyet = accounts[2].MaTaiKhoan });
        foreach (var(name, type, region, desc)in new[]
        {
            ("Đồi chè Cầu Đất", "Thiên nhiên", "Đà Lạt", "Những triền đồi xanh nối tiếp trong sương sớm. Thời gian tham quan gợi ý: 05:30–09:00."),
            ("Phố cổ Hội An", "Văn hóa", "Hội An", "Dạo bước qua những mái ngói cũ và những con phố bên dòng Hoài."),
            ("Vịnh Lan Hạ", "Thiên nhiên", "Hải Phòng", "Khám phá mặt nước xanh, đảo đá và nhịp sống làng chài.")
        }

        )
            db.Add(new DiaDiemDuLich { TenDiaDiem = name, LoaiDiaDiem = type, KhuVuc = region, MoTa = desc, DangHienThi = true });
        var items = new[]
        {
            ("Mây trong thung lũng", "Lưu trú", "Đà Lạt", 650000m, "Một căn phòng nhỏ, cửa sổ lớn và khoảng xanh thật yên. Lưu trú giữa vườn thông, cách trung tâm 15 phút.", "Nhận phòng 14:00 · Trả phòng 12:00"),
            ("Săn mây đón bình minh", "Hoạt động", "Đà Lạt", 350000m, "Cùng hướng dẫn viên địa phương đón những tia nắng đầu tiên trên đồi. Bao gồm di chuyển và bữa sáng nhẹ.", "04:30–08:00"),
            ("Bếp nhà phố Hội", "Ăn uống", "Hội An", 180000m, "Một bữa cơm Quảng, một khoảng sân yên. Thưởng thức món ăn địa phương theo thực đơn theo mùa.", "11:00–21:00"),
            ("Một ngày trên vịnh Lan Hạ", "Hoạt động", "Hải Phòng", 890000m, "Đi thuyền, chèo kayak và ngắm các đảo đá trong hành trình một ngày cùng người bản địa.", "08:00–16:00"),
            ("Xe máy khám phá Đà Lạt", "Phương tiện", "Đà Lạt", 150000m, "Thuê xe theo ngày, nhận xe tại trung tâm. Nhà cung cấp sẽ xác nhận loại xe và điều kiện khi phản hồi.", "07:00–20:00"),
            ("An Nhiên bên sông", "Lưu trú", "Hội An", 720000m, "Không gian nghỉ ngơi ấm áp bên sông, có xe đạp để bạn thong thả khám phá phố cổ.", "Nhận phòng 14:00 · Trả phòng 12:00")
        };
        foreach (var(name, type, region, price, desc, hours)in items)
        {
            var s = new DichVuDuLich
            {
                MaNhaCungCap = provider.MaNhaCungCap,
                TenDichVu = name,
                LoaiDichVu = type,
                KhuVucPhucVu = region,
                GiaThamKhao = price,
                MoTa = desc,
                ThoiGianPhucVu = hours,
                DieuKienPhucVu = "Giá tham khảo. Điều kiện phục vụ và thay đổi/hủy được hai bên thống nhất khi xác nhận yêu cầu. Thanh toán trực tiếp với nhà cung cấp.",
                TrangThaiCongBo = HoTroDuLich.Web.Domain.TrangThaiCongBo.DaDuyet,
                TrangThaiTiepNhan = HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan,
                PhienBanCongBo = 1
            };
            db.Add(s);
            await db.SaveChangesAsync();
            db.Add(new BanCapNhatDichVu { MaDichVu = s.MaDichVu, SoPhienBan = 1, TenDichVu = name, LoaiDichVu = type, KhuVucPhucVu = region, GiaThamKhao = price, MoTa = desc, ThoiGianPhucVu = hours, DieuKienPhucVu = s.DieuKienPhucVu, TrangThaiDuyet = HoTroDuLich.Web.Domain.TrangThaiDuyet.DaDuyet, MaAdminDuyet = accounts[2].MaTaiKhoan, ThoiDiemDuyet = DateTime.UtcNow });
        }

        var today = Services.TravelService.Today;
        db.Add(new NhuCauDongHanh { MaTaiKhoan = accounts[3].MaTaiKhoan, DiemDen = "Đà Lạt", NoiDung = "Đi chậm một chút, ngắm Đà Lạt nhiều hơn", NgayBatDau = today.AddDays(7), NgayKetThuc = today.AddDays(9), NganSachDuKien = 2500000, HinhThucDi = "Nghỉ dưỡng", MongMuonDongHanh = "Tìm 1–2 bạn thích cà phê, nhiếp ảnh và những buổi sáng săn mây. Mỗi người tự quản lý chi phí cá nhân.", PhamViChiaSe = HoTroDuLich.Web.Domain.PhamViChiaSe.CongKhai, TrangThaiCongBo = HoTroDuLich.Web.Domain.TrangThaiNhuCau.DangCongBo });
        var plan = new KeHoachChuyenDi
        {
            MaTaiKhoan = accounts[0].MaTaiKhoan,
            TenKeHoach = "Đà Lạt, những ngày thảnh thơi",
            NoiDen = "Đà Lạt",
            NgayBatDau = today.AddDays(7),
            NgayKetThuc = today.AddDays(9),
            NganSachDuKien = 3000000,
            TrangThai = HoTroDuLich.Web.Domain.TrangThaiKeHoach.Nhap
        };
        db.Add(plan);
        await db.SaveChangesAsync();
        db.AddRange(new HoatDongLichTrinh { MaKeHoach = plan.MaKeHoach, Ngay = plan.NgayBatDau, Gio = new TimeOnly(14, 0), ThuTu = 1, NoiDung = "Nhận phòng, nghỉ ngơi tại Mây trong thung lũng", ChiPhiDuKien = 650000 }, new HoatDongLichTrinh { MaKeHoach = plan.MaKeHoach, Ngay = plan.NgayBatDau.AddDays(1), Gio = new TimeOnly(5, 30), ThuTu = 2, NoiDung = "Đón bình minh tại đồi chè Cầu Đất", ChiPhiDuKien = 350000 });
        db.Add(new ThongBao { MaNguoiNhan = accounts[0].MaTaiKhoan, LoaiThongBao = "ChaoMung", NoiDung = "Chào Minh Anh! Kế hoạch Đà Lạt mẫu đã sẵn sàng để bạn khám phá.", DuongDan = "/Plans/Detail/" + plan.MaKeHoach });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
