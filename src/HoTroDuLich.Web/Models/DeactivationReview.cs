namespace HoTroDuLich.Web.Models;
public record AccountObligation(string Name,string Url);
public record DeactivationReview(List<AccountObligation> Obligations,int Needs,int Connections,int Services,string? ConfirmationToken);
