using System;
using System.Text;

namespace ZeusNX.YoYoMD5
{
    //thank you dbgx86
    public static class YYMD5
    {
        private static int SafeAdd(int x, int y)
        {
            uint lsw = (uint)(x & 0xFFFF) + (uint)(y & 0xFFFF);
            uint msw = ((uint)(x >> 16) + (uint)(y >> 16) + (lsw >> 16)) & 0xFFFF;
            return (int)((msw << 16) | (lsw & 0xFFFF));
        }

        private static int BitRotateLeft(int num, int cnt)
        {
            return (num << cnt) | (int)((uint)num >> (32 - cnt));
        }

        private static int Md5Cmn(int q, int a, int b, int x, int s, int t)
        {
            return SafeAdd(BitRotateLeft(SafeAdd(SafeAdd(a, q), SafeAdd(x, t)), s), b);
        }

        private static int Md5FF(int a, int b, int c, int d, int x, int s, int t)
            => Md5Cmn((b & c) | (~b & d), a, b, x, s, t);

        private static int Md5GG(int a, int b, int c, int d, int x, int s, int t)
            => Md5Cmn((b & d) | (c & ~d), a, b, x, s, t);

        private static int Md5HH(int a, int b, int c, int d, int x, int s, int t)
            => Md5Cmn(b ^ c ^ d, a, b, x, s, t);

        private static int Md5II(int a, int b, int c, int d, int x, int s, int t)
            => Md5Cmn(c ^ (b | ~d), a, b, x, s, t);

        private static int[] BinlMD5(
            int[] x, int len,
            int a = 1732584193, int b = -271733879,
            int c = -1732584194, int d = 271733878,
            bool norm = false)
        {
            if (!norm)
            {
                int idx1 = len >> 5;
                int idx2 = (((len + 64) >> 9) << 4) + 14;

                int needed = Math.Max(idx1 + 1, idx2 + 1);
                if (x.Length < needed)
                {
                    Array.Resize(ref x, needed);
                }

                x[idx1] |= 0x80 << (len % 32);
                x[idx2] = len;
            }

            int blocks = (x.Length + 15) & ~15;
            if (x.Length < blocks)
                Array.Resize(ref x, blocks);

            for (int i = 0; i < x.Length; i += 16)
            {
                int olda = a, oldb = b, oldc = c, oldd = d;

                a = Md5FF(a, b, c, d, x[i + 0], 7, -680876936);
                d = Md5FF(d, a, b, c, x[i + 1], 12, -389564586);
                c = Md5FF(c, d, a, b, x[i + 2], 17, 606105819);
                b = Md5FF(b, c, d, a, x[i + 3], 22, -1044525330);
                a = Md5FF(a, b, c, d, x[i + 4], 7, -176418897);
                d = Md5FF(d, a, b, c, x[i + 5], 12, 1200080426);
                c = Md5FF(c, d, a, b, x[i + 6], 17, -1473231341);
                b = Md5FF(b, c, d, a, x[i + 7], 22, -45705983);
                a = Md5FF(a, b, c, d, x[i + 8], 7, 1770035416);
                d = Md5FF(d, a, b, c, x[i + 9], 12, -1958414417);
                c = Md5FF(c, d, a, b, x[i + 10], 17, -42063);
                b = Md5FF(b, c, d, a, x[i + 11], 22, -1990404162);
                a = Md5FF(a, b, c, d, x[i + 12], 7, 1804603682);
                d = Md5FF(d, a, b, c, x[i + 13], 12, -40341101);
                c = Md5FF(c, d, a, b, x[i + 14], 17, -1502002290);
                b = Md5FF(b, c, d, a, x[i + 15], 22, 1236535329);

                a = Md5GG(a, b, c, d, x[i + 1], 5, -165796510);
                d = Md5GG(d, a, b, c, x[i + 6], 9, -1069501632);
                c = Md5GG(c, d, a, b, x[i + 11], 14, 643717713);
                b = Md5GG(b, c, d, a, x[i + 0], 20, -373897302);
                a = Md5GG(a, b, c, d, x[i + 5], 5, -701558691);
                d = Md5GG(d, a, b, c, x[i + 10], 9, 38016083);
                c = Md5GG(c, d, a, b, x[i + 15], 14, -660478335);
                b = Md5GG(b, c, d, a, x[i + 4], 20, -405537848);
                a = Md5GG(a, b, c, d, x[i + 9], 5, 568446438);
                d = Md5GG(d, a, b, c, x[i + 14], 9, -1019803690);
                c = Md5GG(c, d, a, b, x[i + 3], 14, -187363961);
                b = Md5GG(b, c, d, a, x[i + 8], 20, 1163531501);
                a = Md5GG(a, b, c, d, x[i + 13], 5, -1444681467);
                d = Md5GG(d, a, b, c, x[i + 2], 9, -51403784);
                c = Md5GG(c, d, a, b, x[i + 7], 14, 1735328473);
                b = Md5GG(b, c, d, a, x[i + 12], 20, -1926607734);

                a = Md5HH(a, b, c, d, x[i + 5], 4, -378558);
                d = Md5HH(d, a, b, c, x[i + 8], 11, -2022574463);
                c = Md5HH(c, d, a, b, x[i + 11], 16, 1839030562);
                b = Md5HH(b, c, d, a, x[i + 14], 23, -35309556);
                a = Md5HH(a, b, c, d, x[i + 1], 4, -1530992060);
                d = Md5HH(d, a, b, c, x[i + 4], 11, 1272893353);
                c = Md5HH(c, d, a, b, x[i + 7], 16, -155497632);
                b = Md5HH(b, c, d, a, x[i + 10], 23, -1094730640);
                a = Md5HH(a, b, c, d, x[i + 13], 4, 681279174);
                d = Md5HH(d, a, b, c, x[i + 0], 11, -358537222);
                c = Md5HH(c, d, a, b, x[i + 3], 16, -722521979);
                b = Md5HH(b, c, d, a, x[i + 6], 23, 76029189);
                a = Md5HH(a, b, c, d, x[i + 9], 4, -640364487);
                d = Md5HH(d, a, b, c, x[i + 12], 11, -421815835);
                c = Md5HH(c, d, a, b, x[i + 15], 16, 530742520);
                b = Md5HH(b, c, d, a, x[i + 2], 23, -995338651);

                a = Md5II(a, b, c, d, x[i + 0], 6, -198630844);
                d = Md5II(d, a, b, c, x[i + 7], 10, 1126891415);
                c = Md5II(c, d, a, b, x[i + 14], 15, -1416354905);
                b = Md5II(b, c, d, a, x[i + 5], 21, -57434055);
                a = Md5II(a, b, c, d, x[i + 12], 6, 1700485571);
                d = Md5II(d, a, b, c, x[i + 3], 10, -1894986606);
                c = Md5II(c, d, a, b, x[i + 10], 15, -1051523);
                b = Md5II(b, c, d, a, x[i + 1], 21, -2054922799);
                a = Md5II(a, b, c, d, x[i + 8], 6, 1873313359);
                d = Md5II(d, a, b, c, x[i + 15], 10, -30611744);
                c = Md5II(c, d, a, b, x[i + 6], 15, -1560198380);
                b = Md5II(b, c, d, a, x[i + 13], 21, 1309151649);
                a = Md5II(a, b, c, d, x[i + 4], 6, -145523070);
                d = Md5II(d, a, b, c, x[i + 11], 10, -1120210379);
                c = Md5II(c, d, a, b, x[i + 2], 15, 718787259);
                b = Md5II(b, c, d, a, x[i + 9], 21, -343485551);

                a = SafeAdd(a, olda);
                b = SafeAdd(b, oldb);
                c = SafeAdd(c, oldc);
                d = SafeAdd(d, oldd);
            }

            if (!norm)
            {
                int[] lmao = new int[15];
                int funnysize = len + 32;
                lmao[0] = 0x80;
                lmao[14] = funnysize;
                return BinlMD5(lmao, funnysize, a, b, c, d, true);
            }

            return new[] { a, b, c, d };
        }

        private static int[] Rstr2Binl(string input)
        {
            int[] outArr = new int[(input.Length >> 2) + 1];
            for (int i = 0; i < input.Length; i++)
            {
                outArr[i >> 2] |= (byte)input[i] << ((i % 4) * 8);
            }
            return outArr;
        }

        private static string Binl2Rstr(int[] input)
        {
            var sb = new StringBuilder(input.Length * 4);
            for (int i = 0; i < input.Length * 4; i++)
            {
                sb.Append((char)((input[i >> 2] >> ((i % 4) * 8)) & 0xFF));
            }
            return sb.ToString();
        }

        private static string RawMD5(string s)
        {
            var bin = Rstr2Binl(s);
            var hash = BinlMD5(bin, s.Length * 8);
            return Binl2Rstr(hash);
        }

        private static string Base64Encode(string data)
        {
            byte[] bytes = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                bytes[i] = (byte)data[i];
            return Convert.ToBase64String(bytes);
        }

        public static string GetResult(string value)
        {
            string md5 = RawMD5("MRJA" + value + "PHMD");
            return Base64Encode(md5);
        }

        public static string CalculateZipPassword(string zipURL)
        {
            string fname = zipURL;
            return GetResult(fname);
        }
    }
}