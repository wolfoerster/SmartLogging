using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SmartLogging;

/// <summary>
/// Settings to initialize the LogWriter.
/// </summary>
public class LogSettings
{
    /// <summary>
    /// If true, log entries are written to a file specified by <see cref="LogSettings.LogFileName"/>.
    /// </summary>
    public bool LogToFile { get; set; } = true;

    /// <summary>
    /// If true, log entries are written to a stream specified by <see cref="LogSettings.LogStream"/>.
    /// </summary>
    public bool LogToStream { get; set; } = false;

    /// <summary>
    /// If true, log entries are written to the console.
    /// </summary>
    public bool LogToConsole { get; set; } = false;

    /// <summary>
    /// If true, log entries are written to a queue.
    /// </summary>
    public bool LogToQueue { get; set; } = false;

    /// <summary>
    /// Gets or sets the overall minimum log level which will be processed.
    /// Log entries with a log level smaller than this value will not be processed,
    /// except there is a context specific minimum level specified in property MinimumLogLevels.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets context specific minimum log levels which override the overall minimum log level.
    /// Examples:
    /// <para/>
    /// MinimumLogLevels["Context1"] = LogLevel.Debug will add a specific minimum log level for loggers 
    /// whose context is exactly "Context1".
    /// <para/>
    /// MinimumLogLevels["Context*"] = LogLevel.Debug will add a specific minimum log level for loggers 
    /// whose context starts with "Context". Note that '*' is only supported at the end of the context specifier.
    /// <para/>
    /// Since the log context in most cases is set to the full type name of the class which calls a
    /// log method, you can specify minimum log levels based on namespaces or class names. Examples:
    /// <para/>
    /// MinimumLogLevels["Application1.Module1.Class1"] = LogLevel.Debug will only affect log entries from 'Class1'.
    /// <para/>
    /// MinimumLogLevels["Application1.Module1.*"] = LogLevel.Debug will affect all classes from 'Module1'.
    /// </summary>
    public Dictionary<string, LogLevel> MinimumLogLevels { get; } = new() { { "SmartLogging", LogLevel.Information } };

    /// <summary>
    /// Gets or sets the time in seconds the LogWriter is buffering log entries
    /// before they are written to the output. Valid values are between 0.1 and 10.
    /// </summary>
    public double BufferingTime { get; set; } = 0.9;

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
    /// The stream which is used when LogToStream is true.
    /// </summary>
    [JsonIgnore]
    public Stream LogStream { get; set; } = null;

    /// <summary>
    /// The queue which is used when LogToQueue is true.
    /// </summary>
    [JsonIgnore]
    public ConcurrentQueue<string> LogQueue { get; set; } = null;
}
