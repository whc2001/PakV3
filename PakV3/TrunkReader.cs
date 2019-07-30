using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakV3
{
    class TrunkReader : BinaryReader
    {
        public UInt32 TotalLength { get; private set; }

        public TrunkReader(Stream stream) : base(stream)
        {
            this.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            this.TotalLength = this.ReadUInt32();
            this.BaseStream.Seek(0x0200, SeekOrigin.Begin);
        }

        public TrunkAddress ReadNextResource()
        {
            byte[] buffer = new byte[20];
            int read = this.BaseStream.Read(buffer, 0, buffer.Length);
            if (read < 20)
                throw new EndOfStreamException("无法读取下一个完整对象, 已经读取到末尾?");
            TrunkAddress address = new TrunkAddress()
            {
                Token = BitConverter.ToUInt64(buffer.Take(8).Reverse().ToArray(), 0),
                Offset = BitConverter.ToInt64(buffer.Skip(8).Take(8).ToArray(), 0),
                Length = BitConverter.ToInt32(buffer.Skip(16).Take(4).ToArray(), 0)
            };
            return address;
        }

    }
}
