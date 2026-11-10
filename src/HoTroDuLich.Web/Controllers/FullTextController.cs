using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;
public abstract class FullTextController<T>(T component, TravelService read) : AppController(read)
{
    protected readonly T C = component;
    protected List<TItem> Page<TItem>(List<TItem> rows, int page)
    {
        S.Require(page >= 1, "Số trang phải từ 1 trở lên.", 400);
        ViewBag.SimplePage = page;
        ViewBag.SimpleNext = rows.Count > page * 20;
        ViewBag.TotalRows = rows.Count;
        return rows.Skip((page - 1) * 20).Take(20).ToList();
    }

    protected List<YeuCauDatDichVu> FilterBookings(List<YeuCauDatDichVu> rows, string? state)
    {
        S.Require(string.IsNullOrEmpty(state) || new[] { "ChoPhanHoi", "DeXuatKhac", "DaXacNhan", "TuChoi", "KhongTiepTuc", "DaHuy", "ChoKiemTra", "HoanThanh", "ThucHienMotPhan", "KhongThucHien" }.Contains(state), "Bộ lọc trạng thái không hợp lệ.", 400);
        return string.IsNullOrEmpty(state) ? rows : rows.Where(x => (x.TrangThai).ToString() == state).ToList();
    }

    protected async Task<IActionResult> AgreementPage(long connection)
    {
        ViewBag.Connection = await S.Connection(connection);
        ViewBag.Destination = await S.NeedDestination(connection);
        try
        {
            ViewBag.Form = await S.AgreementForm(connection);
        }
        catch (BusinessException)
        {
        }

        return V("Companions/Agreement", await S.Agreements(connection));
    }

    protected async Task<IActionResult> ChangesPage(long id)
    {
        ViewBag.ServiceName = await S.BookingServiceName(id);
        ViewBag.Changes = await S.Changes(id);
        return V("Bookings/Changes", await S.Booking(id));
    }

    protected async Task<IActionResult> OutcomePage(long id)
    {
        ViewBag.ServiceName = await S.BookingServiceName(id);
        ViewBag.Results = await S.Results(id);
        ViewBag.Items = await S.BookingItems(id);
        return V("Bookings/Outcome", await S.Booking(id));
    }
}
