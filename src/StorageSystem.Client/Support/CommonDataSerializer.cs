using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Qoollo.Client.Support
{
    public static class CommonDataSerializer
    {
        private static IFormatter _formatter = new BinaryFormatter();

        public static byte[] Serialize(object obj)
        {
            var ms = new MemoryStream();
            _formatter.Serialize(ms, obj);

            byte[] body = ms.GetBuffer();

            if (body.Length == ms.Length)
                return body;

            byte[] ret = new byte[ms.Length];
            Buffer.BlockCopy(body, 0, ret, 0, (int)ms.Length);
            return ret;
        }

        public static T Deserialize<T>(byte[] buffer) 
        {
            var ms = new MemoryStream(buffer);
            T body = (T)_formatter.Deserialize(ms);           
            return body;
        }
    }
}
