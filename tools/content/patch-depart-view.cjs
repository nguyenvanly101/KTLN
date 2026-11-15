// Giao dien Khoi hanh / Bo qua tren trang Hom nay. Chay mot lan.
const fs = require('fs');
const root = 'src/HoTroDuLich.Web/';
const patch = (file, fn) => { const f = root + file; let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n'); const b = s; s = fn(s); if (s === b) throw Error('Khong doi: ' + file); if (crlf) s = s.replace(/\n/g, '\r\n'); fs.writeFileSync(f, s); };
const rep = (s, a, b) => { if (!s.includes(a)) throw Error('Khong tim thay: ' + a.slice(0, 80)); return s.replace(a, b); };

patch('Services/JourneyService.cs', s => rep(s, 'var plan=await travel.OwnPlan(planId);\n            travel.Require(clock.HomNay>=plan.NgayBatDau.AddDays(-1)', 'var plan=await travel.OwnPlan(planId);\n            travel.Require(plan.MaTaiKhoan==travel.UserId,"Chỉ chủ chuyến đi mới khởi hành được.",403);\n            travel.Require(clock.HomNay>=plan.NgayBatDau.AddDays(-1)'));
patch('Services/JourneyService.cs', s => rep(s, 'var plan=await travel.OwnPlan(planId);\n            var row=await db.Set<TripProgress>().FirstOrDefaultAsync(p=>p.PlanId==planId);\n            travel.Require(row?.DepartedAt!=null', 'var plan=await travel.OwnPlan(planId);\n            travel.Require(plan.MaTaiKhoan==travel.UserId,"Chỉ chủ chuyến đi mới kết thúc được chuyến.",403);\n            var row=await db.Set<TripProgress>().FirstOrDefaultAsync(p=>p.PlanId==planId);\n            travel.Require(row?.DepartedAt!=null'));

patch('Views/Journey/Today.cshtml', s => {
  s = rep(s, 'var next=Model.Items.FirstOrDefault(x=>x.HoanThanhLuc==null);var done=Model.Items.Count(x=>x.HoanThanhLuc!=null);',
    'var next=Model.Items.FirstOrDefault(x=>x.HoanThanhLuc==null&&x.BoQuaLuc==null);var done=Model.Items.Count(x=>x.HoanThanhLuc!=null);var skipped=Model.Items.Count(x=>x.BoQuaLuc!=null);var prog=Model.Progress;');
  s = rep(s, '<span>hoạt động đã hoàn thành</span>', '<span>hoạt động đã hoàn thành@(skipped>0?" · "+skipped+" bỏ qua":"")</span>');
  const token = '@Html.AntiForgeryToken()<input type="hidden" name="planId" value="@p.MaKeHoach"/>';
  s = rep(s, 'Chia sẻ / in</a></nav>', `Chia sẻ / in</a></nav>
@if(prog?.FinishedAt!=null){<div class="depart-card is-finished" id="depart"><div><strong>Chuyến đi đã kết thúc</strong><span>Kết thúc lúc @Ui.LocalTime(prog.FinishedAt.Value).ToString("HH:mm dd/MM/yyyy")@(prog.DepartedAt!=null?" · khởi hành lúc "+Ui.LocalTime(prog.DepartedAt.Value).ToString("HH:mm"):"")</span></div></div>}
else if(prog?.DepartedAt!=null){<div class="depart-card is-on" id="depart"><div><strong>Đang trên đường</strong><span>Khởi hành lúc @Ui.LocalTime(prog.DepartedAt.Value).ToString("HH:mm dd/MM/yyyy")</span></div>@if(Model.IsOwner){<form action="/Journey/Finish" method="post" onsubmit="return confirm('Kết thúc chuyến đi này?')">${token}<button class="btn btn-outline" type="submit">Kết thúc chuyến</button></form>}</div>}
else if(Model.IsOwner&&TravelService.Today>=p.NgayBatDau.AddDays(-1)&&TravelService.Today<=p.NgayKetThuc){<div class="depart-card" id="depart"><div><strong>Sẵn sàng khởi hành?</strong><span>@(string.IsNullOrWhiteSpace(Model.OriginName)?"Chưa đặt điểm xuất phát (thêm trong Sửa kế hoạch). ":"Xuất phát từ "+Model.OriginName+". ")@(next!=null?"Điểm dừng đầu tiên: "+next.NoiDung+".":"")</span></div><form action="/Journey/Depart" method="post">${token}<button class="btn btn-lime" type="submit">Khởi hành</button></form></div>}`);
  s = rep(s, '</a>}}else{<p>Xem ngày khác hoặc thêm một trải nghiệm khi bạn muốn.</p>}',
    `</a>}@if(Model.IsOwner&&next.Ngay<=TravelService.Today){<div class="next-actions"><form action="/Journey/Complete" method="post">${token}<input type="hidden" name="Id" value="@next.MaHoatDong"/><input type="hidden" name="Version" value="@TravelService.Token(p.RowVersion)"/><button class="btn btn-lime" name="Decision" value="Done" type="submit">Xong, đến điểm tiếp theo</button></form><form action="/Journey/Complete" method="post">${token}<input type="hidden" name="Id" value="@next.MaHoatDong"/><input type="hidden" name="Version" value="@TravelService.Token(p.RowVersion)"/><button class="btn btn-outline-light" name="Decision" value="Skip" type="submit">Bỏ qua</button></form></div>}}else{<p>Xem ngày khác hoặc thêm một trải nghiệm khi bạn muốn.</p>}`);
  s = rep(s, '@(a.HoanThanhLuc!=null?"is-complete":"")', '@(a.HoanThanhLuc!=null?"is-complete":a.BoQuaLuc!=null?"is-skipped":"")');
  s = rep(s, '@if(a.HoanThanhLuc!=null){<span class="badge journey-status">Đã hoàn thành</span>}', '@if(a.HoanThanhLuc!=null){<span class="badge journey-status">Đã hoàn thành</span>}@if(a.BoQuaLuc!=null){<span class="badge journey-status is-skipped">Đã bỏ qua</span>}');
  s = rep(s, '<button class="btn btn-outline btn-small" name="Decision" value="@(a.HoanThanhLuc==null?"Done":"Undo")" type="submit">@(a.HoanThanhLuc==null?"Đánh dấu xong":"Mở lại")<span class="sr-only"> @a.NoiDung</span></button>',
    '@{var pendingAct=a.HoanThanhLuc==null&&a.BoQuaLuc==null;}<button class="btn btn-outline btn-small" name="Decision" value="@(pendingAct?"Done":"Undo")" type="submit">@(pendingAct?"Đánh dấu xong":"Mở lại")<span class="sr-only"> @a.NoiDung</span></button>@if(pendingAct){<button class="btn btn-outline btn-small" name="Decision" value="Skip" type="submit">Bỏ qua<span class="sr-only"> @a.NoiDung</span></button>}');
  return s;
});

patch('wwwroot/css/social.css', s => s + `
.depart-card{display:flex;flex-wrap:wrap;gap:12px;align-items:center;justify-content:space-between;margin:12px 0;padding:14px 16px;border:1px solid #bfd0ff;background:#f4f7ff;border-radius:14px}
.depart-card strong{display:block}.depart-card span{color:#475569;font-size:.92rem}.depart-card.is-on{border-color:#99e0c9;background:#effaf5}.depart-card.is-finished{border-color:#dfe3ea;background:#f8fafc}
.next-actions{display:flex;flex-wrap:wrap;gap:8px;margin-top:10px}.next-actions form{margin:0}
.day-activity.is-skipped{opacity:.7}.journey-status.is-skipped{background:#f1f5f9;color:#475569}
`);
console.log('ok');
