using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;

namespace HoTroDuLich.Web.Models;

public class MapLocationInput : CommandInput
{
 [Required, MaxLength(200), Display(Name="Tên địa điểm/cơ sở")] public string Name { get; set; } = "";
 [Required, MaxLength(400), Display(Name="Địa chỉ do bạn cung cấp")] public string Address { get; set; } = "";
 [Required, MaxLength(200), Display(Name="Khu vực")] public string Region { get; set; } = "";
 [MaxLength(256)] public string? GooglePlaceId { get; set; }
 [Range(-90,90)] public decimal? Latitude { get; set; }
 [Range(-180,180)] public decimal? Longitude { get; set; }
 public string LocationSource { get; set; } = "Provider";
 public bool ConfirmLocation { get; set; }
}

public sealed class BranchInput : MapLocationInput
{
 public string Type { get; set; } = "AnUong";
 [MaxLength(2000)] public string? Summary { get; set; }
 [MaxLength(30)] public string? Phone { get; set; }
 [MaxLength(500)] public string? Website { get; set; }
 [MaxLength(300)] public string? Hours { get; set; }
 public bool? Parking { get; set; }
 public bool? VegetarianOptions { get; set; }
 public bool? VeganOnly { get; set; }
 public bool? AccessibleEntrance { get; set; }
 [MaxLength(2000), Display(Name="Căn cứ quản lý cơ sở")] public string OwnershipEvidence { get; set; } = "";
 public string Operation { get; set; } = "Submit";
}

public sealed class LandmarkInput : MapLocationInput
{
 public long? ParentId { get; set; }
 public string Type { get; set; } = "Point";
 [MaxLength(2000)] public string? Summary { get; set; }
 [MaxLength(300)] public string? Hours { get; set; }
 [MaxLength(1000)] public string? Fees { get; set; }
 [Required, MaxLength(200)] public string SourceName { get; set; } = "";
 [MaxLength(500)] public string? SourceUrl { get; set; }
 public DateOnly CheckedOn { get; set; } = TravelService.Today;
 public bool IsPublished { get; set; } = true;
}

public sealed record BranchChoice(long Id,string Name,string Address,string Type);
public sealed record BranchOwnerPage(List<CoSoDichVu> Branches,Dictionary<long,BanCapNhatCoSo> Pending,Dictionary<long,int> ServiceCounts,bool CanSubmit);
public sealed record BranchEditPage(BranchInput Input,CoSoDichVu? Current,List<BanCapNhatCoSo> History,bool CanSubmit);
public sealed record BranchReviewPage(BanCapNhatCoSo Revision,CoSoDichVu Current,NhaCungCapDichVu Provider);
public sealed record BranchPublicPage(CoSoDichVu Branch,NhaCungCapDichVu Provider,List<DichVuDuLich> Services,MapPosition? Position,string? Warning);

public sealed class PlaceSearchFilter
{
 [MaxLength(200)] public string? Q { get; set; }
 [DisplayFormat(ConvertEmptyStringToNull=false)] public string Category { get; set; } = "";
 public string? AnchorKind { get; set; }
 public long? AnchorId { get; set; }
 [MaxLength(256)] public string? GooglePlaceId { get; set; }
 [MaxLength(200)] public string? AnchorName { get; set; }
 [Range(-90,90)] public decimal? Lat { get; set; }
 [Range(-180,180)] public decimal? Lng { get; set; }
 [Range(0.1,50)] public decimal RadiusKm { get; set; } = 3;
 public string Mode { get; set; } = "walk";
 public bool Parking { get; set; }
 public bool VegetarianOptions { get; set; }
 public bool VeganOnly { get; set; }
 public bool AccessibleEntrance { get; set; }
 public bool IncludeGoogle { get; set; } = true;
 public string Sort { get; set; } = "distance";
 [Range(1,1000)] public int Page { get; set; } = 1;
 public long? PlanId { get; set; }
}

public sealed record MapPosition(decimal Lat,decimal Lng);
public sealed record PlaceAttribution(string Name,string? Url);
public sealed record GooglePlace(string Id,string Name,string Address,MapPosition Position,string MapsUrl,string? BusinessStatus,List<PlaceAttribution> Attributions);
public sealed record PlaceSuggestion(string Id,string Name,string Address,string Source,long? LocalId=null,MapPosition? Position=null,string? MapUrl=null);
public sealed record MapAnchor(string Kind,long? Id,string? GooglePlaceId,string Name,string Address,MapPosition Position,List<PlaceAttribution>? Attributions=null);
public sealed record PlaceHit(string Key,string Name,string Address,string Category,MapPosition? Position,double? DistanceKm,CoSoDichVu? Branch,List<DichVuDuLich> Services,GooglePlace? Google,string Url)
{
 public string Source=>Branch!=null?"Website":Google!=null?"Google Maps":"OpenStreetMap";
}
public sealed record PlaceSearchPage(PlaceSearchFilter Filter,MapAnchor? Anchor,List<PlaceHit> Hits,List<MocDiaDiem> Landmarks,int Total,int MissingLocations,List<string> Notices,bool MapsReady,bool PlacesReady);
public sealed record GoogleRoute(int DistanceMeters,double DurationSeconds,string EncodedPolyline,List<string> Warnings);
