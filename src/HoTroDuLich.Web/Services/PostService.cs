using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class PostInput
{
 [System.ComponentModel.DataAnnotations.MaxLength(4000)] public string? Content { get; set; }
 public string Visibility { get; set; }="Friends";
 public List<IFormFile> Photos { get; set; }=[];
 public bool RemovePhotos { get; set; }
 public string Kind { get; set; }="Share";
 public long? BranchId { get; set; }
 [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? Destination { get; set; }
 public DateOnly? TripStart { get; set; }
 public DateOnly? TripEnd { get; set; }
 public long? ServiceId { get; set; }
}
public sealed class CommentInput
{
 [System.ComponentModel.DataAnnotations.MaxLength(1500)] public string? Content { get; set; }
 public long? ParentId { get; set; }
}
public sealed record PostDto(long Id,long AuthorId,string Author,string? Avatar,string Content,string Visibility,int Photos,DateTime CreatedAt,DateTime? UpdatedAt,int Likes,int Comments,string? MyReaction,bool Mine,string Kind="Share",long? BranchId=null,string? BranchName=null,string? Destination=null,DateOnly? TripStart=null,DateOnly? TripEnd=null,long? ServiceId=null,string? ServiceName=null,Dictionary<string,int>? Reactions=null,int Shares=0,bool Saved=false,SharedPostDto? Shared=null,bool Following=false);
public sealed record SharedPostDto(long Id,long AuthorId,string Author,string Content,string Kind,int Photos,DateTime CreatedAt,long? BranchId,bool Gone);
public sealed record ReactorDto(long Id,string Name,string Emoji,string? Avatar);
public sealed record CommentDto(long Id,long PostId,long? ParentId,long AuthorId,string Author,string? Avatar,string Content,DateTime CreatedAt,DateTime? UpdatedAt,int Likes,bool Liked,bool Mine,bool CanDelete,bool Deleted,string? Link=null);
public sealed record PostReportDto(long Id,string Reporter,string Reason,string? Note,DateTime At,long PostId,long? CommentId,string Excerpt,string Author,bool Hidden);

/// <summary>Bai viet, binh luan va cam xuc kieu Facebook cho khach du lich. Quyen xem: Cong khai / Ban be / Chi minh toi.</summary>
public sealed class PostService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,FriendService friends,IThongBaoService notifications)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 public const int MaxPhotos=4,PageSize=10;
 static readonly string[] Visibilities=["Public","Friends","Private"];
 static readonly string[] Emojis=["like","love","haha","wow","sad","angry"];
 static DateTime Utc(DateTime d)=>DateTime.SpecifyKind(d,DateTimeKind.Utc);
 static DateTime? Utc(DateTime? d)=>d==null?null:Utc(d.Value);
 static string Cut(string s,int n)=>s.Length<=n?s:s[..(n-1)]+"…";
 void Me(){RoleRequired(Traveler,ProviderRole);}

 // ------------------------------------------------------------------ quyen xem
 async Task<IQueryable<BaiViet>> Visible()
 {
  var friendIds=await friends.FriendIds(UserId);var me=UserId;
  // Nguoi khac chi thay bai cua tai khoan dang hoat dong; bai cua co so chi hien khi co so con cong khai.
  var publicBranches=Resolve<BranchService>().PublicBranches();
  return db.Set<BaiViet>().Where(p=>p.DeletedAt==null&&(p.AuthorId==me||((p.Visibility=="Public"||(p.Visibility=="Friends"&&friendIds.Contains(p.AuthorId)))
   &&db.TaiKhoan.Any(a=>a.MaTaiKhoan==p.AuthorId&&a.TrangThai==TrangThaiTaiKhoan.HoatDong)
   &&(p.BranchId==null||publicBranches.Any(b=>b.Id==p.BranchId)))));
 }
 async Task<BaiViet> ViewablePost(long id,bool track=false)
 {
  var q=await Visible();
  var p=await (track?q:q.AsNoTracking()).SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Không tìm thấy bài viết.",404);
  return p;
 }
 async Task<bool> AccountActive(long id)=>await db.TaiKhoan.AsNoTracking().AnyAsync(a=>a.MaTaiKhoan==id&&a.VaiTro==VaiTro.KHACH_DU_LICH&&a.TrangThai==TrangThaiTaiKhoan.HoatDong);

 // ------------------------------------------------------------------ danh sach
 async Task<List<PostDto>> Project(List<BaiViet> posts)
 {
  if(posts.Count==0)return [];
  var ids=posts.Select(p=>p.Id).ToList();
  var names=await friends.Names(posts.Select(p=>p.AuthorId));
  var likes=await db.Set<CamXuc>().AsNoTracking().Where(c=>c.TargetType=="Post"&&ids.Contains(c.TargetId)).GroupBy(c=>c.TargetId).Select(g=>new{Id=g.Key,N=g.Count()}).ToListAsync();
  var mine=await db.Set<CamXuc>().AsNoTracking().Where(c=>c.TargetType=="Post"&&ids.Contains(c.TargetId)&&c.AccountId==UserId).Select(c=>new{c.TargetId,c.Emoji}).ToListAsync();
  var comments=await db.Set<BinhLuan>().AsNoTracking().Where(c=>ids.Contains(c.PostId)&&c.DeletedAt==null).GroupBy(c=>c.PostId).Select(g=>new{Id=g.Key,N=g.Count()}).ToListAsync();
  var authorIds=posts.Select(p=>p.AuthorId).Distinct().ToList();
  var withAvatar=await db.Set<HoSoNguoiDung>().AsNoTracking().Where(h=>authorIds.Contains(h.MaTaiKhoan)&&h.AnhDaiDien!=null).Select(h=>h.MaTaiKhoan).ToListAsync();
  var canSee=new Dictionary<long,bool>();
  foreach(var a in withAvatar)canSee[a]=await friends.CanSeeDetails(a);
  var serviceIds=posts.Where(p=>p.ServiceId!=null).Select(p=>p.ServiceId!.Value).Distinct().ToList();
  var serviceNames=serviceIds.Count==0?new Dictionary<long,string>():await db.DichVuDuLich.AsNoTracking().Where(d=>serviceIds.Contains(d.MaDichVu)).ToDictionaryAsync(d=>d.MaDichVu,d=>d.TenDichVu);
  var reactionRows=await db.Set<CamXuc>().AsNoTracking().Where(c=>c.TargetType=="Post"&&ids.Contains(c.TargetId)).GroupBy(c=>new{c.TargetId,c.Emoji}).Select(g=>new{g.Key.TargetId,g.Key.Emoji,N=g.Count()}).ToListAsync();
  var shareRows=await db.Set<BaiViet>().AsNoTracking().Where(b=>b.SharedPostId!=null&&ids.Contains(b.SharedPostId.Value)&&b.DeletedAt==null).GroupBy(b=>b.SharedPostId!.Value).Select(g=>new{Id=g.Key,N=g.Count()}).ToListAsync();
  var savedSet=(await db.Set<BaiVietDaLuu>().AsNoTracking().Where(s=>s.AccountId==UserId&&ids.Contains(s.PostId)).Select(s=>s.PostId).ToListAsync()).ToHashSet();
  var originalIds=posts.Where(p=>p.SharedPostId!=null).Select(p=>p.SharedPostId!.Value).Distinct().ToList();
  var originals=originalIds.Count==0?new Dictionary<long,BaiViet>():await db.Set<BaiViet>().AsNoTracking().Where(b=>originalIds.Contains(b.Id)).ToDictionaryAsync(b=>b.Id);
  var originalNames=originals.Count==0?new Dictionary<long,string>():await friends.Names(originals.Values.Select(b=>b.AuthorId));
  var followed=(await db.Set<TheoDoiCoSo>().AsNoTracking().Where(f=>f.AccountId==UserId).Select(f=>f.BranchId).ToListAsync()).ToHashSet();
  var branchIds=posts.Where(p=>p.BranchId!=null).Select(p=>p.BranchId!.Value).Distinct().ToList();
  var branchNames=branchIds.Count==0?new Dictionary<long,string>():await db.Set<CoSoDichVu>().AsNoTracking().Where(b=>branchIds.Contains(b.Id)).ToDictionaryAsync(b=>b.Id,b=>b.Name);
  return posts.Select(p=>new PostDto(p.Id,p.AuthorId,p.Kind=="Branch"&&p.BranchId!=null?branchNames.GetValueOrDefault(p.BranchId.Value,"Cơ sở"):names.GetValueOrDefault(p.AuthorId,"Người dùng"),canSee.GetValueOrDefault(p.AuthorId)?"/mxh/anh/"+p.AuthorId:null,p.Content,p.Visibility,Ui.ServicePhotos(p.PhotosJson).Length,Utc(p.CreatedAt),Utc(p.UpdatedAt),likes.FirstOrDefault(x=>x.Id==p.Id)?.N??0,comments.FirstOrDefault(x=>x.Id==p.Id)?.N??0,mine.FirstOrDefault(x=>x.TargetId==p.Id)?.Emoji,p.AuthorId==UserId,p.Kind,p.BranchId,p.BranchId==null?null:branchNames.GetValueOrDefault(p.BranchId.Value),p.Destination,p.TripStart,p.TripEnd,p.ServiceId,p.ServiceId==null?null:serviceNames.GetValueOrDefault(p.ServiceId.Value),
   reactionRows.Where(r=>r.TargetId==p.Id).ToDictionary(r=>r.Emoji,r=>r.N),shareRows.FirstOrDefault(r=>r.Id==p.Id)?.N??0,savedSet.Contains(p.Id),
   p.SharedPostId==null?null:(originals.TryGetValue(p.SharedPostId.Value,out var o)&&o.DeletedAt==null?new SharedPostDto(o.Id,o.AuthorId,o.Kind=="Branch"&&o.BranchId!=null?branchNames.GetValueOrDefault(o.BranchId.Value,"Cơ sở"):originalNames.GetValueOrDefault(o.AuthorId,"Người dùng"),Cut(o.Content,600),o.Kind,Ui.ServicePhotos(o.PhotosJson).Length,Utc(o.CreatedAt),o.BranchId,false):new SharedPostDto(p.SharedPostId.Value,0,"","","Share",0,Utc(p.CreatedAt),null,true)),
   p.BranchId!=null&&followed.Contains(p.BranchId.Value))).ToList();
 }
 /// <summary>Bang tin: "friends" = minh + ban be; "public" = bai cong khai cua moi nguoi. Phan trang bang moc <paramref name="before"/>.</summary>
 /// <summary>Truy van bai viet theo pham vi: home (ban be + co so dang theo doi + cua toi), friends, public, companion, branch, saved.</summary>
 async Task<(IQueryable<BaiViet> Query,bool ByTrip)> ScopeQuery(string scope,long? branchId,string? search)
 {
  var q=await Visible();
  switch(scope)
  {
   case "companion":
   {
    q=q.Where(p=>p.Kind=="Companion"&&p.TripEnd>=TravelService.Today);
    var term=(search??"").Trim();if(term.Length>100)term=term[..100];
    if(term.Length>0)q=q.Where(p=>p.Destination!.Contains(term)||p.Content.Contains(term));
    return (q,true);
   }
   case "branch":
    await OwnBranch(branchId??0);
    return (q.Where(p=>p.Kind=="Branch"&&p.BranchId==branchId),false);
   case "saved":
   {
    var me=UserId;
    return (q.Where(p=>db.Set<BaiVietDaLuu>().Any(s=>s.AccountId==me&&s.PostId==p.Id)),false);
   }
   case "friends":
   {
    var ids=await friends.FriendIds(UserId);ids.Add(UserId);
    return (q.Where(p=>ids.Contains(p.AuthorId)),false);
   }
   case "all":
    return (q,false); // moi bai minh duoc xem: Visible() da gioi han theo quyen
   case "home":
   {
    var ids=await friends.FriendIds(UserId);ids.Add(UserId);
    var followed=await db.Set<TheoDoiCoSo>().AsNoTracking().Where(f=>f.AccountId==UserId).Select(f=>f.BranchId).ToListAsync();
    return (q.Where(p=>ids.Contains(p.AuthorId)||(p.Kind=="Branch"&&p.BranchId!=null&&followed.Contains(p.BranchId.Value))),false);
   }
   default:
    return (q.Where(p=>p.Visibility=="Public"),false);
  }
 }
 public async Task<List<PostDto>> Feed(string scope,long? before,long? branchId=null,string? search=null)
 {
  Me();
  var (q,byTrip)=await ScopeQuery(scope,branchId,search);
  if(before!=null)q=q.Where(p=>p.Id<before);
  var ordered=byTrip?q.AsNoTracking().OrderBy(p=>p.TripStart).ThenByDescending(p=>p.Id):q.AsNoTracking().OrderByDescending(p=>p.Id);
  return await Project(await ordered.Take(PageSize).ToListAsync());
 }
 /// <summary>So bai moi hon moc <paramref name="since"/> trong cung pham vi (hien thanh "Co bai moi").</summary>
 public async Task<int> NewCount(string scope,long since,long? branchId=null)
 {
  Me();
  var (q,_)=await ScopeQuery(scope,branchId,null);
  var me=UserId;
  return await q.AsNoTracking().Where(p=>p.Id>since&&p.AuthorId!=me).CountAsync();
 }
 public async Task<List<PostDto>> ByAuthor(long author,long? before)
 {
  Me();
  Require(await AccountActive(author),"Không tìm thấy người dùng.",404);
  var q=(await Visible()).Where(p=>p.AuthorId==author);
  if(before!=null)q=q.Where(p=>p.Id<before);
  return await Project(await q.AsNoTracking().OrderByDescending(p=>p.Id).Take(PageSize).ToListAsync());
 }
 /// <summary>Anh trong cac bai viet cua mot nguoi ma toi xem duoc (moi nhat truoc), toi da 60 anh.</summary>
 public async Task<List<string>> PhotosOf(long author)
 {
  Me();Require(await AccountActive(author),"Không tìm thấy người dùng.",404);
  var rows=await (await Visible()).Where(p=>p.AuthorId==author&&p.PhotosJson!=null).AsNoTracking().OrderByDescending(p=>p.Id).Take(30).Select(p=>new{p.Id,p.PhotosJson}).ToListAsync();
  var urls=new List<string>();
  foreach(var r in rows){var n=Ui.ServicePhotos(r.PhotosJson).Length;for(var i=0;i<n&&urls.Count<60;i++)urls.Add("/mxh/bai-viet/"+r.Id+"/anh/"+i);}
  return urls;
 }
 public async Task<PostDto> Get(long id){Me();return (await Project([await ViewablePost(id)]))[0];}

 // ------------------------------------------------------------------ tao / sua / xoa
 public async Task<PostDto> Create(PostInput i)
 {
  Me();
  if(Role==ProviderRole)
  {
   Require(i.Kind=="Branch"&&i.BranchId!=null,"Nhà cung cấp đăng tin giới thiệu cho một cơ sở của mình.",400);
   return (await Project([await CreateBranchPost(i.BranchId!.Value,i)]))[0];
  }
  Require(i.Kind is "Share" or "Companion","Loại bài viết không hợp lệ.",400);
  string? destination=null;
  if(i.Kind=="Companion")
  {
   destination=(i.Destination??"").Trim();
   Require(destination.Length is >=2 and <=200,"Nhập điểm đến (2–200 ký tự) để tìm bạn đồng hành.",422);
   Require(i.TripStart!=null&&i.TripEnd!=null&&i.TripStart>=TravelService.Today&&i.TripEnd>=i.TripStart&&i.TripEnd<=TravelService.Today.AddYears(2),"Ngày đi phải từ hôm nay và ngày về không trước ngày đi.",422);
  }
  var content=(i.Content??"").Replace("\r\n","\n").Trim();
  Require(content.Length<=4000,"Bài viết tối đa 4000 ký tự.",400);
  Require(Visibilities.Contains(i.Visibility),"Chọn ai được xem bài viết.",400);
  Require(i.Photos.Count<=MaxPhotos,"Tối đa "+MaxPhotos+" ảnh cho mỗi bài viết.",400);
  Require(content.Length>0||i.Photos.Count>0||destination!=null,"Hãy viết nội dung hoặc chọn ảnh.",422);
  foreach(var f in i.Photos)Require(Path.GetExtension(f.FileName).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png"&&f.Length is >0 and <=5*1024*1024,"Chỉ đăng ảnh JPG hoặc PNG, mỗi ảnh tối đa 5 MB.",422);
  if(i.ServiceId!=null)Require(await PublicServices().AnyAsync(d=>d.MaDichVu==i.ServiceId),"Dịch vụ gắn thẻ không còn được công bố.",422);
  var since=Clock.BayGioUtc.AddHours(-1);
  Require(await db.Set<BaiViet>().CountAsync(p=>p.AuthorId==UserId&&p.CreatedAt>since)<10,"Bạn đăng quá nhiều bài trong một giờ. Hãy thử lại sau.",429);
  BaiViet post=null!;
  await Execute(async()=>
  {
   string? photos=null;
   if(i.Photos.Count>0)photos=System.Text.Json.JsonSerializer.Serialize((await StoreFiles(i.Photos)).Select(x=>x.Path));
   post=new BaiViet{AuthorId=UserId,Content=content,Visibility=i.Visibility,PhotosJson=photos,CreatedAt=Clock.BayGioUtc,Kind=i.Kind,ServiceId=i.ServiceId,Destination=destination,TripStart=destination==null?null:i.TripStart,TripEnd=destination==null?null:i.TripEnd};
   db.Add(post);await db.SaveChangesAsync();
  });
  return (await Project([post]))[0];
 }

 // ------------------------------------------------------------------ tin gioi thieu co so (nha cung cap)
 async Task<CoSoDichVu> OwnBranch(long branchId)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  return await db.Set<CoSoDichVu>().AsNoTracking().SingleOrDefaultAsync(b=>b.Id==branchId&&b.ProviderId==p.MaNhaCungCap)??throw new BusinessException("Không tìm thấy cơ sở của bạn.",404);
 }
 public async Task<(CoSoDichVu Branch,List<BaiViet> Posts)> BranchPostsPage(long branchId)
 {
  var b=await OwnBranch(branchId);
  return (b,await db.Set<BaiViet>().AsNoTracking().Where(x=>x.Kind=="Branch"&&x.BranchId==branchId&&x.DeletedAt==null).OrderByDescending(x=>x.Id).Take(50).ToListAsync());
 }
 public async Task<BaiViet> CreateBranchPost(long branchId,PostInput i)
 {
  var b=await OwnBranch(branchId);
  Require(b.Status=="Approved"&&b.PublishedVersion!=null,"Chỉ đăng tin cho cơ sở đã được duyệt và công bố.",422);
  var content=(i.Content??"").Replace("\r\n","\n").Trim();
  Require(content.Length is >=1 and <=4000,"Nội dung tin từ 1 đến 4000 ký tự.",422);
  Require(i.Photos.Count<=MaxPhotos,"Tối đa "+MaxPhotos+" ảnh cho mỗi tin.",400);
  foreach(var f in i.Photos)Require(Path.GetExtension(f.FileName).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png"&&f.Length is >0 and <=5*1024*1024,"Chỉ đăng ảnh JPG hoặc PNG, mỗi ảnh tối đa 5 MB.",422);
  var since=Clock.BayGioUtc.AddHours(-1);
  Require(await db.Set<BaiViet>().CountAsync(p=>p.AuthorId==UserId&&p.CreatedAt>since)<10,"Bạn đăng quá nhiều tin trong một giờ. Hãy thử lại sau.",429);
  BaiViet created=null!;
  await Execute(async()=>
  {
   string? photos=null;
   if(i.Photos.Count>0)photos=System.Text.Json.JsonSerializer.Serialize((await StoreFiles(i.Photos)).Select(x=>x.Path));
   created=new BaiViet{AuthorId=UserId,Content=content,Visibility="Public",PhotosJson=photos,CreatedAt=Clock.BayGioUtc,Kind="Branch",BranchId=branchId};
   db.Add(created);
   await db.SaveChangesAsync();
  });
  return created;
 }
 public async Task DeleteBranchPost(long postId)
 {
  RoleRequired(ProviderRole);
  var p=await db.Set<BaiViet>().SingleOrDefaultAsync(x=>x.Id==postId&&x.Kind=="Branch"&&x.AuthorId==UserId&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy tin.",404);
  p.DeletedAt=Clock.BayGioUtc;await db.SaveChangesAsync();
 }
 /// <summary>Tin gioi thieu moi nhat cua co so, hien cho moi nguoi o trang co so.</summary>
 public async Task<List<BaiViet>> PublicBranchPosts(long branchId,int take=3)=>await db.Set<BaiViet>().AsNoTracking().Where(x=>x.Kind=="Branch"&&x.BranchId==branchId&&x.DeletedAt==null).OrderByDescending(x=>x.Id).Take(take).ToListAsync();

 public async Task<PostDto> Edit(long id,PostInput i)
 {
  Me();var p=await ViewablePost(id,true);
  Require(p.AuthorId==UserId,"Chỉ tác giả mới sửa được bài viết.",403);
  var content=(i.Content??"").Replace("\r\n","\n").Trim();
  Require(content.Length<=4000,"Bài viết tối đa 4000 ký tự.",400);
  Require(Visibilities.Contains(i.Visibility),"Chọn ai được xem bài viết.",400);
  var photos=i.RemovePhotos?null:p.PhotosJson;
  Require(content.Length>0||photos!=null,"Bài viết cần có nội dung hoặc ảnh.",422);
  p.Content=content;p.Visibility=i.Visibility;p.PhotosJson=photos;p.UpdatedAt=Clock.BayGioUtc;
  await db.SaveChangesAsync();
  return (await Project([p]))[0];
 }
 public async Task Delete(long id)
 {
  Me();var p=await ViewablePost(id,true);
  Require(p.AuthorId==UserId,"Chỉ tác giả mới xóa được bài viết.",403);
  p.DeletedAt=Clock.BayGioUtc;await db.SaveChangesAsync();
 }
 public async Task<string> PhotoFile(long id,int index)
 {
  Me();var p=await ViewablePost(id);
  var photos=Ui.ServicePhotos(p.PhotosJson);Require(index>=0&&index<photos.Length,"Không tìm thấy ảnh.",404);
  var path=Resolve<ILuuTepService>().DuongDanAnToan(photos[index]);
  Require(System.IO.File.Exists(path)&&Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png","Không tìm thấy ảnh.",404);
  return path;
 }

 // ------------------------------------------------------------------ binh luan
 public async Task<List<CommentDto>> Comments(long postId)
 {
  Me();var post=await ViewablePost(postId);
  var rows=await db.Set<BinhLuan>().AsNoTracking().Where(c=>c.PostId==postId).OrderBy(c=>c.Id).Take(300).ToListAsync();
  var parentsWithReplies=rows.Where(r=>r.ParentId!=null&&r.DeletedAt==null).Select(r=>r.ParentId!.Value).ToHashSet();
  rows=rows.Where(r=>r.DeletedAt==null||parentsWithReplies.Contains(r.Id)).ToList();
  var ids=rows.Select(r=>r.Id).ToList();
  var names=await friends.Names(rows.Select(r=>r.AuthorId));
  var likes=await db.Set<CamXuc>().AsNoTracking().Where(c=>c.TargetType=="Comment"&&ids.Contains(c.TargetId)).Select(c=>new{c.TargetId,c.AccountId}).ToListAsync();
  var authorIdsC=rows.Select(r=>r.AuthorId).Distinct().ToList();
  var providerBranch=await (from p in db.NhaCungCapDichVu.AsNoTracking() where authorIdsC.Contains(p.MaTaiKhoan)
   select new{p.MaTaiKhoan,Branch=db.Set<CoSoDichVu>().Where(b=>b.ProviderId==p.MaNhaCungCap&&b.Status=="Approved"&&b.PublishedVersion!=null).OrderBy(b=>b.Id).Select(b=>(long?)b.Id).FirstOrDefault()}).ToListAsync();
  var providerIds=providerBranch.ToDictionary(x=>x.MaTaiKhoan,x=>x.Branch);
  var avatars=new Dictionary<long,bool>();
  foreach(var a in rows.Select(r=>r.AuthorId).Distinct())avatars[a]=await db.Set<HoSoNguoiDung>().AsNoTracking().AnyAsync(h=>h.MaTaiKhoan==a&&h.AnhDaiDien!=null)&&await friends.CanSeeDetails(a);
  return rows.Select(r=>new CommentDto(r.Id,r.PostId,r.ParentId,r.AuthorId,names.GetValueOrDefault(r.AuthorId,"Người dùng"),avatars.GetValueOrDefault(r.AuthorId)?"/mxh/anh/"+r.AuthorId:null,r.DeletedAt==null?r.Content:"",Utc(r.CreatedAt),Utc(r.UpdatedAt),likes.Count(l=>l.TargetId==r.Id),likes.Any(l=>l.TargetId==r.Id&&l.AccountId==UserId),r.AuthorId==UserId,r.DeletedAt==null&&(r.AuthorId==UserId||post.AuthorId==UserId),r.DeletedAt!=null,providerIds.TryGetValue(r.AuthorId,out var pb)?(pb==null?"":"/co-so/"+pb):null)).ToList();
 }
 public async Task<CommentDto> AddComment(long postId,CommentInput i)
 {
  Me();var post=await ViewablePost(postId);
  var text=(i.Content??"").Replace("\r\n","\n").Trim();
  Require(text.Length>0,"Hãy nhập nội dung bình luận.",422);Require(text.Length<=1500,"Bình luận tối đa 1500 ký tự.",400);
  BinhLuan? parent=null;
  if(i.ParentId is long pid){parent=await db.Set<BinhLuan>().AsNoTracking().SingleOrDefaultAsync(c=>c.Id==pid&&c.PostId==postId&&c.DeletedAt==null)??throw new BusinessException("Không tìm thấy bình luận để trả lời.",404);Require(parent.ParentId==null,"Chỉ trả lời bình luận gốc.",422);}
  var since=Clock.BayGioUtc.AddMinutes(-1);
  Require(await db.Set<BinhLuan>().CountAsync(c=>c.AuthorId==UserId&&c.CreatedAt>since)<20,"Bạn bình luận quá nhanh. Hãy thử lại sau ít phút.",429);
  var row=new BinhLuan{PostId=postId,AuthorId=UserId,ParentId=parent?.Id,Content=text,CreatedAt=Clock.BayGioUtc};
  db.Add(row);await db.SaveChangesAsync();
  var who=(await friends.Names([UserId]))[UserId];
  if(post.AuthorId!=UserId)notifications.Tao(post.AuthorId,"BinhLuan",who+" đã bình luận bài viết của bạn: "+Cut(text,60),"/bai-viet/"+post.Id);
  if(parent!=null&&parent.AuthorId!=UserId&&parent.AuthorId!=post.AuthorId)notifications.Tao(parent.AuthorId,"BinhLuan",who+" đã trả lời bình luận của bạn: "+Cut(text,60),"/bai-viet/"+post.Id);
  await db.SaveChangesAsync();
  return (await Comments(postId)).First(c=>c.Id==row.Id);
 }
 async Task<BinhLuan> OwnComment(long id,bool allowPostAuthor)
 {
  var c=await db.Set<BinhLuan>().SingleOrDefaultAsync(x=>x.Id==id&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy bình luận.",404);
  var post=await ViewablePost(c.PostId);
  Require(c.AuthorId==UserId||(allowPostAuthor&&post.AuthorId==UserId),"Bạn không có quyền với bình luận này.",403);
  return c;
 }
 public async Task EditComment(long id,CommentInput i)
 {
  Me();var c=await OwnComment(id,false);
  var text=(i.Content??"").Trim();Require(text.Length>0&&text.Length<=1500,"Bình luận 1–1500 ký tự.",422);
  c.Content=text;c.UpdatedAt=Clock.BayGioUtc;await db.SaveChangesAsync();
 }
 public async Task DeleteComment(long id)
 {
  Me();var c=await OwnComment(id,true);
  c.DeletedAt=Clock.BayGioUtc;await db.SaveChangesAsync();
 }

 // ------------------------------------------------------------------ bao cao bai viet / binh luan
 static readonly string[] ReportReasons=["Spam","QuayRoi","LuaDao","NoiDungXau","Khac"];
 public async Task Report(long postId,long? commentId,string reason,string? note)
 {
  Me();var post=await ViewablePost(postId);
  Require(ReportReasons.Contains(reason),"Chọn lý do báo cáo.",400);
  note=string.IsNullOrWhiteSpace(note)?null:note.Trim();Require(note==null||note.Length<=500,"Ghi chú tối đa 500 ký tự.",400);
  if(commentId!=null)Require(await db.Set<BinhLuan>().AnyAsync(c=>c.Id==commentId&&c.PostId==postId&&c.DeletedAt==null),"Không tìm thấy bình luận.",404);
  Require(post.AuthorId!=UserId,"Bạn không cần báo cáo nội dung của chính mình.",400);
  Require(!await db.Set<BaoCaoBaiViet>().AnyAsync(r=>r.ReporterId==UserId&&r.PostId==postId&&r.CommentId==commentId&&r.Status=="Open"),"Bạn đã báo cáo nội dung này, quản trị viên đang xem xét.",409);
  var since=Clock.BayGioUtc.AddHours(-1);
  Require(await db.Set<BaoCaoBaiViet>().CountAsync(r=>r.ReporterId==UserId&&r.CreatedAt>since)<10,"Bạn báo cáo quá nhiều trong một giờ.",429);
  db.Add(new BaoCaoBaiViet{ReporterId=UserId,PostId=postId,CommentId=commentId,Reason=reason,Note=note,CreatedAt=Clock.BayGioUtc});
  await db.SaveChangesAsync();
 }
 public async Task<List<PostReportDto>> ReportQueue(string status)
 {
  RoleRequired(AdminRole);status=status=="Done"?"Done":"Open";
  var rows=await (from r in db.Set<BaoCaoBaiViet>().AsNoTracking() join p in db.Set<BaiViet>().AsNoTracking() on r.PostId equals p.Id
   where status=="Open"?r.Status=="Open":r.Status!="Open" orderby r.Id descending
   select new{r,p}).Take(100).ToListAsync();
  var names=await friends.Names(rows.SelectMany(x=>new[]{x.r.ReporterId,x.p.AuthorId}));
  var commentIds=rows.Where(x=>x.r.CommentId!=null).Select(x=>x.r.CommentId!.Value).ToList();
  var comments=await db.Set<BinhLuan>().AsNoTracking().Where(c=>commentIds.Contains(c.Id)).ToDictionaryAsync(c=>c.Id,c=>c.Content);
  return rows.Select(x=>new PostReportDto(x.r.Id,names.GetValueOrDefault(x.r.ReporterId,"?"),x.r.Reason,x.r.Note,Utc(x.r.CreatedAt),x.r.PostId,x.r.CommentId,
   x.r.CommentId!=null?"[Bình luận] "+Cut(comments.GetValueOrDefault(x.r.CommentId.Value,""),200):Cut(x.p.Content,200),names.GetValueOrDefault(x.p.AuthorId,"?"),x.p.DeletedAt!=null||x.r.Status=="Hidden")).ToList();
 }
 /// <summary>Quan tri vien xu ly: an noi dung (bai viet hoac binh luan) hoac bo qua bao cao.</summary>
 public async Task HandleReport(long id,bool hide)
 {
  RoleRequired(AdminRole);
  await Execute(async()=>
  {
   var r=await db.Set<BaoCaoBaiViet>().SingleOrDefaultAsync(x=>x.Id==id&&x.Status=="Open")??throw new BusinessException("Báo cáo đã được xử lý.",409);
   if(hide)
   {
    if(r.CommentId!=null){var c=await db.Set<BinhLuan>().SingleOrDefaultAsync(x=>x.Id==r.CommentId);if(c!=null&&c.DeletedAt==null)c.DeletedAt=Clock.BayGioUtc;}
    else{var p=await db.Set<BaiViet>().SingleOrDefaultAsync(x=>x.Id==r.PostId);if(p!=null&&p.DeletedAt==null)p.DeletedAt=Clock.BayGioUtc;}
   }
   r.Status=hide?"Hidden":"Done";r.HandledAt=Clock.BayGioUtc;
   Resolve<INhatKyService>().Ghi(hide?"AnNoiDungBaoCao":"BoQuaBaoCao","BaoCaoBaiViet",r.Id,"Open",r.Status,null);
  });
 }

 // ------------------------------------------------------------------ cam xuc
 /// <summary>Chon / doi / bo cam xuc: chon lai cung cam xuc thi bo. Tra ve so luot va cam xuc cua toi.</summary>
 public async Task<(int Count,string? Mine,Dictionary<string,int> Breakdown)> React(string type,long target,string? emoji)
 {
  Me();
  Require(type is "Post" or "Comment","Loại không hợp lệ.",400);
  emoji=string.IsNullOrWhiteSpace(emoji)?"like":emoji;Require(Emojis.Contains(emoji),"Cảm xúc không hợp lệ.",400);
  long? postAuthor=null;long? commentAuthor=null;long postId;
  if(type=="Post"){var p=await ViewablePost(target);postAuthor=p.AuthorId;postId=p.Id;}
  else{var c=await db.Set<BinhLuan>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==target&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy bình luận.",404);var p=await ViewablePost(c.PostId);commentAuthor=c.AuthorId;postId=p.Id;}
  var row=await db.Set<CamXuc>().SingleOrDefaultAsync(x=>x.TargetType==type&&x.TargetId==target&&x.AccountId==UserId);
  string? mine=emoji;var isNew=false;
  if(row==null){db.Add(new CamXuc{TargetType=type,TargetId=target,AccountId=UserId,Emoji=emoji,CreatedAt=Clock.BayGioUtc});isNew=true;}
  else if(row.Emoji==emoji){db.Remove(row);mine=null;}
  else row.Emoji=emoji;
  try{await db.SaveChangesAsync();}catch(DbUpdateException){}
  var count=await db.Set<CamXuc>().CountAsync(x=>x.TargetType==type&&x.TargetId==target);
  var owner=postAuthor??commentAuthor;
  if(isNew&&owner!=null&&owner!=UserId)notifications.Tao(owner.Value,"CamXuc",(await friends.Names([UserId]))[UserId]+" đã bày tỏ cảm xúc với "+(type=="Post"?"bài viết":"bình luận")+" của bạn.","/bai-viet/"+postId);
  await db.SaveChangesAsync();
  var breakdown=await db.Set<CamXuc>().AsNoTracking().Where(x=>x.TargetType==type&&x.TargetId==target).GroupBy(x=>x.Emoji).Select(g=>new{g.Key,N=g.Count()}).ToDictionaryAsync(g=>g.Key,g=>g.N);
  return (count,mine,breakdown);
 }
 /// <summary>Danh sach nguoi da tha cam xuc (toi da 100), kem bieu tuong.</summary>
 public async Task<List<ReactorDto>> Reactors(string type,long target)
 {
  Me();Require(type is "Post" or "Comment","Loại không hợp lệ.",400);
  if(type=="Post")await ViewablePost(target);
  else{var c=await db.Set<BinhLuan>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==target&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy bình luận.",404);await ViewablePost(c.PostId);}
  var rows=await db.Set<CamXuc>().AsNoTracking().Where(x=>x.TargetType==type&&x.TargetId==target).OrderByDescending(x=>x.Id).Take(100).ToListAsync();
  var names=await friends.Names(rows.Select(r=>r.AccountId));
  return rows.Select(r=>new ReactorDto(r.AccountId,names.GetValueOrDefault(r.AccountId,"Người dùng"),r.Emoji,null)).ToList();
 }

 // ------------------------------------------------------------------ chia se / luu / theo doi
 /// <summary>Chia se mot bai cong khai len trang cua toi, kem loi nhan.</summary>
 public async Task<PostDto> Share(long postId,string? content,string visibility)
 {
  Me();Require(Role==Traveler,"Chỉ khách du lịch mới chia sẻ bài viết.",403);
  var original=await ViewablePost(postId);
  Require(original.Visibility=="Public","Chỉ chia sẻ được bài viết công khai.",422);
  var text=(content??"").Replace("\r\n","\n").Trim();Require(text.Length<=2000,"Lời nhắn tối đa 2000 ký tự.",400);
  Require(Visibilities.Contains(visibility),"Chọn ai được xem bài viết.",400);
  var since=Clock.BayGioUtc.AddHours(-1);
  Require(await db.Set<BaiViet>().CountAsync(p=>p.AuthorId==UserId&&p.CreatedAt>since)<10,"Bạn đăng quá nhiều bài trong một giờ. Hãy thử lại sau.",429);
  // Chia se lai mot bai da duoc chia se: dan ve bai goc.
  var rootId=original.SharedPostId??original.Id;
  BaiViet post=null!;
  await Execute(async()=>
  {
   post=new BaiViet{AuthorId=UserId,Content=text,Visibility=visibility,CreatedAt=Clock.BayGioUtc,Kind="Share",SharedPostId=rootId};
   db.Add(post);await db.SaveChangesAsync();
   var rootPost=rootId==original.Id?original:await db.Set<BaiViet>().AsNoTracking().SingleOrDefaultAsync(b=>b.Id==rootId&&b.DeletedAt==null);
   if(rootPost!=null&&rootPost.AuthorId!=UserId)notifications.Tao(rootPost.AuthorId,"ChiaSe",(await friends.Names([UserId]))[UserId]+" đã chia sẻ bài viết của bạn.","/bai-viet/"+post.Id);
  });
  return (await Project([post]))[0];
 }
 public async Task<bool> SetSaved(long postId,bool saved)
 {
  Me();await ViewablePost(postId);
  var row=await db.Set<BaiVietDaLuu>().SingleOrDefaultAsync(x=>x.AccountId==UserId&&x.PostId==postId);
  if(saved&&row==null)db.Add(new BaiVietDaLuu{AccountId=UserId,PostId=postId,CreatedAt=Clock.BayGioUtc});
  else if(!saved&&row!=null)db.Remove(row);
  try{await db.SaveChangesAsync();}catch(DbUpdateException){}
  return saved;
 }
 /// <summary>Theo doi / bo theo doi mot co so (Trang). Bai gioi thieu cua co so se hien trong "Danh cho ban".</summary>
 public async Task<bool> SetFollow(long branchId,bool follow)
 {
  Me();Require(Role==Traveler,"Chỉ khách du lịch mới theo dõi cơ sở.",403);
  Require(await db.Set<CoSoDichVu>().AnyAsync(b=>b.Id==branchId&&b.Status=="Approved"&&b.PublishedVersion!=null),"Cơ sở không còn được công bố.",404);
  var row=await db.Set<TheoDoiCoSo>().SingleOrDefaultAsync(x=>x.AccountId==UserId&&x.BranchId==branchId);
  if(follow&&row==null)db.Add(new TheoDoiCoSo{AccountId=UserId,BranchId=branchId,CreatedAt=Clock.BayGioUtc});
  else if(!follow&&row!=null)db.Remove(row);
  try{await db.SaveChangesAsync();}catch(DbUpdateException){}
  return follow;
 }
 public async Task<int> FollowerCount(long branchId)=>await db.Set<TheoDoiCoSo>().CountAsync(x=>x.BranchId==branchId);
}
