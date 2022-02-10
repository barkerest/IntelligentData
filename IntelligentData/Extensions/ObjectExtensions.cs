using System;
using System.Linq;
using System.Reflection;

namespace IntelligentData.Extensions
{
    internal static class ObjectExtensions
    {
        internal static object? GetNonPublicField(this object obj, string fieldName)
            => obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);

        internal static T? GetNonPublicField<T>(this object obj, string fieldName)
            => (T?)GetNonPublicField(obj, fieldName);

        internal static bool SetNonPublicField(this object obj, string fieldName, object? value)
        {
            var fld = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (fld is null) return false;
            fld.SetValue(obj, value);
            return true;
        }

        internal static object? GetNonPublicProperty(this object obj, string propertyName)
            => obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);

        internal static T? GetNonPublicProperty<T>(this object obj, string propertyName)
            => (T?) GetNonPublicProperty(obj, propertyName);

        internal static bool SetNonPublicProperty(this object obj, string propertyName, object? value)
        {
            var type = obj.GetType();
            var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                       ?? type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop is null) return false;

            if (prop.GetSetMethod(true) is { } setter)
            {
                setter.Invoke(obj, new[] {value});
                return true;
            }

            var fld = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                          .FirstOrDefault(f => f.Name.Contains($"<{propertyName}>", StringComparison.OrdinalIgnoreCase));
            if (fld is null) return false;
            fld.SetValue(obj, value);
            return true;
        }

    }
}
