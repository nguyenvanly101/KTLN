using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class CauHoi
{
 public long Id { get; set; }
 public long DonViId { get; set; }
 public long AuthorId { get; set; }
 public string Title { get; set; }="";
 public string? Body { get; set; }
 public long? AcceptedAnswerId { get; set; }
 public DateTime CreatedAt { get; set; }
 public DateTime? DeletedAt { get; set; }
}
public sealed class TraLoi
{
 public long Id { get; set; }
 public long QuestionId { get; set; }
 public long AuthorId { get; set; }
 public string Body { get; set; }="";
 public DateTime CreatedAt { get; set; }
 public DateTime? DeletedAt { get; set; }
}
public sealed class BinhChonTraLoi
{
 public long AnswerId { get; set; }
 public long AccountId { get; set; }
 public DateTime CreatedAt { get; set; }
}
public sealed class BaoSaiCoSo
{
 public long Id { get; set; }
 public long BranchId { get; set; }
 public long ReporterId { get; set; }
 public string Field { get; set; }="Khac";
 public string Detail { get; set; }="";
 public string Status { get; set; }="Open";
 public string? ResponseNote { get; set; }
 public DateTime CreatedAt { get; set; }
 public DateTime? ResolvedAt { get; set; }
}
public sealed record QuestionRow(CauHoi Question,string Author,int Answers,bool Solved);
public sealed record AnswerRow(TraLoi Answer,string Author,int Votes,bool Voted,bool Accepted,bool CanDelete);
public sealed record QuestionPage(CauHoi Question,DonViHanhChinh Unit,string Author,List<AnswerRow> Answers,bool CanAccept,bool CanDelete);
public sealed record QaListPage(DonViHanhChinh Unit,List<QuestionRow> Items,int Total);
public sealed record WrongInfoRow(BaoSaiCoSo Report,string Reporter);

/// <summary>Hoi dap theo diem den: khach va nha cung cap dat cau hoi, tra loi, binh chon; nguoi hoi chon cau tra loi huu ich.</summary>
public sealed class QaService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,FriendService friends,IThongBaoService notifications)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 public const int PageSize=15;
 void Me(){RoleRequired(Traveler,ProviderRole,AdminRole);}
 static string Cut(string s,int n)=>s.Length<=n?s:s[..(n-1)]+"…";

 async Task<Dictionary<long,string>> Names(IEnumerable<long> ids)=>await friends.Names(ids);

 public async Task<QaListPage> List(long donViId,int page=1)
 {
  var unit=await db.Set<DonViHanhChinh>().AsNoTracking().SingleOrDefaultAsync(u=>u.Id==donViId)??throw new BusinessException("Không tìm thấy điểm đến.",404);
  var q=db.Set<CauHoi>().AsNoTracking().Where(c=>c.DonViId==donViId&&c.DeletedAt==null);
  var total=await q.CountAsync();
  var rows=await q.OrderByDescending(c=>c.Id).Skip((Math.Max(page,1)-1)*PageSize).Take(PageSize).ToListAsync();
  var ids=rows.Select(r=>r.Id).ToList();
  var counts=await db.Set<TraLoi>().AsNoTracking().Where(a=>ids.Contains(a.QuestionId)&&a.DeletedAt==null).GroupBy(a=>a.QuestionId).Select(g=>new{g.Key,N=g.Count()}).ToListAsync();
  var names=await Names(rows.Select(r=>r.AuthorId));
  return new(unit,rows.Select(r=>new QuestionRow(r,names.GetValueOrDefault(r.AuthorId,"Người dùng"),counts.FirstOrDefault(c=>c.Key==r.Id)?.N??0,r.AcceptedAnswerId!=null)).ToList(),total);
 }
 public async Task<List<QuestionRow>> Recent(long donViId,int take=3)=>(await List(donViId)).Items.Take(take).ToList();

 public async Task<QuestionPage> Detail(long id)
 {
  var q=await db.Set<CauHoi>().AsNoTracking().SingleOrDefaultAsync(c=>c.Id==id&&c.DeletedAt==null)??throw new BusinessException("Không tìm thấy câu hỏi.",404);
  var unit=await db.Set<DonViHanhChinh>().AsNoTracking().SingleAsync(u=>u.Id==q.DonViId);
  var answers=await db.Set<TraLoi>().AsNoTracking().Where(a=>a.QuestionId==id&&a.DeletedAt==null).ToListAsync();
  var aids=answers.Select(a=>a.Id).ToList();
  var votes=await db.Set<BinhChonTraLoi>().AsNoTracking().Where(v=>aids.Contains(v.AnswerId)).ToListAsync();
  var names=await Names(answers.Select(a=>a.AuthorId).Append(q.AuthorId));
  var me=UserId;var admin=Role==AdminRole;
  var rows=answers.Select(a=>new AnswerRow(a,names.GetValueOrDefault(a.AuthorId,"Người dùng"),votes.Count(v=>v.AnswerId==a.Id),votes.Any(v=>v.AnswerId==a.Id&&v.AccountId==me),q.AcceptedAnswerId==a.Id,me>0&&(a.AuthorId==me||admin)))
   .OrderByDescending(r=>r.Accepted).ThenByDescending(r=>r.Votes).ThenBy(r=>r.Answer.Id).ToList();
  return new(q,unit,names.GetValueOrDefault(q.AuthorId,"Người dùng"),rows,me>0&&q.AuthorId==me,me>0&&(q.AuthorId==me||admin));
 }

 public async Task<long> Ask(long donViId,string? title,string? body)
 {
  Me();
  title=(title??"").Trim();body=string.IsNullOrWhiteSpace(body)?null:body.Replace("\r\n","\n").Trim();
  Require(title.Length is >=8 and <=200,"Câu hỏi từ 8 đến 200 ký tự.",422);Require(body==null||body.Length<=2000,"Chi tiết tối đa 2000 ký tự.",422);
  Require(await db.Set<DonViHanhChinh>().AnyAsync(u=>u.Id==donViId),"Không tìm thấy điểm đến.",404);
  var since=Clock.BayGioUtc.AddHours(-1);
  Require(await db.Set<CauHoi>().CountAsync(c=>c.AuthorId==UserId&&c.CreatedAt>since)<5,"Bạn đặt quá nhiều câu hỏi trong một giờ.",429);
  long id=0;
  await Execute(async()=>{var c=new CauHoi{DonViId=donViId,AuthorId=UserId,Title=title,Body=body,CreatedAt=Clock.BayGioUtc};db.Add(c);await db.SaveChangesAsync();id=c.Id;});
  return id;
 }
 public async Task Answer(long questionId,string? body)
 {
  Me();
  body=(body??"").Replace("\r\n","\n").Trim();Require(body.Length is >=2 and <=2000,"Câu trả lời từ 2 đến 2000 ký tự.",422);
  var q=await db.Set<CauHoi>().AsNoTracking().SingleOrDefaultAsync(c=>c.Id==questionId&&c.DeletedAt==null)??throw new BusinessException("Không tìm thấy câu hỏi.",404);
  var since=Clock.BayGioUtc.AddHours(-1);
  Require(await db.Set<TraLoi>().CountAsync(a=>a.AuthorId==UserId&&a.CreatedAt>since)<20,"Bạn trả lời quá nhiều trong một giờ.",429);
  await Execute(async()=>
  {
   db.Add(new TraLoi{QuestionId=questionId,AuthorId=UserId,Body=body,CreatedAt=Clock.BayGioUtc});
   if(q.AuthorId!=UserId)notifications.Tao(q.AuthorId,"TraLoiCauHoi",(await friends.Names([UserId]))[UserId]+" đã trả lời câu hỏi của bạn: "+Cut(q.Title,60),"/hoi-dap/cau/"+q.Id);
  });
 }
 public async Task<bool> Vote(long answerId)
 {
  Me();
  var a=await db.Set<TraLoi>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==answerId&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy câu trả lời.",404);
  Require(a.AuthorId!=UserId,"Bạn không bình chọn câu trả lời của chính mình.",422);
  var row=await db.Set<BinhChonTraLoi>().SingleOrDefaultAsync(v=>v.AnswerId==answerId&&v.AccountId==UserId);
  var voted=row==null;
  if(row==null)db.Add(new BinhChonTraLoi{AnswerId=answerId,AccountId=UserId,CreatedAt=Clock.BayGioUtc});else db.Remove(row);
  try{await db.SaveChangesAsync();}catch(DbUpdateException){}
  return voted;
 }
 public async Task Accept(long answerId)
 {
  Me();
  await Execute(async()=>
  {
   var a=await db.Set<TraLoi>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==answerId&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy câu trả lời.",404);
   var q=await db.Set<CauHoi>().SingleOrDefaultAsync(c=>c.Id==a.QuestionId&&c.DeletedAt==null)??throw new BusinessException("Không tìm thấy câu hỏi.",404);
   Require(q.AuthorId==UserId,"Chỉ người đặt câu hỏi mới chọn được câu trả lời hữu ích.",403);
   q.AcceptedAnswerId=q.AcceptedAnswerId==answerId?null:answerId;
   if(q.AcceptedAnswerId!=null&&a.AuthorId!=UserId)notifications.Tao(a.AuthorId,"TraLoiHuuIch","Câu trả lời của bạn được chọn là hữu ích: "+Cut(q.Title,60),"/hoi-dap/cau/"+q.Id);
  });
 }
 public async Task DeleteQuestion(long id)
 {
  Me();
  await Execute(async()=>
  {
   var q=await db.Set<CauHoi>().SingleOrDefaultAsync(c=>c.Id==id&&c.DeletedAt==null)??throw new BusinessException("Không tìm thấy câu hỏi.",404);
   Require(q.AuthorId==UserId||Role==AdminRole,"Bạn không có quyền xóa câu hỏi này.",403);
   q.DeletedAt=Clock.BayGioUtc;
  });
 }
 public async Task DeleteAnswer(long id)
 {
  Me();
  await Execute(async()=>
  {
   var a=await db.Set<TraLoi>().SingleOrDefaultAsync(x=>x.Id==id&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy câu trả lời.",404);
   Require(a.AuthorId==UserId||Role==AdminRole,"Bạn không có quyền xóa câu trả lời này.",403);
   a.DeletedAt=Clock.BayGioUtc;
   var q=await db.Set<CauHoi>().SingleAsync(c=>c.Id==a.QuestionId);if(q.AcceptedAnswerId==id)q.AcceptedAnswerId=null;
  });
 }

 // ------------------------------------------------------------------ bao sai thong tin co so
 public static readonly (string Key,string Label)[] WrongFields=[("Gio","Giờ mở cửa"),("Gia","Giá"),("DiaChi","Địa chỉ / vị trí"),("DaDong","Đã đóng cửa hoặc chuyển đi"),("LienHe","Số điện thoại / liên hệ"),("Khac","Thông tin khác")];
 public async Task Report(long branchId,string? field,string? detail)
 {
  RoleRequired(Traveler);
  Require(WrongFields.Any(f=>f.Key==field),"Chọn thông tin chưa đúng.",422);
  detail=(detail??"").Trim();Require(detail.Length is >=5 and <=500,"Mô tả từ 5 đến 500 ký tự.",422);
  var branch=await db.Set<CoSoDichVu>().AsNoTracking().SingleOrDefaultAsync(b=>b.Id==branchId&&b.Status=="Approved"&&b.PublishedVersion!=null)??throw new BusinessException("Cơ sở không còn được công bố.",404);
  Require(!await db.Set<BaoSaiCoSo>().AnyAsync(r=>r.BranchId==branchId&&r.ReporterId==UserId&&r.Field==field&&r.Status=="Open"),"Bạn đã báo mục này, nhà cung cấp đang xem xét.",409);
  var since=Clock.BayGioUtc.AddHours(-1);
  Require(await db.Set<BaoSaiCoSo>().CountAsync(r=>r.ReporterId==UserId&&r.CreatedAt>since)<10,"Bạn báo quá nhiều trong một giờ.",429);
  await Execute(async()=>
  {
   db.Add(new BaoSaiCoSo{BranchId=branchId,ReporterId=UserId,Field=field!,Detail=detail,CreatedAt=Clock.BayGioUtc});
   var owner=await db.NhaCungCapDichVu.AsNoTracking().SingleAsync(p=>p.MaNhaCungCap==branch.ProviderId);
   notifications.Tao(owner.MaTaiKhoan,"BaoSaiCoSo","Có khách báo thông tin chưa đúng ở "+branch.Name+" ("+WrongFields.First(f=>f.Key==field).Label+").","/nha-cung-cap/co-so/"+branchId+"/bao-sai");
  });
 }
 async Task<CoSoDichVu> OwnBranch(long branchId)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  return await db.Set<CoSoDichVu>().AsNoTracking().SingleOrDefaultAsync(b=>b.Id==branchId&&b.ProviderId==p.MaNhaCungCap)??throw new BusinessException("Không tìm thấy cơ sở của bạn.",404);
 }
 public async Task<(CoSoDichVu Branch,List<WrongInfoRow> Items)> WrongInfoPage(long branchId)
 {
  var b=await OwnBranch(branchId);
  var rows=await db.Set<BaoSaiCoSo>().AsNoTracking().Where(r=>r.BranchId==branchId).OrderBy(r=>r.Status=="Open"?0:1).ThenByDescending(r=>r.Id).Take(100).ToListAsync();
  var names=await friends.Names(rows.Select(r=>r.ReporterId));
  return (b,rows.Select(r=>new WrongInfoRow(r,names.GetValueOrDefault(r.ReporterId,"Khách"))).ToList());
 }
 public async Task ResolveWrong(long branchId,long reportId,bool fixedIt,string? note)
 {
  await OwnBranch(branchId);
  note=string.IsNullOrWhiteSpace(note)?null:note.Trim();Require(note==null||note.Length<=300,"Ghi chú tối đa 300 ký tự.",422);
  await Execute(async()=>
  {
   var r=await db.Set<BaoSaiCoSo>().SingleOrDefaultAsync(x=>x.Id==reportId&&x.BranchId==branchId&&x.Status=="Open")??throw new BusinessException("Báo cáo đã được xử lý.",409);
   r.Status=fixedIt?"Fixed":"Rejected";r.ResponseNote=note;r.ResolvedAt=Clock.BayGioUtc;
   notifications.Tao(r.ReporterId,"PhanHoiBaoSai",fixedIt?"Cảm ơn bạn, cơ sở đã chỉnh lại thông tin bạn báo.":"Nhà cung cấp phản hồi: thông tin bạn báo vẫn đúng."+(note==null?"":" "+note),"/co-so/"+branchId);
  });
 }
 public async Task<int> OpenWrongCount(long branchId)=>await db.Set<BaoSaiCoSo>().CountAsync(r=>r.BranchId==branchId&&r.Status=="Open");
}
