using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace HoTroDuLich.Web.Models;

// Form/API money uses a decimal point without thousands separators, as HTML number inputs do.
public sealed class ExpenseAmountBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value=context.ValueProvider.GetValue(context.ModelName);
        if(value!=ValueProviderResult.None)context.ModelState.SetModelValue(context.ModelName,value);
        if(decimal.TryParse(value.FirstValue?.Trim(),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var amount))
            context.Result=ModelBindingResult.Success(amount);
        else {context.ModelState.TryAddModelError(context.ModelName,"Nhập số tiền bằng chữ số, dùng dấu chấm cho phần thập phân và không dùng dấu phân cách hàng nghìn.");context.Result=ModelBindingResult.Failed();}
        return Task.CompletedTask;
    }
}
