using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Http;

namespace HoTroDuLich.Web.Services;

/// <summary>Mot truong thong tin rieng cua mot loai co so. Kind: select | multi | text | number | time | list ("ten | gia" moi dong).</summary>
public sealed record TemplateField(string Key,string Label,string Kind,string[]? Options=null,int Max=300,string? Hint=null,bool Tag=false);

/// <summary>Mau thong tin theo loai co so: an uong, luu tru, tham quan/hoat dong, di chuyen. Dung de sinh bieu mau, kiem tra du lieu va hien thi cho khach.</summary>
public static class BranchTemplates
{
 public static IReadOnlyCollection<string> Types=>ServiceKinds.BranchCodes;
 public static TemplateField[] For(string? type)=>ServiceKinds.Fields(type);
 public static string Name(string type,string key)=>$"attr.{type}.{key}";
 static Exception Bad(string m)=>new BusinessException(m,422);

 /// <summary>Doc cac truong "attr.{loai}.{khoa}" cua bieu mau theo mau cua loai da chon, kiem tra va tra ve JSON (hoac null neu de trong).</summary>
 public static string? Collect(IFormCollection form,string? type)
 {
  var result=new Dictionary<string,object>();
  foreach(var f in For(type))
  {
   var name=Name(type!,f.Key);var raw=form[name].Select(x=>(x??"").Trim()).Where(x=>x.Length>0).ToList();
   if(raw.Count==0)continue;
   switch(f.Kind)
   {
    case "select":
     if(!f.Options!.Contains(raw[0]))throw Bad(f.Label+": giá trị không hợp lệ.");
     result[f.Key]=raw[0];break;
    case "multi":
     var chosen=raw.Distinct().ToList();
     if(chosen.Any(x=>!f.Options!.Contains(x)))throw Bad(f.Label+": có lựa chọn không hợp lệ.");
     result[f.Key]=chosen;break;
    case "text":
     if(raw[0].Length>f.Max)throw Bad(f.Label+" tối đa "+f.Max+" ký tự.");
     result[f.Key]=raw[0];break;
    case "time":
     if(!Regex.IsMatch(raw[0],@"^([01]\d|2[0-3]):[0-5]\d$"))throw Bad(f.Label+" cần dạng HH:mm.");
     result[f.Key]=raw[0];break;
    case "date":
     if(!DateOnly.TryParseExact(raw[0],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day))throw Bad(f.Label+" cần dạng ngày hợp lệ.");
     result[f.Key]=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);break;
    case "number":
     if(!int.TryParse(raw[0],NumberStyles.None,CultureInfo.InvariantCulture,out var n)||n<1||n>f.Max)throw Bad(f.Label+" phải từ 1 đến "+f.Max+".");
     result[f.Key]=n;break;
    case "list":
     var lines=raw[0].Replace("\r","").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
     if(lines.Length>f.Max)throw Bad(f.Label+" tối đa "+f.Max+" dòng.");
     var items=new List<object>();
     foreach(var line in lines)
     {
      var parts=line.Split('|',2,StringSplitOptions.TrimEntries);
      if(parts[0].Length==0||parts[0].Length>80)throw Bad(f.Label+": tên mỗi dòng từ 1 đến 80 ký tự.");
      decimal? price=null;
      if(parts.Length==2&&parts[1].Length>0)
      {
       var digits=Regex.Replace(parts[1],@"[\.\s,₫đ]|vnđ|vnd","",RegexOptions.IgnoreCase);
       if(!decimal.TryParse(digits,NumberStyles.None,CultureInfo.InvariantCulture,out var p)||p>1_000_000_000)throw Bad(f.Label+": giá \""+parts[1]+"\" không hợp lệ.");
       price=p;
      }
      items.Add(new{n=parts[0],p=price});
     }
     if(items.Count>0)result[f.Key]=items;break;
   }
  }
  if(result.Count==0)return null;
  var json=JsonSerializer.Serialize(result);
  if(json.Length>5800)throw Bad("Thông tin riêng của cơ sở quá dài. Hãy rút gọn.");
  return json;
 }

 public static Dictionary<string,JsonElement> Parse(string? json)
 {
  if(string.IsNullOrWhiteSpace(json))return [];
  try{using var d=JsonDocument.Parse(json);return d.RootElement.ValueKind==JsonValueKind.Object?d.RootElement.EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.Clone()):[];}
  catch(JsonException){return [];}
 }
 static string Money(decimal p)=>p.ToString("N0",CultureInfo.GetCultureInfo("vi-VN"))+" ₫";
 /// <summary>Danh sach (nhan, cac dong gia tri) de hien cho khach.</summary>
 public static List<(string Label,List<string> Items)> Describe(string? type,string? json)
 {
  var data=Parse(json);var result=new List<(string,List<string>)>();
  foreach(var f in For(type))
  {
   if(!data.TryGetValue(f.Key,out var v))continue;var items=new List<string>();
   switch(v.ValueKind)
   {
    case JsonValueKind.String:items.Add(f.Kind=="date"&&DateOnly.TryParseExact(v.GetString(),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var dd)?dd.ToString("dd/MM/yyyy"):v.GetString()!);break;
    case JsonValueKind.Number:items.Add(v.GetInt32().ToString(CultureInfo.InvariantCulture)+(f.Key=="succhua"?" người":""));break;
    case JsonValueKind.Array:
     foreach(var e in v.EnumerateArray())
     {
      if(e.ValueKind==JsonValueKind.String)items.Add(e.GetString()!);
      else if(e.ValueKind==JsonValueKind.Object)items.Add(e.GetProperty("n").GetString()+(e.TryGetProperty("p",out var p)&&p.ValueKind==JsonValueKind.Number?" — "+Money(p.GetDecimal()):""));
     }
     break;
   }
   if(items.Count>0)result.Add((f.Label,items));
  }
  return result;
 }
 /// <summary>Tu khoa de tim kiem: cac lua chon co danh dau Tag.</summary>
 public static string Tags(string? type,string? json)
 {
  var data=Parse(json);var tags=new List<string>();
  foreach(var f in For(type).Where(x=>x.Tag))
  {
   if(!data.TryGetValue(f.Key,out var v))continue;
   if(v.ValueKind==JsonValueKind.String)tags.Add(v.GetString()!);
   else if(v.ValueKind==JsonValueKind.Array)tags.AddRange(v.EnumerateArray().Where(e=>e.ValueKind==JsonValueKind.String).Select(e=>e.GetString()!));
  }
  return string.Join(", ",tags.Distinct());
 }
 /// <summary>Gia tri hien tai de dien lai bieu mau: ten khoa -> cac dong (list duoc dung dang "ten | gia").</summary>
 public static List<string> Current(Dictionary<string,JsonElement> data,TemplateField f)
 {
  if(!data.TryGetValue(f.Key,out var v))return [];
  return v.ValueKind switch
  {
   JsonValueKind.String=>[v.GetString()!],
   JsonValueKind.Number=>[v.GetInt32().ToString(CultureInfo.InvariantCulture)],
   JsonValueKind.Array=>v.EnumerateArray().Select(e=>e.ValueKind==JsonValueKind.String?e.GetString()!:e.GetProperty("n").GetString()+(e.TryGetProperty("p",out var p)&&p.ValueKind==JsonValueKind.Number?" | "+p.GetDecimal().ToString("0"):"")).ToList(),
   _=>[]
  };
 }
}
