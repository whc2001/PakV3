using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakV3
{
    public class Package : IDisposable
    {
        public string FileName { get; private set; }
        public Int64 Size { get; private set; }

        private FileStream stream;
        private object readLock = new object();

        public Package(string fullPath)
        {
            FileInfo file = new FileInfo(fullPath);
            this.FileName = file.Name;
            this.Size = file.Length;
            this.stream = new FileStream(fullPath, FileMode.Open);
        }

        public byte[] GetData(Int64 start, Int64 length)
        {
            if (start > this.Size)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (start + length > this.Size)
                throw new ArgumentOutOfRangeException(nameof(length));
            lock (this.readLock)
            {
                byte[] buffer = new byte[length];
                this.stream.Seek(start, SeekOrigin.Begin);
                this.stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public void Dispose()
        {
            ((IDisposable)stream).Dispose();
        }

        ~Package()
        {
            this.Dispose();
        }
    }
}
