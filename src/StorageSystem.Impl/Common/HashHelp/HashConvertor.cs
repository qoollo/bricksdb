using System;
using System.Security.Cryptography;
using System.Text;

namespace Qoollo.Impl.Common.HashHelp
{
    public static class HashConvertor
    {
        private static MD5 _alg = MD5.Create();
        private static Random _random = new Random();
        private static object _lock = new object();

        private static byte[] GetBytes(string str)
        {
            byte[] data;
            lock (_lock)
            {
                data = _alg.ComputeHash(Encoding.UTF8.GetBytes(str));
            }
            return data;
        }

        public static string GetString(string str)
        {
            byte[] data = GetBytes(str);

            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static UInt64 GetInt64(string str)
        {
            byte[] data = GetBytes(str);

            return BitConverter.ToUInt64(data, 0);
        }

        public static string GetRandomHash()
        {
            string str = _random.Next().ToString();
            return GetString(str);
        }
    }
}
