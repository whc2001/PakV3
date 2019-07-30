using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakV3
{
    class Program
    {
        const string logFile = @"E:\JX3Dump\Run.log";
        static void Log(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(logFile, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  {message}{Environment.NewLine}");
        }

        static void Main(string[] args)
        {
            File.WriteAllText(logFile, null);
            string dumpDir = @"E:\JX3DUMP";
            FileStream fs = new FileStream(@"E:\JX3\Main\JX3HD\PakV3\TRUNK.DIR", FileMode.Open);
            TrunkReader tr = new TrunkReader(fs);
            PackageManager pm = new PackageManager(@"E:\JX3\Main\JX3HD\PakV3\");
            Log($"Total Length = {tr.TotalLength}");
            int totalLengthStrLen = tr.TotalLength.ToString().Length;
            int i = 0;
            PackageDataDecompressor decomp = new PackageDataDecompressor();
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            while(true)
            {
                try
                {
                    int packageId = 0;
                    TrunkAddress item = tr.ReadNextResource();
                    string name = item.Token.ToString("X16");
                    PackageData data = new PackageData(pm.GetData(item.Offset, item.Length, out packageId));
                    byte[] content = decomp.GetDecompressedData(data.CompressType, data.RawData, data.DecompressedLength);
                    string extension = JX3FileTypeRecognizer.GetExtension(content);
                    DirectoryInfo dir = new DirectoryInfo($"{dumpDir}\\{packageId}");
                    if (!dir.Exists)
                        dir.Create();
                    File.WriteAllBytes($"{dir}\\{name}.{extension}", content);
                    Log($"{(++i).ToString()} / {tr.TotalLength}    Dumped: {name}.{extension} at 0x{item.Offset.ToString("X")}, source length is {item.Length}, data block length is {data.RawLength}, compress type is {data.CompressType.ToString()}, decompressed length is {data.DecompressedLength}");
                }
                catch(EndOfStreamException ex)
                {
                    break;
                }
                catch(Exception ex)
                {
                    Log($">>> Error: {ex.Message}");
                }
            }
            sw.Stop();
            Log($"Finished, elapsed time = {sw.Elapsed.ToString("dd':'hh':'mm':'ss'.'fff")}");
            Console.ReadKey();
        }
    }
}
