using Serilog;
using Serilog.Events;
using DetectiveConanRenamer.Interfaces;

namespace DetectiveConanRenamer.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly ILogger _logger;

        public LoggingService()
        {
            const string logPath = "logs/app.log";
            const int retainedFileCountLimit = 31;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedFileCountLimit,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        public void Information(string message)
        {
            _logger.Information(message);
        }

        public void Warning(string message)
        {
            _logger.Warning(message);
        }

        public void Error(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                _logger.Error(exception, message);
            }
            else
            {
                _logger.Error(message);
            }
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }
    }
} 