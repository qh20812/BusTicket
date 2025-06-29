using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace BusTicketSystem.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AddressFormatAttribute : ValidationAttribute
    {
        private const string AddressRegexPattern = @"^([^,]+,\s*[^,]+,\s*[^,]+.*)$";


        public AddressFormatAttribute()
        {
            ErrorMessage = "Địa chỉ phải theo định dạng: Xã/Phường, Quận/Huyện, Tỉnh/Thành phố. Các phần không được để trống.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var addressString = value.ToString()!;

            if (!Regex.IsMatch(addressString, AddressRegexPattern))
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
            
            var parts = addressString.Split(',');
            if (parts.Length < 3 || parts.Take(3).Any(p => string.IsNullOrWhiteSpace(p.Trim())))
            {
                 return new ValidationResult("Địa chỉ phải có ít nhất 3 phần (Xã/Phường, Quận/Huyện, Tỉnh/Thành phố) và mỗi phần không được để trống.");
            }

            return ValidationResult.Success;
        }
    }
}