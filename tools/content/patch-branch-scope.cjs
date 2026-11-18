// Nha cung cap: moi trang chi hien va dung du lieu cua co so dang chon (yeu cau dat, tin nhan).
// Mo mot yeu cau / cuoc tro chuyen cua co so khac (vd tu thong bao) thi tu chuyen sang co so do va bao ro.
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) { if (s.includes(b)) return s; throw Error('khong thay: ' + a.slice(0, 90)); } return s.replace(a, () => b); };

let b = rd('Services/BranchService.cs');
b = swap(b, `  http.HttpContext.Items.Remove("CurrentBranch");http.HttpContext.Items.Remove("OwnedBranches");
 }`, `  http.HttpContext.Items.Remove("OwnedBranches");http.HttpContext.Items["CurrentBranch"]=all.First(b=>b.Id==id);
 }
 /// <summary>Chuyen sang co so cua minh neu dang chon co so khac; tra ve co so vua chuyen, null neu khong doi.</summary>
 public async Task<OwnedBranch?> Focus(long? branchId)
 {
  if(branchId==null||Role!=ProviderRole)return null;
  var cur=await CurrentBranch();if(cur?.Id==branchId)return null;
  var target=(await OwnedBranches()).FirstOrDefault(x=>x.Id==branchId);if(target==null)return null;
  await SetCurrentBranch(target.Id);return target;
 }
 /// <summary>Co so cua mot yeu cau dat: co so ghi trong yeu cau, hoac co so cua minh dang gan dich vu do.</summary>
 public async Task<long?> BranchOfRequest(long requestId)
 {
  var snap=await db.Set<YeuCauDatCoSo>().AsNoTracking().Where(x=>x.RequestId==requestId).Select(x=>(long?)x.BranchId).FirstOrDefaultAsync();
  if(snap!=null)return snap;
  var service=await db.YeuCauDatDichVu.AsNoTracking().Where(x=>x.MaYeuCau==requestId).Select(x=>x.MaDichVu).FirstOrDefaultAsync();
  var mine=(await OwnedBranches()).Select(x=>x.Id).ToList();
  return await db.Set<DichVuCoSo>().AsNoTracking().Where(x=>x.ServiceId==service&&mine.Contains(x.BranchId)).Select(x=>(long?)x.BranchId).FirstOrDefaultAsync();
 }
 /// <summary>Chi giu yeu cau thuoc co so dang chon (theo co so ghi trong yeu cau; yeu cau cu chua ghi co so thi theo dich vu gan voi co so).</summary>
 public async Task<List<YeuCauDatDichVu>> OfCurrentBranch(List<YeuCauDatDichVu> all)
 {
  var cur=await CurrentBranch();if(cur==null)return [];if(all.Count==0)return all;
  var ids=all.Select(x=>x.MaYeuCau).ToList();
  var snaps=await db.Set<YeuCauDatCoSo>().AsNoTracking().Where(x=>ids.Contains(x.RequestId)).ToDictionaryAsync(x=>x.RequestId,x=>x.BranchId);
  var linked=await ServiceIdsOf(cur.Id);
  return all.Where(x=>snaps.TryGetValue(x.MaYeuCau,out var bid)?bid==cur.Id:linked.Contains(x.MaDichVu)).ToList();
 }`);
wr('Services/BranchService.cs', b);

let c = rd('Controllers/BookingsController.cs');
c = swap(c, 'public async Task<IActionResult> Index(){ViewBag.ServiceNames=await S.BookingServiceNames();return View(await S.Bookings());}',
  'public async Task<IActionResult> Index(){ViewBag.ServiceNames=await S.BookingServiceNames();var all=await S.Bookings();if(User.IsInRole(TravelService.ProviderRole)){ViewBag.BranchName=(await branches.CurrentBranch())?.Name;all=await branches.OfCurrentBranch(all);}return View(all);}');
c = swap(c, ' public async Task<IActionResult> Detail(long id){', ' public async Task<IActionResult> Detail(long id){await FocusRequest(id);');
c = swap(c, ' public async Task<IActionResult> Index(){', ` /// <summary>Nha cung cap mo yeu cau cua co so khac (vd tu thong bao): chuyen sang co so do de xu ly dung noi.</summary>
 async Task FocusRequest(long id){if(!User.IsInRole(TravelService.ProviderRole))return;if(await branches.Focus(await branches.BranchOfRequest(id)) is {} moved)TempData["Success"]="Đã chuyển sang cơ sở "+moved.Name+" để xử lý yêu cầu này.";}
 public async Task<IActionResult> Index(){`);
wr('Controllers/BookingsController.cs', c);

let x = rd('Controllers/FullText/XuLyYeuCauDatController.cs');
x = swap(x, 'public class XuLyYeuCauDatController(IDatDichVuService component,TravelService read):', 'public class XuLyYeuCauDatController(IDatDichVuService component,TravelService read,BranchService branches):');
x = swap(x, '{ var maYeuCau=query.MaYeuCau;', '{ var maYeuCau=query.MaYeuCau;if(await branches.Focus(await branches.BranchOfRequest(maYeuCau)) is {} moved)TempData["Success"]="Đã chuyển sang cơ sở "+moved.Name+" để xử lý yêu cầu này.";');
wr('Controllers/FullText/XuLyYeuCauDatController.cs', x);

let v = rd('Views/Bookings/Index.cshtml');
v = swap(v, '<p>@(provider?"Yêu cầu của khách ở mọi cơ sở của bạn.":', '<p>@(provider?"Yêu cầu của khách ở cơ sở "+(ViewBag.BranchName??"đang chọn")+". Đổi cơ sở ở thanh bên để xem cơ sở khác.":');
wr('Views/Bookings/Index.cshtml', v);

// Tin nhan: chi co so dang chon
let cs = rd('Services/ChatService.cs');
cs = swap(cs, `  long? providerId=Role==ProviderRole?(await MyProvider()).MaNhaCungCap:null;
  IQueryable<CuocTroChuyen> query=`, `  long? providerId=Role==ProviderRole?(await MyProvider()).MaNhaCungCap:null;
  if(providerId!=null)branchId=(await branches.CurrentBranch())?.Id??-1; // nha cung cap: chi co so dang chon
  IQueryable<CuocTroChuyen> query=`);
cs = swap(cs, `  var asSecond=await db.Set<CuocTroChuyen>().AsNoTracking().Where(c=>c.LastMessageId>c.BranchReadId&&c.LastFromTraveler&&(c.PeerId==UserId||(providerId!=null&&c.BranchId!=null&&db.Set<CoSoDichVu>().Any(b=>b.Id==c.BranchId&&b.ProviderId==providerId))))`,
  `  long? currentBranch=providerId!=null?(await branches.CurrentBranch())?.Id??-1:null;
  var asSecond=await db.Set<CuocTroChuyen>().AsNoTracking().Where(c=>c.LastMessageId>c.BranchReadId&&c.LastFromTraveler&&(c.PeerId==UserId||(providerId!=null&&c.BranchId==currentBranch&&db.Set<CoSoDichVu>().Any(b=>b.Id==c.BranchId&&b.ProviderId==providerId))))`);
cs = swap(cs, ' public async Task<int> Unread()', ' public async Task<long?> BranchOf(long conversationId)=>await db.Set<CuocTroChuyen>().AsNoTracking().Where(x=>x.Id==conversationId).Select(x=>x.BranchId).FirstOrDefaultAsync();\n public async Task<int> Unread()');
wr('Services/ChatService.cs', cs);

let cc = rd('Controllers/ChatControllers.cs');
cc = swap(cc, '  =>new(travel.Role==TravelService.ProviderRole?"Branch":"Traveler",open,start,travel.Role==TravelService.ProviderRole?await chat.MyBranches():[],branch);',
  `  {
  if(travel.Role!=TravelService.ProviderRole)return new("Traveler",open,start,[],branch);
  // Nha cung cap: chi co so dang chon; mo cuoc tro chuyen cua co so khac thi chuyen sang co so do.
  var bs=HttpContext.RequestServices.GetRequiredService<BranchService>();
  if(open is long oid&&await bs.Focus(await chat.BranchOf(oid)) is {} moved)TempData["Success"]="Đã chuyển sang cơ sở "+moved.Name+" để trả lời cuộc trò chuyện này.";
  var cur=await bs.CurrentBranch();
  return new("Branch",open,start,(await chat.MyBranches()).Where(b=>b.Id==cur?.Id).ToList(),cur?.Id);
  }`);
cc = swap(cc, '  return V("Chat/Settings",await chat.SettingsPage(coSo??mine[0].Id));', '  return V("Chat/Settings",await chat.SettingsPage(coSo??(await HttpContext.RequestServices.GetRequiredService<BranchService>().CurrentBranch())?.Id??mine[0].Id));');
wr('Controllers/ChatControllers.cs', cc);

let ib = rd('Views/Chat/Inbox.cshtml');
const i0 = ib.indexOf('<select class="form-select" data-chat-branch');
if (i0 >= 0 && !ib.slice(0, i0).endsWith('@if(Model.Branches.Count>1){')) {
  const i1 = ib.indexOf('</select>', i0) + '</select>'.length;
  ib = ib.slice(0, i0) + '@if(Model.Branches.Count>1){' + ib.slice(i0, i1) + '}' + ib.slice(i1);
}
wr('Views/Chat/Inbox.cshtml', ib);
console.log('ok');
