using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class TransactionConfiguration
    {
        public int ElementsCount { get; private set; }

        public TransactionConfiguration(int elementsCount)
        {
            Contract.Requires(elementsCount>0);
            ElementsCount = elementsCount;
        }
    }
}
