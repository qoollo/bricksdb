using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class CollectorConfiguration
    {
        public CollectorConfiguration(string fileWithHashName, int countReplics, int pageSize, bool userHashFile = true)
        {            
            Contract.Requires(fileWithHashName != "");
            Contract.Requires(countReplics>0);
            Contract.Requires(pageSize>0);
            PageSize = pageSize;
            CountReplics = countReplics;
            FileWithHashName = fileWithHashName;
            UseHashFile = userHashFile;
        }

        public CollectorConfiguration(string fileWithHashName, int countReplics)
            :this(fileWithHashName, countReplics, Consts.PageSize)
        {
        }

        public CollectorConfiguration(string fileWithHashName)
            : this(fileWithHashName, Consts.CountReplics, Consts.PageSize)
        {
        }

        public CollectorConfiguration()
            : this(Consts.FileWithHashName, Consts.CountReplics, Consts.PageSize)
        {
        }


        /// <summary>
        /// File with server addresses and hashes
        /// </summary>
        public string FileWithHashName { get; private set; }

        /// <summary>
        /// Replic counts
        /// </summary>
        public int CountReplics { get; private set; }

        /// <summary>
        /// Size of page loaded from page
        /// </summary>
        public int PageSize { get; private set; }

        public bool UseHashFile { get; private set; }
    }
}
