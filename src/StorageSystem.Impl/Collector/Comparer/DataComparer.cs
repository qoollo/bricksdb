using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Collector.Comparer
{
    internal static class DataComparer
    {
        public static int Compare(SearchData data1, SearchData data2, FieldDescription description)
        {
            object value1 = data1.Key;
            object value2 = data2.Key;

            if (description.SystemFieldType == typeof (int) ||
                description.SystemFieldType == typeof (Int16) ||
                description.SystemFieldType == typeof (Int32))
                return IntComparer.Compare(value1, value2);

            if (description.SystemFieldType == typeof(Int64))
                return Int64Comparer.Compare(value1, value2);

            if (description.SystemFieldType == typeof (DateTime))
                return DateTimeComparer.Compare(value1, value2);

            return Consts.CompareFailed;
        }

        public static int Compare(SearchData data1, SearchData data2, List<FieldDescription> descriptions)
        {
            foreach (var fieldDescription in descriptions)
            {
                var result = Compare(data1, data2, fieldDescription);
                if (result != 0)
                    return result;
            }

            return Consts.CompareFailed;
        }
    }
}
