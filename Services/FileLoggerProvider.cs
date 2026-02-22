using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace ECoopSystem.Services;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public FileLoggerProvider(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_logFilePath, categoryName, _lock);
    }

    public void Dispose()
    {
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly string _categoryName;
        private readonly object _lock;

        public FileLogger(string logFilePath, string categoryName, object lockObj)
        {
            _logFilePath = logFilePath;
            _categoryName = categoryName;
            _lock = lockObj;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] [{logLevel}] {_categoryName}: {message}";

            if (exception != null)
            {
                logEntry += $"{Environment.NewLine}Exception: {exception}";
            }

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Silently ignore file write errors to prevent app crashes
                }
            }
        }
    }
}
