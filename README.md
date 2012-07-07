r-csharp-performance-csv
========================

[郵便番号データ (KEN_ALL.CSV)](http://www.post.japanpost.jp/zipcode/dl/kogaki.html) を利用して R と C# で読み取り速度を比較。

CSV リーダーは自作ライブラリーを使用 (ライター等も含んだ完全版は [Bitbucket](https://bitbucket.org/kos59125/recyclebin-commons/) で公開)。

項目
----

* C# オブジェクトマッピング: C# のクラスに CSV の行データをマッピングして読み取り
* C# 文字列配列: 行データを文字列配列として読み取り
* R stringsAsFactors=TRUE: stringsAsFactors を TRUE にして read.csv
* R stringsAsFactors=FALSE: stringsAsFactors を FALSE にして read.csv

結果
----

1 回のみの計測。

* C# オブジェクトマッピング: 7.57 sec
* C# 文字列配列: 0.46 sec
* R stringsAsFactors=TRUE: 8.51 sec
* R stringsAsFactors=FALSE: 3.48 sec

コメント
--------

C# のオブジェクトマッピングについては Parser クラスのインスタンスをキャッシュせずにフィールドごとに毎回生成しているので遅い。
本当はもっと速くできるはず。