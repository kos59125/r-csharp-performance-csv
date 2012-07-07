using System;
using System.Globalization;
using System.Reflection;
using RecycleBin.Commons.Reflection;

namespace RecycleBin.Commons.IO
{
   /// <summary>
   /// Provides metainformation about relationship between
   /// </summary>
   [Serializable]
   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
   public class ValueAttribute : Attribute
   {
      /// <summary>
      /// Gets or sets the zero-based index in an array.
      /// </summary>
      /// <returns>The zero-based index.</returns>
      public int[] ArrayIndex { get; set; }

      /// <summary>
      /// Gets or sets the property index.
      /// </summary>
      /// <returns>The property index.</returns>
      public object[] PropertyIndex { get; set; }

      /// <summary>
      /// Gets or sets the value to regard as <c>Nothing</c>.
      /// </summary>
      /// <returns>The string representation of null value.</returns>
      public string NullString { get; set; }

      /// <summary>
      /// Gets or sets a language culture name.
      /// </summary>
      /// <returns>The language culture name.</returns>
      public string CultureName { get; set; }

      /// <summary>
      /// Gets or sets a parser of the column.
      /// </summary>
      /// <returns>The parser.</returns>
      public Type ParserType { get; set; }

      /// <summary>
      /// Gets or sets a formatter of the column.
      /// </summary>
      /// <returns>The formatter.</returns>
      public Type FormatterType { get; set; }

      /// <summary>
      /// Parses an object and converts it into an string.
      /// </summary>
      /// <param name="value">The object value.</param>
      /// <returns>The string value.</returns>
      public string Format(object value)
      {
         if (value == null)
         {
            return NullString ?? "";
         }
         var valueType = value.GetType();
         var culture = CultureName == null ? null : CultureInfo.GetCultureInfo(CultureName);
         return valueType.Format(value, culture, FormatterType);
      }

      /// <summary>
      /// Parses a string and converts it into an object.
      /// </summary>
      /// <param name="value">The string value.</param>
      /// <param name="memberType">The type to convert.</param>
      /// <returns>The object value.</returns>
      public object Parse(string value, Type memberType)
      {
         if (value == NullString)
         {
            return null;
         }
         var type = memberType;
         if (type.IsArray && ArrayIndex != null)
         {
            type = type.GetElementType();
         }
         if (type.IsNullable() && ParserType == null)
         {
            type = type.GetGenericArguments()[0];
         }
         var culture = CultureName == null ? null : CultureInfo.GetCultureInfo(CultureName);
         return type.Parse(value, culture, ParserType);
      }

      internal GetValue GenerateGetValue(PropertyInfo property)
      {
         GetValue getValue = instance => property.GetValue(instance, PropertyIndex);
         var propertyType = property.PropertyType;
         if (propertyType.IsArray && ArrayIndex != null)
         {
            return instance =>
            {
               var array = getValue(instance) as Array;
               return array == null ? Activator.CreateInstance(propertyType.GetElementType()) : array.GetValue(ArrayIndex);
            };
         }
         return getValue;
      }

      internal GetValue GenerateGetValue(FieldInfo field)
      {
         GetValue getValue = field.GetValue;
         var fieldType = field.FieldType;
         if (fieldType.IsArray && ArrayIndex != null)
         {
            return instance =>
            {
               var array = getValue(instance) as Array;
               return array == null ? Activator.CreateInstance(fieldType.GetElementType()) : array.GetValue(ArrayIndex);
            };
         }
         return getValue;
      }

      internal SetValue GenerateSetValue(PropertyInfo property)
      {
         SetValue setValue = (instance, value) => property.SetValue(instance, value, PropertyIndex);
         var propertyType = property.PropertyType;
         if (propertyType.IsArray && ArrayIndex != null)
         {
            GetValue getValue = instance => property.GetValue(instance, null);
            return (instance, value) => ((Array)getValue(instance)).SetValue(value, ArrayIndex);
         }
         return setValue;
      }

      internal SetValue GenerateSetValue(FieldInfo field)
      {
         SetValue setValue = field.SetValue;
         var fieldType = field.FieldType;
         if (fieldType.IsArray && ArrayIndex != null)
         {
            GetValue getValue = field.GetValue;
            return (instance, value) => ((Array)getValue(instance)).SetValue(value, ArrayIndex);
         }
         return setValue;
      }
   }
}
