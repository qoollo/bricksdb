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

        private static object FindValue(SearchData data, string name)
        {
            var fields = data.Fields;
            for (int i = 0; i < fields.Count; i++)
                if (string.Equals(fields[i].Item2, name, StringComparison.OrdinalIgnoreCase))
                    return fields[i].Item1;

            throw new InvalidOperationException($"Field with name '{name}' is not found inside results");
        }

        public static int Compare(SearchData data1, SearchData data2, FieldDescription description)
        {
            object value1 = FindValue(data1, description.AsFieldName);
            object value2 = FindValue(data2, description.AsFieldName);

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
