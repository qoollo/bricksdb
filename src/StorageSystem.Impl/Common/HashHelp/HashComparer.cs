using System;

namespace Qoollo.Impl.Common.HashHelp
{
    public static class HashComparer
    {
        public static int Compare(object str1, object str2)
        {
            return String.CompareOrdinal((string)str1, (string)str2);
        }
    }
}
