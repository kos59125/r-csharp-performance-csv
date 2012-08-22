r-csharp-performance-csv
========================

[郵便番号データ (KEN_ALL.CSV)](http://www.post.japanpost.jp/zipcode/dl/kogaki.html) を利用して R と C# で読み取り速度を比較。

CSV リーダーは自作ライブラリーを使用 ([TextTables](https://github.com/kos59125/TextTables) で公開)。

項目
----

* C# オブジェクトマッピング: C# のクラスに CSV の行データをマッピングして読み取り
* C# 文字列配列: 行データを文字列配列として読み取り
* R stringsAsFactors=TRUE: stringsAsFactors を TRUE にして read.csv
* R stringsAsFactors=FALSE: stringsAsFactors を FALSE にして read.csv

結果
----

10 回計測しての平均値。

* C# オブジェクトマッピング: 2.749 sec
* C# 文字列配列: 0.640 sec
* R stringsAsFactors=TRUE: 7.454 sec
* R stringsAsFactors=FALSE: 3.116 sec

