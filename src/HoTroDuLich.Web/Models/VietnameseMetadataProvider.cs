using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace HoTroDuLich.Web.Models;
public class VietnameseMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.Attributes.OfType<ValidationAttribute>())
        {
            if (attribute.ErrorMessage != null) continue;
            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute => "Vui lòng nhập {0}.",
                EmailAddressAttribute => "Vui lòng nhập địa chỉ email hợp lệ.",
                MinLengthAttribute => "{0} cần có ít nhất {1} ký tự.",
                MaxLengthAttribute => "{0} không được vượt quá {1} ký tự.",
                RangeAttribute => "{0} phải nằm trong khoảng từ {1} đến {2}.",
                CompareAttribute => "Thông tin xác nhận không khớp. Vui lòng kiểm tra lại.",
                RegularExpressionAttribute => "{0} không đúng định dạng. Vui lòng kiểm tra lại.",
                _ => "{0} không hợp lệ. Vui lòng kiểm tra lại."
            };
        }
    }
}
