namespace HoTroDuLich.Web.Domain;

public sealed class DiscoveryEntry
{
    public long Id { get; set; }
    public string Slug { get; set; } = "";
    public string Kind { get; set; } = "";
    public long? ParentId { get; set; }
    public long? PlaceId { get; set; }
    public long? ServiceId { get; set; }
    public string Title { get; set; } = "";
    public string Region { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationNote { get; set; }
    public string SourceName { get; set; } = "";
    public string? SourceStamp { get; set; }
    public string? SourceUrl { get; set; }
    public DateOnly? CheckedOn { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public decimal? PriceFrom { get; set; }
    public decimal? PriceTo { get; set; }
    public string? PriceUnit { get; set; }
    public string? Includes { get; set; }
    public string? Excludes { get; set; }
    public string? Hours { get; set; }
    public string? OpenDays { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string Tags { get; set; } = "";
    public string? TransportMode { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public string? Contact { get; set; }
    public string Status { get; set; } = "";
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DiscoveryFeature
{
    public long EntryId { get; set; }
    public string Code { get; set; } = "";
    public string State { get; set; } = "";
    public string? Note { get; set; }
}

public sealed class DiscoveryPrice
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public string Label { get; set; } = "";
    public decimal? Amount { get; set; }
    public string Unit { get; set; } = "";
    public string Conditions { get; set; } = "";
    public string Included { get; set; } = "";
    public string Extra { get; set; } = "";
    public bool Active { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? Until { get; set; }
}

public sealed class DiscoveryQuestion
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public long OwnerId { get; set; }
    public string Question { get; set; } = "";
    public string? Answer { get; set; }
    public string? AnswerSource { get; set; }
    public long? AnsweredBy { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public string Status { get; set; } = "";
    public Guid SubmissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DiscoveryExperience
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public long OwnerId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Topics { get; set; } = "";
    public DateOnly VisitedOn { get; set; }
    public int Rating { get; set; }
    public string Status { get; set; } = "";
    public long? ModeratedBy { get; set; }
    public string? ModerationNote { get; set; }
    public Guid SubmissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DiscoveryReport
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public long OwnerId { get; set; }
    public string Category { get; set; } = "";
    public string Body { get; set; } = "";
    public string? EvidenceUrl { get; set; }
    public string Status { get; set; } = "";
    public string? Response { get; set; }
    public long? HandledBy { get; set; }
    public Guid SubmissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DiscoveryFollow
{
    public long EntryId { get; set; }
    public long OwnerId { get; set; }
    public bool Active { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DiscoveryRevision
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public long? ActorId { get; set; }
    public string BeforeJson { get; set; } = "";
    public string AfterJson { get; set; } = "";
    public string Summary { get; set; } = "";
    public bool Public { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class DiscoveryProposal
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public long OwnerId { get; set; }
    public string BaseVersion { get; set; } = "";
    public string Payload { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public long? ReviewedBy { get; set; }
    public Guid SubmissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DiscoveryCollection
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string? Note { get; set; }
    public bool Archived { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DiscoveryCollectionItem
{
    public long CollectionId { get; set; }
    public long EntryId { get; set; }
    public string? Note { get; set; }
    public bool Removed { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DiscoveryRoute
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string PointsJson { get; set; } = "";
    public string Mode { get; set; } = "";
    public bool Archived { get; set; }
    public Guid SubmissionId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

