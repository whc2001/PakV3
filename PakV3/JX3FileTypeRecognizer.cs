using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace PakV3
{
    public static class JX3FileTypeRecognizer
    {
        #region Constants
        private const string DEFAULT_EXTENSION_BINARY = "BIN";
        private const string DEFAULT_EXTENSION_TEXT = "TXT";
        private static readonly byte[] tgaHeader = { 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private static readonly byte[] ttfHeader = { 0x5F, 0x0F, 0x3C, 0xF5 };
        private static readonly List<byte[]> bomHeader = new List<byte[]>() {
            new byte[] { 0xEF, 0xBB, 0xBF },
            new byte[] { 0xFE, 0xFF },
            new byte[] { 0xFF, 0xFE },
            new byte[] { 0x00, 0x00, 0xFE, 0xFF },
            new byte[] { 0x00, 0x00, 0xFF, 0xFE },
        };
        private static readonly string[] cppContent = { "#include", "namespace" };
        private static readonly string[] luaContent = { "function", "end" };
        private static readonly Regex iniPattern = new Regex(@"(\[.+\]\r*\n*(.*=.*\r*\n*){0,}){1,}", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex xmlPattern = new Regex(@"(<.*?/{0,1}>){1,}", RegexOptions.Compiled | RegexOptions.Multiline);
        #endregion

        #region Private functions

        private static bool IsMask(uint data, uint mask) => (data & mask) == mask;
        private static uint SwitchEndian(uint value) => (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 | (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        private static int PatternIndexOf(byte[] source, byte[] pattern)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    return i;
                }
            }
            return -1;
        }
        private static Encoding GetEncodingFromHeader(byte[] content)
        {
            var bom = content.Take(4).ToArray();
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode;
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode;
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return null;
        }
        private static bool IsPlainText(byte[] content, out Encoding assumedEncoding)
        {
            foreach (byte b in content)
            {
                if (b == 0x00)
                {
                    Encoding encoding = GetEncodingFromHeader(content);
                    assumedEncoding = encoding;
                    if (encoding == null)
                        return false;
                    else
                        return true;
                }
            }
            assumedEncoding = Encoding.Default;
            return true;
        }

        #endregion

        private static string strBuf;

        private static readonly Dictionary<string, Func<byte[], bool>> binaryFormats = new Dictionary<string, Func<byte[], bool>>()
        {
            { "ANI", IsAni },
            { "AVI", IsAvi },
            { "BMP", IsBmp },
            { "DDS", IsDds },
            { "FSB", IsFsb },
            { "ICO", IsIco },
            { "IFF", IsIff },
            { "JPEG", IsJpeg },
            { "MP3", IsMp3 },
            { "MP4", IsMp4 },
            { "OGG", IsOgg },
            { "PNG", IsPng },
            { "PSD", IsPsd },
            { "RAR", IsRar },
            { "TGA", IsTga },
            { "CUR", IsCur },
            { "TTF", IsTtf },
            { "WAV", IsWav },
            { "ZIP", IsZip }
        };

        private static readonly Dictionary<string, Func<string, bool>> textFormats = new Dictionary<string, Func<string, bool>>()
        {
            { "JSON", IsJson },
            { "XML", IsXml },
            { "INI", IsIni },
            { "CPP", IsCpp },
            { "LUA", IsLua },
        };

        #region Audio

        public static bool IsMp3(byte[] data)
        {
            #region ID3V2
            // "ID3", first 3 bytes
            if (data[0] == 'I' && data[1] == 'D' && data[2] == '3')
                return true;
            #endregion

            #region ID3V1
            // "TAG", start of the last 128 bytes
            if (data[data.Length - 0x80] == 'T' && data[data.Length - 0x81] == 'A' && data[data.Length - 0x82] == 'G')
                return true;
            #endregion

            #region Raw data
            uint head = SwitchEndian(BitConverter.ToUInt32(data.Take(4).ToArray(), 0));
            // Sync head (31~21bit all 1)
            if (IsMask(head, 0xFFE00000))
            {
                //MPEG-1 (20~19bit is 11)
                if (IsMask(head, 0x1A0000))
                    //Layer 3 (18~17bit is 01)
                    if (IsMask(head, 0x20000))
                        return true;
            }
            #endregion

            return false;
        }

        public static bool IsOgg(byte[] data)
        {
            return data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S';
        }

        public static bool IsWav(byte[] data)
        {
            return (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F')
                && (data[8] == 'W' && data[9] == 'A' && data[10] == 'V' && data[11] == 'E');
        }

        public static bool IsFsb(byte[] data)
        {
            return data[0] == 'F' && data[1] == 'S' && data[2] == 'B' && data[3] == '5';
        }

        #endregion

        #region Video

        public static bool IsMp4(byte[] data)
        {
            return data[4] == 'f' && data[5] == 't' && data[6] == 'y' && data[7] == 'p';
        }

        public static bool IsAvi(byte[] data)
        {
            return (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F')
                && (data[8] == 'A' && data[9] == 'V' && data[10] == 'I' && data[11] == ' ');
        }

        #endregion

        #region Graphics

        public static bool IsPng(byte[] data)
        {
            return data[0] == 0x89 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G';
        }

        public static bool IsJpeg(byte[] data)
        {
            return (data[0] == 0xFF && data[1] == 0xD8)
                && (data[data.Length - 2] == 0xFF && data[data.Length - 1] == 0xD9);
        }

        public static bool IsBmp(byte[] data)
        {
            return (data[0] == 'B' && data[1] == 'M')
                && (BitConverter.ToInt32(data.Skip(2).Take(4).ToArray(), 0) == data.Length);
        }

        public static bool IsTga(byte[] data)
        {
            return data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x02 && data[3] == 0x00
                && data[4] == 0x00 && data[5] == 0x00 && data[6] == 0x00 && data[7] == 0x00;
        }

        public static bool IsDds(byte[] data)
        {
            return data[0] == 'D' && data[1] == 'D' && data[2] == 'S' && data[3] == ' ';
        }

        public static bool IsPsd(byte[] data)
        {
            return data[0] == '8' && data[1] == 'B' && data[2] == 'P' && data[3] == 'S';
        }

        public static bool IsIff(byte[] data)
        {
            return (data[0] == 'F' && data[1] == 'O' && data[2] == 'R' && data[3] == '4')
                && (data[8] == 'C' && data[9] == 'I' && data[10] == 'M' && data[11] == 'G');
        }

        //MUST BE JUDGED AFTER TGA FORMAT!!!!
        public static bool IsCur(byte[] data)
        {
            return data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x02 && data[3] == 0x00;
        }

        public static bool IsAni(byte[] data)
        {
            return (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F')
                && (data[8] == 'A' && data[9] == 'C' && data[10] == 'O' && data[11] == 'N');
        }

        public static bool IsIco(byte[] data)
        {
            return data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0x00;
        }

        #endregion

        #region Script

        public static bool IsJson(string data)
        {
            data = data.Trim();
            if (cppContent.Any(i => data.Contains(i)))  // Prevent it from being recognized as CPP
                return false;
            if (data.StartsWith("{"))
                return data.EndsWith("}");
            else if (data.StartsWith("["))
                return data.EndsWith("]");
            return false;
        }

        public static bool IsIni(string data)
        {
            return data.StartsWith("[") && iniPattern.IsMatch(data);
        }

        public static bool IsXml(string data)
        {
            return (data[0] == '<' && data[1] == '?' && data[2] == 'x' && data[3] == 'm' && data[4] == 'l')
                || (data[0] == '<' && data[1] == '!' && data[2] == '-' && data[3] == '-')
                || xmlPattern.IsMatch(data);
        }

        public static bool IsCpp(string data)
        {
            foreach (var item in cppContent)
                if (data.Contains(item))
                    return true;
            return false;
        }

        public static bool IsLua(string data)
        {
            foreach (var item in luaContent)
                if (data.Contains(item))
                    return true;
            return false;
        }

        #endregion

        #region Container

        public static bool IsRar(byte[] data)
        {
            return data[0] == 'R' && data[1] == 'a' && data[2] == 'r' && data[3] == '!';
        }

        public static bool IsZip(byte[] data)
        {
            return data[0] == 'P' && data[1] == 'K' && data[2] == 0x03 && data[3] == 0x04;
        }

        #endregion

        #region Misc

        public static bool IsTtf(byte[] data)
        {
            return data.Skip(264).Take(4).SequenceEqual(ttfHeader);
        }

        #endregion

        public static string GetExtension(byte[] data)
        {
            Encoding encoding;
            if (IsPlainText(data, out encoding))
            {
                strBuf = encoding.GetString(data);
                foreach (var txtFmt in textFormats)
                {
                    try
                    {
                        if (txtFmt.Value(strBuf))
                            return txtFmt.Key;
                    }
                    catch { }
                }
                return DEFAULT_EXTENSION_TEXT;
            }
            else
            {
                foreach (var binFmt in binaryFormats)
                {
                    try
                    {
                        if (binFmt.Value(data))
                            return binFmt.Key;
                    }
                    catch { }
                }
                return DEFAULT_EXTENSION_BINARY;
            }
        }
    }
}
