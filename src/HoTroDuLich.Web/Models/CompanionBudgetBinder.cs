using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace HoTroDuLich.Web.Models;
public sealed class CompanionBudgetBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value=context.ValueProvider.GetValue(context.ModelName);if(value!=ValueProviderResult.None)context.ModelState.SetModelValue(context.ModelName,value);
        if(string.IsNullOrWhiteSpace(value.FirstValue))context.Result=ModelBindingResult.Success(null);
        else if(decimal.TryParse(value.FirstValue,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var amount)&&amount>=0&&amount<=1000000000&&decimal.Round(amount,2)==amount)context.Result=ModelBindingResult.Success(amount);
        else{context.ModelState.TryAddModelError(context.ModelName,"Ngân sách dùng chữ số, dấu chấm cho phần thập phân, tối đa hai chữ số thập phân và không quá 1 tỷ đồng.");context.Result=ModelBindingResult.Failed();}
        return Task.CompletedTask;
    }
}
