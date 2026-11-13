namespace HoTroDuLich.Web.Domain;

public sealed class LichTrinhMau
{
 public long Id { get; set; }
 public long DonViId { get; set; }
 public string Title { get; set; } = "";
 public int Days { get; set; }
 public string Style { get; set; } = "CanBang";
 public string? Summary { get; set; }
 public string Status { get; set; } = "Draft";
 public int Uses { get; set; }
 public DateTime CreatedAt { get; set; }
 public DateTime UpdatedAt { get; set; }
 public long CreatedBy { get; set; }
}
public sealed class LichTrinhMauMuc
{
 public long Id { get; set; }
 public long TemplateId { get; set; }
 public int DayNo { get; set; }
 public int SortOrder { get; set; }
 public TimeOnly? Time { get; set; }
 public string Content { get; set; } = "";
 public decimal? Cost { get; set; }
 public long? ServiceId { get; set; }
 public long? PlaceId { get; set; }
}

/// <summary>Dong danh muc loai dich vu (xem ServiceKinds).</summary>
public sealed class LoaiDichVuRow
{
 public string Code { get; set; } = "";
 public string Label { get; set; } = "";
 public string GroupCode { get; set; } = "Other";
 public int SortOrder { get; set; } = 100;
 public string? Icon { get; set; }
 public bool Bookable { get; set; }
 public bool ProviderPostable { get; set; } = true;
 public bool ForBranch { get; set; }
 public bool HasPrice { get; set; } = true;
 public bool HasHours { get; set; }
 public bool HasLocation { get; set; } = true;
 public bool TimeBound { get; set; }
 public bool Active { get; set; } = true;
 public bool Builtin { get; set; }
 public string? FieldsJson { get; set; }
 public DateTime UpdatedAt { get; set; }
}

/// <summary>Lien ket chuyen di chung voi nhom chat tu dong cua no.</summary>
public sealed class TripChatLink
{
 public long PlanId { get; set; }
 public long ConversationId { get; set; }
 public DateTime CreatedAt { get; set; }
}

/// <summary>Ghi chu cua mot ngay trong chuyen di.</summary>
public sealed class TripDayNote
{
 public long PlanId { get; set; }
 public int DayNo { get; set; }
 public string Note { get; set; } = "";
 public DateTime UpdatedAt { get; set; }
}
