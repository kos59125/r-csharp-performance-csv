using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RecycleBin.Commons.IO;
using RDotNet;

namespace Comparison
{
   class Program
   {
      static void Main(string[] args)
      {
         Stopwatch stopwatch = new Stopwatch();
         stopwatch.Start();
         using (var reader = new CsvReader("KEN_ALL.CSV"))
         {
            PostalCode c = null;
            while ((c = reader.ReadRecord<PostalCode>()) != null)
            {
            }
         }
         stopwatch.Stop();
         Console.WriteLine("C# オブジェクトマッピング: {0}", stopwatch.Elapsed);

         stopwatch.Restart();
         using (var reader = new CsvReader("KEN_ALL.CSV"))
         {
            string[] c = null;
            while ((c = reader.ReadRecordRaw()) != null)
            {
            }
         }
         stopwatch.Stop();
         Console.WriteLine("C# 文字列配列: {0}", stopwatch.Elapsed);

         Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + Path.PathSeparator + FindRPath());
         using (var engine = REngine.CreateInstance("RDotNet"))
         {
            engine.Initialize();

            stopwatch.Restart();
            engine.Evaluate("read.csv(\"KEN_ALL.CSV\", header=FALSE, stringsAsFactors=TRUE)");
            stopwatch.Stop();
            Console.WriteLine("R stringsAsFactors=TRUE: {0}", stopwatch.Elapsed);

            stopwatch.Restart();
            engine.Evaluate("read.csv(\"KEN_ALL.CSV\", header=FALSE, stringsAsFactors=FALSE)");
            stopwatch.Stop();
            Console.WriteLine("R stringsAsFactors=FALSE: {0}", stopwatch.Elapsed);
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
   }
}
