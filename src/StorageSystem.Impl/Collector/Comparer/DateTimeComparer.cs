using System;

namespace Qoollo.Impl.Collector.Comparer
{
    internal static class DateTimeComparer
    {
        public static int Compare(object value1, object value2)
        {
            var v1 = (DateTime)value1;
            var v2 = (DateTime)value2;

            if (v1 == v2)
                return 0;

            if (v1 < v2)
                return -1;

            return 1;
        }
    }
}
