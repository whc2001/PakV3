using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PakV3
{
    public static class Unmanaged
    {
        [DllImport("lzo.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lzo_init();

        [DllImport("lzo.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lzo1x_decompress(
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] src,
          int src_len,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] dst,
          int dst_len,
          IntPtr wrkmem);
    }
}
