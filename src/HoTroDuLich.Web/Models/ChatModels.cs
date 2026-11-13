using HoTroDuLich.Web.Domain;
using System.ComponentModel.DataAnnotations;
namespace HoTroDuLich.Web.Models;

public sealed class ChatSendInput
{
 public long? ConversationId { get; set; }
 public long? BranchId { get; set; }
 public long? PeerId { get; set; }
 [MaxLength(2000)] public string? Body { get; set; }
 public IFormFile? Image { get; set; }
 public IFormFile? File { get; set; }
 public long? ReplyToId { get; set; }
 public string? Latitude { get; set; }
 public string? Longitude { get; set; }
}
public sealed class ChatSettingsInput
{
 public long BranchId { get; set; }
 public bool Accepting { get; set; } = true;
 [Required] public string ReplyWithin { get; set; } = "Hour";
 [MaxLength(500)] public string? AutoReply { get; set; }
 public bool AutoReplyOffHours { get; set; } = true;
 [MaxLength(300)] public string? Greeting { get; set; }
}
public sealed class QuickReplyInput
{
 public long BranchId { get; set; }
 public long? Id { get; set; }
 [MaxLength(40)] public string Title { get; set; } = "";
 [MaxLength(1000)] public string Body { get; set; } = "";
 public bool Delete { get; set; }
}
public sealed class ChatStateInput
{
 public string? State { get; set; }
 [MaxLength(30)] public string? Label { get; set; }
 [MaxLength(500)] public string? Note { get; set; }
}

public sealed record ChatMessageDto(long Id,string Side,string Kind,string Body,bool Image,DateTime At,long? Booking=null,long? Connection=null,long? SenderId=null,string? Sender=null,bool? Mine=null,long? ReplyTo=null,string? ReplyText=null,string? ReplyAuthor=null,bool Edited=false,bool Deleted=false,decimal? Lat=null,decimal? Lng=null,string? FileName=null,bool File=false,List<ChatReactionDto>? Reactions=null,long? Agreement=null);
public sealed record ChatItemDto(long Id,long BranchId,string BranchName,string Who,string? Preview,bool LastFromMe,DateTime At,int Unread,string State,string? Label,bool Group=false,int Members=0,int Pending=0);
public sealed record ChatThreadDto(ChatItemDto Item,List<ChatMessageDto> Messages,List<ChatBookingCard> Cards,List<ChatConnectionCard> Connections,long OtherReadId,string Me,bool OtherOnline,string Eta,string? Actual,bool CanSend,string? Blocked,string? Note,string BranchType,string? Phone,bool BlockedByMe=false,long? PeerId=null,List<ChatMemberDto>? Members=null,string? MyRole=null,ChatBranchCard? Context=null,List<ChatAgreementCard>? Agreements=null);
public sealed record ChatStartDto(long BranchId,string BranchName,string BranchType,long? ConversationId,string Eta,string? Actual,string? Greeting,bool Accepting,bool OwnBranch,long? PeerId=null,string? PeerName=null);
public sealed record ChatSettingsDto(long BranchId,string BranchName,bool Accepting,string ReplyWithin,string? AutoReply,bool AutoReplyOffHours,string? Greeting,List<TinMauDto> Quick,int Requests=0,int Confirmed=0,string? Actual=null);
public sealed record TinMauDto(long Id,string Title,string Body);
public sealed record ChatPageModel(string Me,long? OpenId,ChatStartDto? Start,List<(long Id,string Name)> Branches,long? BranchFilter);

public sealed class ChatBookingInput
{
 public long? ConversationId { get; set; }
 public long? BranchId { get; set; }
 public long ServiceId { get; set; }
 public DateOnly Start { get; set; }
 public DateOnly? End { get; set; }
 [Range(1,100)] public int People { get; set; }=1;
 [Range(1,100)] public int Quantity { get; set; }=1;
 [MaxLength(1000)] public string? Notes { get; set; }
 [MaxLength(100)] public string? ArrivalDeparture { get; set; }
 public Guid SubmissionId { get; set; }
}
public sealed class ChatBookingAction
{
 public long RequestId { get; set; }
 [Required,MaxLength(40)] public string Decision { get; set; }="";
 [MaxLength(500)] public string? Reason { get; set; }
 [MaxLength(2000)] public string? Text { get; set; }
 public DateOnly? Start { get; set; }
 public DateOnly? End { get; set; }
 [Range(1,100)] public int? People { get; set; }
 [Range(1,100)] public int? Quantity { get; set; }
 [Range(0,1000000000)] public decimal? Cost { get; set; }
}
public sealed record ChatServiceDto(long Id,string Name,string Type,decimal? Price,string? Unit);
public sealed record ChatBookingCard(long Id,long ServiceId,string Service,string Status,string StatusLabel,DateOnly Start,DateOnly? End,int People,int Quantity,decimal? Cost,string? Notes,string? ArrivalDeparture,string? Terms,string? Reason,List<string> Actions,decimal? Deposit=null,string? DepositMethod=null,string? DepositStatus=null,string? DepositNote=null,bool CanReview=false,long? PlanId=null);

public sealed class ChatConnectInput
{
 public long? ConversationId { get; set; }
 public long? PeerId { get; set; }
 public long NeedId { get; set; }
 [MaxLength(1000)] public string? Text { get; set; }
}
public sealed class ChatConnectAction
{
 public long ConnectionId { get; set; }
 [Required,MaxLength(40)] public string Decision { get; set; }="";
}
public sealed record ChatNeedDto(long Id,string Destination,DateOnly Start,DateOnly End,string? Mode);
public sealed record ChatConnectionCard(long Id,long NeedId,string Destination,DateOnly Start,DateOnly End,string? Intro,string Status,string StatusLabel,List<string> Actions,string Link,bool Mine,long OtherId=0);

public sealed record ChatMemberDto(long Id,string Name,string Role,bool Me);
public sealed class ChatGroupInput
{
 [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? Title { get; set; }
 public List<long> MemberIds { get; set; }=[];
}
public sealed class ChatGroupMembersInput
{
 public List<long> AccountIds { get; set; }=[];
}

public sealed record ChatBranchCard(long Id,string Name,string Type,string Address,string? Hours,string? Price,string? Photo,string Url,string? Phone);
public sealed record ChatBookingCheck(bool Ok,string? Message,List<string> Warnings,decimal? Price,string? Unit,decimal? Estimate,string? Note);

public sealed record ChatFindBranch(long Id,string Name,string Type,string Address);
public sealed record ChatFindResult(List<PersonRef> People,List<ChatFindBranch> Branches);
public sealed record PersonRef(long Id,string Name,string Relation);

public sealed record ChatReactionDto(string Emoji,int Count,bool Mine);

public sealed class ChatDepositInput
{
 public long RequestId { get; set; }
 [System.ComponentModel.DataAnnotations.Range(1,1000000000)] public decimal Amount { get; set; }
 [System.ComponentModel.DataAnnotations.MaxLength(40)] public string Method { get; set; }="";
 [System.ComponentModel.DataAnnotations.MaxLength(300)] public string? Note { get; set; }
}
public sealed record ChatPlanDto(long Id,string Name,DateOnly Start,DateOnly End);

public sealed class ChatAgreementInput
{
 public long ConversationId { get; set; }
 public long ConnectionId { get; set; }
 [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? Destination { get; set; }
 public DateOnly Start { get; set; }
 public DateOnly End { get; set; }
 [System.ComponentModel.DataAnnotations.Range(0,1000000000)] public decimal? Budget { get; set; }
 [System.ComponentModel.DataAnnotations.MaxLength(1000)] public string? Scope { get; set; }
}
public sealed class ChatAgreementAction
{
 public long AgreementId { get; set; }
 public string Decision { get; set; }="";
 [System.ComponentModel.DataAnnotations.MaxLength(500)] public string? Reason { get; set; }
}
public sealed class ChatReportInput
{
 public string Reason { get; set; }="";
 [System.ComponentModel.DataAnnotations.MaxLength(500)] public string? Note { get; set; }
 public long? MessageId { get; set; }
}
public sealed record ChatAgreementCard(long Id,long ConnectionId,int Version,string Destination,DateOnly Start,DateOnly End,decimal? Budget,string? Scope,string Status,string StatusLabel,bool Mine,List<string> Actions,string Link,string? Reason);
public sealed record ChatReportDto(long Id,long ReporterId,string Reporter,long? TargetId,string? Target,long ConversationId,string Reason,string? Note,DateTime At,List<string> Context);
