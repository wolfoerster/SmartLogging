using System.IO;

namespace SmartLogging;

/// <summary>
/// Settings to initialize the LogWriter.
/// </summary>
public class LogSettings
{
    /// <summary>
    /// Gets or sets the minimum log level which will be processed.
    /// Log entries with a log level smaller than this value will not be processed.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// If true, log entries are written to the console.
    /// </summary>
    public bool LogToConsole { get; set; } = false;

    /// <summary>
    /// If true, log entries are written to a file.
    /// </summary>
    public bool LogToFile { get; set; } = true;

    /// <summary>
    /// The name of the log file which is used when LogToFile is true.
    /// If this is null the name of the entry assembly is used for the file name, the extension
    /// will be '.log' and the file will be located in the current user's temporary directory.
    /// </summary>
    public string LogFileName { get; set; } = null;

    /// <summary>
    /// The maximum size of the log file (default is 16 MB).
    /// If the log file exceeds the maximum size it will be copied to a file who's name is
    /// the original name plus '.log' (e.g. MyApp.log.log) and the original file is cleared.
    /// </summary>
    public long MaxLogFileSize { get; set; } = 16 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the time in seconds the LogWriter is buffering log entries
    /// before they are written to the output. Valid values are between 0.1 and 10.
    /// </summary>
    public double BufferingTime { get; set; } = 0.9;

    /// <summary>
    /// An optional stream where log entries are written to.
    /// </summary>
    public Stream LogStream { get; set; } = null;
}
