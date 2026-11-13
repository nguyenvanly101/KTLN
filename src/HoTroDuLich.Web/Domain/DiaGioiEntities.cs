// Goi 1 - dia gioi hanh chinh. Cot khop Database/008_DiaGioiHanhChinh.sql.
namespace HoTroDuLich.Web.Domain;

/// <summary>Regime: "Cu" = 63 tinh -> huyen -> xa; "Moi" = 34 tinh -> xa (tu 01/07/2025). Level 1 tinh, 2 huyen, 3 xa.</summary>
public sealed class DonViHanhChinh
{
    public long Id { get; set; }
    public string Regime { get; set; } = "";
    public string Code { get; set; } = "";
    public long? ParentId { get; set; }
    public byte Level { get; set; }
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public string NameFolded { get; set; } = "";
    public string Slug { get; set; } = "";
    public decimal? CenterLat { get; set; }
    public decimal? CenterLng { get; set; }
    public string Status { get; set; } = "Active";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string SourceName { get; set; } = "";
    public DateOnly CheckedOn { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DonViChuyenDoi
{
    public long OldUnitId { get; set; }
    public long NewUnitId { get; set; }
    public string Note { get; set; } = "";
}

public sealed class TenGoiKhacDonVi
{
    public long Id { get; set; }
    public long UnitId { get; set; }
    public string Alias { get; set; } = "";
    public string NameFolded { get; set; } = "";
    public string AliasKind { get; set; } = "";
    public string? Note { get; set; }
}

public sealed class KhuDuLich
{
    public long Id { get; set; }
    public long ProvinceId { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameFolded { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? Summary { get; set; }
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    public int? RadiusM { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public string SourceName { get; set; } = "";
    public DateOnly CheckedOn { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class KhuDuLichDonVi
{
    public long ZoneId { get; set; }
    public long UnitId { get; set; }
}

public sealed class ChuyenDoiDiaGioiCho
{
    public long Id { get; set; }
    public string TableName { get; set; } = "";
    public long RecordId { get; set; }
    public string OldText { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public long? ResolvedUnitId { get; set; }
    public long? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
