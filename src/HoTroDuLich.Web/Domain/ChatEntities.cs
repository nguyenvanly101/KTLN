namespace HoTroDuLich.Web.Domain;
/// <summary>Mot cuoc tro chuyen giua mot khach va mot co so (hop thu chung cua co so).</summary>
public sealed class CuocTroChuyen
{
 public long Id { get; set; }
 public long? BranchId { get; set; }
 public long TravelerId { get; set; }
 public long? PeerId { get; set; }
 public long? BlockedById { get; set; }
 public bool IsGroup { get; set; }
 public string? Title { get; set; }
 public string State { get; set; } = "Open";
 public string? Label { get; set; }
 public string? InternalNote { get; set; }
 public long LastMessageId { get; set; }
 public DateTime LastMessageAt { get; set; }
 public string? LastPreview { get; set; }
 public bool LastFromTraveler { get; set; } = true;
 public long TravelerReadId { get; set; }
 public long BranchReadId { get; set; }
 public DateTime? TravelerReadAt { get; set; }
 public DateTime? BranchReadAt { get; set; }
 public DateTime CreatedAt { get; set; }
}
public sealed class TinNhanChat
{
 public long Id { get; set; }
 public long ConversationId { get; set; }
 public long? SenderId { get; set; }
 public string Side { get; set; } = "";
 public string Kind { get; set; } = "Text";
 public string Body { get; set; } = "";
 public string? ImagePath { get; set; }
 public long? BookingRequestId { get; set; }
 public long? ConnectionId { get; set; }
 public long? AgreementId { get; set; }
 public long? ReplyToId { get; set; }
 public DateTime? EditedAt { get; set; }
 public DateTime? DeletedAt { get; set; }
 public decimal? Lat { get; set; }
 public decimal? Lng { get; set; }
 public string? FileName { get; set; }
 public string? FilePath { get; set; }
 public DateTime CreatedAt { get; set; }
}
public sealed class TinMauChat
{
 public long Id { get; set; }
 public long BranchId { get; set; }
 public string Title { get; set; } = "";
 public string Body { get; set; } = "";
 public DateTime CreatedAt { get; set; }
}
public sealed class CaiDatChatCoSo
{
 public long BranchId { get; set; }
 public bool Accepting { get; set; } = true;
 public string ReplyWithin { get; set; } = "Hour";
 public string? AutoReply { get; set; }
 public bool AutoReplyOffHours { get; set; } = true;
 public string? Greeting { get; set; }
 public DateTime UpdatedAt { get; set; }
}

public sealed class PhanUngTin
{
 public long MessageId { get; set; }
 public long AccountId { get; set; }
 public string Emoji { get; set; } = "like";
 public DateTime CreatedAt { get; set; }
}

public sealed class DatCocChat
{
 public long RequestId { get; set; }
 public decimal Amount { get; set; }
 public string Method { get; set; } = "";
 public string? Note { get; set; }
 public string Status { get; set; } = "Requested";
 public DateTime RequestedAt { get; set; }
 public DateTime? PaidAt { get; set; }
}
public sealed class NhacLichChat
{
 public long RequestId { get; set; }
 public string Kind { get; set; } = "";
 public DateTime SentAt { get; set; }
}

public sealed class BoQuaGoiY
{
 public long AccountId { get; set; }
 public long NeedId { get; set; }
 public DateTime CreatedAt { get; set; }
}
public sealed class BaoCaoChat
{
 public long Id { get; set; }
 public long ReporterId { get; set; }
 public long? TargetId { get; set; }
 public long ConversationId { get; set; }
 public long? MessageId { get; set; }
 public string Reason { get; set; } = "";
 public string? Note { get; set; }
 public string Status { get; set; } = "Open";
 public DateTime CreatedAt { get; set; }
 public DateTime? HandledAt { get; set; }
}
