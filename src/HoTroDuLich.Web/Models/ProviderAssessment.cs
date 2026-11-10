using HoTroDuLich.Web.Domain;
namespace HoTroDuLich.Web.Models;
public record ProviderAssessment(List<DichVuDuLich> Services,List<YeuCauDatDichVu> Waiting,List<YeuCauDatDichVu> Confirmed,List<KetQuaCungCapDichVu> Complaints);
