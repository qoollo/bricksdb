using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Benchmark
{
    class KeyGenerator
    {
        private readonly long _keyRange;
        private Random _rand;

        public KeyGenerator(long keyRange)
        {
            _keyRange = keyRange;
            _rand = new Random();
        }

        public long Generate()
        {
            return LongRandom(0, _keyRange, _rand);
        }

        private long LongRandom(long min, long max, Random rand)
        {
            long result = rand.Next((Int32)(min >> 32), (Int32)(max >> 32));
            result = (result << 32);
            result = result | rand.Next((Int32)min, (Int32)max);
            return result;
        }

    }
}
