using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace lol.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DisplayAttribute>();
            return attr != null ? attr.GetName() : value.ToString();
        }
    }
} 