using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Controllers;
public sealed class ServicePhotosController(TravelService travel,AppDbContext db):AppController(travel)
{
    [HttpGet("/service-photos/{id:long}/{index:int}"),AllowAnonymous]
    public async Task<IActionResult> Public(long id,int index)=>Photo((await S.PublicService(id)).AnhJson,index);
    [HttpGet("/service-photos/review/{id:long}/{index:int}"),Authorize(Roles=TravelService.ProviderRole+","+TravelService.AdminRole)]
    public async Task<IActionResult> Review(long id,int index,long? revision=null)
    {
        var service=S.Role==TravelService.AdminRole ? await S.Find<DichVuDuLich>(id):await S.OwnService(id);
        if(revision is long key){var version=await db.BanCapNhatDichVu.SingleOrDefaultAsync(x=>x.MaBanCapNhat==key&&x.MaDichVu==id)??throw new BusinessException("Không tìm thấy phiên bản.",404);return Photo(version.AnhJson,index);}
        return Photo(service.AnhJson,index);
    }
    private IActionResult Photo(string? json,int index)
    {
        var images=Ui.ServicePhotos(json);if(index<0||index>=images.Length)return NotFound();
        var path=S.UploadPath(images[index]);if(!System.IO.File.Exists(path))return NotFound();
        var extension=Path.GetExtension(path).ToLowerInvariant();if(extension is not (".jpg" or ".jpeg" or ".png"))return NotFound();
        Response.Headers.CacheControl="no-store";
        return PhysicalFile(path,extension==".png"?"image/png":"image/jpeg");
    }
}
