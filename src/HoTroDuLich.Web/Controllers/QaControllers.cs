using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;

public sealed class QaController(TravelService travel,QaService qa):AppController(travel)
{
 const string AnyUser=TravelService.Traveler+","+TravelService.ProviderRole+","+TravelService.AdminRole;
 [AllowAnonymous,HttpGet("/hoi-dap/{donViId:long}")] public async Task<IActionResult> List(long donViId,int page=1)=>V("Qa/List",await qa.List(donViId,page));
 [Authorize(Roles=AnyUser),HttpPost("/hoi-dap/{donViId:long}")] public async Task<IActionResult> Ask(long donViId,string? title,string? body){var id=await qa.Ask(donViId,title,body);return Done("/hoi-dap/cau/"+id,"Đã đăng câu hỏi. Người đã đi và người địa phương sẽ trả lời.");}
 [AllowAnonymous,HttpGet("/hoi-dap/cau/{id:long}")] public async Task<IActionResult> Question(long id)=>V("Qa/Question",await qa.Detail(id));
 [Authorize(Roles=AnyUser),HttpPost("/hoi-dap/cau/{id:long}/tra-loi")] public async Task<IActionResult> Answer(long id,string? body){await qa.Answer(id,body);return Done("/hoi-dap/cau/"+id,"Đã gửi câu trả lời.");}
 [Authorize(Roles=AnyUser),HttpPost("/hoi-dap/tra-loi/{answerId:long}/binh-chon")] public async Task<IActionResult> Vote(long answerId,long questionId){await qa.Vote(answerId);return LocalRedirect("/hoi-dap/cau/"+questionId);}
 [Authorize(Roles=AnyUser),HttpPost("/hoi-dap/tra-loi/{answerId:long}/huu-ich")] public async Task<IActionResult> Accept(long answerId,long questionId){await qa.Accept(answerId);return LocalRedirect("/hoi-dap/cau/"+questionId);}
 [Authorize(Roles=AnyUser),HttpPost("/hoi-dap/cau/{id:long}/xoa")] public async Task<IActionResult> DeleteQuestion(long id,long donViId){await qa.DeleteQuestion(id);return Done("/hoi-dap/"+donViId,"Đã xóa câu hỏi.");}
 [Authorize(Roles=AnyUser),HttpPost("/hoi-dap/tra-loi/{answerId:long}/xoa")] public async Task<IActionResult> DeleteAnswer(long answerId,long questionId){await qa.DeleteAnswer(answerId);return Done("/hoi-dap/cau/"+questionId,"Đã xóa câu trả lời.");}
}

public sealed class WrongInfoController(TravelService travel,QaService qa):AppController(travel)
{
 [Authorize(Roles=TravelService.Traveler),HttpPost("/co-so/{id:long}/bao-sai")] public async Task<IActionResult> Report(long id,string? field,string? detail){await qa.Report(id,field,detail);return Done("/co-so/"+id,"Cảm ơn bạn. Nhà cung cấp đã nhận thông tin bạn báo.");}
 [Authorize(Roles=TravelService.ProviderRole),HttpGet("/nha-cung-cap/co-so/{id:long}/bao-sai")] public async Task<IActionResult> Index(long id)=>V("Qa/WrongInfo",await qa.WrongInfoPage(id));
 [Authorize(Roles=TravelService.ProviderRole),HttpPost("/nha-cung-cap/co-so/{id:long}/bao-sai/{reportId:long}")] public async Task<IActionResult> Resolve(long id,long reportId,bool fixedIt,string? note){await qa.ResolveWrong(id,reportId,fixedIt,note);return Done("/nha-cung-cap/co-so/"+id+"/bao-sai","Đã phản hồi báo cáo.");}
}
