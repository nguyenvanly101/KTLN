// Noi cac phan da co thanh mot luong: tu dien thoi gian di chuyen, bao nhom khi khoi hanh/ket thuc, nhac danh gia, thanh vien danh dau xong va ghi su co. Chay mot lan.
const fs = require('fs');
const root = 'src/HoTroDuLich.Web/';
const patch = (file, fn) => { const f = root + file; let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n'); const b = s; s = fn(s); if (s === b) throw Error('Khong doi: ' + file); if (crlf) s = s.replace(/\n/g, '\r\n'); fs.writeFileSync(f, s); };
const rep = (s, a, b) => { if (!s.includes(a)) throw Error('Khong tim thay: ' + a.slice(0, 80)); return s.replace(a, b); };

// 1. Tu dien thoi gian di chuyen tu ket qua tinh tuyen
patch('Services/PlanBoardService.cs', s => rep(s, '\n async Task SaveNotes(long id,PlanDraft d)', `
 /// <summary>Dien "thoi gian di chuyen truoc" cho cac hang muc trong ngay tu ket qua tinh tuyen. Chi dien o hang muc chua nhap; hang muc da nhap tay duoc giu nguyen.</summary>
 public async Task<object> ApplyTravel(long planId,int day,string? mode,CancellationToken ct)
 {
  var route=await Route(planId,day,mode,ct);
  var minutes=new Dictionary<long,int>();
  foreach(var leg in route.Legs){var to=route.Points[leg.To];if(to.ActivityId is long aid)minutes[aid]=(int)Math.Clamp(Math.Round(leg.Minutes),0,720);}
  int filled=0,kept=0;
  await Execute(async()=>
  {
   var ids=minutes.Keys.ToList();
   var rows=await db.HoatDongLichTrinh.Where(a=>a.MaKeHoach==planId&&ids.Contains(a.MaHoatDong)).ToListAsync();
   foreach(var a in rows){if(a.DiChuyenTruocPhut==null){a.DiChuyenTruocPhut=minutes[a.MaHoatDong];filled++;}else kept++;}
  });
  return new{filled,kept,estimated=route.AnyEstimated};
 }

 async Task SaveNotes(long id,PlanDraft d)`));
patch('Controllers/PlanBoardController.cs', s => rep(s, ' /// <summary>Doi vi tri bam tren ban do thanh ten khu vuc', ` /// <summary>Dien thoi gian di chuyen vao cac hang muc cua mot ngay tu ket qua tinh tuyen (chi o hang muc chua nhap).</summary>
 [HttpPost("/api/lapkehoach/{id:long}/tuyen/ap-dung"),EnableRateLimiting("discovery")] public async Task<IActionResult> ApplyRoute(long id,int day,string? mode,CancellationToken ct)=>await Api(()=>board.ApplyTravel(id,day,mode,ct));
 /// <summary>Doi vi tri bam tren ban do thanh ten khu vuc`));
patch('wwwroot/js/trip-route.js', s => {
  s = rep(s, "      var box = document.createElement('div'); box.className = 'route-injected route-map'; bar.after(box);",
    `      if (d.legs.some(function (l) { return pts[l.to].kind === 'item'; })) {
        var fill = document.createElement('div'); fill.className = 'route-injected route-fill';
        fill.innerHTML = '<button type="button" class="btn btn-sm btn-outline-secondary">Điền thời gian di chuyển vào lịch</button><span role="status"></span>';
        var fb = fill.querySelector('button'), fs = fill.querySelector('span');
        fb.addEventListener('click', function () {
          fb.disabled = true; fs.textContent = 'Đang lưu…';
          var token = (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
          fetch('/api/lapkehoach/' + planId + '/tuyen/ap-dung?day=' + day + '&mode=' + modeSel.value, { method: 'POST', credentials: 'same-origin', headers: { RequestVerificationToken: token } })
            .then(function (r) { return r.json().then(function (j) { if (!r.ok) throw new Error(j.message || 'Không lưu được.'); return j; }); })
            .then(function (j) { fs.textContent = 'Đã điền ' + j.filled + ' hạng mục' + (j.kept ? ', giữ nguyên ' + j.kept + ' hạng mục bạn đã nhập' : '') + (j.estimated ? ' (có đoạn là ước tính)' : '') + '.'; })
            .catch(function (e) { fs.textContent = e.message; fb.disabled = false; });
        });
        bar.after(fill);
      }
      var box = document.createElement('div'); box.className = 'route-injected route-map'; bar.after(box);`);
  return s;
});

// 2. Thanh vien chuyen di chung danh dau xong / bo qua va ghi su co
patch('Services/JourneyService.cs', s => rep(s, 'var plan=await travel.OwnPlan(planId);travel.CheckVersion(plan,input.Version);', 'var plan=await travel.SharedPlan(planId);travel.CheckVersion(plan,input.Version);'));
patch('Services/Components/XuLyTinhHuongService.cs', s => rep(s, 'Ensure(await Resolve<IKeHoach>().LaChuKeHoach(UserId,i.PlanId),"0501-01");\n            Ensure(await db.HoatDongLichTrinh.CountAsync(x => ids.Contains(x.MaHoatDong) && x.MaKeHoach == i.PlanId) == ids.Length, "0501-03");',
  '// Chu chuyen di hoac thanh vien da chap nhan cua chuyen di chung deu ghi duoc su co.\n            Ensure(await Resolve<IKeHoach>().LaChuKeHoach(UserId,i.PlanId)||await db.Set<ThanhVienChuyenDi>().AnyAsync(m=>m.PlanId==i.PlanId&&m.AccountId==UserId&&m.Status=="Accepted"),"0501-01");\n            Ensure(await db.HoatDongLichTrinh.CountAsync(x => ids.Contains(x.MaHoatDong) && x.MaKeHoach == i.PlanId) == ids.Length, "0501-03");'));

// 3. Bao nhom chat khi khoi hanh / ket thuc
patch('Controllers/JourneyControllers.cs', s => {
  s = rep(s, '[HttpPost] public async Task<IActionResult> Depart(long planId){await journey.Depart(planId);return Done(', '[HttpPost] public async Task<IActionResult> Depart(long planId){await journey.Depart(planId);await TellGroup(planId,"Mình vừa bấm Khởi hành cho chuyến đi. Mọi người xem lịch hôm nay và đường đi trong trang chuyến đi nhé.");return Done(');
  s = rep(s, '[HttpPost] public async Task<IActionResult> Finish(long planId){await journey.Finish(planId);return Done(', '[HttpPost] public async Task<IActionResult> Finish(long planId){await journey.Finish(planId);await TellGroup(planId,"Chuyến đi đã kết thúc. Cảm ơn mọi người đã đi cùng; nhớ chia tiền và đánh giá dịch vụ đã dùng nhé.");return Done(');
  s = rep(s, '    [HttpGet] public async Task<IActionResult> Share(long planId)=>View(await journey.Shares(planId));', `    /// <summary>Gui mot tin vao nhom chat cua chuyen di (neu co). Loi gui tin khong lam hong thao tac chinh.</summary>
    async Task TellGroup(long planId,string text)
    {
        try
        {
            var chat=HttpContext.RequestServices.GetRequiredService<ChatService>();
            if(await chat.TripGroupFor(planId) is long group)await chat.Send(new ChatSendInput{ConversationId=group,Body=text});
        }
        catch(Exception e) when(e is BusinessException or Microsoft.EntityFrameworkCore.DbUpdateException){}
    }
    [HttpGet] public async Task<IActionResult> Share(long planId)=>View(await journey.Shares(planId));`);
  return s;
});

// 4. Trang Hom nay: thanh vien cung co nut Xong/Bo qua; sau khi ket thuc nhac danh gia va chia tien
patch('Views/Journey/Today.cshtml', s => {
  s = rep(s, '@if(Model.IsOwner&&next.Ngay<=TravelService.Today){<div class="next-actions">', '@if(next.Ngay<=TravelService.Today){<div class="next-actions">');
  s = rep(s, '</span></div></div>}\nelse if(prog?.DepartedAt!=null)', '</span></div><div class="depart-links">@if(Model.Trip.Bookings.Count>0){<a class="btn btn-outline" href="/Reviews/Mine">Đánh giá dịch vụ đã dùng</a>}<a class="btn btn-outline" href="/Expenses?planId=@p.MaKeHoach">Xem chi tiêu</a><a class="btn btn-outline" href="/Journey/Share?planId=@p.MaKeHoach">Chia sẻ chuyến đi</a></div></div>}\nelse if(prog?.DepartedAt!=null)');
  return s;
});
patch('wwwroot/css/social.css', s => s + `
.route-fill{display:flex;flex-wrap:wrap;gap:10px;align-items:center;margin:0 0 10px;font-size:.9rem;color:#334155}
.depart-links{display:flex;flex-wrap:wrap;gap:8px}
`);
console.log('ok');
