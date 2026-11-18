using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HoTroDuLich.Web.Controllers;

/// <summary>API JSON + luong su kien thoi gian thuc cho tin nhan khach &lt;-&gt; co so.</summary>
[ApiController,Route("chat"),Authorize(Roles=TravelService.Traveler+","+TravelService.ProviderRole)]
public sealed class ChatApiController(ChatService chat,ChatBroker broker):ControllerBase
{
 async Task<IActionResult> Run(Func<Task<object?>> work)
 {
  try{return Ok(await work());}
  catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}
 }
 [HttpGet("luong")]
 public async Task Stream(CancellationToken ct)
 {
  Response.Headers.ContentType="text/event-stream";Response.Headers.CacheControl="no-cache, no-store";Response.Headers["X-Accel-Buffering"]="no";
  var account=chat.Account;var (id,reader)=broker.Subscribe(account);
  try
  {
   await Response.WriteAsync("retry: 3000\n\n",ct);await Response.Body.FlushAsync(ct);
   var read=reader.ReadAsync(ct).AsTask();
   while(!ct.IsCancellationRequested)
   {
    var tick=Task.Delay(20000,ct);
    var done=await Task.WhenAny(read,tick);
    if(done==read){var data=await read;await Response.WriteAsync("data: "+data+"\n\n",ct);read=reader.ReadAsync(ct).AsTask();}
    else await Response.WriteAsync(": ping\n\n",ct);
    await Response.Body.FlushAsync(ct);
   }
  }
  catch(OperationCanceledException){}
  catch(System.Threading.Channels.ChannelClosedException){}
  finally{broker.Unsubscribe(account,id);}
 }
 [HttpGet("danh-sach")] public Task<IActionResult> List(long? coSo,string? loc,string? q)=>Run(async()=>await chat.List(coSo,loc,q));
 [HttpGet("chua-doc")] public Task<IActionResult> Unread()=>Run(async()=>new{count=await chat.Unread()});
 [HttpGet("co-so/{branchId:long}")] public Task<IActionResult> Start(long branchId)=>Run(async()=>await chat.StartInfo(branchId));
 [HttpGet("nguoi/{accountId:long}")] public Task<IActionResult> StartPeer(long accountId)=>Run(async()=>await chat.StartPeer(accountId));
 [HttpPost("{id:long}/chan")] public Task<IActionResult> Block(long id,[FromForm]bool block)=>Run(async()=>{await chat.SetBlock(id,block);return new{ok=true};});
 [HttpGet("{id:long}")] public Task<IActionResult> Thread(long id)=>Run(async()=>await chat.Thread(id));
 [HttpGet("{id:long}/tin")] public Task<IActionResult> Messages(long id,long sau=0,long truoc=0)=>Run(async()=>await chat.Messages(id,sau,60,truoc));
 [HttpPost("gui"),EnableRateLimiting("chat"),RequestSizeLimit(8*1024*1024)]
 public Task<IActionResult> Send([FromForm]ChatSendInput input)=>Run(async()=>{var (m,c)=await chat.Send(input);return new{message=m,conversationId=c};});
 [HttpGet("co-so/{branchId:long}/dich-vu")] public Task<IActionResult> Services(long branchId)=>Run(async()=>await chat.Services(branchId));
 [HttpGet("tim"),EnableRateLimiting("discovery")] public Task<IActionResult> Find(string? q)=>Run(async()=>await chat.Find(q));
 [HttpGet("dat/kiem-tra")] public Task<IActionResult> BookingCheck([FromQuery]ChatBookingInput input)=>Run(async()=>await chat.BookingCheck(input));
 [HttpGet("{id:long}/the")] public Task<IActionResult> Cards(long id)=>Run(async()=>await chat.Cards(id));
 [HttpPost("dat"),EnableRateLimiting("chat")] public Task<IActionResult> Book([FromForm]ChatBookingInput input)=>Run(async()=>{var (m,c)=await chat.SendBooking(input);return new{message=m,conversationId=c};});
 [HttpPost("{id:long}/dat/hanh-dong"),EnableRateLimiting("chat")] public Task<IActionResult> BookingAct(long id,[FromForm]ChatBookingAction input)=>Run(async()=>{await chat.BookingAct(id,input);return new{ok=true};});
 [HttpGet("nguoi/{accountId:long}/loi-ru")] public Task<IActionResult> PeerNeeds(long accountId)=>Run(async()=>await chat.PeerNeeds(accountId));
 /// <summary>Chuyen di cua toi co the moi nguoi dang tro chuyen vao.</summary>
 [HttpGet("nguoi/{accountId:long}/chuyen-di")] public Task<IActionResult> InvitablePlans(long accountId)=>Run(async()=>await HttpContext.RequestServices.GetRequiredService<TripShareService>().InvitablePlans(accountId));
 [HttpPost("moi-chuyen-di"),EnableRateLimiting("chat")] public Task<IActionResult> InviteToPlan([FromForm]long planId,[FromForm]long accountId)=>Run(async()=>{await HttpContext.RequestServices.GetRequiredService<TripShareService>().Invite(planId,accountId);return new{ok=true};});
 [HttpGet("{id:long}/ket-noi")] public Task<IActionResult> Connections(long id)=>Run(async()=>await chat.ConnectionCards(id));
 [HttpPost("ru"),EnableRateLimiting("chat")] public Task<IActionResult> Invite([FromForm]ChatConnectInput input)=>Run(async()=>{var (m,c)=await chat.SendConnection(input);return new{message=m,conversationId=c};});
 [HttpPost("{id:long}/ket-noi/hanh-dong"),EnableRateLimiting("chat")] public Task<IActionResult> ConnectionAct(long id,[FromForm]ChatConnectAction input)=>Run(async()=>{await chat.ConnectionAct(id,input);return new{ok=true};});
 [HttpPost("nhom"),EnableRateLimiting("chat")] public Task<IActionResult> CreateGroup([FromForm]ChatGroupInput input)=>Run(async()=>new{conversationId=await chat.CreateGroup(input)});
 [HttpPost("{id:long}/nhom/them")] public Task<IActionResult> AddMembers(long id,[FromForm]ChatGroupMembersInput input)=>Run(async()=>{await chat.AddGroupMembers(id,input);return new{ok=true};});
 [HttpPost("{id:long}/nhom/xoa")] public Task<IActionResult> RemoveMember(long id,[FromForm]long accountId)=>Run(async()=>{await chat.RemoveGroupMember(id,accountId);return new{ok=true};});
 [HttpPost("{id:long}/nhom/roi")] public Task<IActionResult> Leave(long id)=>Run(async()=>{await chat.LeaveGroup(id);return new{ok=true};});
 [HttpPost("{id:long}/nhom/doi-ten")] public Task<IActionResult> Rename(long id,[FromForm]string? title)=>Run(async()=>{await chat.RenameGroup(id,title);return new{ok=true};});
 [HttpGet("{id:long}/tin/{messageId:long}")] public Task<IActionResult> GetMessage(long id,long messageId)=>Run(async()=>await chat.GetMessage(id,messageId));
 [HttpPost("{id:long}/tin/{messageId:long}/sua"),EnableRateLimiting("chat")] public Task<IActionResult> EditMessage(long id,long messageId,[FromForm]string? body)=>Run(async()=>{await chat.EditMessage(id,messageId,body);return new{ok=true};});
 [HttpPost("{id:long}/tin/{messageId:long}/thu-hoi"),EnableRateLimiting("chat")] public Task<IActionResult> RecallMessage(long id,long messageId)=>Run(async()=>{await chat.RecallMessage(id,messageId);return new{ok=true};});
 [HttpPost("{id:long}/tin/{messageId:long}/cam-xuc"),EnableRateLimiting("chat")] public Task<IActionResult> ReactMessage(long id,long messageId,[FromForm]string? emoji)=>Run(async()=>{await chat.React(id,messageId,emoji);return new{ok=true};});
 [HttpGet("{id:long}/tep/{messageId:long}")]
 public async Task<IActionResult> File(long id,long messageId)
 {
  try{var (path,name)=await chat.FileOf(id,messageId);Response.Headers.CacheControl="private, max-age=3600";return PhysicalFile(path,"application/pdf",name);}
  catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}
 }
 [HttpGet("{id:long}/thoa-thuan")] public Task<IActionResult> Agreements(long id)=>Run(async()=>await chat.AgreementCards(id));
 [HttpPost("thoa-thuan"),EnableRateLimiting("chat")] public Task<IActionResult> SendAgreement([FromForm]ChatAgreementInput input)=>Run(async()=>{var (m,c)=await chat.SendAgreement(input);return new{message=m,conversationId=c};});
 [HttpPost("{id:long}/thoa-thuan/hanh-dong"),EnableRateLimiting("chat")] public Task<IActionResult> AgreementAct(long id,[FromForm]ChatAgreementAction input)=>Run(async()=>{await chat.AgreementAct(id,input);return new{ok=true};});
 [HttpPost("{id:long}/bao-cao"),EnableRateLimiting("chat")] public Task<IActionResult> Report(long id,[FromForm]ChatReportInput input)=>Run(async()=>{await chat.Report(id,input);return new{ok=true};});
 [HttpGet("ke-hoach")] public Task<IActionResult> Plans()=>Run(async()=>(await chat.MyPlans()).Select(p=>new ChatPlanDto(p.Id,p.Name,p.Start,p.End)));
 [HttpPost("{id:long}/dat/coc"),EnableRateLimiting("chat")] public Task<IActionResult> RequestDeposit(long id,[FromForm]ChatDepositInput input)=>Run(async()=>{await chat.RequestDeposit(id,input);return new{ok=true};});
 [HttpPost("{id:long}/dat/coc/xac-nhan"),EnableRateLimiting("chat")] public Task<IActionResult> ConfirmDeposit(long id,[FromForm]long requestId)=>Run(async()=>{await chat.ConfirmDeposit(id,requestId);return new{ok=true};});
 [HttpPost("{id:long}/dat/chuyen-di")] public Task<IActionResult> AssignPlan(long id,[FromForm]long requestId,[FromForm]long? planId)=>Run(async()=>{await chat.AssignPlan(id,requestId,planId);return new{ok=true};});
 [HttpPost("{id:long}/da-xem")] public Task<IActionResult> Read(long id)=>Run(async()=>{await chat.MarkRead(id);return new{ok=true};});
 [HttpPost("{id:long}/dang-soan"),EnableRateLimiting("chat")] public Task<IActionResult> Typing(long id)=>Run(async()=>{await chat.Typing(id);return new{ok=true};});
 [HttpPost("{id:long}/trang-thai")] public Task<IActionResult> State(long id,[FromForm]ChatStateInput input)=>Run(async()=>{await chat.SetState(id,input);return new{ok=true};});
 [HttpGet("tin-mau/{branchId:long}")] public Task<IActionResult> Quick(long branchId)=>Run(async()=>await chat.Quick(branchId));
 [HttpGet("{id:long}/anh/{messageId:long}")]
 public async Task<IActionResult> Image(long id,long messageId)
 {
  try{var path=await chat.ImageFile(id,messageId);Response.Headers.CacheControl="private, max-age=3600";return PhysicalFile(path,Path.GetExtension(path).Equals(".png",StringComparison.OrdinalIgnoreCase)?"image/png":"image/jpeg");}
  catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}
 }
}

public sealed class ChatPagesController(TravelService travel,ChatService chat):AppController(travel)
{
 const string Both=TravelService.Traveler+","+TravelService.ProviderRole;
 async Task<ChatPageModel> Page(long? open=null,ChatStartDto? start=null,long? branch=null)
  {
  if(travel.Role!=TravelService.ProviderRole)return new("Traveler",open,start,[],branch);
  // Nha cung cap: chi co so dang chon; mo cuoc tro chuyen cua co so khac thi chuyen sang co so do.
  var bs=HttpContext.RequestServices.GetRequiredService<BranchService>();
  if(open is long oid&&await bs.Focus(await chat.BranchOf(oid)) is {} moved)TempData["Success"]="Đã chuyển sang cơ sở "+moved.Name+" để trả lời cuộc trò chuyện này.";
  var cur=await bs.CurrentBranch();
  return new("Branch",open,start,(await chat.MyBranches()).Where(b=>b.Id==cur?.Id).ToList(),cur?.Id);
  }
 [HttpGet("/tin-nhan"),Authorize(Roles=Both)] public async Task<IActionResult> Inbox(long? coSo)=>V("Chat/Inbox",await Page(null,null,coSo));
 [HttpGet("/nha-cung-cap/tin-nhan"),Authorize(Roles=TravelService.ProviderRole)] public async Task<IActionResult> BranchInbox(long? coSo)=>V("Chat/Inbox",await Page(null,null,coSo));
 [HttpGet("/tin-nhan/{id:long}"),Authorize(Roles=Both)] public async Task<IActionResult> Thread(long id){await chat.Thread(id);return V("Chat/Inbox",await Page(id));}
 [HttpGet("/nha-cung-cap/tin-nhan/{id:long}"),Authorize(Roles=TravelService.ProviderRole)] public async Task<IActionResult> BranchThread(long id){await chat.Thread(id);return V("Chat/Inbox",await Page(id));}
 [HttpGet("/tin-nhan/co-so/{branchId:long}"),Authorize(Roles=Both)]
 public async Task<IActionResult> WithBranch(long branchId)
 {
  var info=await chat.StartInfo(branchId);
  if(info.ConversationId is long existing)return Redirect("/tin-nhan/"+existing);
  return V("Chat/Inbox",await Page(null,info));
 }
 [HttpGet("/tin-nhan/nguoi/{accountId:long}"),Authorize(Roles=TravelService.Traveler)]
 public async Task<IActionResult> WithPerson(long accountId)
 {
  var info=await chat.StartPeer(accountId);
  if(info.ConversationId is long existing)return Redirect("/tin-nhan/"+existing);
  return V("Chat/Inbox",await Page(null,info));
 }
 [HttpGet("/tin-nhan/dat-cho/{requestId:long}"),Authorize(Roles=Both)]
 public async Task<IActionResult> OfBooking(long requestId)
 {
  var id=await chat.ConversationOfBooking(requestId);
  if(id==null){try{id=await chat.AttachBooking(requestId);}catch(BusinessException){id=null;}}
  return id==null?Redirect("/Bookings/Detail/"+requestId):Redirect("/tin-nhan/"+id);
 }
 [HttpGet("/tin-nhan/dich-vu/{serviceId:long}"),Authorize(Roles=TravelService.Traveler)]
 public async Task<IActionResult> ForService(long serviceId,long? branchId)
 {
  var target=await chat.BranchForService(serviceId,branchId);
  return target==null?Redirect("/Bookings/Create/"+serviceId):Redirect("/tin-nhan/co-so/"+target+"?dichVu="+serviceId);
 }
 [HttpGet("/nha-cung-cap/tin-nhan/cai-dat"),Authorize(Roles=TravelService.ProviderRole)]
 public async Task<IActionResult> Settings(long? coSo)
 {
  var mine=await chat.MyBranches();if(mine.Count==0)return Redirect("/nha-cung-cap/co-so");
  return V("Chat/Settings",await chat.SettingsPage(coSo??(await HttpContext.RequestServices.GetRequiredService<BranchService>().CurrentBranch())?.Id??mine[0].Id));
 }
 [HttpPost("/nha-cung-cap/tin-nhan/cai-dat"),Authorize(Roles=TravelService.ProviderRole)] public async Task<IActionResult> SaveSettings(ChatSettingsInput input){await chat.SaveSettings(input);return Done("/nha-cung-cap/tin-nhan/cai-dat?coSo="+input.BranchId,"Đã lưu cài đặt tin nhắn.");}
 [HttpPost("/nha-cung-cap/tin-nhan/mau"),Authorize(Roles=TravelService.ProviderRole)] public async Task<IActionResult> SaveQuick(QuickReplyInput input){await chat.SaveQuick(input);return Done("/nha-cung-cap/tin-nhan/cai-dat?coSo="+input.BranchId,input.Delete?"Đã xóa tin mẫu.":"Đã lưu tin mẫu.");}
 public async Task<IActionResult> Invalid(ChatSettingsInput input)=>V("Chat/Settings",await chat.SettingsPage(input.BranchId));
}

[Authorize(Roles=TravelService.AdminRole)]
public sealed class ChatReportsController(TravelService travel,ChatService chat):AppController(travel)
{
 [HttpGet("/quan-tri/bao-cao-chat")] public async Task<IActionResult> Index(string? status)=>V("Chat/Reports",(status=="Done"?"Done":"Open",await chat.ReportQueue(status=="Done"?"Done":"Open")));
 [HttpPost("/quan-tri/bao-cao-chat/xu-ly")] public async Task<IActionResult> Close(long id){await chat.CloseReport(id);return Done("/quan-tri/bao-cao-chat","Đã đánh dấu đã xử lý.");}
}
