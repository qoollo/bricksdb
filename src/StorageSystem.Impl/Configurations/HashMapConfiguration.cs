using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.HashFile;

namespace Qoollo.Impl.Configurations
{
    internal class HashMapConfiguration
    {
        public  HashFileType Type { get; private set; }

        /// <summary>
        /// File with server addresses and hashes
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Start mode.
        /// Create - create new file
        /// Open - read file.
        /// </summary>
        public HashMapCreationMode Mode { get; private set; }

        /// <summary>
        /// Count slices in hash file
        /// </summary>
        public int CountSlices { get; private set; }

        public int CountReplics { get; private set; }

        public HashMapConfiguration(string filename, HashMapCreationMode mode, int countSlices, int countReplics, HashFileType type)
        {            
            Contract.Requires(filename!="");
            Contract.Requires(countSlices>0);
            Contract.Requires(countReplics>0);
            Type = type;
            Filename = filename;
            Mode = mode;
            CountSlices = countSlices;
            CountReplics = countReplics;
        }
    }
}
