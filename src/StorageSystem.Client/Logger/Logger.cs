namespace Qoollo.Client.Logger
{
    /// <summary>
    /// Local logger
    /// </summary>
    internal class Logger : Qoollo.Logger.Logger
    {
        private static Logger _instance = new Logger(Qoollo.Logger.LogLevel.FullLog, Qoollo.Logger.LoggerDefault.EmptyLogger);
        
        public static Logger Instance
        {
            get { return _instance; }
        }

        [Qoollo.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Qoollo.Logger.ILogger innerLogger)
        {
            InitializeLoggerInAssembly(innerLogger, typeof (Qoollo.Impl.Logger.Logger).Assembly);
        }

        private Logger(Qoollo.Logger.LogLevel logLevel, Qoollo.Logger.ILogger innerLogger)
            : base(logLevel, "StorageSystemLogger.Client", innerLogger)
        {
        }
    }
}
