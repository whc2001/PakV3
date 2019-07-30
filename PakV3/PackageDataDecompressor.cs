using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Simplicit.Net.Lzo;

namespace PakV3
{
    public class PackageDataDecompressor
    {
        LZOCompressor lzo;

        public byte[] GetDecompressedData(PackageDataCompressType type, byte[] data, Int32 decompressedLength)
        {
            switch (type)
            {
                case PackageDataCompressType.NONE:
                case PackageDataCompressType.UNKNOWN1:
                case PackageDataCompressType.UNKNOWN2:
                default:
                    return data;
                case PackageDataCompressType.LZO:
                    return LzoDecompress(data, decompressedLength);
            }
        }

        private byte[] LzoDecompress(byte[] data, Int32 decompressedLength)
        {
            byte[] buf = lzo.DecompressRaw(data, decompressedLength);
            if (buf.Length == decompressedLength)
                return buf;
            else
                throw new InvalidDataException("解压LZO时出错: 长度有误");
        }

        public PackageDataDecompressor()
        {
            lzo = new LZOCompressor();
        }
    }
}
