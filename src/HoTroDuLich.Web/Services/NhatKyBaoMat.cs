using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HoTroDuLich.Web.Services;

// Always-run filters also observe MVC authorization/antiforgery short circuits.
public sealed class NhatKyAntiforgeryFilter:IAsyncAlwaysRunResultFilter
{
 public async Task OnResultExecutionAsync(ResultExecutingContext context,ResultExecutionDelegate next)
 {
  if(context.Result is IAntiforgeryValidationFailedResult)context.HttpContext.Items["InvalidAntiforgery"]=true;
  await next();
 }
}

public sealed class NhatKyBaoMatMiddleware(RequestDelegate next,ILogger<NhatKyBaoMatMiddleware> logger)
{
 public async Task InvokeAsync(HttpContext context,INhatKyService audit)
 {
  var action=context.Request.Method+" "+context.Request.Path;
  if(action.Length>350)action=action[..350];
  try
  {
   await next(context);
   if(context.Response.StatusCode==403)
    await audit.GhiRiengAsync("TuChoiQuyen","HTTP",0,null,"403",action+"; mức kiểm tra: vai trò hoặc quyền trên bản ghi");
   else if(context.Response.StatusCode==429)
    await audit.GhiRiengAsync("VuotGioiHanTanSuat","HTTP",0,null,"429",action);
   else if(context.Items["InvalidAntiforgery"] is true)
    await audit.GhiRiengAsync("AntiforgeryKhongHopLe","HTTP",0,null,"400",action);
  }
  catch(Exception error)when(!(error is OperationCanceledException&&context.RequestAborted.IsCancellationRequested))
  {
   var reason=$"Tham chiếu {context.TraceIdentifier}; loại lỗi {error.GetType().Name}";
   await audit.GhiRiengAsync("LoiHeThong","HTTP",0,null,"ThatBai",reason);
   logger.LogError("Lỗi yêu cầu; {Reference}; {Type}",context.TraceIdentifier,error.GetType().Name);
   throw;
  }
 }
}
