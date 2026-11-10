using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;

public sealed class DiscoveryEntryConfiguration : IEntityTypeConfiguration<DiscoveryEntry>
{
 public void Configure(EntityTypeBuilder<DiscoveryEntry> e){
 e.ToTable("DiscoveryEntry",t=>{t.HasCheckConstraint("CK_DS_Kind","Kind IN ('Destination','Area','Place','Service','Guide','Transport','Event','Amenity')");t.HasCheckConstraint("CK_DS_Link","(PlaceId IS NULL OR ServiceId IS NULL) AND (PlaceId IS NULL OR Kind='Place') AND (ServiceId IS NULL OR Kind='Service')");t.HasCheckConstraint("CK_DS_Coord","(Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL AND Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)");t.HasCheckConstraint("CK_DS_Price","(PriceFrom IS NULL OR PriceFrom>=0) AND (PriceTo IS NULL OR PriceTo>=0) AND (PriceFrom IS NULL OR PriceTo IS NULL OR PriceTo>=PriceFrom)");t.HasCheckConstraint("CK_DS_Dates","ValidFrom IS NULL OR ValidTo IS NULL OR ValidTo>=ValidFrom");t.HasCheckConstraint("CK_DS_Duration","DurationMinutes IS NULL OR DurationMinutes BETWEEN 1 AND 1440");t.HasCheckConstraint("CK_DS_Status","Status IN ('Draft','Published','Hidden')");t.HasCheckConstraint("CK_DS_Title","LEN(LTRIM(RTRIM(Title)))>0");t.HasCheckConstraint("CK_DS_Self","ParentId IS NULL OR ParentId<>Id");t.HasCheckConstraint("CK_DS_Hours","(OpenTime IS NULL AND CloseTime IS NULL AND OpenDays IS NULL) OR (OpenTime IS NOT NULL AND CloseTime IS NOT NULL AND OpenDays IS NOT NULL AND LEN(OpenDays)>0 AND OpenDays NOT LIKE '%[^1234567]%')");});
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.Slug).HasColumnType("varchar(160)");
 e.Property(x=>x.Kind).HasColumnType("varchar(25)");
 e.Property(x=>x.ParentId).HasColumnType("bigint");
 e.Property(x=>x.PlaceId).HasColumnType("bigint");
 e.Property(x=>x.ServiceId).HasColumnType("bigint");
 e.Property(x=>x.Title).HasColumnType("nvarchar(200)");
 e.Property(x=>x.Region).HasColumnType("nvarchar(100)");
 e.Property(x=>x.Summary).HasColumnType("nvarchar(700)");
 e.Property(x=>x.Body).HasColumnType("nvarchar(max)");
 e.Property(x=>x.Address).HasColumnType("nvarchar(400)");
 e.Property(x=>x.Latitude).HasColumnType("decimal(9,6)");
 e.Property(x=>x.Longitude).HasColumnType("decimal(9,6)");
 e.Property(x=>x.LocationNote).HasColumnType("nvarchar(300)");
 e.Property(x=>x.SourceName).HasColumnType("nvarchar(200)");
 e.Property(x=>x.SourceStamp).HasColumnType("varchar(64)");
 e.Property(x=>x.SourceUrl).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.CheckedOn).HasColumnType("date");
 e.Property(x=>x.ValidFrom).HasColumnType("date");
 e.Property(x=>x.ValidTo).HasColumnType("date");
 e.Property(x=>x.PriceFrom).HasColumnType("decimal(18,2)");
 e.Property(x=>x.PriceTo).HasColumnType("decimal(18,2)");
 e.Property(x=>x.PriceUnit).HasColumnType("nvarchar(100)");
 e.Property(x=>x.Includes).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.Excludes).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.Hours).HasColumnType("nvarchar(700)");
 e.Property(x=>x.OpenDays).HasColumnType("varchar(7)");
 e.Property(x=>x.OpenTime).HasColumnType("time(0)");
 e.Property(x=>x.CloseTime).HasColumnType("time(0)");
 e.Property(x=>x.DurationMinutes).HasColumnType("int");
 e.Property(x=>x.Tags).HasColumnType("nvarchar(500)");
 e.Property(x=>x.TransportMode).HasColumnType("varchar(25)");
 e.Property(x=>x.Origin).HasColumnType("nvarchar(200)");
 e.Property(x=>x.Destination).HasColumnType("nvarchar(200)");
 e.Property(x=>x.Contact).HasColumnType("nvarchar(300)");
 e.Property(x=>x.Status).HasColumnType("varchar(20)");
 e.Property(x=>x.CreatedBy).HasColumnType("bigint");
 e.Property(x=>x.UpdatedBy).HasColumnType("bigint");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.ParentId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<DiaDiemDuLich>().WithMany().HasForeignKey(x=>x.PlaceId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<DichVuDuLich>().WithMany().HasForeignKey(x=>x.ServiceId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.UpdatedBy).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>x.Slug,"UX_DS_Slug").IsUnique();
 e.HasIndex(x=>x.PlaceId,"UX_DS_Place").IsUnique().HasFilter("PlaceId IS NOT NULL");
 e.HasIndex(x=>x.ServiceId,"UX_DS_Service").IsUnique().HasFilter("ServiceId IS NOT NULL");
 e.HasIndex(x=>new{x.Status,x.Kind,x.Region},"IX_DS_Search");
 e.HasIndex(x=>new{x.ParentId,x.Status},"IX_DS_Parent");
 e.HasIndex(x=>new{x.ValidFrom,x.ValidTo},"IX_DS_Dates");
 }
}

public sealed class DiscoveryFeatureConfiguration : IEntityTypeConfiguration<DiscoveryFeature>
{
 public void Configure(EntityTypeBuilder<DiscoveryFeature> e){
 e.ToTable("DiscoveryFeature",t=>{t.HasCheckConstraint("CK_DSF_State","State IN ('Yes','No','Unknown')");t.HasCheckConstraint("CK_DSF_Code","Code IN ('AccessibleEntrance','AccessibleRestroom','Parking','Seating','Children','Indoor','PublicTransport','BookingRequired','Luggage','Charging','FreeEntry','Vegetarian')");});
 e.HasKey(x=>new{x.EntryId,x.Code});
 e.Property(x=>x.EntryId).HasColumnType("bigint").ValueGeneratedNever();
 e.Property(x=>x.Code).HasColumnType("varchar(40)").ValueGeneratedNever();
 e.Property(x=>x.State).HasColumnType("varchar(12)");
 e.Property(x=>x.Note).HasColumnType("nvarchar(500)");
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 }
}

public sealed class DiscoveryPriceConfiguration : IEntityTypeConfiguration<DiscoveryPrice>
{
 public void Configure(EntityTypeBuilder<DiscoveryPrice> e){
 e.ToTable("DiscoveryPrice",t=>{t.HasCheckConstraint("CK_DSP_Amount","Amount IS NULL OR Amount BETWEEN 0 AND 1000000000");t.HasCheckConstraint("CK_DSP_Dates","[From] IS NULL OR Until IS NULL OR Until>=[From]");});
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.EntryId).HasColumnType("bigint");
 e.Property(x=>x.Label).HasColumnType("nvarchar(100)");
 e.Property(x=>x.Amount).HasColumnType("decimal(18,2)");
 e.Property(x=>x.Unit).HasColumnType("nvarchar(100)");
 e.Property(x=>x.Conditions).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.Included).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.Extra).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.Active).HasColumnType("bit");
 e.Property(x=>x.From).HasColumnType("date");
 e.Property(x=>x.Until).HasColumnType("date");
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>x.EntryId,"IX_DSP_Entry");
 }
}

public sealed class DiscoveryQuestionConfiguration : IEntityTypeConfiguration<DiscoveryQuestion>
{
 public void Configure(EntityTypeBuilder<DiscoveryQuestion> e){
 e.ToTable("DiscoveryQuestion",t=>{t.HasCheckConstraint("CK_DSQ_Status","Status IN ('Pending','Answered','Hidden')");t.HasCheckConstraint("CK_DSQ_Answer","Status<>'Answered' OR (Answer IS NOT NULL AND LEN(Answer)>0 AND AnsweredBy IS NOT NULL AND AnsweredAt IS NOT NULL)");});
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.EntryId).HasColumnType("bigint");
 e.Property(x=>x.OwnerId).HasColumnType("bigint");
 e.Property(x=>x.Question).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.Answer).HasColumnType("nvarchar(4000)");
 e.Property(x=>x.AnswerSource).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.AnsweredBy).HasColumnType("bigint");
 e.Property(x=>x.AnsweredAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.Status).HasColumnType("varchar(20)");
 e.Property(x=>x.SubmissionId).HasColumnType("uniqueidentifier");
 e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.OwnerId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.AnsweredBy).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>new{x.EntryId,x.Status},"IX_DSQ_Entry");
 e.HasIndex(x=>new{x.OwnerId,x.SubmissionId},"UX_DSQ_Send").IsUnique();
 }
}

public sealed class DiscoveryExperienceConfiguration : IEntityTypeConfiguration<DiscoveryExperience>
{
 public void Configure(EntityTypeBuilder<DiscoveryExperience> e){
 e.ToTable("DiscoveryExperience",t=>{t.HasCheckConstraint("CK_DSE_Status","Status IN ('Pending','Published','Hidden')");t.HasCheckConstraint("CK_DSE_Rating","Rating BETWEEN 1 AND 5");});
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.EntryId).HasColumnType("bigint");
 e.Property(x=>x.OwnerId).HasColumnType("bigint");
 e.Property(x=>x.Title).HasColumnType("nvarchar(150)");
 e.Property(x=>x.Body).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.Topics).HasColumnType("varchar(200)");
 e.Property(x=>x.VisitedOn).HasColumnType("date");
 e.Property(x=>x.Rating).HasColumnType("int");
 e.Property(x=>x.Status).HasColumnType("varchar(20)");
 e.Property(x=>x.ModeratedBy).HasColumnType("bigint");
 e.Property(x=>x.ModerationNote).HasColumnType("nvarchar(500)");
 e.Property(x=>x.SubmissionId).HasColumnType("uniqueidentifier");
 e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.OwnerId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.ModeratedBy).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>new{x.EntryId,x.Status,x.VisitedOn},"IX_DSE_Entry");
 e.HasIndex(x=>new{x.OwnerId,x.SubmissionId},"UX_DSE_Send").IsUnique();
 }
}

public sealed class DiscoveryReportConfiguration : IEntityTypeConfiguration<DiscoveryReport>
{
 public void Configure(EntityTypeBuilder<DiscoveryReport> e){
 e.ToTable("DiscoveryReport",t=>{t.HasCheckConstraint("CK_DSR_Status","Status IN ('Pending','Checking','Resolved','Rejected')");t.HasCheckConstraint("CK_DSR_Category","Category IN ('Price','Hours','Address','Accessibility','Closed','Content','Other')");t.HasCheckConstraint("CK_DSR_Response","Status NOT IN ('Resolved','Rejected') OR (Response IS NOT NULL AND HandledBy IS NOT NULL)");});
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.EntryId).HasColumnType("bigint");
 e.Property(x=>x.OwnerId).HasColumnType("bigint");
 e.Property(x=>x.Category).HasColumnType("varchar(30)");
 e.Property(x=>x.Body).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.EvidenceUrl).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.Status).HasColumnType("varchar(20)");
 e.Property(x=>x.Response).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.HandledBy).HasColumnType("bigint");
 e.Property(x=>x.SubmissionId).HasColumnType("uniqueidentifier");
 e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.OwnerId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.HandledBy).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>new{x.Status,x.CreatedAt},"IX_DSR_Status");
 e.HasIndex(x=>new{x.OwnerId,x.SubmissionId},"UX_DSR_Send").IsUnique();
 }
}

public sealed class DiscoveryFollowConfiguration : IEntityTypeConfiguration<DiscoveryFollow>
{
 public void Configure(EntityTypeBuilder<DiscoveryFollow> e){
 e.ToTable("DiscoveryFollow");
 e.HasKey(x=>new{x.EntryId,x.OwnerId});
 e.Property(x=>x.EntryId).HasColumnType("bigint").ValueGeneratedNever();
 e.Property(x=>x.OwnerId).HasColumnType("bigint").ValueGeneratedNever();
 e.Property(x=>x.Active).HasColumnType("bit");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.OwnerId).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>new{x.OwnerId,x.Active},"IX_DSF_Owner");
 }
}

public sealed class DiscoveryRevisionConfiguration : IEntityTypeConfiguration<DiscoveryRevision>
{
 public void Configure(EntityTypeBuilder<DiscoveryRevision> e){
 e.ToTable("DiscoveryRevision");
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.EntryId).HasColumnType("bigint");
 e.Property(x=>x.ActorId).HasColumnType("bigint");
 e.Property(x=>x.BeforeJson).HasColumnType("nvarchar(max)");
 e.Property(x=>x.AfterJson).HasColumnType("nvarchar(max)");
 e.Property(x=>x.Summary).HasColumnType("nvarchar(500)");
 e.Property(x=>x.Public).HasColumnType("bit");
 e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)");
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.ActorId).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>new{x.EntryId,x.Id},"IX_DSRevision_Entry");
 }
}

public sealed class DiscoveryProposalConfiguration : IEntityTypeConfiguration<DiscoveryProposal>
{
 public void Configure(EntityTypeBuilder<DiscoveryProposal> e){
 e.ToTable("DiscoveryProposal",t=>{t.HasCheckConstraint("CK_DSProposal_Status","Status IN ('Pending','Accepted','Rejected')");});
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.EntryId).HasColumnType("bigint");
 e.Property(x=>x.OwnerId).HasColumnType("bigint");
 e.Property(x=>x.BaseVersion).HasColumnType("varchar(20)");
 e.Property(x=>x.Payload).HasColumnType("nvarchar(max)");
 e.Property(x=>x.Status).HasColumnType("varchar(20)");
 e.Property(x=>x.Reason).HasColumnType("nvarchar(500)");
 e.Property(x=>x.ReviewedBy).HasColumnType("bigint");
 e.Property(x=>x.SubmissionId).HasColumnType("uniqueidentifier");
 e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.OwnerId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.ReviewedBy).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>new{x.Status,x.CreatedAt},"IX_DSProposal_Status");
 e.HasIndex(x=>new{x.OwnerId,x.SubmissionId},"UX_DSProposal_Send").IsUnique();
 }
}

public sealed class DiscoveryCollectionConfiguration : IEntityTypeConfiguration<DiscoveryCollection>
{
 public void Configure(EntityTypeBuilder<DiscoveryCollection> e){
 e.ToTable("DiscoveryCollection");
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.OwnerId).HasColumnType("bigint");
 e.Property(x=>x.Name).HasColumnType("nvarchar(100)");
 e.Property(x=>x.Note).HasColumnType("nvarchar(500)");
 e.Property(x=>x.Archived).HasColumnType("bit");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.OwnerId).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>new{x.OwnerId,x.Archived},"IX_DSCollection_Owner");
 }
}

public sealed class DiscoveryCollectionItemConfiguration : IEntityTypeConfiguration<DiscoveryCollectionItem>
{
 public void Configure(EntityTypeBuilder<DiscoveryCollectionItem> e){
 e.ToTable("DiscoveryCollectionItem");
 e.HasKey(x=>new{x.CollectionId,x.EntryId});
 e.Property(x=>x.CollectionId).HasColumnType("bigint").ValueGeneratedNever();
 e.Property(x=>x.EntryId).HasColumnType("bigint").ValueGeneratedNever();
 e.Property(x=>x.Note).HasColumnType("nvarchar(500)");
 e.Property(x=>x.Removed).HasColumnType("bit");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<DiscoveryCollection>().WithMany().HasForeignKey(x=>x.CollectionId).OnDelete(DeleteBehavior.NoAction);
 e.HasOne<DiscoveryEntry>().WithMany().HasForeignKey(x=>x.EntryId).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>x.EntryId,"IX_DSCollectionItem_Entry");
 }
}

public sealed class DiscoveryRouteConfiguration : IEntityTypeConfiguration<DiscoveryRoute>
{
 public void Configure(EntityTypeBuilder<DiscoveryRoute> e){
 e.ToTable("DiscoveryRoute",t=>{t.HasCheckConstraint("CK_DSRoute_Mode","Mode IN ('car','foot','bike')");});
 e.HasKey(x=>x.Id);
 e.Property(x=>x.Id).HasColumnType("bigint").UseIdentityColumn();
 e.Property(x=>x.OwnerId).HasColumnType("bigint");
 e.Property(x=>x.Name).HasColumnType("nvarchar(100)");
 e.Property(x=>x.PointsJson).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.Mode).HasColumnType("varchar(12)");
 e.Property(x=>x.Archived).HasColumnType("bit");
 e.Property(x=>x.SubmissionId).HasColumnType("uniqueidentifier");
 e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");
 e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
 e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.OwnerId).OnDelete(DeleteBehavior.NoAction);
 e.HasIndex(x=>new{x.OwnerId,x.Archived},"IX_DSRoute_Owner");
 e.HasIndex(x=>new{x.OwnerId,x.SubmissionId},"UX_DSRoute_Send").IsUnique();
 }
}

