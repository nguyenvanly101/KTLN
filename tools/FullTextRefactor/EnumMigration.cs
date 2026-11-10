using System.IO.Compression;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class EnumMigration {
 public static void Run(string root){
  var project=Path.Combine(root,"src/HoTroDuLich.Web");var metadata=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"domain-metadata.json"))).RootElement;
  var enums=metadata.GetProperty("enums").EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.EnumerateArray().Select(v=>v.GetString()!).ToArray());
  var backup=Path.Combine(root,"preview/before-enum-migration.zip");if(!File.Exists(backup)){using var zip=ZipFile.Open(backup,ZipArchiveMode.Create);foreach(var path in Directory.EnumerateFiles(project,"*",SearchOption.AllDirectories).Where(p=>(p.EndsWith(".cs")||p.EndsWith(".cshtml"))&&!p.Contains("\\obj\\")&&!p.Contains("\\bin\\")))zip.CreateEntryFromFile(path,Path.GetRelativePath(project,path));}
  var enumPath=Path.Combine(project,"Domain/Enums.cs");File.WriteAllText(enumPath,"namespace HoTroDuLich.Web.Domain;\n"+string.Join('\n',enums.Select(e=>"public enum "+e.Key+" {"+string.Join(',',e.Value)+"}"))+"\n");
  var types=metadata.GetProperty("entities").EnumerateArray().ToDictionary(e=>e.GetProperty("name").GetString()!,e=>e.GetProperty("props").EnumerateArray().ToDictionary(p=>p.GetProperty("name").GetString()!,p=>p.GetProperty("type").GetString()!));
  var entityPath=Path.Combine(project,"Domain/Entities.cs");var entityTree=CSharpSyntaxTree.ParseText(File.ReadAllText(entityPath));var entityRoot=(CompilationUnitSyntax)entityTree.GetRoot();
  entityRoot=entityRoot.ReplaceNodes(entityRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>(),(old,current)=>{var owner=(old.Parent as ClassDeclarationSyntax)?.Identifier.Text;if(owner!=null&&types.TryGetValue(owner,out var props)&&props.TryGetValue(old.Identifier.Text,out var type)&&enums.ContainsKey(type.TrimEnd('?'))){var next=current.WithType(SyntaxFactory.ParseTypeName(type).WithTrailingTrivia(SyntaxFactory.Space));if(!type.EndsWith('?'))next=next.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression))).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));return next;}return current;});
  File.WriteAllText(entityPath,entityRoot.NormalizeWhitespace().ToFullString());
  var paths=Directory.EnumerateFiles(project,"*.cs",SearchOption.AllDirectories).Where(p=>!p.Contains("\\obj\\")&&!p.Contains("\\bin\\")).ToArray();
  var parse=CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);var trees=paths.Select(p=>CSharpSyntaxTree.ParseText(File.ReadAllText(p),parse,p)).ToList();var globals=Path.Combine(project,"obj/Debug/net10.0/HoTroDuLich.Web.GlobalUsings.g.cs");if(File.Exists(globals))trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(globals),parse));
  var references=new List<MetadataReference>();var refPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var pack in new[]{"Microsoft.NETCore.App.Ref","Microsoft.AspNetCore.App.Ref"}){var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"dotnet/packs",pack);var version=Directory.GetDirectories(folder).OrderByDescending(x=>Version.Parse(Path.GetFileName(x))).First();foreach(var path in Directory.EnumerateFiles(Path.Combine(version,"ref/net10.0"),"*.dll"))if(refPaths.Add(Path.GetFileName(path)))references.Add(MetadataReference.CreateFromFile(path));}
  foreach(var path in Directory.EnumerateFiles(Path.Combine(root,"preview/fulltext-build"),"*.dll"))if(Path.GetFileName(path)!="HoTroDuLich.Web.dll"&&refPaths.Add(Path.GetFileName(path)))references.Add(MetadataReference.CreateFromFile(path));
  var compilation=CSharpCompilation.Create("EnumMigrationInput",trees,references,new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  var count=0;foreach(var tree in trees.Where(t=>!string.IsNullOrEmpty(t.FilePath))){var visitor=new EnumCalls(compilation.GetSemanticModel(tree));var rewritten=visitor.Visit(tree.GetRoot())!;if(visitor.Changes>0){File.WriteAllText(tree.FilePath,rewritten.NormalizeWhitespace().ToFullString()+"\n");count+=visitor.Changes;}}
  var dbPath=Path.Combine(project,"Data/AppDbContext.cs");var db=File.ReadAllText(dbPath);if(!db.Contains("// FullText enum conversions")){var marker="  b.Conventions.Remove";var at=db.IndexOf(marker,StringComparison.Ordinal);db=db.Insert(at,"  // FullText enum conversions\n"+string.Join('\n',enums.Keys.Select(name=>$"  b.Properties<{name}>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);"))+"\n");var insert="   foreach(var fk in entity.GetForeignKeys())";at=db.IndexOf(insert,StringComparison.Ordinal);db=db.Insert(at,"   foreach(var property in entity.GetProperties().Where(p=>(Nullable.GetUnderlyingType(p.ClrType)??p.ClrType).IsEnum)) property.ValueGenerated=Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;\n");File.WriteAllText(dbPath,db);}
  var enumProperties=types.Values.SelectMany(p=>p).Where(p=>enums.ContainsKey(p.Value.TrimEnd('?'))).Select(p=>p.Key).Distinct().ToArray();
  foreach(var view in Directory.EnumerateFiles(Path.Combine(project,"Views"),"*.cshtml",SearchOption.AllDirectories)){var text=File.ReadAllText(view);foreach(var property in enumProperties)text=System.Text.RegularExpressions.Regex.Replace(text,@"\."+property+@"\b(?!\s*\(|\s*\.ToString\s*\()", "."+property+".ToString()");File.WriteAllText(view,text);}
  Console.WriteLine($"Converted {enums.Count} enums and {count} C# expressions; updated Razor presentation.");
 }
}

sealed class EnumCalls(SemanticModel model):CSharpSyntaxRewriter {
 public int Changes{get;private set;}
 static ITypeSymbol? EnumType(ITypeSymbol? type)=>type is INamedTypeSymbol n&&n.OriginalDefinition.SpecialType==SpecialType.System_Nullable_T?EnumType(n.TypeArguments[0]):type?.TypeKind==TypeKind.Enum?type:null;
 static bool IsString(ITypeSymbol? type)=>type?.SpecialType==SpecialType.System_String;
 ExpressionSyntax ConvertTo(ExpressionSyntax original,ExpressionSyntax visited,ITypeSymbol? target){
  var source=model.GetTypeInfo(original).Type;var toEnum=EnumType(target);var fromEnum=EnumType(source);
  if(toEnum!=null&&IsString(source)){Changes++;var constant=model.GetConstantValue(original);if(constant.HasValue&&constant.Value is string value&&toEnum.GetMembers(value).Length>0)return SyntaxFactory.ParseExpression(toEnum.ToDisplayString()+"."+value).WithTriviaFrom(visited);return SyntaxFactory.ParseExpression("System.Enum.Parse<"+toEnum.ToDisplayString()+">("+visited.WithoutTrivia()+")").WithTriviaFrom(visited);}
  if(IsString(target)&&fromEnum!=null){Changes++;return SyntaxFactory.ParseExpression("("+visited.WithoutTrivia()+").ToString()").WithTriviaFrom(visited);}
  return visited;
 }
 public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node){var result=(AssignmentExpressionSyntax)base.VisitAssignmentExpression(node)!;return result.WithRight(ConvertTo(node.Right,result.Right,model.GetTypeInfo(node.Left).Type));}
 public override SyntaxNode? VisitEqualsValueClause(EqualsValueClauseSyntax node){var result=(EqualsValueClauseSyntax)base.VisitEqualsValueClause(node)!;ITypeSymbol? target=node.Parent is VariableDeclaratorSyntax v?model.GetDeclaredSymbol(v) is ILocalSymbol local?local.Type:null:node.Parent is PropertyDeclarationSyntax p?(model.GetDeclaredSymbol(p) as IPropertySymbol)?.Type:null;return result.WithValue(ConvertTo(node.Value,result.Value,target));}
 public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node){var result=(BinaryExpressionSyntax)base.VisitBinaryExpression(node)!;if(node.IsKind(SyntaxKind.EqualsExpression)||node.IsKind(SyntaxKind.NotEqualsExpression)){var left=model.GetTypeInfo(node.Left).Type;var right=model.GetTypeInfo(node.Right).Type;if(EnumType(left)!=null&&IsString(right)){var value=model.GetConstantValue(node.Right);if(value.HasValue)return result.WithRight(ConvertTo(node.Right,result.Right,left));Changes++;return result.WithLeft(SyntaxFactory.ParseExpression("("+result.Left.WithoutTrivia()+").ToString()").WithTriviaFrom(result.Left));}if(EnumType(right)!=null&&IsString(left)){var value=model.GetConstantValue(node.Left);if(value.HasValue)return result.WithLeft(ConvertTo(node.Left,result.Left,right));Changes++;return result.WithRight(SyntaxFactory.ParseExpression("("+result.Right.WithoutTrivia()+").ToString()").WithTriviaFrom(result.Right));}}return result;}
 PatternSyntax Pattern(PatternSyntax original,PatternSyntax visited,ITypeSymbol? type){var enumType=EnumType(type);if(enumType==null)return visited;return visited.ReplaceNodes(visited.DescendantNodesAndSelf().OfType<ConstantPatternSyntax>(),(o,c)=>{if(c.Expression is LiteralExpressionSyntax literal&&literal.IsKind(SyntaxKind.StringLiteralExpression)&&enumType.GetMembers(literal.Token.ValueText).Length>0){Changes++;return c.WithExpression(SyntaxFactory.ParseExpression(enumType.ToDisplayString()+"."+literal.Token.ValueText));}return c;});}
 public override SyntaxNode? VisitIsPatternExpression(IsPatternExpressionSyntax node){var result=(IsPatternExpressionSyntax)base.VisitIsPatternExpression(node)!;return result.WithPattern(Pattern(node.Pattern,result.Pattern,model.GetTypeInfo(node.Expression).Type));}
 public override SyntaxNode? VisitSwitchExpression(SwitchExpressionSyntax node){var result=(SwitchExpressionSyntax)base.VisitSwitchExpression(node)!;var type=model.GetTypeInfo(node.GoverningExpression).Type;return result.WithArms(SyntaxFactory.SeparatedList(result.Arms.Select((arm,index)=>arm.WithPattern(Pattern(node.Arms[index].Pattern,arm.Pattern,type)))));}
 public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node){
  var result=(InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
  if(node.Expression is MemberAccessExpressionSyntax member&&member.Name.Identifier.Text=="Contains"&&node.ArgumentList.Arguments.Count==1&&EnumType(model.GetTypeInfo(node.ArgumentList.Arguments[0].Expression).Type)!=null){
   var type=model.GetTypeInfo(node.ArgumentList.Arguments[0].Expression).Type!;var enumType=EnumType(type)!;
   if(member.Expression is ImplicitArrayCreationExpressionSyntax array&&array.Initializer.Expressions.All(e=>model.GetConstantValue(e).HasValue&&model.GetConstantValue(e).Value is string)){
    Changes++;var values=array.Initializer.Expressions.Select(e=>enumType.ToDisplayString()+"."+model.GetConstantValue(e).Value);var expr="new "+enumType.ToDisplayString()+(type is INamedTypeSymbol n&&n.OriginalDefinition.SpecialType==SpecialType.System_Nullable_T?"?":"")+"[]{"+string.Join(',',values)+"}.Contains";return result.WithExpression(SyntaxFactory.ParseExpression(expr));
   }
  }
  var info=model.GetSymbolInfo(node);var method=(info.Symbol as IMethodSymbol)??info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();if(method==null)return result;
  var args=result.ArgumentList.Arguments;for(var i=0;i<Math.Min(args.Count,method.Parameters.Length);i++){var target=method.Parameters[i].Type;if(method.Parameters[i].IsParams&&target is IArrayTypeSymbol array)target=array.ElementType;args=args.Replace(args[i],args[i].WithExpression(ConvertTo(node.ArgumentList.Arguments[i].Expression,args[i].Expression,target)));}return result.WithArgumentList(result.ArgumentList.WithArguments(args));
 }
}
