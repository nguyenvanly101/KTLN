using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

// Only coordinates sourced from Google expire; supplier-authored locations remain supplier data.
public sealed class GoogleLocationCleanup(IServiceScopeFactory scopes,ILogger<GoogleLocationCleanup> logger):BackgroundService
{
 public static async Task Purge(AppDbContext db,CancellationToken ct)
 {
  var now=DateTime.UtcNow;
  await db.Set<CoSoDichVu>().Where(x=>x.LocationSource=="Google"&&x.LocationExpiresAt<=now).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Latitude,(decimal?)null).SetProperty(x=>x.Longitude,(decimal?)null).SetProperty(x=>x.LocationExpiresAt,(DateTime?)null),ct);
  await db.Set<BanCapNhatCoSo>().Where(x=>x.LocationSource=="Google"&&x.LocationExpiresAt<=now).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Latitude,(decimal?)null).SetProperty(x=>x.Longitude,(decimal?)null).SetProperty(x=>x.LocationExpiresAt,(DateTime?)null),ct);
  await db.Set<MocDiaDiem>().Where(x=>x.LocationSource=="Google"&&x.LocationExpiresAt<=now).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Latitude,(decimal?)null).SetProperty(x=>x.Longitude,(decimal?)null).SetProperty(x=>x.LocationExpiresAt,(DateTime?)null),ct);
 }
 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
  while(!stoppingToken.IsCancellationRequested)
  {
   try{using var scope=scopes.CreateScope();await Purge(scope.ServiceProvider.GetRequiredService<AppDbContext>(),stoppingToken);}
   catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
   catch(Exception e){logger.LogWarning(e,"Chưa dọn được tọa độ Google hết hạn; sẽ thử lại.");}
   try{await Task.Delay(TimeSpan.FromHours(6),stoppingToken);}catch(OperationCanceledException){break;}
  }
 }
}
