namespace Qoollo.Impl.Sql
{
    public class CustomOperationResult
    {
        public bool IsSuccess { get; private set; }
        public string Description { get; private set; }

        /// <summary>
        // Use if no errors
        /// </summary>
        public CustomOperationResult()
        {
            IsSuccess = true;
        }

        /// <summary>
        /// User if some error happens
        /// </summary>
        /// <param name="errorDescription"></param>
        public CustomOperationResult(string errorDescription)
        {
            IsSuccess = false;
            Description = errorDescription;
        }
    }
}
