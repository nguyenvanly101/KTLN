namespace HoTroDuLich.Web.Services;
public static class ComponentRegistration
{
    public static IServiceCollection AddFullTextComponents(this IServiceCollection services)
    {
        services.AddScoped<IDanhMucService, DanhMucService>();
        services.AddScoped<ITimKiemThongTinService, TimKiemThongTinService>();
        services.AddScoped<ILapKeHoachService, LapKeHoachService>();
        services.AddScoped<IXuLyTinhHuongService, XuLyTinhHuongService>();
        services.AddScoped<ITimNguoiDongHanhService, TimNguoiDongHanhService>();
        services.AddScoped<ICongBoNhuCauService, CongBoNhuCauService>();
        services.AddScoped<IKetNoiDongHanhService, KetNoiDongHanhService>();
        services.AddScoped<IThoaThuanDongHanhService, ThoaThuanDongHanhService>();
        services.AddScoped<IQuanLyDichVuService, QuanLyDichVuService>();
        services.AddScoped<IXetDuyetDichVuService, XetDuyetDichVuService>();
        services.AddScoped<IDatDichVuService, DatDichVuService>();
        services.AddScoped<IDieuChinhDichVuService, DieuChinhDichVuService>();
        services.AddScoped<IKetQuaDichVuService, KetQuaDichVuService>();
        services.AddScoped<IDangKyService, DangKyService>();
        services.AddScoped<IXacMinhNhaCungCapService, XacMinhNhaCungCapService>();
        services.AddScoped<IDangNhapService, DangNhapService>();
        services.AddScoped<IHoSoNguoiDungService, HoSoNguoiDungService>();
        services.AddScoped<IQuyenHoatDongService, QuyenHoatDongService>();
        services.AddScoped<INgungTaiKhoanService, NgungTaiKhoanService>();
        services.AddScoped<IThongBaoService, ThongBaoService>();
        services.AddScoped<INhatKyService, NhatKyService>();
        services.AddScoped<ILuuTepService, LuuTepService>();
        return services;
    }
}
