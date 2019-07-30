using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakV3
{
    public class PackageData
    {
        public UInt64 Unknown { get; private set; }
        public Int32 RawLength { get; private set; }
        public PackageDataCompressType CompressType { get; private set; }
        public Int32 DecompressedLength { get; private set; }
        public byte[] RawData { get; private set; }

        public PackageData(byte[] data)
        {
            this.Unknown = BitConverter.ToUInt64(data.Take(8).ToArray(), 0);
            this.RawLength = BitConverter.ToInt32(data.Skip(28).Take(4).ToArray(), 0);
            this.CompressType = (PackageDataCompressType)BitConverter.ToInt32(data.Skip(36).Take(4).ToArray(), 0);
            this.DecompressedLength = BitConverter.ToInt32(data.Skip(12).Take(4).ToArray(), 0);
            this.RawData = data.Skip(56).ToArray();
        }
    }
}
