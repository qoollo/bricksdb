namespace Qoollo.Impl.Logger
{
    /// <summary>
    /// Local logger
    /// </summary>
    public class Logger : Libs.Logger.Logger
    {
        private static Logger _instance = new Logger(Libs.Logger.LogLevel.FullLog, EmptyLogger);
        
        public static Logger Instance
        {
            get { return _instance; }
        }
        
        [Libs.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Libs.Logger.ILogger innerLogger)
        {
            _instance = new Logger(innerLogger.Level, innerLogger);
        }

        private Logger(Libs.Logger.LogLevel logLevel, Libs.Logger.ILogger innerLogger)
            : base(logLevel, "StorageSystemLogger.Impl", innerLogger)
        {
        }
    }
}
