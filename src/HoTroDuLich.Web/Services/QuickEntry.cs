using System.Globalization;
using System.Text.RegularExpressions;

namespace HoTroDuLich.Web.Services;

public sealed record QuickEntryResult(TimeOnly? Time,string Content,decimal? Cost);

/// <summary>Phan tich cau nhap nhanh kieu tu nhien: "9h ca phe muoi 35k", "an trua bun cha 60.000", "14:30 tham quan chua", "toi nhau 1tr2".</summary>
public static class QuickEntry
{
 static readonly Regex TimeRx=new(@"(?<![\d.,])(?<h>[01]?\d|2[0-3])\s*(?:h|giờ|g|:)\s*(?<m>[0-5]\d)?(?!\d)",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
 static readonly Regex CostMixedRx=new(@"(?<![\d.,])(?<a>\d+)\s*(?:tr|triệu|m)\s*(?<b>\d{1,3})?(?!\d)",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
 static readonly Regex CostKRx=new(@"(?<![\d.,])(?<n>\d+(?:[.,]\d+)?)\s*(?:k|nghìn|ngàn|ng)(?![\p{L}\d])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
 static readonly Regex CostPlainRx=new(@"(?<![\d.,:])(?<n>\d{1,3}(?:[.,]\d{3})+|\d{4,9})\s*(?:₫|đ|vnđ|vnd)?(?![\d:])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
 static readonly (string Word,int Hour)[] Slots=[("sáng",8),("trưa",12),("chiều",15),("tối",19)];

 public static TimeOnly? SlotTime(string? slot)=>slot?.Trim().ToLowerInvariant() switch{"sang" or "sáng"=>new(8,0),"trua" or "trưa"=>new(12,0),"chieu" or "chiều"=>new(15,0),"toi" or "tối"=>new(19,0),_=>null};

 public static QuickEntryResult Parse(string? input,string? defaultSlot=null)
 {
  var text=(input??"").Trim();
  if(text.Length==0)throw new BusinessException("Nhập nội dung hạng mục.",422);
  if(text.Length>400)throw new BusinessException("Nội dung quá dài (tối đa 400 ký tự).",422);
  TimeOnly? time=null;decimal? cost=null;

  // 1. Chi phi truoc (de khong nham "35k" hay "1tr2" voi gio)
  var mixed=CostMixedRx.Match(text);
  if(mixed.Success)
  {
   var millions=decimal.Parse(mixed.Groups["a"].Value,CultureInfo.InvariantCulture);
   decimal rest=0;
   if(mixed.Groups["b"].Success)
   {
    var b=mixed.Groups["b"].Value;
    rest=b.Length switch{1=>decimal.Parse(b,CultureInfo.InvariantCulture)*100_000,2=>decimal.Parse(b,CultureInfo.InvariantCulture)*10_000,_=>decimal.Parse(b,CultureInfo.InvariantCulture)*1_000};
   }
   cost=millions*1_000_000+rest;text=text.Remove(mixed.Index,mixed.Length).Insert(mixed.Index," ");
  }
  if(cost==null)
  {
   var k=CostKRx.Match(text);
   if(k.Success&&decimal.TryParse(k.Groups["n"].Value.Replace(',','.'),NumberStyles.Number,CultureInfo.InvariantCulture,out var kv)){cost=Math.Round(kv*1000);text=text.Remove(k.Index,k.Length).Insert(k.Index," ");}
  }
  if(cost==null)
  {
   var plain=CostPlainRx.Match(text);
   if(plain.Success)
   {
    var digits=Regex.Replace(plain.Groups["n"].Value,@"[.,]","");
    if(decimal.TryParse(digits,NumberStyles.None,CultureInfo.InvariantCulture,out var pv)&&pv>=1000){cost=pv;text=text.Remove(plain.Index,plain.Length).Insert(plain.Index," ");}
   }
  }
  // 2. Gio
  var t=TimeRx.Match(text);
  if(t.Success)
  {
   var h=int.Parse(t.Groups["h"].Value,CultureInfo.InvariantCulture);var m=t.Groups["m"].Success?int.Parse(t.Groups["m"].Value,CultureInfo.InvariantCulture):0;
   // "tối 7h", "chieu 3h": gio nho kem tu chi buoi -> doi sang 24 gio
   var lower=text.ToLowerInvariant();
   if(h<12&&(lower.Contains("chiều")||lower.Contains("tối")))h+=12;
   time=new TimeOnly(h,m);text=text.Remove(t.Index,t.Length).Insert(t.Index," ");
  }
  if(time==null)
  {
   var lower=text.ToLowerInvariant();
   foreach(var (word,hour) in Slots)if(Regex.IsMatch(lower,@"(?<!\p{L})"+word+@"(?!\p{L})")){time=new TimeOnly(hour,0);break;}
  }
  time??=SlotTime(defaultSlot);
  // 3. Noi dung: bo ky tu thua
  var content=Regex.Replace(text,@"\s+"," ").Trim(' ','-','–',',',';',':','·','|');
  if(content.Length==0)throw new BusinessException("Nhập tên hạng mục (ví dụ: 9h cà phê muối 35k).",422);
  if(content.Length>300)content=content[..300];
  if(cost>1_000_000_000)throw new BusinessException("Chi phí quá lớn.",422);
  return new(time,content,cost);
 }

 /// <summary>So tien nhap nhanh o o ngan sach: "3tr", "500k", "2.500.000", "2tr5".</summary>
 public static decimal? ParseMoney(string? input)
 {
  var s=(input??"").Trim();if(s.Length==0)return null;
  var r=Parse("x "+s);
  if(r.Cost==null)throw new BusinessException("Số tiền chưa hiểu. Ví dụ: 3tr, 500k, 2.500.000.",422);
  return r.Cost;
 }
}
