namespace Qoollo.Client.Logger
{
    /// <summary>
    /// Local logger
    /// </summary>
    internal class Logger : Libs.Logger.Logger
    {
        private static Logger _instance = new Logger(Libs.Logger.LogLevel.FullLog, EmptyLogger);
        
        public static Logger Instance
        {
            get { return _instance; }
        }
        
        [Libs.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Libs.Logger.ILogger innerLogger)
        {
            InitializeLoggerInAssembly(innerLogger, typeof (Qoollo.Impl.Logger.Logger).Assembly);
        }

        private Logger(Libs.Logger.LogLevel logLevel, Libs.Logger.ILogger innerLogger)
            : base(logLevel, "StorageSystemLogger.Client", innerLogger)
        {
        }
    }
}
