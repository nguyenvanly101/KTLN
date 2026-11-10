using System.Data;
using HoTroDuLich.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public interface IDongHo { DateTime BayGioUtc{get;} DateOnly HomNay{get;} DateTime DoiSangGioVN(DateTime utc); }
public sealed class DongHo(TimeProvider time):IDongHo {
 private static readonly TimeZoneInfo Vietnam=TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows()?"SE Asia Standard Time":"Asia/Ho_Chi_Minh");
 public DateTime BayGioUtc=>time.GetUtcNow().UtcDateTime;
 public DateOnly HomNay=>DateOnly.FromDateTime(DoiSangGioVN(BayGioUtc));
 public DateTime DoiSangGioVN(DateTime utc)=>TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc,DateTimeKind.Utc),Vietnam);
}
public interface IGiaoDich { Task ThucHienAsync(Func<Task> work); Task<T> ThucHienAsync<T>(Func<Task<T>> work); }
public sealed class GiaoDich(AppDbContext db):IGiaoDich {
 public async Task ThucHienAsync(Func<Task> work)=>await ThucHienAsync(async()=>{await work();return true;});
 public async Task<T> ThucHienAsync<T>(Func<Task<T>> work){
  for(var attempt=0;;attempt++){
   try{await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var result=await work();await db.SaveChangesAsync();await transaction.CommitAsync();return result;}
   catch(Exception e)when(LoiCsdl.So(e) is 1205 or -2){db.ChangeTracker.Clear();if(attempt>=2)throw new BusinessException("Hệ thống đang bận. Vui lòng thử lại sau.",503,"CSDL_TAM_THOI");await Task.Delay(Random.Shared.Next(200,801));}
   catch{db.ChangeTracker.Clear();throw;}
  }
 }
}
public static class LoiCsdl {public static int? So(Exception e)=>e is SqlException sql?sql.Number:e.InnerException==null?null:So(e.InnerException);}
public interface ILuuTep {Task<List<(string Name,string Path,string Type,int Size)>> LuuAsync(List<IFormFile> files);void XoaBuTru();void HoanTat();}
public partial class LuuTepService:ILuuTep {
 private readonly List<string> pendingFiles=[];
 public void XoaBuTru(){foreach(var path in pendingFiles)if(File.Exists(path))File.Delete(path);pendingFiles.Clear();}
 public void HoanTat()=>pendingFiles.Clear();
}
