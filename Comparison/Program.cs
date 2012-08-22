using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RecycleBin.TextTables;
using RDotNet;

namespace Comparison
{
   class Program
   {
      static void Main(string[] args)
      {
         Console.WriteLine("C# オブジェクトマッピング: {0}", DoBenchmark(
            () =>
            {
               using (var reader = new StreamReader("KEN_ALL.CSV"))
               using (var csv = new CsvReader(reader))
               {
                  PostalCode c = null;
                  while (csv.MoveNext())
                  {
                     c = csv.Current.Convert<PostalCode>();
                  }
               }
            }
         ));

         Console.WriteLine("C# 文字列配列: {0}", DoBenchmark(
            () =>
            {

               using (var reader = new StreamReader("KEN_ALL.CSV"))
               using (var csv = new CsvReader(reader))
               {
                  Record record;
                  while (csv.MoveNext())
                  {
                     record = csv.Current;
                  }
               }
            }
         ));

         Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + Path.PathSeparator + FindRPath());
         using (var engine = REngine.CreateInstance("RDotNet"))
         {
            engine.Initialize();

            Console.WriteLine("R stringsAsFactors=TRUE: {0}", DoBenchmark(
               () => engine.Evaluate("read.csv(\"KEN_ALL.CSV\", header=FALSE, stringsAsFactors=TRUE)")
            ));

            Console.WriteLine("R stringsAsFactors=FALSE: {0}", DoBenchmark(
               () => engine.Evaluate("read.csv(\"KEN_ALL.CSV\", header=FALSE, stringsAsFactors=FALSE)")
            ));
         }
      }

      private static string FindRPath()
      {
         Microsoft.Win32.RegistryKey rCore = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\R-core");
         if (rCore == null)
         {
            throw new System.ApplicationException("Registry key is not found.");
         }
         bool is64Bit = System.Environment.Is64BitProcess;
         Microsoft.Win32.RegistryKey r = rCore.OpenSubKey(is64Bit ? "R64" : "R");
         if (r == null)
         {
            throw new System.ApplicationException("Registry key is not found.");
         }
         System.Version currentVersion = new System.Version((string)r.GetValue("Current Version"));
         string installPath = (string)r.GetValue("InstallPath");
         string bin = System.IO.Path.Combine(installPath, "bin");
         // Up to 2.11.x, DLLs are installed in R_HOME\bin.
         // From 2.12.0, DLLs are installed in the one level deeper directory.
         return currentVersion < new System.Version(2, 12) ? bin : System.IO.Path.Combine(bin, is64Bit ? "x64" : "i386");
      }

      private static TimeSpan DoBenchmark(Action action, int replications = 10)
      {
         var stopwatch = new Stopwatch();
         return Enumerable.Repeat(0, replications).Select(
            _ =>
            {
               stopwatch.Restart();
               action();
               stopwatch.Stop();
               return stopwatch.Elapsed;
            }
         ).Aggregate(TimeSpan.Zero, (acc, lap) => acc + lap, total => TimeSpan.FromMilliseconds(total.TotalMilliseconds / replications));
      }
   }
}
