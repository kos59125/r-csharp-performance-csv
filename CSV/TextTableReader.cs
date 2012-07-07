using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RecycleBin.Commons.Reflection;

namespace RecycleBin.Commons.IO
{
   /// <summary>
   /// Represents a reader that parses tabular data in plain-text form.
   /// </summary>
   [Serializable]
   public abstract class TextTableReader : IDisposable
   {
      private readonly List<string> currentRecord;
      private readonly StringBuilder fieldBuffer;
      private readonly StackTextReader reader;
      private readonly Dictionary<Type, List<Tuple<SetValue, ColumnAttribute, Type>>> typeCache;
      private string[] header;
      private ReadState state;

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      /// <param name="stream">The stream to read.</param>
      /// <param name="settings">The settings.</param>
      protected TextTableReader(Stream stream, TextTableReaderSettings settings = null)
      {
         if (stream == null)
         {
            throw new ArgumentNullException("stream");
         }
         if (!stream.CanRead)
         {
            throw new ArgumentException("Stream is not readable.", "stream");
         }
         if (settings == null)
         {
            settings = new TextTableReaderSettings();
         }
         this.reader = new StackTextReader(new StreamReader(stream, settings.Encoding));
         this.currentRecord = new List<string>();
         this.fieldBuffer = new StringBuilder();
         this.typeCache = new Dictionary<Type, List<Tuple<SetValue, ColumnAttribute, Type>>>();
         this.state = ReadState.EndOfRecord;
         this.header = null;
      }

      #region IDisposable Members

      /// <summary>
      /// Disposes the reader.
      /// </summary>
      public void Dispose()
      {
         Dispose(true);
      }

      #endregion

      /// <summary>
      /// Sets the header of the table.
      /// </summary>
      /// <param name="header">The header.</param>
      protected void SetHeader(string[] header)
      {
         this.header = header;
      }

      private string ReadField()
      {
         if (this.state == ReadState.Closed)
         {
            throw new ObjectDisposedException("Stream has been closed.");
         }
         if (this.state == ReadState.EndOfStream)
         {
            return null;
         }
         this.fieldBuffer.Length = 0;
         do
         {
            this.state = Succeed(this.state, this.fieldBuffer);
         } while (state <= ReadState.Escaped);
         return this.fieldBuffer.ToString();
      }

      private bool MoveNext()
      {
         if (this.state == ReadState.Closed)
         {
            throw new ObjectDisposedException("Stream has been closed.");
         }
         if (this.state == ReadState.EndOfStream)
         {
            return false;
         }
         this.currentRecord.Clear();
         do
         {
            var field = ReadField();
            if (field == null)
            {
               break;
            }
            this.currentRecord.Add(field);
         } while (state <= ReadState.EndOfField);
         return this.state != ReadState.EndOfStream || this.currentRecord.Count != 1 || !string.IsNullOrEmpty(this.currentRecord[0]);
      }

      /// <summary>
      /// Reads a row from the input stream.
      /// </summary>
      /// <returns>A collection of field.</returns>
      public string[] ReadRecordRaw()
      {
         return MoveNext() ? this.currentRecord.ToArray() : null;
      }

      /// <summary>
      /// Advances a state of the reader.
      /// </summary>
      /// <param name="state">A current state.</param>
      /// <param name="buffer">A buffer.</param>
      /// <returns>A new state.</returns>
      protected abstract ReadState Succeed(ReadState state, StringBuilder buffer);

      /// <summary>
      /// Reads a record in the specified type.
      /// </summary>
      /// <typeparam name="TRecord">The type of record.</typeparam>
      /// <returns>The record.</returns>
      /// <seealso cref="ColumnAttribute"/>
      public TRecord ReadRecord<TRecord>()
      {
         if (!MoveNext())
         {
            return default(TRecord);
         }
         var recordType = typeof(TRecord);
         if (recordType.IsNullable())
         {
            recordType = recordType.GetGenericArguments()[0];
         }
         var prototype = Activator.CreateInstance(recordType);
         SetValues(this.currentRecord, recordType, ref prototype);
         return (TRecord)prototype;
      }

      private void SetValues(IList<string> record, Type recordType, ref object prototype)
      {
         List<Tuple<SetValue, ColumnAttribute, Type>> members;
         if (!this.typeCache.TryGetValue(recordType, out members))
         {
            var properties = from property in recordType.GetProperties()
                             let attributes = property.GetCustomAttributes(typeof(ColumnAttribute), true)
                             from attribute in attributes.Cast<ColumnAttribute>()
                             let setProperty = attribute.GenerateSetValue(property)
                             select Tuple.Create(setProperty, attribute, property.PropertyType);
            var fields = from field in recordType.GetFields()
                         let attributes = field.GetCustomAttributes(typeof(ColumnAttribute), true)
                         from attribute in attributes.Cast<ColumnAttribute>()
                         let setField = attribute.GenerateSetValue(field)
                         select Tuple.Create(setField, attribute, field.FieldType);
            members = properties.Concat(fields).ToList();
            this.typeCache.Add(recordType, members);
         }

         foreach (var tuple in members)
         {
            var setValue = tuple.Item1;
            var attribute = tuple.Item2;
            var type = tuple.Item3;
            var index = attribute.GetIndex(this.header);
            var value = attribute.Parse(record[index], type);
            setValue(prototype, value);
         }
      }

      /// <summary>
      /// Reads records to the end of the underlying stream.
      /// </summary>
      /// <returns>The records.</returns>
      public IEnumerable<string[]> ReadToEndRaw()
      {
         while (MoveNext())
         {
            yield return this.currentRecord.ToArray();
         }
      }

      /// <summary>
      /// Reads records to the end of the underlying stream.
      /// </summary>
      /// <typeparam name="TRecord">The type of records.</typeparam>
      /// <returns>The records.</returns>
      public IEnumerable<TRecord> ReadToEnd<TRecord>()
      {
         var recordType = typeof(TRecord);
         if (recordType.IsNullable())
         {
            recordType = recordType.GetGenericArguments()[0];
         }
         while (MoveNext())
         {
            var prototype = Activator.CreateInstance(recordType);
            SetValues(this.currentRecord, recordType, ref prototype);
            yield return (TRecord)prototype;
         }
      }

      /// <summary>
      /// Disposes the reader.
      /// </summary>
      public void Close()
      {
         Dispose(true);
      }

      /// <summary>
      /// Disposes the reader.
      /// </summary>
      /// <param name="disposing">Determines disposing unmanaged resources.</param>
      protected virtual void Dispose(bool disposing)
      {
         this.state = ReadState.Closed;
      }

      /// <summary>
      /// Reads the next character from the input stream and advances the character position by one character.
      /// </summary>
      /// <returns>The next character from the input stream, or -1 if no more characters are available.</returns>
      protected int Read()
      {
         return this.reader.Read();
      }

      /// <summary>
      /// Reads the next character from the input stream. The character position will not be changed.
      /// </summary>
      /// <returns>The next character from the input stream, or -1 if no more characters are available.</returns>
      protected int Peek()
      {
         return this.reader.Peek();
      }

      /// <summary>
      /// Pushes a character to the head of the input stream.
      /// </summary>
      /// <param name="character">The character to revert.</param>
      protected void Revert(char character)
      {
         this.reader.Push(character);
      }

      #region Nested type: ReadState

      /// <summary>
      /// Represents a position of a reader.
      /// </summary>
      protected enum ReadState
      {
         /// <summary>
         /// Unquoted field.
         /// </summary>
         Default,

         /// <summary>
         /// Quoted field.
         /// </summary>
         Quoted,

         /// <summary>
         /// Escaped character.
         /// </summary>
         Escaped,

         /// <summary>
         /// End of a field.
         /// </summary>
         EndOfField,

         /// <summary>
         /// End of a record.
         /// </summary>
         EndOfRecord,

         /// <summary>
         /// End of a stream.
         /// </summary>
         EndOfStream,

         /// <summary>
         /// Stream has been closed.
         /// </summary>
         Closed,
      }

      #endregion
   }
}
