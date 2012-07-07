using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RecycleBin.Commons.Reflection
{
   /// <summary>
   /// Provides extension methods for <see cref="Type"/> class.
   /// </summary>
   public static class TypeExtension
   {
      /// <summary>
      /// Checks if the specified type is <see cref="Nullable{T}"/>.
      /// </summary>
      /// <param name="type">The type to check.</param>
      /// <returns><c>True</c> if <paramref name="type"/> is <see cref="Nullable{T}"/>; otherwise, <c>False</c>.</returns>
      public static bool IsNullable(this Type type)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }
         return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
      }

      /// <summary>
      /// Checks if the specified type is a static class.
      /// </summary>
      /// <param name="type">The type to check.</param>
      /// <returns><c>True</c> if <paramref name="type"/> is static; otherwise, <c>False</c>.</returns>
      public static bool IsStatic(this Type type)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }
         return type.IsAbstract && type.IsSealed;
      }

      /// <summary>
      /// Enumerates base types.
      /// </summary>
      /// <param name="type">The type.</param>
      /// <returns>The base types.</returns>
      public static IEnumerable<Type> GetTypeHierarchy(this Type type)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }
         for (var current = type; current != null; current = current.BaseType)
         {
            yield return current;
         }
      }

      /// <summary>
      /// Get extension methods.
      /// </summary>
      /// <param name="type">The type for which extension methods are declared.</param>
      /// <param name="declaringType">The type to declear extension methods.</param>
      /// <param name="inherit">Determines whether the result can contain such extension methods for base type.</param>
      /// <param name="nonPublic">Determines whether the result should contain only public methods.</param>
      /// <returns>The extension methods.</returns>
      public static IEnumerable<MethodInfo> GetExtensionMethods(this Type type, Type declaringType, bool inherit = false, bool nonPublic = false)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }
         if (declaringType == null)
         {
            throw new ArgumentNullException("declaringType");
         }
         if (!declaringType.IsStatic())
         {
            throw new ArgumentException("Not a static class.", "declaringType");
         }
         var binding = BindingFlags.Public | BindingFlags.Static;
         if (nonPublic)
         {
            binding |= BindingFlags.NonPublic;
         }
         return from method in declaringType.GetMethods(binding)
                where method.IsDefined(typeof(ExtensionAttribute), false)
                let parameters = method.GetParameters()
                where parameters.Length > 0
                let parameterType = parameters[0].ParameterType
                where parameterType == type || (inherit && type.IsSubclassOf(parameterType))
                select method;
      }

      /// <summary>
      /// Parses a provided value and converts into an object.
      /// </summary>
      /// <param name="type">The type to convert.</param>
      /// <param name="value">The string to convert.</param>
      /// <param name="provider">A formatting provider.</param>
      /// <param name="parserType">A type of a parser.</param>
      /// <returns>An object.</returns>
      public static object Parse(this Type type, string value, IFormatProvider provider = null, Type parserType = null)
      {
         if (value == null)
         {
            throw new ArgumentNullException("value");
         }
         if (parserType == null)
         {
            return type.ParsePrimitive(value, provider);
         }
         var parser = Activator.CreateInstance(parserType);
         var parse = parserType.GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) });
         if (parse != null)
         {
            return parse.Invoke(parser, new object[] { value, provider });
         }
         parse = parserType.GetMethod("Parse", new[] { typeof(string) });
         if (parse != null)
         {
            return parse.Invoke(parser, new object[] { value });
         }
         throw new ArgumentException(string.Format("Parser type '{0}' does not have Parse(String [, IFormatPrivider]) method.", parserType.FullName), "parserType");
      }

      /// <summary>
      /// Parses a provided value and converts into a primitive object.
      /// </summary>
      /// <param name="type">The type to convert.</param>
      /// <param name="value">The string to convert.</param>
      /// <param name="provider">A formatting provider.</param>
      /// <returns>An object.</returns>
      public static object ParsePrimitive(this Type type, string value, IFormatProvider provider = null)
      {
         if (value == null)
         {
            throw new ArgumentNullException("value");
         }
         var typeCode = Type.GetTypeCode(type);
         switch (typeCode)
         {
            case TypeCode.Boolean:
               return Boolean.Parse(value);
            case TypeCode.Byte:
               return Byte.Parse(value, provider);
            case TypeCode.Char:
               return Char.Parse(value);
            case TypeCode.DateTime:
               return DateTime.Parse(value, provider);
            case TypeCode.Decimal:
               return Decimal.Parse(value, provider);
            case TypeCode.Double:
               return Double.Parse(value, provider);
            case TypeCode.Empty:
               return null;
            case TypeCode.Int16:
               return Int16.Parse(value, provider);
            case TypeCode.Int32:
               return Int32.Parse(value, provider);
            case TypeCode.Int64:
               return Int64.Parse(value, provider);
            case TypeCode.SByte:
               return SByte.Parse(value, provider);
            case TypeCode.Single:
               return Single.Parse(value, provider);
            case TypeCode.String:
               return value;
            case TypeCode.UInt16:
               return UInt16.Parse(value, provider);
            case TypeCode.UInt32:
               return UInt32.Parse(value, provider);
            case TypeCode.UInt64:
               return UInt64.Parse(value, provider);
            default:
               throw new NotSupportedException(string.Format("Type code {0} is not supported.", typeCode));
         }
      }

      /// <summary>
      /// Formats a provided value and converts into a string.
      /// </summary>
      /// <param name="type">The type to convert.</param>
      /// <param name="value">The object to convert.</param>
      /// <param name="provider">A formatting provider.</param>
      /// <param name="formatterType">A type of a formatter.</param>
      /// <returns>A string.</returns>
      public static string Format(this Type type, object value, IFormatProvider provider = null, Type formatterType = null)
      {
         if (value != null && value.GetType() != type)
         {
            throw new ArgumentException("Invalid value.", "value");
         }
         if (formatterType == null)
         {
            return type.FormatPrimitive(value, provider);
         }
         var formatter = Activator.CreateInstance(formatterType);
         var format = formatterType.GetMethod("Format", new[] { typeof(object), typeof(IFormatProvider) });
         if (format != null && format.ReturnType == typeof(string))
         {
            return (string)format.Invoke(formatter, new[] { value, provider });
         }
         format = formatterType.GetMethod("Format", new[] { typeof(object) });
         if (format != null && format.ReturnType == typeof(string))
         {
            return (string)format.Invoke(formatter, new[] { value });
         }
         throw new ArgumentException(string.Format("Formatter type '{0}' does not have Format(Object [, IFormatPrivider]) method.", formatterType.FullName), "formatterType");
      }

      /// <summary>
      /// Parses a provided value and converts into a string.
      /// </summary>
      /// <param name="type">The type to convert.</param>
      /// <param name="value">The object to convert.</param>
      /// <param name="provider">A formatting provider.</param>
      /// <returns>A string.</returns>
      public static string FormatPrimitive(this Type type, object value, IFormatProvider provider = null)
      {
         return string.Format(provider, "{0}", value);
      }
   }
}
