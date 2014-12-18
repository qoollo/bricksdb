namespace Qoollo.Client.Request
{
    public enum RequestState
    {        
        InProcess = 0,
        Complete = 1,
        Error = 2,
        TransactionInProcess = 4,
        DontExist = 3,
        DataNotFound = 5,
    }
}
