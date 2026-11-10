using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class NotificationMigration
{
 public static void Run(string root)
 {
  var services=Path.Combine(root,"src/HoTroDuLich.Web/Services");
  var implementation=Path.Combine(services,"Components/ThongBaoService.cs");
  const string oldSignature="void Tao(long id, string text, string url, string type = \"NghiepVu\")";
  if(!File.ReadAllText(implementation).Contains(oldSignature))throw new InvalidOperationException("Notification signature already migrated.");
  var changed=0;
  foreach(var path in Directory.EnumerateFiles(services,"*.cs",SearchOption.AllDirectories))
  {
   var original=File.ReadAllText(path);
   var visitor=new Reorder();
   var rewritten=visitor.Visit(CSharpSyntaxTree.ParseText(original).GetRoot())!;
   if(visitor.Count>0){File.WriteAllText(path,rewritten.ToFullString());changed+=visitor.Count;}
  }
  foreach(var path in new[]{implementation,Path.Combine(services,"Contracts/IThongBaoService.cs")})
  {
   var source=File.ReadAllText(path).Replace(oldSignature,"void Tao(long maNguoiNhan, string loai, string noiDung, string duongDan)");
   if(path==implementation)source=source.Replace("MaNguoiNhan = id, LoaiThongBao = type, NoiDung = text, DuongDan = url","MaNguoiNhan = maNguoiNhan, LoaiThongBao = loai, NoiDung = noiDung, DuongDan = duongDan");
   File.WriteAllText(path,source);
  }
  Console.WriteLine($"Updated {changed} notification calls to recipient/type/content/link.");
 }
 sealed class Reorder:CSharpSyntaxRewriter
 {
  public int Count;
  public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
  {
   node=(InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
   if(node.Expression is not MemberAccessExpressionSyntax member||member.Name.Identifier.ValueText!="Tao")return node;
   if(node.ArgumentList.Arguments.Count!=4)throw new InvalidOperationException("Unexpected notification call: "+node);
   var args=node.ArgumentList.Arguments;
   Count++;
   return node.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]{args[0],args[3],args[1],args[2]})));
  }
 }
}
