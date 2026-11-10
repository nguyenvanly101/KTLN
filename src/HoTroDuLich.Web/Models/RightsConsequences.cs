using HoTroDuLich.Web.Domain;
namespace HoTroDuLich.Web.Models;
public record RightsConsequences(NhaCungCapDichVu Provider,string Operation,int Services,int Unpublished,int StoppedRequests,int PreservedAgreements);
