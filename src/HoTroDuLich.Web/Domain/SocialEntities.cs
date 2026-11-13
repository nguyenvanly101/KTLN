namespace HoTroDuLich.Web.Domain;
public sealed class KetBan
{
 public long Id { get; set; }
 public long RequesterId { get; set; }
 public long AddresseeId { get; set; }
 public string Status { get; set; } = "Pending";
 public DateTime CreatedAt { get; set; }
 public DateTime? RespondedAt { get; set; }
}
public sealed class BaiViet
{
 public long Id { get; set; }
 public long AuthorId { get; set; }
 public string Content { get; set; } = "";
 public string Visibility { get; set; } = "Public";
 public string? PhotosJson { get; set; }
 public DateTime CreatedAt { get; set; }
 public DateTime? UpdatedAt { get; set; }
 public DateTime? DeletedAt { get; set; }
 public string Kind { get; set; } = "Share";
 public long? BranchId { get; set; }
 public string? Destination { get; set; }
 public DateOnly? TripStart { get; set; }
 public DateOnly? TripEnd { get; set; }
 public long? ServiceId { get; set; }
 public long? PlaceId { get; set; }
 public long? SharedPostId { get; set; }
}
public sealed class BaoCaoBaiViet
{
 public long Id { get; set; }
 public long ReporterId { get; set; }
 public long PostId { get; set; }
 public long? CommentId { get; set; }
 public string Reason { get; set; } = "";
 public string? Note { get; set; }
 public string Status { get; set; } = "Open";
 public DateTime CreatedAt { get; set; }
 public DateTime? HandledAt { get; set; }
}
public sealed class BinhLuan
{
 public long Id { get; set; }
 public long PostId { get; set; }
 public long AuthorId { get; set; }
 public long? ParentId { get; set; }
 public string Content { get; set; } = "";
 public DateTime CreatedAt { get; set; }
 public DateTime? UpdatedAt { get; set; }
 public DateTime? DeletedAt { get; set; }
}
public sealed class CamXuc
{
 public long Id { get; set; }
 public string TargetType { get; set; } = "";
 public long TargetId { get; set; }
 public long AccountId { get; set; }
 public string Emoji { get; set; } = "like";
 public DateTime CreatedAt { get; set; }
}
public sealed class ThanhVienChat
{
 public long ConversationId { get; set; }
 public long AccountId { get; set; }
 public string Role { get; set; } = "Member";
 public long ReadId { get; set; }
 public DateTime JoinedAt { get; set; }
 public DateTime? LeftAt { get; set; }
}

public sealed class ThanhVienChuyenDi
{
 public long PlanId { get; set; }
 public long AccountId { get; set; }
 public string Status { get; set; } = "Pending";
 public long InvitedBy { get; set; }
 public DateTime CreatedAt { get; set; }
 public DateTime? RespondedAt { get; set; }
}

public sealed class BaiVietDaLuu
{
 public long AccountId { get; set; }
 public long PostId { get; set; }
 public DateTime CreatedAt { get; set; }
}
public sealed class TheoDoiCoSo
{
 public long AccountId { get; set; }
 public long BranchId { get; set; }
 public DateTime CreatedAt { get; set; }
}
