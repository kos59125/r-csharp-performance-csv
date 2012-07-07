using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RecycleBin.Commons.IO;

namespace Comparison
{
   public class PostalCode
   {
      [Column(0)]
      public string 全国地方公共団体コード { get; set; }
      [Column(1)]
      public string 旧郵便番号 { get; set; }
      [Column(2)]
      public string 郵便番号 { get; set; }
      [Column(3)]
      public string 都道府県名カナ { get; set; }
      [Column(4)]
      public string 市区町村名カナ { get; set; }
      [Column(5)]
      public string 町域名カナ { get; set; }
      [Column(6)]
      public string 都道府県名 { get; set; }
      [Column(7)]
      public string 市区町村名 { get; set; }
      [Column(8)]
      public string 町域名 { get; set; }
      [Column(9, ParserType = typeof(EnumParser<該当>))]
      public 該当 一町域が二以上の郵便番号で表される場合の表示 { get; set; }
      [Column(10, ParserType = typeof(EnumParser<該当>))]
      public 該当 小字毎に番地が起番されている町域の表示 { get; set; }
      [Column(11, ParserType = typeof(EnumParser<該当>))]
      public 該当 丁目を有する町域の場合の表示 { get; set; }
      [Column(12, ParserType = typeof(EnumParser<該当>))]
      public 該当 一つの郵便番号で二以上の町域を表す場合の表示 { get; set; }
      [Column(13, ParserType = typeof(EnumParser<更新>))]
      public 更新 更新の表示 { get; set; }
      [Column(14, ParserType = typeof(EnumParser<該当>))]
      public 変更 変更の理由 { get; set; }
      //[Column(9)]
      //public string 一町域が二以上の郵便番号で表される場合の表示 { get; set; }
      //[Column(10)]
      //public string 小字毎に番地が起番されている町域の表示 { get; set; }
      //[Column(11)]
      //public string 丁目を有する町域の場合の表示 { get; set; }
      //[Column(12)]
      //public string 一つの郵便番号で二以上の町域を表す場合の表示 { get; set; }
      //[Column(13)]
      //public string 更新の表示 { get; set; }
      //[Column(14)]
      //public string 変更の理由 { get; set; }
   }

   public enum 該当
   {
      該当せず,
      該当
   }

   public enum 更新
   {
      変更なし,
      変更あり,
      廃止
   }

   public enum 変更
   {
      変更なし,
      市政・区政・町政・分区・政令指定都市施行,
      住居表示の実施,
      区画整理,
      郵便区調整等,
      訂正,
      廃止
   }

   public class EnumParser<T>
   {
      public object Parse(string value)
      {
         return Enum.ToObject(typeof(T), int.Parse(value));
      }
   }
}
