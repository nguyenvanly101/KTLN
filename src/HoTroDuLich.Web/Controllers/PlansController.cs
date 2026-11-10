using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;
[Authorize(Roles = TravelService.Traveler)]
[SucAuthorize(SucAccess.TripPlan)]
public class PlansController(TravelService s, TripWorkspaceService trips) : AppController(s)
{
    public async Task<IActionResult> Index() => View(await S.Plans());
    public async Task<IActionResult> Detail(long id)
    {
        return View(await trips.Open(id));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id = 0, long? serviceId = null, long? placeId = null)
    {
        if (id == 0)
            return View(await trips.NewPlan(serviceId, placeId));
        var p = await S.OwnPlan(id);
        return View(new PlanInput { Id = id, Version = TravelService.Token(p.RowVersion), Name = p.TenKeHoach, Destination = p.NoiDen, Start = p.NgayBatDau, End = p.NgayKetThuc, Budget = p.NganSachDuKien, Status = (p.TrangThai).ToString() });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PlanInput i) => Done(TripWorkspaceService.AfterPlanSaved(await S.SavePlan(i), i));
    [HttpGet]
    public async Task<IActionResult> Activity(long id, long activityId = 0, long? serviceId = null, long? placeId = null)
    {
        var p = await S.OwnPlan(id);
        var i = new ActivityInput
        {
            PlanId = id,
            Version = TravelService.Token(p.RowVersion),
            Date = p.NgayBatDau
        };
        ViewBag.Plan = p;
        if (activityId > 0)
        {
            var a = (await S.Activities(id)).SingleOrDefault(x => x.MaHoatDong == activityId) ?? throw new BusinessException("Không tìm thấy hoạt động trong chuyến đi.",404);
            i.Id = a.MaHoatDong;
            i.Date = a.Ngay;
            i.Time = a.Gio;
            i.Content = a.NoiDung;
            i.Cost = a.ChiPhiDuKien;
            i.DurationMinutes=a.ThoiLuongPhut;
            i.TravelMinutes=a.DiChuyenTruocPhut;
            i.ServiceId = a.MaDichVu;
            i.PlaceId = a.MaDiaDiem;
        }
        else if (serviceId != null)
        {
            var v = await S.PublicService(serviceId.Value);
            ViewBag.SourceService=v;
            i.ServiceId = v.MaDichVu;
            i.Content = v.TenDichVu;
            i.Cost = v.GiaThamKhao;
        }
        else if (placeId != null)
        {
            var v = await S.Place(placeId.Value);
            i.PlaceId = v.MaDiaDiem;
            i.Content = v.TenDiaDiem;
        }

        // Route id names the plan; form Id names the activity. Render model values, not route ModelState.
        ModelState.Clear();
        return View(i);
    }

    [HttpPost]
    public async Task<IActionResult> Activity(ActivityInput i)
    {
        await S.SaveActivity(i);
        return Done("/Plans/Detail/" + i.PlanId);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteActivity(CommandInput i, long planId)
    {
        await S.DeleteActivity(i);
        return Done("/Plans/Detail/" + planId);
    }

    [HttpPost]
    public async Task<IActionResult> Decision(CommandInput i)
    {
        await S.PlanDecision(i);
        return Done("/Plans/Detail/" + i.Id);
    }

    [HttpPost]
    public async Task<IActionResult> Reorder(CommandInput i, long planId)
    {
        await S.ReorderActivity(i);
        return Done("/Plans/Detail/" + planId);
    }

    public async Task<IActionResult> Select(long? serviceId, long? placeId)
    {
        await trips.ValidateSelection(serviceId, placeId);
        ViewBag.SelectionName = serviceId is long service ? (await S.PublicService(service)).TenDichVu
            : placeId is long place ? (await S.Place(place)).TenDiaDiem : null;
        ViewBag.ServiceId = serviceId;
        ViewBag.PlaceId = placeId;
        return View(await S.Plans());
    }
}
