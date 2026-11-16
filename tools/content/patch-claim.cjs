// Nha cung cap nhan mot dia diem co san trong Kham pha: mo bieu mau Them co so da dien san ten, dia chi, toa do. Chay mot lan.
const fs = require('fs');
const root = 'src/HoTroDuLich.Web/';
const patch = (file, fn) => { const f = root + file; let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n'); const b = s; s = fn(s); if (s === b) throw Error('Khong doi: ' + file); if (crlf) s = s.replace(/\n/g, '\r\n'); fs.writeFileSync(f, s); };
const rep = (s, a, b) => { if (!s.includes(a)) throw Error('Khong tim thay: ' + a.slice(0, 80)); return s.replace(a, b); };

patch('Services/BranchService.cs', s => rep(s, ' public async Task<BranchEditPage> EditPage(long id=0,BranchInput? rejected=null)', ` /// <summary>Bieu mau Them co so dien san tu mot dia diem da cong bo trong Kham pha (nha cung cap "nhan" dia diem). Chi lay ten, dia chi, khu vuc, toa do; nha cung cap van phai xac nhan vi tri va gui xet duyet nhu binh thuong.</summary>
 public async Task<BranchInput?> ClaimDraft(long entryId)
 {
  RoleRequired(ProviderRole);
  var e=await db.Set<DiscoveryEntry>().AsNoTracking().FirstOrDefaultAsync(x=>x.Id==entryId&&x.Status=="Published"&&x.Kind=="Place"&&x.ServiceId==null&&x.BranchId==null);
  if(e==null)return null;
  var tags=e.Tags??"";
  var type=tags.Contains("Lưu trú",StringComparison.OrdinalIgnoreCase)||tags.Contains("lưu trú")?"LuuTru":tags.Contains("Ăn uống",StringComparison.OrdinalIgnoreCase)||tags.Contains("ăn uống")?"AnUong":"HoatDong";
  static string Cut(string? v,int n)=>string.IsNullOrWhiteSpace(v)?"":v.Trim().Length<=n?v.Trim():v.Trim()[..n];
  return new BranchInput{Name=Cut(e.Title,200),Address=Cut(e.Address,400),Region=Cut(e.Region,200),DonViId=e.DonViId,Latitude=e.Latitude,Longitude=e.Longitude,Type=type,
   OwnershipEvidence=Cut("Nhận từ địa điểm đã có trong Khám phá (mục #"+e.Id+": "+e.Title+").",2000)};
 }
 public async Task<BranchEditPage> EditPage(long id=0,BranchInput? rejected=null)`));

patch('Controllers/BranchControllers.cs', s => rep(s, '[HttpGet("/nha-cung-cap/co-so/them")] public async Task<IActionResult> New()=>V("Branches/Edit",await branches.EditPage());',
  '[HttpGet("/nha-cung-cap/co-so/them")] public async Task<IActionResult> New(long? tu=null){var draft=tu is long entry?await branches.ClaimDraft(entry):null;if(draft!=null)ViewData["ClaimedFrom"]=draft.Name;return V("Branches/Edit",await branches.EditPage(0,draft));}'));

patch('Views/Branches/Edit.cshtml', s => rep(s, ' <a class="back" href="/nha-cung-cap/co-so">← Cơ sở của tôi</a>', ' <a class="back" href="/nha-cung-cap/co-so">← Cơ sở của tôi</a>\n @if(ViewData["ClaimedFrom"] is string claimed){<div class="notice" data-claimed>Đã điền sẵn tên, địa chỉ và vị trí từ địa điểm <strong>@claimed</strong> trong Khám phá. Hãy kiểm tra lại, bổ sung giờ mở cửa, giá, ảnh và căn cứ quản lý cơ sở trước khi gửi.</div>}'));

patch('Views/Discovery/Entry.cshtml', s => rep(s, ' <section class="ds-panel" id="report">', ' @if(e.Kind=="Place"&&e.ServiceId==null&&e.BranchId==null){<section class="ds-panel" id="claim"><h2>Bạn quản lý nơi này?</h2><p>Nhận địa điểm để bổ sung giờ mở cửa, giá, ảnh và nhận yêu cầu từ khách. Thông tin được xét duyệt trước khi công bố.</p>@if(User.IsInRole(TravelService.ProviderRole)){<a class="btn btn-outline" href="/nha-cung-cap/co-so/them?tu=@e.Id">Nhận địa điểm này</a>}else if(!signed){<a class="btn btn-outline" href="/Account/Register">Đăng ký nhà cung cấp</a>}else{<p class="help">Cần tài khoản nhà cung cấp để nhận địa điểm.</p>}</section>}\n <section class="ds-panel" id="report">'));
console.log('ok');
