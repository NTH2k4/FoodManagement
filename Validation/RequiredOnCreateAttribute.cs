using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace FoodManagement.Validation
{
    public class RequiredOnCreateAttribute : ValidationAttribute, IClientModelValidator
    {
        public RequiredOnCreateAttribute()
        {
            ErrorMessage = "Mật khẩu là bắt buộc khi tạo tài khoản.";
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            var httpContextAccessor = (IHttpContextAccessor)context.ActionContext.HttpContext.RequestServices.GetService(typeof(IHttpContextAccessor))!;
            var path = httpContextAccessor.HttpContext?.Request.Path.Value;

            if (path != null && path.Contains("CreateUser", StringComparison.OrdinalIgnoreCase))
            {
                MergeAttribute(context.Attributes, "data-val", "true");
                MergeAttribute(context.Attributes, "data-val-required", ErrorMessage!);
            }
        }

        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }
            attributes.Add(key, value);
            return true;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var httpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor))!;
            var path = httpContextAccessor.HttpContext?.Request.Path.Value;

            if (path != null && path.Contains("CreateUser", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(Convert.ToString(value)))
                {
                    return new ValidationResult(ErrorMessage ?? "Mật khẩu là bắt buộc khi tạo tài khoản.");
                }
            }

            return ValidationResult.Success;
        }

    }
}
