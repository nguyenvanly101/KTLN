using HoTroDuLich.Web.Domain;

namespace HoTroDuLich.Web.Models;

public sealed record TripTask(string Title, string Description, string Url, string Action, bool Attention = false);
public sealed record TripLocation(string Query, bool HasAddress);
public sealed record TripWorkspace(
    KeHoachChuyenDi Plan,
    List<HoatDongLichTrinh> Activities,
    List<YeuCauDatDichVu> Bookings,
    Dictionary<long, string> ServiceNames,
    Dictionary<long, TripLocation> Locations,
    HashSet<long> RequestableServices,
    List<TinhHuongPhatSinh> Incidents,
    List<TripTask> Tasks, int PreparationCount=0, int PreparationDone=0, decimal ActualSpent=0);
