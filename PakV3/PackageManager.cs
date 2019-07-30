using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PakV3
{
    public class PackageManager : IDisposable
    {
        private SortedList<int, Package> packages = new SortedList<int, Package>();
        private long totalLength = 0;

        public byte[] GetData(Int64 start, Int64 length, out int package)
        {
            if (start > this.totalLength)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (start + length > this.totalLength)
                throw new ArgumentOutOfRangeException(nameof(length));
            long len = 0;
            int packageId = 0;
            foreach (KeyValuePair<int, Package> item in packages)
            {
                len += item.Value.Size;
                if (len > start)
                {
                    len -= item.Value.Size;
                    packageId = item.Key;
                    break;
                }
            }
            package = packageId;
            return packages[packageId].GetData(start - len, length);
        }

        public PackageManager(string packageDir)
        {
            DirectoryInfo dir = new DirectoryInfo(packageDir);
            Regex fileNameFormat = new Regex(@"Package(\d+)\.DAT", RegexOptions.IgnoreCase);
            foreach (FileInfo file in dir.EnumerateFiles())
            {
                Match match = fileNameFormat.Match(file.Name);
                if (match.Success)
                {
                    packages.Add(int.Parse(match.Groups[1].Value), new Package(file.FullName));
                }
            }
            if (this.packages.Count == 0)
                throw new FileNotFoundException("指定的文件夹下未发现任何Package*.DAT文件");
            this.totalLength = packages.Sum(i => i.Value.Size);
        }

        public void Dispose()
        {
            foreach (KeyValuePair<int, Package> item in this.packages)
                item.Value.Dispose();
        }

        ~PackageManager()
        {
            this.Dispose();
        }
    }
}
