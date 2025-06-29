using System;
using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (ErrorMessage == null) // Set default error message if not overridden
            {
                ErrorMessage = $"Người đăng ký phải ít nhất {_minimumAge} tuổi.";
            }

            if (value == null)
            {
                return new ValidationResult($"Vui lòng nhập {validationContext.DisplayName}.");
            }

            if (value is DateTime dateOfBirth)
            {
                if (dateOfBirth.Date > DateTime.Today)
                {
                    return new ValidationResult("Ngày sinh không hợp lệ (không thể là ngày trong tương lai).");
                }

                var today = DateTime.Today;
                var age = today.Year - dateOfBirth.Year;
                if (dateOfBirth.Date > today.AddYears(-age))
                {
                    age--;
                }

                if (age < _minimumAge)
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }
            }
            else
            {
                return new ValidationResult("Giá trị cung cấp không phải là ngày hợp lệ.");
            }

            return ValidationResult.Success;
        }
    }
}