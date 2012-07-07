using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RecycleBin.Commons.IO
{
   /// <summary>
   /// Represents a reader for tabular data separated by some special string, comma in paticular.
   /// </summary>
   [Serializable]
   public class CsvReader : TextTableReader
   {
      private const int EndOfStream = -1;

      private readonly char[] buffer;
      private readonly bool closeInput;
      private readonly string delimiter;
      private readonly char delimiterFirst;
      private readonly char quotation;
      private readonly string separator;
      private readonly char separatorFirst;
      private readonly Stream stream;

      /// <summary>
      /// Initializes a new <see cref="CsvReader"/> with the specified path to reading file.
      /// </summary>
      /// <param name="path">The path to reading file.</param>
      /// <param name="settings">The settings.</param>
      public CsvReader(string path, CsvReaderSettings settings = null)
         : this(File.OpenRead(path), settings ?? new CsvReaderSettings(), true) {}

      /// <summary>
      /// Initializes a new <see cref="CsvReader"/> with the specified stream to read.
      /// </summary>
      /// <param name="stream">The stream to read.</param>
      /// <param name="settings">The settings.</param>
      public CsvReader(Stream stream, CsvReaderSettings settings = null)
         : this(stream, settings ?? new CsvReaderSettings(), false) {}

      private CsvReader(Stream stream, CsvReaderSettings settings, bool closeInput)
         : base(stream, settings)
      {
         this.closeInput = closeInput;
         this.delimiter = settings.RecordDelimiter.AsNewline();
         this.delimiterFirst = this.delimiter[0];
         this.separator = settings.FieldDelimiter;
         this.separatorFirst = this.separator[0];
         this.quotation = settings.QuotationCharacter;
         this.buffer = new char[Math.Max(this.delimiter.Length, this.separator.Length)];
         this.stream = stream;
      }

      /// <summary>
      /// Handles the header line.
      /// </summary>
      /// <returns>The header.</returns>
      public string[] ReadHeader()
      {
         var header = ReadRecordRaw();
         SetHeader(header);
         // Returns a copy to avoid modification.
         return header.ToArray();
      }

      /// <summary>
      /// Generates a next state from the current state.
      /// </summary>
      /// <param name="currentState">The current state.</param>
      /// <param name="field">The buffer containing a field value.</param>
      /// <returns>The next state.</returns>
      protected override ReadState Succeed(ReadState currentState, StringBuilder field)
      {
         if (currentState == ReadState.Closed)
         {
            throw new ObjectDisposedException("Stream has been closed.");
         }

         var nextCharacter = Read();
         if (nextCharacter == EndOfStream)
         {
            return ReadState.EndOfStream;
         }

         switch (currentState)
         {
            case ReadState.Default:
               while (nextCharacter != this.delimiterFirst && nextCharacter != this.separatorFirst)
               {
                  if (nextCharacter == EndOfStream)
                  {
                     return ReadState.EndOfStream;
                  }
                  field.Append((char)nextCharacter);
                  nextCharacter = Read();
               }
               if (CheckSpecialToken(nextCharacter, this.delimiter))
               {
                  return ReadState.EndOfRecord;
               }
               if (CheckSpecialToken(nextCharacter, this.separator))
               {
                  return ReadState.EndOfField;
               }
               field.Append((char)nextCharacter);
               return ReadState.Default;
            case ReadState.Quoted:
               while (nextCharacter != this.quotation)
               {
                  if (nextCharacter == EndOfStream)
                  {
                     throw new FormatException();
                  }
                  field.Append((char)nextCharacter);
                  nextCharacter = Read();
               }
               return ReadState.Escaped;
            case ReadState.EndOfRecord:
            case ReadState.EndOfField:
               if (nextCharacter == this.quotation)
               {
                  return ReadState.Quoted;
               }
               if (CheckSpecialToken(nextCharacter, this.delimiter))
               {
                  return ReadState.EndOfRecord;
               }
               if (CheckSpecialToken(nextCharacter, this.separator))
               {
                  return ReadState.EndOfField;
               }
               field.Append((char)nextCharacter);
               return ReadState.Default;
            case ReadState.Escaped:
               if (nextCharacter == this.quotation)
               {
                  field.Append(this.quotation);
                  return ReadState.Quoted;
               }
               if (CheckSpecialToken(nextCharacter, this.delimiter))
               {
                  return ReadState.EndOfRecord;
               }
               if (CheckSpecialToken(nextCharacter, this.separator))
               {
                  return ReadState.EndOfField;
               }
               throw new FormatException();
         }
         throw new InvalidOperationException();
      }

      private bool CheckSpecialToken(int firstCharacter, string token)
      {
         if (firstCharacter != token[0])
         {
            return false;
         }
         for (var index = 1; index < token.Length; index++)
         {
            char c = (char)Read();
            this.buffer[index] = c;
            if (c == token[index])
            {
               continue;
            }
            while (index >= 1)
            {
               Revert(this.buffer[index--]);
            }
            return false;
         }
         return true;
      }

      /// <summary>
      /// Disposes the reader.
      /// </summary>
      /// <param name="disposing">Determines disposing unmanaged resources.</param>
      protected override void Dispose(bool disposing)
      {
         if (this.closeInput)
         {
            try
            {
               this.stream.Close();
            }
            finally
            {
               base.Dispose(disposing);
            }
         }
         else
         {
            base.Dispose(disposing);
         }
      }
   }
}
