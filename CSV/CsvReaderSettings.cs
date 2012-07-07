﻿using System;

namespace RecycleBin.Commons.IO
{
   /// <summary>
   /// Specifies specification of <see cref="CsvReader"/>.
   /// </summary>
   [Serializable]
   public class CsvReaderSettings : TextTableReaderSettings
   {
      private string fieldDelimiter;

      /// <summary>
      /// Initializes a new <see cref="CsvReaderSettings"/>.
      /// </summary>
      public CsvReaderSettings()
      {
         FieldDelimiter = ",";
         RecordDelimiter = EndOfLine.CRLF;
         QuotationCharacter = '"';
      }

      /// <summary>
      /// Gets or sets the delimiter of fields.
      /// </summary>
      public string FieldDelimiter
      {
         get { return this.fieldDelimiter; }
         set
         {
            if (value == null)
            {
               throw new ArgumentNullException("value");
            }
            if (value.Length == 0)
            {
               throw new ArgumentException("No character is contained in the specified delimiter.", "value");
            }
            this.fieldDelimiter = value;
         }
      }

      /// <summary>
      /// Gets or sets the delimiter of records.
      /// </summary>
      public EndOfLine RecordDelimiter { get; set; }

      /// <summary>
      /// Gets or sets the character of quotation.
      /// </summary>
      public char QuotationCharacter { get; set; }
   }
}
