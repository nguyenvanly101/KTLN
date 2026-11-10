using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;
public class AvatarController(TravelService s):AppController(s){
 [AllowAnonymous,HttpGet("/ho-so/anh-dai-dien/{id:long}")]
 public async Task<IActionResult> Image(long id){var shared=await S.SharedProfile(id);if(shared.Avatar==null)return NotFound();var profile=await S.ProfileOf(id);var path=profile.AnhDaiDien;if(path==null)return NotFound();Response.Headers.CacheControl="private, no-store";return PhysicalFile(S.UploadPath(path),Path.GetExtension(path).Equals(".png",StringComparison.OrdinalIgnoreCase)?"image/png":"image/jpeg");}
}
