using System.Globalization;
using System.Numerics;
using Ninject;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.HashFile
{
    internal class HashWriter:HashMap
    {
        private readonly int _countSlices;

        public HashWriter(StandardKernel kernel, string filename, int countSlices) 
            : base(kernel, HashFileType.Writer, filename)
        {
            _countSlices = countSlices;
            CreateNewMap();
        }

        public void SetServer(int index, string host, int portForDistributor, int portForCollector)
        {
            Map[index].Save = new SavedServerId( host, portForDistributor, portForCollector);
        }

        public void Save()
        {            
            CreateNewFile();
        }

        public void CreateNewMap()
        {
            var maxVal = BigInteger.Parse("100000000000000000000000", NumberStyles.HexNumber) * 0x10 - 1;
            var minVal = BigInteger.Parse("0", NumberStyles.HexNumber);
            var step = maxVal / _countSlices;
            BigInteger current;
            string min, cur;

            for (int i = 0; i < _countSlices - 1; i++)
            {
                current = minVal + step;

                min = i == 0 ? "000000000000000000000000" : minVal.ToString("x2");
                cur = current.ToString("x2");

                Map.Add(new HashMapRecord(min, cur));

                minVal = current;
            }

            min = _countSlices == 1 ? "000000000000000000000000" : minVal.ToString("x2");
            cur = maxVal.ToString("x2");

            Map.Add(new HashMapRecord(min, cur));
        }
    }
}
