using System;
using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Collector.Comparer
{
    internal static class DataComparer
    {
        private static int MapCompareResult(int result)
        {
            if (result < 0) return -1;
            if (result > 0) return 1;
            return 0;
        }

        public static int Compare(SearchData data1, SearchData data2, FieldDescription description)
        {
            object value1 = data1.Fields.First(x => string.Equals(x.Item2, description.AsFieldName, StringComparison.OrdinalIgnoreCase)).Item1;
            object value2 = data2.Fields.First(x => string.Equals(x.Item2, description.AsFieldName, StringComparison.OrdinalIgnoreCase)).Item1;

            if (value1 != null && value1 is IComparable)
                return MapCompareResult((value1 as IComparable).CompareTo(value2));

            if (value2 != null && value2 is IComparable)
                return -MapCompareResult((value2 as IComparable).CompareTo(value1));

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

            return 0;
        }
    }
}
