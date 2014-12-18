namespace Qoollo.Client.Request
{
    internal interface IRequestDescription
    {
        bool IsError { get; }

        string ErrorDescription { get; }

        RequestState State { get; }

        string DistributorHash { get; }

        string CacheKey { get; }
    }
}
