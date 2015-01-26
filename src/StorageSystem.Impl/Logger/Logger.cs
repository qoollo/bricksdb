namespace Qoollo.Impl.Logger
{
    /// <summary>
    /// Local logger
    /// </summary>
    public class Logger : Qoollo.Logger.Logger
    {
        private static Logger _instance = new Logger(Qoollo.Logger.LogLevel.FullLog, Qoollo.Logger.LoggerDefault.EmptyLogger);
        
        public static Logger Instance
        {
            get { return _instance; }
        }

        [Qoollo.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Qoollo.Logger.ILogger innerLogger)
        {
            _instance = new Logger(innerLogger.Level, innerLogger);
        }

        private Logger(Qoollo.Logger.LogLevel logLevel, Qoollo.Logger.ILogger innerLogger)
            : base(logLevel, "StorageSystemLogger.Impl", innerLogger)
        {
        }
    }
}
