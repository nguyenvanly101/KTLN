using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HoTroDuLich.Web.Controllers;

/// <summary>API JSON ket ban (giong Facebook): ban be, loi moi, goi y, tim nguoi, ho so.</summary>
[ApiController,Route("api/mxh"),Authorize(Roles=TravelService.Traveler)]
public sealed class FriendsApiController(FriendService friends):ControllerBase
{
 async Task<IActionResult> Run(Func<Task<object?>> work)
 {
  try{return Ok(await work());}
  catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}
 }
 [HttpGet("ban-be")] public Task<IActionResult> Friends(string? q)=>Run(async()=>await friends.Friends(q));
 [HttpGet("loi-moi")] public Task<IActionResult> Requests()=>Run(async()=>{var (i,o)=await friends.Requests();return new{incoming=i,outgoing=o};});
 [HttpGet("goi-y")] public Task<IActionResult> Suggestions()=>Run(async()=>await friends.Suggestions());
 [HttpGet("tim"),EnableRateLimiting("discovery")] public Task<IActionResult> Search(string? q)=>Run(async()=>await friends.Search(q));
 [HttpPost("phu-hop/bo-qua")] public Task<IActionResult> DismissMatch([FromForm]long needId)=>Run(async()=>{await friends.DismissMatch(needId);return new{ok=true};});
 [HttpGet("phu-hop")] public Task<IActionResult> Matches()=>Run(async()=>await friends.CompanionMatches());
 [HttpGet("ho-so/{id:long}")] public Task<IActionResult> Profile(long id)=>Run(async()=>await friends.Profile(id));
 [HttpGet("nguoi/{id:long}/ban-be")] public Task<IActionResult> FriendsOf(long id)=>Run(async()=>await friends.FriendsOf(id));
 [HttpPost("ban-be/gui")] public Task<IActionResult> Send([FromForm]long accountId)=>Run(async()=>new{relation=await friends.Send(accountId)});
 [HttpPost("ban-be/{requestId:long}/chap-nhan")] public Task<IActionResult> Accept(long requestId)=>Run(async()=>{await friends.Respond(requestId,true);return new{ok=true};});
 [HttpPost("ban-be/{requestId:long}/tu-choi")] public Task<IActionResult> Decline(long requestId)=>Run(async()=>{await friends.Respond(requestId,false);return new{ok=true};});
 [HttpPost("ban-be/{requestId:long}/huy")] public Task<IActionResult> Cancel(long requestId)=>Run(async()=>{await friends.Cancel(requestId);return new{ok=true};});
 [HttpPost("ban-be/xoa")] public Task<IActionResult> Unfriend([FromForm]long accountId)=>Run(async()=>{await friends.Unfriend(accountId);return new{ok=true};});
}

/// <summary>API JSON bai viet, binh luan, cam xuc.</summary>
[ApiController,Route("api/mxh"),Authorize(Roles=TravelService.Traveler+","+TravelService.ProviderRole)]
public sealed class PostsApiController(PostService posts):ControllerBase
{
 async Task<IActionResult> Run(Func<Task<object?>> work)
 {
  try{return Ok(await work());}
  catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}
 }
 [HttpGet("bai-viet")] public Task<IActionResult> Feed(string? scope,long? before,long? branchId,string? q)=>Run(async()=>await posts.Feed(scope is "public" or "branch" or "companion" or "home" or "saved" or "friends"?scope:"home",before,branchId,q));
 [HttpPost("bao-cao"),EnableRateLimiting("chat")] public Task<IActionResult> Report([FromForm]long postId,[FromForm]long? commentId,[FromForm]string reason,[FromForm]string? note)=>Run(async()=>{await posts.Report(postId,commentId,reason,note);return new{ok=true};});
 [HttpGet("nguoi/{id:long}/anh")] public Task<IActionResult> PhotosOf(long id)=>Run(async()=>await posts.PhotosOf(id));
 [HttpGet("nguoi/{id:long}/bai-viet")] public Task<IActionResult> ByAuthor(long id,long? before)=>Run(async()=>await posts.ByAuthor(id,before));
 [HttpGet("bai-viet/{id:long}")] public Task<IActionResult> Get(long id)=>Run(async()=>await posts.Get(id));
 [HttpPost("bai-viet"),RequestSizeLimit(24*1024*1024),EnableRateLimiting("chat")] public Task<IActionResult> Create([FromForm]PostInput input)=>Run(async()=>await posts.Create(input));
 [HttpPost("bai-viet/{id:long}/sua")] public Task<IActionResult> Edit(long id,[FromForm]PostInput input)=>Run(async()=>await posts.Edit(id,input));
 [HttpPost("bai-viet/{id:long}/xoa")] public Task<IActionResult> Delete(long id)=>Run(async()=>{await posts.Delete(id);return new{ok=true};});
 [HttpGet("bai-viet/{id:long}/binh-luan")] public Task<IActionResult> Comments(long id)=>Run(async()=>await posts.Comments(id));
 [HttpPost("bai-viet/{id:long}/binh-luan"),EnableRateLimiting("chat")] public Task<IActionResult> AddComment(long id,[FromForm]CommentInput input)=>Run(async()=>await posts.AddComment(id,input));
 [HttpPost("binh-luan/{id:long}/sua")] public Task<IActionResult> EditComment(long id,[FromForm]CommentInput input)=>Run(async()=>{await posts.EditComment(id,input);return new{ok=true};});
 [HttpPost("binh-luan/{id:long}/xoa")] public Task<IActionResult> DeleteComment(long id)=>Run(async()=>{await posts.DeleteComment(id);return new{ok=true};});
 [HttpPost("cam-xuc"),EnableRateLimiting("chat")] public Task<IActionResult> React([FromForm]string type,[FromForm]long target,[FromForm]string? emoji)=>Run(async()=>{var (n,mine,breakdown)=await posts.React(type,target,emoji);return new{count=n,mine,reactions=breakdown};});
 [HttpGet("cam-xuc/danh-sach")] public Task<IActionResult> Reactors(string type,long target)=>Run(async()=>await posts.Reactors(type,target));
 [HttpPost("bai-viet/{id:long}/chia-se"),EnableRateLimiting("chat")] public Task<IActionResult> Share(long id,[FromForm]string? content,[FromForm]string? visibility)=>Run(async()=>await posts.Share(id,content,visibility??"Public"));
 [HttpPost("bai-viet/{id:long}/luu")] public Task<IActionResult> Save(long id,[FromForm]bool saved=true)=>Run(async()=>new{saved=await posts.SetSaved(id,saved)});
 [HttpPost("co-so/{id:long}/theo-doi")] public Task<IActionResult> Follow(long id,[FromForm]bool follow=true)=>Run(async()=>new{following=await posts.SetFollow(id,follow),followers=await posts.FollowerCount(id)});
 [HttpGet("bai-viet-moi")] public Task<IActionResult> NewCount(string? scope,long since,long? branchId)=>Run(async()=>new{count=await posts.NewCount(scope is "home" or "public" or "branch" or "companion" or "saved"?scope:"home",since,branchId)});
}

public sealed class SocialPagesController(TravelService travel,FriendService friends,PostService posts):AppController(travel)
{
 [HttpGet("/bang-tin"),Authorize(Roles=TravelService.Traveler+","+TravelService.ProviderRole)] public IActionResult Feed()=>V("Social/Feed");
 [HttpGet("/bai-viet/{id:long}"),Authorize(Roles=TravelService.Traveler+","+TravelService.ProviderRole)] public async Task<IActionResult> Post(long id){await posts.Get(id);return V("Social/Post",id);}
 [HttpGet("/mxh/bai-viet/{id:long}/anh/{index:int}"),Authorize(Roles=TravelService.Traveler+","+TravelService.ProviderRole)]
 public async Task<IActionResult> PostPhoto(long id,int index)
 {
  var path=await posts.PhotoFile(id,index);Response.Headers.CacheControl="private, max-age=300";
  return PhysicalFile(path,Path.GetExtension(path).Equals(".png",StringComparison.OrdinalIgnoreCase)?"image/png":"image/jpeg");
 }
 [HttpGet("/ban-be"),Authorize(Roles=TravelService.Traveler)] public IActionResult Friends(string? tab)=>V("Social/Friends",tab??"ban-be");
 [HttpGet("/nguoi/{id:long}"),Authorize(Roles=TravelService.Traveler)] public async Task<IActionResult> Profile(long id)=>V("Social/Profile",await friends.Profile(id));
 [HttpGet("/mxh/bia/{id:long}"),Authorize(Roles=TravelService.Traveler)]
 public async Task<IActionResult> Cover(long id)
 {
  var path=await friends.CoverPath(id);Response.Headers.CacheControl="private, max-age=300";
  return PhysicalFile(path,Path.GetExtension(path).Equals(".png",StringComparison.OrdinalIgnoreCase)?"image/png":"image/jpeg");
 }
 [HttpGet("/mxh/anh/{id:long}"),Authorize(Roles=TravelService.Traveler)]
 public async Task<IActionResult> Avatar(long id)
 {
  var path=await friends.AvatarPath(id);Response.Headers.CacheControl="private, max-age=300";
  return PhysicalFile(path,Path.GetExtension(path).Equals(".png",StringComparison.OrdinalIgnoreCase)?"image/png":"image/jpeg");
 }
}

[Authorize(Roles=TravelService.AdminRole)]
public sealed class PostReportsController(TravelService travel,PostService posts):AppController(travel)
{
 [HttpGet("/quan-tri/bao-cao-bai-viet")] public async Task<IActionResult> Index(string? status){var s=status=="Done"?"Done":"Open";return V("Admin/PostReports",(s,await posts.ReportQueue(s)));}
 [HttpPost("/quan-tri/bao-cao-bai-viet/xu-ly")] public async Task<IActionResult> Handle(long id,bool hide){await posts.HandleReport(id,hide);return Done("/quan-tri/bao-cao-bai-viet",hide?"Đã ẩn nội dung bị báo cáo.":"Đã bỏ qua báo cáo.");}
}
