using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace BusTicketSystem.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            var enumType = enumValue.GetType();
            var memberInfo = enumType.GetMember(enumValue.ToString());

            if (memberInfo.Length > 0)
            {
                var displayAttribute = memberInfo[0].GetCustomAttribute<DisplayAttribute>();
                if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.GetName()))
                {
#pragma warning disable CS8603 // Possible null reference return.
                    return displayAttribute.GetName();
#pragma warning restore CS8603 // Possible null reference return.
                }
            }
            return enumValue.ToString(); // Fallback to enum member name if DisplayAttribute is not found
        }
    }
}