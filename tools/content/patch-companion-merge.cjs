// Gop "tim ban dong hanh" ve Bang tin: nhan tin duoc voi nguoi co bai tim ban, moi vao chuyen di ngay trong chat,
// an trang Dong hanh (/Companions, /dong-hanh/...) va nhom Cung di (/Together). Chay mot lan.
const fs = require('fs');
const root = 'src/HoTroDuLich.Web/';
const patch = (file, fn) => { const f = root + file; let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n'); const b = s; s = fn(s); if (s === b) throw Error('Khong doi: ' + file); if (crlf) s = s.replace(/\n/g, '\r\n'); fs.writeFileSync(f, s); };
const rep = (s, a, b) => { if (!s.includes(a)) throw Error('Khong tim thay: ' + a.slice(0, 90)); return s.replace(a, b); };

// ---- 1. Chat: nguoi dang co bai "Tim ban dong hanh" cong khai con han thi nhan tin duoc
patch('Services/ChatService.cs', s => {
  s = rep(s, 'await db.NhuCauDongHanh.AsNoTracking().AnyAsync(n=>n.MaTaiKhoan==accountId&&n.TrangThaiCongBo==TrangThaiNhuCau.DangCongBo),"Chỉ nhắn tin được với bạn bè hoặc người đang có lời rủ đi cùng công khai.",403);',
    'await db.NhuCauDongHanh.AsNoTracking().AnyAsync(n=>n.MaTaiKhoan==accountId&&n.TrangThaiCongBo==TrangThaiNhuCau.DangCongBo)\n   ||await db.Set<BaiViet>().AsNoTracking().AnyAsync(p=>p.AuthorId==accountId&&p.Kind=="Companion"&&p.Visibility=="Public"&&p.DeletedAt==null&&p.TripEnd>=TravelService.Today),"Chỉ nhắn tin được với bạn bè hoặc người đang có bài tìm bạn đồng hành công khai.",403);');
  s = rep(s, '.Select(n=>n.MaTaiKhoan).Distinct().ToListAsync()).ToHashSet();\n  var talked=',
    '.Select(n=>n.MaTaiKhoan).Distinct().ToListAsync()).ToHashSet();\n  withNeed.UnionWith(await db.Set<BaiViet>().AsNoTracking().Where(p=>ids.Contains(p.AuthorId)&&p.Kind=="Companion"&&p.Visibility=="Public"&&p.DeletedAt==null&&p.TripEnd>=TravelService.Today).Select(p=>p.AuthorId).Distinct().ToListAsync());\n  var talked=');
  return s;
});

// ---- 2. Moi vao chuyen di: ban be hoac nguoi da co cuoc tro chuyen rieng
patch('Services/TripShareService.cs', s => {
  s = rep(s, 'public sealed record TripMembersPage(', '/// <summary>Chuyen di cua toi co the moi mot nguoi vao. State: can, pending, accepted, full.</summary>\npublic sealed record TripInviteOption(long Id,string Name,string Destination,DateOnly Start,DateOnly End,string State);\npublic sealed record TripMembersPage(');
  s = rep(s, '  Require(await friends.AreFriends(UserId,accountId),"Chỉ mời được bạn bè của bạn.",403);', '  Require(await friends.AreFriends(UserId,accountId)||await Talked(accountId),"Chỉ mời được bạn bè hoặc người bạn đã trò chuyện.",403);');
  s = rep(s, ' public async Task Invite(long planId,long accountId)', ` Task<bool> Talked(long accountId)=>db.Set<CuocTroChuyen>().AsNoTracking().AnyAsync(x=>x.BranchId==null&&!x.IsGroup&&((x.TravelerId==UserId&&x.PeerId==accountId)||(x.TravelerId==accountId&&x.PeerId==UserId)));
 /// <summary>Cac chuyen di sap toi cua toi kem tinh trang moi voi mot nguoi (de chon ngay trong chat).</summary>
 public async Task<List<TripInviteOption>> InvitablePlans(long accountId)
 {
  RoleRequired(Traveler);
  var plans=await db.KeHoachChuyenDi.AsNoTracking().Where(p=>p.MaTaiKhoan==UserId&&p.NgayKetThuc>=TravelService.Today).Select(p=>new{p.MaKeHoach,p.TenKeHoach,p.NoiDen,p.NgayBatDau,p.NgayKetThuc}).ToListAsync();
  var ids=plans.Select(p=>p.MaKeHoach).ToList();
  var members=await db.Set<ThanhVienChuyenDi>().AsNoTracking().Where(m=>ids.Contains(m.PlanId)).Select(m=>new{m.PlanId,m.AccountId,m.Status}).ToListAsync();
  return plans.OrderBy(p=>p.NgayBatDau).Take(30).Select(p=>
  {
   var mine=members.FirstOrDefault(m=>m.PlanId==p.MaKeHoach&&m.AccountId==accountId);
   var state=mine!=null?(mine.Status=="Pending"?"pending":"accepted"):members.Count(m=>m.PlanId==p.MaKeHoach)>=MaxMembers?"full":"can";
   return new TripInviteOption(p.MaKeHoach,p.TenKeHoach,p.NoiDen,p.NgayBatDau,p.NgayKetThuc,state);
  }).ToList();
 }

 public async Task Invite(long planId,long accountId)`);
  return s;
});
patch('Controllers/ChatControllers.cs', s => rep(s, ' [HttpGet("{id:long}/ket-noi")]', ` /// <summary>Chuyen di cua toi co the moi nguoi dang tro chuyen vao.</summary>
 [HttpGet("nguoi/{accountId:long}/chuyen-di")] public Task<IActionResult> InvitablePlans(long accountId)=>Run(async()=>await HttpContext.RequestServices.GetRequiredService<TripShareService>().InvitablePlans(accountId));
 [HttpPost("moi-chuyen-di"),EnableRateLimiting("chat")] public Task<IActionResult> InviteToPlan([FromForm]long planId,[FromForm]long accountId)=>Run(async()=>{await HttpContext.RequestServices.GetRequiredService<TripShareService>().Invite(planId,accountId);return new{ok=true};});
 [HttpGet("{id:long}/ket-noi")]`));

// ---- 3. Giao dien chat: "Moi vao chuyen di" thay cho "Ru di cung"
patch('Views/Chat/Inbox.cshtml', s => {
  s = rep(s, 'Mở trang cơ sở hoặc lời rủ đi cùng rồi bấm “Nhắn tin”.', 'Mở trang cơ sở hoặc một bài tìm bạn đồng hành rồi bấm “Nhắn tin”.');
  s = rep(s, '<strong>Rủ đi cùng</strong><p class="help">Chọn chuyến bạn muốn cùng đi. Người nhận đồng ý ngay trong cuộc trò chuyện, sau đó hai bạn lập thỏa thuận đi cùng.</p>', '<strong>Mời vào chuyến đi</strong><p class="help">Chọn một chuyến đi của bạn. Người nhận chấp nhận trong mục Chuyến đi, sau đó hai bạn cùng sửa lịch trình và có nhóm chat của chuyến.</p><p class="help" data-connect-empty hidden>Bạn chưa có chuyến đi nào còn mời được người này. <a class="text-link" href="/lapkehoach">Lập kế hoạch mới</a></p>');
  s = rep(s, '<label>Chuyến đi<select class="form-select" data-connect="need"></select></label>', '<label data-connect-pick>Chuyến đi của bạn<select class="form-select" data-connect="need"></select></label>');
  s = rep(s, '<label>Lời giới thiệu (tùy chọn)<textarea class="form-control" rows="2" maxlength="1000" data-connect="text" placeholder="Bạn là ai, thích đi kiểu nào, dự định ra sao…"></textarea></label>', '<label data-connect-pick>Lời nhắn (tùy chọn)<textarea class="form-control" rows="2" maxlength="500" data-connect="text" placeholder="Vài dòng về chuyến đi và điều bạn mong muốn…"></textarea></label>');
  s = rep(s, '<button class="btn btn-dark" type="submit">Gửi lời rủ</button>', '<button class="btn btn-dark" type="submit" data-connect-send>Gửi lời mời</button>');
  s = rep(s, 'data-chat-connect-toggle hidden>@Ui.Icon("users") Rủ đi cùng</button>', 'data-chat-connect-toggle hidden>@Ui.Icon("users") Mời vào chuyến đi</button>');
  return s;
});
patch('wwwroot/js/chat.js', s => {
  const a = s.indexOf('  function setupConnect(peerId, canSend) {'), b = s.indexOf('  // ---------------------------------------------------------------- phia co so');
  if (a < 0 || b < a) throw Error('Khong tim thay khoi ru di cung trong chat.js');
  const block = `  function setupConnect(peerId, canSend) {
    el.connectToggle.hidden = true; el.connect.hidden = true; st.peerId = peerId || null; st.invitable = [];
    if (!peerId || !canSend) return;
    api('/chat/nguoi/' + peerId + '/chuyen-di').then(function (list) {
      if (st.peerId !== peerId) return;
      var open = list.filter(function (p) { return p.state === 'can'; }); st.invitable = open;
      var sel = el.connect.querySelector('[data-connect="need"]'); sel.replaceChildren();
      open.forEach(function (p) { var o = h('option', null, p.name + ' · ' + p.destination + ' · ' + dmy(p.start) + ' – ' + dmy(p.end)); o.value = p.id; sel.appendChild(o); });
      el.connect.querySelectorAll('[data-connect-pick]').forEach(function (x) { x.hidden = !open.length; });
      el.connect.querySelector('[data-connect-send]').hidden = !open.length;
      el.connect.querySelector('[data-connect-empty]').hidden = !!open.length;
      var waiting = list.filter(function (p) { return p.state === 'pending'; }), joined = list.filter(function (p) { return p.state === 'accepted'; });
      if (joined.length) el.meta.appendChild(h('span', null, 'Cùng chuyến đi: ' + joined.map(function (p) { return p.name; }).join(' · ')));
      if (waiting.length) el.meta.appendChild(h('span', null, 'Đã mời, chờ trả lời: ' + waiting.map(function (p) { return p.name; }).join(' · ')));
      el.connectToggle.hidden = false;
    }).catch(function () {});
  }
  if (el.connectToggle) {
    el.connectToggle.addEventListener('click', function () { el.connect.hidden = !el.connect.hidden; });
    el.connectClose.addEventListener('click', function () { el.connect.hidden = true; });
    el.connect.addEventListener('submit', function (e) {
      e.preventDefault(); if (st.sending || !st.peerId) return;
      var planId = el.connect.querySelector('[data-connect="need"]').value, note = el.connect.querySelector('[data-connect="text"]').value.trim();
      var plan = (st.invitable || []).filter(function (p) { return String(p.id) === String(planId); })[0]; if (!plan) return;
      var fd = new FormData(); fd.append('planId', planId); fd.append('accountId', st.peerId);
      st.sending = true; showError('');
      api('/chat/moi-chuyen-di', { method: 'POST', body: fd }).then(function () {
        // bao ngay trong cuoc tro chuyen de nguoi nhan biet can mo muc Chuyen di
        var msg = new FormData(); msg.append('Body', 'Mình vừa mời bạn vào chuyến đi “' + plan.name + '” (' + plan.destination + ', ' + dmy(plan.start) + ' – ' + dmy(plan.end) + '). Bạn mở mục Chuyến đi để chấp nhận nhé.' + (note ? '\\n' + note : ''));
        if (st.openId) msg.append('ConversationId', st.openId); else if (st.startPeer) msg.append('PeerId', st.startPeer);
        return api('/chat/gui', { method: 'POST', body: msg });
      }).then(function (r) {
        el.connect.hidden = true; el.connect.querySelector('[data-connect="text"]').value = '';
        if (!st.openId) { st.openId = r.conversationId; st.startPeer = null; history.replaceState({ id: r.conversationId }, '', base + '/' + r.conversationId); return openThread(r.conversationId, true).then(loadList); }
        fetchNew(); loadList(); setupConnect(st.peerId, true);
      }).catch(function (err) { showError(err.message); }).then(function () { st.sending = false; });
    });
  }

`;
  return s.slice(0, a) + block + s.slice(b);
});

// ---- 4. Bang tin mo thang tab tim ban dong hanh qua ?tab=dong-hanh
patch('wwwroot/js/posts.js', s => rep(s, "    open(isProv ? (branches0.length ? 'branch' : 'public') : 'home');\n", "    open(isProv ? (branches0.length ? 'branch' : 'public') : (/[?&]tab=dong-hanh(&|$)/.test(location.search) ? 'companion' : 'home'));\n"));

// ---- 5. Go loi vao cu
patch('Views/Explore/Index.cshtml', s => {
  s = rep(s, '<a href="/Companions">@Ui.Icon("users") Tìm người đồng hành</a>', '<a href="/bang-tin?tab=dong-hanh">@Ui.Icon("users") Tìm bạn đồng hành</a>');
  s = rep(s, '<a href="/Companions" class="btn btn-lime">Tìm người đồng hành @Ui.Icon("arrow")</a>@if(canPlan){<a href="/Companions/Edit" class="text-link">Hoặc đăng lời rủ của bạn @Ui.Icon("plus")</a>}', '<a href="/bang-tin?tab=dong-hanh" class="btn btn-lime">Tìm bạn đồng hành @Ui.Icon("arrow")</a>@if(canPlan){<a href="/Plans" class="text-link">Hoặc mời bạn bè vào chuyến đi của bạn @Ui.Icon("arrow")</a>}');
  s = rep(s, 'Có người cũng đang muốn đi nơi bạn muốn đến. Tìm nhau, trao đổi lịch trình và cùng viết thêm những câu chuyện.', 'Có người cũng đang muốn đi nơi bạn muốn đến. Đăng bài tìm bạn trên Bảng tin, nhắn tin làm quen rồi mời nhau vào cùng một kế hoạch.');
  return s;
});
patch('Views/Plans/Index.cshtml', s => rep(s, '<a href="/Companions/Connections">@Ui.Icon("users") Kết nối đồng hành @Ui.Icon("arrow")</a>', '<a href="/bang-tin?tab=dong-hanh">@Ui.Icon("users") Tìm bạn đồng hành @Ui.Icon("arrow")</a>'));
patch('Views/Plans/_TripProductShortcuts.cshtml', s => rep(s, '<a href="/Together/Edit?planId=@Model.Plan.MaKeHoach">@Ui.Icon("users")<span><strong>Rủ bạn cùng chuyến</strong><small>Tạo nhóm, thống nhất lịch và chuẩn bị cùng nhau</small></span>@Ui.Icon("arrow")</a>', '<a href="/Plans/Detail/@Model.Plan.MaKeHoach#trip-members">@Ui.Icon("users")<span><strong>Mời bạn cùng chuyến</strong><small>Bạn bè chấp nhận thì cùng sửa lịch và có nhóm chat</small></span>@Ui.Icon("arrow")</a>'));
patch('Views/Shared/_Layout.cshtml', s => {
  s = rep(s, '<a class="@(route=="Together"?"active":"")" href="/Together/Moderation">Báo cáo đồng hành</a>', '');
  s = rep(s, '<a href="/Companions">Tìm người đồng hành</a>', '<a href="/bang-tin?tab=dong-hanh">Tìm bạn đồng hành</a>');
  return s;
});

// ---- 6. An hai luong cu: chuyen huong moi dia chi, bat lai bang cau hinh Features:LegacyCompanion=true
fs.writeFileSync(root + 'Services/LegacyCompanionFilter.cs', `using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HoTroDuLich.Web.Services;

/// <summary>
/// Trang Dong hanh cu (nhu cau, de nghi ket noi, thoa thuan) va nhom Cung di da duoc gop ve Bang tin + ket ban + moi vao ke hoach.
/// Moi dia chi cu duoc chuyen huong; du lieu va ma van giu. Dat Features:LegacyCompanion=true de mo lai.
/// </summary>
public sealed class LegacyCompanionFilter(IConfiguration configuration):IActionFilter
{
 static readonly HashSet<string> Legacy=new(StringComparer.OrdinalIgnoreCase){"Companions","Together","CongBoNhuCau","GuiDeNghiKetNoi","KetThucThoaThuan","LapThoaThuan","PhanHoiDeNghi","SoanNhuCau","TimNhuCau","XacNhanThoaThuan","XemNhuCauChiaSe"};
 public const string Target="/bang-tin?tab=dong-hanh";
 public void OnActionExecuting(ActionExecutingContext context)
 {
  if(configuration.GetValue<bool>("Features:LegacyCompanion"))return;
  var name=context.RouteData.Values["controller"] as string;
  if(name==null||!Legacy.Contains(name))return;
  context.Result=new RedirectResult(context.HttpContext.User.IsInRole(TravelService.Admin)?"/Admin":Target);
 }
 public void OnActionExecuted(ActionExecutedContext context){}
}
`);
patch('Program.cs', s => rep(s, '    o.Filters.Add<NhatKyAntiforgeryFilter>();', '    o.Filters.Add<NhatKyAntiforgeryFilter>();\n    o.Filters.Add<LegacyCompanionFilter>();'));
console.log('ok');
