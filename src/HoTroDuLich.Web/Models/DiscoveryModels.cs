using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Domain;
namespace HoTroDuLich.Web.Models;

public class DiscoveryFilter
{
 [MaxLength(200)] [Display(Name="Từ khóa")] public string? Q { get; set; }
 [MaxLength(100)] [Display(Name="Khu vực")] public string? Region { get; set; }
 [MaxLength(25)] [Display(Name="Loại thông tin")] public string? Kind { get; set; }
 [MaxLength(500)] [Display(Name="Tiện ích")] public string? Features { get; set; }
 [MaxLength(30)] [Display(Name="Chủ đề")] public string? Topic { get; set; }
 [Range(0,1000000000)] [Display(Name="Giá từ")] public decimal? Min { get; set; }
 [Range(0,1000000000)] [Display(Name="Giá đến")] public decimal? Max { get; set; }
 public DateOnly? Date { get; set; }
 public TimeOnly? At { get; set; }
 [Range(1,100000)] public int Page { get; set; }=1;
 public string Sort { get; set; }="relevance";
 [Range(-90,90)] [Display(Name="Vĩ độ")] public double? Lat { get; set; }
 [Range(-180,180)] [Display(Name="Kinh độ")] public double? Lng { get; set; }
 [Range(.1,200)] [Display(Name="Phạm vi")] public double RadiusKm { get; set; }=5;
 public double? South { get; set; } public double? West { get; set; } public double? North { get; set; } public double? East { get; set; }
 public long? PlanId { get; set; }
 public string? Type { get; set; }
 [Display(Name="Đơn vị giá")] public string? Unit { get; set; }
}
public class DiscoveryCommand : CommandInput
{
 public long EntryId { get; set; } public long CollectionId { get; set; }
 [MaxLength(150)] [Display(Name="Tên")] public string Name { get; set; }="";
 [MaxLength(2000),Display(Name="Nội dung"),DisplayFormat(ConvertEmptyStringToNull=false)] public string Body { get; set; }="";
 [MaxLength(1000)] public string? Source { get; set; }
 [MaxLength(200)] public string Topics { get; set; }="";
 public DateOnly? VisitedOn { get; set; }
 [Range(1,5)] public int Rating { get; set; }=4;
 public Guid SubmissionId { get; set; }=Guid.NewGuid();
 public List<long> EntryIds { get; set; }=[];
 public string? ReturnUrl { get; set; }
}
public class DiscoveryEditInput : DiscoveryCommand
{
 [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? LinkedKind { get; set; }
 [Required,MaxLength(200)] [Display(Name="Tiêu đề")] public string Title { get; set; }="";
 [Required,MaxLength(25)] [Display(Name="Loại thông tin")] public string Kind { get; set; }="Guide";
 [Required,MaxLength(100)] [Display(Name="Khu vực")] public string Region { get; set; }="";
 [MaxLength(700),Display(Name="Tóm tắt"),DisplayFormat(ConvertEmptyStringToNull=false)] public string Summary { get; set; }="";
 [MaxLength(20000),Display(Name="Nội dung"),DisplayFormat(ConvertEmptyStringToNull=false)] public string Content { get; set; }="";
 public long? ParentId { get; set; }
 [MaxLength(400)] [Display(Name="Địa chỉ")] public string? Address { get; set; }
 [Range(-90,90)] [Display(Name="Vĩ độ")] public decimal? Latitude { get; set; }
 [Range(-180,180)] [Display(Name="Kinh độ")] public decimal? Longitude { get; set; }
 [MaxLength(300)] public string? LocationNote { get; set; }
 [Required,MaxLength(200)] [Display(Name="Nguồn thông tin")] public string SourceName { get; set; }="";
 [MaxLength(1000)] [Display(Name="Đường dẫn nguồn")] public string? SourceUrl { get; set; }
 public DateOnly? CheckedOn { get; set; } public DateOnly? ValidFrom { get; set; } public DateOnly? ValidTo { get; set; }
 [Range(0,1000000000)] [Display(Name="Giá từ")] public decimal? PriceFrom { get; set; }
 [Range(0,1000000000)] [Display(Name="Giá đến")] public decimal? PriceTo { get; set; }
 [MaxLength(100)] [Display(Name="Đơn vị giá")] public string? PriceUnit { get; set; }
 [MaxLength(1000)] public string? Includes { get; set; }
 [MaxLength(1000)] public string? Excludes { get; set; }
 [MaxLength(700)] public string? Hours { get; set; }
 [MaxLength(7)] public string? OpenDays { get; set; }
 public TimeOnly? OpenTime { get; set; } public TimeOnly? CloseTime { get; set; }
 [Range(1,1440)] public int? DurationMinutes { get; set; }
 [MaxLength(500),DisplayFormat(ConvertEmptyStringToNull=false)] public string Tags { get; set; }="";
 [MaxLength(25)] public string? TransportMode { get; set; }
 [MaxLength(200)] public string? Origin { get; set; }
 [MaxLength(200)] public string? Destination { get; set; }
 [MaxLength(300)] public string? Contact { get; set; }
 public string Status { get; set; }="Draft";
 public List<DiscoveryFeatureInput> FeatureRows { get; set; }=[];
 public List<DiscoveryPriceInput> Prices { get; set; }=[];
}
public class DiscoveryFeatureInput { public string Code {get;set;}="";public string State {get;set;}="Unknown";[MaxLength(500)]public string? Note {get;set;} }
public class DiscoveryPriceInput
{
 [MaxLength(100),DisplayFormat(ConvertEmptyStringToNull=false)] public string Label { get; set; } = "";
 [Range(0,1000000000)] public decimal? Amount { get; set; }
 [MaxLength(100),Display(Name="Đơn vị giá"),DisplayFormat(ConvertEmptyStringToNull=false)] public string Unit { get; set; } = "";
 [MaxLength(1000),DisplayFormat(ConvertEmptyStringToNull=false)] public string Conditions { get; set; } = "";
 [MaxLength(1000),DisplayFormat(ConvertEmptyStringToNull=false)] public string Included { get; set; } = "";
 [MaxLength(1000),DisplayFormat(ConvertEmptyStringToNull=false)] public string Extra { get; set; } = "";
 public DateOnly? From { get; set; }
 public DateOnly? Until { get; set; }
}
public record DiscoveryPoint(double Lat,double Lng,string? Name=null);
public class DiscoveryRouteInput : DiscoveryCommand {public List<DiscoveryPoint> Points {get;set;}=[];public string Mode {get;set;}="car";[Range(.1,20)]public double CorridorKm {get;set;}=2;public double? MaxDetourMinutes {get;set;}public DiscoveryFilter Filter {get;set;}=new();}
public record DiscoveryCard(DiscoveryEntry Entry, double? DistanceKm=null, double? ExtraMinutes=null);
public record DiscoverySnippet(long EntryId,string Title,string Answer,string Source,string? SourceUrl,DateOnly? CheckedOn);
public record DiscoverySearchPage(DiscoveryFilter Filter,List<DiscoveryCard> Items,int Total,List<string> Regions,List<string> Terms,List<DiscoveryQuestion> Answers,List<DiscoveryEntry> Destinations,bool PriceUnitsMixed=false,string? Intent=null,List<DiscoverySnippet>? Snippets=null,List<DiscoveryEntry>? HelpGuides=null);
public record DiscoveryDetailPage(DiscoveryEntry Entry,List<DiscoveryFeature> Features,List<DiscoveryPrice> Prices,List<DiscoveryCard> Related,List<DiscoveryQuestion> Questions,List<DiscoveryExperience> Experiences,Dictionary<long,string> Names,List<DiscoveryRevision> Updates,bool Following,DiscoveryCommand Input);
public record DiscoveryLibraryPage(List<DiscoveryCollection> Collections,List<DiscoveryCollectionItem> Items,Dictionary<long,DiscoveryEntry> Entries,List<DiscoveryRoute> Routes,List<DiscoveryEntry> Followed,List<DiscoveryQuestion> Questions,List<DiscoveryReport> Reports,DiscoveryCommand Input,List<long>? UnavailableFollowed=null);
public record DiscoveryManagePage(List<DiscoveryEntry> Entries,List<DiscoveryQuestion> Questions,List<DiscoveryExperience> Experiences,List<DiscoveryReport> Reports,List<DiscoveryProposal> Proposals,Dictionary<long,string> Names,Dictionary<long,DiscoveryEntry> References,string Tab,DiscoveryCommand Input);
public record DiscoveryRouteResult(double DistanceKm,double Minutes,List<double[]> Coordinates,List<DiscoveryPoint> Points,List<DiscoveryCard> Nearby,string Provider,string Note);
