namespace HoTroDuLich.Web.Services;
public sealed class PreparationReminderService(IServiceScopeFactory scopes,ILogger<PreparationReminderService> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromMinutes(5));
        do { try { using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<ProductService>().RemindDue(token); }
            catch(Exception error) when(!token.IsCancellationRequested){logger.LogError(error,"Không thể tạo lời nhắc chuẩn bị; sẽ thử lại ở chu kỳ sau.");}
        } while(await timer.WaitForNextTickAsync(token));
    }
}
