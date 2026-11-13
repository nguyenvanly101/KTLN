using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;

/// <summary>Anh thuc te cua co so: cong khai khi co so da duoc duyet; ban nhap/ban cho duyet chi chu co so va quan tri xem.</summary>
public sealed class BranchPhotosController(TravelService travel,BranchService branches):AppController(travel)
{
 [HttpGet("/co-so/{id:long}/anh/{index:int}"),AllowAnonymous]
 public async Task<IActionResult> Photo(long id,int index,long? revision=null)
 {
  var path=await branches.PhotoFile(id,index,revision);
  Response.Headers.CacheControl=revision==null?"public, max-age=300":"private, no-store";
  return PhysicalFile(path,Path.GetExtension(path).Equals(".png",StringComparison.OrdinalIgnoreCase)?"image/png":"image/jpeg");
 }
}
