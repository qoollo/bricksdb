namespace Qoollo.Impl.Collector.Comparer
{
    internal static class Int64Comparer
    {
        public static int Compare(object value1, object value2)
        {
            var v1 = (long)value1;
            var v2 = (long)value2;

            if (v1 == v2)
                return 0;

            if (v1 < v2)
                return -1;

            return 1;
        }
    }
}