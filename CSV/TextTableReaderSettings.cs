using System;
using System.Text;

namespace RecycleBin.Commons.IO
{
   /// <summary>
   /// Specifies specification of <see cref="TextTableReader"/>.
   /// </summary>
   [Serializable]
   public class TextTableReaderSettings
   {
      private Encoding encoding;

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      public TextTableReaderSettings()
      {
         this.encoding = Encoding.Default;
      }

      /// <summary>
      /// Gets or sets the encoding of the table.
      /// </summary>
      public Encoding Encoding
      {
         get { return this.encoding; }
         set { this.encoding = value ?? Encoding.Default; }
      }
   }
}
