using System;
using IntelligentData.Delegates;
using IntelligentData.Errors;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace IntelligentData.Extensions
{
    /// <summary>
    /// Extension methods for IProperty and IMutableProperty interfaces.
    /// </summary>
    public static class PropertyExtensions
    {
        private const string RuntimeDefaultAnnotation = "IntelligentData:RuntimeDefault";
        private const string AutoUpdateAnnotation     = "IntelligentData:AutoUpdate";
        private const string StringFormatAnnotation   = "IntelligentData:StringFormat";

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasRuntimeDefault(
            this IMutableProperty        property,
            IRuntimeDefaultValueProvider provider
        )
        {
            property.SetAnnotation(RuntimeDefaultAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasRuntimeDefault(
            this PropertyBuilder         property,
            IRuntimeDefaultValueProvider provider
        )
        {
            property.Metadata.SetAnnotation(RuntimeDefaultAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasAutoUpdate(
            this IMutableProperty    property,
            IAutoUpdateValueProvider provider
        )
        {
            property.SetAnnotation(AutoUpdateAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasAutoUpdate(
            this PropertyBuilder     property,
            IAutoUpdateValueProvider provider
        )
        {
            property.Metadata.SetAnnotation(AutoUpdateAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasStringFormat(
            this IMutableProperty property,
            IStringFormatProvider provider
        )
        {
            property.SetAnnotation(StringFormatAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasStringFormat(
            this PropertyBuilder  property,
            IStringFormatProvider provider
        )
        {
            property.Metadata.SetAnnotation(StringFormatAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public static IMutableProperty HasRuntimeDefault<TProvider>(
            this IMutableProperty property
        )
            where TProvider : IRuntimeDefaultValueProvider
        {
            property.SetAnnotation(RuntimeDefaultAnnotation, typeof(TProvider));
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public static PropertyBuilder HasRuntimeDefault<TProvider>(
            this PropertyBuilder property
        )
            where TProvider : IRuntimeDefaultValueProvider
        {
            property.Metadata.SetAnnotation(RuntimeDefaultAnnotation, typeof(TProvider));
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public static IMutableProperty HasAutoUpdate<TProvider>(
            this IMutableProperty property
        )
            where TProvider : IAutoUpdateValueProvider
        {
            property.SetAnnotation(AutoUpdateAnnotation, typeof(TProvider));
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public static PropertyBuilder HasAutoUpdate<TProvider>(
            this PropertyBuilder property
        )
            where TProvider : IAutoUpdateValueProvider
        {
            property.Metadata.SetAnnotation(AutoUpdateAnnotation, typeof(TProvider));
            return property;
        }

        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public static IMutableProperty HasStringFormat<TProvider>(
            this IMutableProperty property
        )
            where TProvider : IStringFormatProvider
        {
            property.SetAnnotation(StringFormatAnnotation, typeof(TProvider));
            return property;
        }

        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public static PropertyBuilder HasStringFormat<TProvider>(
            this PropertyBuilder property
        )
            where TProvider : IStringFormatProvider
        {
            property.Metadata.SetAnnotation(StringFormatAnnotation, typeof(TProvider));
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasRuntimeDefault(
            this IMutableProperty property,
            ValueProviderDelegate provider
        )
        {
            property.SetAnnotation(RuntimeDefaultAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasRuntimeDefault(
            this PropertyBuilder  property,
            ValueProviderDelegate provider
        )
        {
            property.Metadata.SetAnnotation(RuntimeDefaultAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasRuntimeDefault(
            this IMutableProperty  property,
            Func<object?, object?> provider
        )
        {
            ValueProviderDelegate valueProvider = (_, v, _) => provider(v);
            property.SetAnnotation(RuntimeDefaultAnnotation, valueProvider);
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasRuntimeDefault(
            this PropertyBuilder   property,
            Func<object?, object?> provider
        )
        {
            ValueProviderDelegate valueProvider = (_, v, _) => provider(v);
            property.Metadata.SetAnnotation(RuntimeDefaultAnnotation, valueProvider);
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasRuntimeDefault(
            this IMutableProperty property,
            Func<object?>         provider
        )
        {
            ValueProviderDelegate valueProvider = (_, _, _) => provider();
            property.SetAnnotation(RuntimeDefaultAnnotation, valueProvider);
            return property;
        }

        /// <summary>
        /// Adds a runtime default value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasRuntimeDefault(
            this PropertyBuilder property,
            Func<object?>        provider
        )
        {
            ValueProviderDelegate valueProvider = (_, _, _) => provider();
            property.Metadata.SetAnnotation(RuntimeDefaultAnnotation, valueProvider);
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasAutoUpdate(
            this IMutableProperty property,
            ValueProviderDelegate provider
        )
        {
            property.SetAnnotation(AutoUpdateAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasAutoUpdate(
            this PropertyBuilder  property,
            ValueProviderDelegate provider
        )
        {
            property.Metadata.SetAnnotation(AutoUpdateAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasAutoUpdate(
            this IMutableProperty  property,
            Func<object?, object?> provider
        )
        {
            ValueProviderDelegate valueProvider = (_, v, _) => provider(v);
            property.SetAnnotation(AutoUpdateAnnotation, valueProvider);
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasAutoUpdate(
            this PropertyBuilder   property,
            Func<object?, object?> provider
        )
        {
            ValueProviderDelegate valueProvider = (_, v, _) => provider(v);
            property.Metadata.SetAnnotation(AutoUpdateAnnotation, valueProvider);
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasAutoUpdate(
            this IMutableProperty property,
            Func<object?>         provider
        )
        {
            ValueProviderDelegate valueProvider = (_, _, _) => provider();
            property.SetAnnotation(AutoUpdateAnnotation, valueProvider);
            return property;
        }

        /// <summary>
        /// Adds an auto-update value annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasAutoUpdate(
            this PropertyBuilder property,
            Func<object?>        provider
        )
        {
            ValueProviderDelegate valueProvider = (_, _, _) => provider();
            property.Metadata.SetAnnotation(AutoUpdateAnnotation, valueProvider);
            return property;
        }

        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasStringFormat(
            this IMutableProperty        property,
            StringFormatProviderDelegate provider
        )
        {
            property.SetAnnotation(StringFormatAnnotation, provider);
            return property;
        }

        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasStringFormat(
            this PropertyBuilder         property,
            StringFormatProviderDelegate provider
        )
        {
            property.Metadata.SetAnnotation(StringFormatAnnotation, provider);
            return property;
        }


        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasStringFormat(
            this IMutableProperty  property,
            Func<string?, string?> provider
        )
        {
            StringFormatProviderDelegate formatProvider = (_, v, _) => provider(v);
            property.SetAnnotation(StringFormatAnnotation, formatProvider);
            return property;
        }

        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasStringFormat(
            this PropertyBuilder   property,
            Func<string?, string?> provider
        )
        {
            StringFormatProviderDelegate formatProvider = (_, v, _) => provider(v);
            property.Metadata.SetAnnotation(StringFormatAnnotation, formatProvider);
            return property;
        }


        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IMutableProperty HasStringFormat(
            this IMutableProperty property,
            Func<string?>         provider
        )
        {
            StringFormatProviderDelegate formatProvider = (_, _, _) => provider();
            property.SetAnnotation(StringFormatAnnotation, formatProvider);
            return property;
        }

        /// <summary>
        /// Adds a string format annotation to the property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static PropertyBuilder HasStringFormat(
            this PropertyBuilder property,
            Func<string?>        provider
        )
        {
            StringFormatProviderDelegate formatProvider = (_, _, _) => provider();
            property.Metadata.SetAnnotation(StringFormatAnnotation, formatProvider);
            return property;
        }

        /// <summary>
        /// Determines if the property has a runtime default value annotation.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool HasRuntimeDefault(this IProperty property)
        {
            var annotation = property.FindAnnotation(RuntimeDefaultAnnotation);
            if (annotation?.Value is null) return false;

            if (annotation.Value is IRuntimeDefaultValueProvider) return true;

            if (annotation.Value is ValueProviderDelegate) return true;

            var t = typeof(IRuntimeDefaultValueProvider);
            return annotation.Value is Type type && t.IsAssignableFrom(type);
        }

        /// <summary>
        /// Determines if the property has an auto-update value annotation.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool HasAutoUpdate(this IProperty property)
        {
            var annotation = property.FindAnnotation(AutoUpdateAnnotation);
            if (annotation?.Value is null) return false;

            if (annotation.Value is IAutoUpdateValueProvider) return true;

            if (annotation.Value is ValueProviderDelegate) return true;

            var t = typeof(IAutoUpdateValueProvider);
            return annotation.Value is Type type && t.IsAssignableFrom(type);
        }

        /// <summary>
        /// Determines if the property has a string format annotation.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool HasStringFormat(this IProperty property)
        {
            var annotation = property.FindAnnotation(StringFormatAnnotation);
            if (annotation?.Value is null) return false;

            if (annotation.Value is IStringFormatProvider) return true;

            if (annotation.Value is StringFormatProviderDelegate) return true;

            var t = typeof(IStringFormatProvider);
            return annotation.Value is Type type && t.IsAssignableFrom(type);
        }

        /// <summary>
        /// Gets the runtime default value provider for the property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="serviceProvider">A service provider used to instantiate typed value providers.</param>
        /// <returns>Returns a value provider delegate or null.</returns>
        public static ValueProviderDelegate? GetRuntimeDefaultValueProvider(
            this IProperty   property,
            IServiceProvider serviceProvider
        )
        {
            var annotation = property.FindAnnotation(RuntimeDefaultAnnotation);
            if (annotation?.Value is null) return null;

            if (annotation.Value is IRuntimeDefaultValueProvider obj) return obj.ValueOrDefault;

            if (annotation.Value is ValueProviderDelegate func) return func;

            var tTest = typeof(IRuntimeDefaultValueProvider);
            if (annotation.Value is not Type tVal) return null;
            if (!tTest.IsAssignableFrom(tVal)) return null;

            try
            {
                var objInstance = (IRuntimeDefaultValueProvider)ActivatorUtilities.CreateInstance(
                    serviceProvider,
                    tVal
                );

                return objInstance.ValueOrDefault;
            }
            catch (Exception ex) when (ex is InvalidCastException or InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the auto-update value provider for the property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="serviceProvider">A service provider used to instantiate typed value providers.</param>
        /// <returns>Returns a value provider delegate or null.</returns>
        public static ValueProviderDelegate? GetAutoUpdateValueProvider(
            this IProperty   property,
            IServiceProvider serviceProvider
        )
        {
            var annotation = property.FindAnnotation(AutoUpdateAnnotation);
            if (annotation?.Value is null) return null;

            if (annotation.Value is IAutoUpdateValueProvider obj) return obj.NewValue;

            if (annotation.Value is ValueProviderDelegate func) return func;

            var tTest = typeof(IAutoUpdateValueProvider);
            if (annotation.Value is not Type tVal) return null;
            if (!tTest.IsAssignableFrom(tVal)) return null;

            try
            {
                var objInstance = (IAutoUpdateValueProvider)ActivatorUtilities.CreateInstance(
                    serviceProvider,
                    tVal
                );

                return objInstance.NewValue;
            }
            catch (Exception ex) when (ex is InvalidCastException or InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the runtime default value provider for the property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="serviceProvider">A service provider used to instantiate typed value providers.</param>
        /// <returns>Returns a value provider delegate or null.</returns>
        public static StringFormatProviderDelegate? GetStringFormatProvider(
            this IProperty   property,
            IServiceProvider serviceProvider
        )
        {
            var annotation = property.FindAnnotation(StringFormatAnnotation);
            if (annotation?.Value is null) return null;

            if (annotation.Value is IStringFormatProvider obj) return obj.FormatValue;

            if (annotation.Value is StringFormatProviderDelegate func) return func;

            var tTest = typeof(IStringFormatProvider);
            if (annotation.Value is not Type tVal) return null;
            if (!tTest.IsAssignableFrom(tVal)) return null;

            try
            {
                var objInstance = (IStringFormatProvider)ActivatorUtilities.CreateInstance(
                    serviceProvider,
                    tVal
                );

                return objInstance.FormatValue;
            }
            catch (Exception ex) when (
                (ex is InvalidCastException) ||
                (ex is InvalidOperationException)
            )
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the value of the property from the supplied entity.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="PropertyWithoutAccessorException"></exception>
        public static object? GetValue(this IProperty property, object entity)
        {
            if (property.PropertyInfo is not null && property.PropertyInfo.CanRead)
                return property.PropertyInfo.GetValue(entity);
            
            if (property.FieldInfo is not null)
                return property.FieldInfo.GetValue(entity);
            
            throw new PropertyWithoutAccessorException(property);
        }

        /// <summary>
        /// Gets the value type of the property.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        /// <exception cref="PropertyWithoutAccessorException"></exception>
        public static Type GetValueType(this IProperty property)
        {
            if (property.PropertyInfo is not null && property.PropertyInfo.CanRead) return property.PropertyInfo.PropertyType;

            if (property.FieldInfo is not null) return property.FieldInfo.FieldType;

            throw new PropertyWithoutAccessorException(property);
        }

        /// <summary>
        /// Sets the value of the property in the supplied entity.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <exception cref="PropertyWithoutAccessorException"></exception>
        public static void SetValue(this IProperty property, object entity, object? value)
        {
            if (property.PropertyInfo is not null &&
                property.PropertyInfo.CanWrite)
            {
                property.PropertyInfo.SetValue(entity, value);
                return;
            }
            
            if (property.FieldInfo is not null)
            {
                property.FieldInfo.SetValue(entity, value);
                return;                
            }

            throw new PropertyWithoutAccessorException(property, true);
        }
    }
}
