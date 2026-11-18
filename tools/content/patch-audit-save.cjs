// Ra soat: nut "Luu" o Kham pha luu thang vao tai khoan (danh sach "Da luu"), trang /Saved hien ca cac muc luu tu Kham pha.
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) throw Error('khong thay: ' + a.slice(0, 90)); return s.replace(a, () => b); };

// ---- Dich vu
let dc = rd('Services/DiscoveryCommunity.cs');
dc = swap(dc, ` public async Task Collection(DiscoveryCommand i)`, ` /// <summary>Danh sach luu nhanh cua tai khoan: nut "Luu" tren the Kham pha luu vao day, trang "Da luu" doc lai.</summary>
 public const string QuickListName="Đã lưu";
 IQueryable<DiscoveryCollectionItem> QuickItems()=>db.Set<DiscoveryCollectionItem>().Where(x=>!x.Removed&&db.Set<DiscoveryCollection>().Any(c=>c.Id==x.CollectionId&&c.OwnerId==Me&&!c.Archived&&c.Name==QuickListName));
 /// <summary>Luu mot muc vao danh sach "Da luu" cua tai khoan. Tra ve false neu da luu tu truoc.</summary>
 public async Task<bool> QuickSave(long entryId)
 {
  Signed();if(await QuickItems().AnyAsync(x=>x.EntryId==entryId))return false;
  await Collection(new DiscoveryCommand{Decision="Import",Name=QuickListName,EntryIds=[entryId]});return true;
 }
 public async Task QuickRemove(long entryId)
 {
  Signed();var rows=await QuickItems().Where(x=>x.EntryId==entryId).ToListAsync();Need(rows.Count>0,"Mục này không còn trong danh sách đã lưu.",404);
  foreach(var r in rows){r.Removed=true;r.UpdatedAt=clock.BayGioUtc;}await db.SaveChangesAsync();
 }
 /// <summary>Cac muc da luu tu Kham pha (moi nhat truoc), chi gom muc con cong khai.</summary>
 public async Task<List<DiscoveryEntry>> QuickSaved()
 {
  Signed();var rows=await QuickItems().AsNoTracking().OrderByDescending(x=>x.UpdatedAt).Select(x=>x.EntryId).Take(100).ToListAsync();if(rows.Count==0)return [];
  var list=await Public().AsNoTracking().Where(e=>rows.Contains(e.Id)).ToListAsync();await Hydrate(list);
  return list.OrderBy(e=>rows.IndexOf(e.Id)).ToList();
 }
 public async Task Collection(DiscoveryCommand i)`);
wr('Services/DiscoveryCommunity.cs', dc);

// ---- API
let api = rd('Controllers/DiscoveryApiController.cs');
api = swap(api, ` // Diem cho tuyen:`, ` // Luu nhanh vao tai khoan (khach chua dang nhap: trinh duyet tu luu tren thiet bi).
 [HttpPost("luu"),Microsoft.AspNetCore.Authorization.Authorize]public async Task<IActionResult> QuickSave([FromBody]QuickSaveInput i){try{var added=await discovery.QuickSave(i.EntryId);return Ok(new{added});}catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}}
 public sealed record QuickSaveInput(long EntryId);
 // Diem cho tuyen:`);
wr('Controllers/DiscoveryApiController.cs', api);

// ---- Trang Da luu
let pc = rd('Controllers/ProductControllers.cs');
pc = swap(pc, `public sealed class SavedController(TravelService travel,ProductService product):AppController(travel)
{
    public async Task<IActionResult> Index()=>View(await product.Saved());`, `public sealed class SavedController(TravelService travel,ProductService product,DiscoveryService discovery):AppController(travel)
{
    public async Task<IActionResult> Index(){ViewBag.FromDiscovery=await discovery.QuickSaved();return View(await product.Saved());}
    [HttpPost] public async Task<IActionResult> RemoveEntry(CommandInput input){await discovery.QuickRemove(input.Id);return Done("/Saved","Đã bỏ khỏi danh sách đã lưu.");}`);
wr('Controllers/ProductControllers.cs', pc);

let sv = rd('Views/Saved/Index.cshtml');
sv = swap(sv, `@{ViewData["Title"]="Lựa chọn đã lưu";}`, `@{ViewData["Title"]="Đã lưu";var fromDiscovery=(ViewBag.FromDiscovery as List<DiscoveryEntry>)??[];}`);
sv = swap(sv, ` @if(Model.Count==0){`, ` @if(fromDiscovery.Count>0)
 {
  <section class="saved-discovery" aria-labelledby="saved-ds-title"><h2 id="saved-ds-title">Lưu từ Khám phá <small>@fromDiscovery.Count</small></h2>
   <div class="saved-ds-list">@foreach(var e in fromDiscovery){<article class="saved-ds-item"><a href="@DiscoveryText.LinkTo(e)"><span class="saved-ds-kind">@DiscoveryText.Kind(e.Kind)</span><strong>@e.Title</strong><small>@e.Region</small></a><form method="post" action="/Saved/RemoveEntry">@Html.AntiForgeryToken()<input type="hidden" name="Id" value="@e.Id"/><button class="btn btn-outline btn-sm" type="submit">Bỏ lưu</button></form></article>}</div>
  </section>
 }
 @if(Model.Count==0&&fromDiscovery.Count==0){`);
sv = swap(sv, `<p>Mở một địa điểm hoặc dịch vụ và chọn “Lưu lựa chọn”. Bạn chưa cần chọn ngày đi.</p>`, `<p>Bấm “Lưu” trên thẻ ở Khám phá, hoặc “Lưu lựa chọn” trong trang dịch vụ. Bạn chưa cần chọn ngày đi.</p>`);
sv = swap(sv, ` else {`, ` else if(Model.Count>0) {`);
wr('Views/Saved/Index.cshtml', sv);

// ---- Script: dang nhap thi luu vao tai khoan
let js = rd('wwwroot/js/discovery.js');
js = swap(js, ` $$('[data-ds-save]').forEach(b=>b.addEventListener('click',()=>saveChoice(Number(b.dataset.dsSave),b.dataset.title||'Lựa chọn đã lưu')));`,
` // Da dang nhap: luu vao tai khoan (thay o trang "Da luu"); chua dang nhap: luu tren thiet bi nhu truoc.
 async function saveToAccount(id,title,button){try{const r=await fetch('/api/discovery/luu',{method:'POST',redirect:'manual',headers:{'Content-Type':'application/json',RequestVerificationToken:csrf()},body:JSON.stringify({entryId:id})});if(r.ok){const d=await r.json();if(button){button.classList.add('saved');button.textContent='✓ Đã lưu';}toast(d.added?'Đã lưu “'+title+'”.':'Mục này đã có trong danh sách đã lưu.',{label:'Xem đã lưu',url:'/Saved'});return;}if(r.status&&r.status!==401&&r.status!==0){const d=await r.json().catch(()=>null);toast(d?.message||'Chưa lưu được. Thử lại sau.');return;}}catch{}saveChoice(id,title);}
 $$('[data-ds-save]').forEach(b=>b.addEventListener('click',()=>saveToAccount(Number(b.dataset.dsSave),b.dataset.title||'Lựa chọn đã lưu',b)));`);
wr('wwwroot/js/discovery.js', js);

let css = rd('wwwroot/css/dia-gioi.css');
if (!css.includes('/* Da luu tu Kham pha */')) css += `
/* Da luu tu Kham pha */
.saved-discovery{margin:8px 0 28px}
.saved-discovery h2{font-size:1.2rem;display:flex;gap:8px;align-items:center}
.saved-discovery h2 small{font-size:.8rem;background:var(--teal-light,#eef2ff);border-radius:999px;padding:2px 10px;color:var(--muted)}
.saved-ds-list{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:12px}
.saved-ds-item{display:flex;justify-content:space-between;align-items:center;gap:10px;border:1px solid var(--line);border-radius:14px;padding:12px 14px;background:#fff}
.saved-ds-item a{display:grid;gap:2px;text-decoration:none;color:inherit;min-width:0}
.saved-ds-item strong{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.saved-ds-item small{color:var(--muted)}
.saved-ds-kind{font-size:12px;font-weight:700;color:var(--teal)}
[data-ds-save].saved{color:var(--teal);font-weight:700}
`;
wr('wwwroot/css/dia-gioi.css', css);
console.log('ok');
