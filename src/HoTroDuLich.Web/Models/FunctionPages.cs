using HoTroDuLich.Web.Domain;
namespace HoTroDuLich.Web.Models;

public record AdminOverview(int PendingServices, int PendingVerifications, int Providers, int PendingExplanations);
public record AdminReviewRow(BanCapNhatDichVu Revision, string ProviderName);
public record AdminVerificationRow(HoSoXacMinhNhaCungCap Profile, string ProviderName);
public record PageNavigation(string Route, string Query, string Status, int Page, int Total, int PageSize = 20) {
 public int Pages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
}
public record FilteredPage<T>(List<T> Items, PageNavigation Navigation) {
 public string Query => Navigation.Query;
 public string Status => Navigation.Status;
 public int Total => Navigation.Total;
}
