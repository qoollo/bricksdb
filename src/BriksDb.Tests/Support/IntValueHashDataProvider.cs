using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Support;

namespace Qoollo.Tests.Support
{
    class IntValueHashDataProvider : CommonDataProvider<int, int>
    {
        public override string CalculateHashFromKey(int key)
        {
            return key.ToString(CultureInfo.InvariantCulture);
        }

        public override string CalculateHashFromValue(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
